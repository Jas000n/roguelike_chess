using System;
using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
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
            usedOriginProc = false,
            damageDealt = 0,
            damageTaken = 0
        };
    }

    private void UpgradeUnit(Unit u)
    {
        int nextStar = Mathf.Clamp(u.star + 1, 1, 3);
        if (nextStar == 2)
        {
            u.hp = Mathf.RoundToInt(u.hp * 2.1f);
            u.atk = Mathf.RoundToInt(u.atk * 1.85f);
            u.spd += 3;
        }
        else if (nextStar == 3)
        {
            u.hp = Mathf.RoundToInt(u.hp * 2.6f);
            u.atk = Mathf.RoundToInt(u.atk * 2.2f);
            u.spd += 4;
            if (u.ClassTag == "Artillery") u.range += 1;
        }
        u.star = nextStar;
        u.maxHp = u.hp;
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
        cap += rewardBoardCapBonus;
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
        if (freeRefresh && lockShop && shopOffers.Count > 0)
        {
            battleLog = "商店已锁定：保留上回合商品";
            return;
        }

        bool consumeRerollEngine = false;
        if (!freeRefresh && HasHex("reroll_engine") && rerollEngineFreeUses > 0)
        {
            consumeRerollEngine = true;
            rerollEngineFreeUses--;
        }

        if (!freeRefresh)
        {
            if (!consumeRerollEngine)
            {
                if (gold < 1) { battleLog = "金币不足，无法刷新"; return; }
                gold -= 1;
            }
        }

        shopOffers.Clear();
        for (int i = 0; i < 5; i++) shopOffers.Add(RollShopKeyByLevel());
        EnsureLockedCompShopPity();
    }

    private void EnsureLockedCompShopPity()
    {
        var lc = GetLockedComp();
        if (lc == null || shopOffers.Count == 0)
        {
            lockedCompMissStreak = 0;
            return;
        }

        bool hasFocus = false;
        for (int i = 0; i < shopOffers.Count; i++)
        {
            var d = unitDefs[shopOffers[i]];
            for (int k = 0; k < lc.focusClasses.Length; k++) if (d.classTag == lc.focusClasses[k]) hasFocus = true;
        }
        if (hasFocus)
        {
            lockedCompMissStreak = 0;
            return;
        }

        lockedCompMissStreak++;
        if (lockedCompMissStreak < 3) return;
        lockedCompMissStreak = 0;

        var candidates = new List<UnitDef>();
        foreach (var kv in unitDefs)
        {
            var d = kv.Value;
            if (d.cost > Mathf.Min(4, playerLevel + 1)) continue;
            for (int k = 0; k < lc.focusClasses.Length; k++)
            {
                if (d.classTag == lc.focusClasses[k]) candidates.Add(d);
            }
        }
        if (candidates.Count == 0) return;
        int idx = UnityEngine.Random.Range(0, shopOffers.Count);
        var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        shopOffers[idx] = pick.key;
        battleLog = $"保底触发：补入 {pick.name}";
    }

    private string RollShopKeyByLevel()
    {
        int cost = RollShopCostByLevel();
        var filtered = new List<UnitDef>();
        foreach (var kv in unitDefs) if (kv.Value.cost == cost) filtered.Add(kv.Value);

        if (filtered.Count == 0)
        {
            for (int d = 1; d <= 4 && filtered.Count == 0; d++)
            {
                int c1 = Mathf.Clamp(cost - d, 1, 5);
                int c2 = Mathf.Clamp(cost + d, 1, 5);
                foreach (var kv in unitDefs)
                {
                    if (kv.Value.cost == c1 || kv.Value.cost == c2) filtered.Add(kv.Value);
                }
            }
        }
        return PickWeightedShopKey(filtered);
    }

    private string RollCompUnitKeyByLevel()
    {
        var lc = GetLockedComp();
        if (lc == null) return RollShopKeyByLevel();

        int maxCostByLevel = playerLevel <= 2 ? 2 : playerLevel <= 4 ? 3 : playerLevel <= 6 ? 4 : 5;
        var pool = new List<UnitDef>();
        foreach (var kv in unitDefs)
        {
            var d = kv.Value;
            if (d.cost > maxCostByLevel) continue;
            bool hit = false;
            for (int i = 0; i < lc.focusClasses.Length; i++) if (d.classTag == lc.focusClasses[i]) hit = true;
            for (int i = 0; i < lc.focusOrigins.Length; i++) if (d.originTag == lc.focusOrigins[i]) hit = true;
            if (hit) pool.Add(d);
        }
        if (pool.Count == 0) return RollShopKeyByLevel();
        return PickWeightedShopKey(pool);
    }

    private string PickWeightedShopKey(List<UnitDef> pool)
    {
        if (pool == null || pool.Count == 0) return basePool[UnityEngine.Random.Range(0, basePool.Count)];
        float total = 0f;
        var ws = new float[pool.Count];
        for (int i = 0; i < pool.Count; i++)
        {
            float w = 1f;
            var lc = GetLockedComp();
            if (lc != null)
            {
                float stageBias = playerLevel <= 3 ? 2.2f : playerLevel <= 5 ? 1.8f : 1.4f;
                for (int k = 0; k < lc.focusClasses.Length; k++) if (pool[i].classTag == lc.focusClasses[k]) w += stageBias;
                for (int k = 0; k < lc.focusOrigins.Length; k++) if (pool[i].originTag == lc.focusOrigins[k]) w += 0.9f;
                if (lc.id == "double4" && pool[i].cost <= 2) w += 0.5f;
                if (playerLevel >= 6 && pool[i].cost >= 4) w += 0.7f;
                if (playerLevel <= 3 && pool[i].cost >= 4) w *= 0.65f;
            }

            ws[i] = Mathf.Max(0.05f, w);
            total += ws[i];
        }
        float roll = UnityEngine.Random.Range(0f, total);
        float acc = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += ws[i];
            if (roll <= acc) return pool[i].key;
        }
        return pool[pool.Count - 1].key;
    }

    private int CountOwnedCopies(string key)
    {
        int c = 0;
        for (int i = 0; i < benchUnits.Count; i++) if (benchUnits[i].def.key == key) c++;
        for (int i = 0; i < deploySlots.Count; i++) if (deploySlots[i].def.key == key) c++;
        return c;
    }

    private int CountOwnedCopies(string key, int star)
    {
        int c = 0;
        for (int i = 0; i < benchUnits.Count; i++) if (benchUnits[i].def.key == key && benchUnits[i].star == star) c++;
        for (int i = 0; i < deploySlots.Count; i++) if (deploySlots[i].def.key == key && deploySlots[i].star == star) c++;
        return c;
    }

    private bool CanBuyOfferNow(string key)
    {
        if (benchUnits.Count < 8) return true;
        // 备战席满时，若买入后可立即触发合成，则允许购买（与金铲铲体验对齐）
        return CountOwnedCopies(key, 1) >= 2;
    }

    private float[] GetShopCostOddsByLevel()
    {
        int lv = Mathf.Clamp(playerLevel, 1, 8);
        return lv switch
        {
            1 => new[] { 0f, 0.70f, 0.30f, 0f, 0f, 0f },
            2 => new[] { 0f, 0.55f, 0.35f, 0.10f, 0f, 0f },
            3 => new[] { 0f, 0.40f, 0.40f, 0.18f, 0.02f, 0f },
            4 => new[] { 0f, 0.25f, 0.40f, 0.28f, 0.07f, 0f },
            5 => new[] { 0f, 0.18f, 0.30f, 0.35f, 0.15f, 0.02f },
            6 => new[] { 0f, 0.12f, 0.22f, 0.38f, 0.23f, 0.05f },
            7 => new[] { 0f, 0.08f, 0.16f, 0.34f, 0.30f, 0.12f },
            _ => new[] { 0f, 0.05f, 0.10f, 0.25f, 0.40f, 0.20f },
        };
    }

    private int RollShopCostByLevel()
    {
        var odds = GetShopCostOddsByLevel();
        float r = UnityEngine.Random.Range(0f, 1f);
        float acc = 0f;
        for (int c = 1; c <= 5; c++)
        {
            acc += odds[c];
            if (r <= acc) return c;
        }
        return 3;
    }

    private string GetShopOddsText()
    {
        var o = GetShopCostOddsByLevel();
        return $"1费{o[1] * 100f:0}%  2费{o[2] * 100f:0}%  3费{o[3] * 100f:0}%  4费{o[4] * 100f:0}%  5费{o[5] * 100f:0}%";
    }

    private void BuyOffer(int i)
    {
        if (i < 0 || i >= shopOffers.Count) return;
        string key = shopOffers[i];
        if (!unitDefs.ContainsKey(key)) return;

        var def = unitDefs[key];
        if (gold < def.cost) { battleLog = $"金币不足（需要{def.cost}）"; return; }
        if (!CanBuyOfferNow(key)) { battleLog = "备战席已满"; return; }

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

                    var mergeGroup = kv.Value.GetRange(0, 3);
                    Unit anchor = null;
                    for (int i = 0; i < mergeGroup.Count; i++)
                    {
                        var cand = mergeGroup[i];
                        if (deploySlots.Exists(x => x.id == cand.id))
                        {
                            anchor = cand;
                            break;
                        }
                    }
                    if (anchor == null) anchor = mergeGroup[0];

                    for (int i = 0; i < mergeGroup.Count; i++)
                    {
                        var rm = mergeGroup[i];
                        int bi = benchUnits.FindIndex(x => x.id == rm.id);
                        if (bi >= 0) { benchUnits.RemoveAt(bi); continue; }
                        int di = deploySlots.FindIndex(x => x.id == rm.id);
                        if (di >= 0) deploySlots.RemoveAt(di);
                    }

                    var up = CreateUnit(kv.Key, true);
                    for (int s = 1; s <= fromStar; s++) UpgradeUnit(up);
                    up.star = Mathf.Clamp(fromStar + 1, 1, 3);

                    bool anchorOnBoard = anchor.x >= 0 && anchor.y >= 0;
                    if (anchorOnBoard)
                    {
                        up.x = anchor.x;
                        up.y = anchor.y;
                        deploySlots.Add(up);
                    }
                    else
                    {
                        up.x = -1;
                        up.y = -1;
                        benchUnits.Add(up);
                    }
                    battleLog = fromStar == 1
                        ? $"合成成功：{up.Name} 升为2星"
                        : $"超级合成：{up.Name} 升为3星";
                    PushEvent(fromStar == 1 ? $"★升星成功：{up.Name} 升为2星" : $"★★★超级合成：{up.Name} 升为3星");

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

        int copyCount = u.star == 1 ? 1 : u.star == 2 ? 3 : 9;
        int sellGold = Mathf.Max(1, u.def.cost * copyCount);
        gold += sellGold;

        if (benchIdx >= 0) benchUnits.RemoveAt(benchIdx);
        if (deployIdx >= 0) deploySlots.RemoveAt(deployIdx);

        if (inspectedUnit != null && inspectedUnit.id == u.id) inspectedUnit = null;
        battleLog = $"出售 {u.Name} +{sellGold}金币";
        return true;
    }

    #endregion
}
