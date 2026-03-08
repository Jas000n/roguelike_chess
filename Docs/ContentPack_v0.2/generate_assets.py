#!/usr/bin/env python3
import base64
import json
import os
from pathlib import Path
from urllib import request

API = os.environ.get("DRAWTHINGS_URL", "http://127.0.0.1:7860").rstrip("/")
BASE_DIR = Path(__file__).resolve().parent
OUT = BASE_DIR / "generated"
OUT.mkdir(parents=True, exist_ok=True)

NEG = "lowres, blurry, text, watermark, logo, photorealistic, ugly, noisy, deformed, extra limbs"

HEXES = {
    "hex_rich": "game UI icon, raining gold coins, futuristic fantasy style, clean silhouette, centered composition, high contrast",
    "hex_interest_up": "game UI icon, abacus and coin stacks with subtle glow, economy upgrade, clean silhouette, centered",
    "hex_fast_train": "game UI icon, hourglass and chess unit hologram, training acceleration, crisp edges, centered",
    "hex_thrifty_refresh": "game UI icon, refresh arrows around discount ticket, economy reroll, high contrast",
    "hex_scrap_rebate": "game UI icon, wrench and recycle symbol with coins, salvage rebate, centered",
    "hex_team_atk": "game UI icon, three chess units under one power aura, team buff, vibrant",
    "hex_vanguard_wall": "game UI icon, heavy shield with hexagonal barrier, steel vanguard trait",
    "hex_rider_charge": "game UI icon, horse hoof shockwave, cavalry charge burst, dynamic",
    "hex_cannon_master": "game UI icon, artillery cannon core with focused energy beam, high contrast",
    "hex_artillery_range": "game UI icon, targeting scope and ballistic arc lines, artillery range boost",
    "hex_board_plus": "game UI icon, chessboard with one extra glowing deployment tile, golden",
    "hex_healing": "game UI icon, repair drone and green healing pulse, sustain buff",
    "hex_overclocked_core": "game UI icon, overclock reactor with electric arcs, speed and damage boost",
    "hex_frontline_oath": "game UI icon, frontline shield wall emblem, oath and defense",
    "hex_precision_barrage": "game UI icon, crosshair with dense tracer barrage, precision strike",
}

UNITS = {
    "unit_chariot_tank": "game unit portrait, heavy tank chariot, thick armor plates, blue white steel tones, transparent background style",
    "unit_chariot_sport": "game unit portrait, neon sport chariot, sleek aerodynamic body, cyan green highlights, motion trail",
    "unit_chariot_shock": "game unit portrait, shock chariot with thunder coils, purple electric effects, aggressive silhouette",
    "unit_horse_raider": "game unit portrait, raider horse warrior, dark green assault style, sharp spear, dynamic pose",
    "unit_horse_banner": "game unit portrait, banner horse knight, holy blue gold flag aura, support cavalry look",
    "unit_horse_nightmare": "game unit portrait, nightmare horse, black purple ghost flame, sinister eyes",
    "unit_cannon_burst": "game unit portrait, burst cannon, multi barrel rapid fire machine, red accents",
    "unit_cannon_mortar": "game unit portrait, mortar cannon, high-angle short barrel, orange explosion motif",
    "unit_cannon_missile": "game unit portrait, missile cannon, long barrel with rocket pods, orange red thruster flame",
    "unit_cannon_sniper": "game unit portrait, sniper cannon with elongated rail barrel and frost-blue scope glow, precise and elegant silhouette",
    "unit_cannon_arc": "game unit portrait, arc cannon with tesla coils and violet electric arcs, compact rapid discharge design",
    "unit_chariot_bulwark": "game unit portrait, bulwark chariot with heavy stone-steel shield plating, defensive fortress profile",
    "unit_chariot_ram": "game unit portrait, siege ram chariot with blazing reinforced horn and ember exhaust, brutal impact style",
    "unit_guard_poison": "game unit portrait, poison assassin guard with emerald daggers and toxic mist trails, agile lethal posture",
    "unit_guard_mirror": "game unit portrait, mirror assassin guard with reflective crystalline blades and misty afterimages, high-speed strike vibe",
}

def txt2img(prompt, out_path: Path, steps=18, cfg=6.5, w=1024, h=1024):
    payload = {
        "prompt": prompt,
        "negative_prompt": NEG,
        "steps": steps,
        "sampler_name": "DPM++ 2M Karras",
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
    img_b64 = data["images"][0]
    out_path.write_bytes(base64.b64decode(img_b64))


def main():
    style_suffix = ", game asset style, no text, icon-ready, polished digital painting"
    for k, p in HEXES.items():
        out = OUT / f"{k}.png"
        txt2img(p + style_suffix, out, steps=16, cfg=6.0, w=1024, h=1024)
        print("generated", out)

    for k, p in UNITS.items():
        out = OUT / f"{k}.png"
        txt2img(p + style_suffix, out, steps=18, cfg=6.5, w=1024, h=1024)
        print("generated", out)


if __name__ == "__main__":
    main()
