using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public partial class RoguelikeFramework : MonoBehaviour
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
        public bool usedOriginProc;
        public int attacksDone;

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

    private class CompDef
    {
        public string id;
        public string name;
        public string desc;
        public string[] focusClasses;
        public string[] focusOrigins;
        public int needClass2A;
        public int needClass2B;
        public string classA;
        public string classB;
        public float bonusDmg;
        public float bonusReduction;
        public int bonusSpeed;
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

    private readonly List<CompDef> compDefs = new();
    private string lockedCompId = "";

    private int gold = 10;
    private int playerLevel = 1;
    private int exp;
    private int playerLife = 36;
    private int winStreak;
    private int loseStreak;
    private int battleStartedTurn;
    private int rewardBoardCapBonus;

    private bool battleStarted;
    private float turnTimer;
    private readonly float baseTurnInterval = 0.55f;
    private int turnIndex;
    private int speedLevel = 4;
    private string battleLog = "v0.1 -> v0.2~0.3 进行中";
    private int lastEmergencyStage = -1;
    private readonly List<string> recentEvents = new();
    private bool lastBattleWin;
    private string lastBattleSummary = "";
    private Unit inspectedUnit;
    private bool showBattleStats = true;
    private bool showDevTools = false;
    private bool showCompPanelFoldout;
    private string selectedSynergyKey = "";
    private bool showTooltip;
    private string tooltipText = "";
    private Vector2 tooltipPos;

    private readonly List<string> shopOffers = new();
    private int lockedCompMissStreak;
    private bool lockShop;
    private int rerollEngineFreeUses;
    private readonly HashSet<string> compMilestoneRewarded = new();

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
    private GUIStyle titleStyle;
    private GUIStyle wrapLabelStyle;
    private GUIStyle hudStatStyle;
    private GUIStyle chipTitleStyle;
    private GUIStyle chipMetaStyle;
    private bool stylesReady;
    private float uiScale = 1f;
    private Font uiDynamicFont;

    private readonly Dictionary<string, Texture2D> hexTextures = new();
    private readonly Dictionary<string, Texture2D> unitTextures = new();
    private bool autoDevTriggered;
    private string devPersistentPath = "";
    private string devAutoRunStatus = "idle";

    private const int W = 10;
    private const int H = 6;

    private void Start()
    {
        devPersistentPath = Application.persistentDataPath;
        SetupCamera();
        LoadArt();
        DrawBackground();
        BuildUnitDefs();
        BuildCompDefs();
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
        if (!autoDevTriggered)
        {
            autoDevTriggered = true;
            try
            {
                string triggerA = Path.Combine(Application.persistentDataPath, "DevReports", "autorun_100.flag");
                string triggerB = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "DefaultCompany", "DragonChessLegends", "DevReports", "autorun_100.flag");
                string trigger = File.Exists(triggerA) ? triggerA : (File.Exists(triggerB) ? triggerB : "");
                if (!string.IsNullOrEmpty(trigger))
                {
                    devAutoRunStatus = "running_100";
                    DevRunBalanceIterations(100);
                    battleLog = $"[DEV] 检测到 autorun_100.flag（{trigger}），已自动执行100轮平衡回归";
                    devAutoRunStatus = "done_100";
                    File.Delete(trigger);
                }
                else devAutoRunStatus = "no_flag";
            }
            catch { devAutoRunStatus = "autorun_error"; }
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDevTools = !showDevTools;
            battleLog = showDevTools ? "开发工具已展开（F1 切换）" : "开发工具已收起（F1 切换）";
        }
        if (Input.GetKeyDown(KeyCode.F2)) DevAdvanceOneStep();
        if (Input.GetKeyDown(KeyCode.F3)) RestartRun();
        if (Input.GetKeyDown(KeyCode.F4)) DevRunRegression3Floors();
        if (Input.GetKeyDown(KeyCode.F5)) DevRunBalanceIterations(50);
        if (Input.GetKeyDown(KeyCode.F6)) DevRunBalanceIterations(100);
        if (Input.GetKeyDown(KeyCode.F7)) DevRunUiSmokeTest();
        if (Input.GetKeyDown(KeyCode.B)) DevRunBalanceIterations(50);
        if (Input.GetKeyDown(KeyCode.N)) DevRunBalanceIterations(100);
        if (state == RunState.Prepare && Input.GetKeyDown(KeyCode.Space)) StartBattle();

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

    private void PushEvent(string evt)
    {
        if (string.IsNullOrEmpty(evt)) return;
        recentEvents.Add(evt);
        while (recentEvents.Count > 6) recentEvents.RemoveAt(0);
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

    // Dev flow helpers moved to RoguelikeFramework.Flow.cs

    #region Setup Data

    private void BuildUnitDefs()
    {
        unitDefs.Clear();
        basePool.Clear();

        // 车系变体
        AddDef("chariot_tank", "坦克车", "车", "Vanguard", "Steel", 3, 66, 7, 5, 1);
        AddDef("chariot_sport", "跑车", "车", "Rider", "Wind", 3, 38, 11, 13, 1);
        AddDef("chariot_shock", "震荡车", "车", "Vanguard", "Thunder", 4, 58, 11, 7, 1);

        // 马系变体
        AddDef("horse_raider", "突袭马", "马", "Rider", "Shadow", 2, 33, 11, 12, 1);
        AddDef("horse_banner", "战旗马", "马", "Rider", "Stone", 3, 40, 9, 10, 1);
        AddDef("horse_nightmare", "梦魇马", "马", "Rider", "Night", 4, 37, 15, 11, 1);
        AddDef("horse_wind", "疾风马", "马", "Rider", "Wind", 2, 31, 10, 13, 1);

        // 炮系变体
        AddDef("cannon_missile", "导弹炮", "炮", "Artillery", "Blaze", 4, 29, 18, 7, 4);
        AddDef("cannon_mortar", "迫击炮", "炮", "Artillery", "Stone", 3, 34, 13, 7, 3);
        AddDef("cannon_burst", "连发炮", "炮", "Artillery", "Steel", 2, 28, 10, 10, 3);
        AddDef("cannon_sniper", "狙击炮", "炮", "Artillery", "Shadow", 3, 25, 16, 8, 5);
        AddDef("cannon_arc", "电弧炮", "炮", "Artillery", "Thunder", 2, 27, 11, 9, 3);
        AddDef("cannon_scout", "侦察炮", "炮", "Artillery", "Wind", 1, 22, 8, 10, 3);
        AddDef("cannon_frost", "寒晶炮", "炮", "Artillery", "Frost", 4, 27, 17, 7, 4);

        // 基础补充
        AddDef("general_fire", "火焰君主", "帅", "Leader", "Blaze", 5, 58, 13, 8, 1);
        AddDef("ele_guard", "岩石巨像", "象", "Guardian", "Stone", 3, 60, 8, 5, 1);
        AddDef("guard_assassin", "暗影士", "士", "Assassin", "Shadow", 2, 30, 11, 11, 1);
        AddDef("guard_blade", "夜刃士", "士", "Assassin", "Night", 2, 29, 12, 12, 1);
        AddDef("guard_poison", "毒牙士", "士", "Assassin", "Venom", 3, 32, 13, 11, 1);
        AddDef("guard_mirror", "镜影士", "士", "Assassin", "Venom", 4, 31, 16, 12, 1);
        AddDef("guard_mist", "雾刃士", "士", "Assassin", "Mist", 3, 31, 13, 12, 1);
        AddDef("guard_holy", "圣卫士", "士", "Guardian", "Holy", 3, 54, 8, 7, 1);
        AddDef("chariot_bulwark", "壁垒车", "车", "Vanguard", "Stone", 2, 58, 7, 6, 1);
        AddDef("chariot_ram", "冲城车", "车", "Vanguard", "Blaze", 4, 62, 10, 6, 1);
        AddDef("soldier_phalanx", "方阵兵", "兵", "Vanguard", "Steel", 1, 34, 6, 7, 1);
        AddDef("soldier_guard", "护卫兵", "兵", "Vanguard", "Stone", 1, 36, 6, 7, 1);
        AddDef("soldier_sword", "剑士兵", "兵", "Soldier", "Steel", 1, 26, 9, 8, 1);
        AddDef("soldier_zeal", "誓约兵", "兵", "Soldier", "Holy", 2, 29, 10, 9, 1);
        AddDef("horse_lancer", "枪骑马", "马", "Rider", "Steel", 2, 35, 11, 11, 1);

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
        stages.Add(new StageNode { floor = 6, type = StageType.Elite, power = 4, giveHex = true });
        stages.Add(new StageNode { floor = 7, type = StageType.Normal, power = 5, giveHex = false });
        stages.Add(new StageNode { floor = 8, type = StageType.Boss, power = 5, giveHex = true });
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

        StartPreparationForCurrentStage();
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

    // Core run flow moved to RoguelikeFramework.Flow.cs

    // Units/Shop/Economy flow moved to RoguelikeFramework.Economy.cs



    // Hex/Synergy flow moved to RoguelikeFramework.Synergy.cs



    // Battle logic moved to RoguelikeFramework.Battle.cs



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

    private Texture2D PickIcon(UnitDef d)
    {
        if (d != null && unitTextures.TryGetValue(d.key, out var tex) && tex != null) return tex;
        if (d == null) return shieldIcon;
        if (d.family == "帅") return dragonIcon ?? shieldIcon;
        if (d.family == "马") return horseIcon;
        if (d.family == "炮") return bombIcon;
        if (d.family == "车" || d.family == "兵") return swordIcon;
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
            "cannon_sniper" => new Color(0.62f, 0.82f, 1f),
            "cannon_arc" => new Color(0.72f, 0.62f, 1f),
            "cannon_scout" => new Color(0.55f, 0.95f, 0.95f),
            "chariot_tank" => new Color(0.65f, 0.85f, 1f),
            "chariot_sport" => new Color(0.35f, 1f, 0.9f),
            "chariot_shock" => new Color(0.8f, 0.7f, 1f),
            "chariot_bulwark" => new Color(0.62f, 0.8f, 0.72f),
            "chariot_ram" => new Color(0.9f, 0.66f, 0.45f),
            "soldier_phalanx" => new Color(0.7f, 0.85f, 0.78f),
            "horse_raider" => new Color(0.6f, 0.95f, 0.6f),
            "horse_banner" => new Color(0.6f, 0.85f, 1f),
            "horse_nightmare" => new Color(0.8f, 0.55f, 1f),
            "guard_poison" => new Color(0.66f, 1f, 0.45f),
            "guard_mirror" => new Color(0.72f, 0.72f, 1f),
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
            // 深色高对比调色，保障按钮文字可读
            "Vanguard" => new Color(0.18f, 0.4f, 0.64f, 0.97f),
            "Rider" => new Color(0.15f, 0.5f, 0.36f, 0.97f),
            "Artillery" => new Color(0.58f, 0.3f, 0.16f, 0.97f),
            "Assassin" => new Color(0.38f, 0.22f, 0.62f, 0.97f),
            _ => new Color(0.22f, 0.3f, 0.46f, 0.97f)
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
        float s = Mathf.Max(0.01f, uiScale);
        return new Rect((c.x - w * 0.5f) / s, ((Screen.height - c.y) - h * 0.5f) / s, w / s, h / s);
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

    // Input logic moved to RoguelikeFramework.Input.cs



    // GUI logic moved to RoguelikeFramework.UI.cs


}
