from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent
source_dir = root / "assets"
out_dir = source_dir / "icons"
out_dir.mkdir(parents=True, exist_ok=True)
sizes = (16, 24, 32, 48, 64, 128, 256)
for name in ("veles-launcher-icon", "veles-publisher-icon", "veles-setup-icon"):
    source = Image.open(source_dir / f"{name}.png").convert("RGBA")
    output = out_dir / f"{name}.ico"
    source.save(output, format="ICO", sizes=[(size, size) for size in sizes])
    print(f"created {output} sizes={sizes}")
