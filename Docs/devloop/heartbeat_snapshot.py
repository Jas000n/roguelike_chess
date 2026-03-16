#!/usr/bin/env python3
import subprocess
import os
from datetime import datetime

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))

c2 = os.path.join(ROOT, "Docs", "devloop", "c2_warn_summary.py")
c3 = os.path.join(ROOT, "Docs", "devloop", "c3_mystery_reveal_summary.py")


def run(path):
    out = subprocess.check_output(["python3", path], text=True, cwd=ROOT)
    return out.splitlines()

c2_lines = run(c2)
c3_lines = run(c3)

c2_signal = next((l for l in c2_lines if l.startswith("c2_heartbeat_signal:")), "c2_heartbeat_signal: N/A")
c3_signal = next((l for l in c3_lines if l.startswith("c3_heartbeat_signal:")), "c3_heartbeat_signal: N/A")
c3_status = next((l for l in c3_lines if l.startswith("c3_mystery_status:")), "c3_mystery_status: N/A")

print(f"heartbeat_snapshot_time: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
print(c2_signal)
print(c3_signal)
print(c3_status)

# one-line overall for heartbeat automation
overall = "HEALTHY" if ("warn_runs=0/10" in c2_signal and "status=STABLE" in c3_signal) else "CHECK"
print(f"heartbeat_overall: {overall}")
