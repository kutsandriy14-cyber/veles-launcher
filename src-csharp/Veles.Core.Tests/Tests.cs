using System;
using System.IO;
using Veles.Core;

namespace Veles.Core.Tests
{
    internal static class Tests
    {
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        public static int Main()
        {
            var config = "\uFEFF# comment\nBUILD_NAME=Test\nBUILD_VERSION=0.12.7\nMINECRAFT_VERSION=1.20.1\nMOD_LOADER=Forge\nMOD_LOADER_VERSION=47.2.0\nSERVER_ADDRESS=213.152.43.53:25589\nJAVA_VERSION=17\nJAVA_RUNTIME_PATH=runtime\\java\nMOD_LOADER_PROFILE=launch.json\n";
            var info = BuildInfo.Parse(config); info.Validate();
            Assert(info.BuildVersion == "0.12.7", "Build version parser failed"); Assert(info.ServerAddress == "213.152.43.53:25589", "Server address parser failed"); Assert(info.JavaVersion == "17", "Java version parser failed");
            Assert(VersionComparer.Compare("0.12.7", "0.12.6") > 0, "Version comparison failed"); Assert(VersionComparer.Compare("v1.0.0", "1.0.0") == 0, "Version prefix comparison failed");
            var invalid = BuildInfo.Parse(config.Replace("SERVER_ADDRESS=213.152.43.53:25589", "SERVER_ADDRESS=213.152.43.53:70000")); var rejected = false; try { invalid.Validate(); } catch (InvalidDataException) { rejected = true; } Assert(rejected, "Invalid port was not rejected");
            var file = Path.Combine(Path.GetTempPath(), "veles-servers-" + Guid.NewGuid().ToString("N") + ".dat"); MinecraftServerFile.Write(file, "Veles PlayGame", info.ServerAddress); var bytes = File.ReadAllBytes(file); var raw = System.Text.Encoding.UTF8.GetString(bytes); Assert(raw.Contains("Veles PlayGame"), "Server name missing from NBT"); Assert(raw.Contains(info.ServerAddress), "Server address missing from NBT"); File.Delete(file);
            Console.WriteLine("PASS: parser, version comparison, validation and servers.dat"); return 0;
        }
    }
}
