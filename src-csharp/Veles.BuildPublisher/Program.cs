using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Veles.Core;

namespace Veles.BuildPublisher
{
    internal static class Program
    {
        [STAThread]
        private static void Main() { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new PublisherForm()); }
    }

    internal sealed class PublisherForm : Form
    {
        private TextBox _zip, _name, _version, _minecraft, _loaderVersion, _address, _serverName, _site, _javaVersion, _javaPath, _memoryMin, _memoryMax;
        private ComboBox _loader;
        private Label _status, _accessStatus;
        private string _tokenValue;
        private Button _publish;
        private string _selectedZip;
        private BuildArchiveMetadata _archiveInfo;
        private readonly Color Orange = Color.FromArgb(249, 115, 22);
        private readonly Color Background = Color.FromArgb(12, 10, 9);
        private readonly Color Card = Color.FromArgb(28, 25, 23);
        private readonly Color TextColor = Color.FromArgb(250, 250, 249);
        private readonly Color Muted = Color.FromArgb(168, 162, 158);

        public PublisherForm()
        {
            Text = "Veles Build Publisher"; Width = 1180; Height = 1100; MinimumSize = new Size(1180, 1100); BackColor = Background; ForeColor = TextColor; StartPosition = FormStartPosition.CenterScreen; BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(45), ColumnCount = 1, RowCount = 18, BackColor = Background, AutoScroll = true };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            for (var i = 3; i < 15; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);
            var title = Label("Veles Build Publisher", 28, FontStyle.Bold, Orange); root.Controls.Add(title, 0, 0);
            var repo = Label("Публикация в kutsandriy14-cyber/veles-modpack-releases", 14, FontStyle.Regular, Muted); root.Controls.Add(repo, 0, 1);
            var filePanel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16) }; filePanel.Controls.Add(Label("Архив сборки (.zip)", 14, FontStyle.Regular, Muted)); _zip = TextBoxControl("", true); _zip.Location = new Point(0, 38); _zip.Width = 800; _zip.Font = new Font("Segoe UI", 16); filePanel.Controls.Add(_zip); var choose = Button("Выбрать ZIP", Orange, Color.Black); choose.Location = new Point(820, 34); choose.Width = 220; choose.Height = 48; choose.Font = new Font("Segoe UI", 14, FontStyle.Bold); choose.Click += (s, e) => ChooseZip(); filePanel.Controls.Add(choose); root.Controls.Add(filePanel, 0, 2);
            root.Controls.Add(FieldPanel("Название сборки", _name = TextBoxControl("", true)), 0, 3);
            root.Controls.Add(FieldPanel("Версия сборки", _version = TextBoxControl("", true)), 0, 4);
            root.Controls.Add(FieldPanel("Версия Minecraft", _minecraft = TextBoxControl("", true)), 0, 5);
            root.Controls.Add(FieldPanel("Модлоадер", _loader = LoaderControl()), 0, 6);
            root.Controls.Add(FieldPanel("Версия модлоадера", _loaderVersion = TextBoxControl("", true)), 0, 7);
            root.Controls.Add(FieldPanel("Адрес сервера (IP:PORT)", _address = TextBoxControl("", true)), 0, 8);
            root.Controls.Add(FieldPanel("Название сервера", _serverName = TextBoxControl("", true)), 0, 9);
            root.Controls.Add(FieldPanel("Сайт сервера", _site = TextBoxControl("", true)), 0, 10);
            root.Controls.Add(FieldPanel("Версия Java", _javaVersion = TextBoxControl("", true)), 0, 11);
            root.Controls.Add(FieldPanel("Папка Java в ZIP", _javaPath = TextBoxControl("", true)), 0, 12);
            root.Controls.Add(FieldPanel("Память от", _memoryMin = TextBoxControl("", true)), 0, 13);
            root.Controls.Add(FieldPanel("Память до", _memoryMax = TextBoxControl("", true)), 0, 14);
            SetMetadataFieldsReadOnly();
            var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 15, 0, 0) };
            var access = Button("Настроить доступ", Color.FromArgb(43, 28, 20), TextColor); access.Width = 220; access.Height = 54; access.Font = new Font("Segoe UI", 14); access.Click += (s, e) => ConfigureAccess();
            _publish = Button("Опубликовать сборку", Orange, Color.Black); _publish.Width = 320; _publish.Height = 54; _publish.Font = new Font("Segoe UI", 14, FontStyle.Bold); _publish.Margin = new Padding(25, 0, 0, 0); _publish.Click += async (s, e) => await PublishAsync();
            bottomPanel.Controls.Add(access); bottomPanel.Controls.Add(_publish); root.Controls.Add(bottomPanel, 0, 15);
            _accessStatus = Label("Доступ GitHub не настроен", 12, FontStyle.Regular, Muted); root.Controls.Add(_accessStatus, 0, 16);
            _status = Label("Готово. Выберите ZIP с metadata внутри.", 12, FontStyle.Regular, Muted); _status.AutoSize = false; _status.Dock = DockStyle.Fill; root.Controls.Add(_status, 0, 17);
        }

        private void AddPair(TableLayoutPanel root, int row, string leftCaption, Control left, string rightCaption, Control right)
        {
            var leftPanel = FieldPanel(leftCaption, left); var rightPanel = FieldPanel(rightCaption, right); root.Controls.Add(leftPanel, 0, row); root.Controls.Add(rightPanel, 1, row);
        }
        private Panel FieldPanel(string caption, Control input) { var panel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(16, 10, 16, 10) }; panel.Controls.Add(Label(caption, 14, FontStyle.Regular, Muted)); input.Location = new Point(0, 38); input.Width = 1040; input.Font = new Font("Segoe UI", 16); input.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; panel.Controls.Add(input); return panel; }
        private ComboBox LoaderControl() { var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 12, 11), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 16) }; box.Items.AddRange(new object[] { "Forge", "Fabric", "Quilt", "NeoForge" }); box.SelectedIndex = 0; return box; }
        private TextBox TextBoxControl(string value, bool readOnly) { return new TextBox { Text = value, ReadOnly = readOnly, BackColor = Color.FromArgb(15, 12, 11), ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle, Height = 28 }; }
        private Label Label(string text, float size, FontStyle style, Color color) { return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, style), ForeColor = color }; }
        private Button Button(string text, Color background, Color foreground) { return new Button { Text = text, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; }

        private void ChooseZip()
        {
            using (var dialog = new OpenFileDialog { Filter = "ZIP archive (*.zip)|*.zip", Title = "Выберите архив сборки" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                _selectedZip = dialog.FileName; _zip.Text = Path.GetFileName(_selectedZip); _archiveInfo = null;
                try
                {
                    _archiveInfo = BuildArchiveMetadata.Read(_selectedZip);
                    FillMetadata(_archiveInfo.Info);
                    _status.Text = "Проверено автоматически: metadata, launch.json и встроенная Java найдены.";
                }
                catch (Exception error) { _selectedZip = null; _zip.Text = string.Empty; _status.Text = error.Message; MessageBox.Show(this, error.Message, "Архив не прошёл проверку", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            }
        }

        private void SetMetadataFieldsReadOnly()
        {
            _name.ReadOnly = true; _version.ReadOnly = true; _minecraft.ReadOnly = true; _loaderVersion.ReadOnly = true; _address.ReadOnly = true; _serverName.ReadOnly = true; _site.ReadOnly = true; _javaVersion.ReadOnly = true; _javaPath.ReadOnly = true; _memoryMin.ReadOnly = true; _memoryMax.ReadOnly = true; _loader.Enabled = false;
        }

        private void FillMetadata(BuildInfo info)
        {
            _name.Text = info.BuildName; _version.Text = info.BuildVersion; _minecraft.Text = info.MinecraftVersion; _loader.SelectedItem = info.ModLoader; _loaderVersion.Text = info.ModLoaderVersion; _address.Text = info.ServerAddress; _serverName.Text = info.ServerName; _site.Text = info.SiteUrl; _javaVersion.Text = info.JavaVersion; _javaPath.Text = info.JavaRuntimePath; _memoryMin.Text = info.MemoryMin; _memoryMax.Text = info.MemoryMax;
        }

        private async Task PublishAsync()
        {
            if (string.IsNullOrWhiteSpace(_selectedZip) || _archiveInfo == null) { Warn("Выберите ZIP с корректной metadata внутри архива."); return; }
            if (string.IsNullOrWhiteSpace(_tokenValue)) { Warn("Сначала настройте доступ GitHub отдельной кнопкой."); return; }
            if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_version.Text) || string.IsNullOrWhiteSpace(_minecraft.Text) || string.IsNullOrWhiteSpace(_loaderVersion.Text) || string.IsNullOrWhiteSpace(_address.Text) || string.IsNullOrWhiteSpace(_serverName.Text)) { Warn("Заполните все обязательные поля сборки."); return; }
            string ip; int port; if (!ServerAddressParser.TryParse(_address.Text, out ip, out port)) { Warn("Адрес должен иметь вид IP:PORT, например 213.152.43.53:25589."); return; }
            Version parsedVersion; if (!Version.TryParse(_version.Text.Trim().TrimStart('v', 'V'), out parsedVersion)) { Warn("Версия сборки должна иметь вид 0.12.7."); return; }
            if (!string.Equals(_javaVersion.Text.Trim(), "17", StringComparison.OrdinalIgnoreCase)) { Warn("Для Minecraft 1.20.1 и этой схемы запуска требуется Java 17."); return; }
            try
            {
                BuildArchiveMetadata.Read(_selectedZip); _publish.Enabled = false; _status.Text = "Создание GitHub Release…"; var sha256 = ComputeSha256(_selectedZip);
                var info = string.Join(Environment.NewLine, new[] { "# Generated by Veles Build Publisher", "BUILD_NAME=" + _name.Text.Trim(), "BUILD_VERSION=" + _version.Text.Trim(), "MINECRAFT_VERSION=" + _minecraft.Text.Trim(), "MOD_LOADER=" + _loader.SelectedItem, "MOD_LOADER_VERSION=" + _loaderVersion.Text.Trim(), "SERVER_ADDRESS=" + ip + ":" + port, "SERVER_NAME=" + _serverName.Text.Trim(), "SITE_URL=" + _site.Text.Trim(), "MOD_LOADER_PROFILE=launch.json", "JAVA_VERSION=17", "JAVA_VENDOR=BellSoft Liberica", "JAVA_RUNTIME_PATH=runtime\\java", "MEMORY_MIN=" + _memoryMin.Text.Trim(), "MEMORY_MAX=" + _memoryMax.Text.Trim(), "BUILD_SHA256=" + sha256 }) + Environment.NewLine;
                var temp = Path.Combine(Path.GetTempPath(), "veles-build-info-" + Guid.NewGuid().ToString("N") + ".txt"); File.WriteAllText(temp, info);
                var api = new GitHubClient("kutsandriy14-cyber", "veles-modpack-releases", _tokenValue); var tag = "build-" + _version.Text.Trim().TrimStart('v', 'V'); var release = await api.CreateReleaseAsync(tag, _name.Text.Trim() + " v" + _version.Text.Trim(), "Опубликовано через Veles Build Publisher.", CancellationToken.None);
                _status.Text = "Загрузка архива сборки…"; await api.UploadAssetAsync(release.UploadUrl, _selectedZip, "build.zip", null, CancellationToken.None); _status.Text = "Загрузка конфигурации…"; await api.UploadAssetAsync(release.UploadUrl, temp, "build-info.txt", null, CancellationToken.None); try { File.Delete(temp); } catch { }
                _status.Text = "Готово: релиз " + tag + " опубликован."; MessageBox.Show(this, "Сборка опубликована в GitHub Releases.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error) { _status.Text = "Ошибка публикации."; MessageBox.Show(this, error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _publish.Enabled = true; }
        }

        private void ValidateArchive() { BuildArchiveMetadata.Read(_selectedZip); }
        private void ConfigureAccess()
        {
            using (var dialog = new TokenForm()) if (dialog.ShowDialog(this) == DialogResult.OK) { _tokenValue = dialog.Token; _accessStatus.Text = "Доступ GitHub настроен только на время работы панели"; _accessStatus.ForeColor = Color.LightGreen; }
        }
        private void Warn(string message) { MessageBox.Show(this, message, "Проверьте данные", MessageBoxButtons.OK, MessageBoxIcon.Warning); _status.Text = message; }
        private static string ComputeSha256(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant(); }
    }

    internal sealed class TokenForm : Form
    {
        private readonly TextBox _token; public string Token { get { return _token.Text.Trim(); } }
        public TokenForm()
        {
            Text = "Доступ к GitHub"; Width = 560; Height = 190; StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; BackColor = Color.FromArgb(12, 10, 9); ForeColor = Color.White;
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), RowCount = 3, ColumnCount = 1, BackColor = BackColor }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
            root.Controls.Add(new Label { Text = "Служебный GitHub-токен", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(168, 162, 158), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            _token = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, BackColor = Color.FromArgb(28, 25, 23), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; root.Controls.Add(_token, 0, 1);
            var save = new Button { Text = "Использовать", Width = 120, Height = 30, Anchor = AnchorStyles.Right, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat }; save.Click += (s, e) => { if (string.IsNullOrWhiteSpace(Token)) { MessageBox.Show(this, "Введите токен.", "Доступ GitHub", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } DialogResult = DialogResult.OK; Close(); }; root.Controls.Add(save, 0, 2); AcceptButton = save;
        }
    }
}
