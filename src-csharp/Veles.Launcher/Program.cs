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
        private Label _brand, _title, _subtitle, _version, _address, _profile, _meta, _loader, _minecraft, _status, _notice;
        private Button _update, _launch, _settingsButton;
        private ProgressBar _progress;
        private readonly Color Background = Color.FromArgb(12, 10, 9);
        private readonly Color Card = Color.FromArgb(28, 25, 23);
        private readonly Color Orange = Color.FromArgb(249, 115, 22);
        private readonly Color TextColor = Color.FromArgb(250, 250, 249);
        private readonly Color Muted = Color.FromArgb(168, 162, 158);

        public LauncherForm()
        {
            Text = "Veles Launcher"; Width = 1180; Height = 760; MinimumSize = new Size(1050, 700); BackColor = Background; ForeColor = TextColor; StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Dpi;
            BuildUi(); Shown += async (s, e) => { await RefreshAsync(); await CheckLauncherUpdateAsync(true); };
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 18), RowCount = 6, ColumnCount = 2, BackColor = Background };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); Controls.Add(root);
            var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Background, Margin = new Padding(0) }; header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            _brand = new Label { Text = "V  VELES PLAYGAME\n     SERVER LAUNCHER", AutoSize = false, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = TextColor, TextAlign = ContentAlignment.MiddleLeft };
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Padding = new Padding(0, 5, 0, 0), BackColor = Background }; var site = Button("Сайт сервера", Color.FromArgb(33, 25, 19), TextColor); site.Click += (s, e) => OpenUrl("https://veles-saite.vercel.app/"); var checkLauncher = Button("Обновить Launcher", Color.FromArgb(33, 25, 19), TextColor); checkLauncher.Click += async (s, e) => await CheckLauncherUpdateAsync(true); _settingsButton = Button("Настройки", Color.FromArgb(33, 25, 19), TextColor); _settingsButton.Click += (s, e) => ShowSettings(); actions.Controls.Add(site); actions.Controls.Add(checkLauncher); actions.Controls.Add(_settingsButton); header.Controls.Add(_brand, 0, 0); header.Controls.Add(actions, 1, 0); root.Controls.Add(header, 0, 0); root.SetColumnSpan(header, 2);
            var hero = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Background, Margin = new Padding(0) }; hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78)); hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            var heroText = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Background, Margin = new Padding(0) }; heroText.RowStyles.Add(new RowStyle(SizeType.Percent, 65)); heroText.RowStyles.Add(new RowStyle(SizeType.Percent, 35)); _title = new Label { Text = "Серверная сборка", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 26, FontStyle.Bold), ForeColor = Orange, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true }; _subtitle = new Label { Text = "НЕОФИЦИАЛЬНЫЙ СЕРВЕР  •  ОНЛАЙН   ·   ВЫЖИВАНИЕ / КВЕСТЫ", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), ForeColor = Muted, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }; heroText.Controls.Add(_title, 0, 0); heroText.Controls.Add(_subtitle, 0, 1); _version = new Label { Text = "v—", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Orange, TextAlign = ContentAlignment.MiddleRight }; hero.Controls.Add(heroText, 0, 0); hero.Controls.Add(_version, 1, 0); root.Controls.Add(hero, 0, 1); root.SetColumnSpan(hero, 2);
            root.Controls.Add(MakeServerCard(), 0, 2); root.Controls.Add(MakeBuildCard(), 1, 2);
            var details = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(12), BackColor = Card, Margin = new Padding(6, 8, 6, 0) }; details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33)); details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34)); _loader = Detail(details, "МОДЛОАДЕР", 0); _minecraft = Detail(details, "ВЕРСИЯ MINECRAFT", 1); _status = Detail(details, "СТАТУС", 2); root.Controls.Add(details, 0, 3); root.SetColumnSpan(details, 2);
            _notice = new Label { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Muted, BackColor = Background, Font = new Font("Segoe UI", 9), Visible = false, AutoEllipsis = true }; root.Controls.Add(_notice, 0, 4); root.SetColumnSpan(_notice, 2); var footer = new Label { Text = "Veles PlayGame  ·  Публичные релизы сборок  ·  GitHub Releases", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 8) }; root.Controls.Add(footer, 0, 5); root.SetColumnSpan(footer, 2);
        }

        private Control MakeServerCard()
        {
            var panel = CardPanel(); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20), BackColor = Card, Margin = new Padding(0) }; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.Controls.Add(layout); layout.Controls.Add(Label("◈  ПОДКЛЮЧЕНИЕ К СЕРВЕРУ", 0, 0, 15, TextColor, true), 0, 0); layout.Controls.Add(Label("IP АДРЕС СЕРВЕРА", 0, 0, 9, Muted, true), 0, 1); _address = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = TextColor, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; layout.Controls.Add(_address, 0, 2); var copyRow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Card, Margin = new Padding(0) }; var copy = Button("Копировать", Color.FromArgb(43, 28, 20), TextColor); copy.Width = 126; copy.Click += (s, e) => { if (_address.Text != "—") { Clipboard.SetText(_address.Text); ShowNotice("IP сервера скопирован.", false); } }; copyRow.Controls.Add(copy); layout.Controls.Add(copyRow, 0, 3); layout.Controls.Add(new Label { Text = "Адрес читается из build-info.txt текущего релиза.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), ForeColor = Muted, AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft }, 0, 4); return panel;
        }

        private Control MakeBuildCard()
        {
            var panel = CardPanel(); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(20), BackColor = Card, Margin = new Padding(0) }; layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.Controls.Add(layout); layout.Controls.Add(Label("⇩  СБОРКА И ОБНОВЛЕНИЕ", 0, 0, 15, TextColor, true), 0, 0); _profile = new Label { Text = "Сборка не загружена", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = TextColor, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; layout.Controls.Add(_profile, 0, 1); _meta = new Label { Text = "Сборка сервера пока не опубликована.", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9), ForeColor = Muted, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; layout.Controls.Add(_meta, 0, 2); var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, BackColor = Card, Margin = new Padding(0) }; _update = Button("Проверить и обновить сборку", Orange, Color.FromArgb(23, 13, 6)); _update.Width = 235; _update.Click += async (s, e) => await InstallAsync(); buttons.Controls.Add(_update); _launch = Button("Запустить Minecraft", Color.FromArgb(43, 28, 20), TextColor); _launch.Width = 190; _launch.Enabled = false; _launch.Click += async (s, e) => await LaunchAsync(); buttons.Controls.Add(_launch); layout.Controls.Add(buttons, 0, 3); _progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 6, Style = ProgressBarStyle.Continuous, Maximum = 100, Visible = false }; layout.Controls.Add(_progress, 0, 4); return panel;
        }

        private Label Detail(TableLayoutPanel panel, string caption, int column) { var label = new Label { Text = caption + "\n—", Dock = DockStyle.Fill, Padding = new Padding(8, 4, 8, 4), ForeColor = TextColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft }; panel.Controls.Add(label, column, 0); return label; }
        private Panel CardPanel() { return new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(6, 4, 6, 4), BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) }; }
        private Label Label(string text, int x, int y, float size, Color color, bool bold) { return new Label { Text = text, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color }; }
        private Button Button(string text, Color back, Color fore) { return new Button { Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, FlatAppearance = { BorderColor = Color.FromArgb(90, 55, 34) }, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; }
        private void SetBusy(bool value) { _update.Enabled = !value; _settingsButton.Enabled = !value; _launch.Enabled = !value && _latest != null && !_builds.NeedsUpdate(_latest); _progress.Visible = value; if (value) _progress.Value = 0; }
        private async Task RefreshAsync()
        {
            try
            {
                _status.Text = "СТАТУС\nПроверка…"; _latest = await _builds.GetLatestAsync(CancellationToken.None); var info = _latest.Info; var installed = _builds.ReadInstalled(); var needs = _builds.NeedsUpdate(_latest);
                var buildName = string.IsNullOrWhiteSpace(info.BuildName) ? "Серверная сборка" : info.BuildName.Trim(); Text = "Veles Launcher · " + buildName; _title.Text = buildName; _version.Text = "v" + info.BuildVersion; _address.Text = info.ServerAddress; _profile.Text = buildName; _meta.Text = string.Format("Minecraft {0} · {1} {2}", info.MinecraftVersion, info.ModLoader, info.ModLoaderVersion); _loader.Text = "МОДЛОАДЕР\n" + info.ModLoader + " " + info.ModLoaderVersion; _minecraft.Text = "ВЕРСИЯ MINECRAFT\n" + info.MinecraftVersion; _status.Text = "СТАТУС\n" + (needs ? "Нужно обновить" : "Готово v" + installed.Version); _launch.Enabled = !needs;
                _update.Text = needs ? "Обновить сборку сервера" : "Проверить обновления"; if (needs) ShowNotice("Доступна новая сборка. Вход на сервер заблокирован до обновления.", false); else ShowNotice("Сборка актуальна. Можно запускать игру.", true);
            }
            catch (Exception error) { _latest = null; Text = "Veles Launcher"; _version.Text = "v—"; _address.Text = "—"; _profile.Text = "Сборка не загружена"; _meta.Text = "Сборка сервера пока не опубликована."; _loader.Text = "МОДЛОАДЕР\n—"; _minecraft.Text = "ВЕРСИЯ MINECRAFT\n—"; _status.Text = "СТАТУС\nНет сборки"; _launch.Enabled = false; _update.Enabled = true; ShowNotice(GetFriendlyError(error), false); }
        }
        private async Task InstallAsync()
        {
            try { SetBusy(true); _status.Text = "СТАТУС\nСкачивание…"; var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None); var progress = new Progress<int>(value => _progress.Value = Math.Max(0, Math.Min(100, value))); await _builds.InstallAsync(latest, progress, CancellationToken.None); ShowNotice("Сборка установлена, сервер добавлен в список Minecraft.", true); await RefreshAsync(); }
            catch (Exception error) { ShowNotice(GetFriendlyError(error), false); } finally { SetBusy(false); }
        }
        private async Task LaunchAsync()
        {
            try
            {
                var latest = _latest ?? await _builds.GetLatestAsync(CancellationToken.None); if (_builds.NeedsUpdate(latest)) { ShowNotice("Сначала обновите сборку: запуск заблокирован.", false); return; }
                var profileName = string.IsNullOrWhiteSpace(latest.Info.ModLoaderProfile) ? "launch.json" : latest.Info.ModLoaderProfile; var profilePath = SafeInstancePath(profileName); var profile = LaunchProfile.Load(profilePath);
                var java = JavaRuntimeService.ResolveJavaExecutable(_builds.InstanceDirectory, latest.Info); JavaRuntimeService.VerifySha256(java, latest.Info.JavaRuntimeSha256);
                var classPath = ReplaceTokens(profile.ClassPath); var jvm = ReplaceTokens(profile.JvmArguments); if (string.IsNullOrWhiteSpace(jvm)) jvm = "-Xms" + (string.IsNullOrWhiteSpace(latest.Info.MemoryMin) ? "2G" : latest.Info.MemoryMin) + " -Xmx" + (string.IsNullOrWhiteSpace(latest.Info.MemoryMax) ? "6G" : latest.Info.MemoryMax);
                var game = ReplaceTokens(profile.GameArguments); var arguments = jvm + " -cp " + Quote(classPath) + " " + profile.MainClass + (string.IsNullOrWhiteSpace(game) ? string.Empty : " " + game);
                Process.Start(new ProcessStartInfo { FileName = java, Arguments = arguments, WorkingDirectory = _builds.InstanceDirectory, UseShellExecute = false }); _status.Text = "СТАТУС\nИгра запущена";
            }
            catch (Exception error) { ShowNotice(GetFriendlyError(error), false); }
            await Task.CompletedTask;
        }
        private string ReplaceTokens(string value) { return (value ?? string.Empty).Replace("${INSTANCE}", _builds.InstanceDirectory).Replace("/", "\\"); }
        private string SafeInstancePath(string relative) { var root = Path.GetFullPath(_builds.InstanceDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; var full = Path.GetFullPath(Path.Combine(_builds.InstanceDirectory, relative)); if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Профиль запуска выходит за пределы сборки."); return full; }
        private static string Quote(string value) { return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\""; }
        private string GetFriendlyError(Exception error) { var message = error == null ? string.Empty : error.Message; if (message.IndexOf("404", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("пустой релиз", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("Последний релиз", StringComparison.OrdinalIgnoreCase) >= 0) return "Сборка сервера пока не опубликована."; if (message.IndexOf("build-info.txt", StringComparison.OrdinalIgnoreCase) >= 0) return "Релиз сборки заполнен неправильно. Обратитесь к администратору сервера."; if (message.IndexOf("SHA-256", StringComparison.OrdinalIgnoreCase) >= 0) return "Архив сборки повреждён или не прошёл проверку целостности."; return "Не удалось проверить сборку. Проверьте подключение к интернету."; }
        private void ShowNotice(string text, bool success) { if (_notice == null) return; _notice.Text = text; _notice.ForeColor = success ? Color.LightGreen : Color.FromArgb(255, 190, 150); _notice.Visible = true; }
        private async Task CheckLauncherUpdateAsync(bool askUser)
        {
            try
            {
                var github = new GitHubClient("kutsandriy14-cyber", "veles-launcher"); var release = await github.GetLatestReleaseAsync(CancellationToken.None); Version latestVersion; if (!Version.TryParse((release.TagName ?? "0").TrimStart('v', 'V'), out latestVersion) || latestVersion <= ProductInfo.Version) { if (askUser) ShowNotice("Veles Launcher уже обновлён до последней версии.", true); return; }
                var asset = release.Assets == null ? null : release.Assets.Find(x => string.Equals(x.Name, "VelesLauncherSetup.exe", StringComparison.OrdinalIgnoreCase)); if (asset == null) { if (askUser) ShowNotice("Новая версия найдена, но установщик пока недоступен.", false); return; }
                var answer = MessageBox.Show(this, "Доступна новая версия Veles Launcher v" + latestVersion + ". Обновить приложение сейчас?", "Обновление лаунчера", MessageBoxButtons.YesNo, MessageBoxIcon.Information); if (answer != DialogResult.Yes) return;
                var updater = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Veles.Updater.exe"); if (!File.Exists(updater)) { ShowNotice("Служебный обновлятор не найден рядом с Launcher.", false); return; }
                Process.Start(new ProcessStartInfo { FileName = updater, Arguments = "--auto --wait-pid " + Process.GetCurrentProcess().Id, WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory, UseShellExecute = true }); Application.Exit();
            }
            catch { if (askUser) ShowNotice("Не удалось проверить обновление Launcher. Можно попробовать позже.", false); }
        }
        private void ShowSettings()
        {
            using (var dialog = new SettingsForm(_builds.Settings, _builds.InstanceDirectory))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) { _builds.SaveSettings(dialog.Value); ShowNotice("Настройки сохранены. Путь экземпляра: " + _builds.InstanceDirectory, true); }
            }
        }
        private static void OpenUrl(string url) { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly TextBox _path; private readonly NumericUpDown _minimum; private readonly NumericUpDown _maximum; private readonly Label _javaStatus; public LauncherSettings Value { get; private set; }
        public SettingsForm(LauncherSettings current, string instancePath)
        {
            Value = new LauncherSettings { InstanceDirectory = current.InstanceDirectory, MinimumMemoryMb = current.MinimumMemoryMb, MaximumMemoryMb = current.MaximumMemoryMb };
            Text = "Настройки Veles Launcher"; Width = 720; Height = 430; MinimumSize = new Size(720, 430); MaximumSize = new Size(720, 430); BackColor = Color.FromArgb(12, 10, 9); ForeColor = Color.FromArgb(250, 250, 249); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; AutoScaleMode = AutoScaleMode.Dpi;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 7, BackColor = BackColor }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); Controls.Add(root);
            root.Controls.Add(new Label { Text = "НАСТРОЙКИ ИГРЫ", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 17, FontStyle.Bold), ForeColor = Color.FromArgb(249, 115, 22), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Color.FromArgb(28, 25, 23), Padding = new Padding(12, 10, 12, 10), Margin = new Padding(0) }; pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126)); pathRow.Controls.Add(new Label { Text = "Папка экземпляра", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), TextAlign = ContentAlignment.MiddleLeft }, 0, 0); _path = new TextBox { Text = Value.InstanceDirectory, Dock = DockStyle.Fill, BackColor = Color.FromArgb(38, 35, 33), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(4, 5, 8, 5) }; pathRow.Controls.Add(_path, 1, 0); var browse = new Button { Text = "Выбрать папку", Dock = DockStyle.Fill, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 4, 0, 4) }; browse.Click += (s, e) => { using (var dialog = new FolderBrowserDialog { SelectedPath = _path.Text }) if (dialog.ShowDialog(this) == DialogResult.OK) _path.Text = dialog.SelectedPath; }; pathRow.Controls.Add(browse, 2, 0); root.Controls.Add(pathRow, 0, 1);
            root.Controls.Add(Field("Минимальная память (МБ)", _minimum = MemoryBox(Value.MinimumMemoryMb)), 0, 2); root.Controls.Add(Field("Максимальная память (МБ)", _maximum = MemoryBox(Value.MaximumMemoryMb)), 0, 3);
            _javaStatus = new Label { Text = "Java: встроенный runtime будет проверен внутри установленной сборки", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(249, 115, 22), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true }; root.Controls.Add(_javaStatus, 0, 4);
            root.Controls.Add(new Label { Text = "Java устанавливается автоматически вместе с корректным релизом сборки. Ручная установка не требуется.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft }, 0, 5);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 5, 0, 0) }; var save = new Button { Text = "Сохранить", Width = 120, Height = 34, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; var cancel = new Button { Text = "Отмена", Width = 100, Height = 34, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(43, 28, 20), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) }; save.Click += (s, e) => Save(); buttons.Controls.Add(save); buttons.Controls.Add(cancel); root.Controls.Add(buttons, 0, 6); AcceptButton = save; CancelButton = cancel;
        }
        private Control Field(string caption, Control control) { var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Color.FromArgb(28, 25, 23), Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0) }; row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), TextAlign = ContentAlignment.MiddleLeft }, 0, 0); control.Dock = DockStyle.Fill; control.Margin = new Padding(4, 3, 0, 3); row.Controls.Add(control, 1, 0); return row; }
        private NumericUpDown MemoryBox(int value) { return new NumericUpDown { Minimum = 1024, Maximum = 65536, Increment = 512, Value = Math.Max(1024, Math.Min(65536, value)), Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White, ThousandsSeparator = true }; }
        private void Save() { if (string.IsNullOrWhiteSpace(_path.Text)) { MessageBox.Show(this, "Укажите папку экземпляра.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } if (_maximum.Value < _minimum.Value) { MessageBox.Show(this, "Максимальная память должна быть не меньше минимальной.", "Настройки", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } Value.InstanceDirectory = _path.Text.Trim(); Value.MinimumMemoryMb = (int)_minimum.Value; Value.MaximumMemoryMb = (int)_maximum.Value; DialogResult = DialogResult.OK; Close(); }
    }
}
