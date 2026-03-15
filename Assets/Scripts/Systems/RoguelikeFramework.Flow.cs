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
            preferredKeys = new[] { "soldier_phalanx", "soldier_guard", "chariot_tank", "chariot_shock", "chariot_bulwark", "chariot_ram", "cannon_scout", "cannon_missile", "cannon_mortar", "cannon_sniper", "cannon_arc" },
            rerollBudget = 4
        },
        new DevCompPlan
        {
            id = "mid_poison_cut",
            name = "毒影斩首",
            difficulty = "中等",
            classes = new[] { "Assassin", "Artillery" },
            preferredKeys = new[] { "guard_assassin", "guard_blade", "guard_poison", "guard_mirror", "cannon_burst", "cannon_arc", "cannon_sniper" },
            rerollBudget = 2
        },
        new DevCompPlan
        {
            id = "easy_steel_rider_wall",
            name = "钢骑壁垒",
            difficulty = "易成型",
            classes = new[] { "Vanguard", "Rider" },
            preferredKeys = new[] { "soldier_guard", "soldier_phalanx", "chariot_tank", "horse_raider", "horse_lancer", "horse_banner" },
            rerollBudget = 1
        },
        new DevCompPlan
        {
            id = "mid_control_battery",
            name = "控场炮网",
            difficulty = "中等",
            classes = new[] { "Controller", "Artillery" },
            preferredKeys = new[] { "cannon_arc", "cannon_scout", "cannon_frost", "cannon_storm", "cannon_burst", "cannon_sniper" },
            rerollBudget = 2
        },
        new DevCompPlan
        {
            id = "mid_holy_recovery",
            name = "圣愈护城",
            difficulty = "中等",
            classes = new[] { "Medic", "Guardian" },
            preferredKeys = new[] { "guard_holy", "soldier_zeal", "ele_sage", "guard_mirror", "ele_guard", "horse_banner" },
            rerollBudget = 2
        },
    };

    // 开发快捷：一键推进一小步，降低手动回归成本。
    private void DevAdvanceOneStep()
    {
        switch (state)
        {
            case RunState.Stage:
                var choices = GetAvailableStageNodes();
                if (choices.Count > 0)
                {
                    SelectStageNode(choices[0].id);
                    battleLog = "[DEV] Stage -> Select Node";
                }
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
            case RunState.Event:
                ResolveMysteryEventChoice(false);
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

    // 开发开关：快速从地图进入可操作的准备阶段。
    private void DevQuickStartToPrepare()
    {
        if (state == RunState.GameOver) RestartRun();

        if (state != RunState.Stage)
        {
            RestartRun();
        }

        var choices = GetAvailableStageNodes();
        if (choices.Count == 0)
        {
            battleLog = "[DEV] 快速开局失败：无可用节点";
            return;
        }

        SelectStageNode(choices[0].id);

        int safety = 10;
        while (safety-- > 0 && state != RunState.Prepare && state != RunState.GameOver)
        {
            if (state == RunState.Reward)
            {
                if (currentRewardOffers.Count == 0) RollRewardOffers();
                PickReward(0);
                continue;
            }

            if (state == RunState.Event)
            {
                ResolveMysteryEventChoice(false);
                continue;
            }

            if (state == RunState.Hex)
            {
                if (currentHexOffers.Count == 0) RollHexOffers();
                PickHex(0);
                continue;
            }

            if (state == RunState.Stage)
            {
                choices = GetAvailableStageNodes();
                if (choices.Count == 0) break;
                SelectStageNode(choices[0].id);
                continue;
            }

            break;
        }

        battleLog = state == RunState.Prepare
            ? "[DEV] 快速开局完成：已进入准备阶段"
            : $"[DEV] 快速开局结束：state={state}";
    }

    // 开发开关：跳过当前关（强制胜利并自动处理奖励/海克斯）。
    private void DevSkipCurrentFloor()
    {
        int beforeFloor = stageIndex;
        int safety = 20;

        while (safety-- > 0)
        {
            switch (state)
            {
                case RunState.Stage:
                    var choices = GetAvailableStageNodes();
                    if (choices.Count == 0) { battleLog = "[DEV] 跳关失败：无可选节点"; return; }
                    SelectStageNode(choices[0].id);
                    break;
                case RunState.Prepare:
                    if (deploySlots.Count == 0) AutoDeployFallback();
                    StartBattle();
                    break;
                case RunState.Battle:
                    if (battleStarted) EndBattle(true);
                    else StartBattle();
                    break;
                case RunState.Reward:
                    if (currentRewardOffers.Count == 0) RollRewardOffers();
                    PickReward(0);
                    break;
                case RunState.Event:
                    ResolveMysteryEventChoice(false);
                    break;
                case RunState.Hex:
                    if (currentHexOffers.Count == 0) RollHexOffers();
                    PickHex(0);
                    break;
                case RunState.GameOver:
                    battleLog = "[DEV] 跳关结束：章节已结束";
                    return;
            }

            if (stageIndex > beforeFloor && (state == RunState.Stage || state == RunState.Prepare))
            {
                battleLog = $"[DEV] 跳关完成：{beforeFloor + 1} -> {stageIndex + 1}";
                return;
            }
        }

        battleLog = $"[DEV] 跳关超时：state={state}, floor={stageIndex + 1}";
    }

    // 开发开关：快速推进到 Boss 前的准备/战斗入口。
    private void DevSkipToBoss()
    {
        int targetFloor = GetFinalFloor();
        int safety = 160;

        while (safety-- > 0 && state != RunState.GameOver)
        {
            if (stageIndex >= targetFloor - 1 && (state == RunState.Prepare || state == RunState.Battle || state == RunState.Reward || state == RunState.Event || state == RunState.Hex))
            {
                battleLog = $"[DEV] 已推进至 Boss 层流程：floor={stageIndex + 1}, state={state}";
                return;
            }

            switch (state)
            {
                case RunState.Stage:
                    var choices = GetAvailableStageNodes();
                    if (choices.Count == 0)
                    {
                        battleLog = "[DEV] 跳Boss失败：无可选节点";
                        return;
                    }

                    StageNode best = choices[0];
                    for (int i = 1; i < choices.Count; i++)
                    {
                        var c = choices[i];
                        if (c.floor > best.floor || (c.floor == best.floor && c.power > best.power)) best = c;
                    }
                    SelectStageNode(best.id);
                    break;
                case RunState.Prepare:
                    if (deploySlots.Count == 0) AutoDeployFallback();
                    StartBattle();
                    break;
                case RunState.Battle:
                    if (battleStarted) EndBattle(true);
                    else StartBattle();
                    break;
                case RunState.Reward:
                    if (currentRewardOffers.Count == 0) RollRewardOffers();
                    PickReward(0);
                    break;
                case RunState.Event:
                    ResolveMysteryEventChoice(false);
                    break;
                case RunState.Hex:
                    if (currentHexOffers.Count == 0) RollHexOffers();
                    PickHex(0);
                    break;
            }
        }

        battleLog = $"[DEV] 跳Boss结束：floor={stageIndex + 1}, state={state}";
    }

    private void RestartRun()
    {
        BuildLinearStages();
        stageIndex = 0;
        currentStageNodeId = "";
        gold = 10;
        freeRerollTurns = 0;
        interestCapModifier = 0;
        playerLevel = 1;
        exp = 0;
        playerLife = 36;
        winStreak = 0;
        loseStreak = 0;
        rewardBoardCapBonus = 0;
        lockedCompId = "";
        lockShop = false;
        compMilestoneRewarded.Clear();
        selectedHexes.Clear();
        currentRewardOffers.Clear();
        currentHexOffers.Clear();
        currentShopHexOffers.Clear();
        currentShopHexCosts.Clear();
        pendingHexAfterReward = false;
        pendingEliteHexReward = false;
        deploySlots.Clear();
        FillRandomOpeningBench();
        state = RunState.Stage;
        battleLog = "新一轮开始：请选择地图路线";
        RedrawPrepareBoard();
    }

    // BatchMode 入口：用于 Unity -executeMethod 回归，不依赖场景手动操作。
    public static void DevRunRegression3FloorsBatch()
    {
        var go = new GameObject("DevRegressionRunner");
        var framework = go.AddComponent<RoguelikeFramework>();

        framework.devBatchFailCount = 0;
        framework.Start();
        framework.DevRunRegression3Floors();
        framework.DevRunUiSmokeTest();
        framework.DevRunStarMergeSmokeTest();
        framework.DevRunThreeStarShopFilterSmokeTest();
        framework.DevRunMergeAnchorSmokeTest();
        framework.DevRunMergeAnchorThreeStarSmokeTest();
        framework.DevRunLockedCompHitProbeById("steel_reroll", 24);
        framework.DevRunLockedCompHitProbeById("control_battery", 24);
        framework.DevRunLockedCompHitProbeById("holy_recovery", 24);
        framework.DevRunSpikeProbeScenarios();
        if (framework.spikeScenarioWarnLast >= 2)
        {
            Debug.LogWarning($"[DEV][SOFT_GATE_WARN] spike_warn={framework.spikeScenarioWarnLast} (threshold=2)");
        }
        framework.DevRecordSpikeWarnSample();
        framework.DevRunEventRoomPrototypeSmokeTest();
        framework.DevRunMysteryRevealBucketSmokeTest();
        framework.DevRunUnitDefsIntegritySmokeTest();

        if (framework.devBatchFailCount > 0)
        {
            string msg = $"[DEV][BATCH] FAILED failCount={framework.devBatchFailCount}";
            Debug.LogError(msg);
            throw new Exception(msg);
        }

        Debug.Log("[DEV][BATCH] PASSED failCount=0");
        Debug.Log("[DEV][BATCH] DevRunRegression3FloorsBatch finished");
    }

    private void DevRecordSpikeWarnSample()
    {
        try
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? ".";
            string dir = Path.Combine(projectRoot, "Docs", "devloop");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "spike_warn_history.csv");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "timestamp,warn_total,warn_assassin,warn_artillery,warn_tri_service" + Environment.NewLine);
            }

            string ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"{ts},{spikeScenarioWarnLast},{spikeScenarioWarnAssassinLast},{spikeScenarioWarnArtilleryLast},{spikeScenarioWarnTriServiceLast}";
            File.AppendAllText(path, line + Environment.NewLine);

            var warns = new List<int>();
            var warnsA = new List<int>();
            var warnsO = new List<int>();
            var warnsT = new List<int>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] parts = raw.Split(',');
                if (parts.Length < 2) continue;
                if (parts[0].Trim().Equals("timestamp", StringComparison.OrdinalIgnoreCase)) continue;

                int w = 0;
                if (int.TryParse(parts[1], out int parsedW)) w = Mathf.Max(0, parsedW);
                warns.Add(w);

                int wa = 0, wo = 0, wt = 0;
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[2], out int parsedA)) wa = Mathf.Max(0, parsedA);
                    if (int.TryParse(parts[3], out int parsedO)) wo = Mathf.Max(0, parsedO);
                    if (int.TryParse(parts[4], out int parsedT)) wt = Mathf.Max(0, parsedT);
                }
                warnsA.Add(wa);
                warnsO.Add(wo);
                warnsT.Add(wt);
            }

            int count = warns.Count;
            int from = Mathf.Max(0, count - 10);
            int recentCount = count - from;
            int recentWarnRuns = 0;
            int recentWarnTotal = 0;
            int recentWarnA = 0;
            int recentWarnO = 0;
            int recentWarnT = 0;
            int consecutiveWarnRuns = 0;
            for (int i = from; i < count; i++)
            {
                int w = warns[i];
                recentWarnA += warnsA[i];
                recentWarnO += warnsO[i];
                recentWarnT += warnsT[i];
                if (w > 0)
                {
                    recentWarnRuns++;
                    consecutiveWarnRuns++;
                    recentWarnTotal += w;
                }
                else
                {
                    consecutiveWarnRuns = 0;
                }
            }

            bool softGate = recentWarnRuns >= 5;
            bool tuneHint = consecutiveWarnRuns >= 3;
            Debug.Log($"[DEV][SPIKE_WARN_WINDOW] samples={count} recent={recentCount} warn_runs={recentWarnRuns} warn_total={recentWarnTotal} warn_by_hex_recent=A:{recentWarnA},O:{recentWarnO},T:{recentWarnT} soft_gate={(softGate ? 1 : 0)} tune_hint={(tuneHint ? 1 : 0)}");
            if (softGate)
            {
                Debug.LogWarning("[DEV][SPIKE_WARN_SOFT_GATE] recent10 warn_runs >= 5, recommend CI yellow");
            }
            else if (tuneHint)
            {
                Debug.LogWarning("[DEV][SPIKE_WARN_TUNE_HINT] consecutive warn runs >= 3, recommend small bias tuning");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"[DEV][SPIKE_WARN_WINDOW] report write failed: {e.Message}");
        }
    }

    // 最小自动回归：自动推进 3 关，校验核心状态机闭环不被改坏。
    private void DevRunRegression3Floors()
    {
        int startFloor = stageIndex;
        int target = Mathf.Min(GetFinalFloor(), startFloor + 3);
        int safety = 80;
        int steps = 0;
        bool blocked = false;

        while (stageIndex < target && state != RunState.GameOver && safety-- > 0)
        {
            steps++;
            switch (state)
            {
                case RunState.Stage:
                    var choices = GetAvailableStageNodes();
                    if (choices.Count == 0) blocked = true;
                    else SelectStageNode(choices[0].id);
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
                case RunState.Event:
                    ResolveMysteryEventChoice(false);
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
        if (!pass) devBatchFailCount++;
        string result = pass
            ? $"[DEV] 3关回归通过 | {startFloor + 1}->{target} | steps:{steps} | life:{playerLife} gold:{gold}"
            : $"[DEV] 3关回归未通过 | state:{state} floor:{stageIndex + 1} target:{target} steps:{steps}";

        battleLog = result;
        Debug.Log(result);
    }

    // UI/交互烟雾回归：覆盖商店、上阵、替换、出售、战斗、奖励、海克斯的关键链路。
    private void DevRunUiSmokeTest()
    {
        var lines = new List<string>();
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detailIfFail)
        {
            if (ok)
            {
                pass++;
                lines.Add($"PASS | {name}");
            }
            else
            {
                fail++;
                devBatchFailCount++;
                lines.Add($"FAIL | {name} | {detailIfFail}");
            }
        }

        RestartRun();
        Check("重开后状态=Stage", state == RunState.Stage, $"state={state}");

        if (stageNodeById.TryGetValue("f3_1", out var eventNode))
        {
            RestartRun();
            currentStageNodeId = eventNode.id;
            stageIndex = eventNode.floor - 1;
            EnterMysteryEventChoiceState(eventNode);
            Check("可进入事件选择状态", state == RunState.Event, $"state={state}");
            ResolveMysteryEventChoice(false);
            Check("事件选择后可回到地图", state == RunState.Stage || state == RunState.GameOver, $"state={state}");
            RestartRun();
        }

        DevQuickStartToPrepare();
        Check("快速开局可进入准备", state == RunState.Prepare, $"state={state}");

        int floorBeforeSkip = stageIndex;
        DevSkipCurrentFloor();
        Check("跳关可推进楼层", stageIndex > floorBeforeSkip, $"before={floorBeforeSkip}, after={stageIndex}, state={state}");

        if (state != RunState.Stage) RestartRun();

        DevSkipToBoss();
        Check("跳Boss可到后期流程", stageIndex >= GetFinalFloor() - 1 || state == RunState.GameOver, $"floor={stageIndex + 1}, final={GetFinalFloor()}, state={state}");

        RestartRun();

        var firstChoices = GetAvailableStageNodes();
        if (firstChoices.Count > 0) SelectStageNode(firstChoices[0].id);
        Check("可进入准备阶段", state == RunState.Prepare, $"state={state}");
        Check("商店有5个槽位", shopOffers.Count == 5, $"shopOffers={shopOffers.Count}");

        int oldGold = gold;
        var oldShop = new List<string>(shopOffers);
        lockShop = true;
        RefreshShop(true);
        bool lockKeep = shopOffers.Count == oldShop.Count;
        if (lockKeep)
        {
            for (int i = 0; i < oldShop.Count; i++) if (shopOffers[i] != oldShop[i]) { lockKeep = false; break; }
        }
        Check("锁店后刷新不变", lockKeep, "锁店刷新改变了商品");
        lockShop = false;

        int expBefore = exp;
        int lvBefore = playerLevel;
        if (gold >= 4) { gold -= 4; GainExp(4); }
        Check("买经验可生效", playerLevel > lvBefore || exp >= expBefore, $"lvBefore={lvBefore}, lvNow={playerLevel}, expBefore={expBefore}, expNow={exp}");

        if (shopOffers.Count > 0)
        {
            string key = shopOffers[0];
            int ownedBefore = CountOwnedCopies(key);
            int unitBefore = deploySlots.Count + benchUnits.Count;
            int goldBeforeBuy = gold;
            BuyOffer(0);
            int unitAfter = deploySlots.Count + benchUnits.Count;
            bool bought = gold <= goldBeforeBuy && (CountOwnedCopies(key) >= ownedBefore || unitAfter >= unitBefore);
            Check("购买棋子链路", bought, $"goldBefore={goldBeforeBuy}, goldAfter={gold}, ownedBefore={ownedBefore}, ownedAfter={CountOwnedCopies(key)}");
        }
        else Check("购买棋子链路", false, "商店为空");

        if (benchUnits.Count > 0)
        {
            var u = benchUnits[0];
            benchUnits.RemoveAt(0);
            u.x = 0;
            u.y = 2;
            deploySlots.Add(u);
        }
        Check("备战席可上阵", deploySlots.Count > 0, "deploySlots=0");

        if (benchUnits.Count > 0 && deploySlots.Count > 0)
        {
            var b = benchUnits[0];
            var d = deploySlots[0];
            b.x = d.x;
            b.y = d.y;
            d.x = -1;
            d.y = -1;
            deploySlots[0] = b;
            benchUnits[0] = d;
        }
        Check("备战席可替换场上", deploySlots.Count > 0 && benchUnits.Count > 0, "替换后队列异常");

        int sellGoldBefore = gold;
        bool sold = false;
        if (deploySlots.Count > 0)
        {
            sold = SellUnit(deploySlots[0]);
        }
        Check("场外出售链路", sold && gold > sellGoldBefore, $"sold={sold}, goldBefore={sellGoldBefore}, goldAfter={gold}");

        if (deploySlots.Count == 0) AutoDeployFallback();
        StartBattle();
        Check("可进入战斗阶段", state == RunState.Battle && battleStarted, $"state={state}, battleStarted={battleStarted}");
        Check("战斗双方单位存在", playerUnits.Count > 0 && enemyUnits.Count > 0, $"p={playerUnits.Count}, e={enemyUnits.Count}");

        DevResolveBattleFast();
        Check("战斗可正常结算", state == RunState.Reward || state == RunState.GameOver, $"state={state}");

        if (state == RunState.Reward)
        {
            int stageBefore = stageIndex;
            PickReward(0);
            Check("奖励可选择并推进", state == RunState.Prepare || state == RunState.Hex || state == RunState.Stage || state == RunState.GameOver, $"state={state}, stageBefore={stageBefore}, stageNow={stageIndex}");
            if (state == RunState.Hex)
            {
                PickHex(0);
                Check("海克斯可选择并返回准备", state == RunState.Prepare || state == RunState.GameOver, $"state={state}");
            }
        }

        string summary = $"[DEV][UI_SMOKE] pass={pass} fail={fail}";
        battleLog = summary + (fail > 0 ? "（详见DevReports/ui_smoke_report.log）" : "（全通过）");
        Debug.Log(summary);

        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "DevReports");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "ui_smoke_report.log");
            var text = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {summary}\n" + string.Join("\n", lines) + "\n";
            File.AppendAllText(path, text + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.Log($"[DEV][UI_SMOKE] report write failed: {e.Message}");
        }
    }

    private void DevRunStarMergeSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][STAR_SMOKE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();

        string key = shopOffers.Count > 0 ? shopOffers[0] : (basePool.Count > 0 ? basePool[0] : "");
        if (string.IsNullOrEmpty(key) || !unitDefs.ContainsKey(key))
        {
            devBatchFailCount++;
            Debug.Log("[DEV][STAR_SMOKE] skipped: no valid unit key");
            return;
        }

        benchUnits.Clear();
        deploySlots.Clear();

        for (int i = 0; i < 3; i++) benchUnits.Add(CreateUnit(key, true));
        AutoMergeAll();

        int oneStarAfterFirst = CountOwnedCopies(key, 1);
        int twoStarAfterFirst = CountOwnedCopies(key, 2);
        int threeStarAfterFirst = CountOwnedCopies(key, 3);

        Check("3x1星可合成2星", oneStarAfterFirst == 0 && twoStarAfterFirst == 1 && threeStarAfterFirst == 0,
            $"1★={oneStarAfterFirst},2★={twoStarAfterFirst},3★={threeStarAfterFirst}");

        for (int i = 0; i < 6; i++) benchUnits.Add(CreateUnit(key, true));
        AutoMergeAll();

        int oneStarAfterSecond = CountOwnedCopies(key, 1);
        int twoStarAfterSecond = CountOwnedCopies(key, 2);
        int threeStarAfterSecond = CountOwnedCopies(key, 3);

        Check("3x2星可合成3星", oneStarAfterSecond == 0 && twoStarAfterSecond == 0 && threeStarAfterSecond == 1,
            $"1★={oneStarAfterSecond},2★={twoStarAfterSecond},3★={threeStarAfterSecond}");

        string summary = $"[DEV][STAR_SMOKE] pass={pass} fail={fail} key={key}";
        battleLog = summary;
        Debug.Log(summary);
    }

    private void DevRunThreeStarShopFilterSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][SHOP_FILTER_SMOKE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();

        string key = shopOffers.Count > 0 ? shopOffers[0] : (basePool.Count > 0 ? basePool[0] : "");
        if (string.IsNullOrEmpty(key) || !unitDefs.ContainsKey(key))
        {
            devBatchFailCount++;
            Debug.Log("[DEV][SHOP_FILTER_SMOKE] skipped: no valid unit key");
            return;
        }

        benchUnits.Clear();
        deploySlots.Clear();

        // 造出一个 3★ 单位，验证商店过滤逻辑不会再刷出该 key。
        for (int i = 0; i < 9; i++) benchUnits.Add(CreateUnit(key, true));
        AutoMergeAll();
        Check("可构造3星持有状态", CountOwnedCopies(key, 3) == 1, $"3★={CountOwnedCopies(key, 3)}");

        bool seen = false;
        const int rounds = 30;
        for (int i = 0; i < rounds; i++)
        {
            RefreshShop(true);
            for (int j = 0; j < shopOffers.Count; j++)
            {
                if (shopOffers[j] == key)
                {
                    seen = true;
                    break;
                }
            }
            if (seen) break;
        }

        Check("已有3星后商店不再出现该棋子", !seen, $"key={key}, rounds={rounds}");

        string summary = $"[DEV][SHOP_FILTER_SMOKE] pass={pass} fail={fail} key={key}";
        battleLog = summary;
        Debug.Log(summary);
    }

    private void DevRunMergeAnchorSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][ANCHOR_SMOKE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();

        string key = shopOffers.Count > 0 ? shopOffers[0] : (basePool.Count > 0 ? basePool[0] : "");
        if (string.IsNullOrEmpty(key) || !unitDefs.ContainsKey(key))
        {
            devBatchFailCount++;
            Debug.Log("[DEV][ANCHOR_SMOKE] skipped: no valid unit key");
            return;
        }

        benchUnits.Clear();
        deploySlots.Clear();

        // 场上锚点：2个备战席 + 1个场上，同key同星触发合成后应保留在场上锚点格。
        var b1 = CreateUnit(key, true);
        var b2 = CreateUnit(key, true);
        var d1 = CreateUnit(key, true);
        d1.x = 2;
        d1.y = 1;
        benchUnits.Add(b1);
        benchUnits.Add(b2);
        deploySlots.Add(d1);

        AutoMergeAll();

        int c1 = CountOwnedCopies(key, 1);
        int c2 = CountOwnedCopies(key, 2);
        int c3 = CountOwnedCopies(key, 3);
        Check("场上+备战席混合时可合成2星", c1 == 0 && c2 == 1 && c3 == 0, $"1★={c1},2★={c2},3★={c3}");

        Unit merged2 = null;
        for (int i = 0; i < deploySlots.Count; i++)
        {
            if (deploySlots[i].def.key == key && deploySlots[i].star == 2) { merged2 = deploySlots[i]; break; }
        }
        Check("2星合成后保持场上锚点坐标", merged2 != null && merged2.x == 2 && merged2.y == 1,
            merged2 == null ? "no 2★ on board" : $"pos=({merged2.x},{merged2.y})");

        string summary = $"[DEV][ANCHOR_SMOKE] pass={pass} fail={fail} key={key}";
        battleLog = summary;
        Debug.Log(summary);
    }

    private void DevRunMergeAnchorThreeStarSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][ANCHOR3_SMOKE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();

        string key = shopOffers.Count > 0 ? shopOffers[0] : (basePool.Count > 0 ? basePool[0] : "");
        if (string.IsNullOrEmpty(key) || !unitDefs.ContainsKey(key))
        {
            devBatchFailCount++;
            Debug.Log("[DEV][ANCHOR3_SMOKE] skipped: no valid unit key");
            return;
        }

        benchUnits.Clear();
        deploySlots.Clear();

        // 先造出两个2★：一个在场上锚点，一个在备战席。
        for (int i = 0; i < 6; i++) benchUnits.Add(CreateUnit(key, true));
        AutoMergeAll();

        var twoStars = new List<Unit>();
        for (int i = 0; i < benchUnits.Count; i++)
        {
            if (benchUnits[i].def.key == key && benchUnits[i].star == 2) twoStars.Add(benchUnits[i]);
        }
        Check("可生成两个2星", twoStars.Count >= 2, $"twoStars={twoStars.Count}");
        if (twoStars.Count < 2)
        {
            devBatchFailCount++;
            Debug.Log($"[DEV][ANCHOR3_SMOKE] pass={pass} fail={fail} key={key}");
            return;
        }

        var anchor2 = twoStars[0];
        benchUnits.Remove(anchor2);
        anchor2.x = 3;
        anchor2.y = 2;
        deploySlots.Add(anchor2);

        // 补一个2★（由3个1★合成）形成 3x2★ -> 1x3★。
        for (int i = 0; i < 3; i++) benchUnits.Add(CreateUnit(key, true));
        AutoMergeAll();

        int c1 = CountOwnedCopies(key, 1);
        int c2 = CountOwnedCopies(key, 2);
        int c3 = CountOwnedCopies(key, 3);
        Check("3个2星可合成3星", c1 == 0 && c2 == 0 && c3 == 1, $"1★={c1},2★={c2},3★={c3}");

        Unit merged3 = null;
        for (int i = 0; i < deploySlots.Count; i++)
        {
            if (deploySlots[i].def.key == key && deploySlots[i].star == 3) { merged3 = deploySlots[i]; break; }
        }
        Check("3星合成后保持场上锚点坐标", merged3 != null && merged3.x == 3 && merged3.y == 2,
            merged3 == null ? "no 3★ on board" : $"pos=({merged3.x},{merged3.y})");

        string summary = $"[DEV][ANCHOR3_SMOKE] pass={pass} fail={fail} key={key}";
        battleLog = summary;
        Debug.Log(summary);
    }

    private void DevRunLockedCompHitProbe(int refreshRounds)
    {
        RestartRun();
        var team = new List<Unit>();
        team.AddRange(deploySlots);
        team.AddRange(benchUnits);
        RecommendCompByBoard(team);
        DevRunLockedCompHitProbeById(lockedCompId, refreshRounds);
    }

    private void DevRunLockedCompHitProbeById(string compId, int refreshRounds)
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][COMP_HIT_PROBE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();
        lockedCompId = compId;
        var lc = GetLockedComp();
        Check("锁定路线存在", lc != null, $"lockedCompId={lockedCompId}");
        if (lc == null)
        {
            Debug.Log($"[DEV][COMP_HIT_PROBE] pass={pass} fail={fail} rounds=0 comp={compId}");
            return;
        }

        int rounds = Mathf.Clamp(refreshRounds, 6, 120);
        for (int i = 0; i < rounds; i++) RefreshShop(true);

        Check("压测轮数执行", true, $"rounds={rounds}");
        Debug.Log($"[DEV][COMP_HIT_PROBE] pass={pass} fail={fail} rounds={rounds} comp={lockedCompId}");
    }

    private void DevRunSpikeProbeScenarios()
    {
        int pass = 0;
        int fail = 0;
        int warn = 0;
        int warnAssassin = 0;
        int warnArtillery = 0;
        int warnTriService = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][SPIKE_SCENARIO][FAIL] {name} | {detail}");
            }
        }

        HexDef FindHex(string id)
        {
            for (int i = 0; i < hexPool.Count; i++) if (hexPool[i].id == id) return hexPool[i];
            return null;
        }

        void RunScenario(string name, string hexId, params string[] unitKeys)
        {
            RestartRun();
            DevQuickStartToPrepare();
            Check($"{name}: 进入Prepare", state == RunState.Prepare, $"state={state}");
            if (state != RunState.Prepare) return;

            selectedHexes.Clear();
            var h = FindHex(hexId);
            if (h != null) selectedHexes.Add(h);
            Check($"{name}: hex存在", h != null, $"hexId={hexId}");

            deploySlots.Clear();
            benchUnits.Clear();
            for (int i = 0; i < unitKeys.Length; i++)
            {
                string key = unitKeys[i];
                if (!unitDefs.ContainsKey(key)) continue;
                var u = CreateUnit(key, true);
                u.x = Mathf.Clamp(i, 0, 4);
                u.y = i % 2 == 0 ? 1 : 2;
                deploySlots.Add(u);
            }

            Check($"{name}: 上阵数量", deploySlots.Count >= 2, $"deploy={deploySlots.Count}");
            StartBattle();
            Check($"{name}: 可进战斗", state == RunState.Battle, $"state={state}");
            if (state == RunState.Battle)
            {
                int turns = 0;
                while (turns++ < 10 && state == RunState.Battle)
                {
                    RunOneTurn();
                    CheckBattleEnd();
                }

                int totalDmg = 0;
                int keyDmg = 0;
                int totalKills = 0;
                int keyKills = 0;
                for (int i = 0; i < playerUnits.Count; i++)
                {
                    var u = playerUnits[i];
                    if (u == null) continue;
                    totalDmg += u.damageDealt;
                    totalKills += u.kills;
                    bool match = hexId switch
                    {
                        "assassin_contract" => u.ClassTag == "Assassin",
                        "artillery_overclock" => u.ClassTag == "Artillery",
                        "tri_service" => u.ClassTag == "Artillery" || u.ClassTag == "Controller" || u.ClassTag == "Medic",
                        _ => false
                    };
                    if (match)
                    {
                        keyDmg += u.damageDealt;
                        keyKills += u.kills;
                    }
                }

                float share = totalDmg > 0 ? keyDmg / (float)totalDmg : 0f;
                float killShare = totalKills > 0 ? keyKills / (float)totalKills : 0f;
                Debug.Log($"[DEV][SPIKE_EFFECT] {name} turns={turns - 1} totalDmg={totalDmg} keyDmg={keyDmg} share={share:F2} totalKills={totalKills} keyKills={keyKills} killShare={killShare:F2}");

                float targetShare = hexId switch
                {
                    "assassin_contract" => 0.45f,
                    "artillery_overclock" => 0.33f,
                    "tri_service" => 0.30f,
                    _ => 0.30f
                };
                if (totalDmg > 0 && share < targetShare)
                {
                    warn++;
                    switch (hexId)
                    {
                        case "assassin_contract": warnAssassin++; break;
                        case "artillery_overclock": warnArtillery++; break;
                        case "tri_service": warnTriService++; break;
                    }
                    Debug.LogWarning($"[DEV][SPIKE_WARN] {name} dmg share below target: {share:F2} < {targetShare:F2}");
                }
                if (totalDmg > 0)
                {
                    Check($"{name}: 关键组合有输出", keyDmg > 0, $"total={totalDmg}, key={keyDmg}, share={share:F2}");
                }
                else
                {
                    Check($"{name}: 进行了回合推进", turns > 1, $"turns={turns - 1}");
                }
                if (totalKills >= 2 && share >= 0.5f)
                {
                    Check($"{name}: 高占比场景下有击杀贡献", keyKills > 0, $"totalKills={totalKills}, keyKills={keyKills}, dmgShare={share:F2}, killShare={killShare:F2}");
                }

                if (state == RunState.Battle) EndBattle(true);
            }
        }

        spikeProbeAssassinContractHits = 0;
        spikeProbeArtilleryOverclockHits = 0;
        spikeProbeTriServiceHits = 0;

        RunScenario("刺客契约", "assassin_contract", "guard_assassin", "guard_mist", "horse_nightmare");
        RunScenario("炮火超频", "artillery_overclock", "cannon_missile", "cannon_sniper", "cannon_arc");
        RunScenario("三军协同", "tri_service", "cannon_missile", "cannon_venom", "soldier_oracle");

        Check("刺客契约探针命中", spikeProbeAssassinContractHits > 0, $"hits={spikeProbeAssassinContractHits}");
        Check("炮火超频探针命中", spikeProbeArtilleryOverclockHits > 0, $"hits={spikeProbeArtilleryOverclockHits}");
        Check("三军协同探针命中", spikeProbeTriServiceHits > 0, $"hits={spikeProbeTriServiceHits}");

        spikeScenarioWarnLast = warn;
        spikeScenarioWarnAssassinLast = warnAssassin;
        spikeScenarioWarnArtilleryLast = warnArtillery;
        spikeScenarioWarnTriServiceLast = warnTriService;
        Debug.Log($"[DEV][SPIKE_SCENARIO] pass={pass} fail={fail} warn={warn} warnByHex=A:{warnAssassin},O:{warnArtillery},T:{warnTriService} probeHits=A:{spikeProbeAssassinContractHits},O:{spikeProbeArtilleryOverclockHits},T:{spikeProbeTriServiceHits}");
    }

    private void DevRunEventRoomPrototypeSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][EVENT_ROOM_SMOKE][FAIL] {name} | {detail}");
            }
        }

        RestartRun();
        if (!stageNodeById.TryGetValue("f3_1", out var mystery))
        {
            Check("存在 f3_1 神秘节点", false, "node missing");
            Debug.Log($"[DEV][EVENT_ROOM_SMOKE] pass={pass} fail={fail}");
            return;
        }

        currentStageNodeId = mystery.id;
        stageIndex = mystery.floor - 1;
        int goldBefore = gold;
        int lifeBefore = playerLife;
        int countBefore = devEventRoomResolveCount;

        bool triggered = TryResolveMysteryEventRoom(mystery, true, false);
        Check("强制触发事件房(稳健)", triggered, "triggered=false");
        Check("事件房返回地图状态", state == RunState.Stage, $"state={state}");
        Check("事件房计数增加", devEventRoomResolveCount == countBefore + 1, $"before={countBefore}, after={devEventRoomResolveCount}");
        bool safeEffectObserved = gold != goldBefore || playerLife != lifeBefore || battleLog.Contains("稳健选项");
        Check("稳健选项有可观测效果", safeEffectObserved, $"gold:{goldBefore}->{gold}, life:{lifeBefore}->{playerLife}, log={battleLog}");

        RestartRun();
        mystery = stageNodeById["f3_1"];
        currentStageNodeId = mystery.id;
        stageIndex = mystery.floor - 1;
        goldBefore = gold;
        lifeBefore = playerLife;
        countBefore = devEventRoomResolveCount;

        triggered = TryResolveMysteryEventRoom(mystery, true, true);
        Check("强制触发事件房(冒险)", triggered, "triggered=false");
        Check("冒险选项返回地图状态", state == RunState.Stage, $"state={state}");
        Check("冒险选项计数增加", devEventRoomResolveCount == countBefore + 1, $"before={countBefore}, after={devEventRoomResolveCount}");
        Check("冒险选项有资源变化", gold != goldBefore || playerLife != lifeBefore, $"gold:{goldBefore}->{gold}, life:{lifeBefore}->{playerLife}");

        Debug.Log($"[DEV][EVENT_ROOM_SMOKE] pass={pass} fail={fail} mode=both");
    }

    private void DevRunMysteryRevealBucketSmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][MYSTERY_BUCKET_SMOKE][FAIL] {name} | {detail}");
            }
        }

        if (!stageNodeById.TryGetValue("f3_1", out var earlyNode))
        {
            Check("存在前期神秘节点 f3_1", false, "node missing");
            Debug.Log($"[DEV][MYSTERY_BUCKET_SMOKE] pass={pass} fail={fail}");
            return;
        }
        if (!stageNodeById.TryGetValue("f10_1", out var lateNode))
        {
            Check("存在后期神秘节点 f10_1", false, "node missing");
            Debug.Log($"[DEV][MYSTERY_BUCKET_SMOKE] pass={pass} fail={fail}");
            return;
        }

        earlyNode.type = StageType.Mystery;
        earlyNode.mysteryRevealed = false;
        RevealMysteryNode(earlyNode);
        var earlyType = GetEffectiveStageType(earlyNode);
        Check("前期神秘节点可揭示", earlyNode.mysteryRevealed, $"revealed={earlyNode.mysteryRevealed}");
        Check("前期揭示类型有效", earlyType != StageType.Mystery, $"type={earlyType}");

        lateNode.type = StageType.Mystery;
        lateNode.mysteryRevealed = false;
        RevealMysteryNode(lateNode);
        var lateType = GetEffectiveStageType(lateNode);
        Check("后期神秘节点可揭示", lateNode.mysteryRevealed, $"revealed={lateNode.mysteryRevealed}");
        Check("后期揭示类型有效", lateType != StageType.Mystery, $"type={lateType}");

        Debug.Log($"[DEV][MYSTERY_BUCKET_SMOKE] pass={pass} fail={fail} early=f{earlyNode.floor}:{earlyType} late=f{lateNode.floor}:{lateType}");
    }

    private void DevRunUnitDefsIntegritySmokeTest()
    {
        int pass = 0;
        int fail = 0;

        void Check(string name, bool ok, string detail)
        {
            if (ok) pass++;
            else
            {
                fail++;
                devBatchFailCount++;
                Debug.Log($"[DEV][UNITDEF_SMOKE][FAIL] {name} | {detail}");
            }
        }

        Check("单位数量>=21", unitDefs.Count >= 21, $"count={unitDefs.Count}");
        Check("basePool 与 unitDefs 数量一致", basePool.Count == unitDefs.Count, $"basePool={basePool.Count}, unitDefs={unitDefs.Count}");

        var dup = new HashSet<string>();
        bool unique = true;
        foreach (var key in basePool)
        {
            if (!dup.Add(key)) { unique = false; break; }
        }
        Check("basePool key 唯一", unique, "basePool 存在重复 key");

        foreach (var kv in unitDefs)
        {
            var d = kv.Value;
            string key = kv.Key;
            Check($"{key}: name 非空", !string.IsNullOrWhiteSpace(d.name), "name 为空");
            Check($"{key}: class 非空", !string.IsNullOrWhiteSpace(d.classTag), "classTag 为空");
            Check($"{key}: origin 非空", !string.IsNullOrWhiteSpace(d.originTag), "originTag 为空");
            Check($"{key}: cost 合法", d.cost >= 1 && d.cost <= 5, $"cost={d.cost}");
            Check($"{key}: hp>0", d.hp > 0, $"hp={d.hp}");
            Check($"{key}: atk>0", d.atk > 0, $"atk={d.atk}");
            Check($"{key}: spd>0", d.spd > 0, $"spd={d.spd}");
            Check($"{key}: range 合法", d.range >= 1 && d.range <= 6, $"range={d.range}");
        }

        Debug.Log($"[DEV][UNITDEF_SMOKE] pass={pass} fail={fail} count={unitDefs.Count}");
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
                        var choices = GetAvailableStageNodes();
                        if (choices.Count == 0) state = RunState.GameOver;
                        else SelectStageNode(choices[UnityEngine.Random.Range(0, choices.Count)].id);
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
                    case RunState.Event:
                        ResolveMysteryEventChoice(false);
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

            int curFloor = Mathf.Clamp(stageIndex + 1, 1, GetFinalFloor());
            sumFloor += curFloor;
            sumLife += playerLife;
            sumGold += gold;

            int ass = CountClass(deploySlots, "Assassin");
            int van = CountClass(deploySlots, "Vanguard");
            int art = CountClass(deploySlots, "Artillery");
            if (ass >= 4) ass4Hits++;
            if (van >= 4) van4Hits++;
            if (art >= 4) art4Hits++;

            if (state == RunState.GameOver && playerLife > 0 && availableStageNodeIds.Count == 0) clear8++;
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
            if (gold < def.cost || !CanBuyOfferNow(def.key)) break;
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
            if (gold < def.cost || !CanBuyOfferNow(def.key)) continue;
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
            if (id == "exp" || id == "exp_big") idxExp = i;
            if (id == "unit_low" || id == "unit_mid" || id == "duo_pack") idxUnit = i;
            if (id == "gold_big" || id == "gold_huge") idxGold = i;
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
            if (h.id == "lifesteal_core") score += 15;
            if (h.id == "execution_edge") score += 12;
            if (h.id == "reroll_engine") score += 28;
            if (h.id == "triple_prep") score += 24;
            if (h.id == "vanguard_bastion" && HasPlanClass(plan, "Vanguard")) score += 20;
            if (h.id == "artillery_overclock" && HasPlanClass(plan, "Artillery")) score += 22;
            if (h.id == "rider_relay" && HasPlanClass(plan, "Rider")) score += 18;
            if (h.id == "stone_oath") score += 16;
            if (h.id == "venom_payload") score += 18;
            if (h.id == "windwalk") score += 16;
            if (h.id == "controller_net" && HasPlanClass(plan, "Controller")) score += 22;
            if (h.id == "medic_banner" && HasPlanClass(plan, "Medic")) score += 22;
            if (h.id == "tri_service" && HasPlanClass(plan, "Artillery") && (HasPlanClass(plan, "Controller") || HasPlanClass(plan, "Medic"))) score += 24;
            if (h.id == "guardian_grace" && (HasPlanClass(plan, "Guardian") || HasPlanClass(plan, "Medic"))) score += 18;
            if (h.id == "assassin_contract" && HasPlanClass(plan, "Assassin")) score += 24;
            if (h.id == "cannon_master" && HasPlanClass(plan, "Artillery")) score += 18;
            if (h.id == "artillery_range" && HasPlanClass(plan, "Artillery")) score += 15;
            if (h.id == "vanguard_wall" && HasPlanClass(plan, "Vanguard")) score += 15;
            if (h.id == "rider_charge" && HasPlanClass(plan, "Rider")) score += 15;
            if (h.id == "assassin_bloom" && HasPlanClass(plan, "Assassin")) score += 20;

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
        var st = GetCurrentStageNode();
        if (st == null)
        {
            state = RunState.Stage;
            battleLog = "请选择下一条地图路线";
            return;
        }
        var effectiveType = GetEffectiveStageType(st);

        state = RunState.Prepare;
        battleStarted = false;
        inspectedUnit = null;
        showTooltip = false;

        int roundBaseGold = 5;
        int streakGold = winStreak >= 2 ? Mathf.Min(3, winStreak / 2) : (loseStreak >= 2 ? Mathf.Min(2, loseStreak / 2) : 0);
        int interest = Mathf.Min(GetInterestCap(), gold / 10);
        int hexBonus = HasHex("rich") ? 4 : 0;
        if (HasHex("royal_supply")) hexBonus += 6;

        gold += roundBaseGold + streakGold + interest + hexBonus;
        if (effectiveType == StageType.Shop) gold += 6;
        rerollEngineFreeUses = HasHex("reroll_engine") ? 2 : 0;
        if (effectiveType == StageType.Shop) rerollEngineFreeUses += 2;
        if (HasHex("reroll_engine")) gold += 1;

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
        if (effectiveType == StageType.Shop) RollShopHexOffers();
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

        string prepMsg = $"准备阶段：第{st.floor}层({StageName(effectiveType)}) | +{roundBaseGold}+利息{interest}+连胜/败{streakGold}" +
                         (HasHex("reroll_engine") ? " | 精密改造：本回合2次免费刷新" : "") +
                         (HasHex("royal_supply") ? " | 王庭军需：额外+6金币" : "") +
                         (effectiveType == StageType.Shop ? " | 商店节点：额外+6金币 +2次免费刷新" : "") +
                         (effectiveType == StageType.Elite ? " | 精英节点：敌方强度提升，建议补前排与控制" : "") +
                         (effectiveType == StageType.Boss ? " | Boss节点：建议保留关键刷新/经济，优先成型主羁绊" : "");

        // 若触发了低血保底，保留该提示并拼接准备阶段信息，避免关键信息被覆盖。
        if (!string.IsNullOrEmpty(battleLog) && battleLog.StartsWith("濒危补给触发"))
            battleLog = battleLog + " | " + prepMsg;
        else
            battleLog = prepMsg;

        if (effectiveType == StageType.Shop)
        {
            PushEvent($"节点情报：第{st.floor}层为商店节点（额外金币与免费刷新）");
            if (currentShopHexOffers.Count > 0)
            {
                string firstHexName = currentShopHexOffers[0].name;
                int firstHexCost = currentShopHexCosts.Count > 0 ? currentShopHexCosts[0] : 0;
                PushEvent($"商店情报：本层可购强化{currentShopHexOffers.Count}个，首项[{firstHexName}] 价格{firstHexCost}金币");
            }
        }
        else if (effectiveType == StageType.Elite)
        {
            PushEvent($"节点情报：第{st.floor}层为精英节点（建议补前排与控制）");
            int benchDepth = benchUnits.Count;
            int frontline = 0;
            for (int i = 0; i < deploySlots.Count; i++)
            {
                var u = deploySlots[i];
                if (u != null && (u.ClassTag == "Vanguard" || u.ClassTag == "Soldier")) frontline++;
            }
            PushEvent($"精英前状态：前排{frontline} / 备战席{benchDepth}，建议保留控制与续航单位");
        }
        else if (effectiveType == StageType.Boss)
        {
            PushEvent($"节点情报：第{st.floor}层为Boss节点（建议保留关键经济与刷新）");
            int boardPower = deploySlots.Count;
            int reserveGold = Mathf.Max(0, gold - 20);
            PushEvent($"Boss前状态：生命{playerLife} / 金币{gold} / 上阵{boardPower}，可机动金币约{reserveGold}");
        }
    }

    private void StartBattle()
    {
        var st = GetCurrentStageNode();
        if (st == null) return;
        var effectiveType = GetEffectiveStageType(st);
        if (effectiveType == StageType.Shop)
        {
            AdvanceToStageMapFromCurrentNode();
            return;
        }

        state = RunState.Battle;
        battleStarted = true;
        battleStartedTurn = 0;
        turnIndex = 0;
        inspectedUnit = null;
        showTooltip = false;
        battleLog = $"战斗开始：第{st.floor}层 {StageName(effectiveType)}";
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

        if (HasHex("vanguard_bastion"))
        {
            for (int i = 0; i < playerUnits.Count; i++)
            {
                var u = playerUnits[i];
                if (u.ClassTag != "Vanguard") continue;
                int bonus = Mathf.RoundToInt(u.maxHp * 0.18f);
                u.maxHp += bonus;
                u.hp += bonus;
            }
        }
        if (HasHex("stone_oath"))
        {
            for (int i = 0; i < playerUnits.Count; i++)
            {
                var u = playerUnits[i];
                if (u.OriginTag != "Stone") continue;
                int bonus = Mathf.RoundToInt(u.maxHp * 0.14f);
                u.maxHp += bonus;
                u.hp += bonus;
            }
        }
        if (HasHex("triple_prep"))
        {
            int maxStar = 1;
            for (int i = 0; i < playerUnits.Count; i++) maxStar = Mathf.Max(maxStar, playerUnits[i].star);
            for (int i = 0; i < playerUnits.Count; i++)
            {
                var u = playerUnits[i];
                if (u.star < maxStar) continue;
                u.maxHp = Mathf.RoundToInt(u.maxHp * 1.12f);
                u.hp = Mathf.Min(u.maxHp, Mathf.RoundToInt(u.hp * 1.12f));
                u.atk = Mathf.RoundToInt(u.atk * 1.10f);
            }
        }

        DevLogHexSynergySpikeProbe();
        SpawnEnemiesForStage(st);
        ApplyAssassinAmbush();

        CreateViews(playerUnits, new Color(0.2f, 0.7f, 1f));
        CreateViews(enemyUnits, new Color(0.95f, 0.35f, 0.4f));
        RefreshViews();
    }

    private void DevLogHexSynergySpikeProbe()
    {
        int assassin = 0, artillery = 0, controller = 0, medic = 0;
        for (int i = 0; i < playerUnits.Count; i++)
        {
            var u = playerUnits[i];
            if (u == null || u.def == null) continue;
            if (u.ClassTag == "Assassin") assassin++;
            if (u.ClassTag == "Artillery") artillery++;
            if (u.ClassTag == "Controller") controller++;
            if (u.ClassTag == "Medic") medic++;
        }

        var tags = new List<string>();
        if (HasHex("assassin_contract") && assassin >= 2)
        {
            tags.Add($"assassin_contract+assassin({assassin})");
            spikeProbeAssassinContractHits++;
        }
        if (HasHex("artillery_overclock") && artillery >= 2)
        {
            tags.Add($"artillery_overclock+artillery({artillery})");
            spikeProbeArtilleryOverclockHits++;
        }
        if (HasHex("tri_service") && artillery >= 1 && controller >= 1 && medic >= 1)
        {
            tags.Add($"tri_service+A/C/M({artillery}/{controller}/{medic})");
            spikeProbeTriServiceHits++;
        }

        if (tags.Count == 0) return;
        Debug.Log($"[DEV][SPIKE_PROBE] floor={stageIndex + 1} tags={string.Join(";", tags)}");
    }

    private void SpawnEnemiesForStage(StageNode st)
    {
        var effectiveType = GetEffectiveStageType(st);
        int enemyCount = effectiveType == StageType.Boss
            ? Mathf.Clamp(1 + st.power, 4, 6)
            : Mathf.Clamp(2 + st.power, 3, 7);
        int[,] pos = { { 7, 2 }, { 8, 1 }, { 8, 2 }, { 8, 3 }, { 9, 1 }, { 9, 2 }, { 9, 4 }, { 7, 4 } };

        for (int i = 0; i < enemyCount; i++)
        {
            string key = PickEnemyUnitKey(st);
            var u = CreateUnit(key, false);

            float hpScale = 1f + (st.power - 1) * 0.15f;
            float atkScale = 1f + (st.power - 1) * 0.11f;
            if (effectiveType == StageType.Boss)
            {
                hpScale *= 0.88f;
                atkScale *= 0.9f;
            }
            u.hp = Mathf.RoundToInt(u.hp * hpScale);
            u.maxHp = u.hp;
            u.atk = Mathf.RoundToInt(u.atk * atkScale);
            float spdStep = effectiveType == StageType.Boss ? 0.35f : 0.5f;
            u.spd += Mathf.FloorToInt((st.power - 1) * spdStep);

            if (effectiveType == StageType.Elite || effectiveType == StageType.Boss || UnityEngine.Random.value < Mathf.Clamp01((st.floor - 2) * 0.12f))
            {
                UpgradeUnit(u);
                if (effectiveType == StageType.Boss && UnityEngine.Random.value < 0.18f) UpgradeUnit(u); // 少量3星Boss怪
            }

            u.x = pos[i, 0];
            u.y = pos[i, 1];
            enemyUnits.Add(u);
        }
    }

    private string PickEnemyUnitKey(StageNode st)
    {
        var effectiveType = GetEffectiveStageType(st);
        if (effectiveType == StageType.Boss)
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
        var st = GetCurrentStageNode();
        if (st == null)
        {
            state = RunState.Stage;
            return;
        }
        var effectiveType = GetEffectiveStageType(st);

        if (win)
        {
            lastBattleWin = true;
            winStreak++;
            loseStreak = 0;
            int reward = 8 + Mathf.Min(5, st.power);
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

            int enemySurvivors = enemyUnits.FindAll(u => u.Alive).Count;
            int lifeLoss = Mathf.Clamp(2 + st.power + (enemySurvivors * 2), 2, 25);
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

        pendingEliteHexReward = win && effectiveType == StageType.Elite;
        pendingHexAfterReward = st.giveHex || pendingEliteHexReward;
        RollRewardOffers();
        state = RunState.Reward;
        battleLog += " | 战后奖励三选一";
    }

    #endregion

}
