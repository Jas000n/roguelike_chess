using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public partial class RoguelikeFramework : MonoBehaviour
{
    private enum RunState { Stage, Prepare, Battle, Hex, Reward, GameOver }
    private enum StageType { Normal, Elite, Shop, Mystery, Treasure, Boss }

    private class StageNode
    {
        public string id;
        public StageType type;
        public StageType revealedType;
        public bool mysteryRevealed;
        public int floor;
        public int lane;
        public int power;
        public bool giveHex;
        public bool cleared;
        public readonly List<string> nextIds = new();
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

    private int freeRerollTurns;
    private int interestCapModifier;

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

    // State fields moved to RoguelikeFramework.State.cs


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
        RevalidateConfigData();
        if (!configValidationStatus.StartsWith("pass="))
        {
            string msg = $"[DEV][CONFIG_VALIDATE] FAILED {configValidationStatus}";
            throw new InvalidOperationException(msg);
        }

        LoadGeneratedUnitArt();
        LoadGeneratedHexArt();
        RefreshShop(true);
        FillRandomOpeningBench();

        battleLog = "新一轮已生成：分支地图 + 随机起手 + 海克斯 + 羁绊 + 经济";
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
        if (Input.GetKeyDown(KeyCode.F8))
        {
            devTurboBattle = !devTurboBattle;
            speedLevel = devTurboBattle ? 16 : 4;
            battleLog = devTurboBattle ? "开发开关：极速战斗 x16" : "开发开关：恢复战斗速度 x4";
        }
        if (Input.GetKeyDown(KeyCode.F9)) DevQuickStartToPrepare();
        if (Input.GetKeyDown(KeyCode.F10)) DevSkipCurrentFloor();
        if (Input.GetKeyDown(KeyCode.F11)) DevSkipToBoss();
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

    // Setup Data logic moved to RoguelikeFramework.Data.cs



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
            var r = go.GetComponent<Renderer>();
            // 强化星级视觉(Stage B1)：高星棋子体型小幅变大
            float starScale = 0.8f + (u.star - 1) * 0.12f;
            go.transform.localScale = new Vector3(starScale, starScale, 1);
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
        bool inPlay = Application.isPlaying;
        foreach (var v in views)
        {
            if (v.go)
            {
                if (inPlay) Destroy(v.go);
                else DestroyImmediate(v.go);
            }
            if (v.hpBg)
            {
                if (inPlay) Destroy(v.hpBg);
                else DestroyImmediate(v.hpBg);
            }
            if (v.hpFill)
            {
                if (inPlay) Destroy(v.hpFill);
                else DestroyImmediate(v.hpFill);
            }
        }
        views.Clear();

        foreach (var c in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (c.name != "Cell") continue;
            if (inPlay) Destroy(c);
            else DestroyImmediate(c);
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
        return GetUnitChipColorByCost(u.def.cost);
    }

    private Color GetUnitChipColorByCost(int cost)
    {
        return Mathf.Clamp(cost, 1, 5) switch
        {
            1 => new Color(0.78f, 0.8f, 0.84f, 0.97f),
            2 => new Color(0.16f, 0.62f, 0.34f, 0.97f),
            3 => new Color(0.18f, 0.42f, 0.8f, 0.97f),
            4 => new Color(0.54f, 0.24f, 0.72f, 0.97f),
            5 => new Color(0.84f, 0.48f, 0.12f, 0.97f),
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