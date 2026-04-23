using UnityEngine;
using UnityEngine.UI;

public class GameJamHotbarHUD : MonoBehaviour
{
    GameJamInventoryModel model;
    GameObject canvasGo;
    Image[] slotBGs;
    Image[] slotIcons;
    Text[] slotCounts;
    Image[] slotBorders;
    Text[] slotNumbers;
    GameObject tooltipGo;
    Text tooltipText;

    static readonly Color SlotBG = new Color(0.12f, 0.12f, 0.15f, 0.85f);
    static readonly Color SlotBGSelected = new Color(0.2f, 0.2f, 0.28f, 0.9f);
    static readonly Color BorderNormal = new Color(0.3f, 0.3f, 0.35f, 0.8f);
    static readonly Color BorderSelected = new Color(0.9f, 0.75f, 0.2f, 1f);
    const float SlotSize = 60f;
    const float SlotSpacing = 6f;

    public void Init(GameJamInventoryModel model)
    {
        this.model = model;
        BuildUI();
        BindEvents();
        RefreshAll();
    }

    void BuildUI()
    {
        canvasGo = GameJamUIPrefabHelper.TryLoadPrefab("HotbarPanel");
        if (canvasGo != null)
        {
            FindReferences();
            return;
        }

        canvasGo = new GameObject("HotbarPanel");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var bar = CreateRect("HotbarBar", canvasGo.transform);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 0f);
        barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        float totalWidth = GameJamInventoryModel.HotbarSlotCount * SlotSize
            + (GameJamInventoryModel.HotbarSlotCount - 1) * SlotSpacing + 20f;
        barRect.sizeDelta = new Vector2(totalWidth, SlotSize + 24f);
        barRect.anchoredPosition = new Vector2(0, 12f);

        var barBG = bar.AddComponent<Image>();
        barBG.color = new Color(0.08f, 0.08f, 0.1f, 0.7f);

        int count = GameJamInventoryModel.HotbarSlotCount;
        slotBGs = new Image[count];
        slotIcons = new Image[count];
        slotCounts = new Text[count];
        slotBorders = new Image[count];
        slotNumbers = new Text[count];

        float startX = -(totalWidth - 20f) / 2f + SlotSize / 2f;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * (SlotSize + SlotSpacing);
            CreateSlot(bar.transform, i, x);
        }

        BuildTooltip(canvasGo.transform);
        GameJamUIPrefabHelper.SavePrefab(canvasGo, "HotbarPanel");
    }

    void FindReferences()
    {
        int count = GameJamInventoryModel.HotbarSlotCount;
        slotBGs = new Image[count];
        slotIcons = new Image[count];
        slotCounts = new Text[count];
        slotBorders = new Image[count];
        slotNumbers = new Text[count];

        var bar = canvasGo.transform.Find("HotbarBar");
        for (int i = 0; i < count; i++)
        {
            var slot = bar.Find("Slot_" + i);
            slotBorders[i] = slot.GetComponent<Image>();
            slotBGs[i] = slot.Find("BG").GetComponent<Image>();
            slotIcons[i] = slot.Find("BG/Icon").GetComponent<Image>();
            slotCounts[i] = slot.Find("BG/Count").GetComponent<Text>();
            slotNumbers[i] = slot.Find("Number").GetComponent<Text>();
        }

        tooltipGo = canvasGo.transform.Find("Tooltip").gameObject;
        tooltipText = tooltipGo.transform.Find("Text").GetComponent<Text>();
        tooltipGo.SetActive(false);
    }

    void CreateSlot(Transform parent, int index, float x)
    {
        var slot = CreateRect("Slot_" + index, parent);
        var slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        slotRect.anchoredPosition = new Vector2(x, 2f);

        var border = slot.AddComponent<Image>();
        border.color = index == 0 ? BorderSelected : BorderNormal;
        slotBorders[index] = border;

        var inner = CreateRect("BG", slot.transform);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-4f, -4f);
        innerRect.anchoredPosition = Vector2.zero;
        var bg = inner.AddComponent<Image>();
        bg.color = index == 0 ? SlotBGSelected : SlotBG;
        slotBGs[index] = bg;

        var icon = CreateRect("Icon", inner.transform);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.clear;
        slotIcons[index] = iconImg;

        var countGo = CreateRect("Count", inner.transform);
        var countRect = countGo.GetComponent<RectTransform>();
        countRect.anchorMin = new Vector2(1f, 0f);
        countRect.anchorMax = new Vector2(1f, 0f);
        countRect.pivot = new Vector2(1f, 0f);
        countRect.sizeDelta = new Vector2(40f, 20f);
        countRect.anchoredPosition = new Vector2(-2f, 2f);
        var countText = countGo.AddComponent<Text>();
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countText.fontSize = 14;
        countText.alignment = TextAnchor.LowerRight;
        countText.color = Color.white;
        countText.text = "";
        var countOutline = countGo.AddComponent<Outline>();
        countOutline.effectColor = new Color(0, 0, 0, 0.8f);
        countOutline.effectDistance = new Vector2(1, -1);
        slotCounts[index] = countText;

        var numGo = CreateRect("Number", slot.transform);
        var numRect = numGo.GetComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0f, 1f);
        numRect.anchorMax = new Vector2(0f, 1f);
        numRect.pivot = new Vector2(0f, 1f);
        numRect.sizeDelta = new Vector2(20f, 16f);
        numRect.anchoredPosition = new Vector2(4f, -2f);
        var numText = numGo.AddComponent<Text>();
        numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        numText.fontSize = 12;
        numText.alignment = TextAnchor.UpperLeft;
        numText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        numText.text = (index + 1).ToString();
        slotNumbers[index] = numText;
    }

    void BuildTooltip(Transform parent)
    {
        tooltipGo = CreateRect("Tooltip", parent);
        var rect = tooltipGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(160f, 32f);
        rect.anchoredPosition = new Vector2(0, SlotSize + 40f);

        var bg = tooltipGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.9f);

        var txtGo = CreateRect("Text", tooltipGo.transform);
        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        tooltipText = txtGo.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = 16;
        tooltipText.alignment = TextAnchor.MiddleCenter;
        tooltipText.color = Color.white;

        tooltipGo.SetActive(false);
    }

    void BindEvents()
    {
        model.OnHotbarSlotChanged += OnSlotChanged;
        model.OnSelectedHotbarChanged += OnSelectionChanged;
    }

    void OnSlotChanged(int index)
    {
        RefreshSlot(index);
    }

    void OnSelectionChanged(int index)
    {
        for (int i = 0; i < GameJamInventoryModel.HotbarSlotCount; i++)
        {
            slotBorders[i].color = i == index ? BorderSelected : BorderNormal;
            slotBGs[i].color = i == index ? SlotBGSelected : SlotBG;
        }
    }

    void RefreshSlot(int index)
    {
        var slot = model.hotbarSlots[index];
        if (slot.IsEmpty)
        {
            slotIcons[index].color = Color.clear;
            slotCounts[index].text = "";
        }
        else
        {
            var def = GameJamItemDB.Get(slot.itemId);
            slotIcons[index].color = def != null ? def.iconColor : Color.gray;
            slotCounts[index].text = slot.count > 1 ? slot.count.ToString() : "";
        }
    }

    public void RefreshAll()
    {
        for (int i = 0; i < GameJamInventoryModel.HotbarSlotCount; i++)
            RefreshSlot(i);
    }

    public void ShowTooltip(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= model.hotbarSlots.Length) return;
        var slot = model.hotbarSlots[slotIndex];
        if (slot.IsEmpty)
        {
            tooltipGo.SetActive(false);
            return;
        }
        var def = GameJamItemDB.Get(slot.itemId);
        if (def == null) return;
        tooltipText.text = def.name;
        tooltipGo.SetActive(true);
    }

    public void HideTooltip()
    {
        if (tooltipGo != null) tooltipGo.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (canvasGo != null) canvasGo.SetActive(visible);
    }

    public void Cleanup()
    {
        if (model != null)
        {
            model.OnHotbarSlotChanged -= OnSlotChanged;
            model.OnSelectedHotbarChanged -= OnSelectionChanged;
        }
        if (canvasGo != null) Destroy(canvasGo);
    }

    void OnDestroy() => Cleanup();

    static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }
}
