using System.Collections.Generic;
using UnityEngine;

public class RoguelikeFramework : MonoBehaviour
{
    private enum RunState { Map, Battle, Reward }
    private RunState state = RunState.Map;

    private int floor = 1;
    private bool battleStarted;
    private float turnTimer;
    private readonly float turnInterval = 0.55f;
    private int turnIndex;
    private string battleLog = "点击【进入第一关】开始对战";

    private readonly List<Unit> playerUnits = new();
    private readonly List<Unit> enemyUnits = new();
    private readonly List<UnitView> views = new();

    private Texture2D bgTex;
    private Texture2D tileATex;
    private Texture2D tileBTex;
    private Texture2D dragonIcon;
    private Texture2D horseIcon;
    private Texture2D swordIcon;
    private Texture2D bombIcon;
    private Texture2D shieldIcon;

    private const int W = 8;
    private const int H = 5;

    private class Unit
    {
        public string name;
        public int hp;
        public int atk;
        public int spd;
        public int x;
        public int y;
        public bool player;
        public bool Alive => hp > 0;
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
        r.material.color = new Color(0.12f, 0.1f, 0.14f);
        if (bgTex != null) r.material.mainTexture = bgTex;
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
        if (state != RunState.Battle || !battleStarted) return;

        turnTimer += Time.deltaTime;
        if (turnTimer < turnInterval) return;
        turnTimer = 0;

        RunOneTurn();
        RefreshViews();
        CheckBattleEnd();
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

        playerUnits.Add(new Unit { name = "帅-圣", hp = 36, atk = 9, spd = 7, x = 0, y = 2, player = true });
        playerUnits.Add(new Unit { name = "马-梦", hp = 24, atk = 10, spd = 10, x = 0, y = 1, player = true });
        playerUnits.Add(new Unit { name = "炮-火", hp = 22, atk = 11, spd = 8, x = 0, y = 3, player = true });

        enemyUnits.Add(new Unit { name = "炎魔帅", hp = 34, atk = 9, spd = 7, x = 7, y = 2, player = false });
        enemyUnits.Add(new Unit { name = "雷鸣车", hp = 28, atk = 8, spd = 8, x = 7, y = 1, player = false });
        enemyUnits.Add(new Unit { name = "卒", hp = 20, atk = 7, spd = 9, x = 7, y = 3, player = false });

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
        if (dist <= 1)
        {
            target.hp -= actor.atk;
            battleLog = $"{actor.name} 攻击 {target.name} -{actor.atk}";
        }
        else
        {
            StepToward(actor, target);
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

    private void StepToward(Unit a, Unit b)
    {
        int nx = a.x;
        int ny = a.y;

        if (Mathf.Abs(a.x - b.x) >= Mathf.Abs(a.y - b.y))
            nx += b.x > a.x ? 1 : -1;
        else
            ny += b.y > a.y ? 1 : -1;

        nx = Mathf.Clamp(nx, 0, W - 1);
        ny = Mathf.Clamp(ny, 0, H - 1);

        if (!Occupied(nx, ny))
        {
            a.x = nx;
            a.y = ny;
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
        battleLog = eDead ? "你赢了第一关！" : "你失败了，重开再来";
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
                r.material.color = dark ? new Color(0.18f, 0.18f, 0.24f) : new Color(0.24f, 0.24f, 0.3f);
                if (dark && tileATex != null) r.material.mainTexture = tileATex;
                if (!dark && tileBTex != null) r.material.mainTexture = tileBTex;
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
            r.material.color = c;
            var icon = PickIcon(u.name);
            if (icon != null) r.material.mainTexture = icon;

            var t = new GameObject("Label");
            t.transform.position = go.transform.position + new Vector3(0, 0.58f, 0);
            var tm = t.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.12f;
            tm.fontSize = 36;
            tm.color = Color.white;
            tm.text = $"{u.name}\nHP:{u.hp}";

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
                v.text.text = $"{v.unit.name}\nHP:{Mathf.Max(0, v.unit.hp)}";
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

    private Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(-3.5f + x, -2f + y, 0);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 12, 440, 90), $"龙棋传说（小丑牌式框架）\n第{floor}关\n{battleLog}");

        if (state == RunState.Map)
        {
            GUI.Box(new Rect(16, 110, 360, 120), "路线选择（简化）\n[普通战斗] -> [奖励] -> [Boss]");
            if (GUI.Button(new Rect(30, 185, 140, 30), "进入第一关"))
            {
                StartFirstBattle();
            }
        }

        if (state == RunState.Battle)
        {
            GUI.Box(new Rect(16, 110, 300, 70), "战斗中：自动回合对打");
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
