using UnityEngine;
using UnityEngine.UI;

public class ThronefallWaveWarning : MonoBehaviour
{
    Vector3 worldPosition;
    RectTransform rectTransform;
    RectTransform arrowRect;
    Text countText;
    Text iconText;
    Text arrowText;
    GameObject arrowGo;

    const float ScreenEdgePadding = 60f;
    const float HysteresisInner = 80f;
    bool wasOffScreen;

    public void Init(Vector3 spawnPos, int totalCount, string iconLabel, RectTransform parent)
    {
        worldPosition = spawnPos;

        rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = new Vector2(80, 50);

        // Background
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgGo.AddComponent<CanvasRenderer>();
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);
        bgImg.raycastTarget = false;

        // Icon text
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0);
        iconRect.anchorMax = new Vector2(0.5f, 1);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        iconGo.AddComponent<CanvasRenderer>();
        iconText = iconGo.AddComponent<Text>();
        iconText.text = iconLabel;
        iconText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        iconText.fontSize = 20;
        iconText.fontStyle = FontStyle.Bold;
        iconText.color = new Color(1f, 0.3f, 0.2f);
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.raycastTarget = false;

        // Count text
        var countGo = new GameObject("Count");
        countGo.transform.SetParent(transform, false);
        var countRect = countGo.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0.5f, 0);
        countRect.anchorMax = new Vector2(1, 1);
        countRect.sizeDelta = Vector2.zero;
        countRect.anchoredPosition = Vector2.zero;
        countGo.AddComponent<CanvasRenderer>();
        countText = countGo.AddComponent<Text>();
        countText.text = "x" + totalCount;
        countText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        countText.fontSize = 18;
        countText.color = Color.white;
        countText.alignment = TextAnchor.MiddleCenter;
        countText.raycastTarget = false;

        // Arrow indicator
        arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(transform, false);
        arrowRect = arrowGo.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(30, 30);
        arrowRect.anchoredPosition = new Vector2(0, -35);
        arrowGo.AddComponent<CanvasRenderer>();
        arrowText = arrowGo.AddComponent<Text>();
        arrowText.text = "\u25BC"; // down arrow triangle
        arrowText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        arrowText.fontSize = 24;
        arrowText.color = new Color(1f, 0.85f, 0.2f);
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.raycastTarget = false;
        arrowGo.SetActive(false);
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(worldPosition);
        bool behindCamera = screenPos.z < 0;

        if (behindCamera)
        {
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
        }

        float threshold = wasOffScreen ? HysteresisInner : ScreenEdgePadding;
        bool onScreen = !behindCamera &&
                        screenPos.x > threshold &&
                        screenPos.x < Screen.width - threshold &&
                        screenPos.y > threshold &&
                        screenPos.y < Screen.height - threshold;

        wasOffScreen = !onScreen;

        if (onScreen)
        {
            arrowGo.SetActive(false);
        }
        else
        {
            arrowGo.SetActive(true);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);

            screenPos.x = Mathf.Clamp(screenPos.x, ScreenEdgePadding, Screen.width - ScreenEdgePadding);
            screenPos.y = Mathf.Clamp(screenPos.y, ScreenEdgePadding, Screen.height - ScreenEdgePadding);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRect.localRotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // Convert screen position to canvas anchored position (1920x1080 reference)
        float canvasX = (screenPos.x / Screen.width) * 1920f - 960f;
        float canvasY = (screenPos.y / Screen.height) * 1080f - 540f;
        rectTransform.anchoredPosition = new Vector2(canvasX, canvasY);
    }
}
