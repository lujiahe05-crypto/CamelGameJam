using UnityEngine;
using UnityEngine.UI;

public class GameJamPickupUI : MonoBehaviour
{
    GameObject canvasGo;
    GameObject promptGo;
    bool isShowing;

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        canvasGo = new GameObject("PickupPromptCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        promptGo = new GameObject("PickupPrompt");
        promptGo.transform.SetParent(canvasGo.transform, false);

        var bg = promptGo.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.75f);
        bg.raycastTarget = false;

        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.12f);
        promptRect.anchorMax = new Vector2(0.5f, 0.12f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(160, 50);
        promptRect.anchoredPosition = new Vector2(200, 0);

        var hlg = promptGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(12, 16, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // "E" key badge
        var keyBadge = new GameObject("KeyBadge");
        keyBadge.transform.SetParent(promptGo.transform, false);
        var badgeBG = keyBadge.AddComponent<Image>();
        badgeBG.color = new Color(0.95f, 0.95f, 0.97f);
        badgeBG.raycastTarget = false;
        var badgeRect = keyBadge.GetComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(34, 34);

        var keyText = new GameObject("KeyText");
        keyText.transform.SetParent(keyBadge.transform, false);
        var kt = keyText.AddComponent<Text>();
        kt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        kt.text = "E";
        kt.fontSize = 20;
        kt.fontStyle = FontStyle.Bold;
        kt.alignment = TextAnchor.MiddleCenter;
        kt.color = new Color(0.12f, 0.12f, 0.15f);
        kt.raycastTarget = false;
        var ktRect = keyText.GetComponent<RectTransform>();
        ktRect.anchorMin = Vector2.zero;
        ktRect.anchorMax = Vector2.one;
        ktRect.sizeDelta = Vector2.zero;

        // "采集" label
        var label = new GameObject("Label");
        label.transform.SetParent(promptGo.transform, false);
        var lt = label.AddComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.text = "采集";
        lt.fontSize = 22;
        lt.fontStyle = FontStyle.Bold;
        lt.alignment = TextAnchor.MiddleLeft;
        lt.color = new Color(0.95f, 0.95f, 0.97f);
        lt.raycastTarget = false;
        var ltRect = label.GetComponent<RectTransform>();
        ltRect.sizeDelta = new Vector2(60, 34);

        promptGo.SetActive(false);
    }

    public void Show()
    {
        if (!isShowing && promptGo != null)
        {
            promptGo.SetActive(true);
            isShowing = true;
        }
    }

    public void Hide()
    {
        if (isShowing && promptGo != null)
        {
            promptGo.SetActive(false);
            isShowing = false;
        }
    }

    public bool IsShowing => isShowing;

    void OnDestroy()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }
}
