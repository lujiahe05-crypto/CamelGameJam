using UnityEngine;
using UnityEngine.UI;

public class ThronefallHeroUI : MonoBehaviour
{
    Canvas worldCanvas;
    CanvasGroup hpBarGroup;
    Image hpBarFillImage;
    Image ringBGImage;
    Image ringFillImage;

    Sprite ringSprite;

    public void Init(Transform playerTransform)
    {
        CreateRingSprite();

        var canvasGo = new GameObject("HeroWorldCanvas");
        canvasGo.transform.SetParent(playerTransform, false);
        canvasGo.transform.localPosition = new Vector3(0, 3.5f, 0);
        canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        worldCanvas = canvasGo.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 5;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 120);

        CreateHPBar(canvasGo.transform);
        CreateSkillRing(canvasGo.transform);
    }

    void CreateRingSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float center = (size - 1) * 0.5f;
        float outerR = 31f;
        float innerR = 22f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = (dist >= innerR && dist <= outerR) ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    void CreateHPBar(Transform parent)
    {
        var hpRoot = new GameObject("HPBar");
        hpRoot.transform.SetParent(parent, false);
        var hpRect = hpRoot.AddComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0.05f, 0.35f);
        hpRect.anchorMax = new Vector2(0.95f, 0.45f);
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;

        hpBarGroup = hpRoot.AddComponent<CanvasGroup>();
        hpBarGroup.alpha = 0f;

        // Background
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(hpRoot.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.AddComponent<CanvasRenderer>();
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        bgImg.raycastTarget = false;

        // Fill
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hpRoot.transform, false);
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

    void CreateSkillRing(Transform parent)
    {
        var ringRoot = new GameObject("SkillRing");
        ringRoot.transform.SetParent(parent, false);
        var ringRect = ringRoot.AddComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = new Vector2(0, 25);
        ringRect.sizeDelta = new Vector2(50, 50);

        // BG ring
        var bgGo = new GameObject("RingBG");
        bgGo.transform.SetParent(ringRoot.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.AddComponent<CanvasRenderer>();
        ringBGImage = bgGo.AddComponent<Image>();
        ringBGImage.sprite = ringSprite;
        ringBGImage.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        ringBGImage.raycastTarget = false;

        // Fill ring
        var fillGo = new GameObject("RingFill");
        fillGo.transform.SetParent(ringRoot.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillGo.AddComponent<CanvasRenderer>();
        ringFillImage = fillGo.AddComponent<Image>();
        ringFillImage.sprite = ringSprite;
        ringFillImage.type = Image.Type.Filled;
        ringFillImage.fillMethod = Image.FillMethod.Radial360;
        ringFillImage.fillClockwise = true;
        ringFillImage.fillOrigin = (int)Image.Origin360.Top;
        ringFillImage.fillAmount = 1f;
        ringFillImage.color = new Color(1f, 0.9f, 0.2f);
        ringFillImage.raycastTarget = false;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 dir = transform.position - cam.transform.position;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    public void UpdateHPBar(float ratio)
    {
        if (hpBarFillImage != null)
        {
            hpBarFillImage.fillAmount = ratio;
            hpBarFillImage.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.2f, 0.9f, 0.2f), ratio);
        }
        if (hpBarGroup != null)
            hpBarGroup.alpha = ratio < 0.999f ? 1f : 0f;
    }

    public void UpdateSkillRing(float cdRemaining, float cdTotal)
    {
        if (ringFillImage == null) return;

        if (cdTotal <= 0 || cdRemaining <= 0)
        {
            ringFillImage.fillAmount = 1f;
            ringFillImage.color = new Color(1f, 0.9f, 0.2f);
            return;
        }

        float ratio = 1f - (cdRemaining / cdTotal);
        ringFillImage.fillAmount = ratio;
        ringFillImage.color = new Color(0.5f, 0.5f, 0.5f);
    }

    public void SetVisible(bool visible)
    {
        if (worldCanvas != null)
            worldCanvas.gameObject.SetActive(visible);
    }
}
