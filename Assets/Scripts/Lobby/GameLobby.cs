using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameLobby : MonoBehaviour
{
    GameObject canvasGo;
    Sprite pixelSprite;
    GameObject bgContainer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (FindObjectOfType<GameLobby>() != null) return;
        var go = new GameObject("GameLobby");
        go.AddComponent<GameLobby>();
    }

    void Start()
    {
        CreatePixelSprite();
        ShowLobby();
    }

    void CreatePixelSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }

    void ShowLobby()
    {
        SetupCamera();
        CreateBackground();
        CreateLobbyUI();
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        cam.transform.SetParent(null);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void CreateBackground()
    {
        if (bgContainer != null) Destroy(bgContainer);
        bgContainer = new GameObject("LobbyBG");

        // Decorative grid background
        for (int x = -10; x <= 10; x++)
        {
            for (int y = -6; y <= 6; y++)
            {
                var go = new GameObject("BG");
                go.transform.SetParent(bgContainer.transform);
                go.transform.position = new Vector3(x, y, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pixelSprite;
                sr.sortingOrder = -1;
                sr.color = (x + y) % 2 == 0
                    ? new Color(0.11f, 0.11f, 0.16f)
                    : new Color(0.13f, 0.13f, 0.18f);
            }
        }
    }

    void CreateLobbyUI()
    {
        if (canvasGo != null) Destroy(canvasGo);

        canvasGo = new GameObject("LobbyCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // EventSystem is required for UI interaction
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        // Title
        CreateText(canvasGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 200), new Vector2(800, 120),
            72, new Color(0.9f, 0.9f, 1f), TextAnchor.MiddleCenter,
            "GAME LOBBY");

        // Subtitle
        CreateText(canvasGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 130), new Vector2(800, 50),
            28, new Color(0.6f, 0.6f, 0.8f), TextAnchor.MiddleCenter,
            "Select a game to play");

        // Snake button
        CreateButton(canvasGo.transform, "Snake",
            new Vector2(0, 20),
            new Color(0.15f, 0.5f, 0.15f), new Color(0.2f, 0.65f, 0.2f),
            "SNAKE",
            () => LaunchGame<SnakeGame>());

        // Tetris button
        CreateButton(canvasGo.transform, "Tetris",
            new Vector2(0, -100),
            new Color(0.15f, 0.15f, 0.5f), new Color(0.2f, 0.2f, 0.65f),
            "TETRIS",
            () => LaunchGame<TetrisGame>());

        // Raft button
        CreateButton(canvasGo.transform, "Raft",
            new Vector2(0, -220),
            new Color(0.1f, 0.35f, 0.55f), new Color(0.15f, 0.45f, 0.7f),
            "RAFT SURVIVAL",
            () => LaunchGame<RaftGame>());
    }

    void CreateButton(Transform parent, string name, Vector2 pos,
        Color normalColor, Color hoverColor, string label,
        UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = new GameObject(name + "Button");
        btnGo.transform.SetParent(parent, false);

        var image = btnGo.AddComponent<Image>();
        image.color = normalColor;

        var rect = btnGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(400, 90);

        var button = btnGo.AddComponent<Button>();
        button.targetGraphic = image;

        // Button color transitions
        var colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = hoverColor * 1.2f;
        colors.selectedColor = normalColor;
        button.colors = colors;

        button.onClick.AddListener(onClick);

        // Button label
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(btnGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
        text.fontSize = 36;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.text = label;

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Outline
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(2, -2);
    }

    Text CreateText(Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, Color color, TextAnchor alignment, string content)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.text = content;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(2, -2);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return text;
    }

    void LaunchGame<T>() where T : MonoBehaviour
    {
        // Hide lobby
        if (canvasGo != null) canvasGo.SetActive(false);
        if (bgContainer != null) bgContainer.SetActive(false);

        // Create game
        var gameGo = new GameObject(typeof(T).Name);
        var game = gameGo.AddComponent<T>();

        // Set return callback
        if (game is SnakeGame snakeGame)
            snakeGame.OnReturnToLobby = () => ReturnToLobby();
        else if (game is TetrisGame tetrisGame)
            tetrisGame.OnReturnToLobby = () => ReturnToLobby();
        else if (game is RaftGame raftGame)
            raftGame.OnReturnToLobby = () => ReturnToLobby();
    }

    void ReturnToLobby()
    {
        // Re-show lobby
        SetupCamera();
        if (canvasGo != null) canvasGo.SetActive(true);
        if (bgContainer != null) bgContainer.SetActive(true);
    }
}
