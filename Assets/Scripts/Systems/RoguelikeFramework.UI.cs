using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
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
        float s = Mathf.Max(0.01f, uiScale);
        float guiW = Screen.width / s;
        float guiH = Screen.height / s;
        float x = Mathf.Clamp(tooltipPos.x / s, 10f, guiW - w - 10f);
        float y = Mathf.Clamp(tooltipPos.y / s, 10f, guiH - h - 10f);

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

        float panelGap = 12f;
        float panelW = (w - 32f) * 0.5f;
        float panelH = h - 50f;
        float rowH = 24f;
        float gap = 6f;
        float barMax = panelW - 176f;

        void DrawTeam(string title, List<Unit> team, float px, float py)
        {
            GUI.Box(new Rect(px, py, panelW, panelH), title);
            var sorted = new List<Unit>(team);
            sorted.Sort((a, b) => b.damageDealt.CompareTo(a.damageDealt));
            int topDmg = sorted.Count > 0 ? sorted[0].damageDealt : 0;

            float ry = py + 24f;
            int maxRows = Mathf.FloorToInt((panelH - 30f) / (rowH + gap));
            for (int i = 0; i < sorted.Count && i < maxRows; i++)
            {
                var u = sorted[i];
                string nm = u.damageDealt == topDmg && topDmg > 0 ? $"★{u.Name}" : u.Name;
                GUI.Label(new Rect(px + 8, ry, 72, rowH), nm);
                float dealtW = barMax * (u.damageDealt / (float)maxVal);
                float takenW = barMax * (u.damageTaken / (float)maxVal);

                Color old = GUI.color;
                GUI.color = new Color(0.25f, 0.72f, 1f, 0.95f);
                GUI.DrawTexture(new Rect(px + 82, ry + 2, dealtW, rowH - 4), Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.62f, 0.2f, 0.95f);
                GUI.DrawTexture(new Rect(px + 82, ry + rowH * 0.58f, takenW, rowH * 0.34f), Texture2D.whiteTexture);
                GUI.color = old;

                GUI.Label(new Rect(px + 88 + barMax, ry, 84, rowH), $"{u.damageDealt}/{u.damageTaken}");
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
            StageType.Shop => "商店",
            StageType.Mystery => "问号",
            StageType.Treasure => "宝箱",
            StageType.Boss => "Boss",
            _ => "?"
        };
    }

    private void DrawSynergyClickPanel(float x, float y, float w, float h, List<Unit> team)
    {
        if (team == null || team.Count == 0) return;

        GUI.Box(new Rect(x, y, w, h), "羁绊面板（点击查看效果/棋子池）");

        // 仿金铲铲：显示当前场上可激活的职业/阵营，并支持点击查看详情
        var classes = new List<string>();
        var origins = new List<string>();
        foreach (var u in team)
        {
            if (string.IsNullOrEmpty(u.ClassTag)) continue;
            if (!classes.Contains(u.ClassTag)) classes.Add(u.ClassTag);
            if (!string.IsNullOrEmpty(u.OriginTag) && !origins.Contains(u.OriginTag)) origins.Add(u.OriginTag);
        }
        if (classes.Count == 0 && origins.Count == 0) return;

        float bx = x + 10f;
        float by = y + 26f;
        bool compactTrait = w < 360f || h < 210f;
        float cardW = compactTrait ? 118f : 132f;
        float cardH = compactTrait ? 56f : 66f;
        int cols = Mathf.Max(1, Mathf.FloorToInt((w - 20f) / (cardW + 6f)));
        Vector2 mp = Event.current.mousePosition;
        int idx = 0;
        void DrawTraitCard(string key, bool isClass)
        {
            int c = isClass ? CountClass(team, key) : CountOrigin(team, key);
            bool active2 = c >= 2;
            bool active4 = c >= 4;
            int row = idx / cols;
            int col = idx % cols;
            idx++;

            Rect card = new Rect(bx + col * (cardW + 6f), by + row * (cardH + 6f), cardW, cardH);

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

            string title = isClass ? GetClassCn(key) : GetOriginCn(key);
            string status = active4 ? "★★ 4阶激活" : active2 ? "★ 2阶激活" : "未激活";
            GUI.Label(new Rect(card.x + 8, card.y + 4, card.width - 16, 18), $"{title}");
            GUI.Label(new Rect(card.x + 8, card.y + 24, card.width - 16, 18), $"数量: {c}  (2/4)");
            GUI.Label(new Rect(card.x + 8, card.y + 42, card.width - 16, 18), status);

            if (active2)
            {
                GUI.Label(new Rect(card.x + card.width - 52, card.y + 4, 44, 18), active4 ? "MAX" : "ON");
            }

            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
            {
                selectedSynergyKey = key;
                string info = isClass
                    ? GetSynergyEffectDesc(key, c) + "\n包含棋子：" + GetUnitsOfClassText(key)
                    : GetOriginEffectDesc(key, c) + "\n包含棋子：" + GetUnitsOfOriginText(key);
                ShowTooltip(info);
            }
        };

        int maxRows = Mathf.Max(1, Mathf.FloorToInt((h - 34f) / (cardH + 6f)));
        int maxCards = cols * maxRows;
        int shown = 0;
        for (int i = 0; i < classes.Count && shown < maxCards; i++, shown++) DrawTraitCard(classes[i], true);
        for (int i = 0; i < origins.Count && shown < maxCards; i++, shown++) DrawTraitCard(origins[i], false);

        float legendY = by + Mathf.Ceil(idx / (float)cols) * (cardH + 6f) + 2f;
        if (legendY < y + h - 18f)
        {
            GUI.Label(new Rect(x + 12, legendY, w - 24, 20), "颜色：灰=未激活 蓝=2阶 金=4阶");
        }
    }

    private void DrawSelectedHexPanel(float x, float y, float w, float h)
    {
        GUI.Box(new Rect(x, y, w, h), "");
        GUI.Label(new Rect(x + 12, y + 6, 220, 20), "已选海克斯（点击查看效果）");
        if (selectedHexes.Count == 0)
        {
            GUI.Label(new Rect(x + 12, y + 30, w - 24, 24), "暂无");
            return;
        }

        float chipW = 132f;
        float chipH = 30f;
        int cols = Mathf.Max(1, Mathf.FloorToInt((w - 24f) / (chipW + 8f)));
        for (int i = 0; i < selectedHexes.Count; i++)
        {
            var hx = selectedHexes[i];
            int row = i / cols;
            int col = i % cols;
            float bx = x + 12f + col * (chipW + 8f);
            float by = y + 30f + row * (chipH + 6f);
            string txt = hx.name;
            if (GUI.Button(new Rect(bx, by, chipW, chipH), txt))
            {
                ShowTooltip($"{hx.name} [{hx.rarity}]\n{hx.desc}");
                battleLog = $"查看海克斯：{hx.name}";
            }
        }
    }

    private void DrawCompPanel(float x, float y, float w, float h, List<Unit> team)
    {
        string lockText = string.IsNullOrEmpty(lockedCompId) ? "未锁定路线" : $"已锁定：{GetLockedComp()?.name ?? "未知"}";
        float panelH = showCompPanelFoldout ? h : 52f;
        GUI.Box(new Rect(x, y, w, panelH), $"阵容路线（{lockText}）");
        if (GUI.Button(new Rect(x + w - 132, y + 4, 122, 22), showCompPanelFoldout ? "折叠路线" : "展开路线"))
        {
            showCompPanelFoldout = !showCompPanelFoldout;
        }
        if (!showCompPanelFoldout)
        {
            GUI.Label(new Rect(x + 12, y + 28, w - 150, 18), "路线面板已折叠。点击右侧按钮查看与锁定阵容。");
            return;
        }

        if (GUI.Button(new Rect(x + w - 274, y + 4, 132, 22), "推荐并锁定"))
        {
            RecommendCompByBoard(team);
        }

        float cardW = (w - 30f) * 0.5f;
        float cardH = 64f;
        for (int i = 0; i < compDefs.Count; i++)
        {
            int col = i % 2;
            int row = i / 2;
            float cx = x + 10 + col * (cardW + 10f);
            float cy = y + 28 + row * (cardH + 8f);
            var c = compDefs[i];
            bool activeLock = lockedCompId == c.id;
            float p = GetCompProgress(c, team);
            bool done = IsCompActive(c, team);

            Color old = GUI.color;
            GUI.color = activeLock ? new Color(0.2f, 0.5f, 0.85f, 0.95f) : new Color(0.16f, 0.2f, 0.28f, 0.92f);
            GUI.DrawTexture(new Rect(cx, cy, cardW, cardH), Texture2D.whiteTexture);
            GUI.color = new Color(0.07f, 0.1f, 0.16f, 0.96f);
            GUI.DrawTexture(new Rect(cx + 2, cy + 2, cardW - 4, cardH - 4), Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(new Rect(cx + 8, cy + 6, cardW - 102, 18), $"{c.name} {(activeLock ? "[已锁定]" : "")}");
            GUI.Label(new Rect(cx + 8, cy + 24, cardW - 16, 18), $"进度 {(p * 100f):0}% | 条件 {GetClassCn(c.classA)}{c.needClass2A}/{GetClassCn(c.classB)}{c.needClass2B}");
            GUI.Label(new Rect(cx + 8, cy + 42, cardW - 16, 18), done ? "状态: 已成型（战斗额外加成生效）" : "状态: 成型中");

            if (GUI.Button(new Rect(cx + cardW - 92, cy + 4, 84, 22), activeLock ? "取消锁定" : "锁定路线"))
            {
                lockedCompId = activeLock ? "" : c.id;
                battleLog = activeLock ? $"已取消路线：{c.name}" : $"已锁定路线：{c.name}";
            }
        }
    }

    private void DrawShopHexPanel(float x, float y, float w, float h)
    {
        GUI.Box(new Rect(x, y, w, h), "商店奇物");
        GUI.Label(new Rect(x + 12, y + 8, w - 24, 18), "商店节点可直接购买海克斯与构筑奇物");
        if (GUI.Button(new Rect(x + w - 136, y + 6, 124, 24), "刷新奇物(-2)"))
        {
            if (gold >= 2)
            {
                gold -= 2;
                RollShopHexOffers();
            }
            else battleLog = "金币不足，无法刷新商店奇物";
        }

        if (currentShopHexOffers.Count == 0)
        {
            GUI.Label(new Rect(x + 12, y + 40, w - 24, 22), "暂无奇物可购买");
            return;
        }

        float cardH = 92f;
        for (int i = 0; i < currentShopHexOffers.Count; i++)
        {
            var hOffer = currentShopHexOffers[i];
            int cost = i < currentShopHexCosts.Count ? currentShopHexCosts[i] : 0;
            float cy = y + 38f + i * (cardH + 8f);
            Rect card = new Rect(x + 10f, cy, w - 20f, cardH);

            Color border = hOffer.rarity switch
            {
                "彩" => new Color(1f, 0.72f, 0.26f, 0.96f),
                "金" => new Color(0.9f, 0.76f, 0.28f, 0.96f),
                "蓝" => new Color(0.35f, 0.75f, 1f, 0.96f),
                _ => new Color(0.82f, 0.84f, 0.88f, 0.96f)
            };

            Color old = GUI.color;
            GUI.color = border;
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);
            GUI.DrawTexture(new Rect(card.x + 2, card.y + 2, card.width - 4, card.height - 4), Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(new Rect(card.x + 10, card.y + 8, card.width - 110, 18), $"[{hOffer.rarity}] {hOffer.name}");
            GUI.Label(new Rect(card.x + 10, card.y + 30, card.width - 118, 42), hOffer.desc, wrapLabelStyle);
            if (GUI.Button(new Rect(card.x + card.width - 96, card.y + 28, 84, 30), $"购买({cost})")) BuyShopHex(i);
        }
    }

    private void RecommendCompByBoard(List<Unit> team)
    {
        if (compDefs.Count == 0) return;
        int best = 0;
        int bestScore = int.MinValue;
        for (int i = 0; i < compDefs.Count; i++)
        {
            var c = compDefs[i];
            int score = 0;
            int ca = CountClass(team, c.classA);
            int cb = CountClass(team, c.classB);
            score += ca * 20 + cb * 20;
            for (int t = 0; t < team.Count; t++)
            {
                var u = team[t];
                if (u == null || u.def == null) continue;
                for (int k = 0; k < c.focusClasses.Length; k++) if (u.ClassTag == c.focusClasses[k]) score += 10;
                for (int k = 0; k < c.focusOrigins.Length; k++) if (u.OriginTag == c.focusOrigins[k]) score += 4;
            }
            if (playerLevel <= 3 && c.id == "double4") score -= 30;
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }
        lockedCompId = compDefs[best].id;
        battleLog = $"推荐阵容：{compDefs[best].name}";
        PushEvent($"系统推荐路线：{compDefs[best].name}");
    }

    private void AutoArrangeByLockedComp()
    {
        var lc = GetLockedComp();
        if (lc == null)
        {
            battleLog = "请先在“阵容路线”里手动锁定路线，再使用一键自动布阵";
            PushEvent("未锁定路线：自动布阵已取消");
            return;
        }

        var all = new List<Unit>();
        all.AddRange(deploySlots);
        all.AddRange(benchUnits);
        if (all.Count == 0) return;

        all.Sort((a, b) =>
        {
            int sa = 0;
            int sb = 0;
            for (int i = 0; i < lc.focusClasses.Length; i++)
            {
                if (a.ClassTag == lc.focusClasses[i]) sa += 80;
                if (b.ClassTag == lc.focusClasses[i]) sb += 80;
            }
            for (int i = 0; i < lc.focusOrigins.Length; i++)
            {
                if (a.OriginTag == lc.focusOrigins[i]) sa += 26;
                if (b.OriginTag == lc.focusOrigins[i]) sb += 26;
            }
            sa += a.star * 20 + a.atk + a.maxHp / 6;
            sb += b.star * 20 + b.atk + b.maxHp / 6;
            return sb.CompareTo(sa);
        });

        deploySlots.Clear();
        benchUnits.Clear();
        int cap = GetBoardCap();
        int fx = 0, mx = 0, bx = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var u = all[i];
            if (deploySlots.Count < cap)
            {
                if (u.ClassTag == "Vanguard" || u.ClassTag == "Guardian") { u.x = Mathf.Clamp(fx++, 0, 4); u.y = 1; }
                else if (u.ClassTag == "Artillery" || u.ClassTag == "Controller" || u.ClassTag == "Medic") { u.x = Mathf.Clamp(bx++, 0, 4); u.y = 3; }
                else { u.x = Mathf.Clamp(mx++, 0, 4); u.y = 2; }
                deploySlots.Add(u);
            }
            else
            {
                u.x = -1; u.y = -1;
                if (benchUnits.Count < 8) benchUnits.Add(u);
            }
        }
        RedrawPrepareBoard();
        battleLog = $"已自动布阵：{lc.name}";
        PushEvent($"自动布阵完成：{lc.name}");
    }

    private int ScoreHexForLockedComp(HexDef h)
    {
        if (h == null) return 0;
        var lc = GetLockedComp();
        int score = 0;
        if (h.id == "board_plus") score += 30;
        if (h.id == "fast_train") score += 20;
        if (h.id == "rich") score += 16;
        if (h.id == "interest_up") score += 12;
        if (h.id == "team_atk") score += 10;
        if (h.id == "lifesteal_core") score += 15;
        if (h.id == "execution_edge") score += 12;
        if (lc == null) return score;
        if (h.id == "cannon_master" && (lc.classA == "Artillery" || lc.classB == "Artillery")) score += 22;
        if (h.id == "artillery_range" && (lc.classA == "Artillery" || lc.classB == "Artillery")) score += 18;
        if (h.id == "vanguard_wall" && (lc.classA == "Vanguard" || lc.classB == "Vanguard")) score += 18;
        if (h.id == "rider_charge" && (lc.classA == "Rider" || lc.classB == "Rider")) score += 18;
        if (h.id == "assassin_bloom" && (lc.classA == "Assassin" || lc.classB == "Assassin")) score += 24;
        return score;
    }

    private void DrawHudInfoStrip(Rect rect, string text, TextAnchor align = TextAnchor.MiddleLeft)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.08f, 0.14f, 0.22f, 0.94f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.2f, 0.34f, 0.54f, 0.95f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.color = old;

        var style = new GUIStyle(hudStatStyle);
        style.fontSize = 14;
        style.alignment = align;
        GUI.Label(new Rect(rect.x + 10f, rect.y + 1f, rect.width - 20f, rect.height - 2f), text, style);
    }

    private void DrawShopOddsPanel(Rect rect)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.08f, 0.14f, 0.22f, 0.96f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.22f, 0.38f, 0.58f, 0.95f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
        GUI.color = old;

        GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, 70f, 18f), "刷新概率", chipMetaStyle);

        var odds = GetShopCostOddsByLevel();
        float innerX = rect.x + 82f;
        float chipGap = 8f;
        float chipW = (rect.width - 92f - chipGap * 4f) / 5f;
        for (int cost = 1; cost <= 5; cost++)
        {
            Rect chipRect = new Rect(innerX + (cost - 1) * (chipW + chipGap), rect.y + 4f, chipW, rect.height - 8f);
            Color chipColor = GetUnitChipColorByCost(cost);
            Color fill = Color.Lerp(new Color(0.08f, 0.12f, 0.18f, 0.98f), chipColor, 0.28f);

            GUI.color = chipColor;
            GUI.DrawTexture(chipRect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(chipRect.x + 1.5f, chipRect.y + 1.5f, chipRect.width - 3f, chipRect.height - 3f), Texture2D.whiteTexture);
            GUI.color = old;

            var style = new GUIStyle(hudStatStyle);
            style.fontSize = 14;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(chipRect, $"{cost}费 {odds[cost] * 100f:0}%", style);
        }
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
        titleStyle = new GUIStyle(GUI.skin.label);
        wrapLabelStyle = new GUIStyle(GUI.skin.label);
        hudStatStyle = new GUIStyle(GUI.skin.label);
        chipTitleStyle = new GUIStyle(GUI.skin.label);
        chipMetaStyle = new GUIStyle(GUI.skin.label);

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
        boxStyle.fontSize = 16;

        // 交互按钮统一使用深色底，保障所有文案可读性
        buttonStyle.normal.background = flatButtonTex;
        buttonStyle.hover.background = flatButtonHoverTex;
        buttonStyle.active.background = flatButtonActiveTex;
        buttonStyle.focused.background = flatButtonHoverTex;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.fontSize = 16;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = new Color(0.95f, 0.98f, 1f);
        buttonStyle.padding = new RectOffset(8, 8, 6, 6);
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        labelStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);
        labelStyle.hover.textColor = labelStyle.normal.textColor;
        labelStyle.active.textColor = labelStyle.normal.textColor;
        labelStyle.focused.textColor = labelStyle.normal.textColor;
        labelStyle.fontSize = 16;
        labelStyle.wordWrap = false;
        labelStyle.richText = true;
        labelStyle.clipping = TextClipping.Overflow;

        wrapLabelStyle.normal.textColor = labelStyle.normal.textColor;
        wrapLabelStyle.hover.textColor = labelStyle.normal.textColor;
        wrapLabelStyle.active.textColor = labelStyle.normal.textColor;
        wrapLabelStyle.focused.textColor = labelStyle.normal.textColor;
        wrapLabelStyle.fontSize = 16;
        wrapLabelStyle.wordWrap = true;
        wrapLabelStyle.richText = true;
        wrapLabelStyle.clipping = TextClipping.Overflow;

        hudStatStyle.normal.textColor = new Color(0.98f, 0.98f, 1f);
        hudStatStyle.hover.textColor = hudStatStyle.normal.textColor;
        hudStatStyle.active.textColor = hudStatStyle.normal.textColor;
        hudStatStyle.focused.textColor = hudStatStyle.normal.textColor;
        hudStatStyle.fontSize = 20;
        hudStatStyle.fontStyle = FontStyle.Bold;
        hudStatStyle.alignment = TextAnchor.MiddleLeft;
        hudStatStyle.richText = true;

        titleStyle.normal.textColor = Color.white;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 22;

        chipTitleStyle.normal.textColor = Color.white;
        chipTitleStyle.fontSize = 14;
        chipTitleStyle.fontStyle = FontStyle.Bold;
        chipTitleStyle.wordWrap = false;
        chipTitleStyle.clipping = TextClipping.Overflow;

        chipMetaStyle.normal.textColor = new Color(0.86f, 0.93f, 1f);
        chipMetaStyle.fontSize = 11;
        chipMetaStyle.wordWrap = false;
        chipMetaStyle.clipping = TextClipping.Overflow;

        stylesReady = true;
    }

    private void DrawUnitChipCard(Rect r, UnitDef def, int star, int cost, bool showCost)
    {
        if (def == null) return;
        bool compact = r.width <= 138f;
        Color old = GUI.color;
        GUI.color = GetUnitChipColorByCost(def.cost);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);
        GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Texture2D.whiteTexture);

        var icon = PickIcon(def);
        float iconW = compact ? 40f : 48f;
        GUI.color = new Color(0.13f, 0.18f, 0.28f, 0.95f);
        GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, iconW, iconW), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (icon != null) GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, iconW, iconW), icon, ScaleMode.ScaleToFit, true);
        else GUI.Label(new Rect(r.x + 12, r.y + 18, iconW - 6, 18), "?", chipTitleStyle);

        GUI.color = Color.white;
        float tx = r.x + iconW + 16f;
        float tw = r.width - iconW - 24f;
        string classText = GetClassCn(def.classTag);
        string originText = GetOriginCn(def.originTag);
        GUI.Label(new Rect(tx, r.y + 6, tw, 18), def.name, chipTitleStyle);
        GUI.Label(new Rect(tx, r.y + 24, tw, 14), classText, chipMetaStyle);
        GUI.Label(new Rect(tx, r.y + 39, tw, 14), originText, chipMetaStyle);
        // 强化星级视觉 (Stage B1)
        Color starColor = star == 3 ? new Color(1f, 0.82f, 0.2f) : (star == 2 ? new Color(0.6f, 0.8f, 1f) : new Color(0.86f, 0.93f, 1f));
        GUI.color = starColor;
        string starStr = star == 3 ? "★★★" : (star == 2 ? "★★" : "★");
        GUI.Label(new Rect(tx, r.y + r.height - 18f, 70, 16), starStr, chipMetaStyle);
        if (showCost)
        {
            GUI.color = new Color(1f, 0.86f, 0.38f, 1f);
            GUI.Label(new Rect(r.x + r.width - 48, r.y + r.height - 18f, 42, 16), $"{cost}金", chipMetaStyle);
        }
        GUI.color = old;
    }

    private bool DrawPrimaryActionButton(Rect r, string text, bool danger = false)
    {
        Color old = GUI.color;
        Color fill = danger
            ? new Color(0.72f, 0.26f, 0.22f, 0.98f)
            : new Color(0.9f, 0.62f, 0.18f, 0.98f);
        Color border = danger
            ? new Color(1f, 0.72f, 0.62f, 0.95f)
            : new Color(1f, 0.9f, 0.55f, 0.95f);

        GUI.color = border;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x, r.y + 1, r.width, r.height), text, buttonStyle);
        GUI.color = old;
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        var oldBox = GUI.skin.box;
        var oldButton = GUI.skin.button;
        var oldLabel = GUI.skin.label;
        var oldColor = GUI.color;
        var oldMatrix = GUI.matrix;
        GUI.skin.box = boxStyle;
        GUI.skin.button = buttonStyle;
        GUI.skin.label = labelStyle;

        // Let 768p-class windows keep enough vertical room for the full bottom HUD.
        uiScale = Mathf.Clamp(Screen.height / 900f, 1f, 2f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
        float guiW = Screen.width / uiScale;
        float guiH = Screen.height / uiScale;
        bool compact = guiW < 1480f;
        // Windowed mode on macOS can overlap IMGUI top rows with title bar.
        float topPad = Screen.fullScreen ? 10f : (Application.platform == RuntimePlatform.OSXPlayer ? 58f : 30f);

        float leftW = compact ? 300f : 350f;
        float rightW = compact ? 360f : 430f;
        float centerX = leftW + 20f;
        float centerW = Mathf.Max(360f, guiW - leftW - rightW - 36f);
        float rightX = guiW - rightW - 16f;

        string topInfo = $"生命:{playerLife}  连胜:{winStreak} 连败:{loseStreak}  地图层:{Mathf.Clamp(stageIndex + 1, 1, GetFinalFloor())}/{GetFinalFloor()}";
        GUI.Box(new Rect(16, 16 + topPad, guiW - 32f, 112f), "");
        GUI.Label(new Rect(30, 24 + topPad, 740, 30), "龙棋传说 | 中国象棋 x 自走棋 x 海克斯构筑", titleStyle);
        GUI.Label(new Rect(30, 50 + topPad, 760, 22), topInfo);
        GUI.Label(new Rect(30, 76 + topPad, guiW - 430f, 38), $"战报：{battleLog}", wrapLabelStyle);
        if (recentEvents.Count > 0)
        {
            GUI.Box(new Rect(16, 130 + topPad, leftW, 92), "战报事件");
            for (int i = 0; i < recentEvents.Count && i < 4; i++)
            {
                GUI.Label(new Rect(26, 146 + topPad + i * 18, 360, 18), recentEvents[recentEvents.Count - 1 - i]);
            }
        }

        string stateText = state switch
        {
            RunState.Stage => "阶段选择",
            RunState.Event => "事件抉择",
            RunState.Prepare => "准备阶段",
            RunState.Battle => "自动战斗",
            RunState.Reward => "战斗结算",
            RunState.Hex => "海克斯选择",
            RunState.GameOver => "章节结束",
            _ => "状态未知"
        };
        GUI.Box(new Rect(rightX, 16 + topPad, rightW, 48), $"当前状态：{stateText}");
        GUI.Box(new Rect(rightX, 70 + topPad, rightW, 46), "情报提示：点击棋子或羁绊卡片查看详细说明");

        if (GUI.Button(new Rect(guiW - 126, 14 + topPad, 108, 28), showDevTools ? "收起开发" : "开发工具"))
        {
            showDevTools = !showDevTools;
        }
            if (showDevTools)
            {
            GUI.Box(new Rect(centerX, 12 + topPad, 300, 96), $"屏幕:{Screen.width}x{Screen.height}\nDPI:{Mathf.RoundToInt(Screen.dpi)}\nPath:{devPersistentPath}\nAutoRun:{devAutoRunStatus}\nConfig:{configValidationStatus}");
            if (GUI.Button(new Rect(centerX + 310f, 14 + topPad, 118, 28), "开发推进一步"))
            {
                DevAdvanceOneStep();
            }
            if (GUI.Button(new Rect(centerX + 310f, 46 + topPad, 118, 28), "开发重开"))
            {
                RestartRun();
            }
            if (GUI.Button(new Rect(centerX + 434f, 14 + topPad, 124, 60), "自动回归3关"))
            {
                DevRunRegression3Floors();
            }
            if (GUI.Button(new Rect(centerX + 434f, 78 + topPad, 124, 28), "平衡测试50轮"))
            {
                DevRunBalanceIterations(50);
            }
            if (GUI.Button(new Rect(centerX + 310f, 78 + topPad, 118, 28), "快速开局(F9)"))
            {
                DevQuickStartToPrepare();
            }
            if (GUI.Button(new Rect(centerX + 560f, 78 + topPad, 124, 28), "平衡测试100轮"))
            {
                DevRunBalanceIterations(100);
            }
            if (GUI.Button(new Rect(centerX + 686f, 78 + topPad, 136, 28), "UI烟雾测试"))
            {
                DevRunUiSmokeTest();
            }
            if (GUI.Button(new Rect(centerX + 560f, 46 + topPad, 262, 28), "配置校验(运行中)"))
            {
                RevalidateConfigData();
            }
            if (GUI.Button(new Rect(centerX + 560f, 14 + topPad, 262, 28), devTurboBattle ? "关闭极速战斗(F8)" : "开启极速战斗(F8)"))
            {
                devTurboBattle = !devTurboBattle;
                speedLevel = devTurboBattle ? 16 : 4;
                battleLog = devTurboBattle ? "开发开关：极速战斗 x16" : "开发开关：恢复战斗速度 x4";
            }
            if (GUI.Button(new Rect(centerX + 686f, 46 + topPad, 136, 28), "跳关胜利(F10)"))
            {
                DevSkipCurrentFloor();
            }
            if (GUI.Button(new Rect(centerX + 686f, 14 + topPad, 136, 28), "直达Boss(F11)"))
            {
                DevSkipToBoss();
            }
            if (GUI.Button(new Rect(centerX + 560f, 110 + topPad, 262, 28), "路线命中压测(F12)"))
            {
                DevRunLockedCompHitProbe(24);
            }
            if (GUI.Button(new Rect(guiW - 126, 46 + topPad, 108, 28), "调试+999金"))
            {
                gold += 999;
                battleLog = "作弊生效：金币 +999";
            }
        }

        DrawSelectedHexPanel(centerX, 130 + topPad, centerW, 104);

        if (state == RunState.Stage)
        {
            var choices = GetAvailableStageNodes();
            if (choices.Count == 0)
            {
                GUI.Box(new Rect(16, 220, 420, 110), "恭喜通关当前章节地图！");
                state = RunState.GameOver;
            }
            else
            {
                GUI.Box(new Rect(16, 220, 320, 124), "地图分支");
                GUI.Label(new Rect(28, 248, 292, 76), "选择下一条路线。\n普通/精英会进入准备与战斗，商店节点可购物并购买奇物海克斯，问号会揭示随机事件。", wrapLabelStyle);

                float mapX = 360f;
                float mapY = 210f;
                float mapW = guiW - mapX - 24f;
                float mapH = guiH - mapY - 34f;
                GUI.Box(new Rect(mapX, mapY, mapW, mapH), "章节地图");

                int maxFloor = GetFinalFloor();
                float laneGap = 88f;
                float floorGap = maxFloor > 1 ? (mapW - 140f) / (maxFloor - 1) : 120f;
                float centerY = mapY + mapH * 0.5f;

                Vector2 NodePos(StageNode node)
                {
                    float x = mapX + 78f + (node.floor - 1) * floorGap;
                    float y = centerY + (node.lane - 1.5f) * laneGap;
                    return new Vector2(x, y);
                }

                for (int i = 0; i < stages.Count; i++)
                {
                    var node = stages[i];
                    Vector2 p1 = NodePos(node);
                    for (int j = 0; j < node.nextIds.Count; j++)
                    {
                        if (!stageNodeById.TryGetValue(node.nextIds[j], out var next)) continue;
                        Vector2 p2 = NodePos(next);
                        float midX = (p1.x + p2.x) * 0.5f;
                        Color oc = GUI.color;
                        GUI.color = new Color(0.3f, 0.46f, 0.74f, 0.42f);
                        GUI.DrawTexture(new Rect(Mathf.Min(p1.x, midX), p1.y - 2f, Mathf.Abs(midX - p1.x), 4f), Texture2D.whiteTexture);
                        GUI.DrawTexture(new Rect(midX - 2f, Mathf.Min(p1.y, p2.y), 4f, Mathf.Abs(p2.y - p1.y)), Texture2D.whiteTexture);
                        GUI.DrawTexture(new Rect(Mathf.Min(midX, p2.x), p2.y - 2f, Mathf.Abs(p2.x - midX), 4f), Texture2D.whiteTexture);
                        GUI.color = oc;
                    }
                }

                for (int i = 0; i < stages.Count; i++)
                {
                    var node = stages[i];
                    Vector2 pos = NodePos(node);
                    bool selectable = availableStageNodeIds.Contains(node.id);
                    bool current = node.id == currentStageNodeId;
                    StageType drawType = GetEffectiveStageType(node);
                    string label = drawType == StageType.Mystery ? "?" : StageName(drawType);
                    Color nodeColor = drawType switch
                    {
                        StageType.Normal => new Color(0.34f, 0.42f, 0.62f),
                        StageType.Elite => new Color(0.62f, 0.28f, 0.3f),
                        StageType.Shop => new Color(0.2f, 0.5f, 0.32f),
                        StageType.Treasure => new Color(0.72f, 0.48f, 0.16f),
                        StageType.Boss => new Color(0.5f, 0.18f, 0.18f),
                        _ => new Color(0.4f, 0.36f, 0.62f)
                    };
                    if (node.cleared) nodeColor *= 0.55f;
                    Rect nodeRect = new Rect(pos.x - 34f, pos.y - 22f, 68f, 44f);
                    GUI.color = selectable ? nodeColor : nodeColor * 0.75f;
                    GUI.DrawTexture(nodeRect, Texture2D.whiteTexture);
                    if (current)
                    {
                        GUI.color = new Color(1f, 0.9f, 0.4f, 0.45f);
                        GUI.DrawTexture(new Rect(nodeRect.x - 4f, nodeRect.y - 4f, nodeRect.width + 8f, nodeRect.height + 8f), Texture2D.whiteTexture);
                    }
                    GUI.color = Color.white;
                    GUI.Label(new Rect(nodeRect.x + 8f, nodeRect.y + 4f, nodeRect.width - 16f, 18f), label, chipTitleStyle);
                    GUI.Label(new Rect(nodeRect.x + 6f, nodeRect.y + 22f, nodeRect.width - 12f, 16f), $"第{node.floor}层", chipMetaStyle);
                    if (selectable && GUI.Button(nodeRect, GUIContent.none, GUIStyle.none))
                    {
                        SelectStageNode(node.id);
                    }
                }
            }
        }

        if (state == RunState.Event)
        {
            int floor = pendingEventFloor > 0 ? pendingEventFloor : stageIndex + 1;
            int safeGoldMin = gold;
            int safeGoldMax = gold + 6;
            int safeLifeMin = playerLife;
            int safeLifeMax = Mathf.Min(36, playerLife + 5);
            int riskyGoldAfter = gold + (playerLife > 6 ? 12 : 4);
            int riskyLifeAfter = playerLife > 6 ? Mathf.Max(1, playerLife - 3) : playerLife;
            string suggest = playerLife <= 10
                ? "建议：当前生命偏低，优先稳健路线。"
                : (gold <= 14 ? "建议：当前经济偏紧，可考虑冒险补经济。" : "建议：根据下回合强度选择，稳健保血/冒险抢经济。");

            GUI.Box(new Rect(16, 220, 760, 220), $"奇遇事件（第{floor}层）");
            float pulse = 0.45f + 0.55f * Mathf.PingPong(Time.realtimeSinceStartup * 1.8f, 1f);
            Color oc = GUI.color;
            GUI.color = new Color(1f, 0.86f, 0.35f, 0.18f * pulse);
            GUI.DrawTexture(new Rect(24, 248, 744, 24), Texture2D.whiteTexture);
            GUI.color = oc;
            GUI.Label(new Rect(30, 252, 720, 20), "事件抉择中：请在稳健与冒险之间选择一条路线");

            GUI.Label(new Rect(30, 274, 720, 44), "你遇到了一次事件选择：\n稳健收益更可控；冒险收益更高但需要承担生命代价。", wrapLabelStyle);
            GUI.Label(new Rect(30, 316, 720, 20), suggest);

            GUI.Box(new Rect(30, 336, 340, 82), $"稳健选项（低波动）\n金币 {safeGoldMin}~{safeGoldMax} / 生命 {safeLifeMin}~{safeLifeMax}");
            if (GUI.Button(new Rect(120, 392, 160, 24), "选择稳健"))
            {
                ResolveMysteryEventChoice(false);
            }

            GUI.Box(new Rect(406, 336, 340, 82), $"冒险选项（高收益）\n金币 {gold}->{riskyGoldAfter} / 生命 {playerLife}->{riskyLifeAfter}");
            if (GUI.Button(new Rect(496, 392, 160, 24), "选择冒险"))
            {
                ResolveMysteryEventChoice(true);
            }
        }

        if (state == RunState.Prepare)
        {
            var stageNode = GetCurrentStageNode();
            var stageType = GetEffectiveStageType(stageNode);
            GUI.Box(new Rect(16, 220, leftW, 78), "");
            GUI.Label(new Rect(24, 228, leftW - 20, 20), stageType == StageType.Shop ? "商店节点：可购物、刷新并购买海克斯奇物，不会触发战斗" : "准备阶段：拖拽棋子到战场左侧5列布阵");
            GUI.Label(new Rect(24, 250, leftW - 20, 20), $"羁绊：{GetSynergySummary(deploySlots)}");
            GUI.Label(new Rect(24, 272, leftW - 20, 20), $"阵容评分：{GetCompPowerScore(deploySlots)}");

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
                        GUI.color = new Color(0.24f, 0.32f, 0.5f, 0.6f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                    }
                    else
                    {
                        GUI.color = new Color(0.12f, 0.78f, 1f, 0.46f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                        GUI.color = new Color(1f, 0.95f, 0.65f, 0.98f);
                        GUI.Label(new Rect(cell.x + 4, cell.y + 2, 26, 18), $"{placed.star}★");
                    }
                    GUI.color = old;
                }
            }

            float panelX = 16f;
            float panelY = guiH - 332f;
            float panelW = guiW - 32f;
            float panelH = 316f;
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "准备阶段");

            float opsW = compact ? 472f : 540f;
            float shopAreaW = panelW - 32f;
            float shopGapX = 10f;
            int shopCols = 5;
            float shopW = (shopAreaW - (shopCols - 1) * shopGapX) / shopCols;
            float shopH = 96f;
            float benchGap = 10f;
            float benchW = (panelW - 32f - 7f * benchGap) / 8f;
            float benchH = 102f;

            // B3 奖励视觉支持：计算并展示当前经济预期
            int currentInterestRaw = gold / 10;
            int effectiveCap = GetInterestCap();
            int currentInterest = Mathf.Min(currentInterestRaw, effectiveCap);
            string interestText = effectiveCap < 5 ? $"<color=#ffaa88>利息 +{currentInterest} (上限 {effectiveCap})</color>" : $"利息 +{currentInterest}";

            GUI.Label(new Rect(panelX + 14, panelY + 8, 220, 28), $"<size=21><b>金币 {gold}</b></size> <size=14>{interestText}</size>", hudStatStyle);
            GUI.Label(new Rect(panelX + 260, panelY + 8, 180, 28), $"<size=21><b>经验 {exp}/{ExpNeed(playerLevel)}</b></size>", hudStatStyle);
            GUI.Label(new Rect(panelX + 450, panelY + 8, 160, 28), $"<size=21><b>上阵 {GetBoardCap()}</b></size>", hudStatStyle);
            float topButtonsX = panelX + panelW - opsW;
            float oddsX = panelX + 520f;
            float oddsW = Mathf.Max(310f, topButtonsX - oddsX - 12f);
            DrawShopOddsPanel(new Rect(oddsX, panelY + 6, oddsW, 34f));

            string rerollBtnText = freeRerollTurns > 0 ? $"免费({freeRerollTurns})" : "刷新(-2)";
            if (GUI.Button(new Rect(topButtonsX, panelY + 6, 90, 34), rerollBtnText)) RefreshShop();
            if (GUI.Button(new Rect(topButtonsX + 98, panelY + 6, 108, 34), "买经验(-4)"))
            {
                if (gold >= 4) { gold -= 4; GainExp(4); }
            }
            if (GUI.Button(new Rect(topButtonsX + 214, panelY + 6, 108, 34), lockShop ? "解锁商店" : "锁定商店"))
            {
                lockShop = !lockShop;
                battleLog = lockShop ? "已锁定商店（下回合保留）" : "已解锁商店";
            }
            if (DrawPrimaryActionButton(new Rect(topButtonsX + 330, panelY + 4, 178, 40), stageType == StageType.Shop ? "离开商店" : "开始战斗")) StartBattle();
            DrawHudInfoStrip(new Rect(topButtonsX, panelY + 44, 206f, 22f), lockShop ? "商店状态：已锁定" : "商店状态：未锁定");
            DrawHudInfoStrip(new Rect(topButtonsX + 214, panelY + 44, 128f, 22f), $"路线保底 {lockedCompMissStreak}/3", TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(topButtonsX + 350, panelY + 42, 158, 26), "一键自动布阵")) AutoArrangeByLockedComp();

            GUI.Label(new Rect(panelX + 16, panelY + 76, 240, 20), "商店");
            for (int i = 0; i < shopOffers.Count; i++)
            {
                var d = unitDefs[shopOffers[i]];
                int col = i % shopCols;
                Rect r = new Rect(panelX + 16 + col * (shopW + shopGapX), panelY + 94, shopW, shopH);
                if (CountOwnedCopies(d.key) > 0)
                {
                    float pulse = 0.45f + 0.55f * Mathf.PingPong(Time.realtimeSinceStartup * 2.2f, 1f);
                    Color oc = GUI.color;
                    GUI.color = new Color(1f, 0.92f, 0.35f, 0.38f * pulse);
                    GUI.DrawTexture(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), Texture2D.whiteTexture);
                    GUI.color = oc;
                }
                DrawUnitChipCard(r, d, 1, d.cost, true);
                if (CountOwnedCopies(d.key) > 0)
                {
                    GUI.Label(new Rect(r.x + r.width - 30, r.y + 2, 26, 16), "目标", chipMetaStyle);
                }
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) BuyOffer(i);
            }
            GUI.Label(new Rect(panelX + 16, panelY + 196, 120, 20), "备战席");
            for (int i = 0; i < 8; i++)
            {
                float bx = panelX + 16 + i * (benchW + benchGap);
                if (i < benchUnits.Count)
                {
                    var u = benchUnits[i];
                    Rect r = new Rect(bx, panelY + 214, benchW, benchH);
                    DrawUnitChipCard(r, u.def, u.star, u.def.cost, false);
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        if (!DeployBenchUnitToFirstOpen(i))
                        {
                            inspectedUnit = u;
                            ShowTooltip(BuildUnitTooltip(u));
                            battleLog = $"查看 {u.Name}";
                        }
                    }
                }
                else GUI.Box(new Rect(bx, panelY + 194, benchW, benchH), "");
            }

            if (inspectedUnit != null)
            {
                int depIdx = deploySlots.FindIndex(u => u.id == inspectedUnit.id);
                int benIdx = benchUnits.FindIndex(u => u.id == inspectedUnit.id);
                if (depIdx >= 0)
                {
                    if (GUI.Button(new Rect(topButtonsX, panelY + panelH - 56, 150, 38), "下场(备战席)"))
                    {
                        if (ReturnDeployToBench(depIdx)) RedrawPrepareBoard();
                    }
                }
                if (depIdx >= 0 || benIdx >= 0)
                {
                    if (GUI.Button(new Rect(topButtonsX + 160, panelY + panelH - 56, 150, 38), "出售选中"))
                    {
                        if (SellUnit(inspectedUnit)) RedrawPrepareBoard();
                    }
                }
            }

            float syH = compact ? 210f : 230f;
            float compY = 220f + syH + 8f;
            float foldedCompY = panelY - 64f;
            float compH = showCompPanelFoldout ? Mathf.Max(150f, panelY - compY - 12f) : 52f;
            DrawSynergyClickPanel(rightX, 220, rightW, syH, deploySlots);
            if (stageType == StageType.Shop)
            {
                DrawShopHexPanel(rightX, compY, rightW, Mathf.Max(196f, panelY - compY - 12f));
            }
            else
            {
                DrawCompPanel(rightX, showCompPanelFoldout ? compY : foldedCompY, rightW, compH, deploySlots);
            }
        }

        if (state == RunState.Battle)
        {
            int allyAlive = playerUnits.FindAll(u => u.Alive).Count;
            int enemyAlive = enemyUnits.FindAll(u => u.Alive).Count;
            int allyScore = GetCompPowerScore(playerUnits);
            int enemyScore = GetCompPowerScore(enemyUnits);

            GUI.Box(new Rect(16, 220, leftW, 210), "战斗态势");
            GUI.Label(new Rect(28, 246, leftW - 24, 20), $"回合速度：{speedLevel}x");
            GUI.Label(new Rect(28, 268, leftW - 24, 20), $"存活单位：我方 {allyAlive} / 敌方 {enemyAlive}");
            GUI.Label(new Rect(28, 290, leftW - 24, 20), $"阵容评分：我方 {allyScore} / 敌方 {enemyScore}");
            GUI.Label(new Rect(28, 312, leftW - 24, 20), $"我方羁绊：{GetSynergySummary(playerUnits)}");
            GUI.Label(new Rect(28, 336, leftW - 24, 20), "最近战况：");
            GUI.Label(new Rect(28, 356, leftW - 24, 62), battleLog, wrapLabelStyle);

            if (GUI.Button(new Rect(30, 392, 130, 32), "切换速度"))
            {
                if (speedLevel == 1) speedLevel = 2;
                else if (speedLevel == 2) speedLevel = 4;
                else speedLevel = 1;
            }
            if (GUI.Button(new Rect(170, 392, 170, 32), showBattleStats ? "隐藏战斗统计" : "显示战斗统计"))
            {
                showBattleStats = !showBattleStats;
            }

            DrawSynergyClickPanel(16, 438, leftW, 178, playerUnits);
            for (int i = 0; i < playerUnits.Count; i++)
            {
                var u = playerUnits[i];
                if (!u.Alive) continue;
                Rect cell = GetBoardCellGuiRect(u.x, u.y);
                GUI.Label(new Rect(cell.x + 4, cell.y + 2, 26, 18), $"{u.star}★");
            }
            for (int i = 0; i < enemyUnits.Count; i++)
            {
                var u = enemyUnits[i];
                if (!u.Alive) continue;
                Rect cell = GetBoardCellGuiRect(u.x, u.y);
                Color oc = GUI.color;
                GUI.color = new Color(1f, 0.75f, 0.55f, 1f);
                GUI.Label(new Rect(cell.x + 4, cell.y + 2, 26, 18), $"{u.star}★");
                GUI.color = oc;
            }

            if (showBattleStats)
            {
                DrawBattleStats(rightX, 170, rightW, 470);
            }
        }

        if (state == RunState.Reward)
        {
            GUI.Box(new Rect(16, 220, 760, 280), "战后奖励（三选一）");
            Color oldHdr = GUI.color;
            GUI.color = lastBattleWin ? new Color(0.2f, 0.62f, 0.32f, 0.95f) : new Color(0.72f, 0.28f, 0.24f, 0.95f);
            GUI.DrawTexture(new Rect(16, 186, 760, 28), Texture2D.whiteTexture);
            GUI.color = oldHdr;
            GUI.Label(new Rect(24, 190, 740, 20), string.IsNullOrEmpty(lastBattleSummary) ? "战斗已结束" : lastBattleSummary);
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
                GUI.Label(new Rect(x + 12, 310, 196, 82), r.desc, wrapLabelStyle);

                if (GUI.Button(new Rect(x + 40, 400, 140, 30), "选择")) PickReward(i);
            }

            // 奖励阶段聚焦选择本身，隐藏战斗统计层，避免信息干扰
        }

        if (state == RunState.Hex)
        {
            GUI.Box(new Rect(16, 220, 740, 260), pendingEliteHexReward ? "精英海克斯战利品（三选一）" : "海克斯选择（三选一）");
            int bestHexScore = int.MinValue;
            int bestHexIdx = -1;
            for (int i = 0; i < currentHexOffers.Count; i++)
            {
                int s = ScoreHexForLockedComp(currentHexOffers[i]);
                if (s > bestHexScore) { bestHexScore = s; bestHexIdx = i; }
            }
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
                GUI.Label(new Rect(card.x + 90, card.y + 38, card.width - 96, 110), h.desc, wrapLabelStyle);
                if (i == bestHexIdx) GUI.Label(new Rect(card.x + 90, card.y + 146, card.width - 96, 20), "推荐：契合当前阵容");

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

            float mx = Input.mousePosition.x / uiScale + 14f;
            float my = guiH - (Input.mousePosition.y / uiScale) + 14f;
            var r = new Rect(mx, my, 170, 38);
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, r.width, r.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.18f, 0.42f, 0.72f, 0.96f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(r.x + 8, r.y + 9, r.width - 14, 20), dragText);
        }

        if (!string.IsNullOrEmpty(eventResultToastText) && Time.realtimeSinceStartup <= eventResultToastUntil)
        {
            float remain = Mathf.Clamp01((eventResultToastUntil - Time.realtimeSinceStartup) / 2.4f);
            Color oc = GUI.color;
            GUI.color = new Color(eventResultToastColor.r, eventResultToastColor.g, eventResultToastColor.b, 0.18f + 0.42f * remain);
            GUI.DrawTexture(new Rect(18, 182 + topPad, 420, 28), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(26, 186 + topPad, 404, 20), eventResultToastText);
            GUI.color = oc;
        }

        DrawFloatingTooltip();

        GUI.skin.box = oldBox;
        GUI.skin.button = oldButton;
        GUI.skin.label = oldLabel;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    #endregion
}
