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
        if candidate.is_file() or candidate.is_dir():
            return candidate
    raise FileNotFoundError("Не найден файл или каталог: " + " или ".join(relative_candidates))


def add_entry(archive, source, destination):
    if source.is_dir():
        for child in sorted(source.rglob("*")):
            if not child.is_file() or child.suffix.lower() == ".pdb":
                continue
            relative = child.relative_to(source).as_posix()
            archive.write(child, str(Path(destination) / relative).replace("\\", "/"))
    else:
        archive.write(source, destination)


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
            located = locate(*source) if isinstance(source, tuple) else locate(source)
            add_entry(archive, located, destination)
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
    ("src-csharp/Veles.Launcher/bin/Release", "."),
    (("src-csharp/Veles.Updater/bin/Release/VelesLauncherUpdater.exe", "src-csharp/Veles.Updater/bin/Release/Veles.Updater.exe"), "Veles.Updater.exe"),
], "VelesLauncherSetup.exe")
package("publisher", "Veles Build Publisher", version, "VelesBuildPublisher.exe", "Veles Build Publisher", [
    (("src-csharp/Veles.BuildPublisher/bin/Release/VelesBuildPublisher.exe", "src-csharp/Veles.BuildPublisher/bin/Release/Veles.BuildPublisher.exe"), "VelesBuildPublisher.exe"),
    ("src-csharp/Veles.BuildPublisher/bin/Release/Veles.Core.dll", "Veles.Core.dll"),
], "VelesBuildPublisherSetup.exe")
