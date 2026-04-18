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
        cam.backgroundColor = new Color(0.031f, 0.035f, 0.039f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void CreateBackground()
    {
        if (bgContainer != null) Destroy(bgContainer);
        bgContainer = new GameObject("LobbyBG");

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
                    ? new Color(0.035f, 0.039f, 0.043f)
                    : new Color(0.043f, 0.047f, 0.051f);
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

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        var prefab = Resources.Load<GameObject>("UI/LobbyPanel");
        var panel = Instantiate(prefab, canvasGo.transform, false);

        panel.transform.Find("SnakeButton").GetComponent<Button>()
            .onClick.AddListener(() => LaunchGame<SnakeGame>());
        panel.transform.Find("TetrisButton").GetComponent<Button>()
            .onClick.AddListener(() => LaunchGame<TetrisGame>());
        panel.transform.Find("RaftButton").GetComponent<Button>()
            .onClick.AddListener(() => LaunchGame<RaftGame>());
        panel.transform.Find("ThronefallButton").GetComponent<Button>()
            .onClick.AddListener(() => LaunchGame<ThronefallGame>());
    }

    void LaunchGame<T>() where T : MonoBehaviour
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        if (bgContainer != null) bgContainer.SetActive(false);

        var gameGo = new GameObject(typeof(T).Name);
        var game = gameGo.AddComponent<T>();

        if (game is SnakeGame snakeGame)
            snakeGame.OnReturnToLobby = () => ReturnToLobby();
        else if (game is TetrisGame tetrisGame)
            tetrisGame.OnReturnToLobby = () => ReturnToLobby();
        else if (game is RaftGame raftGame)
            raftGame.OnReturnToLobby = () => ReturnToLobby();
        else if (game is ThronefallGame thronefallGame)
            thronefallGame.OnReturnToLobby = () => ReturnToLobby();
    }

    void ReturnToLobby()
    {
        SetupCamera();
        if (canvasGo != null) canvasGo.SetActive(true);
        if (bgContainer != null) bgContainer.SetActive(true);
    }
}
