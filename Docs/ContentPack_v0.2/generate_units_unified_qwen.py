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
OUT = ROOT / "Assets/Resources/Art/Units"
OUT.mkdir(parents=True, exist_ok=True)

NEG = (
    "text, letters, logo, watermark, signature, blurry, lowres, noisy, jpeg artifacts, "
    "full ui screenshot, interface layout, collage, multiple subjects, cropped head, "
    "deformed anatomy, extra limbs, photorealistic, 3d render"
)

STYLE = (
    "single standalone strategy-game unit portrait asset, same art direction across all units, "
    "neo-oriental mech fantasy inspired by chinese chess, polished concept-art quality, "
    "clean readable silhouette, centered composition, medium-close framing, dramatic rim light, "
    "high contrast value separation, dark navy background with subtle particles, "
    "no frame no border no ui no text"
)

UNITS = {
    "chariot_tank": "heavy shielded chariot tank with layered steel armor and defensive frontal profile, fortress-like body",
    "chariot_sport": "sleek high-speed sport chariot with neon aerodynamic fins and agile racing stance",
    "chariot_shock": "shock chariot with thunder capacitors, electric arcs around wheel hubs, aggressive ram silhouette",
    "chariot_bulwark": "bulwark chariot with stone-steel bastion plating, broad shield prow, immovable defender vibe",
    "chariot_ram": "siege ram chariot with blazing reinforced horn and impact spikes, brutal offensive silhouette",

    "horse_raider": "raider warhorse rider with assault spear and fast attack posture, shadow-green trail effects",
    "horse_banner": "banner cavalry with holy pennant and support aura, disciplined knightly silhouette",
    "horse_nightmare": "nightmare cavalry with ghostly black-purple flame mane and sinister charging stance",

    "cannon_burst": "burst artillery cannon with multi-barrel rapid-fire module and recoil vents",
    "cannon_mortar": "mortar artillery cannon with elevated short barrel and explosive payload chamber",
    "cannon_missile": "missile artillery platform with long barrel, rocket pods, and ignition exhaust",
    "cannon_sniper": "sniper artillery rail-cannon with elongated precision barrel and cold scope glow",
    "cannon_arc": "arc artillery tesla cannon with compact coils and violet chain-lightning discharge",
    "cannon_scout": "light scout cannon with compact chassis, targeting antenna, and quick reposition feel",

    "general_fire": "flame general commander piece with regal armor, blazing cloak aura, authoritative centerpiece",
    "ele_guard": "elephant guardian construct with stone armor and massive protective stance, ancient sentinel feel",

    "guard_assassin": "shadow assassin guard with dual short blades and stealth dash afterimages",
    "guard_blade": "night blade assassin with curved darksteel sword and swift execution posture",
    "guard_poison": "venom assassin with emerald daggers and toxic mist streaks, lethal close-combat profile",
    "guard_mirror": "mirror assassin with crystalline reflective blades and phantom duplicate trails",

    "soldier_phalanx": "phalanx infantry soldier with tower shield and disciplined frontline formation posture",
    "soldier_sword": "sword infantry soldier with compact armor and balanced duelist stance"
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


def txt2img(prompt: str, out_path: Path):
    payload = {
        "prompt": f"{prompt}, {STYLE}",
        "negative_prompt": NEG,
        "steps": 12,
        "sampler_name": "Euler a",
        "cfg_scale": 6.5,
        "width": 1024,
        "height": 1024,
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
    out_path.write_bytes(base64.b64decode(data["images"][0]))


def main():
    if not ping_api():
        raise SystemExit(f"Draw Things API unavailable: {API}")

    print(f"Using API={API}", flush=True)
    print(f"Using model={MODEL}", flush=True)

    for key, desc in UNITS.items():
        out = OUT / f"unit_{key}.png"
        prompt = (
            f"single unit portrait of {desc}, "
            "strategy autobattler unit identity readable at thumbnail size, "
            "consistent brushwork and lighting with the full set"
        )
        txt2img(prompt, out)
        print(f"generated {out}", flush=True)


if __name__ == "__main__":
    main()
