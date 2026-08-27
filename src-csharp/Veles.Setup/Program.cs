using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace Veles.Setup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new SetupForm(Payload.Read())); }
            catch (Exception error) { MessageBox.Show("Не удалось открыть установщик Veles.\n\n" + error.Message, "Veles Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }

    internal sealed class SetupPayload
    {
        public string ProductId { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string TargetExecutable { get; set; }
        public string InstallDirectory { get; set; }
        public string ShortcutName { get; set; }
        public bool StartAfterInstall { get; set; }
    }

    internal sealed class InstalledRecord
    {
        public string ProductId { get; set; }
        public string Version { get; set; }
        public string InstallDirectory { get; set; }
        public string InstalledAtUtc { get; set; }
    }

    internal static class Payload
    {
        private static readonly byte[] Marker = Encoding.ASCII.GetBytes("VELES_PAYLOAD_V1\0");
        public static SetupPayload Read()
        {
            var executable = typeof(Payload).Assembly.Location; var bytes = File.ReadAllBytes(executable); var marker = LastIndexOf(bytes, Marker);
            if (marker < 0 || marker + Marker.Length + 8 > bytes.Length) throw new InvalidDataException("В установщике отсутствует встроенный payload.");
            var length = BitConverter.ToInt64(bytes, marker + Marker.Length); if (length <= 0 || length > bytes.Length) throw new InvalidDataException("Повреждён встроенный payload.");
            using (var stream = new MemoryStream(bytes, marker + Marker.Length + 8, (int)length, false)) using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                var entry = archive.GetEntry("setup.json"); if (entry == null) throw new InvalidDataException("В payload отсутствует setup.json.");
                using (var reader = new StreamReader(entry.Open(), Encoding.UTF8)) return new JavaScriptSerializer().Deserialize<SetupPayload>(reader.ReadToEnd());
            }
        }
        public static void ExtractTo(string executable, string destination)
        {
            var bytes = File.ReadAllBytes(executable); var marker = LastIndexOf(bytes, Marker); var length = BitConverter.ToInt64(bytes, marker + Marker.Length);
            using (var stream = new MemoryStream(bytes, marker + Marker.Length + 8, (int)length, false)) using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, false, Encoding.UTF8))
            {
                foreach (var entry in archive.Entries)
                {
                    var relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar); var full = Path.GetFullPath(Path.Combine(destination, relative)); var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Payload содержит недопустимый путь.");
                    if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(full); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(full)); using (var source = entry.Open()) using (var target = File.Create(full)) source.CopyTo(target);
                }
            }
        }
        private static int LastIndexOf(byte[] source, byte[] value)
        {
            for (var i = source.Length - value.Length; i >= 0; i--) { var match = true; for (var j = 0; j < value.Length; j++) if (source[i + j] != value[j]) { match = false; break; } if (match) return i; } return -1;
        }
    }

    internal sealed class SetupForm : Form
    {
        private readonly SetupPayload _payload; private readonly string _recordPath; private readonly TextBox _path; private readonly Label _headline; private readonly Label _version; private readonly Label _status; private readonly ProgressBar _progress; private readonly Button _install; private readonly Color Orange = Color.FromArgb(249, 115, 22); private readonly Color Background = Color.FromArgb(14, 16, 20); private readonly Color Card = Color.FromArgb(29, 32, 38); private readonly Color Muted = Color.FromArgb(170, 176, 185);
        public SetupForm(SetupPayload payload)
        {
            _payload = payload ?? throw new ArgumentNullException("payload"); _recordPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veles", "installations", payload.ProductId + ".json");
            Text = payload.DisplayName + " Setup"; Width = 760; Height = 470; MinimumSize = new Size(760, 470); MaximumSize = new Size(760, 470); StartPosition = FormStartPosition.CenterScreen; BackColor = Background; ForeColor = Color.White; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; BuildUi();
        }
        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = Background, Padding = new Padding(30), ColumnCount = 1, RowCount = 6 }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); Controls.Add(root);
            var brand = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Background }; brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56)); brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); var mark = new Label { Text = "V", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 34, FontStyle.Bold), ForeColor = Orange, TextAlign = ContentAlignment.MiddleLeft }; brand.Controls.Add(mark, 0, 0); brand.Controls.Add(new Label { Text = "VELES PLAYGAME\nSETUP", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 15, FontStyle.Bold), ForeColor = Color.White, TextAlign = ContentAlignment.MiddleLeft }, 1, 0); root.Controls.Add(brand, 0, 0);
            _headline = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; root.Controls.Add(_headline, 0, 1);
            _version = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Regular), ForeColor = Orange, TextAlign = ContentAlignment.MiddleLeft }; root.Controls.Add(_version, 0, 2);
            var folder = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16, 10, 16, 10) }; folder.Controls.Add(new Label { Text = "Папка установки", AutoSize = true, ForeColor = Muted, Font = new Font("Segoe UI", 9), Location = new Point(16, 7) }); _path = new TextBox { Text = DefaultDirectory(), Location = new Point(16, 32), Width = 570, Height = 26, BackColor = Color.FromArgb(38, 42, 49), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; folder.Controls.Add(_path); var browse = new Button { Text = "Выбрать", Location = new Point(600, 30), Width = 92, Height = 28, BackColor = Color.FromArgb(51, 43, 37), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; browse.Click += (s, e) => { using (var dialog = new FolderBrowserDialog { SelectedPath = _path.Text }) if (dialog.ShowDialog(this) == DialogResult.OK) _path.Text = dialog.SelectedPath; }; folder.Controls.Add(browse); root.Controls.Add(folder, 0, 3);
            _status = new Label { Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 9), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; _progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 5, Visible = false }; var info = new Panel { Dock = DockStyle.Fill, BackColor = Background }; info.Controls.Add(_status); info.Controls.Add(_progress); root.Controls.Add(info, 0, 4);
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Background }; footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); _install = new Button { Text = "Установить", Dock = DockStyle.Fill, BackColor = Orange, ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) }; _install.Click += async (s, e) => await InstallAsync(); footer.Controls.Add(new Label { Text = "Veles Setup · безопасное обновление поверх установленной версии", Dock = DockStyle.Fill, ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft }, 0, 0); footer.Controls.Add(_install, 1, 0); root.Controls.Add(footer, 0, 5); UpdateState();
        }
        private string DefaultDirectory()
        {
            var installed = ReadRecord(); if (installed != null && !string.IsNullOrWhiteSpace(installed.InstallDirectory)) return installed.InstallDirectory;
            var baseName = string.Equals(_payload.ProductId, "publisher", StringComparison.OrdinalIgnoreCase) ? "Veles Build Publisher" : "Veles Launcher"; return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", baseName);
        }
        private void UpdateState()
        {
            var installed = ReadRecord(); var current = installed == null ? null : installed.Version; var updating = installed != null && Directory.Exists(installed.InstallDirectory); _headline.Text = updating ? "Обновление " + _payload.DisplayName : "Установка " + _payload.DisplayName; _version.Text = (current == null ? "Новая установка" : "Установлено v" + current) + "  →  пакет v" + _payload.Version; _status.Text = updating ? "Существующая версия будет аккуратно заменена после закрытия приложения." : "Проверьте папку и нажмите «Установить»."; _install.Text = updating ? "Обновить" : "Установить";
        }
        private async Task InstallAsync()
        {
            try
            {
                _install.Enabled = false; _progress.Visible = true; _progress.Style = ProgressBarStyle.Marquee; _status.Text = "Проверяю запущенные приложения…"; await Task.Delay(200);
                var processName = string.Equals(_payload.ProductId, "publisher", StringComparison.OrdinalIgnoreCase) ? "VelesBuildPublisher" : "VelesLauncher"; if (Process.GetProcessesByName(processName).Length > 0) { _status.Text = "Закройте " + _payload.DisplayName + " и нажмите кнопку ещё раз."; _install.Enabled = true; return; }
                var install = Path.GetFullPath(_path.Text.Trim()); if (string.IsNullOrWhiteSpace(install)) throw new InvalidDataException("Папка установки не указана."); var stage = install.TrimEnd(Path.DirectorySeparatorChar) + ".stage-" + Guid.NewGuid().ToString("N"); var backup = install.TrimEnd(Path.DirectorySeparatorChar) + ".backup-" + Guid.NewGuid().ToString("N"); Directory.CreateDirectory(stage); _status.Text = "Распаковываю пакет…"; Payload.ExtractTo(typeof(Payload).Assembly.Location, stage);
                var old = Directory.Exists(install); if (old) Directory.Move(install, backup); try { Directory.CreateDirectory(Path.GetDirectoryName(install)); Directory.Move(stage, install); } catch { if (Directory.Exists(install)) Directory.Delete(install, true); if (old && Directory.Exists(backup)) Directory.Move(backup, install); throw; }
                if (Directory.Exists(backup)) Directory.Delete(backup, true); SaveRecord(new InstalledRecord { ProductId = _payload.ProductId, Version = _payload.Version, InstallDirectory = install, InstalledAtUtc = DateTime.UtcNow.ToString("o") }); CreateShortcut(install); _status.Text = "Готово. Версия " + _payload.Version + " установлена."; _progress.Visible = false; _install.Text = "Готово"; if (_payload.StartAfterInstall && File.Exists(Path.Combine(install, _payload.TargetExecutable))) Process.Start(new ProcessStartInfo(Path.Combine(install, _payload.TargetExecutable)) { WorkingDirectory = install, UseShellExecute = true });
            }
            catch (Exception error) { _progress.Visible = false; _status.Text = "Установка не выполнена: " + error.Message; _install.Enabled = true; }
        }
        private InstalledRecord ReadRecord() { try { if (!File.Exists(_recordPath)) return null; return new JavaScriptSerializer().Deserialize<InstalledRecord>(File.ReadAllText(_recordPath, Encoding.UTF8)); } catch { return null; } }
        private void SaveRecord(InstalledRecord record) { Directory.CreateDirectory(Path.GetDirectoryName(_recordPath)); File.WriteAllText(_recordPath, new JavaScriptSerializer().Serialize(record), Encoding.UTF8); }
        private void CreateShortcut(string install)
        {
            try { var shellType = Type.GetTypeFromProgID("WScript.Shell"); if (shellType == null) return; dynamic shell = Activator.CreateInstance(shellType); var link = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), _payload.ShortcutName + ".lnk"); dynamic shortcut = shell.CreateShortcut(link); shortcut.TargetPath = Path.Combine(install, _payload.TargetExecutable); shortcut.WorkingDirectory = install; shortcut.IconLocation = Path.Combine(install, _payload.TargetExecutable) + ",0"; shortcut.Save(); } catch { }
        }
    }
}
