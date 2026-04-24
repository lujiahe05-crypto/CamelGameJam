using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public static class GameJamArtLoader
{
    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    public static void ClearIcon(Image image)
    {
        if (image == null)
            return;

        image.sprite = null;
        image.color = Color.clear;
        image.preserveAspect = true;
    }

    public static void ApplyItemIcon(Image image, string itemId, Color fallbackColor)
    {
        if (image == null)
            return;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            ClearIcon(image);
            return;
        }

        var itemDef = GameJamItemDB.Get(itemId);
        var sprite = itemDef != null ? LoadSpriteByName(itemDef.iconName, itemDef.iconPath) : null;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            return;
        }

        image.sprite = null;
        image.color = itemDef != null ? itemDef.iconColor : fallbackColor;
        image.preserveAspect = true;
    }

    public static Sprite LoadSprite(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        if (spriteCache.TryGetValue(rawPath, out var cached))
            return cached;

        foreach (var candidate in BuildCandidatePaths(rawPath, ".png"))
        {
            var sprite = LoadAsset<Sprite>(candidate);
            if (sprite == null)
                sprite = LoadSpriteFromTexture(candidate);
            if (sprite != null)
            {
                spriteCache[rawPath] = sprite;
                return sprite;
            }
        }

        var spriteByName = FindSpriteByFileName(rawPath);
        if (spriteByName != null)
        {
            spriteCache[rawPath] = spriteByName;
            return spriteByName;
        }

        spriteCache[rawPath] = null;
        return null;
    }

    public static GameObject LoadPrefab(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        if (prefabCache.TryGetValue(rawPath, out var cached))
            return cached;

        foreach (var candidate in BuildCandidatePaths(rawPath, ".prefab"))
        {
            var prefab = LoadAsset<GameObject>(candidate);
            if (prefab != null)
            {
                prefabCache[rawPath] = prefab;
                return prefab;
            }
        }

        var prefabByName = FindPrefabByFileName(rawPath);
        if (prefabByName != null)
        {
            prefabCache[rawPath] = prefabByName;
            return prefabByName;
        }

        prefabCache[rawPath] = null;
        return null;
    }

    public static GameObject LoadPrefabByName(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return null;

        var prefab = Resources.Load<GameObject>("Prefab/" + prefabName);
        if (prefab != null)
            return prefab;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets(prefabName + " t:Prefab");
        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!System.IO.Path.GetFileNameWithoutExtension(assetPath)
                    .Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            string destDir = "Assets/Games/GameJam/assets/Resources/Prefab";
            if (!System.IO.Directory.Exists(destDir))
                System.IO.Directory.CreateDirectory(destDir);

            string destPath = destDir + "/" + System.IO.Path.GetFileName(assetPath);
            if (!System.IO.File.Exists(destPath))
            {
                UnityEditor.AssetDatabase.CopyAsset(assetPath, destPath);
                UnityEditor.AssetDatabase.Refresh();
            }

            prefab = Resources.Load<GameObject>("Prefab/" + prefabName);
            if (prefab != null) return prefab;
        }
#endif

        return null;
    }

    public static GameObject InstantiatePrefabByName(string prefabName)
    {
        var prefab = LoadPrefabByName(prefabName);
        return prefab != null ? Object.Instantiate(prefab) : null;
    }

    public static Sprite LoadSpriteByName(string iconName, string iconPath = null)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            return null;

        if (spriteCache.TryGetValue(iconName, out var cached))
            return cached;

        var sprite = Resources.Load<Sprite>("Icon/" + iconName);
        if (sprite != null)
        {
            spriteCache[iconName] = sprite;
            return sprite;
        }

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(iconPath))
        {
            string srcPath = iconPath.Replace('\\', '/');
            if (!srcPath.StartsWith("Assets/"))
                srcPath = "Assets/" + srcPath.TrimStart('/');
            if (string.IsNullOrEmpty(System.IO.Path.GetExtension(srcPath)))
                srcPath += ".png";

            var srcAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(srcPath);
            if (srcAsset != null)
            {
                string destDir = "Assets/Games/GameJam/assets/Resources/Icon";
                if (!System.IO.Directory.Exists(destDir))
                    System.IO.Directory.CreateDirectory(destDir);

                string destPath = destDir + "/" + System.IO.Path.GetFileName(srcPath);
                if (!System.IO.File.Exists(destPath))
                {
                    UnityEditor.AssetDatabase.CopyAsset(srcPath, destPath);
                    UnityEditor.AssetDatabase.Refresh();
                }

                sprite = Resources.Load<Sprite>("Icon/" + iconName);
                if (sprite != null)
                {
                    spriteCache[iconName] = sprite;
                    return sprite;
                }
            }
        }
#endif

        spriteCache[iconName] = null;
        return null;
    }

    public static GameObject InstantiatePrefab(string rawPath)
    {
        var prefab = LoadPrefab(rawPath);
        return prefab != null ? Object.Instantiate(prefab) : null;
    }

    public static void AlignObjectBaseToWorldY(GameObject go, float targetWorldY)
    {
        if (go == null)
            return;

        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float deltaY = targetWorldY - bounds.min.y;
        if (Mathf.Abs(deltaY) > 0.0001f)
            go.transform.position += new Vector3(0f, deltaY, 0f);
    }

    static IEnumerable<string> BuildCandidatePaths(string rawPath, string defaultExtension)
    {
        var results = new List<string>();
        if (string.IsNullOrWhiteSpace(rawPath))
            return results;

        string normalized = rawPath.Replace('\\', '/').Trim();
        normalized = normalized.Replace("/sprites/packageItem_", "/sprites/package/Item_");
        normalized = normalized.Replace("/sprites/packageitem_", "/sprites/package/Item_");
        normalized = normalized.Replace("/UI/sprites/packageItem_", "/UI/sprites/package/Item_");
        normalized = normalized.Replace("/UI/sprites/packageitem_", "/UI/sprites/package/Item_");

        AddCandidate(results, normalized, defaultExtension);

        string withAssets = normalized.StartsWith("Assets/") ? normalized : "Assets/" + normalized.TrimStart('/');
        AddCandidate(results, withAssets, defaultExtension);

        return results;
    }

    static void AddCandidate(List<string> results, string path, string defaultExtension)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        AddUnique(results, path);
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
            AddUnique(results, path + defaultExtension);
    }

    static void AddUnique(List<string> results, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string normalized = path.Replace('\\', '/');
        if (!results.Contains(normalized))
            results.Add(normalized);
    }

    static T LoadAsset<T>(string assetPath) where T : Object
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return null;

#if UNITY_EDITOR
        var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (editorAsset != null)
            return editorAsset;
#endif

        const string resourcesMarker = "/Resources/";
        int resourcesIndex = assetPath.IndexOf(resourcesMarker, System.StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex >= 0)
        {
            string resourcesPath = assetPath.Substring(resourcesIndex + resourcesMarker.Length);
            resourcesPath = Path.ChangeExtension(resourcesPath, null).Replace('\\', '/');
            var res = Resources.Load<T>(resourcesPath);
            if (res != null) return res;
        }

        string fileName = Path.GetFileNameWithoutExtension(assetPath.Replace('\\', '/'));
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var res = Resources.Load<T>(fileName);
            if (res != null) return res;

            var res2 = Resources.Load<T>("ItemIcons/" + fileName);
            if (res2 != null) return res2;

            var res3 = Resources.Load<T>("BuildingModels/" + fileName);
            if (res3 != null) return res3;
        }

        return null;
    }

    static Sprite LoadSpriteFromTexture(string assetPath)
    {
#if UNITY_EDITOR
        var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture != null)
        {
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }
#endif
        return null;
    }

    static Sprite FindSpriteByFileName(string rawPath)
    {
        string fileName = NormalizeFileName(rawPath, "Item_");
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets(fileName + " t:Sprite",
            new[] { "Assets/Games/GameJam/assets/UI/sprites", "Assets/Games/GameJam/assets/UI" });
        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(assetPath).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
                sprite = LoadSpriteFromTexture(assetPath);
            if (sprite != null)
                return sprite;
        }
#endif

        var res = Resources.Load<Sprite>(fileName);
        if (res != null) return res;
        res = Resources.Load<Sprite>("ItemIcons/" + fileName);
        if (res != null) return res;

        return null;
    }

    static GameObject FindPrefabByFileName(string rawPath)
    {
        string fileName = NormalizeFileName(rawPath, null);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

#if UNITY_EDITOR
        var guids = UnityEditor.AssetDatabase.FindAssets(fileName + " t:Prefab",
            new[] { "Assets/Games/GameJam/assets/Model" });
        foreach (var guid in guids)
        {
            string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileNameWithoutExtension(assetPath).Equals(fileName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            string destDir = "Assets/Games/GameJam/assets/Resources/Prefab";
            if (!System.IO.Directory.Exists(destDir))
                System.IO.Directory.CreateDirectory(destDir);
            string destPath = destDir + "/" + System.IO.Path.GetFileName(assetPath);
            if (!System.IO.File.Exists(destPath))
            {
                UnityEditor.AssetDatabase.CopyAsset(assetPath, destPath);
                UnityEditor.AssetDatabase.Refresh();
            }

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
                return prefab;
        }
#endif

        var res = Resources.Load<GameObject>("Prefab/" + fileName);
        if (res != null) return res;
        res = Resources.Load<GameObject>(fileName);
        if (res != null) return res;
        res = Resources.Load<GameObject>("BuildingModels/" + fileName);
        if (res != null) return res;

        return null;
    }

    static string NormalizeFileName(string rawPath, string itemPrefix)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return null;

        string normalized = rawPath.Replace('\\', '/').Trim();
        string fileName = Path.GetFileNameWithoutExtension(normalized);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        if (fileName.StartsWith("packageItem_", System.StringComparison.OrdinalIgnoreCase))
            fileName = "Item_" + fileName.Substring("packageItem_".Length);

        if (!string.IsNullOrWhiteSpace(itemPrefix) &&
            !fileName.StartsWith(itemPrefix, System.StringComparison.OrdinalIgnoreCase) &&
            !fileName.StartsWith("Item_", System.StringComparison.OrdinalIgnoreCase))
        {
            fileName = itemPrefix + fileName;
        }

        return fileName;
    }
}
