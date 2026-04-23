using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class GameJamLobby : MonoBehaviour
{
    GameObject canvasGo;

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
        }

        var btnGo = new GameObject("GameJamButton");
        btnGo.transform.SetParent(canvasGo.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.35f, 0.25f, 0.65f);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(400, 100);
        btnRect.anchoredPosition = Vector2.zero;

        var btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.35f, 0.25f, 0.65f);
        colors.highlightedColor = new Color(0.45f, 0.35f, 0.75f);
        colors.pressedColor = new Color(0.55f, 0.45f, 0.85f);
        btn.colors = colors;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(btnGo.transform, false);
        var txt = txtGo.AddComponent<Text>();
        txt.text = "Game Jam";
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 40;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        btn.onClick.AddListener(LaunchGame);
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
