# C2 Warn Policy (Draft)

目标：在不引入高噪声误报的前提下，把 `SPIKE_WARN` 从纯观察升级为“软门禁”。

## 当前样本（2026-03-15 更新）
- 全量窗口：18 次 Batch
  - `SPIKE_SCENARIO`：均 `pass=18 fail=0`
  - `warn` 出现：6/18 次（总计 6 条）
- recent10 窗口（推荐作为决策主窗口）：
  - `warn_runs=3/10`
  - `warn_total=3`
- 结论：告警呈偶发分布，最近窗口已回落，继续 warn-only 更稳妥。

## 建议软门禁（下一阶段）
- 规则：仅当最近 10 次样本中 `warn_runs >= 5` 时，触发 soft-fail（CI yellow）。
- 规则：若连续 3 次样本 `warn>0`，触发调参提醒（不阻断提交）。

## 处理建议
1. 先继续累积样本到 10~12 次。
2. 若达到 soft-fail 触发线，再小步调整：
   - `targetShare`（按组合）
   - `GetLockedCompClassBiasByLevel` / `LockedCompOriginBias`
3. 调整后复测 5 次，确认 `warn_runs` 回落。
