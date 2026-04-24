using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameJamItemGainToast : MonoBehaviour
{
    class EntryView
    {
        public GameObject go;
        public RectTransform rect;
        public CanvasGroup canvasGroup;
        public Coroutine moveRoutine;
    }

    static GameJamItemGainToast instance;

    readonly List<EntryView> activeEntries = new List<EntryView>();

    const float EntryWidth = 340f;
    const float EntryHeight = 76f;
    const float EntrySpacing = 10f;
    const float BaseOffsetX = 28f;
    const float BaseOffsetY = 28f;
    const float MoveDuration = 0.18f;
    const float FadeInDuration = 0.16f;
    const float HoldDuration = 1.7f;
    const float FadeOutDuration = 0.28f;

    public static void Show(string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return;

        if (instance == null)
            CreateInstance();

        instance.ShowInternal(itemId, amount);
    }

    static void CreateInstance()
    {
        var canvas = FindOrCreateCanvas();

        var root = new GameObject("GameJamItemGainToast");
        root.transform.SetParent(canvas.transform, false);

        instance = root.AddComponent<GameJamItemGainToast>();

        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(BaseOffsetX, BaseOffsetY);
        rect.sizeDelta = new Vector2(EntryWidth, 420f);
    }

    static Canvas FindOrCreateCanvas()
    {
        foreach (var canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return canvas;
        }

        var canvasGo = new GameObject("GameJamItemGainCanvas");
        var canvasComp = canvasGo.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComp.sortingOrder = 998;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvasComp;
    }

    void ShowInternal(string itemId, int amount)
    {
        var def = GameJamItemDB.Get(itemId);
        string itemName = def != null && !string.IsNullOrWhiteSpace(def.name) ? def.name : itemId;
        Color fallbackColor = def != null ? def.iconColor : Color.white;

        var entry = CreateEntry(itemId, itemName, amount, fallbackColor);
        activeEntries.Add(entry);
        RefreshLayout();
        StartCoroutine(PlayEntry(entry));
    }

    EntryView CreateEntry(string itemId, string itemName, int amount, Color fallbackColor)
    {
        var go = new GameObject("ItemGainEntry");
        go.transform.SetParent(transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.sizeDelta = new Vector2(EntryWidth, EntryHeight);
        rect.anchoredPosition = new Vector2(-18f, 0f);

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.11f, 0.14f, 0.22f, 0.86f);
        bg.raycastTarget = false;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 0f);
        iconRect.sizeDelta = new Vector2(54f, 54f);

        var icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        GameJamArtLoader.ApplyItemIcon(icon, itemId, fallbackColor);

        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(go.transform, false);
        var nameRect = nameGo.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(78f, 0f);
        nameRect.offsetMax = new Vector2(-92f, 0f);

        var nameText = nameGo.AddComponent<Text>();
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 26;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = Color.white;
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.verticalOverflow = VerticalWrapMode.Truncate;
        nameText.raycastTarget = false;
        nameText.text = itemName;

        var nameOutline = nameGo.AddComponent<Outline>();
        nameOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        nameOutline.effectDistance = new Vector2(1f, -1f);

        var amountGo = new GameObject("Amount");
        amountGo.transform.SetParent(go.transform, false);
        var amountRect = amountGo.AddComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(1f, 0f);
        amountRect.anchorMax = new Vector2(1f, 1f);
        amountRect.pivot = new Vector2(1f, 0.5f);
        amountRect.anchoredPosition = new Vector2(-12f, 0f);
        amountRect.sizeDelta = new Vector2(88f, EntryHeight);

        var amountText = amountGo.AddComponent<Text>();
        amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        amountText.fontSize = 34;
        amountText.fontStyle = FontStyle.Bold;
        amountText.alignment = TextAnchor.MiddleRight;
        amountText.color = new Color(0.47f, 0.95f, 0.29f);
        amountText.horizontalOverflow = HorizontalWrapMode.Overflow;
        amountText.verticalOverflow = VerticalWrapMode.Overflow;
        amountText.raycastTarget = false;
        amountText.text = "+" + amount;

        var amountOutline = amountGo.AddComponent<Outline>();
        amountOutline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        amountOutline.effectDistance = new Vector2(1f, -1f);

        return new EntryView
        {
            go = go,
            rect = rect,
            canvasGroup = canvasGroup
        };
    }

    IEnumerator PlayEntry(EntryView entry)
    {
        float t = 0f;
        while (t < FadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            entry.canvasGroup.alpha = Mathf.Clamp01(t / FadeInDuration);
            yield return null;
        }

        entry.canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(HoldDuration);

        t = 0f;
        while (t < FadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            entry.canvasGroup.alpha = 1f - Mathf.Clamp01(t / FadeOutDuration);
            yield return null;
        }

        if (entry.moveRoutine != null)
            StopCoroutine(entry.moveRoutine);

        activeEntries.Remove(entry);
        Destroy(entry.go);
        RefreshLayout();
    }

    void RefreshLayout()
    {
        int count = activeEntries.Count;
        for (int i = 0; i < count; i++)
        {
            var entry = activeEntries[i];
            float y = (count - 1 - i) * (EntryHeight + EntrySpacing);
            var target = new Vector2(0f, y);

            if (entry.moveRoutine != null)
                StopCoroutine(entry.moveRoutine);

            entry.moveRoutine = StartCoroutine(MoveEntry(entry, target));
        }
    }

    IEnumerator MoveEntry(EntryView entry, Vector2 target)
    {
        Vector2 start = entry.rect.anchoredPosition;
        float t = 0f;

        while (t < MoveDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / MoveDuration);
            p = 1f - Mathf.Pow(1f - p, 3f);
            entry.rect.anchoredPosition = Vector2.Lerp(start, target, p);
            yield return null;
        }

        entry.rect.anchoredPosition = target;
        entry.moveRoutine = null;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
