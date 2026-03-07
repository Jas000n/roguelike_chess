# DragonChessLegends Progress Log

## 2026-03-07 02:52 EST
### Done
- 新增玩家生命值系统（战败扣血，0血结束）
- 战后流程改为“奖励三选一”并支持奖励后衔接海克斯
- 新建持续迭代路线图 `Docs/DEV_LOOP.md`
- 新增 2星→3星 自动合成功能（优先级高于1星→2星）
- 配置 HEARTBEAT 持续开发循环，避免空转

### Found / Risks
- `RoguelikeFramework.cs` 体量仍然过大，后续改动冲突风险高
- 目前缺少自动化测试，仅靠手动回归
- 奖励池内容深度仍偏浅（需要更多构筑向奖励）

### Next
1. 拆分主脚本（先抽离 Reward/Economy/Battle 逻辑）
2. 增加 run 内失败惩罚的节奏调参（避免前期劝退）
3. 增加构筑型奖励（羁绊定向/职业强化/单位复制）
Current Flow: Checked repository structure and DEV_LOOP.md. Identified Stage A1 (splitting main script) as next priority. Added scaffolding FlowController.

## 2026-03-07 03:03 EST
### Done
- 优化海克斯三选一：默认不再出现已选择海克斯，提升构筑分支差异
- 增加防御逻辑：若出现重复海克斯，自动转化为 +4 金币，避免流程/手感异常
- 增加兜底：若海克斯池被拿空，可从全池重抽，避免后期流程断裂

### Verify
- 代码级验证：检查 `RollHexOffers`/`PickHex` 调用链仍在原状态机路径（Reward->Hex->Prepare）
- 关键风险检查：无新增外部依赖；仅改海克斯发放逻辑，不影响战斗计算分支

### Found
- 仍缺少可脚本化的快速回归入口（当前主要靠手动进入场景验证）

### Next
1. 加一个最小“开发快捷按钮/开关”（跳准备、快速开战、日志摘要）
2. 用该开关跑 3 关流程，重点验证海克斯->战斗->奖励->下一关闭环

## 2026-03-07 03:06 EST
### Done
- 新增开发回归快捷能力：`开发推进一步`（按状态机单步推进 Stage/Prepare/Battle/Reward/Hex/GameOver）
- 新增 `开发重开`，并抽出 `RestartRun()`，替换原 GameOver 内重复重开代码
- 保持改动最小，仅触及流程控制与调试入口，不改战斗数值逻辑

### Verify
- 静态链路检查：`DevAdvanceOneStep()` 覆盖全部 RunState，避免悬空状态
- 代码回归检查：重开逻辑统一到 `RestartRun()`，GameOver 按钮已改为同一路径
- 影响面检查：无新增资源依赖、无场景/Prefab 绑定变更

### Found
- 当前仍缺“自动跑3关并输出摘要”的轻量回归脚本（下一步可以加）

### Next
1. 增加最小自动回归（自动推进并记录每关结果）
2. 跑一轮海克斯/奖励闭环并检查异常日志
3. 继续做海克斯与羁绊联动质量提升（优先高区分度）

## 2026-03-07 03:10 EST
### Done
- 新增最小自动回归入口 `自动回归3关`
- 实现 `DevRunRegression3Floors()`：自动推进 Stage->Prepare->Battle->Reward->Hex 闭环，默认跑 3 关并输出摘要
- 回归中准备阶段会自动补上阵（`AutoDeployFallback`），避免空阵容导致假失败

### Verify
- 静态验证：方法覆盖全部 RunState，含 safety 防死循环
- 结果可观测：写入 `battleLog` + `Debug.Log`，可直接在游戏内和控制台查看回归结果
- 影响面：仅开发验证入口，不改战斗数值和正式流程分支

### Found
- 目前是“强制胜利式回归”，适合流程稳定性验证，不代表平衡性验证

### Next
1. 增加“真实战斗回归模式”（不强制胜利，跑到战斗结束）
2. 修一个高价值反馈点：战斗失败原因可读化（伤害来源/关键触发提示）
