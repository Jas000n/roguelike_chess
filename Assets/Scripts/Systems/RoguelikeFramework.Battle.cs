using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
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

        TryTriggerMedicSupport(actor);

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
        if (HasHex("rider_relay") && actor.ClassTag == "Rider" && !actor.usedOriginProc)
        {
            dmg += 6;
            actor.usedOriginProc = true;
        }
        if (actor.OriginTag == "Night" && !actor.usedOriginProc)
        {
            int night = CountOrigin(actor.player ? playerUnits : enemyUnits, "Night");
            if (night >= 2)
            {
                dmg += 6;
                actor.usedOriginProc = true;
            }
        }
        if (actor.OriginTag == "Shadow")
        {
            int shadow = CountOrigin(actor.player ? playerUnits : enemyUnits, "Shadow");
            if (shadow >= 2 && UnityEngine.Random.value < (shadow >= 4 ? 0.2f : 0.12f))
            {
                dmg = Mathf.RoundToInt(dmg * 1.25f);
            }
        }
        if (actor.OriginTag == "Venom")
        {
            int venom = CountOrigin(actor.player ? playerUnits : enemyUnits, "Venom");
            if (venom >= 2)
            {
                int extra = Mathf.RoundToInt(target.maxHp * (venom >= 4 ? 0.06f : 0.04f));
                dmg += Mathf.Max(2, extra);
            }
        }
        if (actor.OriginTag == "Frost")
        {
            int frost = CountOrigin(actor.player ? playerUnits : enemyUnits, "Frost");
            if (frost >= 2)
            {
                int shard = Mathf.Max(2, Mathf.RoundToInt(target.maxHp * (frost >= 4 ? 0.07f : 0.04f)));
                dmg += shard;
            }
        }
        if (actor.OriginTag == "Thunder")
        {
            int thunder = CountOrigin(actor.player ? playerUnits : enemyUnits, "Thunder");
            if (thunder >= 2)
            {
                dmg += thunder >= 4 ? 7 : 4;
            }
        }
        if (actor.ClassTag == "Controller")
        {
            int ctrl = CountClass(actor.player ? playerUnits : enemyUnits, "Controller");
            if (ctrl >= 2) dmg += 4;
            if (ctrl >= 4) dmg += 6;
        }

        if (HasHex("assassin_bloom") && actor.ClassTag == "Assassin")
        {
            if (!actor.usedCharge) dmg += 8;
            if (UnityEngine.Random.value < 0.12f) dmg = Mathf.RoundToInt(dmg * 1.28f);
        }
        if (HasHex("assassin_gate") && actor.ClassTag == "Assassin" && !actor.usedCharge)
        {
            dmg += 18;
            actor.usedCharge = true;
        }
        if (HasHex("assassin_contract") && actor.ClassTag == "Assassin")
        {
            dmg = Mathf.RoundToInt(dmg * 1.18f);
        }
        if (HasHex("execution_edge"))
        {
            float hpRatio = target.maxHp > 0 ? target.hp / (float)target.maxHp : 1f;
            if (hpRatio < 0.6f) dmg = Mathf.RoundToInt(dmg * (1f + (0.6f - hpRatio) * 0.5f));
        }

        if (dist <= actorRange)
        {
            actor.attacksDone++;
            int real = ApplyDamageWithTraits(actor, target, dmg);
            if (actorRange > 1)
            {
                // 不同炮系弹道差异化
                if (actor.def.key == "cannon_missile") SpawnProjectile(actor, target, new Color(1f, 0.35f, 0.2f), 0.24f, 0.12f);
                else if (actor.def.key == "cannon_mortar") SpawnProjectile(actor, target, new Color(1f, 0.78f, 0.28f), 0.20f, 0.2f);
                else if (actor.def.key == "cannon_burst") SpawnProjectile(actor, target, new Color(1f, 0.25f, 0.25f), 0.14f, 0.08f);
                else SpawnProjectile(actor, target, new Color(1f, 0.75f, 0.2f));

                battleLog = $"{actor.Name} 攻击 {target.Name} -{real}";
                PushEvent($"[{actor.Name}] 命中 [{target.Name}] -{real}");
            }
            else
            {
                if (actor.def.key == "chariot_tank") SpawnHitFlash(target, new Color(0.7f, 0.9f, 1f), 0.75f);
                else if (actor.def.key == "chariot_shock") SpawnHitFlash(target, new Color(0.7f, 0.55f, 1f), 0.9f);
                else SpawnHitFlash(target, new Color(1f, 0.2f, 0.2f), 0.6f);
                battleLog = $"{actor.Name} 近战 {target.Name} -{real}";
                PushEvent($"[{actor.Name}] 近战 [{target.Name}] -{real}");
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
                PushEvent($"[{actor.Name}] 连发追击 -{real2}");
            }
            if (HasHex("artillery_overclock") && actor.ClassTag == "Artillery" && battleStartedTurn % 4 == 0)
            {
                var splashTargets = actor.player ? enemyUnits : playerUnits;
                foreach (var t in splashTargets)
                {
                    if (!t.Alive || t == target) continue;
                    int ad = Mathf.Abs(t.x - target.x) + Mathf.Abs(t.y - target.y);
                    if (ad <= 1) ApplyDamageWithTraits(actor, t, Mathf.Max(1, Mathf.RoundToInt(dmg * 0.33f)));
                }
            }
            if (actor.OriginTag == "Thunder")
            {
                int thunder = CountOrigin(actor.player ? playerUnits : enemyUnits, "Thunder");
                if (thunder >= 2 && actor.attacksDone % 3 == 0)
                {
                    var chainTargets = actor.player ? enemyUnits : playerUnits;
                    Unit chained = null;
                    int bestD = 999;
                    for (int i = 0; i < chainTargets.Count; i++)
                    {
                        var t = chainTargets[i];
                        if (!t.Alive || t.id == target.id) continue;
                        int d = Mathf.Abs(t.x - target.x) + Mathf.Abs(t.y - target.y);
                        if (d < bestD)
                        {
                            bestD = d;
                            chained = t;
                        }
                    }
                    if (chained != null)
                    {
                        int chainDmg = Mathf.Max(2, Mathf.RoundToInt(real * (thunder >= 4 ? 0.55f : 0.35f)));
                        ApplyDamageWithTraits(actor, chained, chainDmg);
                        SpawnHitFlash(chained, new Color(0.65f, 0.85f, 1f), 0.9f);
                        battleLog += $" | 雷弧-{chainDmg}";
                    }
                }
            }
            if (actor.OriginTag == "Frost")
            {
                int frost = CountOrigin(actor.player ? playerUnits : enemyUnits, "Frost");
                if (frost >= 4)
                {
                    var splashTargets = actor.player ? enemyUnits : playerUnits;
                    foreach (var t in splashTargets)
                    {
                        if (!t.Alive || t.id == target.id) continue;
                        int ad = Mathf.Abs(t.x - target.x) + Mathf.Abs(t.y - target.y);
                        if (ad <= 1)
                        {
                            int shard = Mathf.Max(1, Mathf.RoundToInt(real * 0.3f));
                            ApplyDamageWithTraits(actor, t, shard);
                            SpawnHitFlash(t, new Color(0.75f, 0.92f, 1f), 0.75f);
                        }
                    }
                }
            }
        }
        else
        {
            StepToward(actor, target);
            battleLog = $"{actor.Name} 向 {target.Name} 移动";
        }

        battleStartedTurn++;
    }

    private void ApplyAssassinAmbush()
    {
        void AmbushTeam(List<Unit> attackers, List<Unit> defenders)
        {
            if (CountClass(attackers, "Assassin") < 2) return;
            for (int i = 0; i < attackers.Count; i++)
            {
                var assassin = attackers[i];
                if (!assassin.Alive || assassin.ClassTag != "Assassin") continue;

                Unit target = null;
                int bestScore = int.MinValue;
                for (int j = 0; j < defenders.Count; j++)
                {
                    var d = defenders[j];
                    if (!d.Alive) continue;
                    int score = d.range * 10 + (assassin.player ? d.x : (W - 1 - d.x));
                    if (score > bestScore)
                    {
                        bestScore = score;
                        target = d;
                    }
                }
                if (target == null) continue;

                Vector2Int[] offsets = assassin.player
                    ? new[] { new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(-1, 1), new Vector2Int(-2, 0) }
                    : new[] { new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(1, 1), new Vector2Int(2, 0) };

                for (int k = 0; k < offsets.Length; k++)
                {
                    int nx = Mathf.Clamp(target.x + offsets[k].x, 0, W - 1);
                    int ny = Mathf.Clamp(target.y + offsets[k].y, 0, H - 1);
                    bool occupied = false;

                    for (int t = 0; t < attackers.Count; t++)
                    {
                        if (t == i || !attackers[t].Alive) continue;
                        if (attackers[t].x == nx && attackers[t].y == ny) { occupied = true; break; }
                    }
                    if (!occupied)
                    {
                        assassin.x = nx;
                        assassin.y = ny;
                        break;
                    }
                }
            }
        }

        AmbushTeam(playerUnits, enemyUnits);
        AmbushTeam(enemyUnits, playerUnits);
    }

    private void TryTriggerMedicSupport(Unit actor)
    {
        var team = actor.player ? playerUnits : enemyUnits;
        int medic = CountClass(team, "Medic");
        bool grace = HasHex("guardian_grace") && (actor.ClassTag == "Guardian" || actor.ClassTag == "Medic");
        if (actor.ClassTag != "Medic" && !grace) return;
        if (medic < 2 && !grace) return;

        Unit target = null;
        float lowestRatio = 1.1f;
        for (int i = 0; i < team.Count; i++)
        {
            var ally = team[i];
            if (!ally.Alive || ally.hp >= ally.maxHp) continue;
            float ratio = ally.maxHp > 0 ? ally.hp / (float)ally.maxHp : 1f;
            if (ratio < lowestRatio)
            {
                lowestRatio = ratio;
                target = ally;
            }
        }
        if (target == null) return;

        float healScale = medic >= 4 ? 0.16f : medic >= 2 ? 0.08f : 0.05f;
        int heal = Mathf.Max(3, Mathf.RoundToInt(target.maxHp * healScale));
        target.hp = Mathf.Min(target.maxHp, target.hp + heal);
        battleLog = $"{actor.Name} 治疗 {target.Name} +{heal}";
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

        if (to.OriginTag == "Wind")
        {
            int wind = CountOrigin(to.player ? playerUnits : enemyUnits, "Wind");
            float dodge = 0f;
            if (wind >= 2) dodge += 0.08f;
            if (wind >= 4) dodge += 0.08f;
            if (HasHex("windwalk")) dodge += 0.06f;
            if (UnityEngine.Random.value < dodge) return 0;
        }
        if (to.OriginTag == "Mist")
        {
            int mist = CountOrigin(to.player ? playerUnits : enemyUnits, "Mist");
            float dodge = mist >= 4 ? 0.20f : mist >= 2 ? 0.12f : 0f;
            if (UnityEngine.Random.value < dodge) return 0;
        }

        float reduction = GetDamageReduction(to);
        dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * (1f - reduction)));

        // 旧兵种残留风味
        if (to.Family == "士" && UnityEngine.Random.value < 0.15f) return 0;

        to.hp -= dmg;
        from.damageDealt += dmg;
        to.damageTaken += dmg;
        bool killed = to.hp <= 0;

        if (HasHex("lifesteal_core") && from.Alive && dmg > 0)
        {
            int heal = Mathf.Max(1, Mathf.RoundToInt(dmg * 0.12f));
            from.hp = Mathf.Min(from.maxHp, from.hp + heal);
        }
        if (from.OriginTag == "Holy" && from.Alive && dmg > 0)
        {
            int holy = CountOrigin(from.player ? playerUnits : enemyUnits, "Holy");
            float ratio = holy >= 4 ? 0.12f : holy >= 2 ? 0.06f : 0f;
            if (ratio > 0f)
            {
                int heal = Mathf.Max(1, Mathf.RoundToInt(dmg * ratio));
                from.hp = Mathf.Min(from.maxHp, from.hp + heal);
            }
        }
        if (killed && from.player && HasHex("assassin_contract") && from.ClassTag == "Assassin")
        {
            gold += 1;
        }
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
}
