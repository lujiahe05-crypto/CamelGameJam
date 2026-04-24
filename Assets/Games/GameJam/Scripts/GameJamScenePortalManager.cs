using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameJamScenePortalManager : MonoBehaviour
{
    const string ManagerName = "GameJamScenePortalManager";
    const string PortalAName = "testdoor";
    const string PortalBName = "testPortalExit";

    [SerializeField] float activationPadding = 0.9f;
    [SerializeField] float destinationYOffset = 0.05f;
    [SerializeField] float fadeOutDuration = 0.5f;
    [SerializeField] float fadeInDuration = 0.5f;
    [SerializeField] float blackScreenHoldDuration = 0.5f;
    [SerializeField] float teleportCameraDistanceScale = 0.3333f;
    [SerializeField] float cameraSettleTimeout = 2f;
    [SerializeField] string portalALabel = "传送门";
    [SerializeField] string portalBLabel = "传送门";

    static bool sceneHookInstalled;

    GameObject portalA;
    GameObject portalB;
    Canvas portalALabelCanvas;
    Canvas portalBLabelCanvas;
    Transform playerRoot;
    CharacterController playerController;
    bool isTeleporting;
    GameObject blockedPortal;
    CanvasGroup fadeCanvasGroup;
    Canvas promptCanvas;
    Text promptText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallSceneHook()
    {
        if (sceneHookInstalled)
            return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneHookInstalled = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootstrapCurrentScene()
    {
        EnsureManager(SceneManager.GetActiveScene());
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager(scene);
    }

    static void EnsureManager(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].GetComponent<GameJamScenePortalManager>() != null)
                return;
        }

        var go = new GameObject(ManagerName);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<GameJamScenePortalManager>();
    }

    void Update()
    {
        TryResolvePortals();
        TryResolvePlayer();

        if (Camera.main != null)
        {
            var camRot = Camera.main.transform.rotation;
            if (portalALabelCanvas != null)
                portalALabelCanvas.transform.rotation = camRot;
            if (portalBLabelCanvas != null)
                portalBLabelCanvas.transform.rotation = camRot;
        }

        if (portalA == null || portalB == null || playerRoot == null)
        {
            blockedPortal = null;
            SetPromptVisible(false);
            return;
        }

        if (isTeleporting)
        {
            SetPromptVisible(false);
            return;
        }

        if (blockedPortal != null)
        {
            if (IsInsidePortalArea(blockedPortal, playerRoot.position))
            {
                SetPromptVisible(false);
                return;
            }

            blockedPortal = null;
        }

        bool insidePortalA = IsInsidePortalArea(portalA, playerRoot.position);
        bool insidePortalB = IsInsidePortalArea(portalB, playerRoot.position);

        if (insidePortalA)
        {
            SetPromptVisible(true);
            if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(TeleportWithFade(portalB));
            return;
        }

        if (insidePortalB)
        {
            SetPromptVisible(true);
            if (Input.GetKeyDown(KeyCode.E))
                StartCoroutine(TeleportWithFade(portalA));
            return;
        }

        SetPromptVisible(false);
    }

    void TryResolvePortals()
    {
        if (portalA == null)
        {
            portalA = GameObject.Find(PortalAName);
            if (portalA != null)
                portalALabelCanvas = AddFloatingLabel(portalA, portalALabel);
        }

        if (portalB == null)
        {
            portalB = GameObject.Find(PortalBName);
            if (portalB != null)
                portalBLabelCanvas = AddFloatingLabel(portalB, portalBLabel);
        }
    }

    Canvas AddFloatingLabel(GameObject target, string text)
    {
        var canvasGo = new GameObject("Label");
        canvasGo.transform.SetParent(target.transform);
        canvasGo.transform.localPosition = new Vector3(0, 2f, 0);
        canvasGo.transform.localScale = Vector3.one * 0.02f;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var labelText = textGo.AddComponent<Text>();
        labelText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28);
        labelText.fontSize = 28;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.color = new Color(0.4f, 0.9f, 1f);
        labelText.text = text;
        labelText.fontStyle = FontStyle.Bold;

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return canvas;
    }

    void TryResolvePlayer()
    {
        if (playerRoot != null)
            return;

        var controller = FindObjectOfType<GameJamPlayerController>();
        if (controller != null)
        {
            playerRoot = controller.transform;
            playerController = controller.GetComponent<CharacterController>();
            return;
        }

        var cc = FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            playerRoot = cc.transform;
            playerController = cc;
            return;
        }

        GameObject playerByTag = null;
        try
        {
            playerByTag = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            playerByTag = null;
        }

        if (playerByTag != null)
        {
            playerRoot = playerByTag.transform;
            playerController = playerByTag.GetComponent<CharacterController>();
            return;
        }

        var playerByName = GameObject.Find("Player");
        if (playerByName != null)
        {
            playerRoot = playerByName.transform;
            playerController = playerByName.GetComponent<CharacterController>();
        }
    }

    bool IsInsidePortalArea(GameObject portal, Vector3 playerPosition)
    {
        if (portal == null)
            return false;

        if (TryGetCombinedBounds(portal, out var bounds))
        {
            bounds.Expand(new Vector3(activationPadding * 2f, 2f, activationPadding * 2f));
            return bounds.Contains(playerPosition);
        }

        return Vector3.Distance(playerPosition, portal.transform.position) <= activationPadding;
    }

    static bool TryGetCombinedBounds(GameObject go, out Bounds bounds)
    {
        var colliders = go.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;
        bounds = new Bounds();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].enabled)
                continue;

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (hasBounds)
            return true;

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!renderers[i].enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return hasBounds;
    }

    System.Collections.IEnumerator TeleportWithFade(GameObject toPortal)
    {
        if (playerRoot == null || toPortal == null)
            yield break;

        isTeleporting = true;
        EnsureFadeOverlay();
        SetPromptVisible(false);

        yield return FadeOverlay(0f, 1f, fadeOutDuration);
        yield return WaitForSecondsRealtimeSafe(blackScreenHoldDuration);

        TeleportPlayerInstant(toPortal);
        var follow = ApplyTeleportCameraState();
        blockedPortal = toPortal;

        yield return WaitForCameraSettle(follow, cameraSettleTimeout);
        yield return FadeOverlay(1f, 0f, fadeInDuration);

        isTeleporting = false;
    }

    void TeleportPlayerInstant(GameObject targetPortal)
    {
        if (playerRoot == null || targetPortal == null)
            return;

        Vector3 targetPosition = targetPortal.transform.position + Vector3.up * destinationYOffset;

        bool reenableCharacterController = playerController != null && playerController.enabled;
        if (reenableCharacterController)
            playerController.enabled = false;

        playerRoot.position = targetPosition;

        if (reenableCharacterController)
            playerController.enabled = true;
    }

    GameJamCamera ApplyTeleportCameraState()
    {
        var cam = Camera.main;
        if (cam == null)
            return null;

        var follow = cam.GetComponent<GameJamCamera>();
        if (follow == null)
            return null;

        follow.ApplyTeleportZoom(teleportCameraDistanceScale);
        return follow;
    }

    void EnsureFadeOverlay()
    {
        if (fadeCanvasGroup != null)
        {
            EnsurePromptOverlay();
            return;
        }

        var canvasGo = new GameObject("PortalFadeCanvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        canvasGo.AddComponent<GraphicRaycaster>();

        var group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        var image = imageGo.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeCanvasGroup = group;
        EnsurePromptOverlay();
    }

    void EnsurePromptOverlay()
    {
        if (promptCanvas != null && promptText != null)
            return;

        var promptCanvasGo = new GameObject("PortalPromptCanvas");
        promptCanvasGo.transform.SetParent(transform, false);

        promptCanvas = promptCanvasGo.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        promptCanvas.sortingOrder = 5001;

        promptCanvasGo.AddComponent<GraphicRaycaster>();

        var promptGo = new GameObject("PromptText");
        promptGo.transform.SetParent(promptCanvasGo.transform, false);

        promptText = promptGo.AddComponent<Text>();
        promptText.text = "\u6309E\u5f00\u95e8";
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 36;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        promptText.raycastTarget = false;
        promptText.enabled = false;

        var promptRect = promptText.rectTransform;
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 120f);
        promptRect.sizeDelta = new Vector2(320f, 60f);
    }

    void SetPromptVisible(bool visible)
    {
        EnsurePromptOverlay();
        if (promptText == null)
            return;

        promptText.enabled = visible;
    }

    System.Collections.IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
            yield break;

        fadeCanvasGroup.alpha = from;
        if (duration <= 0f)
        {
            fadeCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        fadeCanvasGroup.alpha = to;
    }

    static System.Collections.IEnumerator WaitForSecondsRealtimeSafe(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static System.Collections.IEnumerator WaitForCameraSettle(GameJamCamera follow, float timeout)
    {
        if (follow == null)
            yield break;

        float elapsed = 0f;
        float safeTimeout = Mathf.Max(0.1f, timeout);
        while (!follow.IsCloseToDesiredPose())
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= safeTimeout)
                yield break;

            yield return null;
        }
    }
}
