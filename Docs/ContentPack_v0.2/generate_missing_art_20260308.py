#!/usr/bin/env python3
import base64
import json
import os
from pathlib import Path
from urllib import request

API = os.environ.get("DRAWTHINGS_URL", "http://127.0.0.1:7860").rstrip("/")
MODEL = os.environ.get("DRAWTHINGS_MODEL", "qwen_image_2512_q6p.ckpt")
TIMEOUT = int(os.environ.get("DRAWTHINGS_TIMEOUT", "1800"))
ROOT = Path(__file__).resolve().parents[2]

NEG = (
    "text, letters, logo, watermark, signature, blurry, lowres, noisy, jpeg artifacts, "
    "full ui screenshot, full game scene, interface layout, collage, multiple unrelated objects, "
    "frame, border, deformed, photorealistic, 3d render, cluttered background"
)

UNIT_STYLE = (
    "single standalone strategy autobattler unit portrait asset, neo-oriental mech fantasy based on chinese chess, "
    "clean readable silhouette, centered composition, medium close-up, polished concept art quality, "
    "consistent brushwork and lighting with existing unit set, dark navy background with subtle particles, "
    "one subject only, no text, no ui"
)

HEX_STYLE = (
    "single standalone hex augment icon, centered icon-ready composition, high contrast, clean contour for UI readability, "
    "neo-oriental tactical tech fantasy, navy-cyan base with controlled amber accents, "
    "flat readable background and strong foreground symbol, no text, no border, no frame"
)

UNITS = {
    "soldier_guard": "shield infantry guard soldier with compact steel-stone armor, short spear and protective stance, disciplined frontline defender identity",
    "horse_lancer": "lancer cavalry rider on warhorse with long thrust lance, steel pennant, momentum-forward assault posture, agile but armored"
}

HEXES = {
    "lifesteal_core": "vital core reactor with crimson energy siphon loop and healing pulse arcs, life drain and sustain theme",
    "execution_edge": "sharp execution blade over fractured low-health bar, finisher threshold amplification theme",
    "assassin_bloom": "shadow dagger blossom with critical spark petals and burst impact ring, assassin first-strike theme",
    "assassin_contract": "sealed dark contract sigil with crossed daggers and coin glint, kill-to-earn assassin economy theme",
    "vanguard_bastion": "fortress shield bastion emblem with layered plates and opening battle barrier, frontline tank theme",
    "rider_relay": "cavalry relay insignia with repeated hoof shockwaves and speed trails, chained charge momentum theme",
    "artillery_overclock": "artillery fire-control module overclocked with hot circuitry and splash targeting reticle, barrage frequency theme",
    "stone_oath": "ancient stone oath crest with protective runes and mountain-solid aura, earth durability theme",
    "venom_payload": "toxic payload canister with venom droplets and corroding trajectory arc, damage-over-time poison theme",
    "windwalk": "wind step glyph with swift current ribbons and evasive blur, speed and dodge theme",
    "reroll_engine": "precision reroll engine with circular refresh arrows and calibrated gears, shop refresh economy theme",
    "triple_prep": "three-star tactical blueprint with ascending star nodes and empowered champion core, reroll chase theme"
}


def ping_api():
    for p in ("/sdapi/v1/options", "/"):
        try:
            req = request.Request(API + p, method="GET")
            with request.urlopen(req, timeout=8) as resp:
                if resp.status == 200:
                    return True
        except Exception:
            continue
    return False


def txt2img(prompt: str, out_path: Path, w: int, h: int, steps: int, cfg: float):
    payload = {
        "prompt": prompt,
        "negative_prompt": NEG,
        "steps": steps,
        "sampler_name": "Euler a",
        "cfg_scale": cfg,
        "width": w,
        "height": h,
        "batch_size": 1,
        "seed": -1,
        "model": MODEL,
    }
    req = request.Request(
        API + "/sdapi/v1/txt2img",
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with request.urlopen(req, timeout=TIMEOUT) as resp:
        data = json.loads(resp.read().decode("utf-8"))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(base64.b64decode(data["images"][0]))


def main():
    if not ping_api():
        raise SystemExit(f"Draw Things API unavailable: {API}")

    print(f"Using API={API}")
    print(f"Using model={MODEL}")

    for key, desc in UNITS.items():
        out = ROOT / f"Assets/Resources/Art/Units/unit_{key}.png"
        prompt = (
            f"single unit portrait of {desc}, {UNIT_STYLE}, "
            "must not contain any panel, button, full interface, or scene composition"
        )
        txt2img(prompt, out, 1024, 1024, steps=14, cfg=6.8)
        print(f"generated {out}", flush=True)

    for hid, desc in HEXES.items():
        out = ROOT / f"Assets/Resources/Art/Hexes/hex_{hid}.png"
        prompt = (
            f"single game augment icon of {desc}, {HEX_STYLE}, "
            "must be exactly one icon, not a screen, not a card sheet"
        )
        txt2img(prompt, out, 512, 512, steps=14, cfg=7.0)
        print(f"generated {out}", flush=True)


if __name__ == "__main__":
    main()
