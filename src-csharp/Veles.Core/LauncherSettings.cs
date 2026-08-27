using System;
using System.IO;
using System.Web.Script.Serialization;

namespace Veles.Core
{
    public sealed class LauncherSettings
    {
        public string InstanceDirectory { get; set; }
        public int MinimumMemoryMb { get; set; }
        public int MaximumMemoryMb { get; set; }

        public static LauncherSettings Load(string appDataDirectory)
        {
            var path = GetPath(appDataDirectory);
            try
            {
                var settings = new JavaScriptSerializer().Deserialize<LauncherSettings>(File.ReadAllText(path));
                if (settings != null) return Normalize(settings, appDataDirectory);
            }
            catch { }
            return Normalize(new LauncherSettings(), appDataDirectory);
        }

        public void Save(string appDataDirectory)
        {
            Normalize(this, appDataDirectory);
            Directory.CreateDirectory(appDataDirectory);
            File.WriteAllText(GetPath(appDataDirectory), new JavaScriptSerializer().Serialize(this));
        }

        public static string GetPath(string appDataDirectory) { return Path.Combine(appDataDirectory, "settings.json"); }

        private static LauncherSettings Normalize(LauncherSettings settings, string appDataDirectory)
        {
            if (string.IsNullOrWhiteSpace(settings.InstanceDirectory)) settings.InstanceDirectory = Path.Combine(appDataDirectory, "instances", "veles");
            settings.InstanceDirectory = Path.GetFullPath(Environment.ExpandEnvironmentVariables(settings.InstanceDirectory));
            if (settings.MinimumMemoryMb < 1024) settings.MinimumMemoryMb = 2048;
            if (settings.MaximumMemoryMb < settings.MinimumMemoryMb) settings.MaximumMemoryMb = Math.Max(6144, settings.MinimumMemoryMb);
            if (settings.MaximumMemoryMb > 65536) settings.MaximumMemoryMb = 65536;
            return settings;
        }
    }
}
