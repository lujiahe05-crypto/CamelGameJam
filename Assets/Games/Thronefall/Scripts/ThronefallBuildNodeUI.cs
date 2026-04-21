using UnityEngine;
using UnityEngine.UI;

public class ThronefallBuildNodeUI : MonoBehaviour
{
    Canvas canvas;
    CanvasGroup canvasGroup;
    Text iconText;
    Text priceText;
    GameObject hpBarBG;
    Image hpBarFill;
    CanvasGroup hpBarGroup;

    bool initialized;

    public void Init(Transform nodeTransform)
    {
        var canvasGo = new GameObject("NodeUI");
        canvasGo.transform.SetParent(nodeTransform, false);
        canvasGo.transform.localPosition = new Vector3(0, 3f, 0);

        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        var rt = canvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 80);
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        canvasGroup = canvasGo.AddComponent<CanvasGroup>();

        // Status icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(canvasGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(1, 1);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        iconGo.AddComponent<CanvasRenderer>();
        iconText = iconGo.AddComponent<Text>();
        iconText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        iconText.fontSize = 24;
        iconText.fontStyle = FontStyle.Bold;
        iconText.color = Color.white;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.raycastTarget = false;

        var iconOutline = iconGo.AddComponent<Outline>();
        iconOutline.effectColor = new Color(0, 0, 0, 0.8f);
        iconOutline.effectDistance = new Vector2(1, -1);

        // Price text
        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(canvasGo.transform, false);
        var priceRect = priceGo.AddComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0, 0);
        priceRect.anchorMax = new Vector2(1, 0.5f);
        priceRect.offsetMin = Vector2.zero;
        priceRect.offsetMax = Vector2.zero;
        priceGo.AddComponent<CanvasRenderer>();
        priceText = priceGo.AddComponent<Text>();
        priceText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        priceText.fontSize = 18;
        priceText.color = new Color(1f, 0.85f, 0.2f);
        priceText.alignment = TextAnchor.MiddleCenter;
        priceText.raycastTarget = false;

        var priceOutline = priceGo.AddComponent<Outline>();
        priceOutline.effectColor = new Color(0, 0, 0, 0.8f);
        priceOutline.effectDistance = new Vector2(1, -1);

        // HP bar
        var hpRoot = new GameObject("HPBar");
        hpRoot.transform.SetParent(canvasGo.transform, false);
        var hpRootRect = hpRoot.AddComponent<RectTransform>();
        hpRootRect.anchorMin = new Vector2(0.1f, -0.15f);
        hpRootRect.anchorMax = new Vector2(0.9f, -0.02f);
        hpRootRect.offsetMin = Vector2.zero;
        hpRootRect.offsetMax = Vector2.zero;

        hpBarGroup = hpRoot.AddComponent<CanvasGroup>();
        hpBarGroup.alpha = 0;

        hpBarBG = new GameObject("BG");
        hpBarBG.transform.SetParent(hpRoot.transform, false);
        var bgRect = hpBarBG.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        hpBarBG.AddComponent<CanvasRenderer>();
        var bgImg = hpBarBG.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        bgImg.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hpRoot.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillGo.AddComponent<CanvasRenderer>();
        hpBarFill = fillGo.AddComponent<Image>();
        hpBarFill.color = new Color(0.2f, 0.85f, 0.2f);
        hpBarFill.type = Image.Type.Filled;
        hpBarFill.fillMethod = Image.FillMethod.Horizontal;
        hpBarFill.fillAmount = 1f;
        hpBarFill.raycastTarget = false;

        initialized = true;
    }

    public void UpdateNodeStatus(string icon, int cost, bool canAfford, bool visible)
    {
        if (!initialized) return;

        if (iconText != null)
            iconText.text = icon;

        if (priceText != null)
        {
            priceText.text = cost > 0 ? cost.ToString() : "";
            priceText.color = canAfford
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.9f, 0.2f, 0.2f);
        }

        if (canvasGroup != null)
        {
            float target = visible ? 1f : 0f;
            if (iconText != null && string.IsNullOrEmpty(icon))
                target = 0f;
            canvasGroup.alpha = target;
        }
    }

    public void UpdateHPBar(float ratio, bool visible)
    {
        if (!initialized) return;

        if (hpBarGroup != null)
            hpBarGroup.alpha = visible ? 1f : 0f;

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = Mathf.Clamp01(ratio);
            hpBarFill.color = Color.Lerp(
                new Color(0.9f, 0.15f, 0.15f),
                new Color(0.2f, 0.85f, 0.2f),
                ratio);
        }
    }

    public void SetVisible(bool visible)
    {
        if (canvas != null)
            canvas.gameObject.SetActive(visible);
    }

    void LateUpdate()
    {
        if (!initialized || canvas == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 dir = canvas.transform.position - cam.transform.position;
        if (dir.sqrMagnitude > 0.01f)
            canvas.transform.rotation = Quaternion.LookRotation(dir);
    }
}
