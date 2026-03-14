using System;
using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
    private RunState state = RunState.Stage;

    private readonly List<StageNode> stages = new();
    private readonly Dictionary<string, StageNode> stageNodeById = new();
    private readonly List<string> availableStageNodeIds = new();
    private string currentStageNodeId = "";
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
    private readonly List<HexDef> currentShopHexOffers = new();
    private readonly List<int> currentShopHexCosts = new();
    private bool pendingEliteHexReward;

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
    private bool devTurboBattle;
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
    private int compHitObsRefreshes;
    private int compHitObsClassSlots;
    private int compHitObsOriginSlots;
    private int spikeProbeAssassinContractHits;
    private int spikeProbeArtilleryOverclockHits;
    private int spikeProbeTriServiceHits;
    private int spikeScenarioWarnLast;
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
    private string configValidationStatus = "not-run";
    private string shopOddsConfigSource = "fallback-const";
    private bool shopOddsAssetChecked;
    private readonly Dictionary<int, float[]> shopOddsRuntimeOverride = new();
    private int devBatchFailCount;

    private const int W = 10;
    private const int H = 6;
}
