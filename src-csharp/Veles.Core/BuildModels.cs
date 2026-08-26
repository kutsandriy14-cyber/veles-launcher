using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Veles.Core
{
    public sealed class BuildInfo
    {
        public string BuildName { get; set; }
        public string BuildVersion { get; set; }
        public string MinecraftVersion { get; set; }
        public string ModLoader { get; set; }
        public string ModLoaderVersion { get; set; }
        public string ServerIp { get; set; }
        public string ServerPort { get; set; }
        public string ServerName { get; set; }
        public string SiteUrl { get; set; }
        public string LaunchCommand { get; set; }
        public string ForgeJar { get; set; }
        public string MemoryMin { get; set; }
        public string MemoryMax { get; set; }

        public string ServerAddress
        {
            get { return string.Format("{0}:{1}", ServerIp ?? string.Empty, ServerPort ?? string.Empty); }
        }

        public static BuildInfo Parse(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StringReader(text ?? string.Empty))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
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
                ModLoaderVersion = Get(values, "MOD_LOADER_VERSION"), ServerIp = Get(values, "SERVER_IP"),
                ServerPort = Get(values, "SERVER_PORT"), ServerName = Get(values, "SERVER_NAME"),
                SiteUrl = Get(values, "SITE_URL"), LaunchCommand = Get(values, "LAUNCH_COMMAND"),
                ForgeJar = Get(values, "FORGE_JAR"), MemoryMin = Get(values, "MEMORY_MIN"), MemoryMax = Get(values, "MEMORY_MAX")
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
            if (string.IsNullOrWhiteSpace(ServerIp)) missing.Add("SERVER_IP");
            if (string.IsNullOrWhiteSpace(ServerPort)) missing.Add("SERVER_PORT");
            if (missing.Count > 0) throw new InvalidDataException("В build-info.txt отсутствуют поля: " + string.Join(", ", missing));
        }

        private static string Get(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }
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
        public string TagName { get; set; }
        public string Name { get; set; }
        public string HtmlUrl { get; set; }
        public string UploadUrl { get; set; }
        public List<ReleaseAsset> Assets { get; set; }
    }

    public sealed class ReleaseAsset
    {
        public string Name { get; set; }
        public string BrowserDownloadUrl { get; set; }
        public string UploadUrl { get; set; }
    }
}
