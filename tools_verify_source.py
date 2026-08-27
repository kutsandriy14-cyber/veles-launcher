import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent
launcher = ROOT / "src-csharp"
required = [
    launcher / "Veles.Core" / "BuildModels.cs",
    launcher / "Veles.Core" / "BuildService.cs",
    launcher / "Veles.Core" / "JavaRuntimeService.cs",
    launcher / "Veles.Core" / "LauncherSettings.cs",
    launcher / "Veles.Core" / "GitHubClient.cs",
    launcher / "Veles.Core" / "MinecraftServerFile.cs",
    launcher / "Veles.Launcher" / "Program.cs",
    launcher / "Veles.BuildPublisher" / "Program.cs",
    launcher / "Veles.Updater" / "Program.cs",
]
for path in required:
    assert path.is_file(), f"missing {path}"

text = "\n".join(path.read_text(encoding="utf-8") for path in required)
for value in ["veles-modpack-releases", "build-info.txt", "build.zip", "BUILD_VERSION", "MINECRAFT_VERSION", "MOD_LOADER_VERSION", "SERVER_ADDRESS", "launch.json", "JAVA_RUNTIME_PATH", "VelesLauncherSetup.exe", "SHA256", "servers.dat", "Проверить и обновить сборку", "Настройки", "CheckLauncherUpdateAsync", "--auto", "--wait-pid"]:
    assert value in text, f"missing contract value: {value}"

for forbidden in ["LaunchCommand", "LAUNCH_COMMAND=", "SERVER_IP=", "SERVER_PORT="]:
    assert forbidden not in text, f"forbidden legacy value in source: {forbidden}"
assert "Process.Start(new ProcessStartInfo { FileName = \"cmd.exe\"" not in text, "launcher must not start cmd.exe"

assert not (ROOT / "package.json").exists(), "old Electron package still present"
assert not (ROOT / "src").exists(), "old Electron source directory still present"
setup = (ROOT / "installer" / "VelesLauncher.iss").read_text(encoding="utf-8")
assert "Veles.Launcher.exe" in setup and "Veles.Updater.exe" in setup and "Veles.Core.dll" in setup, "main setup must bundle launcher, updater and core DLL"
assert not (ROOT / "installer" / "VelesLauncherUpdater.iss").exists(), "separate updater setup must not exist"
assert not re.search(r"github_pat_[A-Za-z0-9_]+", text), "possible GitHub token in source"
config = (ROOT.parent / "veles-modpack-releases" / "CONFIG.example.txt").read_text(encoding="utf-8")
for key in ["BUILD_NAME", "BUILD_VERSION", "MINECRAFT_VERSION", "MOD_LOADER", "MOD_LOADER_VERSION", "SERVER_ADDRESS", "MOD_LOADER_PROFILE", "JAVA_VERSION", "JAVA_VENDOR", "JAVA_RUNTIME_PATH", "MEMORY_MIN", "MEMORY_MAX", "BUILD_SHA256"]:
    assert f"{key}=" in config, f"missing config key {key}"
for key in ["SERVER_IP", "SERVER_PORT", "LAUNCH_COMMAND"]:
    assert f"{key}=" not in config, f"legacy config key remains: {key}"

print(f"PASS: {len(required)} C# source files and release contract verified")
