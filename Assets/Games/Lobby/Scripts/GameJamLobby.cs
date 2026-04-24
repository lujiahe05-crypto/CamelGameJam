using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameJamLobby : MonoBehaviour
{
    const string BackgroundImagePath = "Games/GameJam/assets/UI/loadingmask/Standard.png";
    const string TitleImagePath = "Games/GameJam/assets/UI/sprites/loadingscreen/portia_zh_hans.png";
    const string StartButtonImagePath = "Games/GameJam/assets/UI/Texture2D/startui_board.png";

    GameObject canvasGo;
    Sprite backgroundSprite;
    Sprite titleSprite;
    Sprite startButtonSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (SceneManager.GetActiveScene().name != "GameJam") return;
        if (FindObjectOfType<GameJamLobby>() != null) return;
        var go = new GameObject("GameJamLobby");
        go.AddComponent<GameJamLobby>();
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        SetupCamera();
        CreateUI();
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        cam.transform.SetParent(null);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true;
        cam.orthographicSize = 5;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.031f, 0.035f, 0.039f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void CreateUI()
    {
        canvasGo = new GameObject("GameJamCanvas");
        DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGo);
        }

        backgroundSprite = LoadSpriteFromAssetPath(BackgroundImagePath);
        titleSprite = LoadSpriteFromAssetPath(TitleImagePath);
        startButtonSprite = LoadSpriteFromAssetPath(StartButtonImagePath);

        CreateBackground(canvasGo.transform);
        CreateTitle(canvasGo.transform);
        CreateStartButton(canvasGo.transform);
    }

    void CreateBackground(Transform parent)
    {
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(parent, false);

        var image = bgGo.AddComponent<Image>();
        image.sprite = backgroundSprite;
        image.color = Color.white;
        image.preserveAspect = false;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void CreateTitle(Transform parent)
    {
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(parent, false);

        var image = titleGo.AddComponent<Image>();
        image.sprite = titleSprite;
        image.color = Color.white;
        image.preserveAspect = true;

        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 2f / 3f);
        rect.anchorMax = new Vector2(0.5f, 2f / 3f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = titleSprite != null
            ? new Vector2(titleSprite.rect.width, titleSprite.rect.height)
            : new Vector2(720f, 180f);
    }

    void CreateStartButton(Transform parent)
    {
        var btnGo = new GameObject("GameJamButton");
        btnGo.transform.SetParent(parent, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.sprite = startButtonSprite;
        btnImg.color = Color.white;
        btnImg.preserveAspect = true;
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = startButtonSprite != null
            ? new Vector2(startButtonSprite.rect.width, startButtonSprite.rect.height)
            : new Vector2(420f, 120f);
        btnRect.anchoredPosition = Vector2.zero;

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = colors;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = "\u5f00\u59cb\u6e38\u620f";
        txt.font = LoadStartButtonFont();
        txt.fontSize = 40;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        txtRect.localRotation = Quaternion.Euler(0f, 0f, -5f);

        var outline = txtGo.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        btn.onClick.AddListener(LaunchGame);
    }

    static Font LoadStartButtonFont()
    {
        string[] fontNames = { "SimHei", "Microsoft YaHei", "Microsoft JhengHei", "Arial" };
        return Font.CreateDynamicFontFromOSFont(fontNames, 40);
    }

    static Sprite LoadSpriteFromAssetPath(string relativeAssetPath)
    {
        string fullPath = Path.Combine(Application.dataPath, relativeAssetPath);
        if (!File.Exists(fullPath))
            return null;

        byte[] bytes = File.ReadAllBytes(fullPath);
        if (bytes == null || bytes.Length == 0)
            return null;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            Object.Destroy(texture);
            return null;
        }

        texture.name = Path.GetFileNameWithoutExtension(relativeAssetPath);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    void LaunchGame()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        SceneManager.sceneLoaded += OnSceneMainLoaded;
        SceneManager.LoadScene("SceneMain");
    }

    void OnSceneMainLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneMainLoaded;
        var gameGo = new GameObject("GameJamGame");
        var game = gameGo.AddComponent<GameJamGame>();
        game.OnReturnToLobby = ReturnToLobby;
    }

    void ReturnToLobby()
    {
        SceneManager.sceneLoaded += OnLobbyLoaded;
        SceneManager.LoadScene("GameJam");
    }

    void OnLobbyLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLobbyLoaded;
        SetupCamera();
        if (canvasGo != null)
            canvasGo.SetActive(true);
        else
            CreateUI();
    }
}
