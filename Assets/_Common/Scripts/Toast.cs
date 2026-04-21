using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Toast : MonoBehaviour
{
    static Toast instance;

    CanvasGroup canvasGroup;
    Text label;
    Coroutine routine;

    public static void ShowToast(string msg)
    {
        if (instance == null) CreateInstance();
        instance.Show(msg);
    }

    static void CreateInstance()
    {
        var canvas = FindToastCanvas();

        var go = new GameObject("Toast");
        go.transform.SetParent(canvas.transform, false);
        instance = go.AddComponent<Toast>();

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.75f);
        rect.anchorMax = new Vector2(0.5f, 0.75f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360, 70);
        rect.anchoredPosition = Vector2.zero;

        instance.canvasGroup = go.AddComponent<CanvasGroup>();

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        instance.label = txtGo.AddComponent<Text>();
        instance.label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instance.label.fontSize = 32;
        instance.label.alignment = TextAnchor.MiddleCenter;
        instance.label.color = Color.white;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        go.SetActive(false);
    }

    static Canvas FindToastCanvas()
    {
        foreach (var c in FindObjectsOfType<Canvas>())
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                return c;

        var canvasGo = new GameObject("ToastCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        return canvas;
    }

    void Show(string msg)
    {
        label.text = msg;
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(1.5f);
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - t / 0.5f;
            yield return null;
        }
        gameObject.SetActive(false);
        routine = null;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
