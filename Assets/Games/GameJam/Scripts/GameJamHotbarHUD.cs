using UnityEngine;
using UnityEngine.UI;

public class GameJamHotbarHUD : MonoBehaviour
{
    GameJamInventoryModel model;
    GameObject canvasGo;
    GameObject slotTemplate;
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

        slotTemplate = BuildSlotTemplate(bar.transform);
        slotTemplate.SetActive(false);

        BuildTooltip(canvasGo.transform);
        GameJamUIPrefabHelper.SavePrefab(canvasGo, "HotbarPanel");

        InstantiateSlots(bar.transform);
    }

    void FindReferences()
    {
        var bar = canvasGo.transform.Find("HotbarBar");
        slotTemplate = bar.Find("Slot_0")?.gameObject;
        if (slotTemplate != null)
            slotTemplate.SetActive(false);
        for (int i = bar.childCount - 1; i >= 0; i--)
        {
            var child = bar.GetChild(i).gameObject;
            if (child != slotTemplate) Destroy(child);
        }

        InstantiateSlots(bar);

        tooltipGo = canvasGo.transform.Find("Tooltip").gameObject;
        tooltipText = tooltipGo.transform.Find("Text").GetComponent<Text>();
        tooltipGo.SetActive(false);
    }

    GameObject BuildSlotTemplate(Transform parent)
    {
        var slot = CreateRect("Slot_0", parent);
        var slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        slotRect.anchoredPosition = Vector2.zero;

        slot.AddComponent<Image>().color = BorderNormal;

        var inner = CreateRect("BG", slot.transform);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-4f, -4f);
        innerRect.anchoredPosition = Vector2.zero;
        inner.AddComponent<Image>().color = SlotBG;

        var icon = CreateRect("Icon", inner.transform);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        icon.AddComponent<Image>().color = Color.clear;

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
        var outline = countGo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);

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
        numText.text = "";

        return slot;
    }

    void InstantiateSlots(Transform bar)
    {
        int count = GameJamInventoryModel.HotbarSlotCount;
        slotBGs = new Image[count];
        slotIcons = new Image[count];
        slotCounts = new Text[count];
        slotBorders = new Image[count];
        slotNumbers = new Text[count];

        float totalWidth = count * SlotSize + (count - 1) * SlotSpacing + 20f;
        float startX = -(totalWidth - 20f) / 2f + SlotSize / 2f;

        int sel = model != null ? model.selectedHotbar : 0;
        for (int i = 0; i < count; i++)
        {
            var slot = Instantiate(slotTemplate, bar);
            slot.name = "Slot_" + i;
            slot.SetActive(true);

            var rt = slot.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * (SlotSize + SlotSpacing), 2f);

            slotBorders[i] = slot.GetComponent<Image>();
            slotBorders[i].color = i == sel ? BorderSelected : BorderNormal;

            slotBGs[i] = slot.transform.Find("BG").GetComponent<Image>();
            slotBGs[i].color = i == sel ? SlotBGSelected : SlotBG;

            slotIcons[i] = slot.transform.Find("BG/Icon").GetComponent<Image>();
            slotCounts[i] = slot.transform.Find("BG/Count").GetComponent<Text>();
            slotNumbers[i] = slot.transform.Find("Number").GetComponent<Text>();
            slotNumbers[i].text = (i + 1).ToString();
        }
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
            GameJamArtLoader.ClearIcon(slotIcons[index]);
            slotCounts[index].text = "";
        }
        else
        {
            GameJamArtLoader.ApplyItemIcon(slotIcons[index], slot.itemId, Color.gray);
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
