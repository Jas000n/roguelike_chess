#!/usr/bin/env python3
import base64
import json
import os
from pathlib import Path
from urllib import request

API = os.environ.get("DRAWTHINGS_URL", "http://127.0.0.1:7860").rstrip("/")
BASE_DIR = Path(__file__).resolve().parent
OUT = BASE_DIR / "generated_ui"
OUT.mkdir(parents=True, exist_ok=True)

NEG = "text, letters, watermark, logo, blurry, lowres, photorealistic"

ASSETS = {
    "ui_panel": "game ui panel background, sci-fi fantasy strategy game, dark navy with subtle gold frame, clean center area",
    "ui_panel_dark": "game ui dark panel background, deep blue-black tone, subtle bevel, no text",
    "ui_button": "game ui button, cyan blue fantasy sci-fi, glossy, clean border, no text",
    "ui_button_pressed": "game ui button pressed state, deeper cyan blue, inset look, no text",
    "ui_button_warn": "game ui button, warm orange-gold warning style, glossy border, no text",
    "ui_card": "game ui card frame, rarity-compatible neutral frame, dark center and ornate edges, no text",
}


def txt2img(prompt, out_path: Path, w=1024, h=512, steps=16, cfg=6.0):
    payload = {
        "prompt": prompt + ", clean game asset, no text, no logo",
        "negative_prompt": NEG,
        "steps": steps,
        "sampler_name": "DPM++ 2M AYS",
        "cfg_scale": cfg,
        "width": w,
        "height": h,
        "batch_size": 1,
        "seed": -1,
    }
    req = request.Request(
        API + "/sdapi/v1/txt2img",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with request.urlopen(req, timeout=300) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    out_path.write_bytes(base64.b64decode(data["images"][0]))


if __name__ == "__main__":
    for name, prompt in ASSETS.items():
        size = (1024, 512)
        if "button" in name:
            size = (512, 256)
        if name == "ui_card":
            size = (512, 768)
        out = OUT / f"{name}.png"
        txt2img(prompt, out, w=size[0], h=size[1])
        print("generated", out)
