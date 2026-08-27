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
        private readonly Color Orange = Color.FromArgb(249, 115, 22);
        private readonly Color Background = Color.FromArgb(12, 10, 9);
        private readonly Color Card = Color.FromArgb(28, 25, 23);
        private readonly Color TextColor = Color.FromArgb(250, 250, 249);
        private readonly Color Muted = Color.FromArgb(168, 162, 158);

        public PublisherForm()
        {
            Text = "Veles Build Publisher"; Width = 920; Height = 820; MinimumSize = new Size(820, 700); BackColor = Background; ForeColor = TextColor; StartPosition = FormStartPosition.CenterScreen; BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 2, RowCount = 12, BackColor = Background, AutoScroll = true };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
            for (var i = 3; i < 10; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            Controls.Add(root);
            var title = Label("V  VELES BUILD PUBLISHER", 16, FontStyle.Bold, TextColor); root.Controls.Add(title, 0, 0); root.SetColumnSpan(title, 2);
            var repo = Label("Публикация в kutsandriy14-cyber/veles-modpack-releases", 9, FontStyle.Regular, Muted); root.Controls.Add(repo, 0, 1); root.SetColumnSpan(repo, 2);
            var filePanel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(14) }; filePanel.Controls.Add(Label("Архив сборки (.zip)", 9, FontStyle.Regular, Muted)); _zip = TextBoxControl("", true); _zip.Location = new Point(0, 24); _zip.Width = 590; filePanel.Controls.Add(_zip); var choose = Button("Выбрать ZIP", Orange, Color.Black); choose.Location = new Point(610, 22); choose.Width = 150; choose.Click += (s, e) => ChooseZip(); filePanel.Controls.Add(choose); root.Controls.Add(filePanel, 0, 2); root.SetColumnSpan(filePanel, 2);
            AddPair(root, 3, "Название сборки", _name = TextBoxControl("", false), "Версия сборки", _version = TextBoxControl("", false));
            AddPair(root, 4, "Версия Minecraft", _minecraft = TextBoxControl("", false), "Модлоадер", _loader = LoaderControl());
            AddPair(root, 5, "Версия модлоадера", _loaderVersion = TextBoxControl("", false), "Адрес сервера (IP:PORT)", _address = TextBoxControl("", false));
            AddPair(root, 6, "Название сервера", _serverName = TextBoxControl("", false), "Сайт сервера", _site = TextBoxControl("https://veles-saite.vercel.app/", false));
            AddPair(root, 7, "Версия Java", _javaVersion = TextBoxControl("17", true), "Папка Java в ZIP", _javaPath = TextBoxControl("runtime\\java", true));
            AddPair(root, 8, "Память от", _memoryMin = TextBoxControl("2G", false), "Память до", _memoryMax = TextBoxControl("6G", false));
            var accessPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) }; _accessStatus = Label("Доступ GitHub не настроен", 9, FontStyle.Regular, Muted); _accessStatus.Location = new Point(0, 7); accessPanel.Controls.Add(_accessStatus); var access = Button("Настроить доступ", Color.FromArgb(43, 28, 20), TextColor); access.Location = new Point(210, 2); access.Width = 150; access.Click += (s, e) => ConfigureAccess(); accessPanel.Controls.Add(access); root.Controls.Add(accessPanel, 0, 9); root.SetColumnSpan(accessPanel, 2);
            _publish = Button("Опубликовать сборку", Orange, Color.Black); _publish.Dock = DockStyle.Fill; _publish.Height = 42; _publish.Click += async (s, e) => await PublishAsync(); root.Controls.Add(_publish, 0, 10); root.SetColumnSpan(_publish, 2);
            _status = Label("Готово. Выберите ZIP и заполните параметры сборки.", 9, FontStyle.Regular, Muted); _status.AutoSize = false; _status.Dock = DockStyle.Fill; root.Controls.Add(_status, 0, 11); root.SetColumnSpan(_status, 2);
        }

        private void AddPair(TableLayoutPanel root, int row, string leftCaption, Control left, string rightCaption, Control right)
        {
            var leftPanel = FieldPanel(leftCaption, left); var rightPanel = FieldPanel(rightCaption, right); root.Controls.Add(leftPanel, 0, row); root.Controls.Add(rightPanel, 1, row);
        }
        private Panel FieldPanel(string caption, Control input) { var panel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Padding = new Padding(12, 7, 12, 4) }; panel.Controls.Add(Label(caption, 9, FontStyle.Regular, Muted)); input.Location = new Point(0, 25); input.Width = 350; input.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top; panel.Controls.Add(input); return panel; }
        private ComboBox LoaderControl() { var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(15, 12, 11), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; box.Items.AddRange(new object[] { "Forge", "Fabric", "Quilt", "NeoForge" }); box.SelectedIndex = 0; return box; }
        private TextBox TextBoxControl(string value, bool readOnly) { return new TextBox { Text = value, ReadOnly = readOnly, BackColor = Color.FromArgb(15, 12, 11), ForeColor = TextColor, BorderStyle = BorderStyle.FixedSingle, Height = 28 }; }
        private Label Label(string text, float size, FontStyle style, Color color) { return new Label { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, style), ForeColor = color }; }
        private Button Button(string text, Color background, Color foreground) { return new Button { Text = text, BackColor = background, ForeColor = foreground, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) }; }

        private void ChooseZip()
        {
            using (var dialog = new OpenFileDialog { Filter = "ZIP archive (*.zip)|*.zip", Title = "Выберите архив сборки" }) if (dialog.ShowDialog(this) == DialogResult.OK) { _selectedZip = dialog.FileName; _zip.Text = Path.GetFileName(_selectedZip); _status.Text = "Архив выбран. Обязательные файлы будут проверены перед публикацией."; }
        }

        private async Task PublishAsync()
        {
            if (string.IsNullOrWhiteSpace(_selectedZip)) { Warn("Выберите ZIP-архив сборки."); return; }
            if (string.IsNullOrWhiteSpace(_tokenValue)) { Warn("Сначала настройте доступ GitHub отдельной кнопкой."); return; }
            if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_version.Text) || string.IsNullOrWhiteSpace(_minecraft.Text) || string.IsNullOrWhiteSpace(_loaderVersion.Text) || string.IsNullOrWhiteSpace(_address.Text) || string.IsNullOrWhiteSpace(_serverName.Text)) { Warn("Заполните все обязательные поля сборки."); return; }
            string ip; int port; if (!ServerAddressParser.TryParse(_address.Text, out ip, out port)) { Warn("Адрес должен иметь вид IP:PORT, например 213.152.43.53:25589."); return; }
            Version parsedVersion; if (!Version.TryParse(_version.Text.Trim().TrimStart('v', 'V'), out parsedVersion)) { Warn("Версия сборки должна иметь вид 0.12.7."); return; }
            if (!string.Equals(_javaVersion.Text.Trim(), "17", StringComparison.OrdinalIgnoreCase)) { Warn("Для Minecraft 1.20.1 и этой схемы запуска требуется Java 17."); return; }
            try
            {
                ValidateArchive(); _publish.Enabled = false; _status.Text = "Создание GitHub Release…"; var sha256 = ComputeSha256(_selectedZip);
                var info = string.Join(Environment.NewLine, new[] { "# Generated by Veles Build Publisher", "BUILD_NAME=" + _name.Text.Trim(), "BUILD_VERSION=" + _version.Text.Trim(), "MINECRAFT_VERSION=" + _minecraft.Text.Trim(), "MOD_LOADER=" + _loader.SelectedItem, "MOD_LOADER_VERSION=" + _loaderVersion.Text.Trim(), "SERVER_ADDRESS=" + ip + ":" + port, "SERVER_NAME=" + _serverName.Text.Trim(), "SITE_URL=" + _site.Text.Trim(), "MOD_LOADER_PROFILE=launch.json", "JAVA_VERSION=17", "JAVA_VENDOR=BellSoft Liberica", "JAVA_RUNTIME_PATH=runtime\\java", "MEMORY_MIN=" + _memoryMin.Text.Trim(), "MEMORY_MAX=" + _memoryMax.Text.Trim(), "BUILD_SHA256=" + sha256 }) + Environment.NewLine;
                var temp = Path.Combine(Path.GetTempPath(), "veles-build-info-" + Guid.NewGuid().ToString("N") + ".txt"); File.WriteAllText(temp, info);
                var api = new GitHubClient("kutsandriy14-cyber", "veles-modpack-releases", _tokenValue); var tag = "build-" + _version.Text.Trim().TrimStart('v', 'V'); var release = await api.CreateReleaseAsync(tag, _name.Text.Trim() + " v" + _version.Text.Trim(), "Опубликовано через Veles Build Publisher.", CancellationToken.None);
                _status.Text = "Загрузка архива сборки…"; await api.UploadAssetAsync(release.UploadUrl, _selectedZip, "build.zip", null, CancellationToken.None); _status.Text = "Загрузка конфигурации…"; await api.UploadAssetAsync(release.UploadUrl, temp, "build-info.txt", null, CancellationToken.None); try { File.Delete(temp); } catch { }
                _status.Text = "Готово: релиз " + tag + " опубликован."; MessageBox.Show(this, "Сборка опубликована в GitHub Releases.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error) { _status.Text = "Ошибка публикации."; MessageBox.Show(this, error.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { _publish.Enabled = true; }
        }

        private void ValidateArchive()
        {
            using (var archive = ZipFile.OpenRead(_selectedZip))
            {
                var hasProfile = false; var hasJava = false;
                foreach (var entry in archive.Entries)
                {
                    var path = entry.FullName.Replace('\\', '/').TrimStart('/');
                    if (string.Equals(path, "launch.json", StringComparison.OrdinalIgnoreCase)) hasProfile = true;
                    if (string.Equals(path, "runtime/java/bin/javaw.exe", StringComparison.OrdinalIgnoreCase) || string.Equals(path, "runtime/java/bin/java.exe", StringComparison.OrdinalIgnoreCase)) hasJava = true;
                }
                if (!hasProfile) throw new InvalidDataException("В ZIP нет launch.json с параметрами запуска Minecraft.");
                if (!hasJava) throw new InvalidDataException("В ZIP нет встроенной Java: runtime\\java\\bin\\javaw.exe.");
            }
        }
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
