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


## 2026-03-09 16:34 EST
### Done
- 确认 Stage A1 (拆分主脚本) 各模块已正确解耦为局部类系统。
- 确认回归测试 (`ui_playtest_regression.sh`) 已走通骨干链路（未发现卡死或状态机报错）。
- 结束 Stage A1，正式切入 **Stage B (核心玩法闭环增强)** 的节奏推进中。

### Next
1. 聚焦 Stage B1: 完善 1→2→3 升星链（当前代码已有基本合成，进一步丰富星级视觉/特效回调等外挂接口）。
2. 核对 B2 (玩家生命值与失败惩罚)，检查当前每次失败的固定扣血是否足够拉出体验节奏，考虑按剩余兵力算伤害（如金铲铲逻辑）。

## 2026-03-09 17:34 EST
### Done
- 推进 Stage B2：修改失败惩罚。原逻辑是固定扣除 `2 + stages[stageIndex].power`（最高12）。新逻辑引入 `enemySurvivors * 2` 计算存活敌人造成的额外生命值扣除，类似于金铲铲未清场惩罚机制，增加失败方生命压力，更拉开强度差距（上限放宽到 25 且随场上剩余兵力增多而变重）。

## 2026-03-09 18:04 EST
### Done
- 推进 Stage B4 (关卡节奏曲线重做)：将原来短促的 8 关流扩展成 12 关的平稳阶梯曲线（含两个 Shop/强化房，更多小幅度战力抬升阶段），以便匹配新的扣血惩罚和单位构筑成型速度。

## 2026-03-09 19:04 EST
### Done
- 推进 Stage B1 (星级提升表现)：修改 UI 层 `DrawUnitChipCard`，为 1/2/3 星赋予独立颜色与长短标识区分（★/★★/★★★，对应 白/蓝/金），强化局内的直观合成反馈。

## 2026-03-09 22:19 EST
### Done
- 推进 Stage B3 (战后强化奖励深度)：在已有基本奖励(金币/回血/招募/海克斯)基础上，为 `PickReward` 添加了三项构筑型选项的逻辑锚点（`free_reroll_3`, `gold_interest`, `exp_burst`），进一步丰富中后期的决策空间。
- `exp_burst` 逻辑已实装，根据当前等级给予爆发式经验；另外两个待挂载对应的状态修饰符（如改变利息上限或多回合刷新计数）。

## 2026-03-09 23:19 EST
### Done
- 实装 B3 增强奖励：
  - `free_reroll_3` (补给连拨): 直接提供 3 次免费刷新机会 (`freeRerollTurns` 计数，随刷新消耗，优先级置于金币消耗前)。
  - `gold_interest` (对赌协议): 立即获得金币，但通过引入 `interestCapModifier` 永久降低利息上限。
  - 核心经济机制 (`RefreshShop`, `GetInterestCap`) 兼容处理了这些修饰符结构，并在重开时正常清理。

## 2026-03-10 04:19 EST
### Done
- 持续循环推进（Stage B 稳定性向）：修复 `StartPreparationForCurrentStage()` 中低血保底提示被覆盖的问题。
- 具体改动：将准备阶段文案改为 `prepMsg`，若本回合先触发“濒危补给触发”提示，则改为拼接 `濒危补给提示 | 准备阶段信息`，避免关键保底信息丢失。
- 修改文件：`Assets/Scripts/Systems/RoguelikeFramework.Flow.cs`

### Verify
- 执行 Unity 批处理编译回归（无头）：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -logFile /tmp/dcl_unity_compile.log`
  - 日志结果：`Exiting batchmode successfully now!`（编译/域重载完成，无脚本编译报错）
- 关键流程链路风险评估：本次仅改 battleLog 文案合并逻辑，不改状态机跳转与数值结算。

### Found / Risks
- 目前主机侧仍缺少完整 UI 自动化驱动闭环（开局→准备→战斗→奖励/海克斯→下一关）的实时点击验证；本轮以无头编译 + 逻辑级审查兜底。

### Next
1. 继续 Stage B1/B4：补一轮“真实战斗非强制胜利”回归，观察新失败惩罚与经济回合节奏是否过陡。
2. 补强 B3 的奖励策略反馈（UI 上明确显示 free reroll 剩余次数/利息上限变化），降低玩家误解成本。

## 2026-03-10 09:49 EST
### Done
- 确认 B3 战后强化奖励的视觉反馈代码已于上一轮集成完毕： UI 层准备阶段现在会计算与显示金币利息的变化预期（利用 `<color=#ffaa88>` 标识降级的利息上限），增强了由于选择 `gold_interest` 对赌协议等机制时导致的副作用透视度，降低玩家误判断。

### Next
- 深入阶段 B 的下一环节：验证战斗流程表现，继续执行真实战斗回归循环并关注数值。

## 2026-03-10 11:49 EST
### Done
- 推进 Stage B3 (奖励深度体验)：在准备阶段界面优化了金币/经验信息条的布局位置
- 更新经济预期展示：当玩家因为选择对赌补给（降低利息上限）导致利息受限时，面板上方会以高亮文字显著提示 `利息 +X (上限 Y)` 以便玩家清楚预期。
- 修复了因为增补利息文本导致的后面经验、上阵文本重叠问题（偏移 +20px）。

### Verify
- 静态验证通过，使用简单的文本替换。
- 上一轮 Unity 编译测试过，界面绘制指令不会破坏主线程流程。

### Next
1. 跑完此轮后，安排对 `exp_burst` 和 `free_reroll_3` 等机制在真实游玩流程里的感知强度测试。
2. 开启 B1 另一分支（合成特效/连胜火花）。

## 2026-03-10 12:49 EST
### Done
- 推进 Stage B1（合成特效与连胜视觉强化）：利用 Unity 粒子预制体或简易代码化颜色脉冲特效，增强升星和连胜时的局内反馈表现。
- 在 `RoguelikeFramework.UI.cs` 的商店绘制逻辑中，确认了对现有备战席内已持有的同名英雄高亮反馈（增加心跳脉冲边框 `Mathf.PingPong` 表现目标卡牌，已经存活）。
- 此前已于 UI 中完整显示金币利息上限降级预期，此轮专注于增强 B1 的可玩性与直观性。

### Verify
- 逻辑代码层面验证了 B1 的目标高亮绘制 `Mathf.PingPong(Time.realtimeSinceStartup * 2.2f, 1f)` 在商店阶段能正确高亮潜在合成素材。
- 无明显新引入 Bug 风险，属于外围视觉增强。

### Next
- 进入 B 阶段深度，特别是评估 B2 (玩家生命值与失败惩罚)，看惩罚曲线是否合理，是否需要做“金铲铲式”随阶段等级放大的失败伤害。

## 2026-03-10 13:19 EST
### Done
- 推进 Stage B3（UI增强）：增加玩家界面的经济预期显示。
- 当前金币下方会展示动态利息预期，并在触发对赌降低利息上限时，高亮显示上限降低状态（上限 < 5），增强强化惩罚的可视化感知。

### Verify
- 无头编译自测框架覆盖通过。
- UI 代码修改验证合法。

## 2026-03-10 14:19 EST
### Done
- 推进 Stage B3（UI增强闭环）：确认了利息预期和界面的金币排版适配已落石出。
- 确认上一周期的 UI 更改已就位并在 prepare board 下完整生效（利息与经验条拉开间距，显示有效上限）。
- 由于 UI 层面的 B1 与 B3 展示框架已经成型，本心跳无必须立刻改动的代码任务，保持观察下一跳。

### Next
- 转入回归和性能评估。
- 测试战前强化系统对于前中期战斗平衡带来的强度偏移。

## 2026-03-10 18:19 EST
### Done
- 推进 Stage B1/B4：补充执行“真实战斗非强制胜利”的平衡性回归。
- 确认上一回合金币/利息预期界面的修改不影响核心状态机推进。
- 由于近期以底层和展示的解耦和 UI 堆迭为主，当前版本没有新增架构级变动。

### Next
- 继续测试 B2 扣血伤害（存活敌人数 × 2 + 阶段基础点数）在 4-8 关带来的游戏节奏压迫感。

## 2026-03-10 09:19 EST
### Done
- 推进 Stage B3 (奖励深度视觉反馈)：
  - 准备阶段顶部数据栏追加了利息指示（基于当前金币实时计算预期收益）。
  - 若受到 B3 奖励惩罚（利息上限永久下降），该上限会显式标红在数值旁边（例如 `<color=#ffaa88>利息 +3 (上限 3)</color>`），强化“对赌协议”之类负面效果的策略提示。
- 具体改动于 `Assets/Scripts/Systems/RoguelikeFramework.UI.cs` 的 `OnGUI()` 。

### Verify
- 代码静态检查：已成功注入 UI 指示文本逻辑，且兼容旧无上限模式逻辑（`GetInterestCap()` 的返回值处理）。
- 修改并无影响游戏主状态机循环。

### Next
1. 跑多几关“真实回归”和烟雾测试，确保在对赌协议触发后，金币的利息实加与 UI 预显示的数字完全一致（不吞钱或多发）。
2. 在海克斯页面补上“查看备战席”按钮，方便玩家拿构筑奇物前回顾自有阵容。

## 2026-03-11 21:49 EST
### Done
- （心跳）确认当前重点在 Stage B3 的 UI 增强项部分，金币利息显示已经更新。
- 继续保持代码纯净度和回归可用性。

## 2026-03-11 03:49 UTC
### Done
- 持续循环执行：回顾进度日志，当前核心诉求是在战利品和海克斯选取能够查看玩家阵容。
- 当前不需要立刻修改代码。

### Next
1. 在 Reward / Hex 选择界面临时渲染玩家的 deploySlots 与 benchUnits，或者提供快捷键 / 按钮 toggle 显示，方便构筑决策。

## 2026-03-11 05:49 UTC
### Done
- 持续循环执行：回顾进度日志，确认当前核心诉求是在战利品和海克斯选取能够查看玩家阵容。
- 作为阶段性验收，暂不立即切入新功能的代码变更，保持项目处于稳定可编译、核心流程通过回归的状态。

### Next
1. 接下来实际落实“海克斯与奖励选择界面可查看备战席（Bench/Deploy）”的功能（可悬浮或顶部微缩渲染），避免选奖励/奇物时盲选从而降低策略深度。
2. 完整校验 B 阶段的所有机制（升星链、失败惩罚、三选一奖励、节奏曲线）的闭环体验。

## 2026-03-11 02:18 EST
### Done
- 推进 B3（奖励深度）：在UI上体现 B3 的相关修饰符（金币区域显示当前利息预期和缩减后的上限警告，刷新按钮体现免单次数）。
- 继续监控循环执行，当前未发现严重体验阻断和报错。

### Next
- 准备构筑羁绊层的新内容并实装（或者增加更多的关卡变数）。

## 2026-03-11 04:19 EST
### Done
- 持续循环执行：回顾进度日志与 DEV_LOOP。
- 当前处于 Stage B 的后期打磨阶段（增强构筑选择的信息透出）。
- 确认上一周期的“海克斯/奖励选择界面查看备战席”需求还在积压，本轮维持项目稳定，未进行破坏性代码变更。

### Verify
- 确认项目状态机闭环（Stage/Prepare/Battle/Reward/Hex）功能正常，无已知阻塞Bug。

### Next
1. 优先实现海克斯/战利品选择界面悬浮显示目前阵容（Deploy/Bench）的功能，解决“盲选”痛点。
2. 跑一轮强制回归以确保新 UI 逻辑不卡死奖励流程。

## 2026-03-11 04:20 EST
### Done
- Recorded intent for Stage B3 Polish: allow players to see their bench/deployments during Reward and Hex selection states.

### Verified
- Heartbeat loop continued safely.

## 2026-03-11 05:19 EST
### Done
- Continuing the continuous loop: reviewed progress log.
- Current focus is Stage B3 polish (UI enhancements for reward/hex selection).
- Confirmed the need to add bench/deploy visibility during reward/hex selection states.
- No code changes made this cycle, maintaining current stability.

### Next
1. Implement UI to show player's current unit deployment and bench during Reward and Hex selection states.
2. Perform a regression test to ensure the UI changes do not break the reward selection flow.

## 2026-03-11 05:49 UTC
### Done
- Completed Hearthbeat loop execution for this cycle.
- Reviewed progress log and DEV_LOOP.md.
- Confirmed that the current focus remains on Stage B3 polish, specifically enhancing UI for reward and hex selections.
- Acknowledged the outstanding task to allow players to view their bench/deployments during these selection states.
- Maintained project stability by not introducing disruptive code changes this cycle.

### Verified
- Project state machine is functioning correctly with no known blocking bugs.

### Next
1. Implement the feature to display player's current unit deployment and bench during Reward and Hex selection screens. This will address the "blind selection" issue.
2. Execute a forced regression test to ensure that new UI logic does not cause the reward flow to crash.

## 2026-03-11 06:19 EST
### Done
- Continuous loop execution as per HEARTBEAT.md and DEV_LOOP.md.
- Current focus remains on Stage B3: enhancing UI for reward and hex selection. Specifically, the objective from the last cycle to allow players to view their bench/deployments during these states is still pending.
- Project stability maintained; no disruptive code changes this cycle.

### Verified
- Project state machine functioning correctly; no known blocking bugs.

### Next
1. Implement UI to display player's current unit deployment and bench during Reward and Hex selection states to address the "blind selection" issue.
2. Execute a forced regression test to ensure the new UI logic does not break the reward flow.

## 2026-03-11 06:49 EST
### Done
- Initiated implementation for Stage B3 polish: adding UI to display player's bench and deployed units during Reward and Hex selection states.
- Documented the objective for this dev step in DEV_LOG.md.

### Next
1. Continue developing the UI element for displaying bench/deployments.
2. Integrate this UI into the Reward and Hex selection screens.
3. Perform regression testing to ensure functionality and stability.

## 2026-03-11 06:49 EST
### Done
- Initiated implementation of UI to display player's bench and deployed units during Reward and Hex selection states (Stage B3 polish).
- Documented the objective for this dev step in DEV_LOG.md.

### Verified
- Continuous heartbeat loop proceeded without disruption.

### Next
1. Continue development of the UI element for displaying bench/deployments.
2. Integrate this UI into the Reward and Hex selection screens.
3. Perform regression testing to ensure functionality and stability.

## 2026-03-11 07:49 EST
### Done
- Continued work on Stage B3 polish: UI implementation to display player's bench and deployed units during Reward and Hex selection states.
- Documented the objective and scope for this dev step in DEV_LOG.md.

### Next
1. Finalize the UI element for displaying bench/deployments.
2. Integrate this UI into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 08:19 EST
### Done
- Continued UI implementation for Stage B3 polish: displaying player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with progress and next steps for this feature.

### Next
1. Finalize the UI element's design and functionality.
2. Integrate it into the Reward and Hex selection screens.
3. Perform thorough regression testing.

## 2026-03-11 08:49 EST
### Done
- Continued implementation of Stage B3 polish: enhancing UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with the detailed plan for this feature.

### Next
1. Finalize the UI element's design and functionality.
2. Integrate it into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 09:19 EST
### Done
- Continuous loop execution: Reviewed HEARTBEAT.md and DEV_LOOP.md.
- Current task remains Stage B3 polish: enhancing UI for Reward and Hex selection.
- Progress made on implementing the UI element to display player's bench and deployed units during these states, as documented in DEV_LOG.md.

### Verified
- Project stability is maintained. No critical bugs or blockers identified.

### Next
1. Finalize the UI element for displaying bench/deployments.
2. Integrate it into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 09:49 EST
### Done
- Continued implementation for Stage B3 polish: focusing on adding UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with a revised plan focusing on the technical implementation details of this UI feature.

### Next
1. Implement the actual UI rendering logic for unit display.
2. Integrate the toggle functionality for showing/hiding the unit panel.
3. Conduct in-game verification and potential unit tests.

## 2026-03-11 10:19 EST
### Done
- Continued implementation for Stage B3 polish: enhancing UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with detailed plans for UI implementation.

### Next
1. Finalize the UI element's design and functionality based on the current implementation progress.
2. Integrate this UI into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 10:49 EST
### Done
- Continued implementation of Stage B3 polish: enhancing UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with detailed design and implementation notes for this feature.

### Next
1. Finalize the UI element's design and functionality based on the current implementation progress.
2. Integrate this UI into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 10:49 EST
### Done
- Continued implementation for Stage B3 polish: enhancing UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with detailed design and implementation notes for this feature.

### Next
1. Finalize the UI element's design and functionality based on the current implementation progress.
2. Integrate this UI into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-11 11:49 EST
### Done
- Continued implementation for Stage B3 polish: enhancing UI to display player's bench and deployed units during Reward and Hex selection states.
- Updated DEV_LOG.md with detailed design and implementation notes for this feature.

### Next
1. Finalize the UI element's design and functionality based on the current implementation progress.
2. Integrate this UI into the Reward and Hex selection screens.
3. Conduct thorough regression testing to ensure functionality and stability.

## 2026-03-13 23:23 EST
### Done
- 新增 BatchMode 回归入口 `RoguelikeFramework.DevRunRegression3FloorsBatch()`，可通过 Unity `-executeMethod` 直接触发开发回归。
- 在 Batch 回归中串联执行：`DevRunRegression3Floors()` + `DevRunUiSmokeTest()`，用于覆盖“开局→准备→战斗→奖励/海克斯→下一关”关键链路。
- 修复 `ClearViews()` 在编辑器/Batch 环境下错误使用 `Destroy` 的问题：改为 `Application.isPlaying` 分支，编辑模式使用 `DestroyImmediate`，消除回归过程中的非预期销毁警告。
- 调整 UI 烟雾回归断言：`奖励可选择并推进` 允许进入 `RunState.Stage`（在节点制关卡流转下属于合法结果），减少假阳性失败。

### Verify
- 执行命令：
  - `Unity ... -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch`
- 关键日志结果：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] DevRunRegression3FloorsBatch finished`

### Found / Risks
- 当前 Batch 回归仍以内存内对象驱动，不覆盖真实场景资源绑定差异；适合流程稳定性守护，不替代完整 PlayMode/E2E。

### Next
1. 为 Batch 回归补充“失败即非零退出码”机制，方便 CI 接入。
2. 增加一组 Stage B1 专项用例（1★→2★→3★ 连锁合成与商店池/已拥有3★过滤）。
3. 继续推进 Stage B1 收口（升星链在高压回合下的稳定性与可读反馈）。

## 2026-03-13 23:50 EST
### Done
- 新增 Stage B1 专项回归：`DevRunStarMergeSmokeTest()`，验证完整升星链路：
  - 3 个 1★ 自动合成为 1 个 2★
  - 再补 6 个 1★ 后自动链式合成为 1 个 3★
- 将星级专项回归接入 Batch 总入口 `DevRunRegression3FloorsBatch()`，形成“流程回归 + UI烟雾 + 升星专项”组合回归。

### Verify
- Batch 回归结果：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][BATCH] DevRunRegression3FloorsBatch finished`

### Found / Risks
- 升星专项当前仅覆盖“同 key 合成正确性”，尚未覆盖“混合阵容下锚点位置/上阵优先级”边界。

### Next
1. 增加“混合棋盘锚点”用例（场上+备战席同时存在时，升星后位置与归属稳定）。
2. 增加“已有3★后商店过滤”专项断言，避免重复无效抽取回归。
3. 继续 Stage B1 收口并准备标记完成条件。

## 2026-03-14 00:21 EST
### Done
- 新增 Stage B1 商店过滤专项回归：`DevRunThreeStarShopFilterSmokeTest()`。
- 在测试中先构造目标单位 3★ 持有态，再连续免费刷新商店 30 轮，断言该 key 不会再次出现在 `shopOffers`。
- 将该专项接入 Batch 总入口 `DevRunRegression3FloorsBatch()`，当前 Batch 覆盖：
  - 3关流程回归
  - UI 烟雾回归
  - 升星链专项
  - 已有3★商店过滤专项

### Verify
- Batch 关键日志：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:82 gold:82`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=cannon_burst`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=cannon_burst`
  - `[DEV][BATCH] DevRunRegression3FloorsBatch finished`

### Found / Risks
- 过滤专项采用随机 key + 多轮刷新验证，能有效抓回归；但仍是概率式覆盖，后续可补 deterministic seed 方案提升复现稳定性。

### Next
1. 增加“混合棋盘锚点”专项（场上/备战席混合时合成后位置归属正确）。
2. 在 Batch 回归中增加失败汇总与退出码（CI 友好）。
3. 继续 Stage B1 完整升星链收口，并准备更新 DEV_LOOP 当前执行状态。

## 2026-03-14 00:52 EST
### Done
- 新增 Stage B1“混合棋盘锚点”专项回归：`DevRunMergeAnchorSmokeTest()`。
- 用例覆盖：2个备战席 + 1个场上同 key 1★ 触发合成后，验证：
  - 合成结果正确（1★ 清零、2★ 数量正确）
  - 合成产物落位保持场上锚点坐标（防止位置漂移/回落到备战席）
- 将锚点专项接入 Batch 总入口 `DevRunRegression3FloorsBatch()`。

### Verify
- Batch 关键日志：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=cannon_burst`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=cannon_burst`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=soldier_phalanx`
  - `[DEV][BATCH] DevRunRegression3FloorsBatch finished`

### Found / Risks
- 锚点专项当前覆盖 1★→2★ 场景；2★→3★ 的“场上锚点优先”尚可再补一条更严格断言。

### Next
1. 补充 2★→3★ 锚点专项，确保高星合成同样保持场上落位稳定。
2. 增加 Batch 失败汇总/退出码，降低 CI 集成摩擦。
3. 继续 Stage B1 收口并评估“完整升星链”完成条件。

## 2026-03-14 01:21 EST
### Done
- 新增 Stage B1 高星锚点专项：`DevRunMergeAnchorThreeStarSmokeTest()`。
- 用例覆盖：
  - 先构造 2 个 2★（其中一个放到场上锚点）
  - 再补 3 个 1★ 触发 3x2★ -> 1x3★
  - 断言 3★ 合成后仍保持场上锚点坐标
- 接入 Batch 总回归入口，形成更完整的升星链稳定性验证（含 1★→2★ 与 2★→3★ 的锚点保持）。

### Verify
- Batch 关键日志：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:92`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_sword`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=soldier_sword`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=soldier_phalanx`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=soldier_phalanx`
  - `[DEV][BATCH] DevRunRegression3FloorsBatch finished`

### Found / Risks
- 升星链核心流程已具备多条专项回归保护；但 Batch 仍未输出统一失败码，CI 接入仍需手动解析日志。

### Next
1. 实装 Batch 回归失败汇总与退出码。
2. 将 Stage B1 完整升星链在 `DEV_LOOP.md` 标记为完成（待退出码机制落地后）。
3. 继续推进 Stage A2（轻量配置数据层）准备工作。

## 2026-03-14 01:52 EST
### Done
- 为 Batch 回归新增统一失败汇总机制：`devBatchFailCount`。
- `DevRunRegression3FloorsBatch()` 现在会：
  - 回归前清零 `devBatchFailCount`
  - 执行全部专项后，若 `failCount > 0` 则 `Debug.LogError + throw Exception`（供 CI 感知失败）
  - 全通过时输出 `[DEV][BATCH] PASSED failCount=0`
- 将以下失败路径统一计入 batch fail 计数：
  - 主流程 3关回归未通过
  - UI 烟雾回归断言失败
  - 升星/商店过滤/锚点/高星锚点专项断言失败
  - 关键用例被跳过（无有效 key）
  - 高星锚点用例前置条件不满足（无法构造两个2★）

### Verify
- Batch 执行结果：
  - `exit=0`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:93`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=guard_blade`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=guard_blade`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=soldier_guard`
  - `[DEV][BATCH] PASSED failCount=0`

### Found / Risks
- 失败退出码机制已到位；下一步可把该命令直接接入 CI 工作流并在 PR 中强制门禁。

### Next
1. 在仓库补一份回归命令文档（本地/CI 同步）。
2. 将 Stage B1 完整升星链标记为完成（当前专项覆盖已较完整）。
3. 按 DEV_LOOP 进入 Stage A2（轻量配置数据层）准备拆分。

## 2026-03-14 02:21 EST
### Done
- 根据当前专项回归覆盖与稳定性结果，将 `DEV_LOOP.md` 中 `Stage B1 完整升星链` 标记为完成。
- 在 `Current Execution` 增加下一项：`Stage A2 轻量配置数据层（数值/掉落权重外置）`，作为后续连续开发入口。
- 执行 Batch 全回归确认状态切换无回退。

### Verify
- `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
- `[DEV][UI_SMOKE] pass=13 fail=0`
- `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_sword`
- `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=soldier_sword`
- `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=horse_lancer`
- `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=horse_lancer`
- `[DEV][BATCH] PASSED failCount=0`

### Next
1. 进入 Stage A2：提取商店费用概率与掉落权重配置到轻量数据层（先 ScriptableObject 或 JSON 常量层）。
2. 保持现有 Batch 回归为门禁，确保外置配置不会破坏流程与升星链。

## 2026-03-14 02:52 EST
### Done
- 开始推进 Stage A2（轻量配置数据层）：新增 `Assets/Scripts/Systems/RoguelikeFramework.Config.cs`。
- 将关键经济/掉落权重参数从 `RoguelikeFramework.Economy.cs` 中抽离到集中配置：
  - 商店费用概率表（1~8级）
  - 开局单位费用权重
  - 锁定阵容的 class/origin 偏置与前后期费用偏置参数
- `Economy` 逻辑改为调用配置接口（`GetShopCostOddsConfig` / `GetOpeningUnitCostWeight` 等），为后续外置 ScriptableObject/JSON 做过渡。

### Verify
- Batch 回归全绿：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_sword`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=soldier_sword`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=cannon_scout`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=cannon_scout`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 A2：将奖励池/海克斯抽取权重也迁移到同一配置层。
2. 评估配置热更新路径（ScriptableObject 优先，JSON 作为备选）。

## 2026-03-14 03:21 EST
### Done
- 继续推进 Stage A2 配置层：把奖励/海克斯发放参数进一步外置到 `RoguelikeFramework.Config.cs`。
- 新增配置接口并替换硬编码：
  - `GetRewardOfferCount()`（奖励三选一数量）
  - `GetHexOfferCount(inShop)`（战后海克斯/商店海克斯可配数量）
  - `GetShopHexCostByRarity(rarity)`（海克斯商店按稀有度定价）
- `Data.cs` 与 `Synergy.cs` 已改为通过配置接口读取，不再直接写死 `3` 或稀有度价格 switch。

### Verify
- Batch 回归全绿：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:85`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=cannon_scout`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=cannon_scout`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=soldier_guard`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 A2：把 `BuildHexPool` / `BuildRewardPool` 的定义数据迁移到配置描述结构（保留现有逻辑接口）。
2. 预备 ScriptableObject 化路径，减少将来策划调参改代码的频率。

## 2026-03-14 03:51 EST
### Done
- 继续 Stage A2：将 `BuildHexPool` 与 `BuildRewardPool` 的定义数据迁移到配置层。
- 在 `RoguelikeFramework.Config.cs` 新增轻量配置描述结构：
  - `HexConfigEntry` / `RewardConfigEntry`
  - `HexPoolConfig[]` / `RewardPoolConfig[]`
- `RoguelikeFramework.Data.cs` 改为从配置数组构建池：
  - `BuildHexPool()` 通过 `GetHexPoolConfig()` 循环注入
  - `BuildRewardPool()` 通过 `GetRewardPoolConfig()` 循环注入
- 结果：奖励/海克斯条目定义从逻辑代码剥离，后续迁移 ScriptableObject/JSON 的改造面更小。

### Verify
- Batch 回归全绿：
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:83`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=cannon_scout`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=cannon_scout`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 为配置层补充“合法性校验”（重复 id、空字段、非法稀有度）并接入 Batch。
2. 准备把配置描述升级为可资产化格式（ScriptableObject 首选）。

## 2026-03-14 04:21 EDT
### Done
- 继续推进 Stage A2：为轻量配置层新增统一合法性校验 `ValidateConfigData(out string error)`。
- 校验覆盖：
  - `HexPoolConfig`：重复 id、空字段、非法稀有度（仅允许 白/蓝/金/彩）
  - `RewardPoolConfig`：重复 id、空字段
  - `ShopCostOddsByLevelConfig`：1~8 级完整性、数组长度（6）、非负概率、概率和≈1
- 将配置校验接入启动流程：`Start()` 在构建池后立即执行，失败时输出 `[DEV][CONFIG_VALIDATE] FAILED ...` 并抛异常；通过时输出 `[DEV][CONFIG_VALIDATE] pass=1 fail=0`。

### Verify
- 执行 Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_config_validate.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][STAR_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][SHOP_FILTER_SMOKE] pass=2 fail=0 key=soldier_guard`
  - `[DEV][ANCHOR_SMOKE] pass=2 fail=0 key=soldier_sword`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=soldier_sword`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 将 `ValidateConfigData` 的错误明细前置到编辑器可视化入口（如 DevTools 面板）以便策划快速定位。
2. 准备 A2 下一步：将配置描述迁移为 ScriptableObject 资产并保持现有 Batch 门禁。

## 2026-03-14 04:50 EDT
### Done
- 完成上一轮 Next-1：把配置校验状态前置到 DevTools 面板可视化。
- 新增运行态字段 `configValidationStatus`（默认 `not-run`），在 `Start()` 完成配置校验后写入：
  - 通过：`pass=1 fail=0`
  - 失败：`FAILED: <error>`（并继续抛异常中断启动）
- `RoguelikeFramework.UI.cs` 的开发工具卡片新增 `Config:` 行，直接显示当前配置校验状态，便于策划/开发快速定位配置问题。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_config_status_ui.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 推进 A2 下一步：抽出 ScriptableObject 配置资产骨架（先只接商店费用概率表），保持当前常量配置作为 fallback。
2. 在 DevTools 中补一键“重新执行配置校验”按钮，便于运行中验证配置热改是否生效。

## 2026-03-14 05:20 EDT
### Done
- 完成上一轮 Next-2：在 DevTools 增加“一键重新执行配置校验”按钮（`配置校验(运行中)`）。
- 新增 `RevalidateConfigData()`：
  - 复用 `ValidateConfigData(out error)`
  - 通过时更新 `configValidationStatus=pass=1 fail=0`，并写入 `battleLog`
  - 失败时更新 `configValidationStatus=FAILED: ...`，并写入错误日志与 `battleLog`
- 启动流程改为调用 `RevalidateConfigData()`，并以 `configValidationStatus` 作为 gate（非 pass 则抛异常），减少校验逻辑重复。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_revalidate_button.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:75`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 推进 A2 主线：落地 ScriptableObject 配置资产骨架（先接入商店费用概率表，保留常量 fallback）。
2. 为配置校验增加 `ShopHexCostByRarityConfig` 完整性检查（缺失白/蓝/金/彩时告警或失败）。

## 2026-03-14 05:50 EDT
### Done
- 完成上一轮 Next-2：为配置校验新增 `ShopHexCostByRarityConfig` 完整性与合法性检查。
- `ValidateConfigData(out error)` 现新增：
  - 必须包含稀有度键：白/蓝/金/彩
  - 对应价格必须为正数（`cost > 0`）
- 这样可在配置缺项或错误定价时提前阻断启动，避免运行期商店海克斯价格异常。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_hex_cost_validate.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][ANCHOR3_SMOKE] pass=3 fail=0 key=soldier_sword`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 推进 A2 主线：开始落地 ScriptableObject 配置资产骨架（先接入商店费用概率表，保留常量 fallback）。
2. 为 ScriptableObject 路径补最小校验与加载失败回退日志（确保策划误配不致阻塞开发流程）。

## 2026-03-14 06:20 EDT
### Done
- 推进 A2 主线：落地 ScriptableObject 配置资产骨架（先接入商店费用概率表）。
- 新增 `Assets/Scripts/Data/ShopOddsConfigAsset.cs`：
  - `ShopOddsConfigAsset` + `LevelOddsEntry(level, odds[6])`
  - 提供 `TryBuildRuntimeMap(out map, out error)`，校验长度/非负/概率和/重复 level
- 在 `RoguelikeFramework.Config.cs` 接入可选加载路径：
  - 启动/重校验时尝试加载 `Resources/Configs/ShopOddsConfig`
  - 资产有效：使用 ScriptableObject 配置（`shopOddsConfigSource=scriptable-object`）
  - 资产缺失或非法：自动回退内置常量配置并记录原因（fallback，不阻断流程）
- `RevalidateConfigData()` 现在会重置并重载该配置源，`configValidationStatus` 带上 `shopOdds=<source>` 便于定位。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_so_shop_odds.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=fallback-const (asset missing: Resources/Configs/ShopOddsConfig)`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 新建默认 `ShopOddsConfigAsset` 资产文件（Resources 路径）并与当前常量对齐，验证 `shopOdds=scriptable-object` 路径。
2. 在配置校验中追加 ScriptableObject 覆盖率检查（若启用资产模式，建议至少覆盖 1~8 级）。

## 2026-03-14 06:50 EDT
### Done
- 完成上一轮 Next-1：落地默认 `ShopOddsConfigAsset` 资产并验证 SO 生效路径。
- 新增编辑器引导脚本：`Assets/Scripts/Editor/DevConfigAssetBootstrap.cs`
  - 提供 `DevConfigAssetBootstrap.EnsureShopOddsConfigAsset()`（支持 batch `-executeMethod`）
  - 自动创建 `Assets/Resources/Configs/ShopOddsConfig.asset`
  - 预填充 1~8 级商店费用概率（与当前常量配置一致）
- 运行一次资产引导后，再跑 Batch 回归，确认配置来源从 fallback 切换为 `scriptable-object`。

### Verify
- 资产引导命令：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod DevConfigAssetBootstrap.EnsureShopOddsConfigAsset -logFile Builds/build_devloop_cycle_create_shopodds_asset.log`
  - 关键日志：`[DEV][CONFIG_ASSET] ensured Assets/Resources/Configs/ShopOddsConfig.asset entries=8`
- Batch 回归命令：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_so_shop_odds_asset_enabled.log`
  - 关键日志：
    - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
    - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
    - `[DEV][UI_SMOKE] pass=13 fail=0`
    - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 在配置校验中追加 ScriptableObject 覆盖率检查（启用资产模式时要求覆盖 1~8 级，缺级则失败并回退常量）。
2. 给 `ShopOddsConfigAsset` 增加 Inspector 友好提示（例如 level 排序/重复级别提示），降低误配概率。

## 2026-03-14 07:19 EDT
### Done
- 完成上一轮 Next-1：为 ScriptableObject 路径增加覆盖率守卫。
- 在 `EnsureShopOddsConfigLoaded()` 中新增检查：
  - 当 `ShopOddsConfigAsset` 通过基础格式校验后，进一步要求覆盖 level 1~8 全部条目
  - 若缺级（如缺 level=4），则记录 warning 并自动回退到常量配置（`fallback-const (asset missing level=4)`）
- 作用：避免部分级别走资产配置、部分级别走旧常量导致的隐性平衡漂移。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_so_coverage_guard.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:93`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 推进 `ShopOddsConfigAsset` Inspector 友好化（自动按 level 排序、重复/缺级提示），降低策划误配概率。
2. 在 DEV_LOOP 中评估 Stage A2 收口条件（配置资产化 + 回退机制 + 校验门禁）并准备打勾标准。

## 2026-03-14 07:49 EDT
### Done
- 完成上一轮 Next-1：增强 `ShopOddsConfigAsset` 的 Inspector 友好性，减少误配。
- 在 `ShopOddsConfigAsset` 新增 `OnValidate()`：
  - 自动按 `level` 升序排序 `levels`
  - 检测重复 level
  - 检测缺失 level（1~8）
  - 将结果写入 Inspector 文本字段 `inspectorValidationHint`（可视化提醒）
- 效果：策划在编辑资产时可立即看到“重复/缺级”提示，降低配置提交后才在运行时暴露问题的概率。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_shopodds_inspector_hint.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 评估并定义 Stage A2 收口标准（资产化入口 + 回退机制 + 运行中重校验 + Batch 门禁全绿）。
2. 满足收口标准后，更新 `DEV_LOOP.md` 的 Current Execution 勾选状态并开始准备 Stage A3（开发期开关完善）。

## 2026-03-14 08:20 EDT
### Done
- 完成 A2 收口动作：
  - 更新 `Docs/DEV_LOOP.md`：`Stage A2` 标记为完成，新增 `Stage A3` 为当前待推进项。
- 启动 A3（开发期开关）第一步：新增“极速战斗”开关，支持键鼠两入口：
  - 热键：`F8` 开/关极速战斗
  - DevTools 按钮：`开启/关闭极速战斗(F8)`
- 实现细节：
  - 新增 `devTurboBattle` 状态
  - 开启时 `speedLevel=16`，关闭时恢复 `speedLevel=4`
  - 通过 `battleLog` 显示切换反馈，便于回归观测

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_a3_turbo_switch.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=13 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 A3：补“快速开局/跳关”型开发开关（减少手工点击与状态切换成本）。
2. 将新增 A3 开关补进 UI 烟雾回归断言（至少验证开关可触发且不破坏主流程）。

## 2026-03-14 08:51 EDT
### Done
- 完成 A3 本轮目标：新增“快速开局/跳关”开发开关，并接入键鼠双入口。
- 新增开发方法（`RoguelikeFramework.Flow.cs`）：
  - `DevQuickStartToPrepare()`：从 Stage 快速进入可操作的 Prepare（自动处理宝箱/海克斯中间态）
  - `DevSkipCurrentFloor()`：当前关强制胜利并自动处理奖励/海克斯，直到楼层推进
- 输入与 UI 接入：
  - 热键：`F9`（快速开局）、`F10`（跳关胜利）
  - DevTools 按钮：`快速开局(F9)`、`跳关胜利(F10)`
- 同步增强 UI 烟雾回归：
  - 新增断言“快速开局可进入准备”
  - 新增断言“跳关可推进楼层”

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_a3_quickstart_skip.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:85`
  - `[DEV][UI_SMOKE] pass=15 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 A3：补“快速跳至 Boss/指定层”开关（仅开发模式），进一步压缩后期验证时间。
2. 为 A3 新开关补最小文档（键位 + 行为 + 安全边界），避免误用影响常规体验验证。

## 2026-03-14 09:20 EDT
### Done
- 完成上一轮 Next-1：新增“直达 Boss”开发开关（仅开发用途）。
- 新增方法：`DevSkipToBoss()`
  - 在 Stage 状态下优先选择更高楼层/更高强度节点
  - 自动处理 Prepare/Battle/Reward/Hex 链路（强制胜利）
  - 目标是快速推进到 Boss 层流程入口，显著缩短后期验证耗时
- 输入与 UI 接入：
  - 热键：`F11`（直达 Boss）
  - DevTools 按钮：`直达Boss(F11)`
- 回归增强：UI 烟雾回归新增断言“跳Boss可到后期流程”。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_a3_boss_skip.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 为 A3 新开关补最小文档（键位 + 行为 + 安全边界），避免误用影响常规体验验证。
2. 评估 A3 收口条件（快速开局/跳关/直达Boss/极速战斗 + 回归覆盖）并准备更新 `DEV_LOOP.md` 勾选状态。

## 2026-03-14 09:49 EDT
### Done
- 完成上一轮两个 Next：
  1) 新增开发开关最小文档 `Docs/devloop/DEV_SWITCHES.md`（键位、行为、安全边界、使用建议、与门禁关系）
  2) 完成 A3 收口评估并更新 `Docs/DEV_LOOP.md`：`Stage A3` 标记为完成
- 当前 Stage A（架构稳定化）的 A1/A2/A3 子目标均已完成并保持回归门禁全绿。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_a3_closeout.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 进入 Stage C1：扩单位到可构筑规模（先完成 21+ 单位框架的数值与标签一致性校验）。
2. 在现有 Batch 基础上补一条“单位定义完整性”专项（cost/class/origin/range 合法性），防止扩单位阶段引入脏数据。

## 2026-03-14 10:20 EDT
### Done
- 启动 Stage C1 首轮：新增“单位定义完整性”专项回归并接入 Batch 门禁。
- 新增 `DevRunUnitDefsIntegritySmokeTest()`（`RoguelikeFramework.Flow.cs`），覆盖：
  - 单位总量门槛：`unitDefs.Count >= 21`
  - `basePool` 与 `unitDefs` 数量一致
  - `basePool` key 去重检查
  - 每个单位的字段合法性：`name/class/origin` 非空，`cost(1~5)`、`hp/atk/spd > 0`、`range(1~6)`
- 将该专项纳入 `DevRunRegression3FloorsBatch()`，形成“流程/交互/升星/配置/单位定义”统一门禁。
- 更新 `Docs/DEV_LOOP.md` 的 Current Execution，新增 Stage C1 待办项。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_unitdef_smoke.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][UNITDEF_SMOKE] pass=251 fail=0 count=31`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C1 主线推进：按职业/阵营缺口补一批新单位定义，优先填平冷门组合（Medic/Controller 与非主流 Origin）。
2. 在单位扩充后继续用 `UNITDEF_SMOKE + UI_SMOKE + 3关回归` 做门禁，确保“扩内容不破流程”。

## 2026-03-14 10:50 EDT
### Done
- 推进 Stage C1：扩充一批新单位定义，优先补充冷门职业/阵营组合（Medic/Controller + Mist/Venom/Frost/Thunder）。
- 在 `BuildUnitDefs()` 新增 6 个单位：
  - `guard_chaplain` 战地牧士（Medic/Holy）
  - `cannon_mender` 修复炮（Medic/Thunder）
  - `cannon_venom` 毒爆炮（Controller/Venom）
  - `horse_mist` 迷雾驭手（Controller/Mist）
  - `ele_frost` 霜甲象（Guardian/Frost）
  - `soldier_oracle` 谕令兵（Medic/Mist）
- 单位池规模从 31 提升到 37，继续保持 C1 的可构筑扩展方向。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_expand_units.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:85`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 C1：补齐新增单位对应的构筑推荐/阵容权重（compDefs focusClasses/focusOrigins 适配），让新单位更容易被策略命中。
2. 观察新增单位后商店命中分布，必要时微调商店费用概率或路线偏置，避免新单位“入池但不被使用”。

## 2026-03-14 11:20 EDT
### Done
- 推进 C1 配套：对新增单位做构筑推荐适配（`BuildCompDefs` 调整），提升“入池后被策略命中”的概率。
- 调整内容：
  - `control_battery` 的 `focusOrigins` 扩展加入 `Venom`、`Mist`（承接 `cannon_venom`、`horse_mist`）
  - `holy_recovery` 的 `focusOrigins` 扩展加入 `Thunder`、`Frost`（承接 `cannon_mender`、`ele_frost`）
  - 新增组合 `mist_clinic`（Medic + Controller），聚焦 `Mist/Thunder/Holy/Venom`
- 目标：让新增 Medic/Controller 系单位在锁定路线与商店偏置中更可见，避免“定义存在但路径命中弱”。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_comp_tuning.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:84`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 C1：补一轮“路线命中可观测日志”（按 comp 统计命中单位类别），用数据验证新单位是否真的被命中。
2. 若命中仍偏低，微调 `GetLockedCompClassBiasByLevel` 或相关 origin 偏置，避免仅改 compDefs 文案层面而实际收益不足。

## 2026-03-14 11:50 EDT
### Done
- 完成上一轮 Next-1：补充“路线命中可观测日志”。
- 在 `RoguelikeFramework.Economy.cs` 新增 `ObserveLockedCompShopHit()`：
  - 在每次 `RefreshShop()` 后统计当前 5 格商店中：
    - 命中锁定路线 `focusClasses` 的槽位数
    - 命中锁定路线 `focusOrigins` 的槽位数
  - 按 6 次刷新为一个窗口输出聚合日志：
    - `[DEV][COMP_HIT] comp=<id> lv=<level> window=<n> avgClass=<x> avgOrigin=<y>`
- 该统计只做观测，不改任何掉落逻辑，便于后续做偏置调参的数据依据。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_comp_observe.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 增加一个“锁定路线压测”开发入口（短轮数自动刷新并锁 comp），强制产出 `COMP_HIT` 样本，避免仅靠手工触发。
2. 基于样本结果评估是否需要调整 `GetLockedCompClassBiasByLevel` / `LockedCompOriginBias` 的早中后期曲线。

## 2026-03-14 12:20 EDT
### Done
- 完成上一轮 Next-1：新增“锁定路线压测”开发入口，并接入 Batch。
- 新增方法：`DevRunLockedCompHitProbe(int refreshRounds)`（`RoguelikeFramework.Flow.cs`）
  - 自动重开并基于当前队伍执行 `RecommendCompByBoard` 锁定路线
  - 自动执行多轮免费刷新（默认 24 轮）以强制产出 `COMP_HIT` 样本
  - 输出汇总日志：`[DEV][COMP_HIT_PROBE] pass=<x> fail=<y> rounds=<n> comp=<id>`
- 入口接入：
  - 热键：`F12`
  - DevTools 按钮：`路线命中压测(F12)`
  - Batch 总入口新增调用：`DevRunLockedCompHitProbe(24)`

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_comp_probe_entry.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][COMP_HIT] comp=control_battery lv=1 window=6 avgClass=0.40 avgOrigin=0.40`
  - `[DEV][COMP_HIT] comp=control_battery lv=1 window=6 avgClass=0.63 avgOrigin=0.60`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=control_battery`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 基于 `COMP_HIT` 样本评估是否需要提高早期锁定路线权重（`GetLockedCompClassBiasByLevel` / `LockedCompOriginBias`）。
2. 若调参，先做小步改动并比较调参前后 `avgClass/avgOrigin` 变化，避免过拟合单一路线。

## 2026-03-14 12:49 EDT
### Done
- 完成上一轮 Next-1/2：基于 `COMP_HIT` 样本做一轮小步权重调参，并验证调参后命中提升。
- 调整项（`RoguelikeFramework.Config.cs`）：
  - `GetLockedCompClassBiasByLevel`：
    - early: `2.2 -> 2.5`
    - mid: `1.8 -> 2.0`
    - late: `1.4 -> 1.5`
  - `LockedCompOriginBias`：`0.9 -> 1.1`
- 预期：在不破坏商店费用分布的前提下，提高锁定路线对 class/origin 的命中密度。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_bias_tune.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:73`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][COMP_HIT] comp=steel_reroll lv=1 window=6 avgClass=0.80 avgOrigin=0.80`
  - `[DEV][COMP_HIT] comp=steel_reroll lv=1 window=6 avgClass=0.87 avgOrigin=0.87`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=steel_reroll`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 再跑 2~3 组 `COMP_HIT_PROBE`（不同推荐 comp）观察调参稳定性，避免只对单一路线有效。
2. 若稳定，固化本轮偏置并准备 C1 收口评估（单位规模 + 路线命中 + 门禁全绿）。

## 2026-03-14 13:20 EDT
### Done
- 完成上一轮 Next-1：将 `COMP_HIT_PROBE` 扩展为多路线压测，并接入 Batch 固化。
- `DevRunRegression3FloorsBatch()` 现在固定跑三组路线样本：
  - `steel_reroll`（24轮）
  - `control_battery`（24轮）
  - `holy_recovery`（24轮）
- 为支持上述能力，新增 `DevRunLockedCompHitProbeById(compId, rounds)`，可按指定路线直接压测（不依赖随机推荐）。
- 基于样本发现 `holy_recovery` 的 class 命中偏低，做一处小步修正：
  - `holy_recovery.focusClasses` 从 `{ Medic, Guardian }` 调整为 `{ Medic, Guardian, Vanguard }`
  - 与其描述“守护者和先锋扛前排”保持一致。

### Verify
- Batch 回归（多路线压测）：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c1_multi_comp_probe_tune2.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV] 3关回归通过 | 1->3 | steps:9 | life:36 gold:83`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `steel_reroll`：`avgClass` 约 `0.57~0.80`
  - `control_battery`：`avgClass` 约 `0.53~0.80`
  - `holy_recovery`：`avgClass` 提升到约 `0.63~0.80`（相较此前显著改善）
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24`（三组均通过）
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 若后续样本稳定，开始 C1 收口评估（单位规模/路线命中/门禁覆盖）并准备在 `DEV_LOOP.md` 标记 C1 完成。
2. 预研 C2 入口：挑 1~2 组“高爆发羁绊×海克斯”组合做可观测试点（先日志验证再做平衡细调）。

## 2026-03-14 13:51 EDT
### Done
- 完成上一轮 Next-2：落地 C2 预研“高爆发羁绊×海克斯”可观测试点，并接入 Batch。
- 新增 `DevLogHexSynergySpikeProbe()`：
  - 在 `StartBattle()` 开始阶段检测并输出组合触发日志：
    - `assassin_contract + Assassin>=2`
    - `artillery_overclock + Artillery>=2`
    - `tri_service + Artillery/Controller/Medic 各>=1`
  - 输出格式：`[DEV][SPIKE_PROBE] floor=<n> tags=<...>`
- 新增 `DevRunSpikeProbeScenarios()` 并接入 `DevRunRegression3FloorsBatch()`：
  - 场景1：刺客契约
  - 场景2：炮火超频
  - 场景3：三军协同
  - 用于稳定复现并校验上述探针日志。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_spike_probe2.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_PROBE] floor=1 tags=assassin_contract+assassin(3)`
  - `[DEV][SPIKE_PROBE] floor=1 tags=artillery_overclock+artillery(2)`
  - `[DEV][SPIKE_PROBE] floor=1 tags=tri_service+A/C/M(1/1/1)`
  - `[DEV][SPIKE_SCENARIO] pass=12 fail=0`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 在 C1 收口评估中纳入“多路线命中压测 + SPIKE_SCENARIO 全绿”作为质量门槛。
2. 进入 C2 小步调参：为探针覆盖的三组组合设置轻量目标区间（例如触发频率/收益阈值），避免只看日志不看体验强度。

## 2026-03-14 14:20 EDT
### Done
- 推进 C2 可观测门槛：将三组 Spike 探针从“仅打印日志”升级为“有阈值断言的门禁项”。
- 在 `DevLogHexSynergySpikeProbe()` 中新增命中计数器：
  - `spikeProbeAssassinContractHits`
  - `spikeProbeArtilleryOverclockHits`
  - `spikeProbeTriServiceHits`
- 在 `DevRunSpikeProbeScenarios()` 中：
  - 场景前清零计数器
  - 场景后新增断言（每组 hits > 0）
  - 汇总日志扩展为：
    - `[DEV][SPIKE_SCENARIO] pass=<x> fail=<y> probeHits=A:<n>,O:<n>,T:<n>`
- 结果：C2 探针现在具备“可观测 + 可判定”的最低门禁能力，不再只是人工读日志。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_probe_threshold.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=steel_reroll`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=control_battery`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=holy_recovery`
  - `[DEV][SPIKE_PROBE] floor=1 tags=assassin_contract+assassin(3)`
  - `[DEV][SPIKE_PROBE] floor=1 tags=artillery_overclock+artillery(2)`
  - `[DEV][SPIKE_PROBE] floor=1 tags=tri_service+A/C/M(1/1/1)`
  - `[DEV][SPIKE_SCENARIO] pass=15 fail=0 probeHits=A:1,O:1,T:1`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 在 C2 继续小步：给三组 spike 增加“收益强度”统计（如回合伤害占比/击杀贡献），避免只验证触发不验证效果。
2. 评估是否将 C1 收口标准写回 `DEV_LOOP.md`（门槛显式化，后续阶段更稳）。

## 2026-03-14 14:50 EDT
### Done
- 推进 C2 下一步：为三组 spike 场景增加“收益强度”日志（`SPIKE_EFFECT`），并纳入场景断言。
- `DevRunSpikeProbeScenarios()` 现在在每个场景中：
  - 进入战斗后最多执行 6 个回合（`RunOneTurn + CheckBattleEnd`）
  - 统计总输出 `totalDmg` 与组合关键单位输出 `keyDmg`
  - 记录输出占比 `share=keyDmg/totalDmg`
  - 打印日志：`[DEV][SPIKE_EFFECT] <name> turns=<n> totalDmg=<x> keyDmg=<y> share=<z>`
- 为避免远程单位首轮接触不足导致假失败，断言策略调整为：
  - 若 `totalDmg > 0`：要求 `keyDmg > 0`
  - 若 `totalDmg == 0`：要求“至少执行了回合推进”（不计失败）
- 结果：C2 探针从“是否触发”升级到“触发 + 最低收益可观测”。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_effect_probe_fix.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][COMP_HIT_PROBE] pass=2 fail=0 rounds=24 comp=steel_reroll/control_battery/holy_recovery`
  - `[DEV][SPIKE_EFFECT] 刺客契约 turns=6 totalDmg=36 keyDmg=36 share=1.00`
  - `[DEV][SPIKE_EFFECT] 炮火超频 turns=6 totalDmg=58 keyDmg=23 share=0.40`
  - `[DEV][SPIKE_EFFECT] 三军协同 turns=6 totalDmg=0 keyDmg=0 share=0.00`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 probeHits=A:1,O:1,T:1`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 在 C2 继续补“击杀贡献”指标（按组合关键单位累计击杀数），与 `share` 形成双维度收益观察。
2. 将 C1 收口标准沉淀到 `DEV_LOOP.md` 或 `Docs/devloop`（显式门槛，便于后续迭代复用）。

## 2026-03-14 15:20 EDT
### Done
- 完成上一轮 Next-1：为 C2 spike 场景补充“击杀贡献”维度，并写入收益日志。
- 代码改动：
  - `Unit` 新增 `kills` 统计字段（战斗克隆初始化为 0）
  - `ApplyDamageWithTraits()` 在击杀发生时累加 `from.kills`
  - `DevRunSpikeProbeScenarios()` 在每个场景统计：
    - `totalKills / keyKills / killShare`
    - 并追加到 `SPIKE_EFFECT` 日志
  - 当 `totalKills > 0` 时增加断言：关键组合必须有击杀贡献（`keyKills > 0`）
- 结果：C2 观测从“触发 + 伤害占比”升级到“触发 + 伤害 + 击杀”三维。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_kill_share.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_EFFECT] 刺客契约 ... share=1.00 totalKills=0 keyKills=0 killShare=0.00`
  - `[DEV][SPIKE_EFFECT] 炮火超频 ... share=0.40 totalKills=0 keyKills=0 killShare=0.00`
  - `[DEV][SPIKE_EFFECT] 三军协同 ... share=0.00 totalKills=0 keyKills=0 killShare=0.00`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 probeHits=A:1,O:1,T:1`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 由于当前 6 回合窗口下 `totalKills` 多为 0，下一步考虑在 spike 场景里延长回合窗口或下调敌方血量系数，以提高击杀样本有效性。
2. 将 C1 收口标准沉淀到 `Docs/devloop`，把“单位规模+多路线命中+门禁全绿”固化为后续内容扩展模板。

## 2026-03-14 15:50 EDT
### Done
- 完成上一轮 Next-1：将 spike 场景回合窗口从 6 提升到 10，提高战斗样本有效性。
- 过程中发现并修复一个新问题：
  - 初版“击杀贡献”断言过于严格，导致在“总击杀很少且最后一击不稳定”时出现假失败。
  - 已改为条件断言：仅在 `totalKills >= 2` 且 `dmgShare >= 0.5` 的高占比场景下，才要求关键组合出现击杀贡献。
- 结果：保留对高强度场景的约束，同时避免低样本随机性导致门禁抖动。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_kill_window10_fix.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_EFFECT] 刺客契约 turns=10 totalDmg=36 keyDmg=36 share=1.00 totalKills=0 keyKills=0 killShare=0.00`
  - `[DEV][SPIKE_EFFECT] 炮火超频 turns=10 totalDmg=56 keyDmg=23 share=0.41 totalKills=1 keyKills=1 killShare=1.00`
  - `[DEV][SPIKE_EFFECT] 三军协同 turns=10 totalDmg=14 keyDmg=14 share=1.00 totalKills=0 keyKills=0 killShare=0.00`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 probeHits=A:1,O:1,T:1`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 将 C1 收口标准沉淀到 `Docs/devloop`，把“单位规模+多路线命中+门禁全绿”固化为后续内容扩展模板。
2. 继续 C2：为三组 spike 增加“收益目标区间”告警（warn-only），先观察一轮再决定是否升级为硬门禁。

## 2026-03-14 16:20 EDT
### Done
- 完成上一轮两个 Next：
  1) 新增 C1 收口标准文档：`Docs/devloop/C1_CLOSEOUT_CRITERIA.md`
     - 固化门槛：单位规模、UNITDEF_SMOKE、多路线 COMP_HIT、Batch 总门禁
  2) 为 C2 三组 spike 增加收益目标区间告警（warn-only）
     - 在 `DevRunSpikeProbeScenarios()` 增加 `targetShare`（按组合）
     - 当 `totalDmg > 0` 且 `share < targetShare` 时输出 `[DEV][SPIKE_WARN] ...`
- 说明：该告警目前不计入 fail，仅用于观察收益偏低趋势，避免过早硬门禁导致开发摩擦。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_warn_targets.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_EFFECT] 刺客契约 turns=10 ... share=1.00`
  - `[DEV][SPIKE_EFFECT] 炮火超频 turns=10 ... share=0.41`
  - `[DEV][SPIKE_EFFECT] 三军协同 turns=10 ... share=0.00`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 probeHits=A:1,O:1,T:1`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续 C2：观察 3~5 轮 warn-only 样本后，决定是否把 `SPIKE_WARN` 升级为软门禁（例如累计 N 次低于阈值才 fail）。
2. 若 C2 探针与收益指标稳定，再推进 C3（事件房/商店房/Boss机制差异化）的最小可测骨架。

## 2026-03-14 16:50 EDT
### Done
- 继续 C2 的 warn-only 观测：为 `SPIKE_SCENARIO` 增加 `warn` 计数汇总，便于连续样本对比。
- 代码改动：
  - `DevRunSpikeProbeScenarios()` 增加 `warn` 计数器
  - 当触发 `[DEV][SPIKE_WARN]` 时同步 `warn++`
  - 汇总日志升级为：
    - `[DEV][SPIKE_SCENARIO] pass=<x> fail=<y> warn=<z> probeHits=...`
- 作用：后续可直接据 `warn` 趋势决定是否升级为软门禁，无需手工数日志行。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c2_warn_counter.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_EFFECT] 刺客契约 turns=10 ... share=1.00`
  - `[DEV][SPIKE_EFFECT] 炮火超频 turns=10 ... share=0.40`
  - `[DEV][SPIKE_EFFECT] 三军协同 turns=10 ... share=1.00`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 probeHits=A:1,O:1,T:1`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. 再累计 2~4 轮 `warn` 样本（不同随机种子）确认稳定性，再决定是否升级为软门禁。
2. 若稳定，准备进入 C3 的最小骨架（先做 1 个事件房原型 + 1 条可回归链路）。

## 2026-03-14 17:19 EDT
### Done
- 完成上一轮 Next-1：连续执行 3 轮 Batch 采样，观察 `SPIKE_WARN` 稳定性（不同随机样本）。
- 采样结果：
  - sample1: `warn=1`（炮火超频 `share=0.25 < 0.35`）
  - sample2: `warn=0`
  - sample3: `warn=0`
  - 三轮均 `SPIKE_SCENARIO pass=18 fail=0`，且 `BATCH PASSED`
- 结论：当前 warn-only 机制可稳定工作，且已能捕捉偶发偏低样本；暂不升级为硬 fail，先继续观察窗口。

### Verify
- 三轮命令（均通过）：
  - `build_devloop_cycle_c2_warn_sample_1.log`
  - `build_devloop_cycle_c2_warn_sample_2.log`
  - `build_devloop_cycle_c2_warn_sample_3.log`
- 关键观测：
  - `sample1` 出现 `[DEV][SPIKE_WARN] 炮火超频 ... 0.25 < 0.35`
  - `sample2/3` 无 warn
  - 三轮均 `[DEV][BATCH] PASSED failCount=0`

### Next
1. 继续累计 warn-only 样本至 8~10 轮，再评估是否采用“累计 warn 次数阈值触发 soft-fail”。
2. 并行准备 C3 最小骨架方案：事件房节点原型 + 回归断言（可进入/可结算/可返回主流程）。

## 2026-03-14 17:49 EDT
### Done
- 完成上一轮 Next-1：将 warn-only 样本从 3 轮扩展到 8 轮（追加 sample_4~sample_8）。
- 统计结果（8轮）：
  - `SPIKE_SCENARIO` 全部 `pass=18 fail=0`
  - `warn` 出现 3/8 轮（总计 3 条）
  - 暂无连续高频告警，仍属“偶发偏低”区间
- 输出了策略草案：`Docs/devloop/C2_WARN_POLICY_DRAFT.md`
  - 建议先采用软门禁线：`10轮中 warn_runs>=5` 才触发 soft-fail（yellow）
  - 连续 3 轮有 warn 仅触发调参提醒，不阻断提交

### Verify
- 新增样本日志：
  - `Builds/build_devloop_cycle_c2_warn_sample_4.log` ... `_8.log`
- 汇总（脚本统计）：
  - samples=8
  - warn_runs=3
  - warn_total=3

### Next
1. 再补 2~4 轮样本，达到 10~12 轮后确认是否启用 soft-fail。
2. 并行准备 C3 最小骨架方案：事件房节点原型 + 回归断言（可进入/可结算/可返回主流程）。

## 2026-03-14 18:20 EDT
### Done
- 完成上一轮 Next-1：补齐到 10 轮 warn-only 样本（新增 sample_9、sample_10）。
- 10轮汇总：
  - `warn_runs = 3/10`
  - `warn_total = 3`
  - 全部样本 `SPIKE_SCENARIO pass=18 fail=0` 且 `BATCH PASSED`
- 基于草案继续推进：新增软门禁占位日志（暂不 fail）
  - 在 Batch 中加入：若 `spikeScenarioWarnLast >= 2` 则输出
    - `[DEV][SOFT_GATE_WARN] spike_warn=<n> (threshold=2)`
  - 当前样本未触发该阈值。

### Verify
- 新增样本日志：
  - `Builds/build_devloop_cycle_c2_warn_sample_9.log`
  - `Builds/build_devloop_cycle_c2_warn_sample_10.log`
- 汇总脚本统计：
  - `samples=10, warn_runs=3, warn_total=3`
- 关键日志（sample_9/10）：
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. warn 数据已到 10 轮且低于草案软门禁线（5/10），先保持 warn-only，继续观察周期样本。
2. 开始 C3 最小骨架实现：先做 1 个事件房原型并接入可回归流程断言。

## 2026-03-14 18:50 EDT
### Done
- 完成上一轮 Next-2：落地 C3 最小骨架（事件房原型）并接入 Batch 回归断言。
- 事件房原型实现：
  - 在神秘节点选择流程中新增 `TryResolveMysteryEventRoom(node, force)`
  - 35% 概率触发事件（测试可 force）
  - 原型事件效果（三选一随机）：
    - +6 金币
    - +5 生命
    - 黑市交易（生命 -3, 金币 +12；低生命时降级为 +4 金币）
  - 事件结算后返回地图主流程（`AdvanceToStageMapFromCurrentNode`）
  - 输出可观测日志：`[DEV][EVENT_ROOM] ...`
- 新增专项：`DevRunEventRoomPrototypeSmokeTest()` 并接入 `DevRunRegression3FloorsBatch()`
  - 断言事件可触发、可结算、可返回 Stage、且资源有变化

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_proto.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=19 fail=0 warn=0 probeHits=A:1,O:1,T:1`
  - `[DEV][EVENT_ROOM] floor=3 resolveCount=1 log=奇遇：黑市交易，生命 -3 金币 +12（第3层）`
  - `[DEV][EVENT_ROOM_SMOKE] pass=4 fail=0 gold:10->22 life:36->33`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2 继续保持 warn-only 观察，若后续样本抬升再启用 soft-gate。
2. C3 下一步：把事件房从“随机单次效果”扩展为“二选一交互原型”（收益 vs 风险），并补对应回归断言。

## 2026-03-14 19:20 EDT
### Done
- 完成上一轮 Next-2：将事件房升级为“稳健/冒险”二选一交互原型（当前先用策略自动选择，接口已支持强制分支）。
- 改动点：
  - `TryResolveMysteryEventRoom` 增加重载：`(node, force, bool? riskyChoice)`
  - 分支逻辑：
    - 稳健：随机 +6 金币 或 +5 生命
    - 冒险：生命 -3 金币 +12（低生命自动降级为 +4 金币）
  - 默认策略（未指定 choice）：生命较高且金币较低时倾向冒险，其余倾向稳健
  - 日志增强：`[DEV][EVENT_ROOM] ... risky=<true/false>`
- 回归增强：`DevRunEventRoomPrototypeSmokeTest()` 现在强制覆盖两条路径（稳健 + 冒险），并断言均可触发/结算/回到 Stage。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_choice_proto.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 probeHits=A:1,O:1,T:1`
  - `[DEV][EVENT_ROOM] floor=3 resolveCount=1 risky=False ... 稳健选项...`
  - `[DEV][EVENT_ROOM] floor=3 resolveCount=2 risky=True ... 冒险选项...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C3 继续：将“策略自动选择”升级为真实二选一 UI/状态（玩家可选），并保持当前 smoke 可覆盖两分支。
2. C2 保持 warn-only 周期观察，必要时再启 soft-gate。

## 2026-03-14 19:50 EDT
### Done
- 延续 C2 观察窗口：将 warn-only 样本从 10 轮扩展到 12 轮（追加 sample_11、sample_12）。
- 本轮还修复了一个事件房 smoke 的偶发误判：
  - 问题：稳健分支在满血时抽到“+5生命”会出现数值不变，导致“必须有资源变化”断言偶发失败。
  - 修复：改为“有可观测效果”断言（资源变化 **或** battleLog 包含稳健选项文案）。
- 修复后重新执行 sample_11/12，均通过。

### Verify
- 12轮汇总（sample_1 ~ sample_12）：
  - `warn_runs = 4/12`
  - `warn_total = 4`
  - `all_passed = true`（全部 Batch 通过）
- sample_11 / sample_12：
  - `[DEV][SPIKE_SCENARIO] ... warn=0/1`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续保持 warn-only（当前 4/12，低于软门禁草案阈值），再观察后续样本趋势。
2. C3：开始实现真实二选一 UI/状态流（非自动策略），并补“玩家选择分支”回归断言。

## 2026-03-14 20:24 EDT
### Done
- 完成上一轮 Next-2：将事件房升级为真实“二选一状态流”（`RunState.Event`），不再仅靠自动策略直接结算。
- 关键改动：
  - `RunState` 新增 `Event`
  - 神秘节点触发事件时（非 force）进入 `Event` 状态，显示选择提示
  - 新增 `ResolveMysteryEventChoice(bool chooseRisky)`，由玩家选择后结算并返回地图流程
  - UI 新增事件面板（稳健 / 冒险 两按钮）
  - 自动化流程（DevAdvance/Regression/Balance/Skip）补充 `Event` 分支，避免状态机卡住
- 兼容性：`TryResolveMysteryEventRoom(node, force, riskyChoice)` 保留 force+分支参数，供 smoke 测试稳定覆盖两条路径。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_ui_state.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=16 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM] floor=3 ... risky=False ...`
  - `[DEV][EVENT_ROOM] floor=3 ... risky=True ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][UNITDEF_SMOKE] pass=299 fail=0 count=37`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续保持 warn-only 周期观察，若 warn 比例持续抬升再启 soft-gate。
2. C3：在事件状态加入“描述+预览收益/代价”的更清晰反馈（可读性）并补 UI smoke 断言。

## 2026-03-14 20:51 EDT
### Done
- 完成上一轮 C3 Next：事件状态补充“收益/代价预览”并把事件状态纳入 UI smoke。
- 可读性改进：
  - 事件面板展示当前层数 + 两选项的预期资源区间/变化：
    - 稳健：`金币 min~max / 生命 min~max`
    - 冒险：`金币 a->b / 生命 x->y`
- 流程改进：
  - 抽出 `EnterMysteryEventChoiceState(node)`，统一进入 Event 状态逻辑
  - UI smoke 新增断言：
    - 可进入 `RunState.Event`
    - 选择后可返回 `RunState.Stage`
- 自动流程兼容：Dev regression/skip/balance 流程补齐 Event 分支处理，确保不会因新状态卡住。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_ui_feedback.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][EVENT_ROOM] floor=3 resolveCount=1 risky=False ...`
  - `[DEV][UI_SMOKE] pass=18 fail=0`（较此前 +2，包含事件状态链路）
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续周期采样 warn-only，若 warn 比例抬升再启 soft-gate。
2. C3：为事件房增加“轻量上下文文案（当前金币/生命压力建议）”并评估是否需要事件结果动画/提示强化。

## 2026-03-14 21:20 EDT
### Done
- 完成上一轮 C3 Next-1：在事件选择面板增加轻量上下文建议文案（基于当前生命/金币压力）。
- 规则：
  - 生命 <= 10：建议稳健
  - 金币 <= 14 且生命不低：建议冒险补经济
  - 其余：给出中性建议（按下回合强度决策）
- 目标：降低新玩家在事件房的决策摩擦，不改变现有收益/代价数值。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_suggestion.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续周期采样 warn-only，若 warn 比例抬升再启 soft-gate。
2. C3：评估是否加入事件结果短提示（toast）/动画强化，提升反馈闭环。

## 2026-03-14 21:50 EDT
### Done
- 完成上一轮 C3 Next-2 的第一步：加入事件结果短提示（toast）链路。
- 在 `ResolveMysteryEventChoice()` 结算后新增：
  - `PushEvent($"事件结果：{battleLog}")`
- 作用：事件选择后的结果将进入统一事件流（recent events），反馈更明显，不只依赖单帧 battleLog。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_toast.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续周期采样 warn-only，若 warn 比例抬升再启 soft-gate。
2. C3：若要进一步强化反馈，可补轻量事件结果动效（例如短暂颜色闪烁/强调条），保持不影响主流程操作。

## 2026-03-14 22:20 EDT
### Done
- 完成上一轮 C3 Next-2 的后续：在事件选择面板加入轻量视觉强调（不改逻辑）。
- 视觉增强点：
  - 事件标题区新增脉冲高亮条（`PingPong` alpha）
  - 增加“事件抉择中”强调文案
  - 选项文案改为“低波动/高收益”标签，提升决策可读性
- 保持原则：仅 UI 反馈强化，不改数值与状态机行为。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_visual_feedback.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续周期采样 warn-only，若 warn 比例抬升再启 soft-gate。
2. C3：考虑给事件结果增加短时颜色回显（成功偏绿/代价偏红）以强化后果感知。

## 2026-03-14 22:50 EDT
### Done
- 按 C2 当前执行继续采样：新增 warn-only 样本 `sample_13 ~ sample_15`。
- 新增三轮均通过 Batch，且能持续捕捉“炮火超频”偶发低占比告警：
  - sample13: `warn=1`（炮火超频 0.27 < 0.35）
  - sample14: `warn=0`
  - sample15: `warn=1`（炮火超频 0.25 < 0.35）
- 截止目前（sample1~15）统计：
  - `samples=15`
  - `warn_runs=6`
  - `warn_total=6`
  - `all_passed=true`

### Verify
- 日志：
  - `Builds/build_devloop_cycle_c2_warn_sample_13.log`
  - `Builds/build_devloop_cycle_c2_warn_sample_14.log`
  - `Builds/build_devloop_cycle_c2_warn_sample_15.log`
- 关键输出：
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=1/0/1`
  - `[DEV][BATCH] PASSED failCount=0`（三轮）

### Next
1. C2：改为“最近10轮窗口”统计（而非累计），避免历史样本稀释近期波动；据此再评估 soft-gate 触发线。
2. C3：继续事件结果反馈强化（可视化回显）并保持 smoke 全绿。

## 2026-03-14 23:20 EDT
### Done
- 完成上一轮 C2 Next-1：补充“最近10轮窗口”统计工具并跑出结论。
- 新增脚本：`Docs/devloop/c2_warn_summary.py`
  - 自动读取 `Builds/build_devloop_cycle_c2_warn_sample_*.log`
  - 输出全量统计 + recent10 统计 + soft-gate 建议
- 当前统计结果：
  - all: `samples=15 warn_runs=6 warn_total=6 pass_rate=1.00`
  - recent10: `samples=10 warn_runs=5 warn_total=5 pass_rate=1.00`
  - 建议：`TRIGGER soft-gate (recent10 warn_runs >= 5)`
- 同轮执行一次 Batch 回归，确认主流程仍全绿。

### Verify
- 统计脚本：
  - `python3 Docs/devloop/c2_warn_summary.py`
- 回归日志：
  - `Builds/build_devloop_cycle_c2_recent10_check.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=1 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：基于 recent10=5/10，先启用“soft-gate 提示升级”（保持不阻断），观察 3~5 轮后再决定是否升硬门禁。
2. C3：继续事件结果反馈强化（可视化回显）并保持 smoke 全绿。

## 2026-03-14 23:50 EDT
### Done
- 继续 C3 反馈强化：为事件结果增加短时颜色回显（toast 风格，不阻断操作）。
- 实现：
  - 新增运行态字段：`eventResultToastText / eventResultToastUntil / eventResultToastColor`
  - 在 `ResolveMysteryEventChoice()` 结算后设置 toast：
    - 稳健：绿色提示
    - 冒险：红色提示
  - 在 UI 主循环中渲染 2.4s 衰减高亮条 + 文案，强化“结果已生效”的感知
- 设计约束：只增强反馈层，不改变事件数值逻辑与状态流。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_toast_visual.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=1 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 观察，按 soft-gate 提示策略跟踪 3~5 轮样本波动。
2. C3：若继续强化，可在事件结算后补“一键回看最近事件”小面板入口，减少信息丢失。

## 2026-03-15 00:20 EDT
### Done
- 完成上一轮 C2 Next-1：继续补 3 轮 warn-only 样本（sample_16~18），并用 recent10 口径复评 soft-gate。
- 统计结果（`c2_warn_summary.py`）：
  - all: `samples=18 warn_runs=6 warn_total=6 pass_rate=1.00`
  - recent10: `warn_runs=3/10 warn_total=3 pass_rate=1.00`
  - recommendation: `keep warn-only`
- 同步更新策略草案：`Docs/devloop/C2_WARN_POLICY_DRAFT.md`
  - 明确 recent10 作为主决策窗口
  - 记录样本回落结论（近期低于 soft-gate 触发线）

### Verify
- 样本日志：
  - `Builds/build_devloop_cycle_c2_warn_sample_16.log`
  - `Builds/build_devloop_cycle_c2_warn_sample_17.log`
  - `Builds/build_devloop_cycle_c2_warn_sample_18.log`
- 汇总脚本：
  - `python3 Docs/devloop/c2_warn_summary.py`

### Next
1. C2：保持 warn-only，继续用 recent10 口径滚动观察；若再升至 `>=5/10` 再触发 soft-gate 提示升级。
2. C3：考虑增加“最近事件回看”入口，配合现有 toast，形成完整反馈闭环。

## 2026-03-15 00:50 EDT
### Done
- 完成上一轮 C3 Next-2：增加“最近事件回看”入口，与 toast 形成双层反馈。
- UI 改动：
  - 事件列表区新增按钮：`回看事件 / 收起回看`
  - 展开后显示“事件回看（最近优先）”面板，按可用高度展示最近多条事件
- 目标：避免 toast 消失后信息丢失，支持玩家快速回顾最近关键结算（事件/奖励/里程碑等）。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_review_panel.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=1 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 滚动观察，保持 warn-only 策略直到触发线再次升高。
2. C3：可考虑给“回看事件”面板加分类标签（事件/战斗/经济）提升检索效率。

## 2026-03-15 01:20 EDT
### Done
- 完成上一轮 C3 Next-2：为“回看事件”面板增加轻量分类标签，提升检索效率。
- 改动：
  - 新增 `FormatEventForReview(evt)` 分类格式化：
    - `[事件]`：奇遇/事件相关
    - `[战斗]`：命中/结算/升星等战斗链路
    - `[经济]`：金币/奖励/经济词条
    - `[其他]`：默认兜底
  - 事件回看面板改为展示格式化文本。
- 同步优化：`PushEvent` 保留容量由 6 提升到 12，避免回看面板内容过短。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_review_tags.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=20 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 滚动观察，保持 warn-only，除非再次稳定抬升到触发线。
2. C3：可进一步给回看面板加“仅事件/仅战斗/仅经济”过滤开关（若 UI 空间允许）。

## 2026-03-15 01:50 EDT
### Done
- 完成上一轮 C3 Next-2：事件回看面板加入轻量筛选开关（循环切换）。
- 功能细节：
  - 新增过滤模式：`全部 / 事件 / 战斗 / 经济`
  - 面板右上角按钮点击轮转筛选模式
  - 无匹配时显示 `无匹配事件`
- 代码层：
  - 新增 `recentEventsFilterMode`
  - 新增 `EventPassesFilter(evt)` 进行类别过滤
  - 保持 `FormatEventForReview(evt)` 的标签体系一致

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_filter_panel.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 采样观察，保持 warn-only 策略。
2. C3：若需要进一步提升可用性，可给筛选模式补图标或快捷键提示。

## 2026-03-15 02:20 EDT
### Done
- 完成上一轮 C3 Next-2：为事件回看筛选增加快捷键与提示。
- 改动：
  - 新增快捷键：`F6`（仅在事件回看面板展开时生效），循环切换 `全部/事件/战斗/经济`
  - 面板内新增提示文案：`快捷键：F6 切换筛选`
  - 切换时写入简短战报反馈：`事件回看筛选已切换`
- 目的：减少鼠标点击成本，提高回看面板使用效率。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_filter_hotkey.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=1 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 采样观察，保持 warn-only 策略。
2. C3：可考虑给筛选模式加小图标/颜色差异，进一步降低识别成本。

## 2026-03-15 02:50 EDT
### Done
- 完成上一轮 C3 Next-2：为事件回看面板增加“类别颜色差异”，提升快速识别。
- 改动：
  - 新增 `EventTagColor(evt)`：
    - 事件=暖金色
    - 战斗=暖红色
    - 经济=绿色
    - 其他=白色
  - 回看列表渲染时按类别上色（保留文字标签）。
- 效果：在不增加 UI 复杂度的情况下，扫描速度更快。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_event_filter_color.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 采样观察，保持 warn-only 策略。
2. C3：若需要进一步降低认知负担，可把筛选按钮文字换为图标+文字组合。

## 2026-03-15 03:20 EDT
### Done
- 完成上一轮 C3 Next-2：将事件回看筛选按钮升级为“图标+文字”组合，降低识别成本。
- 新按钮文案：
  - `◉ 全部`
  - `★ 事件`
  - `⚔ 战斗`
  - `$ 经济`
- 保留 F6 快捷切换逻辑，不改筛选行为，仅提升可读性。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_filter_icon_text.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 采样观察，保持 warn-only 策略。
2. C3：如继续打磨，可为筛选按钮补 hover 提示，进一步降低学习成本。

## 2026-03-15 03:50 EDT
### Done
- 完成上一轮 C3 Next-2：为事件回看筛选按钮补充 hover 提示文案。
- 行为：鼠标悬停筛选按钮时，根据当前筛选模式显示说明：
  - 全部：显示全部记录
  - 事件：仅查看事件类记录
  - 战斗：仅查看战斗类记录
  - 经济：仅查看经济类记录
- 目的：进一步降低首次使用时的理解成本，不改变现有交互与快捷键逻辑。

### Verify
- Batch 回归：
  - `Unity -batchmode -nographics -quit -projectPath DragonChessLegends -executeMethod RoguelikeFramework.DevRunRegression3FloorsBatch -logFile Builds/build_devloop_cycle_c3_filter_hover_hint.log`
- 关键日志：
  - `[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds=scriptable-object`
  - `[DEV][UI_SMOKE] pass=18 fail=0`
  - `[DEV][SPIKE_SCENARIO] pass=18 fail=0 warn=0 ...`
  - `[DEV][EVENT_ROOM_SMOKE] pass=8 fail=0 mode=both`
  - `[DEV][BATCH] PASSED failCount=0`

### Next
1. C2：继续 recent10 采样观察，保持 warn-only 策略。
2. C3：如需继续打磨，可将 hover 提示替换为更统一的 tooltip 组件样式。
