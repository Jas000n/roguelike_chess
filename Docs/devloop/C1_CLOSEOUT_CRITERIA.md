# Stage C1 Closeout Criteria

目的：把 C1（单位扩充到可构筑规模）的收口门槛固定下来，后续扩内容复用同一套标准。

## 必要条件（全部满足）

1. **单位规模达标**
   - `unitDefs.Count >= 21`（当前已达 37）
   - 新增单位须覆盖至少一个当前弱势职业或阵营组合。

2. **数据完整性门禁通过**
   - `UNITDEF_SMOKE` 全绿：
     - name/class/origin 非空
     - cost 在 1~5
     - hp/atk/spd > 0
     - range 在 1~6
     - `basePool` 与 `unitDefs` 数量一致且 key 唯一

3. **路线命中可观测且稳定**
   - `COMP_HIT_PROBE` 多路线样本全通过（至少 3 条路线）
   - 观察 `avgClass/avgOrigin` 不出现长期接近 0 的路线。

4. **关键流程回归全绿**
   - `DevRunRegression3FloorsBatch()` 总门禁通过
   - 包含：3关流程 + UI烟雾 + 升星专项 + 配置校验 + 单位定义完整性 + 路线命中压测

## 建议条件（非硬门槛）

- 新单位对应路线在锁定推荐中可被命中（不是“入池即隐形”）。
- 扩充后不显著恶化前期经济/节奏（通过 balance 回归抽样观察）。

## 执行建议

- 每次批量加单位后，先跑一轮 Batch；
- 若 `COMP_HIT` 偏低，优先小步调整：
  - `GetLockedCompClassBiasByLevel`
  - `LockedCompOriginBias`
  - `compDefs.focusClasses/focusOrigins`
- 调参后重复 Batch，确认是“增益”不是“漂移”。
