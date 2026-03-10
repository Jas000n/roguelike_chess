# DragonChessLegends（龙棋传说）

> 中国象棋题材的策略自走棋原型，目标体验是：
> **金铲铲（阵容运营） + 杀戮尖塔/小丑牌（Roguelike构筑）**。

当前仓库处于“**可玩迭代版**”：核心循环已经可以从地图选路一路打到结算，分支地图/商店/备战席/海克斯/奖励/自动战斗均已接通，当前重点在于 UI 打磨、数值平衡和阵容深度扩充。

---

## 目录

- [项目定位](#项目定位)
- [当前版本状态](#当前版本状态)
- [核心玩法循环](#核心玩法循环)
- [系统设计（已落地）](#系统设计已落地)
- [阵容与羁绊设计](#阵容与羁绊设计)
- [UI 与交互规则](#ui-与交互规则)
- [项目结构](#项目结构)
- [开发环境与运行方式](#开发环境与运行方式)
- [构建发布](#构建发布)
- [美术资源与 DrawThings 管线](#美术资源与-drawthings-管线)
- [调试与回归能力](#调试与回归能力)
- [开发日志与策划文档](#开发日志与策划文档)
- [已知问题与风险](#已知问题与风险)
- [路线图](#路线图)

---

## 项目定位

**DragonChessLegends** 以象棋兵种为核心题材，融合：

- 自走棋的“买牌-上阵-自动战斗-经济运营”
- Roguelike 的“战后三选一、海克斯构筑、关卡推进”
- 面向长期目标的“可反复游玩阵容差异化”

设计目标不是复刻某一款游戏，而是做出“**阵容成型爽感 + 构筑随机乐趣**”并具备持续扩展能力的独立产品。

---

## 当前版本状态

### 已完成（可玩）

- 章节分支地图（Stage Map -> Prepare -> Battle -> Reward/Hex -> Next Node）
- 准备阶段：商店、刷新、经验、备战席、拖拽上阵、点击上阵
- 自动战斗：单位索敌、移动、攻击、职业/阵营加成结算
- 经济系统：金币、经验、人口上限、连胜/连败、利息、低血保底
- 海克斯三选一与战后奖励三选一
- 阵容路线系统：锁定路线后影响商店权重
- 战斗统计面板、单位属性 tooltip、羁绊点击面板
- 美术资源加载链路（UI、Hex、Units）
- Mac 批处理构建（Unity CLI）
- 费用品质色：1白 / 2绿 / 3蓝 / 4紫 / 5橙
- 星级成长与售卖返还：支持 2 星 / 3 星价值提升
- 商店目标牌高亮：刷到已持有同名牌时会高亮提示

### 最近增强（本次迭代）

- 路线面板支持折叠；开局默认不强制选中路线
- UI 文本显示修复（换行/区域增高）
- 22 张棋子立绘统一重绘（Qwen 模型，高质量参数）
- 新增阵容策划手册与美术规范文档
- 商店与备战席底栏改成满宽布局，接近金铲铲信息组织
- 棋子卡文字重新分层：名称 / 职业 / 阵营 / 星级 / 价格
- 修复备战席上阵链路：拖拽命中框同步到底栏新布局，并增加“点击备战席直接上阵”的兜底交互
- 高费卡基础属性曲线抬高，高费更有“主 C / 高质量单卡”价值
- 加入杀戮尖塔式三路章节地图：普通 / 精英 / 商店 / 问号 / 宝箱 / Boss
- 地图路线默认不可后退，主结构为 3 条长路线，少量交叉节点
- 准备阶段右下角操作区重新分层，修复概率/状态/路线条与商店卡片互相遮挡

### 当前做到什么程度

如果只看“能不能完整玩一把”，答案是：**可以**。

当前版本已经具备：

- 从关卡进入准备阶段
- 买牌、刷牌、锁牌、买经验
- 从备战席上阵、替换、下场、出售
- 自动战斗结算
- 战后奖励与海克斯选择
- 下一关继续运营

如果看“离 Steam 可上架还有多远”，当前阶段更准确地说是：

- **玩法原型完成**
- **可玩版本成立**
- **美术和 UI 进入中后期打磨**
- **平衡和内容量仍明显不够**

也就是说，这不是一个“只有 Demo 菜单”的空壳，而是一个已经打通主循环、正在往正式产品质量推进的原型版本。

---

## 核心玩法循环

1. **Stage（地图页）**
- 在章节地图上选择下一节点
- 节点类型包含普通 / 精英 / 商店 / 问号 / 宝箱 / Boss
- 路线默认向前推进，不可后退

2. **Prepare（准备阶段）**
- 商店买牌、刷新、买经验
- 拖拽棋子到战场
- 观察羁绊激活状态
- 可手动锁定阵容路线并一键自动布阵

3. **Battle（自动战斗）**
- 双方自动执行回合制行动
- 可切换战斗速度
- 可展开/收起战斗统计

4. **Reward / Hex（战后构筑）**
- 战后奖励三选一
- 按关卡节奏触发海克斯三选一
- 进入下一关继续运营

5. **GameOver / 通关**
- 生命归零失败
- 通关当前章节后重开或进入下一轮迭代

---

## 系统设计（已落地）

### 1) 单位与战斗

- 单位基础属性：HP / ATK / SPD / Range / Cost / Class / Origin
- 支持近战与远程
- 支持升级（星级）与自动合成
- 战斗统计：输出、承伤、MVP 可视化
- 费用品质区分已影响 UI 与基础强度曲线

### 2) 羁绊系统

- 职业羁绊（Class）：如 Vanguard / Rider / Artillery / Assassin
- 阵营羁绊（Origin）：如 Steel / Blaze / Thunder / Night / Shadow 等
- 羁绊按上阵阵容判定，不因战斗中死亡立即丢失

### 3) 经济系统

- 回合基础收入
- 利息（按金币区间）
- 连胜/连败奖励
- 买经验升级影响上阵人口
- 低血补给机制（防止无操作空间）

### 4) 构筑系统

- 海克斯三选一：经济、战斗、规则修正等
- 战后奖励三选一：金币、经验、回血、招募等
- 阵容路线锁定：提高目标体系成型稳定性

### 5) 地图推进系统

- 章节地图按楼层推进
- 3 条主路线，少量跨线连接
- 节点类型支持普通 / 精英 / 商店 / 问号 / 宝箱 / Boss
- 商店节点可单独购物后离开
- 问号节点支持揭示随机结果

### 6) Dev 回归系统

- 一键推进流程（DevAdvanceOneStep）
- 3 关快速回归
- 50/100 轮平衡压测（日志输出）

---

## 阵容与羁绊设计

已沉淀阵容策划文档（12 套路线，含易/中/高难、成型条件、化学反应）：

- [Docs/Design/CompPlaybook_v1.md](Docs/Design/CompPlaybook_v1.md)

示例路线：

- 钢铁炮阵
- 骑炮速攻
- 暗夜刺击
- 堡垒炮
- 四刺爆发
- 双四终局
- 吸血反打（预留专属海克斯扩展）

---

## UI 与交互规则

### 准备阶段

- 左键点击棋子：查看属性/羁绊详情
- 拖拽：备战席 <-> 战场
- 点击备战席棋子：直接上阵到最近可用格
- 右键战场棋子：快速下场到备战席
- 可出售选中棋子

### 战斗阶段

- 切换战斗速度（1x/2x/4x）
- 展开/隐藏战斗统计
- 点击单位/羁绊卡片查看详细说明

### 路线面板

- 可折叠
- 默认不自动锁定路线
- 玩家主动选择“锁定路线”后才进入定向运营

### 底栏 HUD

- 商店与备战席占满底部主区域
- 刷新概率独立显示为费用色条
- 右侧操作区独立于商店卡片区，避免状态条与卡片互相覆盖

---

## 项目结构

```text
DragonChessLegends/
├─ Assets/
│  ├─ Scenes/
│  │  └─ Main.unity
│  ├─ Scripts/
│  │  ├─ Systems/
│  │  │  ├─ RoguelikeFramework.cs
│  │  │  ├─ RoguelikeFramework.Flow.cs
│  │  │  ├─ RoguelikeFramework.Battle.cs
│  │  │  ├─ RoguelikeFramework.Economy.cs
│  │  │  ├─ AutoGameGenerator.cs
│  │  │  └─ GameBootstrap.cs
│  │  ├─ Data/
│  │  ├─ Core/
│  │  └─ Editor/
│  │     └─ BuildScript.cs
│  └─ Resources/Art/
│     ├─ Units/
│     ├─ Hexes/
│     └─ UI/
├─ Docs/
│  ├─ PROGRESS_LOG.md
│  ├─ DEV_LOOP.md
│  ├─ Design/CompPlaybook_v1.md
│  ├─ Art/UNIT_ART_STYLE_GUIDE.md
│  └─ ContentPack_v0.2/
│     ├─ generate_units_unified_qwen.py
│     ├─ generate_non_unit_art.py
│     └─ ...
└─ Builds/
   ├─ Mac/DragonChessLegends.app
   └─ build_*.log
```

---

## 开发环境与运行方式

### 环境

- Unity: `6000.3.10f1`
- 平台：macOS（当前主验证环境）

### 在 Unity Editor 运行

1. 用 Unity Hub 打开项目目录
2. 打开场景：`Assets/Scenes/Main.unity`
3. 点击 Play 运行

---

## 构建发布

### 命令行构建（Mac）

```bash
/Applications/Unity/Hub/Editor/6000.3.10f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "/Users/jason/.openclaw/workspace/DragonChessLegends" \
  -executeMethod BuildScript.BuildMac \
  -logFile "/Users/jason/.openclaw/workspace/DragonChessLegends/Builds/build.log"
```

### 输出路径

- `Builds/Mac/DragonChessLegends.app`

---

## 美术资源与 DrawThings 管线

### 棋子统一重绘脚本

- 脚本：`Docs/ContentPack_v0.2/generate_units_unified_qwen.py`
- 默认端口：`7860`
- 模型：`qwen_image_2512_q6p.ckpt`

运行：

```bash
python3 -u Docs/ContentPack_v0.2/generate_units_unified_qwen.py
```

### 美术规范

- 文档：`Docs/Art/UNIT_ART_STYLE_GUIDE.md`
- 规范重点：统一风格锚点、Prompt 模板、负面词、参数建议、验收清单

---

## 调试与回归能力

在游戏内可使用开发入口进行快速验证：

- 开发推进一步
- 开发重开
- 自动回归 3 关
- 平衡测试 50 轮 / 100 轮

平衡报告输出位置（示例）：

- `~/Library/Application Support/DefaultCompany/DragonChessLegends/DevReports/`

---

## 开发日志与策划文档

- 进度日志：`Docs/PROGRESS_LOG.md`
- 循环迭代主表：`Docs/devloop/CYCLE_100_MASTER.md`
- 20 轮迭代记录：`Docs/devloop/ITER_20_LOG.md`
- 50 轮平衡记录：`Docs/devloop/ITER_50_BALANCE.md`
- 阵容策划手册：`Docs/Design/CompPlaybook_v1.md`

---

## 已知问题与风险

- 脚本仍在持续拆分中，`RoguelikeFramework` 仍有体量压力
- 目前以手动/半自动回归为主，缺完整自动化测试套件
- UI 风格虽已统一方向，但仍需更多动态反馈、层级感和动效打磨
- 数值平衡正在持续迭代，部分高难阵容成型波动较大
- 商店/备战席与右下角 HUD 遮挡问题已做过一轮修正，但仍需继续压缩信息密度
- Windows 包与更完整的平台发布流程仍需继续验证

---

## 路线图

### 近程（v0.3）

- 完整化多套阵容成型体验（易中难分层更稳定）
- 优化战斗爽感（技能表现、打击反馈、节奏曲线）
- 继续打磨商店权重与保底策略
- 把章节地图事件、商店节点和问号节点做得更完整

### 中程（v0.4）

- 丰富海克斯池与规则改写类构筑
- 加入更完整的局外成长/章节机制
- UI 系统化重构（组件化、视觉一致性提升）

### 长程（Steam 可上架目标）

- 完整新手引导与可持续内容更新框架
- 更稳定的平衡流程与测试体系
- 成就、存档、设置、发行级体验补齐

---

如果你希望，我下一步可以继续补一版 `README_EN.md`（英文版）和一份 `STEAM_PAGE_BRIEF.md`（商店页文案草案），直接用于对外展示。
