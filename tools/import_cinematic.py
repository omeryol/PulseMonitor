"""Gemini'nin urettigi Sinematik 3D simgelerini (128 px kare, film seridi) projeye alir.
Kullanim: python tools/import_cinematic.py <kaynak_klasor>
- Kareleri 80 px'e kucultur (dosya boyutu), Assets/WeatherAnim/Cinematic3D altina yazar.
- Dongu dikisi bozuk olan seritlerde (son kare -> ilk kare sicramasi) kareleri ileri-geri (ping-pong) siralar.
"""
import glob
import os
import sys

import numpy as np
from PIL import Image

OUT = 80
DST = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "WeatherAnim", "Cinematic3D")


def small(img):
    return img.convert("RGBa").resize((OUT, OUT), Image.LANCZOS).convert("RGBA")


def main(src):
    os.makedirs(DST, exist_ok=True)
    for f in sorted(glob.glob(os.path.join(src, "*.png"))):
        name = os.path.basename(f)
        if name.endswith(".static.png"):
            small(Image.open(f)).save(os.path.join(DST, name), optimize=True)
            continue
        strip = Image.open(f).convert("RGBA")
        h = strip.height
        n = strip.width // h
        frames = [strip.crop((i * h, 0, (i + 1) * h, h)) for i in range(n)]
        a = [np.asarray(x, dtype=np.float32) for x in frames]
        d = [np.abs(a[i] - a[(i + 1) % n]).mean() for i in range(n)]
        seam, mean = d[-1], max(float(np.mean(d[:-1])), 1e-6)
        if seam / mean > 4:
            frames = frames + frames[-2:0:-1]
            print(name, "dikis bozuk -> ileri-geri", len(frames), "kare")
        out = Image.new("RGBA", (OUT * len(frames), OUT), (0, 0, 0, 0))
        for i, fr in enumerate(frames):
            out.paste(small(fr), (i * OUT, 0))
        out.save(os.path.join(DST, name), optimize=True)


if __name__ == "__main__":
    main(sys.argv[1])
