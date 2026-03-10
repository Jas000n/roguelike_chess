using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
    #region Hex / Synergy

    private void RollHexOffers()
    {
        currentHexOffers.Clear();

        // 质量优化：已拿过的海克斯不再重复出现，保证构筑选择持续变化。
        var copy = new List<HexDef>();
        foreach (var h in hexPool)
        {
            if (!HasHex(h.id)) copy.Add(h);
        }

        // 如果海克斯池被拿空（理论上后期可能发生），允许从全池重抽，避免流程断裂。
        if (copy.Count == 0)
        {
            copy.AddRange(hexPool);
        }

        for (int i = 0; i < 3 && copy.Count > 0; i++)
        {
            int pick = UnityEngine.Random.Range(0, copy.Count);
            currentHexOffers.Add(copy[pick]);
            copy.RemoveAt(pick);
        }
    }

    private void PickHex(int idx)
    {
        if (idx < 0 || idx >= currentHexOffers.Count) return;
        var h = currentHexOffers[idx];

        // 防御式保护：避免重复添加同一海克斯导致意外叠层。
        if (!HasHex(h.id))
        {
            selectedHexes.Add(h);
            battleLog = $"获得海克斯：{h.name}";
        }
        else
        {
            gold += 4;
            battleLog = $"重复海克斯转化为 +4 金币：{h.name}";
        }

        AdvanceToStageMapFromCurrentNode();
    }

    private bool HasHex(string id)
    {
        foreach (var h in selectedHexes) if (h.id == id) return true;
        return false;
    }

    private int CountClass(List<Unit> team, string classTag)
    {
        int c = 0;
        // 设计改动：羁绊按“上阵阵容”计算，不因战斗中死亡而失效
        foreach (var u in team) if (u.ClassTag == classTag) c++;
        return c;
    }

    private int CountOrigin(List<Unit> team, string originTag)
    {
        int c = 0;
        foreach (var u in team) if (u.OriginTag == originTag) c++;
        return c;
    }

    private int CountFamily(List<Unit> team, string family)
    {
        int c = 0;
        foreach (var u in team) if (u.Family == family) c++;
        return c;
    }

    private int GetMaHouPaoLevel(List<Unit> team)
    {
        int horse = CountFamily(team, "马");
        int cannon = CountFamily(team, "炮");
        if (horse >= 2 && cannon >= 2) return 2;
        if (horse >= 1 && cannon >= 1) return 1;
        return 0;
    }

    private int GetShiXiangQuanLevel(List<Unit> team)
    {
        bool hasShi = CountFamily(team, "士") >= 1;
        bool hasXiang = CountFamily(team, "象") >= 1;
        bool hasShuai = CountFamily(team, "帅") >= 1;
        if (hasShi && hasXiang && hasShuai) return 2;
        if (hasShi && hasXiang) return 1;
        return 0;
    }

    private int GetCheBingTuiJinLevel(List<Unit> team)
    {
        int chariot = CountFamily(team, "车");
        int soldier = CountFamily(team, "兵");
        if (chariot >= 2 && soldier >= 3) return 2;
        if (chariot >= 1 && soldier >= 2) return 1;
        return 0;
    }

    private int GetChePaoLianYingLevel(List<Unit> team)
    {
        int chariot = CountFamily(team, "车");
        int cannon = CountFamily(team, "炮");
        if (chariot >= 2 && cannon >= 3) return 2;
        if (chariot >= 1 && cannon >= 2) return 1;
        return 0;
    }

    private int GetShuaiBingLingLevel(List<Unit> team)
    {
        int shuai = CountFamily(team, "帅");
        int soldier = CountFamily(team, "兵");
        if (shuai >= 1 && soldier >= 4) return 2;
        if (shuai >= 1 && soldier >= 2) return 1;
        return 0;
    }

    private string GetChessComboSummary(List<Unit> team)
    {
        var parts = new List<string>();
        int mhp = GetMaHouPaoLevel(team);
        int sxq = GetShiXiangQuanLevel(team);
        int cbt = GetCheBingTuiJinLevel(team);
        int cpl = GetChePaoLianYingLevel(team);
        int sbl = GetShuaiBingLingLevel(team);
        if (mhp >= 2) parts.Add("马后炮(2)");
        else if (mhp >= 1) parts.Add("马后炮(1)");
        if (sxq >= 2) parts.Add("士象全(2)");
        else if (sxq >= 1) parts.Add("士象全(1)");
        if (cbt >= 2) parts.Add("车兵推进(2)");
        else if (cbt >= 1) parts.Add("车兵推进(1)");
        if (cpl >= 2) parts.Add("车炮联营(2)");
        else if (cpl >= 1) parts.Add("车炮联营(1)");
        if (sbl >= 2) parts.Add("帅兵令(2)");
        else if (sbl >= 1) parts.Add("帅兵令(1)");
        return parts.Count == 0 ? "" : string.Join(" ", parts);
    }

    private string GetChessComboDesc(List<Unit> team)
    {
        int mhp = GetMaHouPaoLevel(team);
        int sxq = GetShiXiangQuanLevel(team);
        int cbt = GetCheBingTuiJinLevel(team);
        int cpl = GetChePaoLianYingLevel(team);
        int sbl = GetShuaiBingLingLevel(team);
        return
            $"马后炮(马+炮)：{(mhp == 0 ? "未激活" : mhp == 1 ? "1阶" : "2阶")}\n" +
            $"士象全(士+象+帅)：{(sxq == 0 ? "未激活" : sxq == 1 ? "1阶" : "2阶")}\n" +
            $"车兵推进(车+兵)：{(cbt == 0 ? "未激活" : cbt == 1 ? "1阶" : "2阶")}\n" +
            $"车炮联营(车+炮)：{(cpl == 0 ? "未激活" : cpl == 1 ? "1阶" : "2阶")}\n" +
            $"帅兵令(帅+兵)：{(sbl == 0 ? "未激活" : sbl == 1 ? "1阶" : "2阶")}";
    }

    private string GetClassCn(string classTag)
    {
        return classTag switch
        {
            "Vanguard" => "钢铁先锋",
            "Rider" => "机动骑兵",
            "Artillery" => "火力炮阵",
            "Leader" => "领袖",
            "Guardian" => "守护者",
            "Assassin" => "刺客",
            "Soldier" => "士兵",
            _ => classTag
        };
    }

    private string GetOriginCn(string originTag)
    {
        return originTag switch
        {
            "Steel" => "钢铁",
            "Blaze" => "烈焰",
            "Shadow" => "暗影",
            "Thunder" => "雷霆",
            "Night" => "夜幕",
            "Stone" => "岩石",
            "Holy" => "圣辉",
            "Frost" => "霜寒",
            "Wind" => "疾风",
            "Venom" => "毒蚀",
            "Mist" => "雾隐",
            _ => originTag
        };
    }

    private string GetSynergyEffectDesc(string classTag, int count)
    {
        return classTag switch
        {
            "Vanguard" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位减伤 +10% / +22%（海克斯可再叠加）",
            "Rider" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位速度 +2 / +5，首击突进更强",
            "Artillery" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位伤害 +12% / +22%，4层额外射程+1",
            "Assassin" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位暴击率 +20% / +45%，暴击伤害 +35%",
            "Guardian" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位减伤 +12% / +24%，并提供全队小额护卫减伤",
            "Soldier" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位普攻伤害 +10% / +22%",
            "Leader" => $"{GetClassCn(classTag)}：1生效，当前{count}。效果：全队伤害与速度小幅提升",
            _ => $"{GetClassCn(classTag)}：当前{count}"
        };
    }

    private string GetOriginEffectDesc(string originTag, int count)
    {
        return originTag switch
        {
            "Steel" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：本阵营单位减伤 +8% / +16%",
            "Blaze" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：本阵营单位伤害 +10% / +20%",
            "Shadow" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：概率爆发伤害",
            "Thunder" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：命中附带雷击伤害，每3次攻击触发连锁电弧",
            "Night" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：首次爆发伤害增强",
            "Stone" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：本阵营单位减伤 +6% / +14%",
            "Holy" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：本阵营单位命中后小幅回血",
            "Frost" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：命中附加破坚伤害，4层时额外溅射寒晶碎片",
            "Wind" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：本阵营单位速度 +2 / +4",
            "Venom" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：命中附加额外伤害",
            "Mist" => $"{GetOriginCn(originTag)}：2/4生效，当前{count}。效果：受击时有概率闪避",
            _ => $"{GetOriginCn(originTag)}：当前{count}"
        };
    }

    private string GetUnitsOfClassText(string classTag)
    {
        var names = new List<string>();
        foreach (var kv in unitDefs)
        {
            if (kv.Value.classTag == classTag) names.Add(kv.Value.name);
        }
        return names.Count == 0 ? "暂无" : string.Join("、", names);
    }

    private string GetUnitsOfOriginText(string originTag)
    {
        var names = new List<string>();
        foreach (var kv in unitDefs)
        {
            if (kv.Value.originTag == originTag) names.Add(kv.Value.name);
        }
        return names.Count == 0 ? "暂无" : string.Join("、", names);
    }

    private string GetInspectSynergyText(Unit u, List<Unit> team)
    {
        int c = CountClass(team, u.ClassTag);
        int o = CountOrigin(team, u.OriginTag);
        string combos = GetChessComboSummary(team);
        return $"可激活羁绊：{GetClassCn(u.ClassTag)}（当前上阵{c}）\n" +
               $"效果：{GetSynergyEffectDesc(u.ClassTag, c)}\n" +
               $"该羁绊棋子：{GetUnitsOfClassText(u.ClassTag)}\n" +
               $"可激活阵营：{GetOriginCn(u.OriginTag)}（当前上阵{o}）\n" +
               $"该阵营棋子：{GetUnitsOfOriginText(u.OriginTag)}" +
               (string.IsNullOrEmpty(combos) ? "" : $"\n棋型加成：{combos}") +
               $"\n{GetChessComboDesc(team)}";
    }

    private string GetSynergySummary(List<Unit> team)
    {
        string summary = "";
        var classCount = new Dictionary<string, int>();
        var originCount = new Dictionary<string, int>();
        for (int i = 0; i < team.Count; i++)
        {
            var u = team[i];
            if (u == null) continue;
            if (!classCount.ContainsKey(u.ClassTag)) classCount[u.ClassTag] = 0;
            if (!originCount.ContainsKey(u.OriginTag)) originCount[u.OriginTag] = 0;
            classCount[u.ClassTag]++;
            originCount[u.OriginTag]++;
        }

        foreach (var kv in classCount)
        {
            int need = kv.Key == "Leader" ? 1 : 2;
            if (kv.Value >= need) summary += $"{GetClassCn(kv.Key)}({kv.Value}) ";
        }
        foreach (var kv in originCount)
        {
            if (kv.Value >= 2) summary += $"{GetOriginCn(kv.Key)}源({kv.Value}) ";
        }
        string comboSummary = GetChessComboSummary(team);
        if (!string.IsNullOrEmpty(comboSummary)) summary += comboSummary;
        if (string.IsNullOrEmpty(summary)) summary = "暂无激活羁绊";
        return summary;
    }

    private CompDef GetLockedComp()
    {
        for (int i = 0; i < compDefs.Count; i++) if (compDefs[i].id == lockedCompId) return compDefs[i];
        return null;
    }

    private float GetCompProgress(CompDef c, List<Unit> team)
    {
        if (c == null || team == null) return 0f;
        int ca = CountClass(team, c.classA);
        int cb = CountClass(team, c.classB);
        float pa = Mathf.Clamp01(c.needClass2A <= 0 ? 1f : ca / (float)c.needClass2A);
        float pb = Mathf.Clamp01(c.needClass2B <= 0 ? 1f : cb / (float)c.needClass2B);
        return (pa + pb) * 0.5f;
    }

    private bool IsCompActive(CompDef c, List<Unit> team)
    {
        if (c == null || team == null) return false;
        return CountClass(team, c.classA) >= c.needClass2A && CountClass(team, c.classB) >= c.needClass2B;
    }

    private float GetLockedCompDamageBonus(Unit from)
    {
        var c = GetLockedComp();
        if (c == null) return 0f;
        var team = from.player ? playerUnits : enemyUnits;
        if (!IsCompActive(c, team)) return 0f;
        for (int i = 0; i < c.focusClasses.Length; i++) if (from.ClassTag == c.focusClasses[i]) return c.bonusDmg;
        return 0f;
    }

    private float GetLockedCompReductionBonus(Unit u)
    {
        var c = GetLockedComp();
        if (c == null) return 0f;
        var team = u.player ? playerUnits : enemyUnits;
        if (!IsCompActive(c, team)) return 0f;
        for (int i = 0; i < c.focusClasses.Length; i++) if (u.ClassTag == c.focusClasses[i]) return c.bonusReduction;
        return 0f;
    }

    private int GetLockedCompSpeedBonus(Unit u)
    {
        var c = GetLockedComp();
        if (c == null) return 0;
        var team = u.player ? playerUnits : enemyUnits;
        if (!IsCompActive(c, team)) return 0;
        for (int i = 0; i < c.focusClasses.Length; i++) if (u.ClassTag == c.focusClasses[i]) return c.bonusSpeed;
        return 0;
    }

    private string GetBuildSuggestionText()
    {
        return
            "推荐阵容:\n" +
            "1) 易成型-钢铁炮阵: 壁垒车/坦克车 + 连发炮/电弧炮, 前排抗住后排持续输出\n" +
            "2) 易成型-骑炮速攻: 突袭马/跑车 + 连发炮, 前中期节奏快\n" +
            "3) 难成型-四刺爆发: 暗影士/夜刃士/毒牙士/镜影士 + 梦魇马, 后期秒杀轴\n" +
            "4) 中速成型-岩壁要塞: 岩石前排 + 迫击炮慢推\n" +
            "5) 高难终局-双四: 4先锋+4炮, 依赖人口与经济运营";
    }

    private int GetCompPowerScore(List<Unit> team)
    {
        if (team == null) return 0;
        int score = 0;
        for (int i = 0; i < team.Count; i++)
        {
            var u = team[i];
            if (u == null || u.def == null) continue;
            score += u.def.cost * 10;
            score += u.star * 16;
            score += u.atk + u.maxHp / 5 + u.spd * 2;
        }
        int v = CountClass(team, "Vanguard");
        int r = CountClass(team, "Rider");
        int a = CountClass(team, "Artillery");
        int ass = CountClass(team, "Assassin");
        int g = CountClass(team, "Guardian");
        int s = CountClass(team, "Soldier");
        int l = CountClass(team, "Leader");
        int mhp = GetMaHouPaoLevel(team);
        int sxq = GetShiXiangQuanLevel(team);
        int cbt = GetCheBingTuiJinLevel(team);
        int cpl = GetChePaoLianYingLevel(team);
        int sbl = GetShuaiBingLingLevel(team);
        if (v >= 2) score += 40;
        if (v >= 4) score += 70;
        if (r >= 2) score += 35;
        if (r >= 4) score += 65;
        if (a >= 2) score += 38;
        if (a >= 4) score += 72;
        if (ass >= 2) score += 36;
        if (ass >= 4) score += 68;
        if (g >= 2) score += 30;
        if (g >= 4) score += 58;
        if (s >= 2) score += 26;
        if (s >= 4) score += 52;
        if (l >= 1) score += 22;
        if (mhp >= 1) score += 36;
        if (mhp >= 2) score += 40;
        if (sxq >= 1) score += 34;
        if (sxq >= 2) score += 46;
        if (cbt >= 1) score += 30;
        if (cbt >= 2) score += 38;
        if (cpl >= 1) score += 32;
        if (cpl >= 2) score += 42;
        if (sbl >= 1) score += 28;
        if (sbl >= 2) score += 36;
        return score;
    }

    private float GetCritMultiplier(Unit from)
    {
        if (from.ClassTag != "Assassin") return 1f;
        var team = from.player ? playerUnits : enemyUnits;
        int assassin = CountClass(team, "Assassin");
        float critChance = 0.1f;
        if (assassin >= 2) critChance += 0.2f;
        if (assassin >= 4) critChance += 0.45f;
        if (UnityEngine.Random.value < critChance) return 1.35f;
        return 1f;
    }

    private float GetDamageMultiplier(Unit from)
    {
        float m = 1f;
        var team = from.player ? playerUnits : enemyUnits;

        int art = CountClass(team, "Artillery");
        if (from.ClassTag == "Artillery")
        {
            if (art >= 2) m += 0.12f;
            if (art >= 4) m += 0.22f;
            if (HasHex("cannon_master")) m += 0.25f;
            if (HasHex("artillery_overclock")) m += 0.15f;
        }

        if (HasHex("team_atk")) m += 0.08f;
        if (HasHex("execution_edge")) m += 0.04f;
        m += GetLockedCompDamageBonus(from);

        int soldier = CountClass(team, "Soldier");
        if (from.ClassTag == "Soldier")
        {
            if (soldier >= 2) m += 0.10f;
            if (soldier >= 4) m += 0.22f;
        }

        int leader = CountClass(team, "Leader");
        if (leader >= 1) m += 0.06f;
        if (from.ClassTag == "Leader") m += 0.10f;

        int maHouPao = GetMaHouPaoLevel(team);
        if (maHouPao >= 1)
        {
            if (from.Family == "马") m += maHouPao >= 2 ? 0.18f : 0.10f;
            if (from.Family == "炮") m += maHouPao >= 2 ? 0.22f : 0.12f;
        }

        int shiXiangQuan = GetShiXiangQuanLevel(team);
        if (shiXiangQuan >= 1) m += shiXiangQuan >= 2 ? 0.08f : 0.04f;

        int cheBingTuiJin = GetCheBingTuiJinLevel(team);
        if (cheBingTuiJin >= 1)
        {
            if (from.Family == "车") m += cheBingTuiJin >= 2 ? 0.16f : 0.09f;
            if (from.Family == "兵") m += cheBingTuiJin >= 2 ? 0.14f : 0.08f;
        }

        int chePaoLianYing = GetChePaoLianYingLevel(team);
        if (chePaoLianYing >= 1)
        {
            if (from.Family == "车") m += chePaoLianYing >= 2 ? 0.14f : 0.08f;
            if (from.Family == "炮") m += chePaoLianYing >= 2 ? 0.18f : 0.10f;
        }

        int shuaiBingLing = GetShuaiBingLingLevel(team);
        if (shuaiBingLing >= 1 && from.Family == "兵")
        {
            m += shuaiBingLing >= 2 ? 0.18f : 0.10f;
        }

        int blaze = CountOrigin(team, "Blaze");
        if (from.OriginTag == "Blaze")
        {
            if (blaze >= 2) m += 0.10f;
            if (blaze >= 4) m += 0.20f;
        }
        int frost = CountOrigin(team, "Frost");
        if (from.OriginTag == "Frost")
        {
            if (frost >= 2) m += 0.08f;
            if (frost >= 4) m += 0.18f;
        }
        int venom = CountOrigin(team, "Venom");
        if (from.OriginTag == "Venom")
        {
            if (venom >= 2) m += 0.08f;
            if (venom >= 4) m += 0.18f;
            if (HasHex("venom_payload")) m += 0.08f;
        }

        return m;
    }

    private int GetSpeedBonus(Unit u)
    {
        int b = 0;
        var team = u.player ? playerUnits : enemyUnits;
        int rider = CountClass(team, "Rider");
        if (u.ClassTag == "Rider")
        {
            if (rider >= 2) b += 2;
            if (rider >= 4) b += 5;
            if (HasHex("rider_relay")) b += 2;
        }

        int wind = CountOrigin(team, "Wind");
        if (u.OriginTag == "Wind")
        {
            if (wind >= 2) b += 2;
            if (wind >= 4) b += 4;
            if (HasHex("windwalk")) b += 2;
        }
        int cheBingTuiJin = GetCheBingTuiJinLevel(team);
        if (cheBingTuiJin >= 1 && u.Family == "兵") b += cheBingTuiJin >= 2 ? 2 : 1;
        if (CountClass(team, "Leader") >= 1) b += 1;
        b += GetLockedCompSpeedBonus(u);
        return b;
    }

    private int GetRangeBonus(Unit u)
    {
        int b = 0;
        var team = u.player ? playerUnits : enemyUnits;
        int art = CountClass(team, "Artillery");
        if (u.ClassTag == "Artillery" && art >= 4) b += 1;
        if (u.ClassTag == "Artillery" && HasHex("artillery_range")) b += 1;
        int chePaoLianYing = GetChePaoLianYingLevel(team);
        if (u.Family == "炮" && chePaoLianYing >= 2) b += 1;
        return b;
    }

    private float GetDamageReduction(Unit u)
    {
        float r = 0f;
        var team = u.player ? playerUnits : enemyUnits;
        int van = CountClass(team, "Vanguard");
        if (u.ClassTag == "Vanguard")
        {
            if (van >= 2) r += 0.10f;
            if (van >= 4) r += 0.22f;
            if (HasHex("vanguard_wall")) r += 0.18f;
        }
        int guardian = CountClass(team, "Guardian");
        if (u.ClassTag == "Guardian")
        {
            if (guardian >= 2) r += 0.12f;
            if (guardian >= 4) r += 0.24f;
        }
        if (guardian >= 2) r += 0.04f;

        int steel = CountOrigin(team, "Steel");
        if (u.OriginTag == "Steel")
        {
            if (steel >= 2) r += 0.08f;
            if (steel >= 4) r += 0.16f;
        }
        int stone = CountOrigin(team, "Stone");
        if (u.OriginTag == "Stone")
        {
            if (stone >= 2) r += 0.06f;
            if (stone >= 4) r += 0.14f;
            if (HasHex("stone_oath")) r += 0.08f;
        }
        int mist = CountOrigin(team, "Mist");
        if (u.OriginTag == "Mist")
        {
            if (mist >= 2) r += 0.03f;
            if (mist >= 4) r += 0.08f;
        }
        int shiXiangQuan = GetShiXiangQuanLevel(team);
        if (shiXiangQuan >= 1) r += shiXiangQuan >= 2 ? 0.12f : 0.06f;
        int cheBingTuiJin = GetCheBingTuiJinLevel(team);
        if (cheBingTuiJin >= 1 && (u.Family == "车" || u.Family == "兵")) r += cheBingTuiJin >= 2 ? 0.11f : 0.06f;
        int shuaiBingLing = GetShuaiBingLingLevel(team);
        if (shuaiBingLing >= 1) r += shuaiBingLing >= 2 ? 0.08f : 0.04f;
        if (u.ClassTag == "Vanguard" && HasHex("vanguard_bastion")) r += 0.08f;
        r += GetLockedCompReductionBonus(u);
        return Mathf.Clamp01(r);
    }

    #endregion
}
