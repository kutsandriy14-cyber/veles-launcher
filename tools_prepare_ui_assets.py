from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parent
for folder in (root / "assets" / "ui-icons", root / "src-csharp" / "Veles.Launcher" / "Assets" / "ui-icons"):
    folder.mkdir(parents=True, exist_ok=True)
    for source in folder.glob("*.png"):
        with Image.open(source).convert("RGBA") as image:
            resized = image.resize((64, 64), Image.Resampling.LANCZOS)
            resized.save(source, format="PNG", optimize=True)
            print(f"optimized {source} -> {source.stat().st_size} bytes")
