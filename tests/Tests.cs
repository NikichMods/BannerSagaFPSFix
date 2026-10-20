// SPDX-License-Identifier: MPL-2.0
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Threading;
using BannerSagaFPSFix;

internal static class Tests {
    private static string sandbox;
    private static int count;
    private sealed class Fixture : IDisposable {
        internal string Root;
        internal Patcher Patcher;
        internal PatchProfile Profile;
        internal byte[] Original;
        internal Fixture() {
            Root=Path.Combine(sandbox,Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(Root,"win32"));
            byte[] body=new byte[512]; new Random(17).NextBytes(body);
            byte[] changed=(byte[])body.Clone(); for (int i=150;i<160;i++) changed[i]=2;
            Original=BannerSagaFPSFix.Patcher.Encode(body,36);
            Profile=new PatchProfile { OriginalHash=BannerSagaFPSFix.Patcher.Hash(Original), OriginalBodyHash=BannerSagaFPSFix.Patcher.Hash(body),
                PatchedBodyHash=BannerSagaFPSFix.Patcher.Hash(changed),BodyLength=body.Length,Offset=150,Count=10,SwfVersion=36 };
            Patcher=new Patcher(Root,Profile,delegate { return false; });
            File.WriteAllBytes(Patcher.Target,Original);
            Directory.CreateDirectory(Path.Combine(Root,"save"));
            File.WriteAllText(Path.Combine(Root,"save","sentinel"),"unchanged");
        }
        public void Dispose() {
            if (!Path.GetFullPath(Root).StartsWith(Path.GetFullPath(sandbox)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new Exception("Unsafe test cleanup path");
            if (Directory.Exists(Root)) Directory.Delete(Root,true);
        }
    }
    private static void Assert(bool condition,string message) { if (!condition) throw new Exception(message); }
    private static void Fails(Action action) {
        try { action(); } catch (IOException) { return; } catch (UnauthorizedAccessException) { return; }
        throw new Exception("Expected safe refusal");
    }
    private static void Test(string name,Action<Fixture> action) {
        using (Fixture f=new Fixture()) { action(f); Assert(File.ReadAllText(Path.Combine(f.Root,"save","sentinel"))=="unchanged","Save changed"); }
        count++; Console.WriteLine("PASS "+name);
    }
    [STAThread] private static int Main(string[] args) {
        try {
            sandbox=Path.GetFullPath(args[0]); Directory.CreateDirectory(sandbox);
            Test("status is read only",f=> { Assert(f.Patcher.Inspect().State==PatchState.Original,"state"); Assert(!Directory.Exists(f.Patcher.Support),"status wrote files"); });
            Test("install and exact restore",f=> { Assert(f.Patcher.Install()=="Installed","install"); Assert(f.Patcher.Inspect().State==PatchState.Fixed,"patched state"); Assert(Patcher.Hash(File.ReadAllBytes(f.Patcher.Backup))==f.Profile.OriginalHash,"backup"); f.Patcher.Restore(); Assert(File.ReadAllBytes(f.Patcher.Target).SequenceEqual(f.Original),"restore differs"); });
            Test("install and restore idempotency",f=> { f.Patcher.Install(); string hash=f.Patcher.Inspect().FileHash; Assert(f.Patcher.Install()=="AlreadyInstalled","second install"); Assert(hash==f.Patcher.Inspect().FileHash,"second install changed file"); f.Patcher.Restore(); Assert(f.Patcher.Restore()=="AlreadyOriginal","second restore"); f.Patcher.Install(); Assert(f.Patcher.Inspect().State==PatchState.Fixed,"reinstall"); });
            Test("unknown original refuses without backup writes",f=> { byte[] bytes=(byte[])f.Original.Clone(); bytes[3]=12; File.WriteAllBytes(f.Patcher.Target,bytes); Fails(()=>f.Patcher.Install()); Assert(!Directory.Exists(f.Patcher.Support),"unknown wrote files"); Assert(File.ReadAllBytes(f.Patcher.Target).SequenceEqual(bytes),"unknown replaced"); });
            Test("running game blocks both operations",f=> { Patcher p=new Patcher(f.Root,f.Profile,delegate {return true;}); Fails(()=>p.Install()); Assert(!Directory.Exists(p.Support),"running wrote files"); f.Patcher.Install(); Fails(()=>p.Restore()); Assert(f.Patcher.Inspect().State==PatchState.Fixed,"running restored"); });
            Test("damaged existing backup blocks install",f=> { Directory.CreateDirectory(f.Patcher.Support); File.WriteAllBytes(f.Patcher.Backup,new byte[16]); Fails(()=>f.Patcher.Install()); Assert(f.Patcher.Inspect().State==PatchState.Original,"target changed"); });
            Test("damaged backup blocks restore",f=> { f.Patcher.Install(); File.WriteAllBytes(f.Patcher.Backup,new byte[16]); string hash=f.Patcher.Inspect().FileHash; Fails(()=>f.Patcher.Restore()); Assert(hash==f.Patcher.Inspect().FileHash,"target changed"); });
            Test("missing backup blocks restore",f=> { f.Patcher.Install(); File.Delete(f.Patcher.Backup); Fails(()=>f.Patcher.Restore()); Assert(f.Patcher.Inspect().State==PatchState.Fixed,"target changed"); });
            Test("later foreign changes preserved",f=> { f.Patcher.Install(); byte[] bytes=File.ReadAllBytes(f.Patcher.Target); bytes[3]=12; File.WriteAllBytes(f.Patcher.Target,bytes); Fails(()=>f.Patcher.Restore()); Assert(File.ReadAllBytes(f.Patcher.Target).SequenceEqual(bytes),"foreign changes lost"); });
            Test("unmanaged existing fix recognized",f=> { File.WriteAllBytes(f.Patcher.Target,f.Patcher.Transform(f.Original)); Inspection s=f.Patcher.Inspect(); Assert(s.State==PatchState.Fixed && !s.BackupPresent,"unmanaged recognition"); Assert(f.Patcher.Install()=="AlreadyInstalled","unmanaged installed twice"); Fails(()=>f.Patcher.Restore()); Assert(!Directory.Exists(f.Patcher.Support),"invented backup"); });
            Test("concurrent transaction refused",f=> { Directory.CreateDirectory(f.Patcher.Support); using (FileStream hold=new FileStream(Path.Combine(f.Patcher.Support,"transaction.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) Fails(()=>f.Patcher.Install()); Assert(f.Patcher.Inspect().State==PatchState.Original,"concurrent changed file"); });
            Test("locked target leaves original and verified backup",f=> { using (FileStream hold=new FileStream(f.Patcher.Target,FileMode.Open,FileAccess.Read,FileShare.Read)) Fails(()=>f.Patcher.Install()); Assert(f.Patcher.Inspect().State==PatchState.Original,"locked file changed"); Assert(f.Patcher.Inspect().BackupValid,"backup lost"); Assert(Directory.GetFiles(Path.GetDirectoryName(f.Patcher.Target),"*.tmp").Length==0,"temp leaked"); });
            Test("interrupted precommit state can recover",f=> { Directory.CreateDirectory(f.Patcher.Support); File.WriteAllBytes(f.Patcher.Backup,f.Original); File.WriteAllText(Path.Combine(f.Patcher.Support,"installation.xml"),"incomplete metadata"); f.Patcher.Install(); f.Patcher.Restore(); Assert(f.Patcher.Inspect().State==PatchState.Original,"recovery"); });
            Test("metadata cannot redirect restore",f=> { f.Patcher.Install(); File.WriteAllText(Path.Combine(f.Patcher.Support,"installation.xml"),"<!DOCTYPE bad [<!ENTITY x SYSTEM 'file:///bad'>]><bad>&x;</bad>"); f.Patcher.Restore(); Assert(f.Patcher.Inspect().State==PatchState.Original,"metadata used as authority"); });
            Test("checksum corruption refused",f=> { byte[] bytes=f.Patcher.Transform(f.Original); bytes[bytes.Length-1]^=1; File.WriteAllBytes(f.Patcher.Target,bytes); Fails(()=>f.Patcher.Restore()); Assert(f.Patcher.Inspect().State==PatchState.Unsupported,"bad checksum accepted"); });
            Test("wrong patch profile cannot produce output",f=> { f.Profile.PatchedBodyHash=new string('0',64); Fails(()=>f.Patcher.Install()); Assert(!File.Exists(f.Patcher.Backup),"backup created before transform verified"); Assert(File.ReadAllBytes(f.Patcher.Target).SequenceEqual(f.Original),"target changed"); });
            Test("reparse path rejected",f=> {
                string link=Path.Combine(f.Root,"linked-root");
                ProcessStartInfo info=new ProcessStartInfo("cmd.exe","/d /c mklink /J \""+link+"\" \""+Path.Combine(f.Root,"win32")+"\"") {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                using (Process child=Process.Start(info)) { child.StandardOutput.ReadToEnd(); child.StandardError.ReadToEnd(); child.WaitForExit(); Assert(child.ExitCode==0,"Could not create test junction"); }
                try { Fails(()=>Patcher.SafePath(link)); }
                finally { Directory.Delete(link); }
            });
            if (args.Length>=3) Host(args[1],args[2]);
            Console.WriteLine("PASS total="+count);
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Host(string originalPath,string legacyPath) {
        string root=Path.Combine(sandbox,"host-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root,"win32"));
        Patcher p=new Patcher(root,PatchProfile.Production(),delegate {return false;});
        File.Copy(originalPath,p.Target);
        byte[] accepted=p.Decode(File.ReadAllBytes(legacyPath));
        Assert(p.Inspect().State==PatchState.Original,"host original");
        p.Install();
        Assert(p.Decode(File.ReadAllBytes(p.Target)).SequenceEqual(accepted),"Native output differs from accepted test.8");
        string patchedHash=p.Inspect().FileHash;
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using (MainForm form=new MainForm()) form.Preview(root,Path.Combine(sandbox,"ui-installed-ru.png"),true);
        using (MainForm form=new MainForm()) form.Preview(root,Path.Combine(sandbox,"ui-installed-en.png"),false);
        p.Restore();
        Assert(Patcher.Hash(File.ReadAllBytes(p.Target))==PatchProfile.Production().OriginalHash,"host restore mismatch");
        using (MainForm form=new MainForm()) form.Preview(root,Path.Combine(sandbox,"ui-original-ru.png"),true);
        using (MainForm form=new MainForm()) {
            FieldInfo preview=typeof(MainForm).GetField("previewMode",BindingFlags.NonPublic|BindingFlags.Instance);
            preview.SetValue(form,true); form.ShowInTaskbar=false; form.StartPosition=FormStartPosition.Manual;
            form.Location=new System.Drawing.Point(-20000,-20000);
            Control control=(Control)typeof(MainForm).GetField("folder",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            control.Text=root; form.Show();
            Button check=(Button)typeof(MainForm).GetField("inspect",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            Button install=(Button)typeof(MainForm).GetField("install",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            Button restore=(Button)typeof(MainForm).GetField("restore",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(form);
            check.PerformClick(); WaitUi(form);
            Assert(install.Enabled && !restore.Enabled,"GUI original actions");
            install.PerformClick(); WaitUi(form);
            Assert(!install.Enabled && restore.Enabled && p.Inspect().State==PatchState.Fixed,"GUI install action");
            restore.PerformClick(); WaitUi(form);
            Assert(install.Enabled && !restore.Enabled && p.Inspect().State==PatchState.Original,"GUI restore action");
            Assert(Patcher.Hash(File.ReadAllBytes(p.Target))==PatchProfile.Production().OriginalHash,"GUI exact restore");
            form.Close();
        }
        count++; Console.WriteLine("PASS real GUI check/install/restore buttons on isolated host fixture");
        File.WriteAllText(Path.Combine(sandbox,"host-verification.txt"),"original="+PatchProfile.Production().OriginalHash+Environment.NewLine+"patched="+patchedHash+Environment.NewLine+"content="+Patcher.Hash(accepted)+Environment.NewLine+"restored_exact=true\nnormalized_payload_equals_accepted_test8=true\n");
        count++; Console.WriteLine("PASS isolated original -> native patch (test.8 equivalent) -> exact original: "+patchedHash);
        // Retain the isolated fixture for review; it stays under ignored local/.
    }
    private static void WaitUi(MainForm form) {
        FieldInfo busy=typeof(MainForm).GetField("busy",BindingFlags.NonPublic|BindingFlags.Instance);
        DateTime deadline=DateTime.UtcNow.AddSeconds(30);
        while ((bool)busy.GetValue(form)) {
            if (DateTime.UtcNow>deadline) throw new Exception("GUI operation timeout");
            Application.DoEvents(); Thread.Sleep(10);
        }
        Application.DoEvents();
    }
}
