using System.Collections.Generic;
using UnityEngine;

public partial class RoguelikeFramework
{
    // Stage A2: 轻量配置数据层（先以内置配置集中管理，后续可平滑迁移到 ScriptableObject/JSON）
    private static readonly Dictionary<int, float[]> ShopCostOddsByLevelConfig = new()
    {
        { 1, new[] { 0f, 0.70f, 0.30f, 0f, 0f, 0f } },
        { 2, new[] { 0f, 0.55f, 0.35f, 0.10f, 0f, 0f } },
        { 3, new[] { 0f, 0.40f, 0.40f, 0.18f, 0.02f, 0f } },
        { 4, new[] { 0f, 0.25f, 0.40f, 0.28f, 0.07f, 0f } },
        { 5, new[] { 0f, 0.18f, 0.30f, 0.35f, 0.15f, 0.02f } },
        { 6, new[] { 0f, 0.12f, 0.22f, 0.38f, 0.23f, 0.05f } },
        { 7, new[] { 0f, 0.08f, 0.16f, 0.34f, 0.30f, 0.12f } },
        { 8, new[] { 0f, 0.05f, 0.10f, 0.25f, 0.40f, 0.20f } }
    };

    private float GetOpeningUnitCostWeight(int cost)
    {
        return cost switch
        {
            1 => 1.15f,
            2 => 1.0f,
            3 => 0.7f,
            _ => 0.2f
        };
    }

    private void EnsureShopOddsConfigLoaded()
    {
        if (shopOddsAssetChecked) return;
        shopOddsAssetChecked = true;

        var asset = Resources.Load<ShopOddsConfigAsset>("Configs/ShopOddsConfig");
        if (asset == null)
        {
            shopOddsConfigSource = "fallback-const (asset missing: Resources/Configs/ShopOddsConfig)";
            return;
        }

        if (!asset.TryBuildRuntimeMap(out var runtimeMap, out var err))
        {
            shopOddsConfigSource = $"fallback-const (asset invalid: {err})";
            Debug.LogWarning($"[DEV][CONFIG_VALIDATE] ShopOddsConfigAsset invalid, fallback to const. reason={err}");
            return;
        }

        for (int level = 1; level <= 8; level++)
        {
            if (runtimeMap.ContainsKey(level)) continue;
            shopOddsConfigSource = $"fallback-const (asset missing level={level})";
            Debug.LogWarning($"[DEV][CONFIG_VALIDATE] ShopOddsConfigAsset missing level={level}, fallback to const.");
            return;
        }

        shopOddsRuntimeOverride.Clear();
        foreach (var kv in runtimeMap) shopOddsRuntimeOverride[kv.Key] = kv.Value;
        shopOddsConfigSource = "scriptable-object";
    }

    private float[] GetShopCostOddsConfig(int level)
    {
        EnsureShopOddsConfigLoaded();

        int lv = Mathf.Clamp(level, 1, 8);
        if (shopOddsRuntimeOverride.TryGetValue(lv, out var runtimeOdds)) return runtimeOdds;
        return ShopCostOddsByLevelConfig.TryGetValue(lv, out var odds)
            ? odds
            : ShopCostOddsByLevelConfig[8];
    }

    private float GetLockedCompClassBiasByLevel(int level)
    {
        return level <= 3 ? 2.5f : level <= 5 ? 2.0f : 1.5f;
    }

    private const float LockedCompOriginBias = 1.1f;
    private const float Double4LowCostBonus = 0.5f;
    private const float LateGameHighCostBonus = 0.7f;
    private const float EarlyGameHighCostPenalty = 0.65f;

    private static readonly Dictionary<string, int> ShopHexCostByRarityConfig = new()
    {
        { "白", 3 },
        { "蓝", 5 },
        { "金", 8 },
        { "彩", 12 }
    };

    private int GetShopHexCostByRarity(string rarity)
    {
        if (string.IsNullOrEmpty(rarity)) return 3;
        return ShopHexCostByRarityConfig.TryGetValue(rarity, out var cost) ? cost : 3;
    }

    private readonly struct HexConfigEntry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Rarity;
        public readonly string Desc;

        public HexConfigEntry(string id, string name, string rarity, string desc)
        {
            Id = id;
            Name = name;
            Rarity = rarity;
            Desc = desc;
        }
    }

    private readonly struct RewardConfigEntry
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Desc;

        public RewardConfigEntry(string id, string name, string desc)
        {
            Id = id;
            Name = name;
            Desc = desc;
        }
    }

    private static readonly HexConfigEntry[] HexPoolConfig =
    {
        new("rich", "金币雨", "蓝", "每回合准备阶段额外 +4 金币"),
        new("interest_up", "理财大师", "蓝", "利息上限 +2"),
        new("cannon_master", "炮火专精", "金", "炮系伤害 +25%，开局前3回合额外 +10%"),
        new("rider_charge", "骑兵冲锋", "金", "骑兵速度 +2，首击额外 +8 伤害"),
        new("vanguard_wall", "钢铁壁垒", "蓝", "先锋单位受到伤害 -18%"),
        new("team_atk", "全军增幅", "白", "全队攻击 +2"),
        new("artillery_range", "超远校准", "蓝", "炮系射程 +1"),
        new("board_plus", "超载部署", "金", "上阵人数上限 +1"),
        new("fast_train", "快速练兵", "白", "每回合额外 +2 经验"),
        new("healing", "战备修复", "白", "每回合准备阶段，上阵棋子回复 20% 最大生命"),
        new("lifesteal_core", "嗜血核心", "金", "全队造成伤害时回复该伤害的 12% 生命"),
        new("execution_edge", "处决边缘", "蓝", "攻击低血目标时额外增伤，越残血越高"),
        new("assassin_bloom", "刺影绽放", "金", "刺客暴击率+12%，首击额外伤害+8"),
        new("assassin_contract", "暗影契约", "彩", "刺客暴击伤害提升，参与击杀时额外获得1金币"),
        new("vanguard_bastion", "壁垒军令", "金", "先锋获得额外减伤与开战护盾"),
        new("rider_relay", "连环冲阵", "蓝", "骑兵前8回合额外速度，首次命中后再次突进"),
        new("artillery_overclock", "火控超频", "彩", "炮手额外伤害，第四次攻击附带溅射"),
        new("stone_oath", "磐石誓约", "金", "石系单位生命与减伤提高"),
        new("venom_payload", "毒蚀载荷", "金", "毒系命中附加持续伤害"),
        new("windwalk", "疾风步", "蓝", "风系单位速度和闪避提高"),
        new("reroll_engine", "精密改造", "彩", "每回合首次刷新免费，并额外获得1次刷新"),
        new("triple_prep", "追三计划", "彩", "场上最高星级单位再获额外属性"),
        new("royal_supply", "王庭军需", "彩", "准备阶段额外获得 6 金币，并在商店节点额外刷新 1 张海克斯奇物"),
        new("assassin_gate", "影门突袭", "彩", "刺客开战切入后排时首次攻击额外造成 18 伤害并获得 25% 减伤"),
        new("guardian_grace", "神佑守望", "金", "守护者与医者每回合首次行动时为最低生命友军回复生命"),
        new("controller_net", "控场棋网", "金", "控场奇谋单位追加伤害提高，并获得额外射程"),
        new("medic_banner", "回春军旗", "金", "医者治疗量提高，且被治疗单位获得短暂减伤"),
        new("tri_service", "三军协同", "彩", "炮/控/医同时上阵时，全队获得额外伤害与续航")
    };

    private static readonly RewardConfigEntry[] RewardPoolConfig =
    {
        new("gold_big", "藏宝箱", "立即获得 +10 金币"),
        new("gold_huge", "黄金密约", "立即获得 +16 金币"),
        new("heal", "战地医疗", "恢复 8 点生命"),
        new("heal_big", "再生矩阵", "恢复 15 点生命"),
        new("exp", "战术复盘", "获得 6 点经验"),
        new("exp_big", "大师课程", "获得 10 点经验"),
        new("unit_low", "招募新兵", "获得 1 个随机 1-3 费棋子"),
        new("unit_mid", "精锐补员", "获得 1 个随机 2-4 费棋子（偏向当前路线）"),
        new("duo_pack", "双人补给", "获得 2 个随机低费棋子"),
        new("reroll_pack", "补给券", "免费刷新商店并额外 +2 金币"),
        new("board_bonus", "扩编令", "本局上阵上限永久 +1"),
        new("star_up", "战地升星", "随机提升 1 个我方棋子星级（最高3星）"),
        new("hex_random", "奇物箱", "随机获得 1 个未拥有海克斯"),
        new("free_reroll_3", "补给连拨", "接下来3回合，每回合首次刷新免费"),
        new("gold_interest", "对赌协议", "获得10金币，但本局利息上限-1"),
        new("exp_burst", "极限阅历", "获得等同于当前等级*3的经验值")
    };

    private int GetRewardOfferCount()
    {
        return 3;
    }

    private int GetHexOfferCount(bool inShop)
    {
        if (inShop) return HasHex("royal_supply") ? 3 : 2;
        return 3;
    }

    private HexConfigEntry[] GetHexPoolConfig()
    {
        return HexPoolConfig;
    }

    private RewardConfigEntry[] GetRewardPoolConfig()
    {
        return RewardPoolConfig;
    }

    private void RevalidateConfigData()
    {
        shopOddsAssetChecked = false;
        shopOddsRuntimeOverride.Clear();
        EnsureShopOddsConfigLoaded();

        if (ValidateConfigData(out var error))
        {
            configValidationStatus = $"pass=1 fail=0 | shopOdds={shopOddsConfigSource}";
            battleLog = "[DEV][CONFIG_VALIDATE] 配置校验通过";
            Debug.Log($"[DEV][CONFIG_VALIDATE] pass=1 fail=0 | shopOdds={shopOddsConfigSource}");
            return;
        }

        configValidationStatus = $"FAILED: {error}";
        battleLog = $"[DEV][CONFIG_VALIDATE] {error}";
        Debug.LogError($"[DEV][CONFIG_VALIDATE] FAILED {error}");
    }

    private bool ValidateConfigData(out string error)
    {
        error = "";

        var hexIds = new HashSet<string>();
        var validHexRarity = new HashSet<string> { "白", "蓝", "金", "彩" };
        for (int i = 0; i < HexPoolConfig.Length; i++)
        {
            var h = HexPoolConfig[i];
            if (string.IsNullOrWhiteSpace(h.Id))
            {
                error = $"HexPoolConfig[{i}] id 为空";
                return false;
            }
            if (!hexIds.Add(h.Id))
            {
                error = $"HexPoolConfig 存在重复 id: {h.Id}";
                return false;
            }
            if (string.IsNullOrWhiteSpace(h.Name) || string.IsNullOrWhiteSpace(h.Desc))
            {
                error = $"HexPoolConfig[{i}]({h.Id}) name/desc 为空";
                return false;
            }
            if (!validHexRarity.Contains(h.Rarity))
            {
                error = $"HexPoolConfig[{i}]({h.Id}) 稀有度非法: {h.Rarity}";
                return false;
            }
        }

        var rewardIds = new HashSet<string>();
        for (int i = 0; i < RewardPoolConfig.Length; i++)
        {
            var r = RewardPoolConfig[i];
            if (string.IsNullOrWhiteSpace(r.Id))
            {
                error = $"RewardPoolConfig[{i}] id 为空";
                return false;
            }
            if (!rewardIds.Add(r.Id))
            {
                error = $"RewardPoolConfig 存在重复 id: {r.Id}";
                return false;
            }
            if (string.IsNullOrWhiteSpace(r.Name) || string.IsNullOrWhiteSpace(r.Desc))
            {
                error = $"RewardPoolConfig[{i}]({r.Id}) name/desc 为空";
                return false;
            }
        }

        for (int level = 1; level <= 8; level++)
        {
            if (!ShopCostOddsByLevelConfig.TryGetValue(level, out var odds))
            {
                error = $"ShopCostOddsByLevelConfig 缺少 level={level}";
                return false;
            }
            if (odds == null || odds.Length != 6)
            {
                error = $"ShopCostOddsByLevelConfig[{level}] 长度非法（期望6）";
                return false;
            }

            float sum = 0f;
            for (int i = 0; i < odds.Length; i++)
            {
                if (odds[i] < 0f)
                {
                    error = $"ShopCostOddsByLevelConfig[{level}][{i}] 为负数";
                    return false;
                }
                sum += odds[i];
            }

            if (Mathf.Abs(sum - 1f) > 0.01f)
            {
                error = $"ShopCostOddsByLevelConfig[{level}] 概率和异常: {sum:F3}";
                return false;
            }
        }

        string[] requiredHexRarities = { "白", "蓝", "金", "彩" };
        for (int i = 0; i < requiredHexRarities.Length; i++)
        {
            string rarity = requiredHexRarities[i];
            if (!ShopHexCostByRarityConfig.TryGetValue(rarity, out int cost))
            {
                error = $"ShopHexCostByRarityConfig 缺少稀有度: {rarity}";
                return false;
            }
            if (cost <= 0)
            {
                error = $"ShopHexCostByRarityConfig[{rarity}] 价格非法: {cost}";
                return false;
            }
        }

        return true;
    }
}
