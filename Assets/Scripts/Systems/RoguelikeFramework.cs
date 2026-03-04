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

    private enum RunState { Map, Battle, Reward }
    private RunState state = RunState.Map;

    private int floor = 1;
    private bool battleStarted;
    private float turnTimer;
    private readonly float baseTurnInterval = 0.55f;
    private int turnIndex;
    private int speedLevel = 1; // 1x / 2x / 4x
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

    private const int W = 10;
    private const int H = 6;

    private class Unit
    {
        public string name;
        public int hp;
        public int atk;
        public int spd;
        public int range = 1; // 近战=1，远程>1
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
        if (state != RunState.Battle || !battleStarted) return;

        float turnInterval = baseTurnInterval / speedLevel;
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

        // 第一步：7个兵种全部上场（先不做变体）
        playerUnits.Add(new Unit { name = "帅", hp = 40, atk = 8, spd = 6, range = 1, x = 0, y = 2, player = true });
        playerUnits.Add(new Unit { name = "车", hp = 34, atk = 9, spd = 7, range = 1, x = 1, y = 1, player = true });
        playerUnits.Add(new Unit { name = "马", hp = 28, atk = 10, spd = 10, range = 1, x = 1, y = 3, player = true });
        playerUnits.Add(new Unit { name = "炮", hp = 24, atk = 10, spd = 8, range = 3, x = 1, y = 2, player = true });
        playerUnits.Add(new Unit { name = "象", hp = 36, atk = 7, spd = 5, range = 1, x = 0, y = 4, player = true });
        playerUnits.Add(new Unit { name = "士", hp = 26, atk = 8, spd = 9, range = 1, x = 0, y = 1, player = true });
        playerUnits.Add(new Unit { name = "兵", hp = 22, atk = 7, spd = 8, range = 1, x = 2, y = 2, player = true });

        enemyUnits.Add(new Unit { name = "帅", hp = 40, atk = 8, spd = 6, range = 1, x = 9, y = 2, player = false });
        enemyUnits.Add(new Unit { name = "车", hp = 34, atk = 9, spd = 7, range = 1, x = 8, y = 1, player = false });
        enemyUnits.Add(new Unit { name = "马", hp = 28, atk = 10, spd = 10, range = 1, x = 8, y = 3, player = false });
        enemyUnits.Add(new Unit { name = "炮", hp = 24, atk = 10, spd = 8, range = 3, x = 8, y = 2, player = false });
        enemyUnits.Add(new Unit { name = "象", hp = 36, atk = 7, spd = 5, range = 1, x = 9, y = 4, player = false });
        enemyUnits.Add(new Unit { name = "士", hp = 26, atk = 8, spd = 9, range = 1, x = 9, y = 1, player = false });
        enemyUnits.Add(new Unit { name = "兵", hp = 22, atk = 7, spd = 8, range = 1, x = 7, y = 2, player = false });

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
            GUI.Box(new Rect(16, 110, 360, 120), "路线选择（简化）\n[普通战斗] -> [奖励] -> [Boss]");
            if (GUI.Button(new Rect(30, 185, 140, 30), "进入第一关"))
            {
                StartFirstBattle();
            }
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
