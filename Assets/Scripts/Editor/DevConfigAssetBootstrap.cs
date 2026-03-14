#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DevConfigAssetBootstrap
{
    [MenuItem("DragonChessLegends/Dev/Ensure Shop Odds Config Asset")]
    public static void EnsureShopOddsConfigAssetMenu()
    {
        EnsureShopOddsConfigAsset();
    }

    public static void EnsureShopOddsConfigAsset()
    {
        const string resourcesDir = "Assets/Resources/Configs";
        const string assetPath = resourcesDir + "/ShopOddsConfig.asset";

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(resourcesDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Configs");

        var asset = AssetDatabase.LoadAssetAtPath<ShopOddsConfigAsset>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<ShopOddsConfigAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.levels ??= new List<ShopOddsConfigAsset.LevelOddsEntry>();
        asset.levels.Clear();

        void Add(int level, float a0, float a1, float a2, float a3, float a4, float a5)
        {
            asset.levels.Add(new ShopOddsConfigAsset.LevelOddsEntry
            {
                level = level,
                odds = new[] { a0, a1, a2, a3, a4, a5 }
            });
        }

        Add(1, 0f, 0.70f, 0.30f, 0f, 0f, 0f);
        Add(2, 0f, 0.55f, 0.35f, 0.10f, 0f, 0f);
        Add(3, 0f, 0.40f, 0.40f, 0.18f, 0.02f, 0f);
        Add(4, 0f, 0.25f, 0.40f, 0.28f, 0.07f, 0f);
        Add(5, 0f, 0.18f, 0.30f, 0.35f, 0.15f, 0.02f);
        Add(6, 0f, 0.12f, 0.22f, 0.38f, 0.23f, 0.05f);
        Add(7, 0f, 0.08f, 0.16f, 0.34f, 0.30f, 0.12f);
        Add(8, 0f, 0.05f, 0.10f, 0.25f, 0.40f, 0.20f);

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DEV][CONFIG_ASSET] ensured {assetPath} entries={asset.levels.Count}");
    }
}
#endif
