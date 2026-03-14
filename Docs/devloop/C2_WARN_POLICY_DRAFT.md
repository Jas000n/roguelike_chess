# C2 Warn Policy (Draft)

目标：在不引入高噪声误报的前提下，把 `SPIKE_WARN` 从纯观察升级为“软门禁”。

## 当前样本（2026-03-14）
- 样本窗口：8 次 Batch
- `SPIKE_SCENARIO`：均 `pass=18 fail=0`
- `warn` 出现：3/8 次（总计 3 条）
- 结论：存在偶发偏低，但尚不具备稳定复现性，不宜直接 hard-fail。

## 建议软门禁（下一阶段）
- 规则：仅当最近 10 次样本中 `warn_runs >= 5` 时，触发 soft-fail（CI yellow）。
- 规则：若连续 3 次样本 `warn>0`，触发调参提醒（不阻断提交）。

## 处理建议
1. 先继续累积样本到 10~12 次。
2. 若达到 soft-fail 触发线，再小步调整：
   - `targetShare`（按组合）
   - `GetLockedCompClassBiasByLevel` / `LockedCompOriginBias`
3. 调整后复测 5 次，确认 `warn_runs` 回落。
