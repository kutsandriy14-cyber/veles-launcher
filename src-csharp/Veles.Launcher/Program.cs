using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using Veles.Core;

namespace Veles.Launcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Cef.EnableHighDPISupport();
            var cefSettings = new CefSettings { CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Veles", "cef-cache") };
            if (!Cef.Initialize(cefSettings, true, (IBrowserProcessHandler)null)) return;
            Application.Run(new LauncherForm());
            Cef.Shutdown();
        }
    }

    public sealed class LauncherForm : Form
    {
        private readonly BuildService _builds = new BuildService();
        private ChromiumWebBrowser _browser;
        private BuildSnapshot _latest;
        private bool _busy;
        private string _lastNotice = "Сборка сервера пока не опубликована.";

        public LauncherForm()
        {
            Text = "Veles Launcher";
            Width = 1180;
            Height = 760;
            MinimumSize = new Size(900, 620);
            BackColor = Color.FromArgb(13, 11, 10);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            BuildUi();
            Shown += async (s, e) => { await RefreshAsync(); await CheckLauncherUpdateAsync(false); };
        }

        private void BuildUi()
        {
            _browser = new ChromiumWebBrowser("about:blank") { Dock = DockStyle.Fill };
            _browser.JavascriptObjectRepository.Settings.LegacyBindingEnabled = true;
            _browser.JavascriptObjectRepository.Register("velesBridge", new LauncherBridge(this), false, new BindingOptions());
            Controls.Add(_browser);
            var html = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebUi", "index.html");
            if (File.Exists(html)) _browser.Load(new Uri(html).AbsoluteUri);
            else _browser.LoadHtml("<html><body style='background:#100e0d;color:white;font-family:Segoe UI;padding:30px'>Web UI не найден. Переустановите Launcher.</body></html>", "http://veles.local/");
        }

        private async Task RefreshAsync()
        {
            try
            {
                var allBuilds = await _builds.GetAllAsync(CancellationToken.None);
                _latest = null;
                foreach (var candidate in allBuilds) if (candidate.Release.IsActive) { _latest = candidate; break; }
                if (_latest == null) throw new InvalidDataException("Нет активной сборки с build-info.txt и build.zip.");
                var info = _latest.Info;
                var installed = _builds.ReadInstalled();
                var needs = _builds.NeedsUpdate(_latest);
                var buildName = string.IsNullOrWhiteSpace(info.BuildName) ? "Серверная сборка" : info.BuildName.Trim();
                var state = "{\"buildName\":" + Json(buildName) + ",\"version\":" + Json(info.BuildVersion) + ",\"address\":" + Json(info.ServerAddress) + ",\"minecraft\":" + Json(info.MinecraftVersion) + ",\"loader\":" + Json(info.ModLoader) + ",\"loaderVersion\":" + Json(info.ModLoaderVersion) + ",\"status\":" + Json(needs ? "Нужно обновить" : "Готово v" + (installed == null ? info.BuildVersion : installed.Version)) + ",\"installed\":" + ((!needs).ToString().ToLowerInvariant()) + ",\"needsUpdate\":" + needs.ToString().ToLowerInvariant() + ",\"builds\":[" + BuildListJson(allBuilds) + "]}";
                ApplyState(state);
                ApplyNotice(needs ? "Доступна новая сборка. Вход на сервер заблокирован до обновления." : "Сборка актуальна. Можно запускать игру.", needs ? false : true);
            }
            catch (Exception error)
            {
                _latest = null;
                ApplyState("{\"buildName\":\"Серверная сборка\",\"version\":\"—\",\"address\":\"—\",\"minecraft\":\"—\",\"loader\":\"—\",\"loaderVersion\":\"\",\"status\":\"Нет сборки\",\"installed\":false,\"needsUpdate\":true}");
                ApplyNotice(GetFriendlyError(error), false);
            }
        }

        internal async Task InstallAsync()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                ApplyNotice("Проверяю последний релиз сборки…", false);
                ApplyUiText("status-value", "Скачивание…");
                var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None);
                await _builds.InstallAsync(latest, null, CancellationToken.None);
                await RefreshAsync();
                ApplyNotice("Сборка установлена, сервер добавлен в список Minecraft.", true);
            }
            catch (Exception error)
            {
                ApplyNotice(GetFriendlyError(error), false);
                ApplyUiText("status-value", "Ошибка");
            }
            finally
            {
                _busy = false;
                ApplyUiText("update-button", _latest == null ? "Проверить и обновить сборку" : (_builds.NeedsUpdate(_latest) ? "Обновить сборку сервера" : "Проверить обновления"));
            }
        }

        internal async Task LaunchAsync()
        {
            try
            {
                var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None);
                if (_builds.NeedsUpdate(latest)) { ApplyNotice("Сначала обновите сборку: запуск заблокирован.", false); return; }
                var profileName = string.IsNullOrWhiteSpace(latest.Info.ModLoaderProfile) ? "launch.json" : latest.Info.ModLoaderProfile;
                var profilePath = SafeInstancePath(profileName);
                var profile = LaunchProfile.Load(profilePath);
                var java = JavaRuntimeService.ResolveJavaExecutable(_builds.InstanceDirectory, latest.Info);
                JavaRuntimeService.VerifySha256(java, latest.Info.JavaRuntimeSha256);
                var classPath = ReplaceTokens(profile.ClassPath);
                var jvm = ReplaceTokens(profile.JvmArguments);
                if (string.IsNullOrWhiteSpace(jvm)) jvm = "-Xms" + (string.IsNullOrWhiteSpace(latest.Info.MemoryMin) ? "2G" : latest.Info.MemoryMin) + " -Xmx" + (string.IsNullOrWhiteSpace(latest.Info.MemoryMax) ? "6G" : latest.Info.MemoryMax);
                var game = ReplaceTokens(profile.GameArguments);
                var arguments = jvm + " -cp " + Quote(classPath) + " " + profile.MainClass + (string.IsNullOrWhiteSpace(game) ? string.Empty : " " + game);
                Process.Start(new ProcessStartInfo { FileName = java, Arguments = arguments, WorkingDirectory = _builds.InstanceDirectory, UseShellExecute = false });
                ApplyUiText("status-value", "Игра запущена");
                ApplyNotice("Minecraft запущен.", true);
            }
            catch (Exception error) { ApplyNotice(GetFriendlyError(error), false); }
        }

        internal void OpenSettings()
        {
            using (var dialog = new SettingsForm(_builds.Settings))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) { _builds.SaveSettings(dialog.Value); ApplyNotice("Настройки сохранены.", true); }
            }
        }

        internal void OpenSite() { Process.Start(new ProcessStartInfo("https://veles-saite.vercel.app/") { UseShellExecute = true }); }
        internal void CopyAddress()
        {
            if (_latest == null || string.IsNullOrWhiteSpace(_latest.Info.ServerAddress)) { ApplyNotice("Адрес появится после публикации сборки.", false); return; }
            Clipboard.SetText(_latest.Info.ServerAddress);
            ApplyNotice("IP сервера скопирован.", true);
        }

        private async Task CheckLauncherUpdateAsync(bool askUser)
        {
            try
            {
                var github = new GitHubClient("kutsandriy14-cyber", "veles-launcher");
                var release = await github.GetLatestReleaseAsync(CancellationToken.None);
                Version latestVersion;
                if (!Version.TryParse((release.TagName ?? "0").TrimStart('v', 'V'), out latestVersion) || latestVersion <= ProductInfo.Version) return;
                var asset = release.Assets == null ? null : release.Assets.Find(x => string.Equals(x.Name, "VelesLauncherSetup.exe", StringComparison.OrdinalIgnoreCase));
                if (asset == null) return;
                var answer = MessageBox.Show(this, "Доступна новая версия Veles Launcher v" + latestVersion + ". Обновить приложение сейчас?", "Обновление лаунчера", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer != DialogResult.Yes) return;
                var updater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Veles.Updater.exe");
                if (!File.Exists(updater)) { ApplyNotice("Служебный обновлятор не найден рядом с Launcher.", false); return; }
                Process.Start(new ProcessStartInfo { FileName = updater, Arguments = "--auto --wait-pid " + Process.GetCurrentProcess().Id, WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory, UseShellExecute = true });
                Application.Exit();
            }
            catch { if (askUser) ApplyNotice("Не удалось проверить обновление Launcher.", false); }
        }

        private void ApplyState(string stateJson)
        {
            if (_browser == null || !_browser.IsBrowserInitialized) return;
            try { _browser.GetMainFrame().ExecuteJavaScriptAsync("window.velesSetState(" + stateJson + ");", "veles://state", 0); } catch { }
        }
        private void ApplyUiText(string id, string value)
        {
            if (_browser == null || !_browser.IsBrowserInitialized) return;
            try { _browser.GetMainFrame().ExecuteJavaScriptAsync("(function(){var e=document.getElementById(" + Json(id) + ");if(e)e.textContent=" + Json(value) + ";})();", "veles://text", 0); } catch { }
        }
        private void ApplyNotice(string text, bool success)
        {
            _lastNotice = text;
            if (_browser == null || !_browser.IsBrowserInitialized) return;
            try { _browser.GetMainFrame().ExecuteJavaScriptAsync("(function(){var e=document.getElementById('notice');if(e){e.textContent=" + Json(text) + ";e.style.color=" + Json(success ? "#9fe3ad" : "#e3a982") + ";}})();", "veles://notice", 0); } catch { }
        }
        private string BuildListJson(System.Collections.Generic.List<BuildSnapshot> builds) { var items = new System.Collections.Generic.List<string>(); foreach (var item in builds) { var name = string.IsNullOrWhiteSpace(item.Info.BuildName) ? "Серверная сборка" : item.Info.BuildName.Trim(); items.Add("{\"name\":" + Json(name) + ",\"version\":" + Json(item.Info.BuildVersion) + ",\"active\":" + item.Release.IsActive.ToString().ToLowerInvariant() + ",\"priority\":" + item.Release.Priority + "}"); } return string.Join(",", items); }
        private string ReplaceTokens(string value) { return (value ?? string.Empty).Replace("${INSTANCE}", _builds.InstanceDirectory).Replace("/", "\\"); }
        private string SafeInstancePath(string relative) { var root = Path.GetFullPath(_builds.InstanceDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; var full = Path.GetFullPath(Path.Combine(_builds.InstanceDirectory, relative)); if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Профиль запуска выходит за пределы сборки."); return full; }
        private static string Quote(string value) { return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\""; }
        private static string Json(string value) { return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n") + "\""; }
        private string GetFriendlyError(Exception error) { var message = error == null ? string.Empty : error.Message; if (message.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("пустой релиз", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("Последний релиз", StringComparison.OrdinalIgnoreCase) >= 0) return "Сборка сервера пока не опубликована."; if (message.IndexOf("build-info.txt", StringComparison.OrdinalIgnoreCase) >= 0) return "Релиз сборки заполнен неправильно. Обратитесь к администратору сервера."; if (message.IndexOf("SHA-256", StringComparison.OrdinalIgnoreCase) >= 0) return "Архив сборки повреждён или не прошёл проверку целостности."; return "Не удалось проверить сборку. Проверьте подключение к интернету."; }
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class LauncherBridge
    {
        private readonly LauncherForm _form;
        public LauncherBridge(LauncherForm form) { _form = form; }
        public void Invoke(string action) { Notify(action); }
        public void Notify(string action)
        {
            if (action == "build.update") _form.BeginInvoke(new Action(async () => await _form.InstallAsync()));
            else if (action == "game.launch") _form.BeginInvoke(new Action(async () => await _form.LaunchAsync()));
            else if (action == "settings.open") _form.BeginInvoke(new Action(_form.OpenSettings));
            else if (action == "site.open") _form.BeginInvoke(new Action(_form.OpenSite));
            else if (action == "server.copy") _form.BeginInvoke(new Action(_form.CopyAddress));
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly TextBox _path;
        private readonly NumericUpDown _minimum;
        private readonly NumericUpDown _maximum;
        public LauncherSettings Value { get; private set; }
        public SettingsForm(LauncherSettings current)
        {
            Value = new LauncherSettings { InstanceDirectory = current.InstanceDirectory, MinimumMemoryMb = current.MinimumMemoryMb, MaximumMemoryMb = current.MaximumMemoryMb };
            Text = "Настройки Veles Launcher"; Width = 720; Height = 430; MinimumSize = new Size(720, 430); MaximumSize = new Size(720, 430); BackColor = Color.FromArgb(12, 10, 9); ForeColor = Color.White; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 6, BackColor = BackColor }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
            root.Controls.Add(new Label { Text = "НАСТРОЙКИ ИГРЫ", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(249, 115, 22), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.FromArgb(28, 25, 23), Padding = new Padding(10), Margin = new Padding(0) }; pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116)); pathRow.Controls.Add(new Label { Text = "Папка экземпляра", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), TextAlign = ContentAlignment.MiddleLeft }, 0, 0); _path = new TextBox { Text = Value.InstanceDirectory, Dock = DockStyle.Fill, BackColor = Color.FromArgb(38, 35, 33), ForeColor = Color.White, Margin = new Padding(4, 5, 8, 5) }; pathRow.Controls.Add(_path, 1, 0); var browse = new Button { Text = "Выбрать папку", Dock = DockStyle.Fill, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat }; browse.Click += (s, e) => { using (var dialog = new FolderBrowserDialog { SelectedPath = _path.Text }) if (dialog.ShowDialog(this) == DialogResult.OK) _path.Text = dialog.SelectedPath; }; pathRow.Controls.Add(browse, 2, 0); root.Controls.Add(pathRow, 0, 1);
            root.Controls.Add(Field("Минимальная память (МБ)", _minimum = MemoryBox(Value.MinimumMemoryMb)), 0, 2); root.Controls.Add(Field("Максимальная память (МБ)", _maximum = MemoryBox(Value.MaximumMemoryMb)), 0, 3);
            var javaPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
            var javaTitle = new Label { Text = "Java: ", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
            var javaRuntime = new Label { Text = "встроенный runtime", AutoSize = true, ForeColor = Color.FromArgb(249, 115, 22), Font = new Font("Segoe UI", 10), Location = new Point(42, 0) };
            var javaDesc = new Label { Text = "Java устанавливается автоматически вместе\nс корректным релизом сборки", AutoSize = true, ForeColor = Color.FromArgb(168, 162, 158), Font = new Font("Segoe UI", 9), Location = new Point(0, 24) };
            javaPanel.Controls.Add(javaTitle); javaPanel.Controls.Add(javaRuntime); javaPanel.Controls.Add(javaDesc); root.Controls.Add(javaPanel, 0, 4);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 8, 0, 0) }; var save = new Button { Text = "Сохранить", Width = 120, Height = 34, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat }; var cancel = new Button { Text = "Отмена", Width = 100, Height = 34, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(43, 28, 20), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 10, 0) }; save.Click += (s, e) => Save(); buttons.Controls.Add(save); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 5); AcceptButton = save; CancelButton = cancel;
        }
        private Control Field(string caption, Control control) { var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.FromArgb(28, 25, 23), Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0) }; row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 225)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), TextAlign = ContentAlignment.MiddleLeft }, 0, 0); control.Dock = DockStyle.Fill; row.Controls.Add(control, 1, 0); return row; }
        private NumericUpDown MemoryBox(int value) { return new NumericUpDown { Minimum = 1024, Maximum = 65536, Increment = 512, Value = Math.Max(1024, Math.Min(65536, value)), Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White, ThousandsSeparator = true }; }
        private void Save() { if (string.IsNullOrWhiteSpace(_path.Text)) { MessageBox.Show(this, "Укажите папку экземпляра.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } if (_maximum.Value < _minimum.Value) { MessageBox.Show(this, "Максимальная память должна быть не меньше минимальной.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } Value.InstanceDirectory = _path.Text.Trim(); Value.MinimumMemoryMb = (int)_minimum.Value; Value.MaximumMemoryMb = (int)_maximum.Value; DialogResult = DialogResult.OK; Close(); }
    }
}
