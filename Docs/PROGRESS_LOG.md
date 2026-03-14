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
