using System;
using System.Collections.Generic;
using UnityEngine;

public class RoguelikeFramework : MonoBehaviour
{
    private enum RunState { Stage, Prepare, Battle, Hex, Reward, GameOver }
    private enum StageType { Normal, Elite, Shop, Boss }

    private class StageNode
    {
        public StageType type;
        public int floor;
        public int power;
        public bool giveHex;
    }

    private class UnitDef
    {
        public string key;
        public string name;
        public string family; // 车/马/炮...
        public string classTag; // Vanguard/Rider/Artillery/...
        public string originTag; // Steel/Blaze/Shadow/...
        public int cost;
        public int hp;
        public int atk;
        public int spd;
        public int range;
    }

    private class Unit
    {
        public string id;
        public UnitDef def;
        public int hp;
        public int maxHp;
        public int atk;
        public int spd;
        public int range;
        public int star = 1;
        public int x;
        public int y;
        public bool player;
        public bool usedCharge;

        public int damageDealt;
        public int damageTaken;

        public string Name => def != null ? def.name : "?";
        public string Family => def != null ? def.family : "?";
        public string ClassTag => def != null ? def.classTag : "?";
        public string OriginTag => def != null ? def.originTag : "?";
        public bool Alive => hp > 0;
    }

    private class UnitView
    {
        public Unit unit;
        public GameObject go;
        public GameObject hpBg;
        public GameObject hpFill;
    }

    private class HexDef
    {
        public string id;
        public string name;
        public string rarity;
        public string desc;
    }

    private class RewardDef
    {
        public string id;
        public string name;
        public string desc;
    }

    private Material CreateRuntimeMaterial(Texture2D tex, Color color)
    {
        Shader s = Shader.Find("Universal Render Pipeline/Unlit");
        if (s == null) s = Shader.Find("Unlit/Texture");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Standard");
        var m = new Material(s);
        m.color = color;
        if (tex != null) m.mainTexture = tex;
        return m;
    }

    private RunState state = RunState.Stage;

    private readonly List<StageNode> stages = new();
    private int stageIndex;

    private readonly Dictionary<string, UnitDef> unitDefs = new();
    private readonly List<string> basePool = new();

    private readonly List<Unit> benchUnits = new();
    private readonly List<Unit> deploySlots = new();
    private readonly List<Unit> playerUnits = new();
    private readonly List<Unit> enemyUnits = new();
    private readonly List<UnitView> views = new();

    private readonly List<HexDef> hexPool = new();
    private readonly List<HexDef> selectedHexes = new();
    private readonly List<HexDef> currentHexOffers = new();

    private readonly List<RewardDef> rewardPool = new();
    private readonly List<RewardDef> currentRewardOffers = new();
    private bool pendingHexAfterReward;

    private int gold = 10;
    private int playerLevel = 1;
    private int exp;
    private int playerLife = 30;
    private int winStreak;
    private int loseStreak;
    private int battleStartedTurn;

    private bool battleStarted;
    private float turnTimer;
    private readonly float baseTurnInterval = 0.55f;
    private int turnIndex;
    private int speedLevel = 4;
    private string battleLog = "v0.1 -> v0.2~0.3 进行中";
    private Unit inspectedUnit;
    private bool showBattleStats = true;
    private string selectedSynergyKey = "";
    private bool showTooltip;
    private string tooltipText = "";
    private Vector2 tooltipPos;

    private readonly List<string> shopOffers = new();

    private int draggingDeploy = -1;
    private GameObject dragGhost;
    private bool isDragging;
    private bool draggingFromBench;
    private bool pendingDrag;
    private Vector2 pendingDragStart;

    private Texture2D bgTex;
    private Texture2D tileATex;
    private Texture2D tileBTex;
    private Texture2D dragonIcon;
    private Texture2D horseIcon;
    private Texture2D swordIcon;
    private Texture2D bombIcon;
    private Texture2D shieldIcon;
    private Texture2D uiPanelTex;
    private Texture2D uiPanelDarkTex;
    private Texture2D uiButtonTex;
    private Texture2D uiButtonHoverTex;
    private Texture2D uiButtonPressedTex;

    private Texture2D flatPanelTex;
    private Texture2D flatButtonTex;
    private Texture2D flatButtonHoverTex;
    private Texture2D flatButtonActiveTex;

    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private bool stylesReady;

    private readonly Dictionary<string, Texture2D> hexTextures = new();
    private readonly Dictionary<string, Texture2D> unitTextures = new();

    private const int W = 10;
    private const int H = 6;

    private void Start()
    {
        SetupCamera();
        LoadArt();
        DrawBackground();
        BuildUnitDefs();
        BuildLinearStages();
        BuildHexPool();
        BuildRewardPool();
        LoadGeneratedUnitArt();
        LoadGeneratedHexArt();
        RefreshShop(true);

        // 初始阵容
        benchUnits.Add(CreateUnit("soldier_sword", true));
        benchUnits.Add(CreateUnit("horse_raider", true));
        benchUnits.Add(CreateUnit("cannon_burst", true));

        battleLog = "M1/M2/M3 框架已接入：线性关卡 + 海克斯 + 羁绊 + 经济";
    }

    private void Update()
    {
        HandleMouseDrag();
        HandleUnitInspectClick();
        HandlePrepareQuickActions();

        if (state != RunState.Battle || !battleStarted) return;

        float turnInterval = baseTurnInterval / speedLevel;
        turnTimer += Time.deltaTime;
        if (turnTimer < turnInterval) return;
        turnTimer = 0;

        RunOneTurn();
        RefreshViews();
        CheckBattleEnd();
    }

    private void HandlePrepareQuickActions()
    {
        if (state != RunState.Prepare) return;
        if (!Input.GetMouseButtonDown(1)) return;

        // 右键战场棋子：快速下场到备战席
        var cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            foreach (var v in views)
            {
                if (v.go != hit.collider.gameObject || v.unit == null) continue;
                int idx = deploySlots.FindIndex(u => u.id == v.unit.id);
                if (idx >= 0)
                {
                    ReturnDeployToBench(idx);
                    RedrawPrepareBoard();
                    return;
                }
            }
        }
    }

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
        playerLife = 30;
        winStreak = 0;
        loseStreak = 0;
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

    #region Setup Data

    private void BuildUnitDefs()
    {
        unitDefs.Clear();
        basePool.Clear();

        // 车系变体
        AddDef("chariot_tank", "坦克车", "车", "Vanguard", "Steel", 3, 52, 8, 5, 1);
        AddDef("chariot_sport", "跑车", "车", "Rider", "Neon", 3, 34, 11, 12, 1);
        AddDef("chariot_shock", "震荡车", "车", "Vanguard", "Thunder", 4, 42, 12, 7, 1);

        // 马系变体
        AddDef("horse_raider", "突袭马", "马", "Rider", "Shadow", 2, 30, 11, 11, 1);
        AddDef("horse_banner", "战旗马", "马", "Rider", "Holy", 3, 34, 9, 10, 1);
        AddDef("horse_nightmare", "梦魇马", "马", "Rider", "Night", 4, 32, 13, 10, 1);

        // 炮系变体
        AddDef("cannon_missile", "导弹炮", "炮", "Artillery", "Blaze", 4, 28, 16, 7, 4);
        AddDef("cannon_mortar", "迫击炮", "炮", "Artillery", "Earth", 3, 30, 12, 7, 3);
        AddDef("cannon_burst", "连发炮", "炮", "Artillery", "Steel", 2, 26, 9, 10, 3);

        // 基础补充
        AddDef("general_fire", "火焰君主", "帅", "Leader", "Blaze", 5, 50, 12, 7, 1);
        AddDef("ele_guard", "岩石巨像", "象", "Guardian", "Stone", 3, 48, 8, 5, 1);
        AddDef("guard_assassin", "暗影士", "士", "Assassin", "Shadow", 2, 28, 10, 10, 1);
        AddDef("guard_blade", "夜刃士", "士", "Assassin", "Night", 2, 27, 11, 11, 1);
        AddDef("soldier_sword", "剑士兵", "兵", "Soldier", "Steel", 1, 24, 8, 8, 1);

        foreach (var kv in unitDefs) basePool.Add(kv.Key);
    }

    private void AddDef(string key, string name, string family, string classTag, string originTag, int cost, int hp, int atk, int spd, int range)
    {
        unitDefs[key] = new UnitDef
        {
            key = key,
            name = name,
            family = family,
            classTag = classTag,
            originTag = originTag,
            cost = cost,
            hp = hp,
            atk = atk,
            spd = spd,
            range = range
        };
    }

    private void BuildLinearStages()
    {
        stages.Clear();
        stages.Add(new StageNode { floor = 1, type = StageType.Normal, power = 1, giveHex = false });
        stages.Add(new StageNode { floor = 2, type = StageType.Normal, power = 2, giveHex = true });
        stages.Add(new StageNode { floor = 3, type = StageType.Elite, power = 3, giveHex = false });
        stages.Add(new StageNode { floor = 4, type = StageType.Shop, power = 3, giveHex = true });
        stages.Add(new StageNode { floor = 5, type = StageType.Normal, power = 4, giveHex = false });
        stages.Add(new StageNode { floor = 6, type = StageType.Elite, power = 5, giveHex = true });
        stages.Add(new StageNode { floor = 7, type = StageType.Normal, power = 6, giveHex = false });
        stages.Add(new StageNode { floor = 8, type = StageType.Boss, power = 7, giveHex = true });
    }

    private void BuildHexPool()
    {
        hexPool.Clear();
        AddHex("rich", "金币雨", "蓝", "每回合准备阶段额外 +4 金币");
        AddHex("interest_up", "理财大师", "蓝", "利息上限 +2");
        AddHex("cannon_master", "炮火专精", "金", "炮系伤害 +25%");
        AddHex("rider_charge", "骑兵冲锋", "金", "马系首击额外 +8 伤害");
        AddHex("vanguard_wall", "钢铁壁垒", "蓝", "车系受到伤害 -18%");
        AddHex("team_atk", "全军增幅", "白", "全队攻击 +2");
        AddHex("artillery_range", "超远校准", "蓝", "炮系射程 +1");
        AddHex("board_plus", "超载部署", "金", "上阵人数上限 +1");
        AddHex("fast_train", "快速练兵", "白", "每回合额外 +2 经验");
        AddHex("healing", "战备修复", "白", "每回合准备阶段，上阵棋子回复 20% 最大生命");
    }

    private void AddHex(string id, string name, string rarity, string desc)
    {
        hexPool.Add(new HexDef { id = id, name = name, rarity = rarity, desc = desc });
    }

    private void BuildRewardPool()
    {
        rewardPool.Clear();
        rewardPool.Add(new RewardDef { id = "gold_big", name = "藏宝箱", desc = "立即获得 +10 金币" });
        rewardPool.Add(new RewardDef { id = "heal", name = "战地医疗", desc = "恢复 8 点生命" });
        rewardPool.Add(new RewardDef { id = "exp", name = "战术复盘", desc = "获得 6 点经验" });
        rewardPool.Add(new RewardDef { id = "unit_low", name = "招募新兵", desc = "获得 1 个随机 1-3 费棋子" });
        rewardPool.Add(new RewardDef { id = "reroll_pack", name = "补给券", desc = "免费刷新商店并额外 +2 金币" });
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
            case "heal":
                playerLife = Mathf.Min(30, playerLife + 8);
                battleLog = "奖励：恢复 8 生命";
                break;
            case "exp":
                GainExp(6);
                battleLog = "奖励：+6 经验";
                break;
            case "unit_low":
                if (benchUnits.Count < 8)
                {
                    string key = RollShopKeyByLevel();
                    benchUnits.Add(CreateUnit(key, true));
                    battleLog = $"奖励：获得 {unitDefs[key].name}";
                }
                else
                {
                    gold += 4;
                    battleLog = "备战席满，改为 +4 金币";
                }
                break;
            case "reroll_pack":
                RefreshShop(true);
                gold += 2;
                battleLog = "奖励：免费刷新商店 +2 金币";
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

        StartPreparationForCurrentStage();
    }

    #endregion

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

    private void EndBattle(bool win)
    {
        battleStarted = false;

        if (win)
        {
            winStreak++;
            loseStreak = 0;
            int reward = 8 + Mathf.Min(5, stages[stageIndex].power);
            gold += reward;
            battleLog = $"胜利！+{reward}金币";
        }
        else
        {
            loseStreak++;
            winStreak = 0;
            int reward = 4;
            gold += reward;

            int lifeLoss = Mathf.Clamp(2 + stages[stageIndex].power, 2, 12);
            playerLife -= lifeLoss;
            battleLog = $"失败，保底 +{reward}金币 | 生命 -{lifeLoss}";

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

    #region Units / Shop / Economy

    private Unit CreateUnit(string key, bool player)
    {
        var def = unitDefs[key];
        return new Unit
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8),
            def = def,
            hp = def.hp,
            maxHp = def.hp,
            atk = def.atk,
            spd = def.spd,
            range = def.range,
            player = player,
            x = -1,
            y = -1,
            star = 1
        };
    }

    private Unit CloneUnit(Unit src)
    {
        return new Unit
        {
            id = src.id,
            def = src.def,
            hp = src.hp,
            maxHp = src.maxHp,
            atk = src.atk,
            spd = src.spd,
            range = src.range,
            player = src.player,
            x = src.x,
            y = src.y,
            star = src.star,
            usedCharge = false,
            damageDealt = 0,
            damageTaken = 0
        };
    }

    private void UpgradeUnit(Unit u)
    {
        u.star++;
        u.hp = Mathf.RoundToInt(u.hp * 1.75f);
        u.maxHp = u.hp;
        u.atk = Mathf.RoundToInt(u.atk * 1.55f);
        u.spd += 2;
    }

    private void GainExp(int value)
    {
        exp += value;
        while (playerLevel < 8)
        {
            int need = ExpNeed(playerLevel);
            if (exp < need) break;
            exp -= need;
            playerLevel++;
        }
    }

    private int ExpNeed(int lv)
    {
        return 4 + lv * 2;
    }

    private int GetBoardCap()
    {
        // 体验优化：1级就允许上3个，避免“拖不上去”的误解
        int cap = 3 + Mathf.FloorToInt(playerLevel / 2f);
        if (HasHex("board_plus")) cap += 1;
        return Mathf.Clamp(cap, 3, 9);
    }

    private int GetInterestCap()
    {
        int cap = 5;
        if (HasHex("interest_up")) cap += 2;
        return cap;
    }

    private void RefreshShop(bool freeRefresh = false)
    {
        if (!freeRefresh)
        {
            if (gold < 1) { battleLog = "金币不足，无法刷新"; return; }
            gold -= 1;
        }

        shopOffers.Clear();
        for (int i = 0; i < 5; i++) shopOffers.Add(RollShopKeyByLevel());
    }

    private string RollShopKeyByLevel()
    {
        // 简化权重池（M3）
        int roll = UnityEngine.Random.Range(0, 100);
        int maxCostByLevel = playerLevel <= 2 ? 2 : playerLevel <= 4 ? 3 : playerLevel <= 6 ? 4 : 5;

        var candidates = new List<UnitDef>();
        foreach (var kv in unitDefs)
        {
            if (kv.Value.cost <= maxCostByLevel) candidates.Add(kv.Value);
        }

        // 偏向低费
        int targetCost;
        if (roll < 50) targetCost = Mathf.Min(2, maxCostByLevel);
        else if (roll < 80) targetCost = Mathf.Min(3, maxCostByLevel);
        else if (roll < 95) targetCost = Mathf.Min(4, maxCostByLevel);
        else targetCost = maxCostByLevel;

        var filtered = candidates.FindAll(c => c.cost == targetCost);
        if (filtered.Count == 0) filtered = candidates;
        return filtered[UnityEngine.Random.Range(0, filtered.Count)].key;
    }

    private void BuyOffer(int i)
    {
        if (i < 0 || i >= shopOffers.Count) return;
        string key = shopOffers[i];
        if (!unitDefs.ContainsKey(key)) return;

        var def = unitDefs[key];
        if (gold < def.cost) { battleLog = $"金币不足（需要{def.cost}）"; return; }
        if (benchUnits.Count >= 8) { battleLog = "备战席已满"; return; }

        gold -= def.cost;
        benchUnits.Add(CreateUnit(key, true));
        shopOffers.RemoveAt(i);
        battleLog = $"购买 {def.name} -{def.cost}金";
        AutoMergeAll();
        RedrawPrepareBoard();
    }

    private void AutoMergeAll()
    {
        bool merged;
        do
        {
            merged = false;
            var all = new List<Unit>();
            all.AddRange(benchUnits);
            all.AddRange(deploySlots);

            // 先尝试 2星->3星，再尝试 1星->2星
            int[] mergeStars = { 2, 1 };
            foreach (int fromStar in mergeStars)
            {
                var countByKey = new Dictionary<string, List<Unit>>();
                foreach (var u in all)
                {
                    if (u.star != fromStar) continue;
                    if (!countByKey.ContainsKey(u.def.key)) countByKey[u.def.key] = new List<Unit>();
                    countByKey[u.def.key].Add(u);
                }

                foreach (var kv in countByKey)
                {
                    if (kv.Value.Count < 3) continue;

                    int removed = 0;
                    for (int i = benchUnits.Count - 1; i >= 0 && removed < 3; i--)
                    {
                        if (benchUnits[i].def.key == kv.Key && benchUnits[i].star == fromStar)
                        {
                            benchUnits.RemoveAt(i);
                            removed++;
                        }
                    }
                    for (int i = deploySlots.Count - 1; i >= 0 && removed < 3; i--)
                    {
                        if (deploySlots[i].def.key == kv.Key && deploySlots[i].star == fromStar)
                        {
                            deploySlots.RemoveAt(i);
                            removed++;
                        }
                    }

                    var up = CreateUnit(kv.Key, true);
                    for (int s = 1; s <= fromStar; s++) UpgradeUnit(up);
                    up.star = Mathf.Clamp(fromStar + 1, 1, 3);

                    benchUnits.Add(up);
                    battleLog = fromStar == 1
                        ? $"合成成功：{up.Name} 升为2星"
                        : $"超级合成：{up.Name} 升为3星";

                    merged = true;
                    break;
                }

                if (merged) break;
            }
        } while (merged);
    }

    private void AutoDeployFallback()
    {
        while (benchUnits.Count > 0 && deploySlots.Count < GetBoardCap())
        {
            var u = benchUnits[0];
            benchUnits.RemoveAt(0);
            u.x = deploySlots.Count % 3;
            u.y = 1 + deploySlots.Count / 3;
            deploySlots.Add(u);
        }
    }

    private bool ReturnDeployToBench(int deployIdx)
    {
        if (deployIdx < 0 || deployIdx >= deploySlots.Count) return false;
        if (benchUnits.Count >= 8)
        {
            battleLog = "备战席已满，无法下场";
            return false;
        }

        var u = deploySlots[deployIdx];
        u.x = -1;
        u.y = -1;
        benchUnits.Add(u);
        deploySlots.RemoveAt(deployIdx);
        battleLog = $"{u.Name} 已下场";
        return true;
    }

    private bool SellUnit(Unit u)
    {
        if (u == null || u.def == null) return false;
        int benchIdx = benchUnits.FindIndex(x => x.id == u.id);
        int deployIdx = deploySlots.FindIndex(x => x.id == u.id);
        if (benchIdx < 0 && deployIdx < 0) return false;

        int sellGold = Mathf.Max(1, u.def.cost);
        gold += sellGold;

        if (benchIdx >= 0) benchUnits.RemoveAt(benchIdx);
        if (deployIdx >= 0) deploySlots.RemoveAt(deployIdx);

        if (inspectedUnit != null && inspectedUnit.id == u.id) inspectedUnit = null;
        battleLog = $"出售 {u.Name} +{sellGold}金币";
        return true;
    }

    #endregion

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

        StartPreparationForCurrentStage();
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

    private string GetSynergyEffectDesc(string classTag, int count)
    {
        return classTag switch
        {
            "Vanguard" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位减伤 +10% / +22%（海克斯可再叠加）",
            "Rider" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位速度 +2 / +5，首击突进更强",
            "Artillery" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位伤害 +12% / +22%，4层额外射程+1",
            "Assassin" => $"{GetClassCn(classTag)}：2/4生效，当前{count}。效果：本羁绊单位暴击率 +20% / +45%，暴击伤害 +35%",
            _ => $"{GetClassCn(classTag)}：当前{count}"
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

    private string GetInspectSynergyText(Unit u, List<Unit> team)
    {
        int c = CountClass(team, u.ClassTag);
        return $"可激活羁绊：{GetClassCn(u.ClassTag)}（当前上阵{c}）\n" +
               $"效果：{GetSynergyEffectDesc(u.ClassTag, c)}\n" +
               $"该羁绊棋子：{GetUnitsOfClassText(u.ClassTag)}";
    }

    private string GetSynergySummary(List<Unit> team)
    {
        int v = CountClass(team, "Vanguard");
        int r = CountClass(team, "Rider");
        int a = CountClass(team, "Artillery");
        int ass = CountClass(team, "Assassin");

        string summary = "";
        if (v >= 2) summary += $"钢铁先锋({v}) ";
        if (r >= 2) summary += $"机动骑兵({r}) ";
        if (a >= 2) summary += $"火力炮阵({a}) ";
        if (ass >= 2) summary += $"暗影刺客({ass}) ";
        if (string.IsNullOrEmpty(summary)) summary = "暂无激活羁绊";
        return summary;
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
        }

        if (HasHex("team_atk")) m += 0.08f;

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
        }
        return b;
    }

    private int GetRangeBonus(Unit u)
    {
        int b = 0;
        var team = u.player ? playerUnits : enemyUnits;
        int art = CountClass(team, "Artillery");
        if (u.ClassTag == "Artillery" && art >= 4) b += 1;
        if (u.ClassTag == "Artillery" && HasHex("artillery_range")) b += 1;
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
        return Mathf.Clamp01(r);
    }

    #endregion

    #region Battle

    private void RunOneTurn()
    {
        var order = new List<Unit>();
        order.AddRange(playerUnits.FindAll(u => u.Alive));
        order.AddRange(enemyUnits.FindAll(u => u.Alive));
        order.Sort((a, b) => (b.spd + GetSpeedBonus(b)).CompareTo(a.spd + GetSpeedBonus(a)));
        if (order.Count == 0) return;

        var actor = order[turnIndex % order.Count];
        turnIndex++;
        if (!actor.Alive) return;

        var targets = actor.player ? enemyUnits : playerUnits;
        var target = NearestAlive(actor, targets);
        if (target == null) return;

        int actorRange = actor.range + GetRangeBonus(actor);
        int dist = Mathf.Abs(actor.x - target.x) + Mathf.Abs(actor.y - target.y);
        int dmg = Mathf.RoundToInt(actor.atk * GetDamageMultiplier(actor) * GetCritMultiplier(actor));

        // 特色能力
        if (actor.Name.Contains("突袭马") && !actor.usedCharge)
        {
            dmg += 6;
            if (HasHex("rider_charge")) dmg += 8;
            actor.usedCharge = true;
        }
        if (actor.Name.Contains("跑车") && !actor.usedCharge)
        {
            dmg += 5;
            actor.usedCharge = true;
        }

        if (dist <= actorRange)
        {
            int real = ApplyDamageWithTraits(actor, target, dmg);
            if (actorRange > 1)
            {
                // 不同炮系弹道差异化
                if (actor.def.key == "cannon_missile") SpawnProjectile(actor, target, new Color(1f, 0.35f, 0.2f), 0.24f, 0.12f);
                else if (actor.def.key == "cannon_mortar") SpawnProjectile(actor, target, new Color(1f, 0.78f, 0.28f), 0.20f, 0.2f);
                else if (actor.def.key == "cannon_burst") SpawnProjectile(actor, target, new Color(1f, 0.25f, 0.25f), 0.14f, 0.08f);
                else SpawnProjectile(actor, target, new Color(1f, 0.75f, 0.2f));

                battleLog = $"{actor.Name} 攻击 {target.Name} -{real}";
            }
            else
            {
                if (actor.def.key == "chariot_tank") SpawnHitFlash(target, new Color(0.7f, 0.9f, 1f), 0.75f);
                else if (actor.def.key == "chariot_shock") SpawnHitFlash(target, new Color(0.7f, 0.55f, 1f), 0.9f);
                else SpawnHitFlash(target, new Color(1f, 0.2f, 0.2f), 0.6f);
                battleLog = $"{actor.Name} 近战 {target.Name} -{real}";
            }

            if (actor.Name.Contains("迫击炮"))
            {
                var splashTargets = actor.player ? enemyUnits : playerUnits;
                foreach (var t in splashTargets)
                {
                    if (!t.Alive || t == target) continue;
                    int ad = Mathf.Abs(t.x - target.x) + Mathf.Abs(t.y - target.y);
                    if (ad <= 1)
                    {
                        ApplyDamageWithTraits(actor, t, Mathf.Max(1, Mathf.RoundToInt(dmg * 0.45f)));
                        SpawnHitFlash(t, new Color(1f, 0.5f, 0.2f), 0.75f);
                    }
                }
            }

            if (actor.Name.Contains("连发炮") && target.Alive)
            {
                int real2 = ApplyDamageWithTraits(actor, target, Mathf.Max(1, Mathf.RoundToInt(dmg * 0.5f)));
                battleLog += $" | 连发 -{real2}";
            }
        }
        else
        {
            StepToward(actor, target);
            battleLog = $"{actor.Name} 向 {target.Name} 移动";
        }

        battleStartedTurn++;
    }

    private Unit NearestAlive(Unit from, List<Unit> list)
    {
        Unit best = null;
        int bestD = 999;
        foreach (var u in list)
        {
            if (!u.Alive) continue;
            int d = Mathf.Abs(from.x - u.x) + Mathf.Abs(from.y - u.y);
            if (d < bestD)
            {
                bestD = d;
                best = u;
            }
        }
        return best;
    }

    private int ApplyDamageWithTraits(Unit from, Unit to, int raw)
    {
        int dmg = raw;

        float reduction = GetDamageReduction(to);
        dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (1f - reduction)));

        // 旧兵种残留风味
        if (to.Family == "士" && UnityEngine.Random.value < 0.15f) return 0;

        to.hp -= dmg;
        from.damageDealt += dmg;
        to.damageTaken += dmg;
        return dmg;
    }

    private void StepToward(Unit a, Unit b)
    {
        int step = a.ClassTag == "Rider" ? 2 : 1;
        for (int i = 0; i < step; i++)
        {
            int nx = a.x;
            int ny = a.y;

            if (Mathf.Abs(a.x - b.x) >= Mathf.Abs(a.y - b.y)) nx += b.x > a.x ? 1 : -1;
            else ny += b.y > a.y ? 1 : -1;

            nx = Mathf.Clamp(nx, 0, W - 1);
            ny = Mathf.Clamp(ny, 0, H - 1);

            if (!Occupied(nx, ny))
            {
                a.x = nx;
                a.y = ny;
            }
            else break;
        }
    }

    private bool Occupied(int x, int y)
    {
        foreach (var u in playerUnits) if (u.Alive && u.x == x && u.y == y) return true;
        foreach (var u in enemyUnits) if (u.Alive && u.x == x && u.y == y) return true;
        return false;
    }

    private void CheckBattleEnd()
    {
        bool pDead = playerUnits.TrueForAll(u => !u.Alive);
        bool eDead = enemyUnits.TrueForAll(u => !u.Alive);

        if (!pDead && !eDead) return;
        EndBattle(!pDead && eDead);
    }

    #endregion

    #region Visuals

    private void LoadArt()
    {
        bgTex = Resources.Load<Texture2D>("Art/board_bg");
        tileATex = Resources.Load<Texture2D>("Art/tile_a");
        tileBTex = Resources.Load<Texture2D>("Art/tile_b");
        dragonIcon = Resources.Load<Texture2D>("Art/icon_dragon");
        horseIcon = Resources.Load<Texture2D>("Art/icon_horse");
        swordIcon = Resources.Load<Texture2D>("Art/icon_sword");
        bombIcon = Resources.Load<Texture2D>("Art/icon_bomb");
        shieldIcon = Resources.Load<Texture2D>("Art/icon_shield");
        uiPanelTex = Resources.Load<Texture2D>("Art/UI/ui_panel");
        uiPanelDarkTex = Resources.Load<Texture2D>("Art/UI/ui_panel_dark");
        uiButtonTex = Resources.Load<Texture2D>("Art/UI/ui_button");
        uiButtonHoverTex = Resources.Load<Texture2D>("Art/UI/ui_button_hover");
        uiButtonPressedTex = Resources.Load<Texture2D>("Art/UI/ui_button_pressed");

        LoadGeneratedHexArt();
        LoadGeneratedUnitArt();
    }

    private void LoadGeneratedHexArt()
    {
        hexTextures.Clear();
        foreach (var h in hexPool)
        {
            var tex = Resources.Load<Texture2D>($"Art/Hexes/hex_{h.id}");
            if (tex != null) hexTextures[h.id] = tex;
        }
    }

    private void LoadGeneratedUnitArt()
    {
        unitTextures.Clear();
        foreach (var kv in unitDefs)
        {
            var tex = Resources.Load<Texture2D>($"Art/Units/unit_{kv.Key}");
            if (tex != null) unitTextures[kv.Key] = tex;
        }
    }

    private void DrawBackground()
    {
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "BG";
        bg.transform.position = new Vector3(0, 0, 1.2f);
        bg.transform.localScale = new Vector3(16f, 10f, 1f);
        var r = bg.GetComponent<Renderer>();
        r.material = CreateRuntimeMaterial(bgTex, new Color(0.12f, 0.1f, 0.14f));
    }

    private void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var c = new GameObject("Main Camera");
            cam = c.AddComponent<Camera>();
            c.tag = "MainCamera";
        }
        cam.orthographic = true;
        cam.orthographicSize = 6;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.08f, 0.07f, 0.11f);
    }

    private void DrawBoard()
    {
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Cell";
                q.transform.position = GridToWorld(x, y) + new Vector3(0, 0, 0.3f);
                q.transform.localScale = new Vector3(0.95f, 0.95f, 1);
                var r = q.GetComponent<Renderer>();
                bool dark = (x + y) % 2 == 0;
                var c = dark ? new Color(0.18f, 0.18f, 0.24f) : new Color(0.24f, 0.24f, 0.3f);
                var t = dark ? tileATex : tileBTex;
                r.material = CreateRuntimeMaterial(t, c);
            }
        }
    }

    private void CreateViews(List<Unit> units, Color c)
    {
        foreach (var u in units)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Unit";
            go.transform.position = GridToWorld(u.x, u.y);
            go.transform.localScale = new Vector3(0.8f, 0.8f, 1);
            var r = go.GetComponent<Renderer>();
            r.material = CreateRuntimeMaterial(PickIcon(u), PickVariantTint(u));

            var hpBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hpBg.name = "HPBarBg";
            hpBg.transform.localScale = new Vector3(0.8f, 0.1f, 1f);
            hpBg.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, new Color(0f, 0f, 0f, 0.7f));

            var hpFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hpFill.name = "HPBarFill";
            hpFill.transform.localScale = new Vector3(0.78f, 0.07f, 1f);
            hpFill.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, u.player ? new Color(0.2f, 0.95f, 0.35f) : new Color(0.95f, 0.2f, 0.2f));

            views.Add(new UnitView { unit = u, go = go, hpBg = hpBg, hpFill = hpFill });
        }
    }

    private void RefreshViews()
    {
        foreach (var v in views)
        {
            if (v.unit.Alive)
            {
                v.go.SetActive(true);
                if (v.hpBg) v.hpBg.SetActive(true);
                if (v.hpFill) v.hpFill.SetActive(true);

                v.go.transform.position = GridToWorld(v.unit.x, v.unit.y);
                var barCenter = v.go.transform.position + new Vector3(0f, 0.56f, -0.05f);
                if (v.hpBg) v.hpBg.transform.position = barCenter;

                float hpRatio = v.unit.maxHp > 0 ? Mathf.Clamp01((float)v.unit.hp / v.unit.maxHp) : 0f;
                float fullWidth = 0.78f;
                if (v.hpFill)
                {
                    v.hpFill.transform.localScale = new Vector3(fullWidth * hpRatio, 0.07f, 1f);
                    float left = barCenter.x - fullWidth / 2f;
                    float fillX = left + (fullWidth * hpRatio) / 2f;
                    v.hpFill.transform.position = new Vector3(fillX, barCenter.y, -0.06f);
                }
            }
            else
            {
                v.go.SetActive(false);
                if (v.hpBg) v.hpBg.SetActive(false);
                if (v.hpFill) v.hpFill.SetActive(false);
            }
        }
    }

    private void ClearViews()
    {
        foreach (var v in views)
        {
            if (v.go) Destroy(v.go);
            if (v.hpBg) Destroy(v.hpBg);
            if (v.hpFill) Destroy(v.hpFill);
        }
        views.Clear();

        foreach (var c in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (c.name == "Cell") Destroy(c);
        }
    }

    private Texture2D PickIcon(Unit u)
    {
        if (u?.def != null && unitTextures.TryGetValue(u.def.key, out var tex) && tex != null)
        {
            return tex;
        }

        if (u.Family == "帅") return dragonIcon ?? shieldIcon;
        if (u.Family == "马") return horseIcon;
        if (u.Family == "炮") return bombIcon;
        if (u.Family == "车" || u.Family == "兵") return swordIcon;
        return shieldIcon;
    }

    private Color PickVariantTint(Unit u)
    {
        // 同家族不同变体做视觉区分，并叠加敌我主色调
        Color baseTint = u.def.key switch
        {
            "cannon_missile" => new Color(1f, 0.55f, 0.2f),
            "cannon_mortar" => new Color(0.95f, 0.75f, 0.35f),
            "cannon_burst" => new Color(1f, 0.35f, 0.3f),
            "chariot_tank" => new Color(0.65f, 0.85f, 1f),
            "chariot_sport" => new Color(0.35f, 1f, 0.9f),
            "chariot_shock" => new Color(0.8f, 0.7f, 1f),
            "horse_raider" => new Color(0.6f, 0.95f, 0.6f),
            "horse_banner" => new Color(0.6f, 0.85f, 1f),
            "horse_nightmare" => new Color(0.8f, 0.55f, 1f),
            _ => new Color(0.88f, 0.88f, 0.92f)
        };

        // 保留原始素材主体，只做主色系偏移
        Color allyPalette = new Color(0.62f, 0.86f, 1f);   // 冷青蓝
        Color enemyPalette = new Color(1f, 0.58f, 0.5f);   // 暖橙红
        Color teamTint = u.player ? allyPalette : enemyPalette;

        return Color.Lerp(baseTint, teamTint, 0.38f);
    }

    private Color GetUnitChipColor(Unit u)
    {
        if (u == null || u.def == null) return new Color(0.26f, 0.34f, 0.5f, 0.95f);
        return GetUnitChipColorByClass(u.ClassTag);
    }

    private Color GetUnitChipColorByClass(string classTag)
    {
        return classTag switch
        {
            "Vanguard" => new Color(0.3f, 0.58f, 0.86f, 0.95f),
            "Rider" => new Color(0.27f, 0.73f, 0.58f, 0.95f),
            "Artillery" => new Color(0.83f, 0.46f, 0.26f, 0.95f),
            "Assassin" => new Color(0.56f, 0.32f, 0.86f, 0.95f),
            _ => new Color(0.36f, 0.44f, 0.6f, 0.95f)
        };
    }

    private void SpawnProjectile(Unit from, Unit to, Color color, float scale = 0.18f, float duration = 0.14f)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "FX_Projectile";
        p.transform.localScale = Vector3.one * scale;
        p.transform.position = GridToWorld(from.x, from.y) + new Vector3(0, 0, -0.2f);
        p.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, color);
        StartCoroutine(MoveFx(p, GridToWorld(to.x, to.y) + new Vector3(0, 0, -0.2f), duration));
    }

    private void SpawnHitFlash(Unit target, Color color, float scale = 0.6f)
    {
        var fx = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fx.name = "FX_Hit";
        fx.transform.position = GridToWorld(target.x, target.y) + new Vector3(0, 0, -0.25f);
        fx.transform.localScale = Vector3.one * scale;
        fx.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, color);
        StartCoroutine(DestroyFx(fx, 0.1f));
    }

    private System.Collections.IEnumerator MoveFx(GameObject go, Vector3 end, float t)
    {
        Vector3 start = go.transform.position;
        float e = 0f;
        while (e < t)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / t);
            go.transform.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        if (go) Destroy(go);
    }

    private System.Collections.IEnumerator DestroyFx(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) Destroy(go);
    }

    private Vector3 GridToWorld(int x, int y) => new Vector3(-4.5f + x, -2.5f + y, 0);

    private Rect GetBoardCellGuiRect(int x, int y)
    {
        var cam = Camera.main;
        if (cam == null) return new Rect(-9999, -9999, 0, 0);

        Vector3 c = cam.WorldToScreenPoint(GridToWorld(x, y));
        Vector3 rx = cam.WorldToScreenPoint(GridToWorld(x, y) + new Vector3(0.48f, 0f, 0f));
        Vector3 uy = cam.WorldToScreenPoint(GridToWorld(x, y) + new Vector3(0f, 0.48f, 0f));
        float w = Mathf.Abs(rx.x - c.x) * 2f;
        float h = Mathf.Abs(uy.y - c.y) * 2f;
        return new Rect(c.x - w * 0.5f, (Screen.height - c.y) - h * 0.5f, w, h);
    }

    private void RedrawPrepareBoard()
    {
        if (state != RunState.Prepare) return;
        ClearViews();
        DrawBoard();
        var preview = deploySlots.FindAll(u => u.x >= 0 && u.x < 5 && u.y >= 0 && u.y < H);
        CreateViews(preview, new Color(0.2f, 0.7f, 1f));
        RefreshViews();
    }

    #endregion

    #region Input

    private void HandleUnitInspectClick()
    {
        if ((state != RunState.Battle && state != RunState.Prepare) || !Input.GetMouseButtonDown(0)) return;
        if (isDragging) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // 点击空白处关闭悬浮窗
            showTooltip = false;
            return;
        }

        foreach (var v in views)
        {
            if (v.go == hit.collider.gameObject)
            {
                inspectedUnit = v.unit;
                ShowTooltip(BuildUnitTooltip(v.unit), v.unit);
                battleLog = $"查看 {v.unit.Name}{v.unit.star}★";
                return;
            }
        }

        // 点到非棋子对象也关闭
        showTooltip = false;
    }

    private void HandleMouseDrag()
    {
        if (state != RunState.Prepare) return;

        int deployCols = 5;
        int deployRows = H;

        float panelX = 16f;
        float panelY = Screen.height - 170f;
        float benchX = panelX + 16f;
        float benchY = panelY + 88f;
        float benchSlotW = 90f;
        float benchBtnW = 84f;
        int benchCols = 8;

        int FindDeployAt(int x, int y)
        {
            for (int i = 0; i < deploySlots.Count; i++) if (deploySlots[i].x == x && deploySlots[i].y == y) return i;
            return -1;
        }

        Vector2 GetGridAtMouse()
        {
            float mx = Input.mousePosition.x;
            float myGui = Screen.height - Input.mousePosition.y;

            for (int r = 0; r < deployRows; r++)
            {
                for (int c = 0; c < deployCols; c++)
                {
                    var cell = GetBoardCellGuiRect(c, r);
                    if (mx >= cell.x && mx <= cell.xMax && myGui >= cell.y && myGui <= cell.yMax) return new Vector2(c, r);
                }
            }

            for (int i = 0; i < benchCols; i++)
            {
                float bx = benchX + i * benchSlotW;
                if (mx >= bx && mx <= bx + benchBtnW && myGui >= benchY && myGui <= benchY + 45f) return new Vector2(-1, i);
            }
            return new Vector2(-2, -2);
        }

        if (!isDragging)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 clickGrid = GetGridAtMouse();
                pendingDrag = false;
                draggingDeploy = -1;
                draggingFromBench = false;

                if (clickGrid.x >= 0 && clickGrid.x < deployCols && clickGrid.y >= 0 && clickGrid.y < deployRows)
                {
                    int deployIdx = FindDeployAt((int)clickGrid.x, (int)clickGrid.y);
                    if (deployIdx >= 0)
                    {
                        draggingDeploy = deployIdx;
                        draggingFromBench = false;
                        pendingDrag = true;
                        pendingDragStart = Input.mousePosition;
                    }
                }
                else if (clickGrid.x == -1)
                {
                    int benchIdx = (int)clickGrid.y;
                    if (benchIdx >= 0 && benchIdx < benchUnits.Count)
                    {
                        draggingDeploy = benchIdx;
                        draggingFromBench = true;
                        pendingDrag = true;
                        pendingDragStart = Input.mousePosition;
                    }
                }
                return;
            }

            if (pendingDrag && Input.GetMouseButton(0))
            {
                if (Vector2.Distance((Vector2)Input.mousePosition, pendingDragStart) > 8f)
                {
                    isDragging = true;
                    pendingDrag = false;
                    if (draggingFromBench && draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
                        battleLog = $"拖拽中：{benchUnits[draggingDeploy].Name}";
                    else if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
                        battleLog = $"拖拽中：{deploySlots[draggingDeploy].Name}";
                }
                return;
            }

            if (pendingDrag && Input.GetMouseButtonUp(0))
            {
                pendingDrag = false;
                draggingDeploy = -1;
                draggingFromBench = false;
            }
            return;
        }

        // 标准拖拽：按下开始，松开落子
        if (!Input.GetMouseButtonUp(0)) return;

        Vector2 releaseGrid = GetGridAtMouse();
        if (releaseGrid.x >= 0 && releaseGrid.x < deployCols && releaseGrid.y >= 0 && releaseGrid.y < deployRows)
        {
            int tx = (int)releaseGrid.x;
            int ty = (int)releaseGrid.y;
            int targetDeployIdx = FindDeployAt(tx, ty);

            if (draggingFromBench)
            {
                if (draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
                {
                    if (targetDeployIdx >= 0) battleLog = "目标格已有棋子";
                    else if (deploySlots.Count >= GetBoardCap()) battleLog = $"上阵已满（上限{GetBoardCap()}）";
                    else
                    {
                        var moved = benchUnits[draggingDeploy];
                        moved.x = tx;
                        moved.y = ty;
                        deploySlots.Add(moved);
                        benchUnits.RemoveAt(draggingDeploy);
                        AutoMergeAll();
                        RedrawPrepareBoard();
                    }
                }
            }
            else if (draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                if (targetDeployIdx >= 0 && targetDeployIdx != draggingDeploy)
                {
                    int ox = deploySlots[draggingDeploy].x;
                    int oy = deploySlots[draggingDeploy].y;
                    deploySlots[targetDeployIdx].x = ox;
                    deploySlots[targetDeployIdx].y = oy;
                }
                deploySlots[draggingDeploy].x = tx;
                deploySlots[draggingDeploy].y = ty;
                RedrawPrepareBoard();
            }
        }
        else if (releaseGrid.x == -1)
        {
            if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                int benchIdx = (int)releaseGrid.y;
                if (benchIdx >= benchUnits.Count)
                {
                    var u = deploySlots[draggingDeploy];
                    u.x = -1;
                    u.y = -1;
                    benchUnits.Add(u);
                    deploySlots.RemoveAt(draggingDeploy);
                    AutoMergeAll();
                    RedrawPrepareBoard();
                }
                else battleLog = "该备战席格子已有棋子";
            }
        }

        isDragging = false;
        pendingDrag = false;
        draggingDeploy = -1;
        draggingFromBench = false;
    }

    #endregion

    #region GUI

    private string BuildUnitTooltip(Unit u)
    {
        var team = u.player ? (state == RunState.Battle ? playerUnits : deploySlots) : enemyUnits;
        return
            $"【{u.Name}{u.star}★】{(u.player ? "我方" : "敌方")}\n" +
            $"生命 {Mathf.Max(0, u.hp)}/{u.maxHp}  攻击 {u.atk}  速度 {u.spd}  射程 {u.range}\n" +
            $"职业 {GetClassCn(u.ClassTag)} / 阵营 {u.OriginTag}\n" +
            $"{GetInspectSynergyText(u, team)}\n" +
            $"本场输出 {u.damageDealt}  本场承伤 {u.damageTaken}";
    }

    private void ShowTooltip(string text, Unit anchor = null)
    {
        tooltipText = text;

        if (anchor != null && Camera.main != null)
        {
            Vector3 wp = GridToWorld(anchor.x, anchor.y) + new Vector3(0.7f, 0.55f, 0f);
            Vector3 sp = Camera.main.WorldToScreenPoint(wp);
            tooltipPos = new Vector2(sp.x, Screen.height - sp.y);
        }
        else
        {
            tooltipPos = new Vector2(Input.mousePosition.x + 16f, Screen.height - Input.mousePosition.y + 16f);
        }

        showTooltip = true;
    }

    private void DrawFloatingTooltip()
    {
        if (!showTooltip || string.IsNullOrEmpty(tooltipText)) return;

        float w = 460f;
        float h = 220f;
        float x = Mathf.Clamp(tooltipPos.x, 10f, Screen.width - w - 10f);
        float y = Mathf.Clamp(tooltipPos.y, 10f, Screen.height - h - 10f);

        // 阴影
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(x + 4, y + 4, w, h), Texture2D.whiteTexture);

        // 外边框（偏金色）
        GUI.color = new Color(0.78f, 0.66f, 0.35f, 0.98f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // 主体底色
        GUI.color = new Color(0.07f, 0.1f, 0.15f, 0.98f);
        GUI.DrawTexture(new Rect(x + 2, y + 2, w - 4, h - 4), Texture2D.whiteTexture);

        // 标题条
        GUI.color = new Color(0.12f, 0.18f, 0.28f, 0.98f);
        GUI.DrawTexture(new Rect(x + 2, y + 2, w - 4, 30), Texture2D.whiteTexture);

        // 分割线
        GUI.color = new Color(0.78f, 0.66f, 0.35f, 0.8f);
        GUI.DrawTexture(new Rect(x + 12, y + 36, w - 24, 1), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(x + 10, y + 8, w - 20, 20), "战场情报 / 羁绊信息");
        GUI.Label(new Rect(x + 12, y + 42, w - 24, h - 50), tooltipText);
    }

    private void DrawBattleStats(float x, float y, float w, float h)
    {
        GUI.Box(new Rect(x, y, w, h), "战斗统计");

        var all = new List<Unit>();
        all.AddRange(playerUnits);
        all.AddRange(enemyUnits);

        int maxVal = 1;
        foreach (var u in all)
        {
            if (u.damageDealt > maxVal) maxVal = u.damageDealt;
            if (u.damageTaken > maxVal) maxVal = u.damageTaken;
        }

        GUI.Label(new Rect(x + 10, y + 20, w - 20, 20), "蓝=输出  橙=承伤（分队面板）");

        float panelGap = 10f;
        float panelW = (w - 30f) * 0.5f;
        float panelH = h - 46f;
        float rowH = 21f;
        float gap = 4f;
        float barMax = panelW - 165f;

        void DrawTeam(string title, List<Unit> team, float px, float py)
        {
            GUI.Box(new Rect(px, py, panelW, panelH), title);
            var sorted = new List<Unit>(team);
            sorted.Sort((a, b) => b.damageDealt.CompareTo(a.damageDealt));

            float ry = py + 24f;
            int maxRows = Mathf.FloorToInt((panelH - 30f) / (rowH + gap));
            for (int i = 0; i < sorted.Count && i < maxRows; i++)
            {
                var u = sorted[i];
                GUI.Label(new Rect(px + 8, ry, 70, rowH), $"{u.Name}");
                float dealtW = barMax * (u.damageDealt / (float)maxVal);
                float takenW = barMax * (u.damageTaken / (float)maxVal);

                Color old = GUI.color;
                GUI.color = new Color(0.25f, 0.72f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(px + 80, ry + 1, dealtW, rowH - 3), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.62f, 0.2f, 0.95f);
                GUI.DrawTexture(new Rect(px + 80, ry + rowH * 0.56f, takenW, rowH * 0.38f), Texture2D.whiteTexture);
                GUI.color = old;

                GUI.Label(new Rect(px + 84 + barMax, ry, 78, rowH), $"{u.damageDealt}/{u.damageTaken}");
                ry += rowH + gap;
            }
        }

        float py = y + 42f;
        DrawTeam("我方阵容", playerUnits, x + 10f, py);
        DrawTeam("敌方阵容", enemyUnits, x + 20f + panelW + panelGap, py);
    }

    private string StageName(StageType t)
    {
        return t switch
        {
            StageType.Normal => "普通",
            StageType.Elite => "精英",
            StageType.Shop => "商店强化",
            StageType.Boss => "Boss",
            _ => "?"
        };
    }

    private void DrawSynergyClickPanel(float x, float y, float w, float h, List<Unit> team)
    {
        if (team == null || team.Count == 0) return;

        GUI.Box(new Rect(x, y, w, h), "羁绊面板（点击查看效果/棋子池）");

        // 仿金铲铲：只显示当前场上已有棋子对应的羁绊
        var classes = new List<string>();
        foreach (var u in team)
        {
            if (string.IsNullOrEmpty(u.ClassTag)) continue;
            if (!classes.Contains(u.ClassTag)) classes.Add(u.ClassTag);
        }
        if (classes.Count == 0) return;

        float bx = x + 10;
        Vector2 mp = Event.current.mousePosition;

        for (int i = 0; i < classes.Count; i++)
        {
            string cls = classes[i];
            int c = CountClass(team, cls);
            bool active2 = c >= 2;
            bool active4 = c >= 4;

            Rect card = new Rect(bx + i * 160, y + 24, 150, 74);

            Color old = GUI.color;
            Color baseColor;
            if (active4) baseColor = new Color(1f, 0.82f, 0.32f, 0.98f);      // 金
            else if (active2) baseColor = new Color(0.35f, 0.75f, 1f, 0.98f); // 蓝
            else baseColor = new Color(0.36f, 0.36f, 0.36f, 0.92f);           // 灰

            bool hover = card.Contains(mp);
            Color show = hover ? new Color(Mathf.Min(1f, baseColor.r + 0.12f), Mathf.Min(1f, baseColor.g + 0.12f), Mathf.Min(1f, baseColor.b + 0.12f), 1f) : baseColor;

            // 外发光/边框（激活时更明显）
            if (active2)
            {
                GUI.color = active4 ? new Color(1f, 0.9f, 0.45f, 0.45f) : new Color(0.45f, 0.85f, 1f, 0.4f);
                GUI.DrawTexture(new Rect(card.x - 3, card.y - 3, card.width + 6, card.height + 6), Texture2D.whiteTexture);
            }

            GUI.color = show;
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(0.08f, 0.11f, 0.16f, 0.96f);
            GUI.DrawTexture(new Rect(card.x + 2, card.y + 2, card.width - 4, card.height - 4), Texture2D.whiteTexture);
            GUI.color = old;

            string status = active4 ? "★★ 4阶激活" : active2 ? "★ 2阶激活" : "未激活";
            GUI.Label(new Rect(card.x + 8, card.y + 6, card.width - 16, 18), $"{GetClassCn(cls)}");
            GUI.Label(new Rect(card.x + 8, card.y + 24, card.width - 16, 18), $"数量: {c}  (2/4)");
            GUI.Label(new Rect(card.x + 8, card.y + 42, card.width - 16, 18), $"状态: {status}");

            if (active2)
            {
                GUI.Label(new Rect(card.x + card.width - 52, card.y + 4, 44, 18), active4 ? "MAX" : "ON");
            }

            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                selectedSynergyKey = cls;
                string info = GetSynergyEffectDesc(selectedSynergyKey, c) + "\n" +
                              $"包含棋子：{GetUnitsOfClassText(selectedSynergyKey)}";
                ShowTooltip(info);
            }
        }

        GUI.Label(new Rect(x + 12, y + 102, w - 24, 20), "颜色说明：灰=未激活，蓝=2阶激活，金=4阶激活（战斗中死亡也会保留羁绊）");
    }

    private Texture2D CreateFlatTexture(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void EnsureGuiStyles()
    {
        if (stylesReady) return;

        boxStyle = new GUIStyle(GUI.skin.box);
        buttonStyle = new GUIStyle(GUI.skin.button);
        labelStyle = new GUIStyle(GUI.skin.label);

        // 统一深蓝铜色主题：提高层次感和信息可读性
        flatPanelTex = CreateFlatTexture(new Color(0.05f, 0.09f, 0.14f, 0.92f));
        flatButtonTex = CreateFlatTexture(new Color(0.15f, 0.34f, 0.56f, 0.98f));
        flatButtonHoverTex = CreateFlatTexture(new Color(0.2f, 0.44f, 0.7f, 0.99f));
        flatButtonActiveTex = CreateFlatTexture(new Color(0.1f, 0.24f, 0.42f, 0.99f));

        // 通用面板统一用程序化底色，避免贴图风格不统一
        boxStyle.normal.background = flatPanelTex;
        boxStyle.hover.background = boxStyle.normal.background;
        boxStyle.active.background = boxStyle.normal.background;
        boxStyle.focused.background = boxStyle.normal.background;
        boxStyle.padding = new RectOffset(12, 12, 10, 10);
        boxStyle.richText = true;
        boxStyle.normal.textColor = new Color(0.93f, 0.96f, 1f);
        boxStyle.hover.textColor = boxStyle.normal.textColor;
        boxStyle.active.textColor = boxStyle.normal.textColor;
        boxStyle.focused.textColor = boxStyle.normal.textColor;
        boxStyle.fontSize = 13;

        // 交互按钮统一使用深色底，保障所有文案可读性
        buttonStyle.normal.background = flatButtonTex;
        buttonStyle.hover.background = flatButtonHoverTex;
        buttonStyle.active.background = flatButtonActiveTex;
        buttonStyle.focused.background = flatButtonHoverTex;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 13;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = new Color(0.95f, 0.98f, 1f);
        buttonStyle.padding = new RectOffset(8, 8, 6, 6);
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
        labelStyle.hover.textColor = labelStyle.normal.textColor;
        labelStyle.active.textColor = labelStyle.normal.textColor;
        labelStyle.focused.textColor = labelStyle.normal.textColor;
        labelStyle.fontSize = 13;

        stylesReady = true;
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        var oldBox = GUI.skin.box;
        var oldButton = GUI.skin.button;
        var oldLabel = GUI.skin.label;
        GUI.skin.box = boxStyle;
        GUI.skin.button = buttonStyle;
        GUI.skin.label = labelStyle;

        string topInfo = $"生命:{playerLife}  金币:{gold}  等级:{playerLevel}({exp}/{ExpNeed(playerLevel)})  上阵上限:{GetBoardCap()}  连胜:{winStreak} 连败:{loseStreak}";
        GUI.Box(new Rect(16, 12, 860, 96), "");
        GUI.Label(new Rect(30, 18, 840, 20), "龙棋传说 | 中国象棋 x 自走棋 x 海克斯构筑");
        GUI.Label(new Rect(30, 42, 840, 20), topInfo);
        GUI.Label(new Rect(30, 66, 840, 28), $"战报：{battleLog}");

        string stateText = state switch
        {
            RunState.Stage => "阶段选择",
            RunState.Prepare => "准备阶段",
            RunState.Battle => "自动战斗",
            RunState.Reward => "战斗结算",
            RunState.Hex => "海克斯选择",
            RunState.GameOver => "章节结束",
            _ => "状态未知"
        };
        GUI.Box(new Rect(886, 12, 318, 48), $"当前状态：{stateText}");
        GUI.Box(new Rect(886, 62, 318, 46), "情报提示：点击棋子或羁绊卡片查看详细说明");

        if (GUI.Button(new Rect(1088, 14, 108, 28), "调试+999金"))
        {
            gold += 999;
            battleLog = "作弊生效：金币 +999";
        }
        if (GUI.Button(new Rect(950, 14, 130, 28), "开发推进一步"))
        {
            DevAdvanceOneStep();
        }
        if (GUI.Button(new Rect(950, 46, 130, 28), "开发重开"))
        {
            RestartRun();
        }
        if (GUI.Button(new Rect(810, 14, 130, 60), "自动回归3关"))
        {
            DevRunRegression3Floors();
        }

        GUI.Box(new Rect(16, 112, 560, 92), "");
        GUI.Label(new Rect(28, 118, 180, 20), "已选海克斯");
        string hexTxt = selectedHexes.Count == 0 ? "暂无" : string.Join(" | ", selectedHexes.ConvertAll(h => h.name));
        GUI.Label(new Rect(28, 140, 540, 54), hexTxt);

        if (state == RunState.Stage)
        {
            if (stageIndex >= stages.Count)
            {
                GUI.Box(new Rect(16, 220, 420, 110), "恭喜通关当前线性章节！");
                state = RunState.GameOver;
                return;
            }

            var st = stages[stageIndex];
            GUI.Box(new Rect(16, 220, 420, 140), $"下一关：第{st.floor}关  [{StageName(st.type)}]\n强度:{st.power}  过关后海克斯:{(st.giveHex ? "是" : "否")}\n线性推进模式（先不做分叉地图）");
            if (GUI.Button(new Rect(30, 320, 140, 30), "进入准备"))
            {
                StartPreparationForCurrentStage();
            }
        }

        if (state == RunState.Prepare)
        {
            GUI.Box(new Rect(16, 210, 520, 64), $"准备阶段：直接拖拽到战场左侧5列布阵 | 羁绊：{GetSynergySummary(deploySlots)}");

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    Rect cell = GetBoardCellGuiRect(x, y);
                    if (cell.width <= 1f || cell.height <= 1f) continue;

                    Unit placed = null;
                    for (int i = 0; i < deploySlots.Count; i++)
                    {
                        if (deploySlots[i].x == x && deploySlots[i].y == y) { placed = deploySlots[i]; break; }
                    }

                    Color old = GUI.color;
                    if (placed == null)
                    {
                        GUI.color = new Color(0.22f, 0.28f, 0.4f, 0.4f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                    }
                    else
                    {
                        GUI.color = new Color(0.12f, 0.78f, 1f, 0.32f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                    }
                    GUI.color = old;
                }
            }

            float panelX = 16f;
            float panelY = Screen.height - 170f;
            float panelW = Screen.width - 32f;
            GUI.Box(new Rect(panelX, panelY, panelW, 154f), "准备阶段");

            GUI.Label(new Rect(panelX + 16, panelY + 8, 220, 20), "商店（费用分层已接入）");
            for (int i = 0; i < shopOffers.Count; i++)
            {
                var d = unitDefs[shopOffers[i]];
                Rect r = new Rect(panelX + 16 + i * 110, panelY + 28, 104, 45);

                Color old = GUI.color;
                // 商店与备战席保持同一职业配色，避免同棋子颜色不一致
                GUI.color = GetUnitChipColorByClass(d.classTag);

                if (GUI.Button(r, $"{d.name}\n{d.cost}金")) BuyOffer(i);
                GUI.color = old;
            }
            if (GUI.Button(new Rect(panelX + 600, panelY + 34, 120, 28), "刷新(-1)")) RefreshShop();
            if (GUI.Button(new Rect(panelX + 730, panelY + 34, 120, 28), "买经验(-4)"))
            {
                if (gold >= 4) { gold -= 4; GainExp(4); }
            }

            GUI.Label(new Rect(panelX + 16, panelY + 70, 120, 20), "备战席");
            for (int i = 0; i < 8; i++)
            {
                float bx = panelX + 16 + i * 90;
                if (i < benchUnits.Count)
                {
                    var u = benchUnits[i];
                    Color oldBtn = GUI.color;
                    GUI.color = GetUnitChipColor(u);
                    if (GUI.Button(new Rect(bx, panelY + 88, 84, 45), $"{u.Name}\n{u.star}★"))
                    {
                        inspectedUnit = u;
                        ShowTooltip(BuildUnitTooltip(u));
                        battleLog = $"查看 {u.Name}";
                    }
                    GUI.color = oldBtn;
                }
                else GUI.Box(new Rect(bx, panelY + 88, 84, 45), "");
            }

            if (GUI.Button(new Rect(panelX + panelW - 160, panelY + 100, 140, 36), "开始战斗")) StartBattle();

            if (inspectedUnit != null)
            {
                int depIdx = deploySlots.FindIndex(u => u.id == inspectedUnit.id);
                int benIdx = benchUnits.FindIndex(u => u.id == inspectedUnit.id);
                if (depIdx >= 0)
                {
                    if (GUI.Button(new Rect(panelX + panelW - 315, panelY + 100, 140, 36), "下场(备战席)"))
                    {
                        if (ReturnDeployToBench(depIdx)) RedrawPrepareBoard();
                    }
                }
                if (depIdx >= 0 || benIdx >= 0)
                {
                    if (GUI.Button(new Rect(panelX + panelW - 470, panelY + 100, 140, 36), "出售选中"))
                    {
                        if (SellUnit(inspectedUnit)) RedrawPrepareBoard();
                    }
                }
            }

            DrawSynergyClickPanel(548, 220, 660, 126, deploySlots);
        }

        if (state == RunState.Battle)
        {
            int allyAlive = playerUnits.FindAll(u => u.Alive).Count;
            int enemyAlive = enemyUnits.FindAll(u => u.Alive).Count;

            GUI.Box(new Rect(16, 220, 500, 244),
                $"战斗中（自动执行）\n回合速度：{speedLevel}x\n存活单位：我方 {allyAlive} / 敌方 {enemyAlive}\n我方羁绊：{GetSynergySummary(playerUnits)}\n\n最近战况：\n{battleLog}");

            if (GUI.Button(new Rect(30, 416, 130, 32), "切换速度"))
            {
                if (speedLevel == 1) speedLevel = 2;
                else if (speedLevel == 2) speedLevel = 4;
                else speedLevel = 1;
            }
            if (GUI.Button(new Rect(170, 416, 170, 32), showBattleStats ? "隐藏战斗统计" : "显示战斗统计"))
            {
                showBattleStats = !showBattleStats;
            }

            DrawSynergyClickPanel(16, 470, 500, 120, playerUnits);

            if (showBattleStats)
            {
                DrawBattleStats(540, 170, 668, 470);
            }
        }

        if (state == RunState.Reward)
        {
            GUI.Box(new Rect(16, 220, 760, 280), "战后奖励（三选一）");
            for (int i = 0; i < currentRewardOffers.Count; i++)
            {
                var r = currentRewardOffers[i];
                float x = 30 + i * 240;

                Color old = GUI.color;
                GUI.color = new Color(0.28f, 0.52f, 0.8f, 0.95f);
                GUI.DrawTexture(new Rect(x, 260, 220, 180), Texture2D.whiteTexture);
                GUI.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);
                GUI.DrawTexture(new Rect(x + 2, 262, 216, 176), Texture2D.whiteTexture);
                GUI.color = old;

                GUI.Label(new Rect(x + 12, 276, 196, 28), r.name);
                GUI.Label(new Rect(x + 12, 310, 196, 82), r.desc);

                if (GUI.Button(new Rect(x + 40, 400, 140, 30), "选择")) PickReward(i);
            }

            if (GUI.Button(new Rect(30, 462, 170, 30), showBattleStats ? "隐藏战斗统计" : "显示战斗统计")) showBattleStats = !showBattleStats;
            if (showBattleStats) DrawBattleStats(790, 170, 418, 470);
        }

        if (state == RunState.Hex)
        {
            GUI.Box(new Rect(16, 220, 740, 260), "海克斯选择（三选一）");
            for (int i = 0; i < currentHexOffers.Count; i++)
            {
                var h = currentHexOffers[i];
                float x = 30 + i * 240;
                Rect card = new Rect(x, 260, 220, 180);

                Color rarityColor = h.rarity switch
                {
                    "金" => new Color(1f, 0.8f, 0.3f, 1f),
                    "蓝" => new Color(0.35f, 0.75f, 1f, 1f),
                    _ => new Color(0.8f, 0.8f, 0.8f, 1f)
                };

                Color old = GUI.color;
                GUI.color = rarityColor;
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);
                GUI.DrawTexture(new Rect(card.x + 2, card.y + 2, card.width - 4, card.height - 4), Texture2D.whiteTexture);
                GUI.color = old;

                var imageRect = new Rect(card.x + 10, card.y + 12, 72, 72);
                if (hexTextures.TryGetValue(h.id, out var hTex) && hTex != null)
                {
                    GUI.DrawTexture(imageRect, hTex, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    GUI.Box(imageRect, "ICON");
                }

                GUI.Label(new Rect(card.x + 90, card.y + 12, card.width - 96, 22), $"[{h.rarity}] {h.name}");
                GUI.Label(new Rect(card.x + 90, card.y + 38, card.width - 96, 110), h.desc);

                if (GUI.Button(new Rect(x + 40, 400, 140, 30), "选择")) PickHex(i);
            }
        }

        if (state == RunState.GameOver)
        {
            GUI.Box(new Rect(16, 220, 520, 130), "章节完成！\n你已经跑通 M1/M2/M3 的基础框架。\n现在可以进入内容填充和平衡阶段。");
            if (GUI.Button(new Rect(30, 315, 140, 30), "重新开始"))
            {
                RestartRun();
            }
        }

        if (state == RunState.Prepare && isDragging)
        {
            string dragText = "拖拽中";
            if (draggingFromBench && draggingDeploy >= 0 && draggingDeploy < benchUnits.Count) dragText = $"拖拽中：{benchUnits[draggingDeploy].Name}";
            if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count) dragText = $"拖拽中：{deploySlots[draggingDeploy].Name}";

            float mx = Input.mousePosition.x + 14f;
            float my = Screen.height - Input.mousePosition.y + 14f;
            var r = new Rect(mx, my, 170, 38);
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width, r.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.18f, 0.42f, 0.72f, 0.96f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(r.x + 8, r.y + 9, r.width - 14, 20), dragText);
        }

        DrawFloatingTooltip();

        GUI.skin.box = oldBox;
        GUI.skin.button = oldButton;
        GUI.skin.label = oldLabel;
    }

    #endregion
}
