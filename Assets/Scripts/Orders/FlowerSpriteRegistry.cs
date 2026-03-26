using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在 Inspector 中拖入 Assets/Prefabs 下的花朵预制体，运行时按预制体名称查找 Sprite（根物体上的 SpriteRenderer）。
/// </summary>
public class FlowerSpriteRegistry : MonoBehaviour
{

    [SerializeField] GameObject[] flowerPrefabs;

    readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        RebuildCache();
    }

    public void RebuildCache()
    {
        _sprites.Clear();
        if (flowerPrefabs == null)
            return;

        foreach (var prefab in flowerPrefabs)
        {
            if (prefab == null)
                continue;

            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"[FlowerSpriteRegistry] 预制体「{prefab.name}」没有 SpriteRenderer 或 Sprite，已跳过。");
                continue;
            }

            _sprites[prefab.name] = sr.sprite;
        }
    }

    public bool TryGetSprite(string flowerPrefabName, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(flowerPrefabName))
            return false;

        string key = StripCloneSuffix(flowerPrefabName.Trim());
        return _sprites.TryGetValue(key, out sprite);
    }

    static string StripCloneSuffix(string name)
    {
        const string suffix = "(Clone)";
        if (name.EndsWith(suffix, StringComparison.Ordinal))
            return name.Substring(0, name.Length - suffix.Length).Trim();
        return name;
    }
}
