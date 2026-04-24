using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameJamOffscreenIndicator : MonoBehaviour
{
    class Entry
    {
        public System.Func<Vector3?> getPos;
        public string label;
        public Color color;
        public RectTransform rootRect;
        public RectTransform arrowRect;
        public Text infoText;
    }

    static bool hookInstalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallHook()
    {
        if (hookInstalled) return;
        SceneManager.sceneLoaded += (scene, mode) => EnsureManager(scene);
        hookInstalled = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapCurrent() => EnsureManager(SceneManager.GetActiveScene());

    static void EnsureManager(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;
        foreach (var r in scene.GetRootGameObjects())
            if (r.GetComponent<GameJamOffscreenIndicator>() != null) return;
        var go = new GameObject("GameJamOffscreenIndicator");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<GameJamOffscreenIndicator>();
    }

    Canvas canvas;
    readonly List<Entry> entries = new List<Entry>();
    Transform playerCache;
    float playerSearchCd;

    void Start()
    {
        var canvasGo = new GameObject("OffscreenCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        AddEntry(() => GetGOPos("testdoor"), "传送门", new Color(0.4f, 0.9f, 1f));
    }

    static Vector3? GetGOPos(string objName)
    {
        var go = GameObject.Find(objName);
        return go != null ? (Vector3?)go.transform.position : null;
    }

    void AddEntry(System.Func<Vector3?> getPos, string label, Color color)
    {
        var e = new Entry { getPos = getPos, label = label, color = color };

        var root = new GameObject(label + "Indicator");
        root.transform.SetParent(canvas.transform, false);
        e.rootRect = root.AddComponent<RectTransform>();
        e.rootRect.sizeDelta = new Vector2(130, 36);
        e.rootRect.pivot = new Vector2(0.5f, 0.5f);
        e.rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        e.rootRect.anchorMax = new Vector2(0.5f, 0.5f);

        // Background
        var bg = new GameObject("BG").AddComponent<Image>();
        bg.transform.SetParent(root.transform, false);
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        var bgRect = bg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // Arrow character — rotated each frame to face target
        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(root.transform, false);
        var arrowText = arrowGo.AddComponent<Text>();
        arrowText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 22);
        arrowText.fontSize = 22;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = color;
        arrowText.text = "▶"; // ▶
        e.arrowRect = arrowText.rectTransform;
        e.arrowRect.anchorMin = new Vector2(0f, 0f);
        e.arrowRect.anchorMax = new Vector2(0f, 1f);
        e.arrowRect.pivot = new Vector2(0.5f, 0.5f);
        e.arrowRect.sizeDelta = new Vector2(30f, 0f);
        e.arrowRect.anchoredPosition = new Vector2(15f, 0f);

        // Label + distance text
        var infoGo = new GameObject("Info");
        infoGo.transform.SetParent(root.transform, false);
        e.infoText = infoGo.AddComponent<Text>();
        e.infoText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
        e.infoText.fontSize = 18;
        e.infoText.alignment = TextAnchor.MiddleLeft;
        e.infoText.color = Color.white;
        var infoRect = e.infoText.rectTransform;
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.offsetMin = new Vector2(32f, 0f);
        infoRect.offsetMax = new Vector2(-4f, 0f);

        root.SetActive(false);
        entries.Add(e);
    }

    void Update()
    {
        if (Camera.main == null) return;
        var player = GetPlayer();
        var playerPos = player != null ? player.position : Vector3.zero;
        foreach (var e in entries)
            Tick(e, playerPos);
    }

    void Tick(Entry e, Vector3 playerPos)
    {
        var worldPos = e.getPos();
        if (worldPos == null)
        {
            e.rootRect.gameObject.SetActive(false);
            return;
        }

        var vp = Camera.main.WorldToViewportPoint(worldPos.Value);
        bool onScreen = vp.z > 0f && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
        e.rootRect.gameObject.SetActive(!onScreen);
        if (onScreen) return;

        // Direction from viewport center toward target (flip if behind camera)
        var dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        if (vp.z < 0f) dir = -dir;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // Clamp to screen border with padding
        const float pad = 44f;
        float hw = Screen.width  * 0.5f - pad;
        float hh = Screen.height * 0.5f - pad;
        float sx = Mathf.Abs(dir.x) > 0.001f ? hw / Mathf.Abs(dir.x) : float.MaxValue;
        float sy = Mathf.Abs(dir.y) > 0.001f ? hh / Mathf.Abs(dir.y) : float.MaxValue;
        float s  = Mathf.Min(sx, sy);

        e.rootRect.anchoredPosition = new Vector2(dir.x * s, dir.y * s);

        // Rotate arrow to face target direction
        e.arrowRect.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        float dist = Vector3.Distance(playerPos, worldPos.Value);
        e.infoText.text = e.label + " " + Mathf.RoundToInt(dist) + "m";
    }

    Transform GetPlayer()
    {
        playerSearchCd -= Time.deltaTime;
        if (playerCache == null || playerSearchCd <= 0f)
        {
            playerSearchCd = 3f;
            var ctrl = FindObjectOfType<GameJamPlayerController>();
            if (ctrl != null) return playerCache = ctrl.transform;
            try
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) return playerCache = go.transform;
            }
            catch (UnityException) { }
        }
        return playerCache;
    }
}
