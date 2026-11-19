"""Sinematik 3D simgelerinden film seridi uretir (Assets/WeatherAnim/Cinematic3D).
Kaynak: Assets/Weather/Cinematic3D/<simge>.png (128x128). Cikti: <simge>.png (20 kare yan yana, 64 px) ve <simge>.static.png.
Calistir: python tools/render_cinematic.py   (Pillow + numpy gerekir). Katmanlar yoktur; hareket piksel maskeleriyle turetilir.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Assets", "Weather", "Cinematic3D")
DST = os.path.join(ROOT, "Assets", "WeatherAnim", "Cinematic3D")
N, SIZE = 20, 64


def load(name):
    return Image.open(os.path.join(SRC, name + ".png")).convert("RGBA")


def shift(img, dx, dy):
    """Alt piksel kaydirma (alfa carpilmis uzayda, kenar saydam)."""
    pm = img.convert("RGBa")
    out = pm.transform(pm.size, Image.AFFINE, (1, 0, -dx, 0, 1, -dy), resample=Image.BICUBIC)
    return out.convert("RGBA")


def affine(img, angle=0.0, scale=1.0, dx=0.0, dy=0.0):
    w, h = img.size
    c = (w / 2, h / 2)
    a = math.radians(angle)
    cos, sin = math.cos(a) / scale, math.sin(a) / scale
    # ters donusum: cikti noktasindan kaynaga
    tx = c[0] - cos * (c[0] + dx) - sin * (c[1] + dy)
    ty = c[1] + sin * (c[0] + dx) - cos * (c[1] + dy)
    pm = img.convert("RGBa")
    out = pm.transform(pm.size, Image.AFFINE, (cos, sin, tx, -sin, cos, ty), resample=Image.BICUBIC)
    return out.convert("RGBA")


def arr(img):
    return np.asarray(img, dtype=np.float32)


def to_img(a):
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def over(bottom, top):
    out = bottom.copy()
    out.alpha_composite(top)
    return out


def blur_mask(m, radius):
    im = Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(radius))
    return np.asarray(im, dtype=np.float32) / 255.0


def glow(mask, color, radius, strength):
    a = blur_mask(mask, radius) * strength
    out = np.zeros(mask.shape + (4,), np.float32)
    out[..., 0], out[..., 1], out[..., 2] = color
    out[..., 3] = np.clip(a, 0, 1) * 255
    return to_img(out)


def warm_mask(img):
    a = arr(img)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    m = np.clip((r - b - 70) / 60.0, 0, 1) * (al > 40)
    return m


def cloudy(base, t):
    return affine(base, 0, 1 + 0.02 * math.sin(2 * math.pi * t), 3.0 * math.sin(2 * math.pi * t), 1.5 * math.sin(4 * math.pi * t))


def clear_day(base, t):
    s = 2 * math.pi * t
    img = affine(base, 4.0 * math.sin(s), 1 + 0.025 * math.sin(s))
    a = arr(img)
    m = warm_mask(img)[..., None]
    a[..., :3] = a[..., :3] * (1 + 0.14 * math.sin(s) * m) + 22 * math.sin(s) * m * np.array([1, 0.9, 0.5])
    halo = glow(warm_mask(img), (255, 200, 80), 7, 0.45 + 0.25 * math.sin(s))
    return over(halo, to_img(a))


def clear_night(base, t):
    s = 2 * math.pi * t
    img = affine(base, 2.0 * math.sin(s), 1 + 0.015 * math.sin(s))
    a = arr(img)
    alpha = a[..., 3] / 255.0
    halo = glow(alpha, (190, 210, 255), 8, 0.35 + 0.3 * (0.5 + 0.5 * math.sin(s)))
    return over(halo, img)


def partly_day(base, t):
    s = 2 * math.pi * t
    a = arr(base)
    m = warm_mask(base)
    sun = a.copy()
    sun[..., 3] = a[..., 3] * m
    cloud = a.copy()
    cloud[..., 3] = a[..., 3] * (1 - m)
    # gunes rengini bulut altina yay (bulut kayinca gorunen bosluk dolsun)
    under = Image.fromarray(np.clip(sun, 0, 255).astype(np.uint8), "RGBA")
    for _ in range(3):
        under = under.filter(ImageFilter.MaxFilter(5))
    under_a = arr(under)
    under_a[..., 3] = np.minimum(under_a[..., 3], 255)
    sun_img = to_img(under_a)
    # gunes parlama
    sa = arr(sun_img)
    k = 1 + 0.12 * math.sin(s)
    sa[..., :3] = sa[..., :3] * k
    sun_img = to_img(sa)
    halo = glow(m, (255, 200, 80), 6, 0.35 + 0.2 * math.sin(s))
    cloud_img = affine(to_img(cloud), 0, 1, 3.5 * math.sin(s), 1.0 * math.sin(2 * s))
    return over(over(halo, sun_img), cloud_img)


def partly_night(base, t):
    s = 2 * math.pi * t
    return affine(base, 0, 1, 2.5 * math.sin(s), 1.0 * math.sin(2 * s))


def fog(base, t):
    a = arr(base)
    h, w = a.shape[:2]
    out = np.zeros_like(a)
    xs = np.arange(w, dtype=np.float32)
    for y in range(h):
        dx = 4.0 * math.sin(2 * math.pi * t + y / 7.0) + 2.0 * math.sin(4 * math.pi * t - y / 4.0)
        for c in range(4):
            out[y, :, c] = np.interp(xs - dx, xs, a[y, :, c], left=0, right=0)
    return to_img(out)


def falling(base, t, top, cycles, sway=0.0, bottom=122):
    """Bulutun altindaki bant (damla/tane) asagi kayar; ust ve alt kenarda silinir, dongu dikissiz."""
    h = bottom - top
    a = arr(base)
    cloud = a.copy()
    cloud[top:, :, 3] = 0
    band = Image.fromarray(np.clip(np.where(np.arange(128)[:, None, None] >= top, a, 0), 0, 255).astype(np.uint8), "RGBA")
    ramp = np.zeros(128, np.float32)
    fade_in, fade_out = 7.0, 10.0
    for y in range(top, 128):
        u = min((y - top) / fade_in, (bottom - y) / fade_out + (0 if y < bottom else -1))
        ramp[y] = float(np.clip(u, 0, 1))
    out = to_img(cloud)
    phase = (t * cycles) % 1.0
    for k in (0, -1):
        dy = phase * h + k * h
        dx = sway * math.sin(2 * math.pi * t + k)
        sh = arr(shift(band, dx, dy))
        # kaydirilmis kopyanin pencere maskesi: kaynak satirina gore degil, ciktidaki satira gore
        win = np.zeros(128, np.float32)
        for y in range(128):
            yy = y - dy  # ciktidaki y'nin kaynagi
            if top <= yy < 128:
                u = min((yy - top) / fade_in, (bottom - yy) / fade_out)
                win[y] = float(np.clip(u, 0, 1))
        sh[..., 3] *= win[:, None]
        out = over(out, to_img(sh))
    return out


def rain(base, t):
    return falling(base, t, 84, 3)


def snow(base, t):
    return falling(base, t, 80, 2, sway=2.5)


FLASH = {4: 1.0, 5: 0.35, 6: 0.85, 7: 0.25}


def thunder(base, i, t):
    f = FLASH.get(i, 0.0)
    a = arr(base)
    r, g, b, al = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
    bolt = np.clip((b - r - 25) / 60.0, 0, 1) * np.clip((b - 120) / 80.0, 0, 1) * (al > 40)
    bolt[:56] = 0  # bulutun mavi golgesi simsek sayilmasin
    gain = 0.75 + 0.9 * f
    a[..., :3] = a[..., :3] * (1 + (gain - 1) * bolt[..., None]) + 40 * f * bolt[..., None]
    a[..., :3] += f * 0.22 * (255 - a[..., :3])  # bulut ici aydinlanma
    img = to_img(a)
    halo = glow(bolt, (150, 190, 255), 9, 1.4 * f)
    return over(halo, img)


def render(name, fn, uses_index=False):
    base = load(name)
    frames = []
    for i in range(N):
        t = i / N
        fr = fn(base, i, t) if uses_index else fn(base, t)
        frames.append(fr)
    return frames


def small(img):
    return img.convert("RGBa").resize((SIZE, SIZE), Image.LANCZOS).convert("RGBA")


def main():
    jobs = {
        "ClearDay": (clear_day, False), "ClearNight": (clear_night, False), "Cloudy": (cloudy, False),
        "Fog": (fog, False), "PartlyDay": (partly_day, False), "PartlyNight": (partly_night, False),
        "Rain": (rain, False), "Snow": (snow, False), "Thunder": (thunder, True),
    }
    os.makedirs(DST, exist_ok=True)
    for name, (fn, idx) in jobs.items():
        frames = [small(f) for f in render(name, fn, idx)]
        strip = Image.new("RGBA", (SIZE * N, SIZE), (0, 0, 0, 0))
        for i, f in enumerate(frames):
            strip.paste(f, (i * SIZE, 0))
        strip.save(os.path.join(DST, name + ".png"), optimize=True)
        small(load(name)).save(os.path.join(DST, name + ".static.png"), optimize=True)
        if len(sys.argv) > 1:  # onizleme: python tools/render_cinematic.py <klasor>
            sheet = Image.new("RGBA", (SIZE * 10, SIZE * 2), (55, 65, 85, 255))
            for i, f in enumerate(frames):
                sheet.alpha_composite(f, ((i % 10) * SIZE, (i // 10) * SIZE))
            sheet.save(os.path.join(sys.argv[1], name + "_sheet.png"))
        print(name, "ok")


if __name__ == "__main__":
    main()
