using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class GameJamResourceCopier
{
    const string IconDest = "Assets/Games/GameJam/Resources/ItemIcons";
    const string PrefabDest = "Assets/Games/GameJam/Resources/BuildingModels";

    [MenuItem("GameJam/Copy Referenced Assets to Resources")]
    public static void CopyAll()
    {
        CopyItemIcons();
        CopyBuildingPrefabs();
        AssetDatabase.Refresh();
        Debug.Log("[GameJamResourceCopier] Done.");
    }

    static void CopyItemIcons()
    {
        EnsureDir(IconDest);
        var table = LoadJson<PortiaItemTableData>("Assets/Games/GameJam/Resources/PortiaConfigs/ItemTable.json");
        if (table == null || table.items == null) return;

        int count = 0;
        foreach (var item in table.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.iconPath))
                continue;

            string srcPath = NormalizePath(item.iconPath);
            if (string.IsNullOrWhiteSpace(srcPath))
                continue;

            var sprite = FindSpriteAsset(srcPath);
            if (sprite == null) continue;

            string spritePath = AssetDatabase.GetAssetPath(sprite);
            string fileName = Path.GetFileName(spritePath);
            string destPath = IconDest + "/" + fileName;

            if (File.Exists(destPath)) continue;

            AssetDatabase.CopyAsset(spritePath, destPath);
            count++;
        }
        Debug.Log($"[GameJamResourceCopier] Copied {count} icon sprites to {IconDest}");
    }

    static void CopyBuildingPrefabs()
    {
        EnsureDir(PrefabDest);
        var table = LoadJson<PortiaBuildingTableData>("Assets/Games/GameJam/Resources/PortiaConfigs/BuildingTable.json");
        if (table == null || table.buildings == null) return;

        int count = 0;
        foreach (var b in table.buildings)
        {
            if (b == null || string.IsNullOrWhiteSpace(b.prefabPath))
                continue;

            string srcPath = b.prefabPath.Replace('\\', '/');
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
            if (prefab == null)
            {
                string fileName = Path.GetFileNameWithoutExtension(srcPath);
                var guids = AssetDatabase.FindAssets(fileName + " t:Prefab",
                    new[] { "Assets/Games/GameJam/assets/Model" });
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(p).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                        srcPath = p;
                        break;
                    }
                }
            }

            if (prefab == null) continue;

            string destFileName = Path.GetFileName(srcPath);
            string destPath = PrefabDest + "/" + destFileName;
            if (File.Exists(destPath)) continue;

            AssetDatabase.CopyAsset(srcPath, destPath);
            count++;
        }
        Debug.Log($"[GameJamResourceCopier] Copied {count} building prefabs to {PrefabDest}");
    }

    static Sprite FindSpriteAsset(string rawPath)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(rawPath);
        if (sprite != null) return sprite;

        string normalized = rawPath.Replace("/sprites/packageItem_", "/sprites/package/Item_")
                                   .Replace("/sprites/packageitem_", "/sprites/package/Item_");
        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(normalized);
        if (sprite != null) return sprite;

        if (string.IsNullOrEmpty(Path.GetExtension(normalized)))
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(normalized + ".png");
            if (sprite != null) return sprite;
        }

        string fileName = Path.GetFileNameWithoutExtension(normalized);
        if (fileName.StartsWith("packageItem_", System.StringComparison.OrdinalIgnoreCase))
            fileName = "Item_" + fileName.Substring("packageItem_".Length);

        var guids = AssetDatabase.FindAssets(fileName + " t:Sprite",
            new[] { "Assets/Games/GameJam/assets/UI/sprites", "Assets/Games/GameJam/assets/UI" });
        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(assetPath).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null) return sprite;
            }
        }

        return null;
    }

    static string NormalizePath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        string p = raw.Replace('\\', '/').Trim();
        if (!p.StartsWith("Assets/")) p = "Assets/" + p.TrimStart('/');
        return p;
    }

    static T LoadJson<T>(string assetPath) where T : class
    {
        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        if (textAsset == null) return null;
        return JsonUtility.FromJson<T>(textAsset.text);
    }

    static void EnsureDir(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    [System.Serializable]
    class PortiaItemTableData
    {
        public PortiaItemEntry[] items;
    }

    [System.Serializable]
    class PortiaItemEntry
    {
        public string itemId;
        public string iconPath;
    }

    [System.Serializable]
    class PortiaBuildingTableData
    {
        public PortiaBuildingEntry[] buildings;
    }

    [System.Serializable]
    class PortiaBuildingEntry
    {
        public string itemId;
        public string prefabPath;
    }
}
