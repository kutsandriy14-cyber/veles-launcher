using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Veles.Core
{
    public sealed class BuildSnapshot
    {
        public ReleaseInfo Release { get; set; }
        public BuildInfo Info { get; set; }
        public ReleaseAsset Archive { get; set; }
        public ReleaseAsset Config { get; set; }
    }

    public sealed class BuildService
    {
        public const string Owner = "kutsandriy14-cyber";
        public const string BuildsRepository = "veles-modpack-releases";
        private readonly GitHubClient _github = new GitHubClient(Owner, BuildsRepository);
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private readonly HttpClient _http = new HttpClient();
        public LauncherSettings Settings { get; private set; }

        public string AppDataDirectory { get; private set; }
        public string InstanceDirectory { get { return Settings == null ? Path.Combine(AppDataDirectory, "instances", "veles") : Settings.InstanceDirectory; } }
        public string MetadataPath { get { return Path.Combine(AppDataDirectory, "installed-build.json"); } }

        public BuildService()
        {
            AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Veles Launcher");
            Directory.CreateDirectory(AppDataDirectory); Settings = LauncherSettings.Load(AppDataDirectory);
        }

        public void SaveSettings(LauncherSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            settings.Save(AppDataDirectory); Settings = settings; Directory.CreateDirectory(InstanceDirectory);
        }

        public async Task<BuildSnapshot> GetLatestAsync(CancellationToken cancellationToken)
        {
            var release = await _github.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
            var configAsset = FindAsset(release, "build-info.txt");
            var archiveAsset = FindAsset(release, "build.zip");
            if (configAsset == null || archiveAsset == null) throw new InvalidDataException("Последний релиз не содержит build-info.txt и build.zip.");
            var text = await _http.GetStringAsync(configAsset.BrowserDownloadUrl).ConfigureAwait(false);
            var info = BuildInfo.Parse(text); info.Validate();
            return new BuildSnapshot { Release = release, Info = info, Archive = archiveAsset, Config = configAsset };
        }

        public InstalledBuild ReadInstalled()
        {
            try { return _json.Deserialize<InstalledBuild>(File.ReadAllText(MetadataPath)); } catch { return null; }
        }

        public bool NeedsUpdate(BuildSnapshot latest)
        {
            var installed = ReadInstalled();
            return installed == null || VersionComparer.Compare(installed.Version, latest.Info.BuildVersion) < 0;
        }

        public async Task InstallAsync(BuildSnapshot latest, IProgress<int> progress, CancellationToken cancellationToken)
        {
            var temp = Path.Combine(Path.GetTempPath(), "veles-build-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);
            try
            {
                var zipPath = Path.Combine(temp, "build.zip");
                await DownloadFileAsync(latest.Archive.BrowserDownloadUrl, zipPath, progress, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(latest.Info.BuildSha256))
                {
                    using (var sha = SHA256.Create()) using (var file = File.OpenRead(zipPath))
                    {
                        var actual = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", string.Empty).ToLowerInvariant();
                        if (!string.Equals(actual, latest.Info.BuildSha256.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 build.zip не совпадает с build-info.txt.");
                    }
                }
                var staging = Path.Combine(temp, "instance");
                Directory.CreateDirectory(staging);
                ZipFile.ExtractToDirectory(zipPath, staging);
                ValidateExtractedInstance(staging, latest.Info);
                var backup = InstanceDirectory + ".backup-" + Guid.NewGuid().ToString("N");
                if (Directory.Exists(InstanceDirectory)) Directory.Move(InstanceDirectory, backup);
                try
                {
                    Directory.Move(staging, InstanceDirectory);
                    MinecraftServerFile.Write(Path.Combine(InstanceDirectory, "servers.dat"), latest.Info.ServerName ?? "Veles PlayGame", latest.Info.ServerAddress);
                    if (Directory.Exists(backup)) Directory.Delete(backup, true);
                }
                catch
                {
                    if (Directory.Exists(InstanceDirectory)) Directory.Delete(InstanceDirectory, true);
                    if (Directory.Exists(backup)) Directory.Move(backup, InstanceDirectory);
                    throw;
                }
                var installed = new InstalledBuild { Version = latest.Info.BuildVersion, Name = latest.Info.BuildName, Minecraft = latest.Info.MinecraftVersion, ModLoader = latest.Info.ModLoader, ModLoaderVersion = latest.Info.ModLoaderVersion, Server = latest.Info.ServerAddress, InstalledAtUtc = DateTime.UtcNow };
                File.WriteAllText(MetadataPath, _json.Serialize(installed));
            }
            finally { try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { } }
        }

        private async Task DownloadFileAsync(string url, string target, IProgress<int> progress, CancellationToken cancellationToken)
        {
            using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength ?? 0;
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = File.Create(target))
                {
                    var buffer = new byte[1024 * 1024]; long read = 0; int count;
                    while ((count = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    { await output.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false); read += count; if (progress != null && total > 0) progress.Report((int)(read * 100L / total)); }
                }
            }
        }

        private static void ValidateExtractedInstance(string staging, BuildInfo info)
        {
            var profileName = string.IsNullOrWhiteSpace(info.ModLoaderProfile) ? "launch.json" : info.ModLoaderProfile;
            var root = Path.GetFullPath(staging).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var profilePath = Path.GetFullPath(Path.Combine(staging, profileName));
            if (!profilePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Профиль запуска выходит за пределы архива.");
            LaunchProfile.Load(profilePath);
            JavaRuntimeService.ResolveJavaExecutable(staging, info);
        }

        private static ReleaseAsset FindAsset(ReleaseInfo release, string name)
        {
            foreach (var asset in release.Assets) if (string.Equals(asset.Name, name, StringComparison.OrdinalIgnoreCase)) return asset;
            return null;
        }
    }

    public static class VersionComparer
    {
        public static int Compare(string left, string right)
        {
            var a = (left ?? "0").TrimStart('v', 'V').Split('.', '-', '+');
            var b = (right ?? "0").TrimStart('v', 'V').Split('.', '-', '+');
            for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                int x, y; int.TryParse(i < a.Length ? a[i] : "0", out x); int.TryParse(i < b.Length ? b[i] : "0", out y);
                if (x != y) return x > y ? 1 : -1;
            }
            return 0;
        }
    }
}
