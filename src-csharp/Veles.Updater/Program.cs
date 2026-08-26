using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using Veles.Core;

namespace Veles.Updater
{
    internal static class Program
    {
        [STAThread]
        private static void Main() { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new UpdaterForm()); }
    }

    internal sealed class UpdaterForm : Form
    {
        private readonly GitHubClient _github = new GitHubClient("kutsandriy14-cyber", "veles-launcher");
        private readonly HttpClient _http = new HttpClient();
        private readonly Version _current = new Version(0, 1, 0);
        private Label _status, _versions; private Button _install; private ReleaseInfo _release;
        public UpdaterForm()
        {
            Text = "Veles Launcher Updater"; Width = 560; Height = 360; BackColor = Color.FromArgb(16, 11, 8); ForeColor = Color.White; StartPosition = FormStartPosition.CenterScreen; BuildUi(); Shown += async (s, e) => await CheckAsync();
        }
        private void BuildUi()
        {
            var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(32) }; Controls.Add(root);
            root.Controls.Add(Label("V  VELES LAUNCHER UPDATER", 0, 0, 14, FontStyle.Bold, Color.White)); root.Controls.Add(Label("Проверка официального релиза лаунчера", 2, 28, 9, FontStyle.Regular, Color.FromArgb(163, 149, 141))); root.Controls.Add(Label("ОБНОВЛЕНИЕ ПРИЛОЖЕНИЯ", 0, 76, 10, FontStyle.Bold, Color.FromArgb(255, 122, 25))); root.Controls.Add(Label("Проверка версии", 0, 100, 24, FontStyle.Bold, Color.White)); _status = Label("Проверяем GitHub Releases…", 0, 145, 11, FontStyle.Regular, Color.FromArgb(200, 184, 173)); _versions = Label("", 0, 172, 10, FontStyle.Regular, Color.FromArgb(255, 157, 60)); root.Controls.Add(_status); root.Controls.Add(_versions); _install = new Button { Text = "Скачать и установить", Location = new Point(0, 215), Width = 190, Height = 36, Enabled = false, BackColor = Color.FromArgb(255, 122, 25), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; _install.Click += async (s, e) => await InstallAsync(); root.Controls.Add(_install);
        }
        private async Task CheckAsync()
        {
            try
            {
                _release = await _github.GetLatestReleaseAsync(CancellationToken.None); var latest = ParseVersion(_release.TagName); _versions.Text = "Установлено: v" + _current + " · Последняя: v" + latest;
                if (latest > _current) { _status.Text = "Доступна новая версия лаунчера."; _install.Enabled = FindAsset(_release, "VelesLauncherSetup.exe") != null; if (!_install.Enabled) _status.Text = "Релиз найден, но установщик VelesLauncherSetup.exe отсутствует."; }
                else _status.Text = "Установлена последняя версия лаунчера.";
            }
            catch (Exception error) { _status.Text = error.Message; }
        }
        private async Task InstallAsync()
        {
            var asset = FindAsset(_release, "VelesLauncherSetup.exe"); if (asset == null) return;
            try { _install.Enabled = false; _status.Text = "Скачивание установщика…"; var path = Path.Combine(Path.GetTempPath(), "VelesLauncherSetup.exe"); var data = await _http.GetByteArrayAsync(asset.BrowserDownloadUrl); File.WriteAllBytes(path, data); Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); Application.Exit(); }
            catch (Exception error) { _status.Text = error.Message; _install.Enabled = true; }
        }
        private static Version ParseVersion(string value) { Version result; return Version.TryParse((value ?? "0").TrimStart('v', 'V').Replace("build-", ""), out result) ? result : new Version(0, 0); }
        private static ReleaseAsset FindAsset(ReleaseInfo release, string name) { foreach (var asset in release.Assets) if (string.Equals(asset.Name, name, StringComparison.OrdinalIgnoreCase)) return asset; return null; }
        private static Label Label(string text, int x, int y, float size, FontStyle style, Color color) { return new Label { Text = text, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", size, style), ForeColor = color }; }
    }
}
