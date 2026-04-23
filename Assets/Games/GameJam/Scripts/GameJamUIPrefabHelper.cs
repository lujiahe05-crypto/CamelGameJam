using UnityEngine;

public static class GameJamUIPrefabHelper
{
    const string PrefabFolder = "GameJamUI";
    const string LegacyPrefabFolder = "UI";

    public static GameObject TryLoadPrefab(string canvasName)
    {
        var prefab = Resources.Load<GameObject>(PrefabFolder + "/" + canvasName);
        if (prefab == null)
            prefab = Resources.Load<GameObject>(LegacyPrefabFolder + "/" + canvasName);
        return prefab != null ? Object.Instantiate(prefab) : null;
    }

    public static void SavePrefab(GameObject instance, string canvasName)
    {
#if UNITY_EDITOR
        string dir = "Assets/Games/GameJam/Resources/" + PrefabFolder;
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(instance, dir + "/" + canvasName + ".prefab");
        Debug.Log($"[UIPrefab] Saved prefab: {dir}/{canvasName}.prefab");
#endif
    }
}
