using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Veles.Core;

namespace Veles.Launcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }

    internal sealed class LauncherForm : Form
    {
        private readonly BuildService _builds = new BuildService();
        private BuildSnapshot _latest;
        private Label _version, _address, _profile, _meta, _loader, _minecraft, _status, _notice;
        private Button _update, _launch;
        private ProgressBar _progress;
        private readonly Color Background = Color.FromArgb(16, 11, 8);
        private readonly Color Card = Color.FromArgb(27, 21, 18);
        private readonly Color Orange = Color.FromArgb(255, 122, 25);
        private readonly Color Text = Color.FromArgb(245, 238, 232);
        private readonly Color Muted = Color.FromArgb(163, 149, 141);

        public LauncherForm()
        {
            Text = "Veles Launcher"; Width = 1040; Height = 680; MinimumSize = new Size(900, 600); BackColor = Background; ForeColor = Text; StartPosition = FormStartPosition.CenterScreen;
            BuildUi(); Shown += async (s, e) => await RefreshAsync();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(30, 24, 30, 20), RowCount = 5, ColumnCount = 2, BackColor = Background };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 155)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); Controls.Add(root);
            var brand = new Label { Text = "V  VELES LAUNCHER\n     TerraFirmaGreg: Modern", AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Text, Dock = DockStyle.Fill };
            var site = Button("Сайт сервера", Color.FromArgb(33, 25, 19), Text); site.Anchor = AnchorStyles.Top | AnchorStyles.Right; site.Click += (s, e) => OpenUrl("https://veles-saite.vercel.app/");
            root.Controls.Add(brand, 0, 0); root.Controls.Add(site, 1, 0);
            var hero = new Panel { Dock = DockStyle.Fill }; root.Controls.Add(hero, 0, 1); root.SetColumnSpan(hero, 2);
            var title = new Label { Text = "TerraFirmaGreg: Modern", AutoSize = true, Location = new Point(0, 20), Font = new Font("Segoe UI", 28, FontStyle.Bold), ForeColor = Orange };
            var subtitle = new Label { Text = "Одна сборка · один сервер · все параметры из GitHub Release", AutoSize = true, Location = new Point(3, 70), Font = new Font("Segoe UI", 10), ForeColor = Muted }; _version = new Label { Text = "v—", AutoSize = true, Location = new Point(720, 38), Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Orange };
            hero.Controls.Add(title); hero.Controls.Add(subtitle); hero.Controls.Add(_version);
            root.Controls.Add(MakeServerCard(), 0, 2); root.Controls.Add(MakeBuildCard(), 1, 2);
            var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(12), BackColor = Card }; details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            _loader = Detail(details, "МОДЛОАДЕР", 0); _minecraft = Detail(details, "ВЕРСИЯ MINECRAFT", 1); _status = Detail(details, "СТАТУС", 2); root.Controls.Add(details, 0, 3); root.SetColumnSpan(details, 2);
            var footer = new Label { Text = "Veles PlayGame · Публичные релизы сборок GitHub", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 8) }; root.Controls.Add(footer, 0, 4); root.SetColumnSpan(footer, 2);
        }

        private Control MakeServerCard()
        {
            var panel = CardPanel(); panel.Padding = new Padding(22); panel.Controls.Add(Label("◈  ПОДКЛЮЧЕНИЕ", 0, 0, 17, Text, true)); panel.Controls.Add(Label("IP АДРЕС СЕРВЕРА", 0, 48, 9, Muted, true)); _address = Label("—", 0, 68, 19, Text, true); panel.Controls.Add(_address);
            var copy = Button("Копировать", Color.FromArgb(43, 28, 20), Text); copy.Location = new Point(240, 64); copy.Width = 110; copy.Click += (s, e) => { if (_address.Text != "—") { Clipboard.SetText(_address.Text); ShowNotice("IP сервера скопирован.", false); } }; panel.Controls.Add(copy);
            panel.Controls.Add(Label("Адрес читается из build-info.txt текущего релиза.", 0, 118, 9, Muted, false)); return panel;
        }

        private Control MakeBuildCard()
        {
            var panel = CardPanel(); panel.Padding = new Padding(22); panel.Controls.Add(Label("⇩  ЗАПУСК ИГРЫ", 0, 0, 17, Text, true)); _profile = Label("—", 0, 45, 13, Text, true); _meta = Label("—", 0, 70, 9, Muted, false); panel.Controls.Add(_profile); panel.Controls.Add(_meta);
            _update = Button("Установить / обновить сборку", Orange, Color.FromArgb(23, 13, 6)); _update.Location = new Point(22, 100); _update.Width = 300; _update.Click += async (s, e) => await InstallAsync(); panel.Controls.Add(_update);
            _launch = Button("Запустить Minecraft", Color.FromArgb(43, 28, 20), Text); _launch.Location = new Point(22, 140); _launch.Width = 300; _launch.Enabled = false; _launch.Click += async (s, e) => await LaunchAsync(); panel.Controls.Add(_launch);
            _progress = new ProgressBar { Location = new Point(22, 178), Width = 300, Height = 5, Style = ProgressBarStyle.Continuous, Maximum = 100, Visible = false }; panel.Controls.Add(_progress); return panel;
        }

        private Label Detail(TableLayoutPanel panel, string caption, int column) { var label = new Label { Text = caption + "\n—", Dock = DockStyle.Fill, Padding = new Padding(5), ForeColor = Text, Font = new Font("Segoe UI", 10, FontStyle.Bold) }; panel.Controls.Add(label, column, 0); return label; }
        private Panel CardPanel() { return new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(6), BorderStyle = BorderStyle.FixedSingle }; }
        private Label Label(string text, int x, int y, float size, Color color, bool bold) { return new Label { Text = text, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color }; }
        private Button Button(string text, Color back, Color fore) { return new Button { Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, FlatAppearance = { BorderColor = Color.FromArgb(90, 55, 34) }, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; }
        private void SetBusy(bool value) { _update.Enabled = !value; _launch.Enabled = !value && _latest != null && !_builds.NeedsUpdate(_latest); _progress.Visible = value; if (value) _progress.Value = 0; }
        private async Task RefreshAsync()
        {
            try
            {
                _status.Text = "СТАТУС\nПроверка…"; _latest = await _builds.GetLatestAsync(CancellationToken.None); var info = _latest.Info; var installed = _builds.ReadInstalled(); var needs = _builds.NeedsUpdate(_latest);
                _version.Text = "v" + info.BuildVersion; _address.Text = info.ServerAddress; _profile.Text = info.BuildName; _meta.Text = string.Format("Minecraft {0} · {1} {2}", info.MinecraftVersion, info.ModLoader, info.ModLoaderVersion); _loader.Text = "МОДЛОАДЕР\n" + info.ModLoader + " " + info.ModLoaderVersion; _minecraft.Text = "ВЕРСИЯ MINECRAFT\n" + info.MinecraftVersion; _status.Text = "СТАТУС\n" + (needs ? "Нужно обновить" : "Готово v" + installed.Version); _launch.Enabled = !needs;
                _update.Text = needs ? "Установить / обновить сборку" : "Проверить обновления"; if (needs) ShowNotice("Доступна новая сборка. Вход на сервер заблокирован до обновления.", false); else ShowNotice("Сборка актуальна. Можно запускать игру.", true);
            }
            catch (Exception error) { _status.Text = "СТАТУС\nОшибка"; ShowNotice(error.Message, false); }
        }
        private async Task InstallAsync()
        {
            try { SetBusy(true); _status.Text = "СТАТУС\nСкачивание…"; var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None); var progress = new Progress<int>(value => _progress.Value = Math.Max(0, Math.Min(100, value))); await _builds.InstallAsync(latest, progress, CancellationToken.None); ShowNotice("Сборка установлена, сервер добавлен в список Minecraft.", true); await RefreshAsync(); }
            catch (Exception error) { ShowNotice(error.Message, false); } finally { SetBusy(false); }
        }
        private async Task LaunchAsync()
        {
            try
            {
                var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None); if (_builds.NeedsUpdate(latest)) { ShowNotice("Сначала обновите сборку: запуск заблокирован.", false); return; }
                var command = latest.Info.LaunchCommand; if (string.IsNullOrWhiteSpace(command)) command = "start.bat"; var full = Path.Combine(_builds.InstanceDirectory, command);
                if (!File.Exists(full)) { MessageBox.Show("В архиве нет " + command + ". Добавьте стартовый файл сборки и укажите LAUNCH_COMMAND в build-info.txt.", "Не найден запуск", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Process.Start(new ProcessStartInfo { FileName = "cmd.exe", Arguments = "/c \"\"" + full + "\"\"", WorkingDirectory = _builds.InstanceDirectory, UseShellExecute = false }); _status.Text = "СТАТУС\nИгра запущена";
            }
            catch (Exception error) { ShowNotice(error.Message, false); }
            await Task.CompletedTask;
        }
        private void ShowNotice(string text, bool success) { _notice = _notice ?? new Label { AutoSize = false, Height = 30, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft }; _notice.Text = text; _notice.ForeColor = success ? Color.LightGreen : Color.FromArgb(255, 190, 150); if (_notice.Parent == null) Controls.Add(_notice); }
        private static void OpenUrl(string url) { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
    }
}
