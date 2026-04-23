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

    static bool sceneHookInstalled;

    GameObject portalA;
    GameObject portalB;
    Transform playerRoot;
    CharacterController playerController;
    bool isTeleporting;
    GameObject blockedPortal;
    CanvasGroup fadeCanvasGroup;

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

        if (portalA == null || portalB == null || playerRoot == null)
        {
            blockedPortal = null;
            return;
        }

        if (isTeleporting)
            return;

        if (blockedPortal != null)
        {
            if (IsInsidePortalArea(blockedPortal, playerRoot.position))
                return;

            blockedPortal = null;
        }

        bool insidePortalA = IsInsidePortalArea(portalA, playerRoot.position);
        bool insidePortalB = IsInsidePortalArea(portalB, playerRoot.position);

        if (insidePortalA)
        {
            StartCoroutine(TeleportWithFade(portalB));
            return;
        }

        if (insidePortalB)
            StartCoroutine(TeleportWithFade(portalA));
    }

    void TryResolvePortals()
    {
        if (portalA == null)
            portalA = GameObject.Find(PortalAName);

        if (portalB == null)
            portalB = GameObject.Find(PortalBName);
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
            return;

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
