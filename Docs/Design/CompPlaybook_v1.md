# 龙棋传说 阵容路线策划 v1

目标：维护当前版本可成型的职业线、家族大羁绊和运营思路。  
核心原则：`家族负责大羁绊，职业负责战斗职责`。

## 当前职业框架

- Vanguard：主前排
- Guardian：副前排 / 护卫
- Rider：机动节奏
- Assassin：切后排
- Artillery：纯输出炮
- Controller：功能炮 / 控场炮
- Soldier：稳定站场
- Medic：治疗续航
- Leader：高费团队核心

## 当前家族大羁绊

- 马后炮：马 + 炮
- 士象全：士 + 象 + 帅
- 车兵推进：车 + 兵
- 车炮联营：车 + 炮
- 帅兵令：帅 + 兵

这些大羁绊仍然成立，不因为职业拆分而消失。

## 推荐路线

### 1. 钢铁炮阵

- 定位：易成型
- 核心职业：Vanguard 2 + Artillery 2
- 核心家族：车炮联营
- 核心棋子：壁垒车、坦克车、连发炮、导弹炮
- 思路：前排拖回合，纯输出炮接管战斗
- 优先海克斯：`cannon_master` `artillery_overclock` `vanguard_wall`

### 2. 功能炮网

- 定位：中速成型
- 核心职业：Controller 2 + Artillery 2
- 核心家族：车炮联营 / 马后炮
- 核心棋子：电弧炮、侦察炮、寒晶炮、天幕炮、连发炮
- 思路：功能炮负责远程压制，输出炮负责收割
- 优先海克斯：`controller_net` `artillery_range` `tri_service`

### 3. 骑炮速攻

- 定位：易成型
- 核心职业：Rider 2 + Artillery 2
- 核心家族：马后炮
- 核心棋子：突袭马、枪骑马、连发炮、狙击炮
- 思路：马系先手冲散，炮线后续补火力
- 优先海克斯：`rider_charge` `rider_relay` `cannon_master`

### 4. 梦魇斩首

- 定位：中后期转型
- 核心职业：Assassin 2 + Rider 2
- 核心家族：马后炮
- 核心棋子：暗影士、毒牙士、梦魇马、突袭马
- 思路：刺客开战直接切后排，梦魇马补进场爆发
- 优先海克斯：`assassin_gate` `assassin_contract` `rider_charge`

### 5. 士象全护城

- 定位：中速成型
- 核心职业：Guardian 2 + Medic 2
- 核心家族：士象全
- 核心棋子：镜影士、圣卫士、岩石巨像、玄灵象、火焰君主
- 思路：前排厚度和续航一起堆，打长回合
- 优先海克斯：`guardian_grace` `medic_banner` `stone_oath`

### 6. 圣辉续航

- 定位：易成型
- 核心职业：Medic 2 + Soldier 2
- 核心家族：帅兵令
- 核心棋子：誓约兵、圣卫士、剑士兵、火焰君主
- 思路：靠医者抬血线，让兵系持续站场
- 优先海克斯：`medic_banner` `guardian_grace` `healing`

### 7. 车兵推进

- 定位：易成型
- 核心职业：Vanguard 2 + Soldier 2
- 核心家族：车兵推进
- 核心棋子：方阵兵、护卫兵、壁垒车、冲城车
- 思路：低费前排扎实，适合前期稳血和连胜
- 优先海克斯：`vanguard_bastion` `vanguard_wall` `team_atk`

### 8. 雷霆前压

- 定位：中速成型
- 核心职业：Guardian 2 + Controller 2
- 核心家族：车炮联营
- 核心棋子：震荡车、电弧炮、寒晶炮、战旗马
- 思路：前排护卫吃线，功能炮提供额外追加伤害
- 优先海克斯：`controller_net` `guardian_grace` `tri_service`

### 9. 双四终局

- 定位：高难终局
- 核心职业：Vanguard 4 + Artillery 4
- 核心家族：车炮联营
- 核心棋子：坦克车、震荡车、壁垒车、冲城车、导弹炮、迫击炮、连发炮、狙击炮
- 思路：传统大后期模板，要求经济与人口
- 优先海克斯：`board_plus` `artillery_overclock` `vanguard_bastion`

### 10. 炮医协同

- 定位：中速构筑
- 核心职业：Artillery 2 + Controller 2 + Medic 2
- 核心家族：马后炮 / 车炮联营
- 核心棋子：连发炮、电弧炮、天幕炮、圣卫士、誓约兵、玄灵象
- 思路：功能炮提供压制，医者抬持续作战，输出炮做终结
- 优先海克斯：`tri_service` `medic_banner` `controller_net`

### 11. 天幕压制

- 定位：高费控场核心
- 核心职业：Controller 4 + Artillery 2
- 核心家族：车炮联营
- 核心棋子：电弧炮、侦察炮、寒晶炮、天幕炮、狙击炮
- 思路：利用高射程和控场职业增伤，靠远程频率把对面后排直接压死
- 优先海克斯：`controller_net` `artillery_range` `tri_service`

### 12. 玄灵续航

- 定位：高费奶核体系
- 核心职业：Medic 2 + Guardian 2 + Vanguard 2
- 核心家族：士象全 / 车兵推进
- 核心棋子：玄灵象、圣卫士、镜影士、岩石巨像、壁垒车
- 思路：玄灵象提供高费续航上限，前排靠双护卫和先锋站住
- 优先海克斯：`medic_banner` `guardian_grace` `stone_oath`

## 设计审视

- 炮系现在不是“全体 Artillery”：
  - 输出炮走 Artillery
  - 功能炮走 Controller
- 马系现在不是“全体 Rider”：
  - 机动马走 Rider
  - 战旗马走 Guardian
  - 梦魇马走 Assassin
- 车系现在不是“全体 Vanguard”：
  - 坦克/壁垒走 Vanguard
  - 震荡车走 Guardian
  - 跑车走 Assassin

## 当前缺口

- Controller 已补 5 费核心 `天幕炮`，后续需要给它更鲜明的技能表现
- Medic 已补 5 费核心 `玄灵象`，后续需要把治疗反馈做得更明显
- Soldier 主 C 还不够鲜明
- Leader 只有一张，后续还需要第二个高费领袖位
