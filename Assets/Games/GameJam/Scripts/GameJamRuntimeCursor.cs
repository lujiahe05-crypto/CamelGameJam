using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameJamRuntimeCursor : MonoBehaviour
{
    const float RefreshInterval = 0.25f;

    static bool installed;
    static Texture2D cursorTexture;
    static readonly string[] CursorCandidatePaths = new[]
    {
        "Games/GameJam/assets/UI/Texture2D/MouseIcon.png",
        "Games/GameJam/Resources/GameJamUI/MouseIcon.png",
    };
    float nextRefreshTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (installed)
            return;

        EnsureCursorObject();
        SceneManager.sceneLoaded += OnSceneLoaded;
        installed = true;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCursor();
    }

    static void EnsureCursorObject()
    {
        if (FindObjectOfType<GameJamRuntimeCursor>() != null)
        {
            ApplyCursor();
            return;
        }

        var go = new GameObject("GameJamRuntimeCursor");
        DontDestroyOnLoad(go);
        go.AddComponent<GameJamRuntimeCursor>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyCursor();
    }

    void OnEnable()
    {
        ApplyCursor();
    }

    void LateUpdate()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + RefreshInterval;
        ApplyCursor();
    }

    static void ApplyCursor()
    {
        if (cursorTexture == null)
            cursorTexture = LoadCursorTexture();

        Cursor.visible = true;

        if (cursorTexture == null)
            return;

        var hotspot = Vector2.zero;
        Cursor.SetCursor(cursorTexture, hotspot, CursorMode.ForceSoftware);
    }

    static Texture2D LoadCursorTexture()
    {
        var loaded = Resources.Load<Texture2D>("GameJamUI/MouseIcon");
        if (loaded != null)
        {
            if (loaded.isReadable && loaded.format == TextureFormat.RGBA32)
                return loaded;

            var rt = RenderTexture.GetTemporary(loaded.width, loaded.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(loaded, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var readable = new Texture2D(loaded.width, loaded.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, loaded.width, loaded.height), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            readable.name = "GameJamRuntimeMouseIcon";
            readable.filterMode = FilterMode.Bilinear;
            readable.wrapMode = TextureWrapMode.Clamp;
            return readable;
        }

        string assetsRoot = Application.dataPath;
        for (int i = 0; i < CursorCandidatePaths.Length; i++)
        {
            string fullPath = Path.Combine(assetsRoot, CursorCandidatePaths[i]);
            if (!File.Exists(fullPath))
                continue;

            byte[] bytes = File.ReadAllBytes(fullPath);
            if (bytes == null || bytes.Length == 0)
                continue;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                continue;
            }

            texture.name = "GameJamRuntimeMouseIcon";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        return null;
    }
}
