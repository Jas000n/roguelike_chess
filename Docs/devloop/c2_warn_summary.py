#!/usr/bin/env python3
import csv
import glob
import os
import re
from statistics import mean

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
DOCS_DEVLOOP = os.path.join(ROOT, "Docs", "devloop")
BUILDS = os.path.join(ROOT, "Builds")
CSV_PATH = os.path.join(DOCS_DEVLOOP, "spike_warn_history.csv")


def summarize(name, data):
    # data: list[(idx, warn_total, warn_a, warn_o, warn_t, passed)]
    warn_runs = sum(1 for _, w, *_ in data if w > 0)
    warn_total = sum(w for _, w, *_ in data)
    warn_a = sum(a for _, _, a, *_ in data)
    warn_o = sum(o for _, _, _, o, *_ in data)
    warn_t = sum(t for _, _, _, _, t, _ in data)
    pass_rate = mean(1.0 if p else 0.0 for _, _, _, _, _, p in data)
    print(
        f"[{name}] samples={len(data)} warn_runs={warn_runs} warn_total={warn_total} "
        f"warn_by_hex=A:{warn_a},O:{warn_o},T:{warn_t} pass_rate={pass_rate:.2f}"
    )


rows = []

# Preferred source: rolling csv written by DevRecordSpikeWarnSample
if os.path.exists(CSV_PATH):
    with open(CSV_PATH, encoding="utf-8", errors="ignore", newline="") as f:
        reader = csv.reader(f)
        for i, parts in enumerate(reader, start=1):
            if not parts:
                continue
            if parts[0].strip().lower() == "timestamp":
                continue
            # old format: ts,warn
            # new format: ts,warn_total,warn_assassin,warn_artillery,warn_tri_service
            warn = int(parts[1]) if len(parts) >= 2 and parts[1].strip().isdigit() else 0
            warn_a = int(parts[2]) if len(parts) >= 3 and parts[2].strip().isdigit() else 0
            warn_o = int(parts[3]) if len(parts) >= 4 and parts[3].strip().isdigit() else 0
            warn_t = int(parts[4]) if len(parts) >= 5 and parts[4].strip().isdigit() else 0
            rows.append((i, max(0, warn), max(0, warn_a), max(0, warn_o), max(0, warn_t), True))

# Fallback source: historical sample logs
if not rows:
    paths = sorted(glob.glob(os.path.join(BUILDS, "build_devloop_cycle_c2_warn_sample_*.log")))
    for p in paths:
        txt = open(p, encoding="utf-8", errors="ignore").read()
        m = re.search(r"\[DEV\]\[SPIKE_SCENARIO\] pass=(\d+) fail=(\d+) warn=(\d+)(?: warnByHex=A:(\d+),O:(\d+),T:(\d+))?", txt)
        if not m:
            continue
        sm = re.search(r"sample_(\d+)\.log$", p)
        sample = int(sm.group(1)) if sm else len(rows) + 1
        warn = int(m.group(3))
        warn_a = int(m.group(4) or 0)
        warn_o = int(m.group(5) or 0)
        warn_t = int(m.group(6) or 0)
        passed = "[DEV][BATCH] PASSED" in txt
        rows.append((sample, warn, warn_a, warn_o, warn_t, passed))

rows.sort(key=lambda x: x[0])
if not rows:
    print("no samples found")
    raise SystemExit(0)

summarize("all", rows)
recent = rows[-10:]
summarize("recent10", recent)
print(
    "recent10 detail:",
    ", ".join(
        f"s{n}:w{w}(A{a}/O{o}/T{t})"
        for n, w, a, o, t, _ in recent
    ),
)

# soft-gate recommendation
recent_warn_runs = sum(1 for _, w, *_ in recent if w > 0)
recent_warn_a = sum(a for _, _, a, _, _, _ in recent)
recent_warn_o = sum(o for _, _, _, o, _, _ in recent)
recent_warn_t = sum(t for _, _, _, _, t, _ in recent)

consecutive_warn_runs = 0
for _, w, *_ in reversed(recent):
    if w > 0:
        consecutive_warn_runs += 1
    else:
        break

if recent_warn_runs >= 5:
    print("recommendation: TRIGGER soft-gate (recent10 warn_runs >= 5)")
else:
    print("recommendation: keep warn-only (recent10 warn_runs < 5)")

if consecutive_warn_runs >= 3:
    print("tune_hint_window: TRIGGER (consecutive recent warns >= 3)")
else:
    print("tune_hint_window: keep observe (consecutive recent warns < 3)")

bucket_totals = {
    "assassin_contract": recent_warn_a,
    "artillery_overclock": recent_warn_o,
    "tri_service": recent_warn_t,
}
dominant_bucket = max(bucket_totals, key=bucket_totals.get)
dominant_value = bucket_totals[dominant_bucket]
if dominant_value > 0:
    print(f"dominant_warn_bucket: {dominant_bucket} ({dominant_value})")
    print(f"tuning_hint: prioritize small-step retune on {dominant_bucket} targetShare/bias")
else:
    print("dominant_warn_bucket: none")
