using System;
using System.Collections.Generic;
using UnityEngine;


public class FlowerSpriteRegistry : MonoBehaviour
{

    [SerializeField] GameObject[] flowerPrefabs;

    readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

    /// <summary>获取所有鲜花预制体列表</summary>
    public GameObject[] GetAllFlowerPrefabs()
    {
        return flowerPrefabs;
    }

    /// <summary>获取所有鲜花名称列表</summary>
    public string[] GetAllFlowerNames()
    {
        if (flowerPrefabs == null) return new string[0];
        var names = new System.Collections.Generic.List<string>();
        foreach (var prefab in flowerPrefabs)
        {
            if (prefab != null)
                names.Add(prefab.name);
        }
        return names.ToArray();
    }

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
