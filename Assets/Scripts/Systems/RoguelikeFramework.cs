using System.Collections.Generic;
using UnityEngine;

public class RoguelikeFramework : MonoBehaviour
{
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

    private enum RunState { Map, Prepare, Battle, Reward }
    private RunState state = RunState.Map;

    private int floor = 1;
    private bool battleStarted;
    private float turnTimer;
    private readonly float baseTurnInterval = 0.55f;
    private int turnIndex;
    private int speedLevel = 4; // 1x / 2x / 4x，默认4x
    private string battleLog = "点击【进入第一关】开始对战";

    private readonly List<Unit> playerUnits = new();
    private readonly List<Unit> enemyUnits = new();
    private readonly List<UnitView> views = new();

    // 第二步：商店/备战席/站位/合成
    private int gold = 10;
    private bool runInitialized;
    private readonly List<string> shopOffers = new();
    private readonly List<Unit> benchUnits = new(); // 备战席
    private readonly List<Unit> deploySlots = new(); // 上阵槽位（最多7）
    private int selectedBench = -1;
    private int selectedDeploy = -1;
    private readonly string[] baseNames = new[] { "帅", "车", "马", "炮", "象", "士", "兵" };
    private readonly Dictionary<string, int> piecePrice = new()
    {
        { "兵", 1 }, { "士", 2 }, { "象", 2 }, { "马", 3 }, { "车", 4 }, { "炮", 4 }, { "帅", 5 }
    };

    private int draggingDeploy = -1;
    private Vector3 dragStartPos;
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

    private class Unit
    {
        public string name;
        public int hp;
        public int atk;
        public int spd;
        public int range = 1; // 近战=1，远程>1
        public int star = 1;
        public int x;
        public int y;
        public bool player;
        public bool Alive => hp > 0;
    }

    private int EffectiveAtk(Unit u)
    {
        int aura = 0;
        var team = u.player ? playerUnits : enemyUnits;
        foreach (var ally in team)
        {
            if (!ally.Alive || !ally.name.Contains("帅")) continue;
            int d = Mathf.Abs(ally.x - u.x) + Mathf.Abs(ally.y - u.y);
            if (d <= 1 && ally != u) aura += 2; // 帅：邻近光环
        }
        return u.atk + aura;
    }

    private Unit CreateBaseUnit(string n, bool player)
    {
        var u = new Unit { name = n, player = player };
        switch (n)
        {
            case "帅": u.hp = 40; u.atk = 8; u.spd = 6; u.range = 1; break;
            case "车": u.hp = 34; u.atk = 9; u.spd = 7; u.range = 1; break;
            case "马": u.hp = 28; u.atk = 10; u.spd = 10; u.range = 1; break;
            case "炮": u.hp = 24; u.atk = 10; u.spd = 8; u.range = 3; break;
            case "象": u.hp = 36; u.atk = 7; u.spd = 5; u.range = 1; break;
            case "士": u.hp = 26; u.atk = 8; u.spd = 9; u.range = 1; break;
            default: u.hp = 22; u.atk = 7; u.spd = 8; u.range = 1; break;
        }
        return u;
    }

    private Unit CloneUnit(Unit src)
    {
        return new Unit { name = src.name, hp = src.hp, atk = src.atk, spd = src.spd, range = src.range, star = src.star, player = src.player, x = src.x, y = src.y };
    }

    private void UpgradeUnit(Unit u)
    {
        u.star++;
        u.hp = Mathf.RoundToInt(u.hp * 1.5f);
        u.atk = Mathf.RoundToInt(u.atk * 1.35f);
        u.spd += 1;
    }

    private void InitPreparation()
    {
        state = RunState.Prepare;
        battleStarted = false;
        battleLog = "准备阶段：购买、上阵、调站位";
        selectedBench = -1;
        selectedDeploy = -1;

        // 只在第一次初始化阵容；后续阶段保留上阶段购买和编队结果
        if (!runInitialized)
        {
            runInitialized = true;
            gold = 10;
            benchUnits.Clear();
            deploySlots.Clear();

            benchUnits.Add(CreateBaseUnit("兵", true));
            benchUnits.Add(CreateBaseUnit("兵", true));
            benchUnits.Add(CreateBaseUnit("马", true));
        }

        RefreshShop();
        AutoMergeAll();
    }

    private void RefreshShop()
    {
        if (gold < 1 && shopOffers.Count > 0) return;
        if (shopOffers.Count > 0) gold -= 1;
        shopOffers.Clear();
        for (int i = 0; i < 5; i++) shopOffers.Add(baseNames[Random.Range(0, baseNames.Length)]);
    }

    private void BuyOffer(int i)
    {
        if (i < 0 || i >= shopOffers.Count) return;
        string n = shopOffers[i];
        int cost = piecePrice.ContainsKey(n) ? piecePrice[n] : 3;
        if (gold < cost) { battleLog = $"金币不足（需要{cost}）"; return; }
        if (benchUnits.Count >= 8) { battleLog = "备战席已满"; return; }
        gold -= cost;
        benchUnits.Add(CreateBaseUnit(n, true));
        battleLog = $"购买 {n} -{cost}金";
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
            foreach (var n in baseNames)
            {
                var same = all.FindAll(u => u.name == n && u.star == 1);
                if (same.Count >= 3)
                {
                    int removed = 0;
                    for (int i = benchUnits.Count - 1; i >= 0 && removed < 3; i--)
                    {
                        if (benchUnits[i].name == n && benchUnits[i].star == 1) { benchUnits.RemoveAt(i); removed++; }
                    }
                    for (int i = deploySlots.Count - 1; i >= 0 && removed < 3; i--)
                    {
                        if (deploySlots[i].name == n && deploySlots[i].star == 1) { deploySlots.RemoveAt(i); removed++; }
                    }
                    var up = CreateBaseUnit(n, true);
                    UpgradeUnit(up);
                    benchUnits.Add(up);
                    battleLog = $"合成成功：{n} 升为2星";
                    merged = true;
                    break;
                }
            }
        } while (merged);
    }

    private class UnitView
    {
        public Unit unit;
        public GameObject go;
        public TextMesh text;
    }

    private void Start()
    {
        SetupCamera();
        LoadArt();
        DrawBackground();
    }

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

    private void Update()
    {
        HandleMouseDrag();

        if (state != RunState.Battle || !battleStarted) return;

        float turnInterval = baseTurnInterval / speedLevel;
        turnTimer += Time.deltaTime;
        if (turnTimer < turnInterval) return;
        turnTimer = 0;

        RunOneTurn();
        RefreshViews();
        CheckBattleEnd();
    }

    private void HandleMouseDrag()
    {
        if (state != RunState.Prepare) return;

        // 战场区域（与 OnGUI 保持一致）
        float boardX = 32f;
        float boardY = 136f;
        float cellW = 70f;
        float cellH = 55f;
        int rows = 4;
        int cols = 7;

        // 底部面板区域（商店 + 备战席）
        float panelX = 16f;
        float panelY = Screen.height - 170f;
        float benchX = panelX + 16f;
        float benchY = panelY + 88f;
        float benchCellW = 70f;
        int benchCols = 8;

        int FindDeployAt(int x, int y)
        {
            for (int i = 0; i < deploySlots.Count; i++)
            {
                if (deploySlots[i].x == x && deploySlots[i].y == y) return i;
            }
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
                    if (mx >= gx && mx <= gx + (cellW - 4f) && myGui >= gy && myGui <= gy + (cellH - 4f))
                    {
                        return new Vector2(c, r);
                    }
                }
            }

            for (int i = 0; i < benchCols; i++)
            {
                float bx = benchX + i * benchCellW;
                if (mx >= bx && mx <= bx + 65f && myGui >= benchY && myGui <= benchY + 45f)
                {
                    return new Vector2(-1, i);
                }
            }

            return new Vector2(-2, -2);
        }

        // 已选中时：棋子跟随鼠标
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

        // 第一次点击：选中棋子（备战席或战场）
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
                var r = dragGhost.GetComponent<Renderer>();
                r.material = CreateRuntimeMaterial(null, new Color(0.3f, 0.8f, 1f, 0.75f));
            }

            return;
        }

        // 第二次点击：放下（类似金铲铲点击拾取/点击放置）
        if (clickGrid.x >= 0 && clickGrid.x < cols && clickGrid.y >= 0 && clickGrid.y < rows)
        {
            int tx = (int)clickGrid.x;
            int ty = (int)clickGrid.y;
            int targetDeployIdx = FindDeployAt(tx, ty);

            if (draggingFromBench)
            {
                if (draggingDeploy >= 0 && draggingDeploy < benchUnits.Count)
                {
                    if (targetDeployIdx >= 0)
                    {
                        battleLog = "目标格已有棋子，请放到空格";
                    }
                    else if (deploySlots.Count >= 7)
                    {
                        battleLog = "上阵已满（最多7个）";
                    }
                    else
                    {
                        var moved = benchUnits[draggingDeploy];
                        moved.x = tx;
                        moved.y = ty;
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
                    int oldX = deploySlots[draggingDeploy].x;
                    int oldY = deploySlots[draggingDeploy].y;
                    deploySlots[targetDeployIdx].x = oldX;
                    deploySlots[targetDeployIdx].y = oldY;
                }

                deploySlots[draggingDeploy].x = tx;
                deploySlots[draggingDeploy].y = ty;
            }
        }
        else if (clickGrid.x == -1)
        {
            // 放回备战席（仅战场棋子可放回）
            if (!draggingFromBench && draggingDeploy >= 0 && draggingDeploy < deploySlots.Count)
            {
                int benchIdx = (int)clickGrid.y;
                if (benchIdx >= benchUnits.Count)
                {
                    var u = deploySlots[draggingDeploy];
                    u.x = -1;
                    u.y = -1;
                    benchUnits.Add(u);
                    deploySlots.RemoveAt(draggingDeploy);
                    AutoMergeAll();
                }
                else
                {
                    battleLog = "该备战席格子已有棋子";
                }
            }
        }

        // 无论是否成功放置，第二次点击后结束选中
        isDragging = false;
        draggingDeploy = -1;
        draggingFromBench = false;
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }
    }

    private void StartFirstBattle()
    {
        state = RunState.Battle;
        battleStarted = true;
        turnIndex = 0;
        battleLog = "战斗开始";

        playerUnits.Clear();
        enemyUnits.Clear();
        ClearViews();
        DrawBoard();

        // 玩家来自备战/上阵结果（使用棋盘坐标）
        if (deploySlots.Count == 0)
        {
            // 防呆：空阵容则自动带3个兵
            var u1 = CreateBaseUnit("兵", true); u1.x = 0; u1.y = 1; deploySlots.Add(u1);
            var u2 = CreateBaseUnit("马", true); u2.x = 1; u2.y = 1; deploySlots.Add(u2);
            var u3 = CreateBaseUnit("炮", true); u3.x = 2; u3.y = 1; deploySlots.Add(u3);
        }

        // 映射到战场坐标
        int[,] playerPos = { { 0, 1 }, { 0, 2 }, { 0, 3 }, { 1, 1 }, { 1, 2 }, { 1, 3 }, { 2, 2 } };
        for (int i = 0; i < deploySlots.Count && i < 7; i++)
        {
            var u = CloneUnit(deploySlots[i]);
            u.player = true;
            // 使用之前保存的坐标，或者使用默认值
            if (u.x < 0 || u.x >= W || u.y < 0 || u.y >= H)
            {
                u.x = playerPos[i, 0];
                u.y = playerPos[i, 1];
            }
            playerUnits.Add(u);
        }

        // 敌方阵容随机生成
        enemyUnits.Clear();
        int enemyCount = Mathf.Min(3 + floor, 7); // 随回合数增加
        var used = new HashSet<string>();
        for (int i = 0; i < enemyCount; i++)
        {
            string n;
            do { n = baseNames[Random.Range(0, baseNames.Length)]; } while (used.Contains(n));
            used.Add(n);
            var u = CreateBaseUnit(n, false);
            int[,] pos = { { 7, 2 }, { 8, 1 }, { 8, 2 }, { 8, 3 }, { 9, 1 }, { 9, 2 }, { 9, 4 } };
            u.x = pos[i, 0];
            u.y = pos[i, 1];
            enemyUnits.Add(u);
        }

        CreateViews(playerUnits, new Color(0.2f, 0.7f, 1f));
        CreateViews(enemyUnits, new Color(0.95f, 0.35f, 0.4f));
    }

    private void RunOneTurn()
    {
        var order = new List<Unit>();
        order.AddRange(playerUnits.FindAll(u => u.Alive));
        order.AddRange(enemyUnits.FindAll(u => u.Alive));
        order.Sort((a, b) => b.spd.CompareTo(a.spd));
        if (order.Count == 0) return;

        var actor = order[turnIndex % order.Count];
        turnIndex++;
        if (!actor.Alive) return;

        var targets = actor.player ? enemyUnits : playerUnits;
        var target = NearestAlive(actor, targets);
        if (target == null) return;

        int dist = Mathf.Abs(actor.x - target.x) + Mathf.Abs(actor.y - target.y);
        int dmg = EffectiveAtk(actor);

        if (dist <= actor.range)
        {
            // 马：突击伤害
            if (actor.name.Contains("马")) dmg += 3;

            // 士：有概率连击
            bool chain = actor.name.Contains("士") && Random.value < 0.35f;

            int real = ApplyDamageWithTraits(actor, target, dmg);
            if (actor.range > 1)
            {
                SpawnProjectile(actor, target, new Color(1f, 0.75f, 0.2f));
                battleLog = $"{actor.name} 远程攻击 {target.name} -{real}";
            }
            else
            {
                SpawnHitFlash(target, new Color(1f, 0.2f, 0.2f));
                battleLog = $"{actor.name} 近战攻击 {target.name} -{real}";
            }

            if (chain && target.Alive)
            {
                int real2 = ApplyDamageWithTraits(actor, target, Mathf.Max(1, dmg / 2));
                battleLog += $" | 连击 -{real2}";
            }

            // 象：溅射（相邻敌人）
            if (actor.name.Contains("象"))
            {
                var splashTargets = actor.player ? enemyUnits : playerUnits;
                foreach (var t in splashTargets)
                {
                    if (!t.Alive || t == target) continue;
                    int ad = Mathf.Abs(t.x - target.x) + Mathf.Abs(t.y - target.y);
                    if (ad == 1)
                    {
                        ApplyDamageWithTraits(actor, t, Mathf.Max(1, dmg / 2));
                        SpawnHitFlash(t, new Color(1f, 0.5f, 0.2f));
                    }
                }
            }
        }
        else
        {
            StepTowardByTrait(actor, target);
            battleLog = $"{actor.name} 向 {target.name} 移动";
        }
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
        // 士：闪避
        if (to.name.Contains("士") && Random.value < 0.25f) return 0;

        int dmg = raw;
        // 象：减伤
        if (to.name.Contains("象")) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.7f));
        // 帅：减伤
        if (to.name.Contains("帅")) dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.85f));

        to.hp -= dmg;
        return dmg;
    }

    private void StepTowardByTrait(Unit a, Unit b)
    {
        int step = 1;
        if (a.name.Contains("马")) step = 2; // 马：更机动

        for (int i = 0; i < step; i++)
        {
            int nx = a.x;
            int ny = a.y;

            if (a.name.Contains("士"))
            {
                // 士：偏斜线机动
                nx += b.x > a.x ? 1 : -1;
                ny += b.y > a.y ? 1 : -1;
            }
            else if (a.name.Contains("象"))
            {
                // 象：大步走（2格），取近似
                if (Mathf.Abs(a.x - b.x) >= Mathf.Abs(a.y - b.y)) nx += (b.x > a.x ? 1 : -1);
                else ny += (b.y > a.y ? 1 : -1);
            }
            else
            {
                if (Mathf.Abs(a.x - b.x) >= Mathf.Abs(a.y - b.y)) nx += b.x > a.x ? 1 : -1;
                else ny += b.y > a.y ? 1 : -1;
            }

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

        battleStarted = false;
        state = RunState.Reward;
        if (eDead)
        {
            gold += 5; // 过关奖励
            battleLog = "你赢了这一关！+5金币";
        }
        else
        {
            battleLog = "你失败了，调整阵容再来";
        }
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
            var icon = PickIcon(u.name);
            r.material = CreateRuntimeMaterial(icon, c);

            var t = new GameObject("Label");
            t.transform.position = go.transform.position + new Vector3(0, 0.58f, 0);
            var tm = t.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.12f;
            tm.fontSize = 36;
            tm.color = Color.white;
            tm.text = $"{u.name}{u.star}★\nHP:{u.hp}";

            views.Add(new UnitView { unit = u, go = go, text = tm });
        }
    }

    private void RefreshViews()
    {
        foreach (var v in views)
        {
            if (v.unit.Alive)
            {
                v.go.SetActive(true);
                v.text.gameObject.SetActive(true);
                v.go.transform.position = GridToWorld(v.unit.x, v.unit.y);
                v.text.transform.position = v.go.transform.position + new Vector3(0, 0.58f, 0);
                v.text.text = $"{v.unit.name}{v.unit.star}★\nHP:{Mathf.Max(0, v.unit.hp)}";
            }
            else
            {
                v.go.SetActive(false);
                v.text.gameObject.SetActive(false);
            }
        }
    }

    private void ClearViews()
    {
        foreach (var v in views)
        {
            if (v.go) Destroy(v.go);
            if (v.text) Destroy(v.text.gameObject);
        }
        views.Clear();

        foreach (var c in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (c.name == "Cell") Destroy(c);
        }
    }

    private Texture2D PickIcon(string n)
    {
        if (n.Contains("帅") || n.Contains("炎魔")) return dragonIcon ?? shieldIcon;
        if (n.Contains("马")) return horseIcon;
        if (n.Contains("炮")) return bombIcon;
        if (n.Contains("车") || n.Contains("卒") || n.Contains("剑")) return swordIcon;
        return shieldIcon;
    }

    private void SpawnProjectile(Unit from, Unit to, Color color)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        p.name = "FX_Projectile";
        p.transform.localScale = Vector3.one * 0.18f;
        p.transform.position = GridToWorld(from.x, from.y) + new Vector3(0, 0, -0.2f);
        var r = p.GetComponent<Renderer>();
        r.material = CreateRuntimeMaterial(null, color);
        StartCoroutine(MoveFx(p, GridToWorld(to.x, to.y) + new Vector3(0, 0, -0.2f), 0.14f, true));
    }

    private void SpawnHitFlash(Unit target, Color color)
    {
        var fx = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fx.name = "FX_Hit";
        fx.transform.position = GridToWorld(target.x, target.y) + new Vector3(0, 0, -0.25f);
        fx.transform.localScale = Vector3.one * 0.6f;
        var r = fx.GetComponent<Renderer>();
        r.material = CreateRuntimeMaterial(null, color);
        StartCoroutine(DestroyFx(fx, 0.1f));
    }

    private System.Collections.IEnumerator MoveFx(GameObject go, Vector3 end, float t, bool burstAtEnd)
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
        if (burstAtEnd)
        {
            var burst = GameObject.CreatePrimitive(PrimitiveType.Quad);
            burst.name = "FX_Burst";
            burst.transform.position = end;
            burst.transform.localScale = Vector3.one * 0.45f;
            var r = burst.GetComponent<Renderer>();
            r.material = CreateRuntimeMaterial(null, new Color(1f, 0.35f, 0.2f));
            StartCoroutine(DestroyFx(burst, 0.08f));
        }
        Destroy(go);
    }

    private System.Collections.IEnumerator DestroyFx(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) Destroy(go);
    }

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(-4.5f + x, -2.5f + y, 0);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 12, 520, 95), $"龙棋传说（小丑牌式框架）\n第{floor}关 | 当前阶段：7兵种基础版（无变体）\n{battleLog}");

        if (state == RunState.Map)
        {
            GUI.Box(new Rect(16, 110, 360, 120), "路线选择（简化）\n[准备阶段] -> [普通战斗] -> [奖励]");
            if (GUI.Button(new Rect(30, 185, 140, 30), "进入准备"))
            {
                InitPreparation();
            }
        }

        if (state == RunState.Prepare)
        {
            float cellW = 70f;
            float cellH = 55f;
            int rows = 4;
            int cols = 7;

            // 上方：战场
            float boardX = 32f;
            float boardY = 136f;
            GUI.Box(new Rect(16, 110, 520, 255), "战场（拖拽布阵）");

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
                        if (deploySlots[i].x == c && deploySlots[i].y == r)
                        {
                            placed = deploySlots[i];
                            placedIdx = i;
                            break;
                        }
                    }

                    if (placed != null)
                    {
                        if (GUI.Button(new Rect(gx, gy, cellW - 4, cellH - 4), $"{placed.name}\n{placed.star}★"))
                        {
                            selectedDeploy = placedIdx;
                        }
                    }
                    else
                    {
                        GUI.Box(new Rect(gx, gy, cellW - 4, cellH - 4), "");
                    }
                }
            }

            // 下方：商店 + 备战席（类似金铲铲底部操作区）
            float panelX = 16f;
            float panelY = Screen.height - 170f;
            float panelW = Screen.width - 32f;
            GUI.Box(new Rect(panelX, panelY, panelW, 154f), $"准备阶段 | 金币: {gold} | 回合:{floor}");

            GUI.Label(new Rect(panelX + 16, panelY + 8, 120, 20), "商店");
            for (int i = 0; i < shopOffers.Count; i++)
            {
                string n = shopOffers[i];
                int cost = piecePrice.ContainsKey(n) ? piecePrice[n] : 3;
                if (GUI.Button(new Rect(panelX + 16 + i * 90, panelY + 28, 85, 45), $"{n}\n{cost}金")) BuyOffer(i);
            }
            if (GUI.Button(new Rect(panelX + 16 + 5 * 90 + 10, panelY + 36, 120, 28), "刷新(-1)")) RefreshShop();

            GUI.Label(new Rect(panelX + 16, panelY + 70, 120, 20), "备战席");
            for (int i = 0; i < 8; i++)
            {
                float bx = panelX + 16 + i * 70;
                if (i < benchUnits.Count)
                {
                    var u = benchUnits[i];
                    if (GUI.Button(new Rect(bx, panelY + 88, 65, 45), $"{u.name}\n{u.star}★")) selectedBench = i;
                }
                else
                {
                    GUI.Box(new Rect(bx, panelY + 88, 65, 45), "");
                }
            }

            if (GUI.Button(new Rect(panelX + panelW - 150, panelY + 104, 130, 35), "开始战斗"))
            {
                StartFirstBattle();
            }

            GUI.Label(new Rect(panelX + 560, panelY + 28, panelW - 720, 70), "操作：\n1) 商店购买到备战席\n2) 从备战席拖到上方战场\n3) 战场内可拖拽换位");
        }

        if (state == RunState.Battle)
        {
            GUI.Box(new Rect(16, 110, 360, 90), $"战斗中：自动回合对打\n战斗速度：{speedLevel}x");
            if (GUI.Button(new Rect(30, 170, 120, 24), "切换加速"))
            {
                if (speedLevel == 1) speedLevel = 2;
                else if (speedLevel == 2) speedLevel = 4;
                else speedLevel = 1;
            }
        }

        if (state == RunState.Reward)
        {
            GUI.Box(new Rect(16, 110, 360, 120), "第一关结束\n可扩展：三选一奖励（加棋子/加属性/加金币）");
            if (GUI.Button(new Rect(30, 185, 140, 30), "回到地图"))
            {
                state = RunState.Map;
                floor = 2;
                ClearViews();
                battleLog = "已完成第一关";
            }
        }
    }
}
