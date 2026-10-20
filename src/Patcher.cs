// SPDX-License-Identifier: MPL-2.0
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace BannerSagaFPSFix {
    internal sealed class PatchProfile {
        internal string OriginalHash, OriginalBodyHash, PatchedBodyHash;
        internal int BodyLength, Offset, Count;
        internal byte SwfVersion;
        internal static PatchProfile Production() {
            return new PatchProfile {
                OriginalHash="7d24a5910b9a0b3dfdae21443e3f0b7edd2510d6269d8407b6d0d31a5178a317",
                OriginalBodyHash="8bcb8396f5bcfde810adcc08855feca3e6749fb8b28f4bda4932a734caffb2d9",
                PatchedBodyHash="cbc79b96e1e18a84619280d60a7d03a4be4b12cf65025513ca60f9b14709426b",
                BodyLength=43644613, Offset=42694795, Count=10, SwfVersion=36
            };
        }
    }
    internal enum PatchState { Original, Fixed, Unsupported }
    internal sealed class Inspection {
        internal PatchState State;
        internal bool BackupPresent, BackupValid, Running;
        internal string FileHash;
    }
    internal sealed class Patcher {
        internal const string RelativeTarget="win32\\app.game-saga1.air.swf";
        internal readonly string Root, Target, Support, Backup;
        private readonly PatchProfile profile;
        private readonly Func<bool> running;
        internal Patcher(string root) : this(root, PatchProfile.Production(), IsGameRunning) {}
        internal Patcher(string root, PatchProfile profile, Func<bool> running) {
            if (String.IsNullOrWhiteSpace(root)) throw new IOException("Choose the game folder.");
            Root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (Root.StartsWith("\\\\",StringComparison.Ordinal)) throw new IOException("Use a local game folder.");
            Target=Path.Combine(Root,RelativeTarget);
            Support=Path.Combine(Root,".BannerSagaFPSFix");
            Backup=Path.Combine(Support,"original.swf");
            this.profile=profile; this.running=running;
            if (profile.Offset<0 || profile.Count<1 || profile.Offset>profile.BodyLength-profile.Count)
                throw new IOException("Invalid patch profile.");
        }
        internal static string Hash(byte[] bytes) {
            using (SHA256 sha=SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
        }
        internal static void SafePath(string path) {
            string cursor=Path.GetFullPath(path);
            while (!String.IsNullOrEmpty(cursor)) {
                try {
                    FileAttributes flags=File.GetAttributes(cursor);
                    if ((flags&FileAttributes.ReparsePoint)!=0)
                        throw new IOException("Links and junctions are not supported: "+cursor);
                } catch (FileNotFoundException) {} catch (DirectoryNotFoundException) {}
                string parent=Path.GetDirectoryName(cursor);
                if (String.Equals(parent,cursor,StringComparison.OrdinalIgnoreCase)) break;
                cursor=parent;
            }
        }
        private byte[] Read(string path) {
            SafePath(path);
            using (FileStream file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                if (file.Length<8 || file.Length>profile.BodyLength+1048576L)
                    throw new IOException("Unsupported file size.");
                byte[] result=new byte[(int)file.Length];
                int done=0;
                while (done<result.Length) {
                    int n=file.Read(result,done,result.Length-done);
                    if (n==0) throw new IOException("Incomplete file read.");
                    done+=n;
                }
                return result;
            }
        }
        internal static bool IsGameRunning() {
            Process[] processes=Process.GetProcessesByName("The Banner Saga");
            try { return processes.Length!=0; }
            finally { foreach (Process p in processes) p.Dispose(); }
        }
        private void Closed() {
            if (running()) throw new IOException("Close The Banner Saga before installing or restoring.");
        }
        internal Inspection Inspect() {
            SafePath(Target); SafePath(Support);
            if (!File.Exists(Target)) throw new IOException("Game file not found. Select the folder containing win32.");
            byte[] bytes=Read(Target);
            PatchState state=Recognize(bytes);
            bool exists=File.Exists(Backup), valid=false;
            if (exists) {
                try { valid=Hash(Read(Backup))==profile.OriginalHash; }
                catch (IOException) { valid=false; }
            }
            return new Inspection { State=state, FileHash=Hash(bytes), BackupPresent=exists,
                BackupValid=valid, Running=running() };
        }
        private PatchState Recognize(byte[] bytes) {
            if (Hash(bytes)==profile.OriginalHash) return PatchState.Original;
            try { if (Hash(Decode(bytes))==profile.PatchedBodyHash) return PatchState.Fixed; }
            catch (InvalidDataException) {} catch (IOException) {} catch (ArgumentException) {}
            return PatchState.Unsupported;
        }
        private static uint Adler(byte[] bytes) {
            uint a=1,b=0;
            foreach (byte v in bytes) { a=(a+v)%65521; b=(b+a)%65521; }
            return (b<<16)|a;
        }
        internal byte[] Decode(byte[] bytes) {
            if (bytes.Length<14 || bytes[0]!='C' || bytes[1]!='W' || bytes[2]!='S'
                || bytes[3]!=profile.SwfVersion || BitConverter.ToUInt32(bytes,4)!=(uint)(profile.BodyLength+8))
                throw new InvalidDataException("Unsupported SWF header.");
            if ((bytes[8]&15)!=8 || (bytes[8]>>4)>7 || ((bytes[8]<<8)+bytes[9])%31!=0 || (bytes[9]&32)!=0)
                throw new InvalidDataException("Unsupported zlib header.");
            byte[] result=new byte[profile.BodyLength];
            using (MemoryStream memory=new MemoryStream(bytes,10,bytes.Length-14,false))
            using (DeflateStream stream=new DeflateStream(memory,CompressionMode.Decompress)) {
                int done=0;
                while (done<result.Length) {
                    int n=stream.Read(result,done,result.Length-done);
                    if (n==0) throw new InvalidDataException("Truncated SWF data.");
                    done+=n;
                }
                if (stream.ReadByte()!=-1) throw new InvalidDataException("Expanded file exceeds expected size.");
            }
            int k=bytes.Length-4;
            uint checksum=((uint)bytes[k]<<24)|((uint)bytes[k+1]<<16)|((uint)bytes[k+2]<<8)|bytes[k+3];
            if (checksum!=Adler(result)) throw new InvalidDataException("SWF checksum mismatch.");
            return result;
        }
        internal static byte[] Encode(byte[] body, byte version) {
            using (MemoryStream output=new MemoryStream()) {
                output.Write(new byte[] {(byte)'C',(byte)'W',(byte)'S',version},0,4);
                byte[] length=BitConverter.GetBytes(body.Length+8); output.Write(length,0,4);
                output.WriteByte(0x78); output.WriteByte(0x9c);
                using (DeflateStream stream=new DeflateStream(output,CompressionLevel.Optimal,true))
                    stream.Write(body,0,body.Length);
                uint checksum=Adler(body);
                output.WriteByte((byte)(checksum>>24)); output.WriteByte((byte)(checksum>>16));
                output.WriteByte((byte)(checksum>>8)); output.WriteByte((byte)checksum);
                return output.ToArray();
            }
        }
        internal byte[] Transform(byte[] bytes) {
            if (Hash(bytes)!=profile.OriginalHash) throw new IOException("Only the verified original game file can be patched.");
            byte[] body=Decode(bytes);
            if (Hash(body)!=profile.OriginalBodyHash) throw new IOException("Original content hash mismatch.");
            for (int i=0;i<profile.Count;i++) body[profile.Offset+i]=2;
            if (Hash(body)!=profile.PatchedBodyHash) throw new IOException("Patched content hash mismatch.");
            byte[] output=Encode(body,profile.SwfVersion);
            if (Hash(Decode(output))!=profile.PatchedBodyHash) throw new IOException("Patched output verification failed.");
            return output;
        }
        private FileStream Lock() {
            SafePath(Support); Directory.CreateDirectory(Support); SafePath(Support);
            string path=Path.Combine(Support,"transaction.lock"); SafePath(path);
            return new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        }
        private static void WriteNew(string path, byte[] bytes) {
            SafePath(path);
            using (FileStream file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                file.Write(bytes,0,bytes.Length); file.Flush(true);
            }
        }
        private void Record(string outputHash) {
            string path=Path.Combine(Support,"installation.xml"); SafePath(path);
            string temporary=Path.Combine(Support,"record-"+Guid.NewGuid().ToString("N")+".tmp");
            try {
                using (XmlWriter writer=XmlWriter.Create(temporary,new XmlWriterSettings { Indent=true, Encoding=new UTF8Encoding(false) })) {
                    writer.WriteStartElement("BannerSagaFPSFix");
                    writer.WriteElementString("Version",BuildInfo.Version);
                    writer.WriteElementString("SourceCommit",BuildInfo.Commit);
                    writer.WriteElementString("OriginalSha256",profile.OriginalHash);
                    writer.WriteElementString("InstalledSha256",outputHash);
                    writer.WriteElementString("PatchedContentSha256",profile.PatchedBodyHash);
                    writer.WriteElementString("RecordedUtc",DateTime.UtcNow.ToString("o"));
                    writer.WriteEndElement();
                }
                if (File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        private void Replace(byte[] output, string expectedCurrentHash) {
            string temporary=Path.Combine(Path.GetDirectoryName(Target),".BannerSagaFPSFix-"+Guid.NewGuid().ToString("N")+".tmp");
            try {
                WriteNew(temporary,output);
                if (Hash(Read(temporary))!=Hash(output)) throw new IOException("Temporary output verification failed.");
                Closed(); SafePath(Target); SafePath(Backup);
                if (Hash(Read(Backup))!=profile.OriginalHash) throw new IOException("Original backup verification failed.");
                if (Hash(Read(Target))!=expectedCurrentHash) throw new IOException("Game file changed during operation; replacement cancelled.");
                // Same-volume atomic replacement; original backup is never consumed.
                File.Replace(temporary,Target,null);
                if (Hash(Read(Target))!=Hash(output)) throw new IOException("Installed file verification failed; verified original backup is preserved.");
            } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal string Install() {
            Closed();
            Inspection initial=Inspect();
            if (initial.State==PatchState.Fixed) return "AlreadyInstalled";
            if (initial.State!=PatchState.Original) throw new IOException("Unsupported version or another mod changed the game file. Nothing was replaced.");
            using (FileStream operation=Lock()) {
                Closed();
                byte[] original=Read(Target);
                byte[] patched=Transform(original);
                if (File.Exists(Backup)) {
                    if (Hash(Read(Backup))!=profile.OriginalHash) throw new IOException("Existing backup is damaged or unrelated. Nothing was replaced.");
                } else {
                    string temporary=Path.Combine(Support,"backup-"+Guid.NewGuid().ToString("N")+".tmp");
                    try {
                        WriteNew(temporary,original);
                        if (Hash(Read(temporary))!=profile.OriginalHash) throw new IOException("Backup verification failed.");
                        File.Move(temporary,Backup);
                    } finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                Record(Hash(patched));
                Replace(patched,profile.OriginalHash);
            }
            return "Installed";
        }
        internal string Restore() {
            Closed();
            Inspection initial=Inspect();
            if (initial.State==PatchState.Original) return "AlreadyOriginal";
            if (initial.State!=PatchState.Fixed) throw new IOException("Game file changed after patching. Restore cancelled to preserve other changes.");
            if (!initial.BackupValid) throw new IOException("Verified original backup is missing or damaged. Restore cancelled.");
            using (FileStream operation=Lock()) {
                Closed();
                byte[] current=Read(Target);
                if (Recognize(current)!=PatchState.Fixed) throw new IOException("Game file changed during restore.");
                byte[] original=Read(Backup);
                if (Hash(original)!=profile.OriginalHash) throw new IOException("Backup verification failed.");
                Replace(original,Hash(current));
            }
            return "Restored";
        }
    }
}
