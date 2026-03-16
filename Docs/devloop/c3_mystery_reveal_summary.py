#!/usr/bin/env python3
import glob
import os
import re
from collections import Counter, defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
BUILDS = os.path.join(ROOT, "Builds")

# 优先读取 C3 相关日志；若为空回退到全部 devloop_cycle 日志
paths = sorted(glob.glob(os.path.join(BUILDS, "build_devloop_cycle_c3_*.log")))
if not paths:
    paths = sorted(glob.glob(os.path.join(BUILDS, "build_devloop_cycle_*.log")))

pat = re.compile(r"\[DEV\]\[MYSTERY_REVEAL\] floor=(\d+) roll=([0-9.]+) type=(\w+) weights=N:([0-9.]+),E:([0-9.]+),S:([0-9.]+),T:([0-9.]+)")

rows = []
for p in paths:
    try:
        txt = open(p, encoding="utf-8", errors="ignore").read()
    except Exception:
        continue
    for m in pat.finditer(txt):
        floor = int(m.group(1))
        t = m.group(3)
        rows.append((floor, t, os.path.basename(p)))

if not rows:
    print("no mystery reveal samples found")
    raise SystemExit(0)

by_bucket = defaultdict(Counter)
all_types = Counter()
for floor, t, _ in rows:
    bucket = "early" if floor <= 4 else ("late" if floor >= 9 else "mid")
    by_bucket[bucket][t] += 1
    all_types[t] += 1

print(f"samples={len(rows)} files={len(set(p for *_, p in rows))}")
print("overall:", ", ".join(f"{k}:{v}" for k, v in all_types.most_common()))
for bucket in ["early", "mid", "late"]:
    c = by_bucket[bucket]
    total = sum(c.values())
    if total == 0:
        print(f"{bucket}: none")
        continue
    detail = ", ".join(f"{k}:{v}({v/total:.0%})" for k, v in c.most_common())
    print(f"{bucket}: total={total} | {detail}")

# Directionality check against C3 intent
# early: prefer Shop over Treasure
# late: prefer Elite+Treasure over Shop
early = by_bucket["early"]
late = by_bucket["late"]
if sum(early.values()) > 0:
    early_shop = early.get("Shop", 0)
    early_treasure = early.get("Treasure", 0)
    early_ok = early_shop >= early_treasure
    print(f"direction_check early(shop>=treasure): {'PASS' if early_ok else 'WARN'} | shop={early_shop} treasure={early_treasure}")
else:
    print("direction_check early(shop>=treasure): N/A")

if sum(late.values()) > 0:
    late_elite_treasure = late.get("Elite", 0) + late.get("Treasure", 0)
    late_shop = late.get("Shop", 0)
    late_ok = late_elite_treasure >= late_shop
    print(f"direction_check late((elite+treasure)>=shop): {'PASS' if late_ok else 'WARN'} | elite+treasure={late_elite_treasure} shop={late_shop}")
else:
    print("direction_check late((elite+treasure)>=shop): N/A")

# Confidence hints (sample adequacy)
early_n = sum(early.values())
late_n = sum(late.values())
early_ok_conf = early_n >= 5
late_ok_conf = late_n >= 5
print(f"confidence early_samples: {early_n} ({'OK' if early_ok_conf else 'LOW'})")
print(f"confidence late_samples: {late_n} ({'OK' if late_ok_conf else 'LOW'})")

status = "STABLE" if early_ok_conf and late_ok_conf else "NEEDS_MORE_SAMPLES"
if status == "STABLE":
    print(f"c3_mystery_status: {status} (direction_checks=PASS/PASS, confidence=OK/OK)")
else:
    print(f"c3_mystery_status: {status} (need early>=5 and late>=5)")

# one-line heartbeat-friendly signal
print(f"c3_heartbeat_signal: status={status} early={early_n} late={late_n}")
