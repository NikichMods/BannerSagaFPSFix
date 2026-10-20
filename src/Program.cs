// SPDX-License-Identifier: MPL-2.0
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace BannerSagaFPSFix {
    internal static class Program {
        [STAThread] private static int Main(string[] args) {
            if (args.Length>0) {
                try {
                    if (args.Length==1 && args[0]=="--version") { Console.WriteLine(BuildInfo.Version+" "+BuildInfo.Commit); return 0; }
                    if (args.Length!=2) throw new IOException("Usage: BannerSagaFPSFix.exe --status|--install|--restore GAME_FOLDER");
                    Patcher patcher=new Patcher(args[1]);
                    if (args[0]=="--status") {
                        Inspection s=patcher.Inspect();
                        Console.WriteLine("State="+s.State+" BackupValid="+s.BackupValid+" Running="+s.Running+" SHA256="+s.FileHash);
                    } else if (args[0]=="--install") Console.WriteLine(patcher.Install());
                    else if (args[0]=="--restore") Console.WriteLine(patcher.Restore());
                    else throw new IOException("Unknown command.");
                    return 0;
                } catch (Exception e) { Console.Error.WriteLine(e.Message); return 2; }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }
    internal sealed class MainForm : Form {
        private readonly TextBox folder=new TextBox();
        private readonly Button browse=new Button(), inspect=new Button(), install=new Button(), restore=new Button();
        private readonly Label title=new Label(), intro=new Label(), pathLabel=new Label(), status=new Label(), detail=new Label(), foot=new Label();
        private readonly ComboBox language=new ComboBox();
        private readonly TableLayoutPanel layout=new TableLayoutPanel();
        private bool busy, inspected, previewMode;
        private Inspection last;
        private bool Russian { get { return language.SelectedIndex==0; } }
        private string T(string ru,string en) { return Russian?ru:en; }
        internal MainForm() {
            Text="BannerSagaFPSFix";
            Font=new Font("Segoe UI",10F);
            ClientSize=new Size(740,420); MinimumSize=new Size(720,450);
            StartPosition=FormStartPosition.CenterScreen;
            BackColor=SystemColors.Window;
            layout.Dock=DockStyle.Fill; layout.Padding=new Padding(24); layout.ColumnCount=1; layout.RowCount=9;
            for (int i=0;i<8;i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
            Controls.Add(layout);
            FlowLayoutPanel top=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true,WrapContents=false };
            title.Font=new Font(Font.FontFamily,18F,FontStyle.Bold); title.AutoSize=true; title.Margin=new Padding(0,0,30,12);
            language.DropDownStyle=ComboBoxStyle.DropDownList; language.Width=112;
            language.Items.AddRange(new object[] {"Русский","English"});
            language.SelectedIndex=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="ru"?0:1;
            top.Controls.Add(title); top.Controls.Add(language); layout.Controls.Add(top);
            intro.AutoSize=true; intro.MaximumSize=new Size(670,0); intro.Margin=new Padding(0,0,0,20); layout.Controls.Add(intro);
            pathLabel.AutoSize=true; pathLabel.Margin=new Padding(0,0,0,6); layout.Controls.Add(pathLabel);
            TableLayoutPanel row=new TableLayoutPanel { Dock=DockStyle.Fill,AutoSize=false,Height=34,ColumnCount=2,RowCount=1,Margin=new Padding(0,0,0,12) };
            row.RowStyles.Add(new RowStyle(SizeType.Absolute,34));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,110));
            folder.Dock=DockStyle.Fill; folder.Margin=new Padding(0,0,10,0);
            browse.Dock=DockStyle.Fill; browse.AutoSize=false; row.Controls.Add(folder,0,0); row.Controls.Add(browse,1,0); layout.Controls.Add(row);
            status.Font=new Font(Font,FontStyle.Bold); status.AutoSize=true; status.Margin=new Padding(0,8,0,6); layout.Controls.Add(status);
            detail.AutoSize=true; detail.MaximumSize=new Size(670,0); detail.Margin=new Padding(0,0,0,18); layout.Controls.Add(detail);
            FlowLayoutPanel actions=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,18) };
            foreach (Button button in new Button[] {inspect,install,restore}) { button.AutoSize=true; button.Padding=new Padding(10,5,10,5); button.Margin=new Padding(0,0,10,5); actions.Controls.Add(button); }
            layout.Controls.Add(actions);
            foot.AutoSize=true; foot.MaximumSize=new Size(670,0); foot.ForeColor=SystemColors.GrayText; layout.Controls.Add(foot);
            browse.Click+=delegate {
                using (FolderBrowserDialog dialog=new FolderBrowserDialog()) {
                    dialog.Description=T("Выберите папку The Banner Saga, содержащую win32.","Select The Banner Saga folder containing win32.");
                    dialog.SelectedPath=folder.Text;
                    if (dialog.ShowDialog(this)==DialogResult.OK) { folder.Text=dialog.SelectedPath; Check(); }
                }
            };
            folder.TextChanged+=delegate { inspected=false; last=null; RefreshText(); };
            language.SelectedIndexChanged+=delegate { RefreshText(); };
            inspect.Click+=delegate { Check(); };
            install.Click+=delegate { Change(false); };
            restore.Click+=delegate { Change(true); };
            Shown+=delegate { if (!previewMode && folder.Text.Length>0) Check(); };
            folder.Text=DetectGame(); RefreshText();
        }
        private static string DetectGame() {
            foreach (RegistryView view in new RegistryView[] {RegistryView.Registry32,RegistryView.Registry64}) {
                try {
                    using (RegistryKey machine=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view))
                    using (RegistryKey key=machine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 237990")) {
                        string path=key==null?null:key.GetValue("InstallLocation") as string;
                        if (path!=null && File.Exists(Path.Combine(path,Patcher.RelativeTarget))) return path;
                    }
                } catch (System.Security.SecurityException) {} catch (UnauthorizedAccessException) {}
            }
            return "";
        }
        private void RefreshText() {
            title.Text="Banner Saga FPS Fix";
            intro.Text=T("Убирает просадку FPS в карточке героя, останавливая анимацию портрета. Для The Banner Saga 1, Steam Windows 2.58.17.",
                "Fixes hero-screen FPS drops by stopping portrait animation. For The Banner Saga 1, Steam Windows 2.58.17.");
            pathLabel.Text=T("Папка игры (внутри должна быть папка win32)","Game folder (must contain the win32 folder)");
            browse.Text=T("Обзор…","Browse…"); inspect.Text=T("Проверить","Check");
            install.Text=T("Установить фикс","Install fix"); restore.Text=T("Восстановить оригинал","Restore original");
            foot.Text=T("Сохранения и локализация не меняются. Оригинальный файл сохраняется в папке .BannerSagaFPSFix внутри игры. Версия ",
                "Saves and localization are unchanged. The original is backed up in .BannerSagaFPSFix inside the game folder. Version ")+BuildInfo.Version;
            install.Enabled=!busy && last!=null && last.State==PatchState.Original && !last.Running && (!last.BackupPresent || last.BackupValid);
            restore.Enabled=!busy && last!=null && last.State==PatchState.Fixed && last.BackupValid && !last.Running;
            browse.Enabled=inspect.Enabled=folder.Enabled=language.Enabled=!busy;
            if (busy) { status.Text=T("Выполняется операция…","Working…"); detail.Text=T("Пожалуйста, не запускайте игру до завершения.","Please keep the game closed until the operation finishes."); return; }
            if (!inspected || last==null) { status.Text=T("Выберите папку и нажмите «Проверить».","Choose the game folder and select Check."); detail.Text=""; return; }
            if (last.Running) { status.Text=T("Игра запущена","The game is running"); detail.Text=T("Закройте The Banner Saga, затем нажмите «Проверить».","Close The Banner Saga, then select Check."); return; }
            if (last.BackupPresent && !last.BackupValid) { status.Text=T("Резервная копия не прошла проверку","Backup verification failed"); detail.Text=T("Установка и откат заблокированы. Существующие файлы сохранены.","Install and restore are blocked. Existing files have been preserved."); return; }
            if (last.State==PatchState.Original) { status.Text=T("Поддерживаемый оригинал — фикс не установлен","Supported original — fix is not installed"); detail.Text=T("Можно установить фикс. Перед заменой будет проверена резервная копия.","Ready to install. A verified original backup will be saved before replacement."); }
            else if (last.State==PatchState.Fixed) { status.Text=T("FPS-фикс установлен","FPS fix is installed"); detail.Text=last.BackupValid?
                T("Портреты в карточке героя статичны. Оригинал можно восстановить кнопкой ниже.","Hero-card portraits are static. You can restore the original using the button below."):
                T("Фикс уже есть, но этот патчер не располагает проверенной резервной копией. Для отката используйте прежний способ установки.","The fix is present, but this patcher has no verified original backup. Restore it using the original installation method."); }
            else { status.Text=T("Неподдерживаемая версия или изменённый файл","Unsupported version or modified file"); detail.Text=T("Файл не будет заменён. Другие моды и версии пока не поддерживаются.","Nothing will be replaced. Other mods and versions are not currently supported."); }
        }
        private void Background(Action work, Action success) {
            busy=true; RefreshText();
            Task.Factory.StartNew(work).ContinueWith(delegate(Task task) {
                if (IsDisposed) return;
                BeginInvoke((Action)delegate {
                    busy=false;
                    if (task.IsFaulted) {
                        inspected=false; last=null; RefreshText();
                        Exception error=task.Exception.GetBaseException();
                        MessageBox.Show(this,T("Операция не выполнена.\n\n","Operation was not completed.\n\n")+error.Message,
                            "BannerSagaFPSFix",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    } else { success(); RefreshText(); }
                });
            });
        }
        private void Check() {
            string selected=folder.Text; Inspection found=null;
            Background(delegate { found=new Patcher(selected).Inspect(); },delegate { last=found; inspected=true; });
        }
        private void Change(bool undo) {
            string selected=folder.Text; Inspection found=null;
            Background(delegate { Patcher p=new Patcher(selected); if (undo) p.Restore(); else p.Install(); found=p.Inspect(); },
                delegate { last=found; inspected=true; });
        }
        internal void Preview(string root, string destination, bool russian) {
            previewMode=true; ShowInTaskbar=false; StartPosition=FormStartPosition.Manual;
            Location=new Point(-20000,-20000);
            language.SelectedIndex=russian?0:1;
            folder.Text=root; last=new Patcher(root).Inspect(); inspected=true; RefreshText();
            Show(); PerformLayout(); Application.DoEvents();
            using (Bitmap bitmap=new Bitmap(Width,Height)) { DrawToBitmap(bitmap,new Rectangle(0,0,Width,Height)); bitmap.Save(destination); }
            Close();
        }
    }
}
