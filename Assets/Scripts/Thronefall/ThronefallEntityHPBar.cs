using UnityEngine;
using UnityEngine.UI;

public class ThronefallEntityHPBar : MonoBehaviour
{
    CanvasGroup hpBarGroup;
    Image hpBarFillImage;
    bool alwaysShow;

    public void Init(Transform parent, float yOffset, float barWidth = 120f, bool alwaysVisible = false)
    {
        alwaysShow = alwaysVisible;

        var canvasGo = new GameObject("EntityHPCanvas");
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.localPosition = new Vector3(0, yOffset, 0);
        canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 4;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(barWidth, 12f);

        hpBarGroup = canvasGo.AddComponent<CanvasGroup>();
        hpBarGroup.alpha = alwaysShow ? 1f : 0f;

        canvasGo.AddComponent<ThronefallBillboard>();

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.AddComponent<CanvasRenderer>();
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        bgImg.raycastTarget = false;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(canvasGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillGo.AddComponent<CanvasRenderer>();
        hpBarFillImage = fillGo.AddComponent<Image>();
        hpBarFillImage.color = new Color(0.2f, 0.9f, 0.2f);
        hpBarFillImage.type = Image.Type.Filled;
        hpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
        hpBarFillImage.fillAmount = 1f;
        hpBarFillImage.raycastTarget = false;
    }

    public void UpdateHP(float ratio)
    {
        if (hpBarFillImage != null)
        {
            hpBarFillImage.fillAmount = ratio;
            hpBarFillImage.color = Color.Lerp(
                new Color(0.9f, 0.15f, 0.15f),
                new Color(0.2f, 0.9f, 0.2f),
                ratio);
        }
        if (hpBarGroup != null)
            hpBarGroup.alpha = (alwaysShow || ratio < 0.999f) ? 1f : 0f;
    }
}
