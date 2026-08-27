using System;
using System.IO;
using System.Security.Cryptography;

namespace Veles.Core
{
    public static class JavaRuntimeService
    {
        public static string ResolveJavaExecutable(string instanceDirectory, BuildInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.JavaRuntimePath)) throw new InvalidDataException("В конфигурации не указан встроенный Java runtime.");
            var root = Path.GetFullPath(instanceDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var runtime = Path.GetFullPath(Path.Combine(instanceDirectory, info.JavaRuntimePath));
            if (!runtime.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("JAVA_RUNTIME_PATH выходит за пределы сборки.");
            var javaw = Path.Combine(runtime, "bin", "javaw.exe"); var java = Path.Combine(runtime, "bin", "java.exe");
            if (File.Exists(javaw)) return javaw;
            if (File.Exists(java)) return java;
            throw new FileNotFoundException("В сборке не найден встроенный Java runtime. Ожидался файл runtime\\java\\bin\\javaw.exe.");
        }

        public static void VerifySha256(string filePath, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected)) return;
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(filePath))
            {
                var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                if (!string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 встроенной Java не совпадает с конфигурацией.");
            }
        }
    }
}
