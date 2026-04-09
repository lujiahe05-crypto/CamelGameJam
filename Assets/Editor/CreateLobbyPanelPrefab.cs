using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CreateLobbyPanelPrefab
{
    [InitializeOnLoadMethod]
    static void AutoCreate()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/LobbyPanel.prefab") == null)
            Create();
    }

    public static void Create()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        var root = CreateUIObject("LobbyPanel", null);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.sizeDelta = Vector2.zero;
        root.anchoredPosition = Vector2.zero;

        // Title
        var titleRect = CreateUIObject("Title", root.transform);
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0, 200);
        titleRect.sizeDelta = new Vector2(800, 120);
        AddText(titleRect.gameObject, "GAME LOBBY", 72, new Color(0.9f, 0.9f, 1f));
        AddOutline(titleRect.gameObject, new Color(0, 0, 0, 0.8f), new Vector2(2, -2));

        // Subtitle
        var subRect = CreateUIObject("Subtitle", root.transform);
        subRect.anchorMin = new Vector2(0.5f, 0.5f);
        subRect.anchorMax = new Vector2(0.5f, 0.5f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.anchoredPosition = new Vector2(0, 130);
        subRect.sizeDelta = new Vector2(800, 50);
        AddText(subRect.gameObject, "Select a game to play", 28, new Color(0.6f, 0.6f, 0.8f));
        AddOutline(subRect.gameObject, new Color(0, 0, 0, 0.8f), new Vector2(2, -2));

        // Snake button
        CreateGameButton(root.transform, "SnakeButton", new Vector2(0, 20),
            new Color(0.15f, 0.5f, 0.15f), new Color(0.2f, 0.65f, 0.2f),
            new Color(0.24f, 0.78f, 0.24f), "SNAKE");

        // Tetris button
        CreateGameButton(root.transform, "TetrisButton", new Vector2(0, -100),
            new Color(0.15f, 0.15f, 0.5f), new Color(0.2f, 0.2f, 0.65f),
            new Color(0.24f, 0.24f, 0.78f), "TETRIS");

        // Raft button
        CreateGameButton(root.transform, "RaftButton", new Vector2(0, -220),
            new Color(0.1f, 0.35f, 0.55f), new Color(0.15f, 0.45f, 0.7f),
            new Color(0.18f, 0.54f, 0.84f), "RAFT SURVIVAL");

        // Save
        string path = "Assets/Resources/UI/LobbyPanel.prefab";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.Refresh();
        Debug.Log("LobbyPanel prefab created at " + path);
    }

    static void CreateGameButton(Transform parent, string name, Vector2 pos,
        Color normal, Color highlighted, Color pressed, string label)
    {
        var btnRect = CreateUIObject(name, parent);
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = pos;
        btnRect.sizeDelta = new Vector2(400, 90);

        var img = AddImage(btnRect.gameObject, normal);

        var btn = btnRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlighted;
        colors.pressedColor = pressed;
        colors.selectedColor = normal;
        btn.colors = colors;

        // Label
        var labelRect = CreateUIObject("Label", btnRect.transform);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        AddText(labelRect.gameObject, label, 36, Color.white);
        AddOutline(labelRect.gameObject, new Color(0, 0, 0, 0.6f), new Vector2(2, -2));
    }

    static RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = 5;
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Font GetBuiltinFont()
    {
        // Unity built-in Arial
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            font = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
        if (font == null)
            Debug.LogError("Cannot find built-in Arial font!");
        return font;
    }

    static Text AddText(GameObject go, string content, int fontSize, Color color)
    {
        go.AddComponent<CanvasRenderer>();
        var text = go.AddComponent<Text>();
        text.font = GetBuiltinFont();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        return text;
    }

    static Image AddImage(GameObject go, Color color)
    {
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static Outline AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        return outline;
    }
}
