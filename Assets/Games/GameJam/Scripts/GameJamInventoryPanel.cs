using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class GameJamInventoryPanel : MonoBehaviour
{
    public GameJamInventoryModel Model { get; private set; }

    GameObject canvasGo;
    GameObject panelGo;

    // Main grid
    Image[] mainSlotBGs;
    Image[] mainSlotIcons;
    Text[] mainSlotCounts;
    Image[] mainSlotBorders;

    // Hotbar in panel
    Image[] hotbarSlotBGs;
    Image[] hotbarSlotIcons;
    Text[] hotbarSlotCounts;
    Image[] hotbarSlotBorders;
    Text[] hotbarSlotNumbers;

    // Detail panel
    GameObject detailGo;
    Image detailIcon;
    Text detailName;
    Text detailType;
    Text detailDesc;
    Text detailPrice;
    Image detailRarityBar;

    // Bottom bar
    Text goldText;
    Button unlockBtn;
    Text unlockBtnText;
    GameObject splitDialogGo;
    InputField splitInput;
    int splitFromIndex;
    bool splitFromHotbar;

    // Selection
    int selectedIndex = -1;
    bool selectedIsHotbar;

    // Tooltip
    GameObject tooltipGo;
    Text tooltipNameText;

    static readonly Color PanelBG = new Color(0.08f, 0.09f, 0.1f, 0.95f);
    static readonly Color SlotEmpty = new Color(0.15f, 0.15f, 0.18f, 0.9f);
    static readonly Color SlotFilled = new Color(0.18f, 0.18f, 0.22f, 0.95f);
    static readonly Color SlotSelected = new Color(0.25f, 0.25f, 0.35f, 1f);
    static readonly Color BorderDefault = new Color(0.25f, 0.25f, 0.3f, 0.6f);
    static readonly Color TextDim = new Color(0.65f, 0.65f, 0.7f);
    static readonly Color TextBright = new Color(0.95f, 0.95f, 0.97f);
    const float SlotSize = 64f;
    const float SlotGap = 6f;
    const int Columns = 8;
    const int Rows = 4;

    public void Init(GameJamInventoryModel model)
    {
        Model = model;
        BindEvents();
    }

    void EnsureUI()
    {
        if (canvasGo == null) BuildUI();
    }

    void BuildUI()
    {
        canvasGo = GameJamUIPrefabHelper.TryLoadPrefab("InventoryPanel");
        if (canvasGo != null)
        {
            FindReferences();
            return;
        }

        canvasGo = new GameObject("InventoryPanel");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimmer = MakeRect("Dimmer", canvasGo.transform);
        var dimRect = dimmer.GetComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.sizeDelta = Vector2.zero;
        var dimImg = dimmer.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.5f);

        float panelW = 900f;
        float panelH = 600f;
        panelGo = MakeRect("Panel", canvasGo.transform);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        var panelBG = panelGo.AddComponent<Image>();
        panelBG.color = PanelBG;

        // Title
        var titleGo = MakeRect("Title", panelGo.transform);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0, 40f);
        titleRect.anchoredPosition = Vector2.zero;
        var titleText = titleGo.AddComponent<Text>();
        titleText.font = GetFont();
        titleText.fontSize = 22;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = TextBright;
        titleText.text = "背 包";

        float contentTop = -48f;
        float detailWidth = 220f;
        float rightX = detailWidth + 24f;
        float rightWidth = panelW - rightX - 16f;

        BuildDetailPanel(panelGo.transform, contentTop, detailWidth);
        BuildMainGrid(panelGo.transform, contentTop, rightX, rightWidth);
        BuildHotbarRow(panelGo.transform, rightX, rightWidth);
        BuildBottomBar(panelGo.transform, detailWidth);
        BuildSplitDialog(canvasGo.transform);
        BuildTooltip(canvasGo.transform);

        if (EventSystem.current == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        canvasGo.SetActive(false);
        GameJamUIPrefabHelper.SavePrefab(canvasGo, "InventoryPanel");
        canvasGo.SetActive(true);
    }

    void FindReferences()
    {
        panelGo = canvasGo.transform.Find("Panel").gameObject;

        // Detail panel
        detailGo = panelGo.transform.Find("Detail").gameObject;
        detailIcon = detailGo.transform.Find("Icon").GetComponent<Image>();
        detailName = detailGo.transform.Find("Name").GetComponent<Text>();
        detailRarityBar = detailGo.transform.Find("RarityBar").GetComponent<Image>();
        detailType = detailGo.transform.Find("RarityBar/TypeText").GetComponent<Text>();
        detailDesc = detailGo.transform.Find("Desc").GetComponent<Text>();
        detailPrice = detailGo.transform.Find("Price").GetComponent<Text>();
        detailGo.SetActive(false);

        // Main grid
        int mCount = GameJamInventoryModel.MainSlotCount;
        mainSlotBGs = new Image[mCount];
        mainSlotIcons = new Image[mCount];
        mainSlotCounts = new Text[mCount];
        mainSlotBorders = new Image[mCount];
        var mainGrid = panelGo.transform.Find("MainGrid");
        for (int i = 0; i < mCount; i++)
        {
            var slot = mainGrid.Find("MSlot_" + i);
            mainSlotBorders[i] = slot.GetComponent<Image>();
            mainSlotBGs[i] = slot.Find("Inner").GetComponent<Image>();
            mainSlotIcons[i] = slot.Find("Inner/Icon").GetComponent<Image>();
            mainSlotCounts[i] = slot.Find("Inner/Count").GetComponent<Text>();
            var handler = slot.GetComponent<GameJamSlotDragHandler>();
            if (handler != null) handler.panel = this;
        }

        // Hotbar row
        int hCount = GameJamInventoryModel.HotbarSlotCount;
        hotbarSlotBGs = new Image[hCount];
        hotbarSlotIcons = new Image[hCount];
        hotbarSlotCounts = new Text[hCount];
        hotbarSlotBorders = new Image[hCount];
        hotbarSlotNumbers = new Text[hCount];
        var hotbarRow = panelGo.transform.Find("HotbarRow");
        for (int i = 0; i < hCount; i++)
        {
            var slot = hotbarRow.Find("HSlot_" + i);
            hotbarSlotBorders[i] = slot.GetComponent<Image>();
            hotbarSlotBGs[i] = slot.Find("Inner").GetComponent<Image>();
            hotbarSlotIcons[i] = slot.Find("Inner/Icon").GetComponent<Image>();
            hotbarSlotCounts[i] = slot.Find("Inner/Count").GetComponent<Text>();
            hotbarSlotNumbers[i] = slot.Find("Num").GetComponent<Text>();
            var handler = slot.GetComponent<GameJamSlotDragHandler>();
            if (handler != null) handler.panel = this;
        }

        // Bottom bar
        goldText = panelGo.transform.Find("BottomBar/Gold").GetComponent<Text>();
        var sortBtn = panelGo.transform.Find("BottomBar/SortBtn").GetComponent<Button>();
        sortBtn.onClick.RemoveAllListeners();
        sortBtn.onClick.AddListener(OnSortClicked);
        var discardBtn = panelGo.transform.Find("BottomBar/DiscardBtn").GetComponent<Button>();
        discardBtn.onClick.RemoveAllListeners();
        discardBtn.onClick.AddListener(OnDiscardClicked);
        var sellBtn = panelGo.transform.Find("BottomBar/SellBtn").GetComponent<Button>();
        sellBtn.onClick.RemoveAllListeners();
        sellBtn.onClick.AddListener(OnSellClicked);

        // Unlock button
        var unlockGo = panelGo.transform.Find("UnlockBtn");
        if (unlockGo != null)
        {
            unlockBtn = unlockGo.GetComponent<Button>();
            unlockBtn.onClick.RemoveAllListeners();
            unlockBtn.onClick.AddListener(OnUnlockClicked);
            unlockBtnText = unlockGo.Find("Text").GetComponent<Text>();
        }

        // Split dialog
        splitDialogGo = canvasGo.transform.Find("SplitDialog").gameObject;
        splitInput = splitDialogGo.transform.Find("Input").GetComponent<InputField>();
        splitInput.textComponent = splitDialogGo.transform.Find("Input/Text").GetComponent<Text>();
        var splitOK = splitDialogGo.transform.Find("SplitOK").GetComponent<Button>();
        splitOK.onClick.RemoveAllListeners();
        splitOK.onClick.AddListener(OnSplitConfirm);
        var splitCancel = splitDialogGo.transform.Find("SplitCancel").GetComponent<Button>();
        splitCancel.onClick.RemoveAllListeners();
        splitCancel.onClick.AddListener(() => splitDialogGo.SetActive(false));
        splitDialogGo.SetActive(false);

        // Tooltip
        tooltipGo = canvasGo.transform.Find("SlotTooltip").gameObject;
        tooltipNameText = tooltipGo.transform.Find("TName").GetComponent<Text>();
        tooltipGo.SetActive(false);
    }

    void BuildDetailPanel(Transform parent, float top, float width)
    {
        detailGo = MakeRect("Detail", parent);
        var rect = detailGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(16f, 60f);
        rect.offsetMax = new Vector2(16f + width, -48f);

        var bg = detailGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.13f, 0.8f);

        float y = -16f;

        var iconGo = MakeRect("Icon", detailGo.transform);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(80, 80);
        iconRect.anchoredPosition = new Vector2(0, y);
        detailIcon = iconGo.AddComponent<Image>();
        detailIcon.color = Color.clear;
        y -= 90f;

        detailName = MakeText("Name", detailGo.transform, 20, TextAnchor.MiddleCenter, TextBright,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, y), new Vector2(0, 28));
        y -= 34f;

        var rarityBarGo = MakeRect("RarityBar", detailGo.transform);
        var rbRect = rarityBarGo.GetComponent<RectTransform>();
        rbRect.anchorMin = new Vector2(0.15f, 1f);
        rbRect.anchorMax = new Vector2(0.85f, 1f);
        rbRect.pivot = new Vector2(0.5f, 1f);
        rbRect.sizeDelta = new Vector2(0, 22f);
        rbRect.anchoredPosition = new Vector2(0, y);
        detailRarityBar = rarityBarGo.AddComponent<Image>();
        detailRarityBar.color = Color.clear;
        detailType = MakeText("TypeText", rarityBarGo.transform, 14, TextAnchor.MiddleCenter, Color.white,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        y -= 30f;

        var line = MakeRect("Line", detailGo.transform);
        var lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.1f, 1f);
        lineRect.anchorMax = new Vector2(0.9f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.sizeDelta = new Vector2(0, 1f);
        lineRect.anchoredPosition = new Vector2(0, y);
        line.AddComponent<Image>().color = new Color(1, 1, 1, 0.1f);
        y -= 10f;

        detailDesc = MakeText("Desc", detailGo.transform, 15, TextAnchor.UpperCenter, TextDim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, y), new Vector2(-24f, 80));
        y -= 90f;

        detailPrice = MakeText("Price", detailGo.transform, 16, TextAnchor.MiddleCenter,
            new Color(1f, 0.85f, 0.3f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 12f), new Vector2(0, 30));

        detailGo.SetActive(false);
    }

    void BuildMainGrid(Transform parent, float top, float leftX, float availWidth)
    {
        var gridContainer = MakeRect("MainGrid", parent);
        var gcRect = gridContainer.GetComponent<RectTransform>();
        gcRect.anchorMin = new Vector2(0f, 1f);
        gcRect.anchorMax = new Vector2(0f, 1f);
        gcRect.pivot = new Vector2(0f, 1f);
        float gridW = Columns * SlotSize + (Columns - 1) * SlotGap;
        float gridH = Rows * SlotSize + (Rows - 1) * SlotGap;
        gcRect.sizeDelta = new Vector2(gridW, gridH);
        float gridLeft = leftX + (availWidth - gridW) / 2f;
        gcRect.anchoredPosition = new Vector2(gridLeft, top);

        int count = GameJamInventoryModel.MainSlotCount;
        mainSlotBGs = new Image[count];
        mainSlotIcons = new Image[count];
        mainSlotCounts = new Text[count];
        mainSlotBorders = new Image[count];

        for (int i = 0; i < count; i++)
        {
            int row = i / Columns;
            int col = i % Columns;
            float x = col * (SlotSize + SlotGap);
            float y = -row * (SlotSize + SlotGap);
            CreateGridSlot(gridContainer.transform, i, false, x, y);
        }
    }

    void BuildHotbarRow(Transform parent, float leftX, float availWidth)
    {
        var hbContainer = MakeRect("HotbarRow", parent);
        var hbRect = hbContainer.GetComponent<RectTransform>();
        hbRect.anchorMin = new Vector2(0f, 0f);
        hbRect.anchorMax = new Vector2(0f, 0f);
        hbRect.pivot = new Vector2(0f, 0f);

        int hCount = GameJamInventoryModel.HotbarSlotCount;
        float hbW = hCount * SlotSize + (hCount - 1) * SlotGap;
        float hbH = SlotSize + 18f;
        hbRect.sizeDelta = new Vector2(hbW, hbH);
        float hbLeft = leftX + (availWidth - hbW) / 2f;
        hbRect.anchoredPosition = new Vector2(hbLeft, 56f);

        var label = MakeText("HBLabel", hbContainer.transform, 14, TextAnchor.MiddleCenter, TextDim,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, 0), new Vector2(0, 18f));
        label.text = "快 捷 栏";

        hotbarSlotBGs = new Image[hCount];
        hotbarSlotIcons = new Image[hCount];
        hotbarSlotCounts = new Text[hCount];
        hotbarSlotBorders = new Image[hCount];
        hotbarSlotNumbers = new Text[hCount];

        for (int i = 0; i < hCount; i++)
        {
            float x = i * (SlotSize + SlotGap);
            float y = 0f;
            CreateGridSlot(hbContainer.transform, i, true, x, y);
        }
    }

    void CreateGridSlot(Transform parent, int index, bool isHotbar, float x, float y)
    {
        var slot = MakeRect((isHotbar ? "HSlot_" : "MSlot_") + index, parent);
        var slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, isHotbar ? 0f : 1f);
        slotRect.anchorMax = new Vector2(0f, isHotbar ? 0f : 1f);
        slotRect.pivot = new Vector2(0f, isHotbar ? 0f : 1f);
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        slotRect.anchoredPosition = new Vector2(x, y);

        var border = slot.AddComponent<Image>();
        border.color = BorderDefault;

        var inner = MakeRect("Inner", slot.transform);
        var innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-3f, -3f);
        innerRect.anchoredPosition = Vector2.zero;
        var bg = inner.AddComponent<Image>();
        bg.color = SlotEmpty;

        var icon = MakeRect("Icon", inner.transform);
        var iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.12f, 0.12f);
        iconRect.anchorMax = new Vector2(0.88f, 0.88f);
        iconRect.sizeDelta = Vector2.zero;
        iconRect.anchoredPosition = Vector2.zero;
        var iconImg = icon.AddComponent<Image>();
        iconImg.color = Color.clear;
        iconImg.raycastTarget = false;

        var countGo = MakeRect("Count", inner.transform);
        var cRect = countGo.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(1f, 0f);
        cRect.anchorMax = new Vector2(1f, 0f);
        cRect.pivot = new Vector2(1f, 0f);
        cRect.sizeDelta = new Vector2(40f, 18f);
        cRect.anchoredPosition = new Vector2(-2f, 2f);
        var cText = countGo.AddComponent<Text>();
        cText.font = GetFont();
        cText.fontSize = 13;
        cText.alignment = TextAnchor.LowerRight;
        cText.color = Color.white;
        cText.raycastTarget = false;
        var cOutline = countGo.AddComponent<Outline>();
        cOutline.effectColor = new Color(0, 0, 0, 0.8f);
        cOutline.effectDistance = new Vector2(1, -1);

        if (isHotbar)
        {
            hotbarSlotBGs[index] = bg;
            hotbarSlotIcons[index] = iconImg;
            hotbarSlotCounts[index] = cText;
            hotbarSlotBorders[index] = border;

            var numGo = MakeRect("Num", slot.transform);
            var nRect = numGo.GetComponent<RectTransform>();
            nRect.anchorMin = new Vector2(0f, 1f);
            nRect.anchorMax = new Vector2(0f, 1f);
            nRect.pivot = new Vector2(0f, 1f);
            nRect.sizeDelta = new Vector2(18f, 14f);
            nRect.anchoredPosition = new Vector2(3f, -2f);
            var nText = numGo.AddComponent<Text>();
            nText.font = GetFont();
            nText.fontSize = 11;
            nText.alignment = TextAnchor.UpperLeft;
            nText.color = new Color(0.6f, 0.6f, 0.65f, 0.8f);
            nText.text = (index + 1).ToString();
            nText.raycastTarget = false;
            hotbarSlotNumbers[index] = nText;
        }
        else
        {
            mainSlotBGs[index] = bg;
            mainSlotIcons[index] = iconImg;
            mainSlotCounts[index] = cText;
            mainSlotBorders[index] = border;
        }

        var handler = slot.AddComponent<GameJamSlotDragHandler>();
        handler.isHotbar = isHotbar;
        handler.slotIndex = index;
        handler.panel = this;
    }

    void BuildBottomBar(Transform parent, float detailWidth)
    {
        var bar = MakeRect("BottomBar", parent);
        var barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(0f, 0f);
        barRect.pivot = new Vector2(0f, 0f);
        barRect.sizeDelta = new Vector2(detailWidth + 8f, 50f);
        barRect.anchoredPosition = new Vector2(12f, 6f);

        // Sort button
        CreateButton("SortBtn", bar.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(4f, 0f), new Vector2(80f, 36f),
            "整理", OnSortClicked);

        // Discard button
        CreateButton("DiscardBtn", bar.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(92f, 0f), new Vector2(80f, 36f),
            "丢弃", OnDiscardClicked);

        // Sell button
        CreateButton("SellBtn", bar.transform,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(180f, 0f), new Vector2(80f, 36f),
            "售卖", OnSellClicked);

        // Gold
        goldText = MakeText("Gold", bar.transform, 16, TextAnchor.MiddleRight,
            new Color(1f, 0.85f, 0.3f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-4f, 0), new Vector2(100f, 30f));
        goldText.text = "金币: 0";

        // Unlock row button (above bottom bar on the right side)
        var unlockGo = MakeRect("UnlockBtn", parent);
        var unlockRect = unlockGo.GetComponent<RectTransform>();
        unlockRect.anchorMin = new Vector2(1f, 0f);
        unlockRect.anchorMax = new Vector2(1f, 0f);
        unlockRect.pivot = new Vector2(1f, 0f);
        unlockRect.sizeDelta = new Vector2(200f, 32f);
        unlockRect.anchoredPosition = new Vector2(-16f, 62f);
        unlockGo.AddComponent<Image>().color = new Color(0.22f, 0.22f, 0.28f);
        unlockBtn = unlockGo.AddComponent<Button>();
        var uColors = unlockBtn.colors;
        uColors.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        uColors.pressedColor = new Color(0.18f, 0.18f, 0.24f);
        uColors.disabledColor = new Color(0.15f, 0.15f, 0.18f, 0.6f);
        unlockBtn.colors = uColors;
        unlockBtn.onClick.AddListener(OnUnlockClicked);

        var unlockTxtGo = MakeRect("Text", unlockGo.transform);
        var utRect = unlockTxtGo.GetComponent<RectTransform>();
        utRect.anchorMin = Vector2.zero;
        utRect.anchorMax = Vector2.one;
        utRect.sizeDelta = Vector2.zero;
        unlockBtnText = unlockTxtGo.AddComponent<Text>();
        unlockBtnText.font = GetFont();
        unlockBtnText.fontSize = 14;
        unlockBtnText.alignment = TextAnchor.MiddleCenter;
        unlockBtnText.color = TextBright;

        // Hints
        var hints = MakeText("Hints", parent,
            12, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 4f), new Vector2(0, 16f));
        hints.text = "Tab关闭  |  左键选中  |  拖拽移动  |  Shift+左键拆分";
    }

    void BuildSplitDialog(Transform parent)
    {
        splitDialogGo = MakeRect("SplitDialog", parent);
        var rect = splitDialogGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 140f);

        var bg = splitDialogGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 0.98f);

        MakeText("SplitTitle", splitDialogGo.transform, 18, TextAnchor.MiddleCenter, TextBright,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -8f), new Vector2(0, 32f)).text = "拆分数量";

        var inputGo = MakeRect("Input", splitDialogGo.transform);
        var inputRect = inputGo.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(140f, 36f);
        inputRect.anchoredPosition = new Vector2(0, 4f);
        var inputBG = inputGo.AddComponent<Image>();
        inputBG.color = new Color(0.2f, 0.2f, 0.25f);
        splitInput = inputGo.AddComponent<InputField>();
        splitInput.contentType = InputField.ContentType.IntegerNumber;

        var inputText = MakeRect("Text", inputGo.transform);
        var itRect = inputText.GetComponent<RectTransform>();
        itRect.anchorMin = Vector2.zero;
        itRect.anchorMax = Vector2.one;
        itRect.sizeDelta = new Vector2(-8f, 0);
        var itText = inputText.AddComponent<Text>();
        itText.font = GetFont();
        itText.fontSize = 18;
        itText.alignment = TextAnchor.MiddleCenter;
        itText.color = TextBright;
        splitInput.textComponent = itText;

        CreateButton("SplitOK", splitDialogGo.transform,
            new Vector2(0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 12f), new Vector2(80f, 32f),
            "确认", OnSplitConfirm);

        CreateButton("SplitCancel", splitDialogGo.transform,
            new Vector2(0.7f, 0f), new Vector2(0.7f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0, 12f), new Vector2(80f, 32f),
            "取消", () => splitDialogGo.SetActive(false));

        splitDialogGo.SetActive(false);
    }

    void BuildTooltip(Transform parent)
    {
        tooltipGo = MakeRect("SlotTooltip", parent);
        var rect = tooltipGo.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140f, 30f);
        rect.pivot = new Vector2(0.5f, 0f);
        var bg = tooltipGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        bg.raycastTarget = false;

        tooltipNameText = MakeText("TName", tooltipGo.transform, 14, TextAnchor.MiddleCenter, TextBright,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        tooltipNameText.raycastTarget = false;

        tooltipGo.SetActive(false);
    }

    void BindEvents()
    {
        Model.OnMainSlotChanged += idx => RefreshMainSlot(idx);
        Model.OnHotbarSlotChanged += idx => RefreshHotbarSlot(idx);
        Model.OnGoldChanged += RefreshGold;
        Model.OnSlotsUnlocked += OnSlotsUnlocked;
    }

    void RefreshMainSlot(int index)
    {
        if (mainSlotBGs == null) return;
        if (!Model.IsSlotUnlocked(index))
        {
            mainSlotBGs[index].color = new Color(0.08f, 0.08f, 0.1f, 0.5f);
            mainSlotIcons[index].color = new Color(0.3f, 0.3f, 0.35f, 0.4f);
            mainSlotCounts[index].text = "";
            mainSlotBorders[index].color = new Color(0.15f, 0.15f, 0.18f, 0.4f);
            return;
        }
        var slot = Model.mainSlots[index];
        RefreshSlotVisuals(slot, mainSlotBGs[index], mainSlotIcons[index],
            mainSlotCounts[index], mainSlotBorders[index],
            !selectedIsHotbar && selectedIndex == index);
    }

    void RefreshHotbarSlot(int index)
    {
        if (hotbarSlotBGs == null) return;
        var slot = Model.hotbarSlots[index];
        RefreshSlotVisuals(slot, hotbarSlotBGs[index], hotbarSlotIcons[index],
            hotbarSlotCounts[index], hotbarSlotBorders[index],
            selectedIsHotbar && selectedIndex == index);
    }

    void RefreshSlotVisuals(GameJamInventorySlot slot, Image bg, Image icon,
        Text count, Image border, bool selected)
    {
        if (slot.IsEmpty)
        {
            bg.color = selected ? SlotSelected : SlotEmpty;
            GameJamArtLoader.ClearIcon(icon);
            count.text = "";
            border.color = selected ? GameJamItemDB.GetRarityColor(GameJamRarity.Common) : BorderDefault;
        }
        else
        {
            var def = GameJamItemDB.Get(slot.itemId);
            bg.color = selected ? SlotSelected : SlotFilled;
            GameJamArtLoader.ApplyItemIcon(icon, slot.itemId, Color.gray);
            count.text = slot.count > 1 ? slot.count.ToString() : "";
            if (selected && def != null)
                border.color = GameJamItemDB.GetRarityColor(def.rarity);
            else if (def != null && def.rarity > GameJamRarity.Common)
                border.color = GameJamItemDB.GetRarityColor(def.rarity) * 0.6f;
            else
                border.color = BorderDefault;
        }
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < GameJamInventoryModel.MainSlotCount; i++)
            RefreshMainSlot(i);
        for (int i = 0; i < GameJamInventoryModel.HotbarSlotCount; i++)
            RefreshHotbarSlot(i);
        RefreshGold();
        RefreshDetail();
        RefreshUnlockButton();
    }

    void RefreshGold()
    {
        if (goldText != null)
            goldText.text = $"金币: {Model.gold}";
        RefreshUnlockButton();
    }

    public void SelectSlot(bool isHotbar, int index)
    {
        int prevIdx = selectedIndex;
        bool prevHotbar = selectedIsHotbar;

        selectedIndex = index;
        selectedIsHotbar = isHotbar;

        if (prevIdx >= 0)
        {
            if (prevHotbar) RefreshHotbarSlot(prevIdx);
            else RefreshMainSlot(prevIdx);
        }

        if (isHotbar) RefreshHotbarSlot(index);
        else RefreshMainSlot(index);

        RefreshDetail();
    }

    public void ClearSelection()
    {
        int prevIdx = selectedIndex;
        bool prevHotbar = selectedIsHotbar;
        selectedIndex = -1;

        if (prevIdx >= 0)
        {
            if (prevHotbar) RefreshHotbarSlot(prevIdx);
            else RefreshMainSlot(prevIdx);
        }

        detailGo.SetActive(false);
    }

    void RefreshDetail()
    {
        if (selectedIndex < 0)
        {
            detailGo.SetActive(false);
            return;
        }

        var slots = selectedIsHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (selectedIndex >= slots.Length || slots[selectedIndex].IsEmpty)
        {
            detailGo.SetActive(false);
            return;
        }

        var slot = slots[selectedIndex];
        var def = GameJamItemDB.Get(slot.itemId);
        if (def == null)
        {
            detailGo.SetActive(false);
            return;
        }

        detailGo.SetActive(true);
        GameJamArtLoader.ApplyItemIcon(detailIcon, slot.itemId, def.iconColor);
        detailName.text = def.name;
        var rarityColor = GameJamItemDB.GetRarityColor(def.rarity);
        detailType.text = $"{GameJamItemDB.GetTypeName(def.type)} · {GameJamItemDB.GetRarityName(def.rarity)}";
        detailType.color = Color.white;
        detailRarityBar.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
        detailDesc.text = def.description;
        detailPrice.text = $"售价: {def.sellPrice}g";
    }

    public void ShowSplitDialog(bool isHotbar, int slotIndex)
    {
        splitFromHotbar = isHotbar;
        splitFromIndex = slotIndex;

        var slots = isHotbar ? Model.hotbarSlots : Model.mainSlots;
        int half = slots[slotIndex].count / 2;
        splitInput.text = half.ToString();

        splitDialogGo.SetActive(true);
    }

    void OnSplitConfirm()
    {
        if (!int.TryParse(splitInput.text, out int amount) || amount <= 0)
        {
            splitDialogGo.SetActive(false);
            return;
        }

        var allSlots = splitFromHotbar ? Model.hotbarSlots : Model.mainSlots;
        int emptyIdx = -1;
        bool emptyIsHotbar = false;

        for (int i = 0; i < Model.mainSlots.Length; i++)
        {
            if (Model.mainSlots[i].IsEmpty)
            {
                emptyIdx = i;
                emptyIsHotbar = false;
                break;
            }
        }
        if (emptyIdx < 0)
        {
            for (int i = 0; i < Model.hotbarSlots.Length; i++)
            {
                if (Model.hotbarSlots[i].IsEmpty)
                {
                    emptyIdx = i;
                    emptyIsHotbar = true;
                    break;
                }
            }
        }

        if (emptyIdx >= 0)
        {
            Model.SplitStack(splitFromHotbar, splitFromIndex, amount, emptyIsHotbar, emptyIdx);
            RefreshAllSlots();
        }

        splitDialogGo.SetActive(false);
    }

    void OnSortClicked()
    {
        Model.Sort();
        ClearSelection();
    }

    void OnDiscardClicked()
    {
        if (selectedIndex < 0) return;
        Model.RemoveAt(selectedIsHotbar, selectedIndex);
        ClearSelection();
    }

    void OnSellClicked()
    {
        if (selectedIndex < 0) return;
        var slots = selectedIsHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (selectedIndex >= slots.Length || slots[selectedIndex].IsEmpty) return;

        var slot = slots[selectedIndex];
        var def = GameJamItemDB.Get(slot.itemId);
        if (def == null || def.sellPrice <= 0)
        {
            Toast.ShowToast("该物品无法售卖");
            return;
        }

        int amount = slot.count;
        int totalGold = def.sellPrice * amount;
        Model.RemoveItem(slot.itemId, amount);
        Model.AddGold(totalGold);
        Toast.ShowToast($"售出 {def.name} x{amount}，获得 {totalGold} 金币");
        ClearSelection();
    }

    void OnUnlockClicked()
    {
        if (Model.UnlockRow())
        {
            Toast.ShowToast("解锁了新的背包行!");
        }
    }

    void RefreshUnlockButton()
    {
        if (unlockBtn == null) return;

        int cost = Model.GetUnlockCost();
        if (cost < 0)
        {
            unlockBtnText.text = "已全部解锁";
            unlockBtn.interactable = false;
        }
        else
        {
            unlockBtnText.text = $"解锁一行 ({cost} 金币)";
            unlockBtn.interactable = Model.gold >= cost;
        }
    }

    void OnSlotsUnlocked()
    {
        RefreshAllSlots();
    }

    public void ShowSlotTooltip(bool isHotbar, int index, RectTransform slotRect)
    {
        var slots = isHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty) return;
        var def = GameJamItemDB.Get(slots[index].itemId);
        if (def == null) return;

        tooltipNameText.text = def.name;

        var ttRect = tooltipGo.GetComponent<RectTransform>();
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasGo.GetComponent<RectTransform>(),
            RectTransformUtility.WorldToScreenPoint(null, slotRect.position),
            null, out worldPos);
        ttRect.position = worldPos + new Vector3(0, SlotSize * 0.6f, 0);

        tooltipGo.SetActive(true);
    }

    public void HideSlotTooltip()
    {
        if (tooltipGo != null) tooltipGo.SetActive(false);
    }

    public void Show()
    {
        EnsureUI();
        canvasGo.SetActive(true);
        RefreshAllSlots();
    }

    public void Hide()
    {
        if (canvasGo != null) canvasGo.SetActive(false);
        if (splitDialogGo != null) splitDialogGo.SetActive(false);
        ClearSelection();
    }

    public void Cleanup()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    void OnDestroy() => Cleanup();

    // ---------- UI helpers ----------

    void CreateButton(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = MakeRect(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.22f, 0.28f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
        colors.pressedColor = new Color(0.18f, 0.18f, 0.24f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txtGo = MakeRect("Text", go.transform);
        var tRect = txtGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.font = GetFont();
        txt.fontSize = 16;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = TextBright;
        txt.text = label;
    }

    Text MakeText(string name, Transform parent, int fontSize, TextAnchor align, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 pos, Vector2 size)
    {
        var go = MakeRect(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        var txt = go.AddComponent<Text>();
        txt.font = GetFont();
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = color;
        return txt;
    }

    static Font GetFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }
}
