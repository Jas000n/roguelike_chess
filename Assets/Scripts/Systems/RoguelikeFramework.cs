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

    private int gold = 10;
    private int playerLevel = 1;
    private int exp;
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

    private readonly List<string> shopOffers = new();

    private int draggingDeploy = -1;
    private GameObject dragGhost;
    private bool isDragging;
    private bool draggingFromBench;

    private Texture2D bgTex;
    private Texture2D tileATex;
    private Texture2D tileBTex;
    private Texture2D dragonIcon;
    private Texture2D horseIcon;
    private Texture2D swordIcon;
    private Texture2D bombIcon;
    private Texture2D shieldIcon;

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

        if (state != RunState.Battle || !battleStarted) return;

        float turnInterval = baseTurnInterval / speedLevel;
        turnTimer += Time.deltaTime;
        if (turnTimer < turnInterval) return;
        turnTimer = 0;

        RunOneTurn();
        RefreshViews();
        CheckBattleEnd();
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
            playerUnits.Add(u);
        }

        SpawnEnemiesForStage(st);

        CreateViews(playerUnits, new Color(0.2f, 0.7f, 1f));
        CreateViews(enemyUnits, new Color(0.95f, 0.35f, 0.4f));
        RefreshViews();
    }

    private void SpawnEnemiesForStage(StageNode st)
    {
        int enemyCount = Mathf.Clamp(3 + st.power, 3, 8);
        int[,] pos = { { 7, 2 }, { 8, 1 }, { 8, 2 }, { 8, 3 }, { 9, 1 }, { 9, 2 }, { 9, 4 }, { 7, 4 } };

        for (int i = 0; i < enemyCount; i++)
        {
            string key = PickEnemyUnitKey(st);
            var u = CreateUnit(key, false);

            float hpScale = 1f + (st.power - 1) * 0.2f;
            float atkScale = 1f + (st.power - 1) * 0.15f;
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
        state = RunState.Reward;

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
            battleLog = $"失败，保底 +{reward}金币";
        }
    }

    private void NextAfterReward()
    {
        if (stageIndex >= stages.Count)
        {
            state = RunState.GameOver;
            return;
        }

        bool giveHex = stages[stageIndex].giveHex;
        stageIndex++;

        if (giveHex)
        {
            RollHexOffers();
            state = RunState.Hex;
            battleLog = "海克斯选择：三选一";
            return;
        }

        state = RunState.Stage;
        battleLog = "继续前进到下一关";
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

            // 同 key 才可合
            var countByKey = new Dictionary<string, List<Unit>>();
            foreach (var u in all)
            {
                if (u.star != 1) continue;
                if (!countByKey.ContainsKey(u.def.key)) countByKey[u.def.key] = new List<Unit>();
                countByKey[u.def.key].Add(u);
            }

            foreach (var kv in countByKey)
            {
                if (kv.Value.Count < 3) continue;

                int removed = 0;
                for (int i = benchUnits.Count - 1; i >= 0 && removed < 3; i--)
                {
                    if (benchUnits[i].def.key == kv.Key && benchUnits[i].star == 1)
                    {
                        benchUnits.RemoveAt(i);
                        removed++;
                    }
                }
                for (int i = deploySlots.Count - 1; i >= 0 && removed < 3; i--)
                {
                    if (deploySlots[i].def.key == kv.Key && deploySlots[i].star == 1)
                    {
                        deploySlots.RemoveAt(i);
                        removed++;
                    }
                }

                var up = CreateUnit(kv.Key, true);
                UpgradeUnit(up);
                benchUnits.Add(up);
                battleLog = $"合成成功：{up.Name} 升为2星";
                merged = true;
                break;
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

    #endregion

    #region Hex / Synergy

    private void RollHexOffers()
    {
        currentHexOffers.Clear();
        var copy = new List<HexDef>(hexPool);
        for (int i = 0; i < 3 && copy.Count > 0; i++)
        {
            int idx = UnityEngine.Random.Range(0, copy.Count);
            currentHexOffers.Add(copy[idx]);
            copy.RemoveAt(idx);
        }
    }

    private void PickHex(int idx)
    {
        if (idx < 0 || idx >= currentHexOffers.Count) return;
        var h = currentHexOffers[idx];
        selectedHexes.Add(h);
        battleLog = $"获得海克斯：{h.name}";
        state = RunState.Stage;
    }

    private bool HasHex(string id)
    {
        foreach (var h in selectedHexes) if (h.id == id) return true;
        return false;
    }

    private int CountClass(List<Unit> team, string classTag)
    {
        int c = 0;
        foreach (var u in team) if (u.Alive && u.ClassTag == classTag) c++;
        return c;
    }

    private string GetSynergySummary(List<Unit> team)
    {
        int v = CountClass(team, "Vanguard");
        int r = CountClass(team, "Rider");
        int a = CountClass(team, "Artillery");

        string s = "";
        if (v >= 2) s += $"钢铁先锋({v}) ";
        if (r >= 2) s += $"机动骑兵({r}) ";
        if (a >= 2) s += $"火力炮阵({a}) ";
        if (string.IsNullOrEmpty(s)) s = "暂无激活羁绊";
        return s;
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
        int dmg = Mathf.RoundToInt(actor.atk * GetDamageMultiplier(actor));

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
                SpawnProjectile(actor, target, new Color(1f, 0.75f, 0.2f));
                battleLog = $"{actor.Name} 攻击 {target.Name} -{real}";
            }
            else
            {
                SpawnHitFlash(target, new Color(1f, 0.2f, 0.2f));
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
                        SpawnHitFlash(t, new Color(1f, 0.5f, 0.2f));
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
            r.material = CreateRuntimeMaterial(PickIcon(u), c);

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
        if (u.Family == "帅") return dragonIcon ?? shieldIcon;
        if (u.Family == "马") return horseIcon;
        if (u.Family == "炮") return bombIcon;
        if (u.Family == "车" || u.Family == "兵") return swordIcon;
        return shieldIcon;
    }

    private void SpawnProjectile(Unit from, Unit to, Color color)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "FX_Projectile";
        p.transform.localScale = Vector3.one * 0.18f;
        p.transform.position = GridToWorld(from.x, from.y) + new Vector3(0, 0, -0.2f);
        p.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, color);
        StartCoroutine(MoveFx(p, GridToWorld(to.x, to.y) + new Vector3(0, 0, -0.2f), 0.14f));
    }

    private void SpawnHitFlash(Unit target, Color color)
    {
        var fx = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fx.name = "FX_Hit";
        fx.transform.position = GridToWorld(target.x, target.y) + new Vector3(0, 0, -0.25f);
        fx.transform.localScale = Vector3.one * 0.6f;
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

    #endregion

    #region Input

    private void HandleUnitInspectClick()
    {
        if ((state != RunState.Battle && state != RunState.Prepare) || !Input.GetMouseButtonDown(0)) return;
        if (isDragging) return;

        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        foreach (var v in views)
        {
            if (v.go == hit.collider.gameObject)
            {
                inspectedUnit = v.unit;
                battleLog = $"查看 {v.unit.Name}{v.unit.star}★";
                return;
            }
        }
    }

    private void HandleMouseDrag()
    {
        if (state != RunState.Prepare) return;

        float boardX = 32f;
        float boardY = 220f;
        float cellW = 70f;
        float cellH = 55f;
        int rows = 4;
        int cols = 7;

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

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float gx = boardX + c * cellW;
                    float gy = boardY + r * cellH;
                    if (mx >= gx && mx <= gx + (cellW - 4f) && myGui >= gy && myGui <= gy + (cellH - 4f)) return new Vector2(c, r);
                }
            }

            for (int i = 0; i < benchCols; i++)
            {
                float bx = benchX + i * benchSlotW;
                if (mx >= bx && mx <= bx + benchBtnW && myGui >= benchY && myGui <= benchY + 45f) return new Vector2(-1, i);
            }
            return new Vector2(-2, -2);
        }

        if (isDragging && dragGhost != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                float zDist = -0.5f - cam.transform.position.z;
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDist));
                dragGhost.transform.position = new Vector3(world.x, world.y, -0.5f);
            }
        }

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 clickGrid = GetGridAtMouse();

        if (!isDragging)
        {
            if (clickGrid.x >= 0 && clickGrid.x < cols && clickGrid.y >= 0 && clickGrid.y < rows)
            {
                int deployIdx = FindDeployAt((int)clickGrid.x, (int)clickGrid.y);
                if (deployIdx >= 0)
                {
                    draggingDeploy = deployIdx;
                    draggingFromBench = false;
                    isDragging = true;
                }
            }
            else if (clickGrid.x == -1)
            {
                int benchIdx = (int)clickGrid.y;
                if (benchIdx >= 0 && benchIdx < benchUnits.Count)
                {
                    draggingDeploy = benchIdx;
                    draggingFromBench = true;
                    isDragging = true;
                }
            }

            if (isDragging)
            {
                dragGhost = GameObject.CreatePrimitive(PrimitiveType.Quad);
                dragGhost.name = "DragGhost";
                dragGhost.transform.localScale = Vector3.one * 0.8f;
                dragGhost.GetComponent<Renderer>().material = CreateRuntimeMaterial(null, new Color(0.3f, 0.8f, 1f, 0.75f));
            }
            return;
        }

        if (clickGrid.x >= 0 && clickGrid.x < cols && clickGrid.y >= 0 && clickGrid.y < rows)
        {
            int tx = (int)clickGrid.x;
            int ty = (int)clickGrid.y;
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
                        moved.x = tx; moved.y = ty;
                        deploySlots.Add(moved);
                        benchUnits.RemoveAt(draggingDeploy);
                        AutoMergeAll();
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
            }
        }
        else if (clickGrid.x == -1)
        {
            if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                int benchIdx = (int)clickGrid.y;
                if (benchIdx >= benchUnits.Count)
                {
                    var u = deploySlots[draggingDeploy];
                    u.x = -1; u.y = -1;
                    benchUnits.Add(u);
                    deploySlots.RemoveAt(draggingDeploy);
                    AutoMergeAll();
                }
                else battleLog = "该备战席格子已有棋子";
            }
        }

        isDragging = false;
        draggingDeploy = -1;
        draggingFromBench = false;
        if (dragGhost != null) Destroy(dragGhost);
        dragGhost = null;
    }

    #endregion

    #region GUI

    private void DrawInspectPanel(float x, float y, float w, float h)
    {
        string txt = "单击棋子查看属性";
        if (inspectedUnit != null)
        {
            txt =
                $"{inspectedUnit.Name}{inspectedUnit.star}★ ({(inspectedUnit.player ? "我方" : "敌方")})\n" +
                $"HP {Mathf.Max(0, inspectedUnit.hp)}/{inspectedUnit.maxHp}  ATK {inspectedUnit.atk}  SPD {inspectedUnit.spd}  RNG {inspectedUnit.range}\n" +
                $"职业 {inspectedUnit.ClassTag} / 阵营 {inspectedUnit.OriginTag}\n" +
                $"本场输出 {inspectedUnit.damageDealt}  本场承伤 {inspectedUnit.damageTaken}";
        }
        GUI.Box(new Rect(x, y, w, h), txt);
    }

    private void DrawBattleStats(float x, float y, float w, float h)
    {
        GUI.Box(new Rect(x, y, w, h), "战斗统计（柱状图）");

        var all = new List<Unit>();
        all.AddRange(playerUnits);
        all.AddRange(enemyUnits);

        int maxVal = 1;
        foreach (var u in all)
        {
            if (u.damageDealt > maxVal) maxVal = u.damageDealt;
            if (u.damageTaken > maxVal) maxVal = u.damageTaken;
        }

        GUI.Label(new Rect(x + 10, y + 20, w - 20, 18), "蓝=造成伤害 橙=承受伤害");

        float barMax = w - 150f;
        float rowH = 18f;
        float gap = 4f;

        void DrawTeam(string title, List<Unit> team, float sy)
        {
            GUI.Label(new Rect(x + 10, sy, w - 20, 18), title);
            float ry = sy + 18f;
            foreach (var u in team)
            {
                GUI.Label(new Rect(x + 10, ry, 78, rowH), $"{u.Name}");
                float dealtW = barMax * (u.damageDealt / (float)maxVal);
                float takenW = barMax * (u.damageTaken / (float)maxVal);

                Color old = GUI.color;
                GUI.color = new Color(0.25f, 0.72f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(x + 90, ry + 1, dealtW, rowH - 2), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.62f, 0.2f, 0.95f);
                GUI.DrawTexture(new Rect(x + 90, ry + rowH * 0.55f, takenW, rowH * 0.4f), Texture2D.whiteTexture);
                GUI.color = old;

                GUI.Label(new Rect(x + 94 + barMax, ry, 70, rowH), $"{u.damageDealt}/{u.damageTaken}");
                ry += rowH + gap;
            }
        }

        float top = y + 40f;
        DrawTeam("我方", playerUnits, top);
        float enemyStart = top + (playerUnits.Count + 1) * (rowH + gap) + 8f;
        DrawTeam("敌方", enemyUnits, enemyStart);
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

    private void OnGUI()
    {
        string topInfo = $"金币:{gold}  等级:{playerLevel}({exp}/{ExpNeed(playerLevel)})  上阵上限:{GetBoardCap()}  连胜:{winStreak} 连败:{loseStreak}";
        GUI.Box(new Rect(16, 12, 760, 95), $"龙棋传说（M1/M2/M3 进行版）\n{topInfo}\n{battleLog}");

        if (GUI.Button(new Rect(784, 20, 130, 32), "作弊 +999金币"))
        {
            gold += 999;
            battleLog = "作弊生效：金币 +999";
        }

        DrawInspectPanel(784, 58, 420, 92);

        GUI.Box(new Rect(16, 110, 350, 95), "已选海克斯");
        string hexTxt = selectedHexes.Count == 0 ? "暂无" : string.Join(" | ", selectedHexes.ConvertAll(h => h.name));
        GUI.Label(new Rect(26, 132, 330, 64), hexTxt);

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
            float cellW = 70f;
            float cellH = 55f;
            int rows = 4;
            int cols = 7;

            float boardX = 32f;
            float boardY = 220f;
            GUI.Box(new Rect(16, 210, 520, 255), $"战场（拖拽布阵）| 羁绊：{GetSynergySummary(deploySlots)}");

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float gx = boardX + c * cellW;
                    float gy = boardY + r * cellH;

                    Unit placed = null;
                    int placedIdx = -1;
                    for (int i = 0; i < deploySlots.Count; i++)
                    {
                        if (deploySlots[i].x == c && deploySlots[i].y == r) { placed = deploySlots[i]; placedIdx = i; break; }
                    }

                    if (placed != null)
                    {
                        if (GUI.Button(new Rect(gx, gy, cellW - 4, cellH - 4), $"{placed.Name}\n{placed.star}★"))
                        {
                            inspectedUnit = placed;
                            battleLog = $"查看 {placed.Name}";
                        }
                    }
                    else GUI.Box(new Rect(gx, gy, cellW - 4, cellH - 4), "");
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
                if (GUI.Button(new Rect(panelX + 16 + i * 110, panelY + 28, 104, 45), $"{d.name}\n{d.cost}金")) BuyOffer(i);
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
                    if (GUI.Button(new Rect(bx, panelY + 88, 84, 45), $"{u.Name}\n{u.star}★"))
                    {
                        inspectedUnit = u;
                        battleLog = $"查看 {u.Name}";
                    }
                }
                else GUI.Box(new Rect(bx, panelY + 88, 84, 45), "");
            }

            if (GUI.Button(new Rect(panelX + panelW - 160, panelY + 100, 140, 36), "开始战斗")) StartBattle();
        }

        if (state == RunState.Battle)
        {
            GUI.Box(new Rect(16, 220, 360, 90), $"战斗中（自动）\n速度：{speedLevel}x | 我方羁绊：{GetSynergySummary(playerUnits)}");
            if (GUI.Button(new Rect(30, 280, 120, 24), "切换加速"))
            {
                if (speedLevel == 1) speedLevel = 2;
                else if (speedLevel == 2) speedLevel = 4;
                else speedLevel = 1;
            }

            DrawBattleStats(548, 170, 660, 470);
        }

        if (state == RunState.Reward)
        {
            GUI.Box(new Rect(16, 220, 420, 130), "结算\n可继续下一关（线性）\n关卡奖励与连胜经济已生效");
            if (GUI.Button(new Rect(30, 315, 140, 30), "继续")) NextAfterReward();
            DrawBattleStats(548, 170, 660, 470);
        }

        if (state == RunState.Hex)
        {
            GUI.Box(new Rect(16, 220, 740, 260), "海克斯选择（三选一）");
            for (int i = 0; i < currentHexOffers.Count; i++)
            {
                var h = currentHexOffers[i];
                float x = 30 + i * 240;
                GUI.Box(new Rect(x, 260, 220, 180), $"[{h.rarity}] {h.name}\n\n{h.desc}");
                if (GUI.Button(new Rect(x + 40, 400, 140, 30), "选择")) PickHex(i);
            }
        }

        if (state == RunState.GameOver)
        {
            GUI.Box(new Rect(16, 220, 520, 130), "章节完成！\n你已经跑通 M1/M2/M3 的基础框架。\n现在可以进入内容填充和平衡阶段。");
            if (GUI.Button(new Rect(30, 315, 140, 30), "重新开始"))
            {
                stageIndex = 0;
                gold = 10;
                playerLevel = 1;
                exp = 0;
                winStreak = 0;
                loseStreak = 0;
                selectedHexes.Clear();
                benchUnits.Clear();
                deploySlots.Clear();
                benchUnits.Add(CreateUnit("soldier_sword", true));
                benchUnits.Add(CreateUnit("horse_raider", true));
                benchUnits.Add(CreateUnit("cannon_burst", true));
                state = RunState.Stage;
                battleLog = "新一轮开始";
            }
        }
    }

    #endregion
}
