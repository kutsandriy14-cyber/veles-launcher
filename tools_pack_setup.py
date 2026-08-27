import json
import shutil
import struct
import sys
import zipfile
from pathlib import Path

MARKER = b"VELES_PAYLOAD_V1\0"
ROOT = Path(__file__).resolve().parent


def locate(*relative_candidates):
    for relative in relative_candidates:
        candidate = ROOT / relative
        if candidate.is_file():
            return candidate
    raise FileNotFoundError("Не найден бинарник: " + " или ".join(relative_candidates))


def package(product_id, display_name, version, target_executable, shortcut_name, files, output_name):
    base = ROOT / "src-csharp" / "Veles.Setup" / "bin" / "Release" / "Veles.Setup.exe"
    output = ROOT / "artifacts" / "setup" / output_name
    output.parent.mkdir(parents=True, exist_ok=True)
    temp = ROOT / (".payload-" + product_id + ".zip")
    manifest = {
        "ProductId": product_id,
        "DisplayName": display_name,
        "Version": version,
        "TargetExecutable": target_executable,
        "InstallDirectory": "",
        "ShortcutName": shortcut_name,
        "StartAfterInstall": product_id == "launcher",
    }
    with zipfile.ZipFile(temp, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("setup.json", json.dumps(manifest, ensure_ascii=False, separators=(",", ":")))
        for source, destination in files:
            archive.write(locate(*source) if isinstance(source, tuple) else locate(source), destination)
    shutil.copyfile(base, output)
    payload = temp.read_bytes()
    with output.open("ab") as stream:
        stream.write(MARKER)
        stream.write(struct.pack("<q", len(payload)))
        stream.write(payload)
    temp.unlink()
    print(f"created {output} ({output.stat().st_size} bytes)")


version = sys.argv[1] if len(sys.argv) > 1 else "0.1.8"
package("launcher", "Veles Launcher", version, "VelesLauncher.exe", "Veles Launcher", [
    (("src-csharp/Veles.Launcher/bin/Release/VelesLauncher.exe", "src-csharp/Veles.Launcher/bin/Release/Veles.Launcher.exe"), "VelesLauncher.exe"),
    ("src-csharp/Veles.Launcher/bin/Release/Veles.Core.dll", "Veles.Core.dll"),
    (("src-csharp/Veles.Updater/bin/Release/VelesLauncherUpdater.exe", "src-csharp/Veles.Updater/bin/Release/Veles.Updater.exe"), "Veles.Updater.exe"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/server.png", "Assets/ui-icons/server.png"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/build.png", "Assets/ui-icons/build.png"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/update.png", "Assets/ui-icons/update.png"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/play.png", "Assets/ui-icons/play.png"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/settings.png", "Assets/ui-icons/settings.png"),
    ("src-csharp/Veles.Launcher/Assets/ui-icons/wifi.png", "Assets/ui-icons/wifi.png"),
    ("src-csharp/Veles.Launcher/WebUi/index.html", "WebUi/index.html"),
    ("src-csharp/Veles.Launcher/WebUi/main.js", "WebUi/main.js"),
    ("src-csharp/Veles.Launcher/WebUi/styles.css", "WebUi/styles.css"),
], "VelesLauncherSetup.exe")
package("publisher", "Veles Build Publisher", version, "VelesBuildPublisher.exe", "Veles Build Publisher", [
    (("src-csharp/Veles.BuildPublisher/bin/Release/VelesBuildPublisher.exe", "src-csharp/Veles.BuildPublisher/bin/Release/Veles.BuildPublisher.exe"), "VelesBuildPublisher.exe"),
    ("src-csharp/Veles.BuildPublisher/bin/Release/Veles.Core.dll", "Veles.Core.dll"),
], "VelesBuildPublisherSetup.exe")
