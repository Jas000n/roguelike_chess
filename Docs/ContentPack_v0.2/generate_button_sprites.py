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

NEG = (
    "full interface, whole ui screen, dashboard, multiple panels, text, letters, logo, watermark,"
    " scene, character, background clutter, blurry, lowres"
)

PROMPTS = {
    "ui_button": "single game button sprite only, rounded rectangle, cyan blue gradient, glossy edge, centered, isolated object, plain dark background",
    "ui_button_hover": "single game button sprite only, rounded rectangle, bright cyan blue glow, hover state, centered, isolated object, plain dark background",
    "ui_button_pressed": "single game button sprite only, rounded rectangle, deep blue pressed inset style, centered, isolated object, plain dark background",
    "ui_button_warn": "single game button sprite only, rounded rectangle, orange gold warning style, glossy edge, centered, isolated object, plain dark background",
}


def txt2img(prompt: str, out_path: Path):
    payload = {
        "prompt": prompt + ", no text, no icon, no symbols, game ui asset",
        "negative_prompt": NEG,
        "steps": 18,
        "sampler_name": "DPM++ 2M AYS",
        "cfg_scale": 6.5,
        "width": 768,
        "height": 320,
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
    for name, prompt in PROMPTS.items():
        out = OUT / f"{name}.png"
        txt2img(prompt, out)
        print("generated", out)
