using UnityEngine;
using UnityEngine.UI;

public class GameJamInteractionUI : MonoBehaviour
{
    GameObject canvasGo;
    GameObject promptGo;
    Text promptText;

    void Start()
    {
        canvasGo = new GameObject("InteractionCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        promptGo = new GameObject("Prompt");
        promptGo.transform.SetParent(canvasGo.transform, false);

        var bg = promptGo.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);

        var rect = promptGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.15f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260, 50);
        rect.anchoredPosition = Vector2.zero;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(promptGo.transform, false);
        promptText = txtGo.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 26;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        promptGo.SetActive(false);
    }

    public void Show(string message, bool raw = false)
    {
        promptText.text = raw ? message : $"[E] 采集 {message}";
        promptGo.SetActive(true);
    }

    public void Hide()
    {
        if (promptGo != null) promptGo.SetActive(false);
    }

    void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }
}
