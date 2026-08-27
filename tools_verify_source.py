import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent
launcher = ROOT / "src-csharp"
required = [
    launcher / "Veles.Core" / "BuildModels.cs",
    launcher / "Veles.Core" / "BuildService.cs",
    launcher / "Veles.Core" / "GitHubClient.cs",
    launcher / "Veles.Core" / "MinecraftServerFile.cs",
    launcher / "Veles.Launcher" / "Program.cs",
    launcher / "Veles.BuildPublisher" / "Program.cs",
    launcher / "Veles.Updater" / "Program.cs",
]
for path in required:
    assert path.is_file(), f"missing {path}"

text = "\n".join(path.read_text(encoding="utf-8") for path in required)
for value in ["veles-modpack-releases", "build-info.txt", "build.zip", "BUILD_VERSION", "MINECRAFT_VERSION", "MOD_LOADER_VERSION", "SERVER_IP", "SERVER_PORT", "VelesLauncherSetup.exe", "SHA256", "servers.dat", "НЕОФИЦИАЛЬНЫЙ СЕРВЕР", "Проверить и обновить сборку"]:
    assert value in text, f"missing contract value: {value}"

assert not (ROOT / "package.json").exists(), "old Electron package still present"
assert not (ROOT / "src").exists(), "old Electron source directory still present"
setup = (ROOT / "installer" / "VelesLauncher.iss").read_text(encoding="utf-8")
assert "Veles.Launcher.exe" in setup and "Veles.Updater.exe" in setup and "Veles.Core.dll" in setup, "main setup must bundle launcher, updater and core DLL"
assert not (ROOT / "installer" / "VelesLauncherUpdater.iss").exists(), "separate updater setup must not exist"
assert not re.search(r"github_pat_[A-Za-z0-9_]+", text), "possible GitHub token in source"
config = (ROOT.parent / "veles-modpack-releases" / "CONFIG.example.txt").read_text(encoding="utf-8")
for key in ["BUILD_NAME", "BUILD_VERSION", "MINECRAFT_VERSION", "MOD_LOADER", "MOD_LOADER_VERSION", "SERVER_IP", "SERVER_PORT", "LAUNCH_COMMAND", "BUILD_SHA256"]:
    assert f"{key}=" in config, f"missing config key {key}"

print(f"PASS: {len(required)} C# source files and release contract verified")
