using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
    #region Setup Data

    private void BuildUnitDefs()
    {
        unitDefs.Clear();
        basePool.Clear();

        // 车系变体
        AddDef("chariot_tank", "坦克车", "车", "Vanguard", "Steel", 3, 66, 7, 5, 1);
        AddDef("chariot_sport", "跑车", "车", "Assassin", "Wind", 3, 36, 12, 13, 1);
        AddDef("chariot_shock", "震荡车", "车", "Guardian", "Thunder", 4, 60, 11, 7, 1);

        // 马系变体
        AddDef("horse_raider", "突袭马", "马", "Rider", "Shadow", 2, 33, 11, 12, 1);
        AddDef("horse_banner", "战旗马", "马", "Guardian", "Stone", 3, 44, 8, 10, 1);
        AddDef("horse_nightmare", "梦魇马", "马", "Assassin", "Night", 4, 35, 16, 11, 1);
        AddDef("horse_wind", "疾风马", "马", "Rider", "Wind", 2, 31, 10, 13, 1);

        // 炮系变体
        AddDef("cannon_missile", "导弹炮", "炮", "Artillery", "Blaze", 4, 29, 18, 7, 4);
        AddDef("cannon_mortar", "迫击炮", "炮", "Artillery", "Stone", 3, 34, 13, 7, 3);
        AddDef("cannon_burst", "连发炮", "炮", "Artillery", "Steel", 2, 28, 10, 10, 3);
        AddDef("cannon_sniper", "狙击炮", "炮", "Artillery", "Shadow", 3, 25, 16, 8, 5);
        AddDef("cannon_arc", "电弧炮", "炮", "Controller", "Thunder", 2, 27, 10, 9, 3);
        AddDef("cannon_scout", "侦察炮", "炮", "Controller", "Wind", 1, 24, 7, 10, 3);
        AddDef("cannon_frost", "寒晶炮", "炮", "Controller", "Frost", 4, 29, 15, 7, 4);
        AddDef("cannon_storm", "天幕炮", "炮", "Controller", "Blaze", 5, 34, 18, 8, 5);

        // 基础补充
        AddDef("general_fire", "火焰君主", "帅", "Leader", "Blaze", 5, 58, 13, 8, 1);
        AddDef("ele_guard", "岩石巨像", "象", "Guardian", "Stone", 3, 60, 8, 5, 1);
        AddDef("guard_assassin", "暗影士", "士", "Assassin", "Shadow", 2, 30, 11, 11, 1);
        AddDef("guard_blade", "夜刃士", "士", "Soldier", "Night", 2, 35, 11, 10, 1);
        AddDef("guard_poison", "毒牙士", "士", "Assassin", "Venom", 3, 32, 13, 11, 1);
        AddDef("guard_mirror", "镜影士", "士", "Guardian", "Mist", 4, 46, 12, 10, 1);
        AddDef("guard_mist", "雾刃士", "士", "Assassin", "Mist", 3, 31, 13, 12, 1);
        AddDef("guard_holy", "圣卫士", "士", "Medic", "Holy", 3, 48, 8, 8, 2);
        AddDef("ele_sage", "玄灵象", "象", "Medic", "Mist", 5, 62, 10, 7, 2);
        AddDef("chariot_bulwark", "壁垒车", "车", "Vanguard", "Stone", 2, 58, 7, 6, 1);
        AddDef("chariot_ram", "冲城车", "车", "Vanguard", "Blaze", 4, 62, 10, 6, 1);
        AddDef("soldier_phalanx", "方阵兵", "兵", "Vanguard", "Steel", 1, 34, 6, 7, 1);
        AddDef("soldier_guard", "护卫兵", "兵", "Vanguard", "Stone", 1, 36, 6, 7, 1);
        AddDef("soldier_sword", "剑士兵", "兵", "Soldier", "Steel", 1, 26, 9, 8, 1);
        AddDef("soldier_zeal", "誓约兵", "兵", "Medic", "Holy", 2, 31, 8, 9, 2);
        AddDef("horse_lancer", "枪骑马", "马", "Rider", "Steel", 2, 35, 11, 11, 1);

        foreach (var kv in unitDefs) basePool.Add(kv.Key);
    }

    private void AddDef(string key, string name, string family, string classTag, string originTag, int cost, int hp, int atk, int spd, int range)
    {
        // Higher-cost units should feel premium before star-ups and synergies kick in.
        float hpScale = 1f + Mathf.Max(0, cost - 1) * 0.08f;
        float atkScale = 1f + Mathf.Max(0, cost - 1) * 0.06f;
        unitDefs[key] = new UnitDef
        {
            key = key,
            name = name,
            family = family,
            classTag = classTag,
            originTag = originTag,
            cost = cost,
            hp = Mathf.RoundToInt(hp * hpScale),
            atk = Mathf.RoundToInt(atk * atkScale),
            spd = spd,
            range = range
        };
    }

    private void BuildLinearStages()
    {
        stages.Clear();
        stageNodeById.Clear();
        availableStageNodeIds.Clear();

        StageNode AddStage(string id, int floor, int lane, StageType type, int power, bool giveHex)
        {
            var node = new StageNode
            {
                id = id,
                floor = floor,
                lane = lane,
                type = type,
                revealedType = type,
                power = power,
                giveHex = giveHex
            };
            stages.Add(node);
            stageNodeById[id] = node;
            return node;
        }

        void Link(string from, params string[] to)
        {
            if (!stageNodeById.TryGetValue(from, out var node)) return;
            for (int i = 0; i < to.Length; i++)
            {
                if (!node.nextIds.Contains(to[i])) node.nextIds.Add(to[i]);
            }
        }

        StageType[,] layout =
        {
            { StageType.Normal,   StageType.Normal,   StageType.Normal   },
            { StageType.Shop,     StageType.Normal,   StageType.Treasure },
            { StageType.Normal,   StageType.Mystery,  StageType.Normal   },
            { StageType.Shop,     StageType.Normal,   StageType.Shop     },
            { StageType.Normal,   StageType.Normal,   StageType.Treasure },
            { StageType.Mystery,  StageType.Shop,     StageType.Normal   },
            { StageType.Normal,   StageType.Elite,    StageType.Normal   },
            { StageType.Shop,     StageType.Normal,   StageType.Mystery  },
            { StageType.Elite,    StageType.Normal,   StageType.Treasure },
            { StageType.Normal,   StageType.Shop,     StageType.Elite    },
            { StageType.Mystery,  StageType.Normal,   StageType.Normal   },
            { StageType.Treasure, StageType.Elite,    StageType.Shop     }
        };

        int[,] power =
        {
            { 1, 1, 1 },
            { 1, 1, 0 },
            { 2, 2, 2 },
            { 1, 2, 1 },
            { 3, 3, 0 },
            { 3, 1, 3 },
            { 4, 5, 4 },
            { 1, 4, 4 },
            { 6, 5, 0 },
            { 5, 1, 6 },
            { 6, 6, 6 },
            { 0, 7, 1 }
        };

        bool[,] giveHex =
        {
            { false, false, false },
            { false, false, false },
            { false, false, false },
            { false, false, false },
            { false, true,  false },
            { false, false, false },
            { false, true,  false },
            { false, false, false },
            { true,  false, false },
            { false, false, true  },
            { false, true,  false },
            { false, true,  false }
        };

        for (int floor = 1; floor <= 12; floor++)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                AddStage($"f{floor}_{lane}", floor, lane, layout[floor - 1, lane], power[floor - 1, lane], giveHex[floor - 1, lane]);
            }
        }

        AddStage("f13_boss", 13, 1, StageType.Boss, 9, true);

        for (int floor = 1; floor < 12; floor++)
        {
            for (int lane = 0; lane < 3; lane++)
            {
                Link($"f{floor}_{lane}", $"f{floor + 1}_{lane}");
            }
        }

        // 少量跨线，制造路线选择，但不做全互联
        Link("f3_0", "f4_1");
        Link("f4_2", "f5_1");
        Link("f5_1", "f6_2");
        Link("f6_0", "f7_1");
        Link("f8_2", "f9_1");
        Link("f9_0", "f10_1");
        Link("f10_1", "f11_2");
        Link("f11_0", "f12_1");

        Link("f12_0", "f13_boss");
        Link("f12_1", "f13_boss");
        Link("f12_2", "f13_boss");

        availableStageNodeIds.Add("f1_0");
        availableStageNodeIds.Add("f1_1");
        availableStageNodeIds.Add("f1_2");
    }


    private void BuildCompDefs()
    {
        compDefs.Clear();
        compDefs.Add(new CompDef
        {
            id = "iron_artillery",
            name = "钢铁炮阵",
            desc = "前排钢铁抗线，后排炮阵连续爆破",
            focusClasses = new[] { "Vanguard", "Artillery" },
            focusOrigins = new[] { "Steel", "Blaze", "Thunder" },
            classA = "Vanguard",
            classB = "Artillery",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.12f,
            bonusReduction = 0.08f,
            bonusSpeed = 0
        });
        compDefs.Add(new CompDef
        {
            id = "rider_burst",
            name = "骑炮速攻",
            desc = "骑兵抢节奏，炮组收割",
            focusClasses = new[] { "Rider", "Artillery" },
            focusOrigins = new[] { "Wind", "Thunder", "Stone" },
            classA = "Rider",
            classB = "Artillery",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.08f,
            bonusReduction = 0f,
            bonusSpeed = 2
        });
        compDefs.Add(new CompDef
        {
            id = "shadow_night",
            name = "暗夜刺击",
            desc = "暗影刺客跳后排，夜幕爆发斩杀",
            focusClasses = new[] { "Assassin", "Rider" },
            focusOrigins = new[] { "Shadow", "Night", "Venom" },
            classA = "Assassin",
            classB = "Rider",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.15f,
            bonusReduction = 0f,
            bonusSpeed = 1
        });
        compDefs.Add(new CompDef
        {
            id = "double4",
            name = "双四终局",
            desc = "4先锋+4炮的后期终局阵",
            focusClasses = new[] { "Vanguard", "Artillery" },
            focusOrigins = new[] { "Steel", "Stone", "Blaze", "Thunder" },
            classA = "Vanguard",
            classB = "Artillery",
            needClass2A = 4,
            needClass2B = 4,
            bonusDmg = 0.18f,
            bonusReduction = 0.12f,
            bonusSpeed = 1
        });
        compDefs.Add(new CompDef
        {
            id = "poison_cut",
            name = "毒影斩首",
            desc = "刺客切后排，炮手补收割",
            focusClasses = new[] { "Assassin", "Artillery" },
            focusOrigins = new[] { "Venom", "Shadow", "Night", "Thunder" },
            classA = "Assassin",
            classB = "Artillery",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.14f,
            bonusReduction = 0.02f,
            bonusSpeed = 1
        });
        compDefs.Add(new CompDef
        {
            id = "steel_rider_wall",
            name = "钢骑壁垒",
            desc = "先锋稳前排，骑兵打节奏",
            focusClasses = new[] { "Vanguard", "Rider" },
            focusOrigins = new[] { "Steel", "Stone", "Wind", "Shadow" },
            classA = "Vanguard",
            classB = "Rider",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.07f,
            bonusReduction = 0.11f,
            bonusSpeed = 2
        });
        compDefs.Add(new CompDef
        {
            id = "stone_fortress",
            name = "岩壁要塞",
            desc = "大地与先锋堆叠减伤，慢但很硬",
            focusClasses = new[] { "Vanguard", "Guardian" },
            focusOrigins = new[] { "Stone", "Steel" },
            classA = "Vanguard",
            classB = "Guardian",
            needClass2A = 2,
            needClass2B = 1,
            bonusDmg = 0.05f,
            bonusReduction = 0.14f,
            bonusSpeed = 0
        });
        compDefs.Add(new CompDef
        {
            id = "venom_exec",
            name = "毒蚀处决",
            desc = "毒系持续压血线，刺客快速处决",
            focusClasses = new[] { "Assassin", "Rider" },
            focusOrigins = new[] { "Venom", "Night", "Shadow" },
            classA = "Assassin",
            classB = "Rider",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.16f,
            bonusReduction = 0f,
            bonusSpeed = 2
        });
        compDefs.Add(new CompDef
        {
            id = "wind_sniper",
            name = "疾风狙杀",
            desc = "风系加速让后排炮手打出频率压制",
            focusClasses = new[] { "Artillery", "Rider" },
            focusOrigins = new[] { "Wind", "Shadow", "Thunder" },
            classA = "Artillery",
            classB = "Rider",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.11f,
            bonusReduction = 0f,
            bonusSpeed = 3
        });
        compDefs.Add(new CompDef
        {
            id = "blaze_rush",
            name = "烈焰突围",
            desc = "烈焰增伤配合先锋冲城，爆发过前排",
            focusClasses = new[] { "Vanguard", "Artillery" },
            focusOrigins = new[] { "Blaze", "Steel" },
            classA = "Vanguard",
            classB = "Artillery",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.13f,
            bonusReduction = 0.05f,
            bonusSpeed = 0
        });
        compDefs.Add(new CompDef
        {
            id = "steel_reroll",
            name = "钢铁追三",
            desc = "低费钢铁单位追三星打中期质量差",
            focusClasses = new[] { "Vanguard", "Soldier" },
            focusOrigins = new[] { "Steel", "Stone" },
            classA = "Vanguard",
            classB = "Soldier",
            needClass2A = 2,
            needClass2B = 1,
            bonusDmg = 0.08f,
            bonusReduction = 0.10f,
            bonusSpeed = 1
        });
        compDefs.Add(new CompDef
        {
            id = "holy_guard",
            name = "圣卫铁壁",
            desc = "守护者和先锋构筑前排要塞",
            focusClasses = new[] { "Guardian", "Vanguard" },
            focusOrigins = new[] { "Holy", "Stone", "Steel" },
            classA = "Guardian",
            classB = "Vanguard",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.05f,
            bonusReduction = 0.16f,
            bonusSpeed = 0
        });
        compDefs.Add(new CompDef
        {
            id = "frost_sniper",
            name = "霜影炮狙",
            desc = "寒晶炮和狙击炮压制后排",
            focusClasses = new[] { "Artillery", "Assassin" },
            focusOrigins = new[] { "Frost", "Shadow", "Mist" },
            classA = "Artillery",
            classB = "Assassin",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.14f,
            bonusReduction = 0.02f,
            bonusSpeed = 2
        });
        compDefs.Add(new CompDef
        {
            id = "control_battery",
            name = "控场炮网",
            desc = "功能炮与输出炮混编，靠射程与持续压制赢回合",
            focusClasses = new[] { "Controller", "Artillery" },
            focusOrigins = new[] { "Thunder", "Frost", "Blaze", "Wind" },
            classA = "Controller",
            classB = "Artillery",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.14f,
            bonusReduction = 0.02f,
            bonusSpeed = 1
        });
        compDefs.Add(new CompDef
        {
            id = "holy_recovery",
            name = "圣愈护城",
            desc = "医者抬血线，守护者和先锋扛住前排",
            focusClasses = new[] { "Medic", "Guardian" },
            focusOrigins = new[] { "Holy", "Mist", "Stone" },
            classA = "Medic",
            classB = "Guardian",
            needClass2A = 2,
            needClass2B = 2,
            bonusDmg = 0.05f,
            bonusReduction = 0.14f,
            bonusSpeed = 0
        });
    }

    private StageNode GetCurrentStageNode()
    {
        if (string.IsNullOrEmpty(currentStageNodeId)) return null;
        stageNodeById.TryGetValue(currentStageNodeId, out var node);
        return node;
    }

    private StageType GetEffectiveStageType(StageNode node)
    {
        if (node == null) return StageType.Normal;
        return node.type == StageType.Mystery && node.mysteryRevealed ? node.revealedType : node.type;
    }

    private int GetFinalFloor()
    {
        int maxFloor = 1;
        for (int i = 0; i < stages.Count; i++) maxFloor = Mathf.Max(maxFloor, stages[i].floor);
        return maxFloor;
    }

    private List<StageNode> GetAvailableStageNodes()
    {
        var result = new List<StageNode>();
        for (int i = 0; i < availableStageNodeIds.Count; i++)
        {
            if (stageNodeById.TryGetValue(availableStageNodeIds[i], out var node)) result.Add(node);
        }
        return result;
    }

    private void RevealMysteryNode(StageNode node)
    {
        if (node == null || node.type != StageType.Mystery || node.mysteryRevealed) return;

        float roll = UnityEngine.Random.value;
        if (roll < 0.42f) node.revealedType = StageType.Normal;
        else if (roll < 0.58f) node.revealedType = StageType.Elite;
        else if (roll < 0.79f) node.revealedType = StageType.Shop;
        else node.revealedType = StageType.Treasure;
        node.mysteryRevealed = true;
    }

    private void SelectStageNode(string nodeId)
    {
        if (!stageNodeById.TryGetValue(nodeId, out var node)) return;
        currentStageNodeId = node.id;
        stageIndex = Mathf.Max(0, node.floor - 1);
        if (node.type == StageType.Mystery) RevealMysteryNode(node);

        var effective = GetEffectiveStageType(node);
        if (effective == StageType.Treasure)
        {
            pendingHexAfterReward = node.giveHex;
            RollRewardOffers();
            state = RunState.Reward;
            battleLog = $"进入宝箱节点：第{node.floor}层";
            return;
        }

        StartPreparationForCurrentStage();
    }

    private void AdvanceToStageMapFromCurrentNode()
    {
        var node = GetCurrentStageNode();
        if (node == null)
        {
            state = RunState.GameOver;
            return;
        }

        node.cleared = true;
        availableStageNodeIds.Clear();
        for (int i = 0; i < node.nextIds.Count; i++)
        {
            string nextId = node.nextIds[i];
            if (stageNodeById.TryGetValue(nextId, out var next) && !next.cleared) availableStageNodeIds.Add(nextId);
        }

        currentStageNodeId = "";
        if (availableStageNodeIds.Count == 0)
        {
            state = RunState.GameOver;
            battleLog += " | 章节结束";
            return;
        }

        state = RunState.Stage;
        battleLog += " | 返回地图选择下一条路线";
    }

    private void BuildHexPool()
    {
        hexPool.Clear();
        AddHex("rich", "金币雨", "蓝", "每回合准备阶段额外 +4 金币");
        AddHex("interest_up", "理财大师", "蓝", "利息上限 +2");
        AddHex("cannon_master", "炮火专精", "金", "炮系伤害 +25%，开局前3回合额外 +10%");
        AddHex("rider_charge", "骑兵冲锋", "金", "骑兵速度 +2，首击额外 +8 伤害");
        AddHex("vanguard_wall", "钢铁壁垒", "蓝", "先锋单位受到伤害 -18%");
        AddHex("team_atk", "全军增幅", "白", "全队攻击 +2");
        AddHex("artillery_range", "超远校准", "蓝", "炮系射程 +1");
        AddHex("board_plus", "超载部署", "金", "上阵人数上限 +1");
        AddHex("fast_train", "快速练兵", "白", "每回合额外 +2 经验");
        AddHex("healing", "战备修复", "白", "每回合准备阶段，上阵棋子回复 20% 最大生命");
        AddHex("lifesteal_core", "嗜血核心", "金", "全队造成伤害时回复该伤害的 12% 生命");
        AddHex("execution_edge", "处决边缘", "蓝", "攻击低血目标时额外增伤，越残血越高");
        AddHex("assassin_bloom", "刺影绽放", "金", "刺客暴击率+12%，首击额外伤害+8");
        AddHex("assassin_contract", "暗影契约", "彩", "刺客暴击伤害提升，参与击杀时额外获得1金币");
        AddHex("vanguard_bastion", "壁垒军令", "金", "先锋获得额外减伤与开战护盾");
        AddHex("rider_relay", "连环冲阵", "蓝", "骑兵前8回合额外速度，首次命中后再次突进");
        AddHex("artillery_overclock", "火控超频", "彩", "炮手额外伤害，第四次攻击附带溅射");
        AddHex("stone_oath", "磐石誓约", "金", "石系单位生命与减伤提高");
        AddHex("venom_payload", "毒蚀载荷", "金", "毒系命中附加持续伤害");
        AddHex("windwalk", "疾风步", "蓝", "风系单位速度和闪避提高");
        AddHex("reroll_engine", "精密改造", "彩", "每回合首次刷新免费，并额外获得1次刷新");
        AddHex("triple_prep", "追三计划", "彩", "场上最高星级单位再获额外属性");
        AddHex("royal_supply", "王庭军需", "彩", "准备阶段额外获得 6 金币，并在商店节点额外刷新 1 张海克斯奇物");
        AddHex("assassin_gate", "影门突袭", "彩", "刺客开战切入后排时首次攻击额外造成 18 伤害并获得 25% 减伤");
        AddHex("guardian_grace", "神佑守望", "金", "守护者与医者每回合首次行动时为最低生命友军回复生命");
        AddHex("controller_net", "控场棋网", "金", "控场奇谋单位追加伤害提高，并获得额外射程");
        AddHex("medic_banner", "回春军旗", "金", "医者治疗量提高，且被治疗单位获得短暂减伤");
        AddHex("tri_service", "三军协同", "彩", "炮/控/医同时上阵时，全队获得额外伤害与续航");
    }

    private void AddHex(string id, string name, string rarity, string desc)
    {
        hexPool.Add(new HexDef { id = id, name = name, rarity = rarity, desc = desc });
    }

    private void BuildRewardPool()
    {
        rewardPool.Clear();
        rewardPool.Add(new RewardDef { id = "gold_big", name = "藏宝箱", desc = "立即获得 +10 金币" });
        rewardPool.Add(new RewardDef { id = "gold_huge", name = "黄金密约", desc = "立即获得 +16 金币" });
        rewardPool.Add(new RewardDef { id = "heal", name = "战地医疗", desc = "恢复 8 点生命" });
        rewardPool.Add(new RewardDef { id = "heal_big", name = "再生矩阵", desc = "恢复 15 点生命" });
        rewardPool.Add(new RewardDef { id = "exp", name = "战术复盘", desc = "获得 6 点经验" });
        rewardPool.Add(new RewardDef { id = "exp_big", name = "大师课程", desc = "获得 10 点经验" });
        rewardPool.Add(new RewardDef { id = "unit_low", name = "招募新兵", desc = "获得 1 个随机 1-3 费棋子" });
        rewardPool.Add(new RewardDef { id = "unit_mid", name = "精锐补员", desc = "获得 1 个随机 2-4 费棋子（偏向当前路线）" });
        rewardPool.Add(new RewardDef { id = "duo_pack", name = "双人补给", desc = "获得 2 个随机低费棋子" });
        rewardPool.Add(new RewardDef { id = "reroll_pack", name = "补给券", desc = "免费刷新商店并额外 +2 金币" });
        rewardPool.Add(new RewardDef { id = "board_bonus", name = "扩编令", desc = "本局上阵上限永久 +1" });
        rewardPool.Add(new RewardDef { id = "star_up", name = "战地升星", desc = "随机提升 1 个我方棋子星级（最高3星）" });
        rewardPool.Add(new RewardDef { id = "hex_random", name = "奇物箱", desc = "随机获得 1 个未拥有海克斯" });
        // Stage B3 奖励深度：新增更多构筑向选项
        rewardPool.Add(new RewardDef { id = "free_reroll_3", name = "补给连拨", desc = "接下来3回合，每回合首次刷新免费" });
        rewardPool.Add(new RewardDef { id = "gold_interest", name = "对赌协议", desc = "获得10金币，但本局利息上限-1" });
        rewardPool.Add(new RewardDef { id = "exp_burst", name = "极限阅历", desc = "获得等同于当前等级*3的经验值" });
    }

    private void RollRewardOffers()
    {
        currentRewardOffers.Clear();
        var copy = new List<RewardDef>(rewardPool);
        for (int i = 0; i < 3 && copy.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, copy.Count);
            currentRewardOffers.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
    }

    private void PickReward(int idx)
    {
        if (idx < 0 || idx >= currentRewardOffers.Count) return;

        var r = currentRewardOffers[idx];
        switch (r.id)
        {
            case "gold_big":
                gold += 10;
                battleLog = "奖励：+10 金币";
                break;
            case "gold_huge":
                gold += 16;
                battleLog = "奖励：+16 金币";
                break;
            case "heal":
                playerLife = Mathf.Min(36, playerLife + 8);
                battleLog = "奖励：恢复 8 生命";
                break;
            case "heal_big":
                playerLife = Mathf.Min(36, playerLife + 15);
                battleLog = "奖励：恢复 15 生命";
                break;
            case "exp":
                GainExp(6);
                battleLog = "奖励：+6 经验";
                break;
            case "exp_big":
                GainExp(10);
                battleLog = "奖励：+10 经验";
                break;
            case "unit_low":
                if (benchUnits.Count < 8)
                {
                    string key = RollCompUnitKeyByLevel();
                    benchUnits.Add(CreateUnit(key, true));
                    battleLog = $"奖励：获得 {unitDefs[key].name}";
                    PushEvent($"奖励命中：{unitDefs[key].name}");
                }
                else
                {
                    gold += 4;
                    battleLog = "备战席满，改为 +4 金币";
                }
                break;
            case "unit_mid":
                if (benchUnits.Count < 8)
                {
                    string key = RollUnitKeyByCostRange(2, 4, true);
                    benchUnits.Add(CreateUnit(key, true));
                    battleLog = $"奖励：精锐补员 {unitDefs[key].name}";
                }
                else
                {
                    gold += 5;
                    battleLog = "备战席满，精锐补员改为 +5 金币";
                }
                break;
            case "duo_pack":
                for (int t = 0; t < 2 && benchUnits.Count < 8; t++)
                {
                    string key = RollUnitKeyByCostRange(1, 2, false);
                    benchUnits.Add(CreateUnit(key, true));
                }
                battleLog = "奖励：获得双人补给";
                break;
            case "free_reroll_3":
                freeRerollTurns = 3;
                battleLog = "奖励：未来3回合首次刷新免费 (系统待接入)";
                break;
            case "gold_interest":
                gold += 10;
                interestCapModifier -= 1;
                battleLog = "奖励：对赌协议 +10金币 (上限惩罚待接入)";
                break;
            case "exp_burst":
                int burst = playerLevel * 3;
                GainExp(burst);
                battleLog = $"奖励：极限阅历 +{burst}经验";
                break;
            case "reroll_pack":
                RefreshShop(true);
                gold += 2;
                battleLog = "奖励：免费刷新商店 +2 金币";
                break;
            case "board_bonus":
                rewardBoardCapBonus = Mathf.Min(2, rewardBoardCapBonus + 1);
                battleLog = $"奖励：本局上阵上限 +1（当前额外{rewardBoardCapBonus}）";
                break;
            case "star_up":
                {
                    var all = new List<Unit>();
                    all.AddRange(deploySlots);
                    all.AddRange(benchUnits);
                    all = all.FindAll(u => u.star < 3);
                    if (all.Count > 0)
                    {
                        var pick = all[UnityEngine.Random.Range(0, all.Count)];
                        UpgradeUnit(pick);
                        battleLog = $"奖励：{pick.Name} 升至 {pick.star}★";
                    }
                    else
                    {
                        gold += 6;
                        battleLog = "全员已3星，改为 +6 金币";
                    }
                }
                break;
            case "hex_random":
                {
                    var cands = new List<HexDef>();
                    foreach (var h in hexPool) if (!HasHex(h.id)) cands.Add(h);
                    if (cands.Count > 0)
                    {
                        var h = cands[UnityEngine.Random.Range(0, cands.Count)];
                        selectedHexes.Add(h);
                        battleLog = $"奖励：获得海克斯 {h.name}";
                    }
                    else
                    {
                        gold += 4;
                        battleLog = "海克斯已拿满，改为 +4 金币";
                    }
                }
                break;
        }

        currentRewardOffers.Clear();

        if (pendingHexAfterReward)
        {
            pendingHexAfterReward = false;
            RollHexOffers();
            state = RunState.Hex;
            battleLog += " | 海克斯三选一";
            return;
        }

        AdvanceToStageMapFromCurrentNode();
    }

    private string RollUnitKeyByCostRange(int minCost, int maxCost, bool preferComp)
    {
        var pool = new List<UnitDef>();
        var lc = GetLockedComp();
        int levelCap = Mathf.Min(5, playerLevel + 1);
        int lo = Mathf.Clamp(minCost, 1, 5);
        int hi = Mathf.Clamp(maxCost, lo, 5);
        foreach (var kv in unitDefs)
        {
            var d = kv.Value;
            if (d.cost < lo || d.cost > hi || d.cost > levelCap) continue;
            if (preferComp && lc != null)
            {
                bool hit = false;
                for (int i = 0; i < lc.focusClasses.Length; i++) if (d.classTag == lc.focusClasses[i]) hit = true;
                for (int i = 0; i < lc.focusOrigins.Length; i++) if (d.originTag == lc.focusOrigins[i]) hit = true;
                if (!hit) continue;
            }
            pool.Add(d);
        }
        if (pool.Count == 0) return RollShopKeyByLevel();
        return PickWeightedShopKey(pool);
    }

    #endregion
}
