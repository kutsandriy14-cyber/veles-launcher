using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Veles.Core
{
    public sealed class BuildInfo
    {
        public string BuildName { get; set; }
        public string BuildVersion { get; set; }
        public string MinecraftVersion { get; set; }
        public string ModLoader { get; set; }
        public string ModLoaderVersion { get; set; }
        public string ServerAddressValue { get; set; }
        public string ServerName { get; set; }
        public string SiteUrl { get; set; }
        public string ModLoaderProfile { get; set; }
        public string JavaVersion { get; set; }
        public string JavaVendor { get; set; }
        public string JavaRuntimePath { get; set; }
        public string JavaRuntimeSha256 { get; set; }
        public string MemoryMin { get; set; }
        public string MemoryMax { get; set; }
        public string BuildSha256 { get; set; }

        public string ServerAddress
        {
            get { return ServerAddressValue ?? string.Empty; }
        }

        public static BuildInfo Parse(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StringReader(text ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim().TrimStart('\uFEFF');
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var separator = line.IndexOf('=');
                    if (separator <= 0) continue;
                    values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
                }
            }
            return new BuildInfo
            {
                BuildName = Get(values, "BUILD_NAME"), BuildVersion = Get(values, "BUILD_VERSION"),
                MinecraftVersion = Get(values, "MINECRAFT_VERSION"), ModLoader = Get(values, "MOD_LOADER"),
                ModLoaderVersion = Get(values, "MOD_LOADER_VERSION"), ServerAddressValue = Get(values, "SERVER_ADDRESS"), ServerName = Get(values, "SERVER_NAME"),
                SiteUrl = Get(values, "SITE_URL"), ModLoaderProfile = Get(values, "MOD_LOADER_PROFILE"),
                JavaVersion = Get(values, "JAVA_VERSION"), JavaVendor = Get(values, "JAVA_VENDOR"), JavaRuntimePath = Get(values, "JAVA_RUNTIME_PATH"), JavaRuntimeSha256 = Get(values, "JAVA_RUNTIME_SHA256"),
                MemoryMin = Get(values, "MEMORY_MIN"), MemoryMax = Get(values, "MEMORY_MAX"), BuildSha256 = Get(values, "BUILD_SHA256")
            };
        }

        public void Validate()
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(BuildName)) missing.Add("BUILD_NAME");
            if (string.IsNullOrWhiteSpace(BuildVersion)) missing.Add("BUILD_VERSION");
            if (string.IsNullOrWhiteSpace(MinecraftVersion)) missing.Add("MINECRAFT_VERSION");
            if (string.IsNullOrWhiteSpace(ModLoader)) missing.Add("MOD_LOADER");
            if (string.IsNullOrWhiteSpace(ModLoaderVersion)) missing.Add("MOD_LOADER_VERSION");
            if (string.IsNullOrWhiteSpace(ServerAddressValue)) missing.Add("SERVER_ADDRESS");
            if (string.IsNullOrWhiteSpace(JavaVersion)) missing.Add("JAVA_VERSION");
            if (string.IsNullOrWhiteSpace(JavaVendor)) missing.Add("JAVA_VENDOR");
            if (string.IsNullOrWhiteSpace(JavaRuntimePath)) missing.Add("JAVA_RUNTIME_PATH");
            if (!string.IsNullOrWhiteSpace(JavaVendor) && !string.Equals(JavaVendor, "BellSoft Liberica", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("JAVA_VENDOR должен быть BellSoft Liberica.");
            if (!string.Equals(JavaVersion, "17", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Для этой серверной сборки требуется Java 17.");
            if (missing.Count > 0) throw new InvalidDataException("В build-info.txt отсутствуют поля: " + string.Join(", ", missing));
            string parsedIp; int parsedPort;
            if (!ServerAddressParser.TryParse(ServerAddressValue, out parsedIp, out parsedPort)) throw new InvalidDataException("SERVER_ADDRESS должен иметь вид IP:PORT, например 213.152.43.53:25589.");
            ServerAddressValue = string.Format("{0}:{1}", parsedIp, parsedPort);
            if (string.IsNullOrWhiteSpace(ModLoaderProfile)) ModLoaderProfile = "launch.json";
            if (!string.Equals(ModLoaderProfile, "launch.json", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("MOD_LOADER_PROFILE должен быть launch.json.");
            var supportedLoaders = new[] { "Forge", "Fabric", "Quilt", "NeoForge" }; if (!supportedLoaders.Any(x => string.Equals(x, ModLoader, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("MOD_LOADER должен быть Forge, Fabric, Quilt или NeoForge.");
        }

        private static string Get(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }
    }

    public static class ServerAddressParser
    {
        public static bool TryParse(string value, out string ip, out int port)
        {
            ip = string.Empty; port = 0; var text = (value ?? string.Empty).Trim();
            if (text.StartsWith("[") && text.IndexOf("]:", StringComparison.Ordinal) > 0) { var end = text.IndexOf("]:", StringComparison.Ordinal); ip = text.Substring(1, end - 1); return int.TryParse(text.Substring(end + 2), out port) && port > 0 && port <= 65535 && ip.Length > 0; }
            var separator = text.LastIndexOf(':'); if (separator <= 0 || separator == text.Length - 1) return false;
            ip = text.Substring(0, separator).Trim(); return int.TryParse(text.Substring(separator + 1).Trim(), out port) && port > 0 && port <= 65535 && (ip.IndexOf('.') >= 0 || ip.IndexOf(':') >= 0 || ip.Equals("localhost", StringComparison.OrdinalIgnoreCase));
        }
    }

    public sealed class LaunchProfile
    {
        public string JavaPath { get; set; }
        public string MainClass { get; set; }
        public string ClassPath { get; set; }
        public string JvmArguments { get; set; }
        public string GameArguments { get; set; }

        public static LaunchProfile Load(string path)
        {
            if (!File.Exists(path)) throw new InvalidDataException("В архиве нет launch.json. Добавьте профиль запуска сборки.");
            var json = new System.Web.Script.Serialization.JavaScriptSerializer(); var values = json.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            var profile = new LaunchProfile { JavaPath = Get(values, "javaPath"), MainClass = Get(values, "mainClass"), ClassPath = Get(values, "classPath"), JvmArguments = Get(values, "jvmArguments"), GameArguments = Get(values, "gameArguments") };
            if (string.IsNullOrWhiteSpace(profile.MainClass) || string.IsNullOrWhiteSpace(profile.ClassPath)) throw new InvalidDataException("launch.json должен содержать mainClass и classPath.");
            if (!Regex.IsMatch(profile.MainClass, "^[A-Za-z_$][A-Za-z0-9_$.]*$")) throw new InvalidDataException("mainClass в launch.json имеет недопустимый формат.");
            var allArguments = (profile.ClassPath + " " + profile.JvmArguments + " " + profile.GameArguments).ToLowerInvariant();
            foreach (var forbidden in new[] { "cmd.exe", "powershell", "start.bat", ".bat", "&&", "||", "|" }) if (allArguments.Contains(forbidden)) throw new InvalidDataException("launch.json содержит запрещённую команду или оболочку.");
            return profile;
        }

        private static string Get(Dictionary<string, object> values, string key) { object value; return values != null && values.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : string.Empty; }
    }

    public sealed class InstalledBuild
    {
        public string Version { get; set; }
        public string Name { get; set; }
        public string Minecraft { get; set; }
        public string ModLoader { get; set; }
        public string ModLoaderVersion { get; set; }
        public string Server { get; set; }
        public DateTime InstalledAtUtc { get; set; }
    }

    public sealed class ReleaseInfo
    {
        public string Id { get; set; }
        public string TagName { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public string HtmlUrl { get; set; }
        public string UploadUrl { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public List<ReleaseAsset> Assets { get; set; }
    }

    public sealed class ReleaseAsset
    {
        public string Name { get; set; }
        public string BrowserDownloadUrl { get; set; }
        public string UploadUrl { get; set; }
    }
}
