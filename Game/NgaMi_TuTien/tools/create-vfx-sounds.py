"""Tạo âm mẫu tổng hợp cho Nga Mi, không dùng mẫu âm thanh bên ngoài.

Chạy từ bất kỳ thư mục nào: python tools/create-vfx-sounds.py
Các WAV nguồn nằm ở src/assets/raw; tools/build-assets.js chép sang bản game.
"""
import math
import random
import struct
import wave
from pathlib import Path

RATE = 22050
OUT = Path(__file__).resolve().parent.parent / "src" / "assets" / "raw"

# Tần số đầu/cuối, thời lượng, tỷ lệ nhiễu và kiểu đường bao. Hai biến thể có seed riêng.
SOUNDS = {
    "kiem": (950, 320, .36, .56, "sweep"),
    "va_cham": (680, 120, .32, .72, "hit"),
    "tu_khi": (220, 680, .58, .24, "sweep"),
    "ngoc": (1300, 1080, .65, .04, "bell"),
    "lua": (180, 95, .65, .90, "sweep"),
    "nuoc": (700, 310, .56, .50, "water"),
    "phu_van": (520, 1050, .70, .12, "bell"),
    "rong": (130, 70, .80, .40, "dragon"),
    "trung": (1550, 680, .58, .50, "egg"),
    "ti_thi": (420, 650, .76, .14, "bell"),
}


def render(name, variant, spec):
    f0, f1, duration, noise_mix, shape = spec
    f0 *= 1 + (variant - 1.5) * .08
    f1 *= 1 + (variant - 1.5) * .08
    rng = random.Random(20261115 + list(SOUNDS).index(name) * 10 + variant)
    samples, filtered, phase = [], 0.0, 0.0
    for i in range(round(duration * RATE)):
        t = i / RATE
        x = t / duration
        freq = f0 + (f1 - f0) * x
        phase += 2 * math.pi * freq / RATE
        filtered = filtered * .83 + rng.uniform(-1, 1) * .17
        if shape == "bell":
            env = min(1, t * 60) * math.exp(-4.8 * x)
            tone = math.sin(phase) * .65 + math.sin(phase * 2.01) * .22 + math.sin(phase * 3.97) * .10
        elif shape == "hit":
            env = min(1, t * 100) * math.exp(-7 * x)
            tone = math.sin(phase) * .65
        elif shape == "dragon":
            env = math.sin(math.pi * x) ** .8
            tone = (math.sin(phase) + .35 * math.sin(phase * 2.02)) * .55 * (1 + .15 * math.sin(t * 21))
        elif shape == "water":
            env = math.sin(math.pi * x) ** 1.2
            tone = math.sin(phase + 2 * math.sin(t * 27)) * .5
        elif shape == "egg":
            env = min(1, t * 90) * math.exp(-5 * x)
            tone = math.sin(phase) * .5 + math.sin(phase * 2.5) * .15
        else:
            env = math.sin(math.pi * x) ** 1.5
            tone = math.sin(phase) * .55
        value = env * ((1 - noise_mix) * tone + noise_mix * filtered * 2.0)
        samples.append(max(-1.0, min(1.0, value * .55)))
    filename = OUT / f"sfx_{name}_{variant}.wav"
    if filename.exists():
        raise FileExistsError(f"Không ghi đè âm hiện có: {filename}")
    with wave.open(str(filename), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(b"".join(struct.pack("<h", round(s * 32767)) for s in samples))
    print(filename.name)


if __name__ == "__main__":
    for key, parameters in SOUNDS.items():
        for number in (1, 2):
            render(key, number, parameters)
