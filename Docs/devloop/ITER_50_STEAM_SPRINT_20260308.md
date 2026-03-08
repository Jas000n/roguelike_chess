# ITER_50_STEAM_SPRINT_20260308

目标：持续进行“新需求 -> 开发 -> 试玩截图 -> 修复 -> 新需求”的 50 次微迭代，朝 Steam 可上架品质推进。  
环境：macOS / Unity 6000.3.10f1 / 游戏内开发工具 + game-ui-playtester。

## 试玩证据（部分）
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/01_game_launch_20260308_013337.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/05_after_f2_20260308_013521.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/06_battle_state_20260308_013549.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/09_after_layout_refactor_stage_20260308_014410.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/11_after_layout_refactor_prepare_20260308_014450.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/12_after_layout_refactor_battle_20260308_014452.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/13_after_dev_balance50_20260308_014531.png`
- `/Users/jason/.codex/skills/game-ui-playtester/tmp-artifacts/iter50/14_after_balance_50_100_20260308_014639.png`

## 50 次微迭代记录
1. 需求：确认当前版本可交互。动作：启动 build 并截图基线。结果：确认阶段页可显示。
2. 需求：验证“进入准备”链路。动作：点击+热键 F2 进入 Prepare。结果：状态机可推进。
3. 需求：检查拖拽前可读性。动作：截图 Prepare 面板。结果：发现信息重叠。
4. 需求：检查战斗信息负载。动作：进入 Battle 并截图。结果：统计面板压住战场。
5. 需求：确认 Reward/Hex 流程。动作：用开发热键推进。结果：流程闭环正常。
6. 需求：聚焦 UI 主问题。动作：归纳“左中右层级混乱”。结果：确定重构方向。
7. 需求：减少顶部拥挤。动作：设计全宽顶栏与分区。结果：进入实现。
8. 需求：减少中心遮挡。动作：规划右侧停靠统计。结果：进入实现。
9. 需求：统一布局基线。动作：在 OnGUI 引入 left/right/center 布局变量。结果：主框架生效。
10. 需求：状态信息更集中。动作：状态框改到右上。结果：顶部层级更清晰。
11. 需求：开发按钮不遮挡主信息。动作：开发按钮按屏幕宽度重新锚定。结果：开发区可见。
12. 需求：海克斯文本不挤。动作：海克斯栏移到中上并扩宽。结果：换行更稳定。
13. 需求：准备阶段信息收敛。动作：摘要面板收回左列。结果：不再压住棋盘中心。
14. 需求：羁绊/路线面板减少遮挡。动作：右侧 dock。结果：战场中心可视空间提升。
15. 需求：战斗统计不挡战场。动作：BattleStats 移到右列。结果：单位动线更清楚。
16. 需求：战斗文本控制。动作：战斗说明盒宽度收窄。结果：画面拥挤感下降。
17. 需求：奖励页统计位置统一。动作：Reward 的 stats 同步右 dock。结果：结构一致。
18. 需求：回归确认。动作：build + 启动试玩截图。结果：布局重构通过。
19. 需求：增强“爽感”构筑。动作：新增海克斯 `lifesteal_core`。结果：支持吸血反打。
20. 需求：斩杀体验强化。动作：新增海克斯 `execution_edge`。结果：低血处决更明显。
21. 需求：刺客流上限。动作：新增海克斯 `assassin_bloom`。结果：刺客爆发提高。
22. 需求：新海克斯可被 AI 识别。动作：`ScoreHexForLockedComp` 增加评分。结果：推荐逻辑覆盖。
23. 需求：自动回归选海克斯不失真。动作：`DevPickHexIndex` 接入新海克斯评分。结果：回归决策更合理。
24. 需求：嗜血逻辑落地。动作：在 `ApplyDamageWithTraits` 加吸血回复。结果：战斗续航可见。
25. 需求：处决逻辑落地。动作：在 `RunOneTurn` 按目标血量动态增伤。结果：收割更连贯。
26. 需求：刺客海克斯落地。动作：刺客首击增伤+额外暴击。结果：刺客流手感提升。
27. 需求：泛用伤害小幅修正。动作：`execution_edge` 增加基础伤害系数。结果：后期体验平滑。
28. 需求：保持数值可控。动作：所有新增加成走已有 battle 链路。结果：低风险接入。
29. 需求：解决“先锋难凑”。动作：新增 1 费单位 `soldier_guard`。结果：前排成型更稳。
30. 需求：提升骑兵中期过渡。动作：新增 2 费 `horse_lancer`。结果：骑兵路线容错提高。
31. 需求：路线池扩展。动作：新增 `poison_cut`（刺客+炮手）。结果：中速收割流可玩。
32. 需求：路线池扩展。动作：新增 `steel_rider_wall`（先锋+骑兵）。结果：稳健运营流可玩。
33. 需求：自动回归阵容多样性。动作：DevCompPlan 新增 2 套路线。结果：压测分布更丰富。
34. 需求：双四可达成性。动作：双四优选列表加入 `soldier_guard`。结果：4先锋概率抬升。
35. 需求：易成型流一致性。动作：钢骑壁垒设为易成型低 reroll。结果：新手路径更稳。
36. 需求：刺客路线中期稳定。动作：毒影斩首设为中等 reroll。结果：波动下降。
37. 需求：验证重构不破。动作：Batch build（batch1）通过。结果：可构建。
38. 需求：验证新增内容不破。动作：Batch build（batch2）通过。结果：可构建。
39. 需求：真实战斗验证。动作：F2/F5 试玩并截图。结果：UI 与战斗均可运行。
40. 需求：压测性能与平衡。动作：触发 Dev 平衡 50 轮。结果：游戏内回报可读。
41. 需求：确认可视证据链。动作：保存多状态截图（Stage/Prepare/Battle/Reward）。结果：证据完整。
42. 需求：修正构建工具链。动作：BuildScript 抽象通用 Build 方法并保留 Mac/Win 入口。结果：发布链更稳。
43. 需求：Windows 交付能力。动作：安装 windows-mono 模块并成功构建 Win 包。结果：跨平台交付成立。
44. 需求：便于分发。动作：产出 `DragonChessLegends_Windows.zip`。结果：可直接分享。
45. 需求：文档完善。动作：README 重写为完整项目说明。结果：对外展示能力提升。
46. 需求：美术流程沉淀。动作：保留统一棋子重绘脚本与规范。结果：后续风格可持续。
47. 需求：路线可选性。动作：路线面板折叠+默认不锁定。结果：避免开局强引导。
48. 需求：可玩度闭环。动作：新增海克斯+新单位+新路线联动。结果：阵容化学反应提升。
49. 需求：Steam 级目标拆解。动作：整理近中远路线。结果：后续冲刺可执行。
50. 需求：进入下一轮大迭代。动作：形成本台账与问题清单。结果：可继续 100 轮开发循环。

## 本批次结果摘要
- UI：完成核心布局重构，战场遮挡显著降低。
- 玩法：新增 3 个海克斯 + 2 个单位 + 2 条阵容路线。
- 工程：Mac/Windows 构建链已可用；build 全部通过。
- 证据：已保留多状态截图，支撑后续视觉迭代复盘。

## 下一批（建议）
1. 做“战斗播报条动画化 + 数字飘字”增强打击反馈。
2. 做“海克斯卡牌视觉升级（统一卡面模板+稀有度动效）”。
3. 做“章节 meta（局外升级/成就）”补 Steam 长线留存。
