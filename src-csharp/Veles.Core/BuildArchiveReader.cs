using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web.Script.Serialization;

namespace Veles.Core
{
    public sealed class BuildArchiveMetadata
    {
        public BuildInfo Info { get; private set; }
        public string MetadataFileName { get; private set; }
        public long ArchiveBytes { get; private set; }

        private BuildArchiveMetadata() { }

        public static BuildArchiveMetadata Read(string zipPath)
        {
            if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath)) throw new FileNotFoundException("Архив сборки не найден.", zipPath);
            var result = new BuildArchiveMetadata { ArchiveBytes = new FileInfo(zipPath).Length };
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                ZipArchiveEntry metadata = Find(archive, "build-info.txt");
                if (metadata != null)
                {
                    result.MetadataFileName = "build-info.txt";
                    result.Info = BuildInfo.Parse(ReadEntry(metadata));
                }
                else
                {
                    metadata = Find(archive, "build-metadata.json");
                    if (metadata == null) throw new InvalidDataException("В ZIP нет build-info.txt или build-metadata.json.");
                    result.MetadataFileName = "build-metadata.json";
                    result.Info = BuildInfo.Parse(JsonToKeyValueText(ReadEntry(metadata)));
                }
                result.Info.Validate();
                if (Find(archive, "launch.json") == null) throw new InvalidDataException("В ZIP нет launch.json с параметрами запуска Minecraft.");
                if (Find(archive, "runtime/java/bin/javaw.exe") == null && Find(archive, "runtime/java/bin/java.exe") == null) throw new InvalidDataException("В ZIP нет встроенной Java: runtime\\java\\bin\\javaw.exe.");
            }
            return result;
        }

        private static ZipArchiveEntry Find(ZipArchive archive, string path)
        {
            foreach (var entry in archive.Entries)
            {
                var normalized = (entry.FullName ?? string.Empty).Replace('\\', '/').TrimStart('/');
                if (string.Equals(normalized, path, StringComparison.OrdinalIgnoreCase)) return entry;
            }
            return null;
        }

        private static string ReadEntry(ZipArchiveEntry entry)
        {
            using (var stream = entry.Open()) using (var reader = new StreamReader(stream, Encoding.UTF8, true)) return reader.ReadToEnd();
        }

        private static string JsonToKeyValueText(string json)
        {
            var serializer = new JavaScriptSerializer();
            var values = serializer.DeserializeObject(json) as Dictionary<string, object>;
            if (values == null) throw new InvalidDataException("build-metadata.json должен содержать объект с metadata полями.");
            var text = new StringBuilder();
            foreach (var pair in values)
            {
                if (pair.Value == null || pair.Value is Dictionary<string, object> || pair.Value is object[]) continue;
                text.Append(pair.Key).Append('=').Append(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
            }
            return text.ToString();
        }
    }
}
