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

## 2026-03-07 03:11 EST
### Done
- 强化战斗结算反馈：新增 `BuildBattleOutcomeDetail()`
- 胜利日志增加双方存活数量
- 失败日志增加敌方剩余生命与“关键威胁单位（最高输出）”信息，帮助理解失败原因

### Verify
- 静态验证：`EndBattle()` 新增信息仅依赖已有 `playerUnits/enemyUnits` 战斗态数据
- 风险检查：不改结算奖励、不改扣血公式，仅增强可解释反馈

### Found
- 当前关键威胁只看“造成伤害最高”，下一步可加入“承伤最高/控制触发”维度

### Next
1. 在战斗统计面板补充“失败复盘摘要”（MVP伤害/承伤）
2. 做一轮真实战斗回归（非强制胜利）验证日志一致性

## 2026-03-07 03:33 EST
### Done
- 推进 Stage A1：把 `RoguelikeFramework` 改为 `partial class`，开始按职责拆分文件
- 新建 `Assets/Scripts/Systems/RoguelikeFramework.Flow.cs`
- 将流程相关逻辑迁移到 Flow 文件：
  - 开发回归入口：`DevAdvanceOneStep` / `DevRunRegression3Floors` / `RestartRun`
  - 核心状态机流转：`StartPreparationForCurrentStage` / `StartBattle` / `SpawnEnemiesForStage` / `EndBattle`
  - 战斗结算复盘：`BuildBattleOutcomeDetail`
- 原 `RoguelikeFramework.cs` 保留主数据结构与系统逻辑，流程职责解耦第一步完成

### Verify
- 静态检查：上述方法在主文件中不再重复定义，已由 Flow 分文件单点维护
- 影响面评估：仅做代码组织重构，不改数值和状态机行为分支
- 仓库检查：当前变更仅涉及 `RoguelikeFramework.cs` 与新增 `RoguelikeFramework.Flow.cs`

### Found / Risks
- 仍未进行 Unity Editor 实机回归（本轮为结构重构，下一轮需跑完整闭环）
- 其余职责（Economy/Battle细节/UI）仍在主文件，后续仍需继续拆分

### Next
1. 在 Unity 内跑一轮强制 3 关回归 + 一轮真实战斗回归，确认拆分无行为回归
2. 继续 Stage A1：抽离 Economy/Shop 相关方法到独立 partial 文件
3. 逐步补“最小可脚本化回归”入口，减少每次改动后的手工验证成本

## 2026-03-07 04:35 EST
### Done
- 执行持续迭代第1轮（稳定性）：清理 Unity 过时 API 告警
- `GameBootstrap` 从 `FindObjectOfType` 升级为 `FindFirstObjectByType`
- `AutoGameGenerator` 中两处 `FindObjectsOfType` 升级为 `FindObjectsByType(..., FindObjectsSortMode.None)`

### Verify
- 批处理 Build 成功（见 `Builds/build_devloop_cycle1.log`）
- 旧告警（上述 3 处）不再出现

### Next
1. 持续迭代第2轮：提升核心可玩深度（经济/构筑决策）
2. 设计并实现“可脚本化真实战斗回归”摘要，减少手工试错
3. 开始 Stage A2（数据配置外置，降低单文件改动风险）

## 2026-03-07 12:15 EST
### Done
- 玩法深化（Steam 品质向）：新增“阵营羁绊”并接入真实战斗数值，不再只有职业羁绊
  - 新增阵营计数与中文映射：`CountOrigin`, `GetOriginCn`, `GetUnitsOfOriginText`
  - Tooltip / 羁绊摘要加入阵营信息，构筑反馈更完整
  - 战斗公式接入阵营效果：
    - `Blaze`：阵营内增伤
    - `Steel`：阵营内减伤
    - `Thunder`：阵营内速度加成
    - `Night`：首击追加伤害（每单位一次）
    - `Shadow`：概率额外暴击增伤
- 单位状态扩展：新增 `usedOriginProc`，用于一次性阵营触发管理
- 保持开发验证闭环：新增后重新批处理构建通过（`build_devloop_cycle9.log`）

### Verify
- Build 成功：`Builds/build_devloop_cycle9.log`
- 真机触发 50 轮平衡回归（键位 `B`）后，日志新增数据：
  - 2026-03-07 12:04:09：8关通关 25/50，平均到达 7.76，平均生命 5.5
  - 2026-03-07 12:04:28：8关通关 23/50，平均到达 7.78，平均生命 4.3
  - 报告位置：`~/Library/Application Support/DefaultCompany/DragonChessLegends/DevReports/balance_50_report.log`

### Found
- 自动截图权限弹窗会打断“点击+截图”流水线，但不影响构建和日志型回归
- `4先锋`触发率仍偏低（已从 0 出现到偶发），需继续做低费先锋池和商店权重微调

### Next
1. 继续 50 轮 x 4 组平衡回归，目标：`4先锋`触发率 >= 10%
2. 商店概率重构（前中期提高 1~2费命中，后期再放开 4~5费）
3. 阵营羁绊 UI 面板独立卡片化（职业/阵营双栏），提升可读性与策略表达

## 2026-03-09 11:16 EST
### Done
- 按 Stage A1 继续拆分主脚本：将 `#region Hex / Synergy` 从 `RoguelikeFramework.cs` 抽离到新文件 `Assets/Scripts/Systems/RoguelikeFramework.Synergy.cs`。
- 主文件对应区域替换为“已迁移”注释，降低单文件体积与冲突面，便于后续继续拆 Economy/Battle/UI。

### Verify
- 静态检查：`RoguelikeFramework.cs` 中原 Hex/Synergy 方法已移除，方法定义集中在 `RoguelikeFramework.Synergy.cs`。
- 关键流程回归尝试：执行 `Tools/ui_playtest_regression.sh` 触发失败，原因为当前环境缺少 `screencapture` 命令（脚本在首张截图处中断，未进入完整回归链路）。

### Found / Risks
- 当前 host 环境不具备脚本截图能力，导致“开局→准备→战斗→奖励/海克斯→下一关”的自动 UI 回归无法完整跑通。
- 下一轮需要先为回归脚本加“无截图降级模式”或补齐截图依赖，避免阻塞持续心跳开发循环。

### Next
1. 为 `ui_playtest_regression.sh` 增加 screenshot capability 检测与降级（无 `screencapture` 时仅记录步骤日志，不中断）。
2. 在可运行环境重新执行关键链路回归，确认状态机闭环（Stage/Prepare/Battle/Reward/Hex）。
3. 继续 Stage A1：优先拆 `Units/Shop/Economy` 区域到独立 partial。

## 2026-03-09 12:28 EST
### Done
- 继续执行 Stage A1 (拆分主脚本)：将 `RoguelikeFramework.cs` 中的 `Units / Shop / Economy`、`Battle`、`GUI`、`Input`、`Setup Data` 等关键区域分别抽离到对应的 partial 文件中。
- `ui_playtest_regression.sh` 自适应了无截图环境（降级为跳过截屏并记录跳过日志），从而能够不中断流程完整跑完脚本步骤。

### Verify
- 手动回归了 `ui_playtest_regression.sh` 流程，日志显示其能够正常步进至 `prepare final snapshot`（尽管未能真正截图），说明 UI 交互指令能够正常发射。
- 静态检查代码确保 C# partial 类分割边界无语法残漏。

### Found / Risks
- `RoguelikeFramework.cs` 文件瘦身明显，但依赖关系还属于强耦合的单例，下一步 (Stage A2) 需整理外部数据与表现层的分离接口。

### Next
1. 考虑引入无头/单元测试式的状态机全流程测试（快速验证回合推进逻辑）。
2. 在真机上再进行一次构建（或者无头模式跑一遍），以确保刚拆解后的编译通过性并确认玩法表现在新架构下全绿。

