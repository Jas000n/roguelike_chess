using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public partial class RoguelikeFramework
{
    private class DevCompPlan
    {
        public string id;
        public string name;
        public string difficulty;
        public string[] classes;
        public string[] preferredKeys;
        public int rerollBudget;
    }

    private readonly List<DevCompPlan> devPlans = new()
    {
        new DevCompPlan
        {
            id = "easy_vanguard_artillery",
            name = "钢铁炮阵",
            difficulty = "易成型",
            classes = new[] { "Vanguard", "Artillery" },
            preferredKeys = new[] { "soldier_phalanx", "chariot_tank", "chariot_bulwark", "cannon_burst", "cannon_arc", "cannon_scout", "cannon_mortar" },
            rerollBudget = 1
        },
        new DevCompPlan
        {
            id = "easy_rider_artillery",
            name = "骑炮速攻",
            difficulty = "易成型",
            classes = new[] { "Rider", "Artillery" },
            preferredKeys = new[] { "horse_raider", "horse_banner", "chariot_sport", "cannon_burst", "cannon_scout", "cannon_sniper" },
            rerollBudget = 1
        },
        new DevCompPlan
        {
            id = "hard_assassin_4",
            name = "四刺爆发",
            difficulty = "难成型",
            classes = new[] { "Assassin", "Rider" },
            preferredKeys = new[] { "guard_assassin", "guard_blade", "guard_poison", "guard_mirror", "horse_nightmare" },
            rerollBudget = 3
        },
        new DevCompPlan
        {
            id = "hard_vanguard_4_artillery_4",
            name = "双四终局",
            difficulty = "高难终局",
            classes = new[] { "Vanguard", "Artillery" },
            preferredKeys = new[] { "soldier_phalanx", "chariot_tank", "chariot_shock", "chariot_bulwark", "chariot_ram", "cannon_scout", "cannon_missile", "cannon_mortar", "cannon_sniper", "cannon_arc" },
            rerollBudget = 4
        },
    };

    // 开发快捷：一键推进一小步，降低手动回归成本。
    private void DevAdvanceOneStep()
    {
        switch (state)
        {
            case RunState.Stage:
                StartPreparationForCurrentStage();
                battleLog = "[DEV] Stage -> Prepare";
                break;
            case RunState.Prepare:
                StartBattle();
                battleLog = "[DEV] Prepare -> Battle";
                break;
            case RunState.Battle:
                EndBattle(true);
                battleLog = "[DEV] Force Win -> Reward/Next";
                break;
            case RunState.Reward:
                if (currentRewardOffers.Count == 0) RollRewardOffers();
                PickReward(0);
                break;
            case RunState.Hex:
                if (currentHexOffers.Count == 0) RollHexOffers();
                PickHex(0);
                break;
            case RunState.GameOver:
                RestartRun();
                break;
        }
    }

    private void RestartRun()
    {
        stageIndex = 0;
        gold = 10;
        playerLevel = 1;
        exp = 0;
        playerLife = 36;
        winStreak = 0;
        loseStreak = 0;
        lockedCompId = "";
        lockShop = false;
        compMilestoneRewarded.Clear();
        selectedHexes.Clear();
        currentRewardOffers.Clear();
        currentHexOffers.Clear();
        pendingHexAfterReward = false;
        benchUnits.Clear();
        deploySlots.Clear();
        benchUnits.Add(CreateUnit("soldier_sword", true));
        benchUnits.Add(CreateUnit("horse_raider", true));
        benchUnits.Add(CreateUnit("cannon_burst", true));
        state = RunState.Stage;
        battleLog = "新一轮开始";
        RedrawPrepareBoard();
    }

    // 最小自动回归：自动推进 3 关，校验核心状态机闭环不被改坏。
    private void DevRunRegression3Floors()
    {
        int startFloor = stageIndex;
        int target = Mathf.Min(stages.Count, startFloor + 3);
        int safety = 80;
        int steps = 0;
        bool blocked = false;

        while (stageIndex < target && state != RunState.GameOver && safety-- > 0)
        {
            steps++;
            switch (state)
            {
                case RunState.Stage:
                    StartPreparationForCurrentStage();
                    break;
                case RunState.Prepare:
                    if (deploySlots.Count == 0) AutoDeployFallback();
                    StartBattle();
                    // 回归模式下快速收敛，避免等待实时回合
                    if (state == RunState.Battle && battleStarted) EndBattle(true);
                    break;
                case RunState.Battle:
                    if (!battleStarted) { blocked = true; }
                    else EndBattle(true);
                    break;
                case RunState.Reward:
                    if (currentRewardOffers.Count == 0) RollRewardOffers();
                    PickReward(0);
                    break;
                case RunState.Hex:
                    if (currentHexOffers.Count == 0) RollHexOffers();
                    PickHex(0);
                    break;
                case RunState.GameOver:
                    blocked = true;
                    break;
            }

            if (blocked) break;
        }

        bool pass = stageIndex >= target;
        string result = pass
            ? $"[DEV] 3关回归通过 | {startFloor + 1}->{target} | steps:{steps} | life:{playerLife} gold:{gold}"
            : $"[DEV] 3关回归未通过 | state:{state} floor:{stageIndex + 1} target:{target} steps:{steps}";

        battleLog = result;
        Debug.Log(result);
    }

    private void DevRunBalanceIterations(int rounds)
    {
        if (rounds < 1) rounds = 1;
        rounds = Mathf.Clamp(rounds, 1, 200);

        int clear8 = 0;
        int died = 0;
        int sumLife = 0;
        int sumGold = 0;
        int sumFloor = 0;
        int easyPick = 0;
        int hardPick = 0;
        int ass4Hits = 0;
        int van4Hits = 0;
        int art4Hits = 0;

        var planPickStats = new Dictionary<string, int>();
        foreach (var p in devPlans) planPickStats[p.name] = 0;

        for (int i = 0; i < rounds; i++)
        {
            RestartRun();
            var plan = devPlans[UnityEngine.Random.Range(0, devPlans.Count)];
            planPickStats[plan.name]++;
            if (plan.difficulty.Contains("易")) easyPick++; else hardPick++;

            int safety = 400;
            while (state != RunState.GameOver && safety-- > 0)
            {
                switch (state)
                {
                    case RunState.Stage:
                        StartPreparationForCurrentStage();
                        break;
                    case RunState.Prepare:
                        DevAutoBuildBoard(plan);
                        StartBattle();
                        DevResolveBattleFast();
                        break;
                    case RunState.Reward:
                        if (currentRewardOffers.Count == 0) RollRewardOffers();
                        PickReward(DevPickRewardIndex(plan));
                        break;
                    case RunState.Hex:
                        if (currentHexOffers.Count == 0) RollHexOffers();
                        PickHex(DevPickHexIndex(plan));
                        break;
                    case RunState.Battle:
                        DevResolveBattleFast();
                        break;
                }
            }

            int curFloor = Mathf.Clamp(stageIndex + 1, 1, stages.Count);
            sumFloor += curFloor;
            sumLife += playerLife;
            sumGold += gold;

            int ass = CountClass(deploySlots, "Assassin");
            int van = CountClass(deploySlots, "Vanguard");
            int art = CountClass(deploySlots, "Artillery");
            if (ass >= 4) ass4Hits++;
            if (van >= 4) van4Hits++;
            if (art >= 4) art4Hits++;

            if (stageIndex >= stages.Count) clear8++;
            if (playerLife <= 0) died++;
        }

        string planStats = "";
        foreach (var kv in planPickStats) planStats += $"{kv.Key}:{kv.Value} ";
        battleLog = $"[DEV] 平衡{rounds}轮 | 8关通关:{clear8} | 平均到达关卡:{(sumFloor / (float)rounds):0.00} | 平均生命:{(sumLife / (float)rounds):0.0} | 平均金币:{(sumGold / (float)rounds):0.0} | 易/难:{easyPick}/{hardPick} | 4刺:{ass4Hits} 4先锋:{van4Hits} 4炮:{art4Hits}";
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {battleLog} | 阵容分布 {planStats}";
        Debug.Log(line);
        DevWriteBalanceReport(line, rounds);
    }

    private void DevWriteBalanceReport(string line, int rounds)
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "DevReports");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"balance_{rounds}_report.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.Log($"[DEV] 写入平衡报告失败: {e.Message}");
        }
    }

    private void DevResolveBattleFast()
    {
        if (state != RunState.Battle || !battleStarted) return;
        int guard = 420;
        while (state == RunState.Battle && battleStarted && guard-- > 0)
        {
            RunOneTurn();
            CheckBattleEnd();
        }
        if (guard <= 0 && state == RunState.Battle)
        {
            EndBattle(true);
            battleLog = "[DEV] 战斗超时，强制结算胜利";
        }
    }

    private void DevAutoBuildBoard(DevCompPlan plan)
    {
        if (plan == null) return;

        int rerolls = 0;
        while (rerolls < plan.rerollBudget && gold > 2)
        {
            int idx = DevFindBestShopOffer(plan);
            if (idx >= 0)
            {
                BuyOffer(idx);
            }
            else
            {
                RefreshShop();
                rerolls++;
            }
        }

        // 尝试把当前商店里最匹配的全吃掉，确保“易成型”能快速落地。
        int loops = 0;
        while (loops++ < 8)
        {
            int idx = DevFindBestShopOffer(plan);
            if (idx < 0) break;
            var def = unitDefs[shopOffers[idx]];
            if (gold < def.cost || benchUnits.Count >= 8) break;
            BuyOffer(idx);
        }

        DevRebuildDeployByPlan(plan);
    }

    private int DevFindBestShopOffer(DevCompPlan plan)
    {
        int bestIdx = -1;
        int bestScore = int.MinValue;
        for (int i = 0; i < shopOffers.Count; i++)
        {
            var def = unitDefs[shopOffers[i]];
            if (gold < def.cost || benchUnits.Count >= 8) continue;
            int score = DevScoreDefForPlan(def, plan);
            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = i;
            }
        }
        return bestIdx;
    }

    private int DevScoreDefForPlan(UnitDef def, DevCompPlan plan)
    {
        int score = def.cost * 2 + def.atk + def.hp / 8 + def.spd;
        for (int i = 0; i < plan.classes.Length; i++)
        {
            if (def.classTag == plan.classes[i]) score += 38;
        }
        for (int i = 0; i < plan.preferredKeys.Length; i++)
        {
            if (def.key == plan.preferredKeys[i]) score += 26;
        }
        if (plan.difficulty.Contains("易") && def.cost <= 3) score += 18;
        if (!plan.difficulty.Contains("易") && def.cost >= 3) score += 12;
        return score;
    }

    private void DevRebuildDeployByPlan(DevCompPlan plan)
    {
        var all = new List<Unit>();
        all.AddRange(deploySlots);
        all.AddRange(benchUnits);
        if (all.Count == 0) return;

        all.Sort((a, b) =>
        {
            int sa = DevScoreUnitForPlan(a, plan);
            int sb = DevScoreUnitForPlan(b, plan);
            return sb.CompareTo(sa);
        });

        int cap = GetBoardCap();
        var selected = new List<Unit>();
        for (int i = 0; i < all.Count && selected.Count < cap; i++)
        {
            selected.Add(all[i]);
        }

        deploySlots.Clear();
        benchUnits.Clear();

        int frontRow = 1;
        int midRow = 2;
        int backRow = 3;
        int fx = 0;
        int mx = 0;
        int bx = 0;

        for (int i = 0; i < selected.Count; i++)
        {
            var u = selected[i];
            if (u.ClassTag == "Vanguard")
            {
                u.x = Mathf.Clamp(fx++, 0, 4);
                u.y = frontRow;
            }
            else if (u.ClassTag == "Artillery")
            {
                u.x = Mathf.Clamp(bx++, 0, 4);
                u.y = backRow;
            }
            else
            {
                u.x = Mathf.Clamp(mx++, 0, 4);
                u.y = midRow;
            }
            deploySlots.Add(u);
        }

        for (int i = selected.Count; i < all.Count && benchUnits.Count < 8; i++)
        {
            all[i].x = -1;
            all[i].y = -1;
            benchUnits.Add(all[i]);
        }

        AutoMergeAll();
        RedrawPrepareBoard();
    }

    private int DevScoreUnitForPlan(Unit u, DevCompPlan plan)
    {
        int score = u.star * 30 + u.atk + u.maxHp / 6 + u.spd + u.def.cost * 4;
        for (int i = 0; i < plan.classes.Length; i++)
        {
            if (u.ClassTag == plan.classes[i]) score += 42;
        }
        for (int i = 0; i < plan.preferredKeys.Length; i++)
        {
            if (u.def.key == plan.preferredKeys[i]) score += 25;
        }
        return score;
    }

    private int DevPickRewardIndex(DevCompPlan plan)
    {
        int idxExp = -1;
        int idxUnit = -1;
        int idxGold = -1;
        for (int i = 0; i < currentRewardOffers.Count; i++)
        {
            var id = currentRewardOffers[i].id;
            if (id == "exp") idxExp = i;
            if (id == "unit_low") idxUnit = i;
            if (id == "gold_big") idxGold = i;
        }

        if (plan.difficulty.Contains("高难") || plan.difficulty.Contains("难"))
        {
            if (idxExp >= 0) return idxExp;
            if (idxUnit >= 0) return idxUnit;
        }
        if (idxGold >= 0) return idxGold;
        return 0;
    }

    private int DevPickHexIndex(DevCompPlan plan)
    {
        int best = 0;
        int bestScore = int.MinValue;
        for (int i = 0; i < currentHexOffers.Count; i++)
        {
            var h = currentHexOffers[i];
            int score = 0;
            if (h.id == "board_plus") score += 30;
            if (h.id == "fast_train") score += 24;
            if (h.id == "rich") score += 20;
            if (h.id == "interest_up") score += 16;
            if (h.id == "team_atk") score += 14;
            if (h.id == "healing") score += 10;
            if (h.id == "cannon_master" && HasPlanClass(plan, "Artillery")) score += 18;
            if (h.id == "artillery_range" && HasPlanClass(plan, "Artillery")) score += 15;
            if (h.id == "vanguard_wall" && HasPlanClass(plan, "Vanguard")) score += 15;
            if (h.id == "rider_charge" && HasPlanClass(plan, "Rider")) score += 15;

            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        return best;
    }

    private bool HasPlanClass(DevCompPlan plan, string cls)
    {
        for (int i = 0; i < plan.classes.Length; i++) if (plan.classes[i] == cls) return true;
        return false;
    }


    #region Core Flow

    private void StartPreparationForCurrentStage()
    {
        if (stageIndex >= stages.Count)
        {
            state = RunState.GameOver;
            battleLog = "通关！你完成了线性章节。";
            return;
        }

        state = RunState.Prepare;
        battleStarted = false;
        inspectedUnit = null;
        showTooltip = false;

        int roundBaseGold = 5;
        int streakGold = winStreak >= 2 ? Mathf.Min(3, winStreak / 2) : (loseStreak >= 2 ? Mathf.Min(2, loseStreak / 2) : 0);
        int interest = Mathf.Min(GetInterestCap(), gold / 10);
        int hexBonus = HasHex("rich") ? 4 : 0;

        gold += roundBaseGold + streakGold + interest + hexBonus;

        int expGain = 2 + (HasHex("fast_train") ? 2 : 0);
        GainExp(expGain);

        if (HasHex("healing"))
        {
            foreach (var u in deploySlots)
            {
                u.hp = Mathf.Min(u.maxHp, u.hp + Mathf.RoundToInt(u.maxHp * 0.2f));
            }
        }

        RefreshShop(true);
        AutoMergeAll();
        RedrawPrepareBoard();

        // 低血保底：每关仅触发一次，避免“完全无操作空间”的挫败。
        if (playerLife <= 12 && lastEmergencyStage != stageIndex)
        {
            lastEmergencyStage = stageIndex;
            gold += 3;
            playerLife = Mathf.Min(36, playerLife + 3);
            if (benchUnits.Count < 8)
            {
                string ek = RollShopKeyByLevel();
                benchUnits.Add(CreateUnit(ek, true));
                battleLog = $"濒危补给触发：+3金币 +3生命 +1单位({unitDefs[ek].name})";
                PushEvent("濒危补给触发（低血保底）");
            }
            else
            {
                battleLog = "濒危补给触发：+3金币 +3生命";
                PushEvent("濒危补给触发（低血保底）");
            }
        }

        var st = stages[stageIndex];
        battleLog = $"准备阶段：第{st.floor}关({st.type}) | +{roundBaseGold}+利息{interest}+连胜/败{streakGold}";
    }

    private void StartBattle()
    {
        if (stageIndex >= stages.Count) return;

        var st = stages[stageIndex];
        state = RunState.Battle;
        battleStarted = true;
        battleStartedTurn = 0;
        turnIndex = 0;
        inspectedUnit = null;
        showTooltip = false;
        battleLog = $"战斗开始：第{st.floor}关 {st.type}";
        var lc = GetLockedComp();
        if (lc != null && IsCompActive(lc, deploySlots))
        {
            battleLog += $" | 阵容成型加成已激活：{lc.name}";
            if (!compMilestoneRewarded.Contains(lc.id))
            {
                compMilestoneRewarded.Add(lc.id);
                gold += 3;
                battleLog += " | 成型里程碑 +3金币";
                PushEvent($"成型里程碑达成：{lc.name} (+3金币)");
            }
        }

        playerUnits.Clear();
        enemyUnits.Clear();
        ClearViews();
        DrawBoard();

        // 玩家单位
        if (deploySlots.Count == 0)
        {
            AutoDeployFallback();
        }

        int[,] fallbackPos = { { 0, 1 }, { 0, 2 }, { 0, 3 }, { 1, 1 }, { 1, 2 }, { 1, 3 }, { 2, 2 }, { 2, 3 } };
        int maxDeploy = GetBoardCap();
        for (int i = 0; i < deploySlots.Count && i < maxDeploy; i++)
        {
            var u = CloneUnit(deploySlots[i]);
            u.player = true;
            u.usedCharge = false;
            if (u.x < 0 || u.x >= W || u.y < 0 || u.y >= H)
            {
                u.x = fallbackPos[i, 0];
                u.y = fallbackPos[i, 1];
            }
            else
            {
                // 准备阶段直接在战场坐标布阵
                u.y = Mathf.Clamp(u.y, 0, H - 1);
            }
            playerUnits.Add(u);
        }

        SpawnEnemiesForStage(st);

        CreateViews(playerUnits, new Color(0.2f, 0.7f, 1f));
        CreateViews(enemyUnits, new Color(0.95f, 0.35f, 0.4f));
        RefreshViews();
    }

    private void SpawnEnemiesForStage(StageNode st)
    {
        int enemyCount = Mathf.Clamp(2 + st.power, 3, 7);
        int[,] pos = { { 7, 2 }, { 8, 1 }, { 8, 2 }, { 8, 3 }, { 9, 1 }, { 9, 2 }, { 9, 4 }, { 7, 4 } };

        for (int i = 0; i < enemyCount; i++)
        {
            string key = PickEnemyUnitKey(st);
            var u = CreateUnit(key, false);

            float hpScale = 1f + (st.power - 1) * 0.15f;
            float atkScale = 1f + (st.power - 1) * 0.11f;
            u.hp = Mathf.RoundToInt(u.hp * hpScale);
            u.maxHp = u.hp;
            u.atk = Mathf.RoundToInt(u.atk * atkScale);
            u.spd += Mathf.FloorToInt((st.power - 1) * 0.5f);

            if (st.type == StageType.Elite || st.type == StageType.Boss || UnityEngine.Random.value < Mathf.Clamp01((st.floor - 2) * 0.12f))
            {
                UpgradeUnit(u);
                if (st.type == StageType.Boss && UnityEngine.Random.value < 0.45f) UpgradeUnit(u); // 有机会3星
            }

            u.x = pos[i, 0];
            u.y = pos[i, 1];
            enemyUnits.Add(u);
        }
    }

    private string PickEnemyUnitKey(StageNode st)
    {
        if (st.type == StageType.Boss)
        {
            string[] bossLike = { "chariot_tank", "cannon_missile", "horse_nightmare", "general_fire" };
            return bossLike[UnityEngine.Random.Range(0, bossLike.Length)];
        }
        return basePool[UnityEngine.Random.Range(0, basePool.Count)];
    }

    private string BuildBattleOutcomeDetail(bool win)
    {
        int allyAlive = playerUnits.FindAll(u => u.Alive).Count;
        int enemyAlive = enemyUnits.FindAll(u => u.Alive).Count;

        if (win)
        {
            return $"我方存活:{allyAlive} 敌方存活:{enemyAlive}";
        }

        int enemyHpLeft = 0;
        Unit topThreat = null;
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            var e = enemyUnits[i];
            if (!e.Alive) continue;
            enemyHpLeft += Mathf.Max(0, e.hp);
            if (topThreat == null || e.damageDealt > topThreat.damageDealt) topThreat = e;
        }

        string threatTxt = topThreat == null
            ? "关键威胁:无"
            : $"关键威胁:{topThreat.Name} 造成{topThreat.damageDealt}伤害";

        return $"我方存活:{allyAlive} 敌方存活:{enemyAlive} 敌方剩余生命:{enemyHpLeft} | {threatTxt}";
    }

    private void EndBattle(bool win)
    {
        battleStarted = false;

        if (win)
        {
            lastBattleWin = true;
            winStreak++;
            loseStreak = 0;
            int reward = 8 + Mathf.Min(5, stages[stageIndex].power);
            var lc = GetLockedComp();
            if (lc != null && IsCompActive(lc, deploySlots)) reward += 2;
            gold += reward;
            battleLog = $"胜利！+{reward}金币 | {BuildBattleOutcomeDetail(true)}";
            lastBattleSummary = $"胜利结算：+{reward}金币";
            PushEvent($"胜利结算 +{reward} 金币");
            if (winStreak >= 3 && winStreak % 3 == 0)
            {
                gold += 3;
                battleLog += " | 连胜爆发：额外 +3金币";
                lastBattleSummary += " | 连胜爆发+3金币";
                PushEvent("连胜爆发 +3 金币");
            }
        }
        else
        {
            lastBattleWin = false;
            loseStreak++;
            winStreak = 0;
            int reward = 4;
            gold += reward;

            int lifeLoss = Mathf.Clamp(2 + stages[stageIndex].power, 2, 12);
            playerLife -= lifeLoss;
            battleLog = $"失败，保底 +{reward}金币 | 生命 -{lifeLoss} | {BuildBattleOutcomeDetail(false)}";
            lastBattleSummary = $"失败结算：保底+{reward}金币，生命-{lifeLoss}";
            PushEvent($"失败结算 生命-{lifeLoss}");

            if (playerLife <= 0)
            {
                playerLife = 0;
                state = RunState.GameOver;
                battleLog += " | 生命耗尽，挑战结束";
                return;
            }
        }

        pendingHexAfterReward = stages[stageIndex].giveHex;
        stageIndex++;

        if (stageIndex >= stages.Count)
        {
            state = RunState.GameOver;
            battleLog += " | 章节结束";
            return;
        }

        RollRewardOffers();
        state = RunState.Reward;
        battleLog += " | 战后奖励三选一";
    }

    #endregion

}
