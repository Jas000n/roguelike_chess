# 20轮开发迭代日志

## 环境
- 项目: `/Users/jason/.openclaw/workspace/DragonChessLegends`
- Build 输出: `/Users/jason/.openclaw/workspace/DragonChessLegends/Builds/Mac/DragonChessLegends.app`
- 关键截图目录: `/Users/jason/.openclaw/workspace/DragonChessLegends/Builds/playtest_cycles`

## 迭代记录
1. 编译失败排查：定位 `RoguelikeFramework` 多文件重复实现（StartBattle/EndBattle/RefreshShop 等）。
2. 修复冲突：将旧架构残留 `RoguelikeFramework.Battle.cs`、`RoguelikeFramework.Economy.cs` 置空保留，避免重复与失效字段。
3. Build 验证：`build_devloop_cycle2.log` 成功，恢复可构建状态。
4. 首轮试玩截图：拉起 app 并采集基线图（`cycle00_baseline_*`、`cycle02_loaded_*`）。
5. 自动点击阻塞发现：辅助功能权限弹窗导致点击不可用（证据 `cycle01_after_enter_*`）。
6. 权限恢复后继续：`cliclick` 可执行，开始自动化尝试。
7. UI问题发现：开发按钮区域重叠/污染主界面，影响可读性与操作。
8. UI修复：新增开发面板折叠开关（保留作弊按钮），默认可收起。
9. 可读性修复：重做职业色按钮深色高对比方案，解决文本对比不足。
10. 布阵可视化修复：提高准备阶段格子底色与占位高亮透明度。
11. 战斗统计修复：重排间距、条宽、行高，缓解“挤成一坨”。
12. OnGUI回归修复：移除 Stage 分支早退导致的 GUI 状态未恢复风险。
13. 字体层级优化：标题/面板/按钮字号统一上调，信息更易读。
14. 开发流程优化：新增 `F1` 快捷切换开发工具（避免常驻干扰玩家视图）。
15. Build 回归：`build_devloop_cycle3.log` 成功。
16. Build 回归：`build_devloop_cycle4.log` 成功。
17. Build 回归：`build_devloop_cycle5.log` 成功。
18. 试玩截图回归：收起开发面板后的主界面基线（`cycle06_postfix_*`）。
19. 交互问题复测：点击坐标与显示位置存在偏差（疑似 Retina 缩放坐标系差异），已记录为下一优先修复项。
20. 下一步定义：进入“输入坐标系统一 + 可自动拖拽回归 + 多轮战斗平衡”阶段。

## 当前结论
- 构建已稳定恢复，连续 3 次开发构建成功。
- UI 结构、对比度、统计面板可读性已改善。
- 当前主阻塞从“编译失败”转为“Retina 下自动点击/拖拽坐标映射不稳定”。

## 下一轮（建议立刻执行）
1. 统一输入坐标系（屏幕像素/点坐标换算），修掉“看得到点不到”。
2. 完成自动拖拽脚本（商店 -> 备战席 -> 战场 -> 下场 -> 出售）并固化为回归脚本。
3. 增加一组固定种子战斗回归（3关、8关）输出数值摘要（胜率、平均回合、伤害分布）。
