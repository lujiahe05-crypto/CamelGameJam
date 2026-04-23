using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateInventoryWindowPrefab
{
    const string PrefabPath = "Assets/Games/GameJam/Resources/GameJamUI/InventoryWindow.prefab";
    const string UiFolder = "Assets/Games/GameJam/Resources/GameJamUI";
    const string LegacyPrefabPath = "Assets/Games/GameJam/Resources/UI/Inventory Window Prefab.prefab";

    [InitializeOnLoadMethod]
    static void AutoCreate()
    {
        //EditorApplication.delayCall += TryForceRebuildAfterCompile;
    }

    static void TryForceRebuildAfterCompile()
    {
        EditorApplication.delayCall -= TryForceRebuildAfterCompile;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        Create();
    }

    [MenuItem("GameJam/UI/Create Inventory Window Prefab")]
    public static void Create()
    {
        EnsureFolder();
        RemoveLegacyPrefab();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        var root = CreateUIObject("InventoryWindow", null);
        Stretch(root);
        var canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.gameObject.AddComponent<GraphicRaycaster>();
        root.gameObject.AddComponent<CanvasGroup>();
        root.gameObject.SetActive(false);

        var dimmer = CreateUIObject("Dimmer", root.transform);
        Stretch(dimmer);
        AddImage(dimmer.gameObject, new Color(0f, 0f, 0f, 0.48f));

        var safeArea = CreateUIObject("SafeArea", root.transform);
        Stretch(safeArea, new Vector2(36f, 26f), new Vector2(-36f, -26f));

        CreateTopBar(safeArea.transform);
        CreateBody(safeArea.transform);
        CreateTooltip(safeArea.transform);
        CreateSplitDialog(safeArea.transform);
        CreateDragLayer(safeArea.transform);

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.Refresh();
        Debug.Log("InventoryWindow prefab created at " + PrefabPath);
    }

    static void RemoveLegacyPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath) != null)
            AssetDatabase.DeleteAsset(LegacyPrefabPath);
    }

    static void CreateTopBar(Transform parent)
    {
        var topBar = CreateUIObject("TopBar", parent);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.sizeDelta = new Vector2(0f, 110f);
        topBar.anchoredPosition = Vector2.zero;

        var topBarBg = CreateUIObject("TopBarBg", topBar.transform);
        Stretch(topBarBg);
        var topBarBgImage = AddImage(
            topBarBg.gameObject,
            new Color(1f, 1f, 1f, 0.98f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/uititle_bg.png"));
        topBarBgImage.type = Image.Type.Sliced;

        var title = CreateUIObject("Title_Backpack", topBar.transform);
        title.anchorMin = new Vector2(0f, 0.5f);
        title.anchorMax = new Vector2(0f, 0.5f);
        title.pivot = new Vector2(0f, 0.5f);
        title.sizeDelta = new Vector2(220f, 56f);
        title.anchoredPosition = new Vector2(24f, -4f);
        AddText(title.gameObject, "Inventory", 30, Color.white, TextAnchor.MiddleLeft);
        AddOutline(title.gameObject, new Color(0.38f, 0.24f, 0.18f, 0.95f), new Vector2(2f, -2f));

        var tabs = CreateUIObject("Tabs", topBar.transform);
        tabs.anchorMin = new Vector2(0.5f, 0.5f);
        tabs.anchorMax = new Vector2(0.5f, 0.5f);
        tabs.pivot = new Vector2(0.5f, 0.5f);
        tabs.sizeDelta = new Vector2(980f, 92f);
        tabs.anchoredPosition = new Vector2(30f, -4f);

        string[] names = { "Backpack", "Role", "Task", "Handbook", "Social", "Map", "Calendar", "Album" };
        string[] labels = { "Bag", "Role", "Task", "Guide", "Social", "Map", "Date", "Album" };
        Sprite[] icons =
        {
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_1.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_4.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_3.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_8.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_5.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_2.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_6.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/menu_9.png")
        };

        for (int i = 0; i < names.Length; i++)
        {
            var tab = CreateTab(tabs.transform, "Tab_" + names[i], labels[i], i == 0, icons[i]);
            var rect = tab.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(i * 118f, 0f);
        }

        var close = CreateButton(topBar.transform, "Btn_Close", new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(62f, 62f),
            string.Empty, new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_red.png"));
        var icon = CreateUIObject("Icon_X", close.transform);
        Stretch(icon);
        var closeIcon = AddImage(icon.gameObject, Color.white, false, LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/close_select.png"));
        closeIcon.preserveAspect = true;
    }

    static void CreateBody(Transform parent)
    {
        var body = CreateUIObject("Body", parent);
        body.anchorMin = new Vector2(0f, 0f);
        body.anchorMax = new Vector2(1f, 1f);
        body.offsetMin = new Vector2(0f, 0f);
        body.offsetMax = new Vector2(0f, -110f);

        var leftPanel = CreateUIObject("LeftPanel", body.transform);
        leftPanel.anchorMin = new Vector2(0f, 0f);
        leftPanel.anchorMax = new Vector2(0f, 1f);
        leftPanel.pivot = new Vector2(0f, 0.5f);
        leftPanel.sizeDelta = new Vector2(470f, 0f);
        leftPanel.anchoredPosition = new Vector2(0f, 0f);
        var leftPanelBg = CreateUIObject("LeftPanelBg", leftPanel.transform);
        Stretch(leftPanelBg);
        var leftPanelBgImage = AddImage(
            leftPanelBg.gameObject,
            new Color(1f, 1f, 1f, 0.92f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        leftPanelBgImage.type = Image.Type.Sliced;

        var characterName = CreateUIObject("CharacterName", leftPanel.transform);
        characterName.anchorMin = new Vector2(0f, 1f);
        characterName.anchorMax = new Vector2(0f, 1f);
        characterName.pivot = new Vector2(0f, 1f);
        characterName.sizeDelta = new Vector2(180f, 54f);
        characterName.anchoredPosition = new Vector2(18f, -120f);
        AddText(characterName.gameObject, "Player", 28, Color.white, TextAnchor.MiddleLeft);
        AddOutline(characterName.gameObject, new Color(0.42f, 0.42f, 0.42f, 0.9f), new Vector2(2f, -2f));

        CreateCharacterPreview(leftPanel.transform);
        CreateEquipSlots(leftPanel.transform);
        CreateStatsPanel(leftPanel.transform);

        var rightPanel = CreateUIObject("RightPanel", body.transform);
        rightPanel.anchorMin = new Vector2(0f, 0f);
        rightPanel.anchorMax = new Vector2(1f, 1f);
        rightPanel.offsetMin = new Vector2(490f, 40f);
        rightPanel.offsetMax = new Vector2(-12f, -26f);
        var rightPanelBg = CreateUIObject("RightPanelBg", rightPanel.transform);
        Stretch(rightPanelBg);
        var rightPanelBgImage = AddImage(
            rightPanelBg.gameObject,
            new Color(1f, 1f, 1f, 0.92f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        rightPanelBgImage.type = Image.Type.Sliced;

        CreateItemDetailPanel(rightPanel.transform);
        CreateBagPanel(rightPanel.transform);
        CreateBottomBar(rightPanel.transform);
        CreateHintBar(rightPanel.transform);
    }

    static void CreateCharacterPreview(Transform parent)
    {
        var previewRoot = CreateUIObject("CharacterPreviewRoot", parent);
        previewRoot.anchorMin = new Vector2(0f, 0f);
        previewRoot.anchorMax = new Vector2(0f, 1f);
        previewRoot.pivot = new Vector2(0f, 0.5f);
        previewRoot.sizeDelta = new Vector2(240f, 0f);
        previewRoot.anchoredPosition = new Vector2(250f, 0f);

        var frame = CreateUIObject("PreviewFrame", previewRoot.transform);
        frame.anchorMin = new Vector2(0.5f, 0.5f);
        frame.anchorMax = new Vector2(0.5f, 0.5f);
        frame.pivot = new Vector2(0.5f, 0.5f);
        frame.sizeDelta = new Vector2(220f, 470f);
        frame.anchoredPosition = new Vector2(-20f, -10f);
        var previewFrameImage = AddImage(
            frame.gameObject,
            new Color(1f, 1f, 1f, 0.68f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/texture3d/windows/windows001_2.png"));
        previewFrameImage.type = Image.Type.Sliced;

        var shadow = CreateUIObject("PreviewShadow", previewRoot.transform);
        shadow.anchorMin = new Vector2(0.5f, 0f);
        shadow.anchorMax = new Vector2(0.5f, 0f);
        shadow.pivot = new Vector2(0.5f, 0.5f);
        shadow.sizeDelta = new Vector2(140f, 32f);
        shadow.anchoredPosition = new Vector2(-20f, 60f);
        AddImage(shadow.gameObject, new Color(0f, 0f, 0f, 0.18f));

        var portrait = CreateUIObject("PreviewRawImage", previewRoot.transform);
        portrait.anchorMin = new Vector2(0.5f, 0.5f);
        portrait.anchorMax = new Vector2(0.5f, 0.5f);
        portrait.pivot = new Vector2(0.5f, 0.5f);
        portrait.sizeDelta = new Vector2(220f, 470f);
        portrait.anchoredPosition = new Vector2(-20f, -20f);
        var portraitImage = AddImage(
            portrait.gameObject,
            new Color(1f, 1f, 1f, 0.96f),
            false,
            LoadSprite(
                "Assets/Games/GameJam/assets/UI/headicon/Oaks.png",
                "Assets/Games/GameJam/assets/UI/minihead/Oaks.png"));
        portraitImage.preserveAspect = true;
    }

    static void CreateEquipSlots(Transform parent)
    {
        var equipGroup = CreateUIObject("EquipSlotGroup", parent);
        equipGroup.anchorMin = new Vector2(0f, 0.5f);
        equipGroup.anchorMax = new Vector2(0f, 0.5f);
        equipGroup.pivot = new Vector2(0f, 0.5f);
        equipGroup.sizeDelta = new Vector2(78f, 420f);
        equipGroup.anchoredPosition = new Vector2(456f, -14f);

        string[] equipNames = { "Head", "Body", "Legs", "Weapon", "Accessory1", "Accessory2" };
        Sprite[] equipIcons =
        {
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/player2.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/cloth.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/cloth.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/weapon.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/partner.png"),
            LoadSprite("Assets/Games/GameJam/assets/UI/mapicon/company.png")
        };
        for (int i = 0; i < equipNames.Length; i++)
        {
            var slot = CreateEquipSlot(equipGroup.transform, "EquipSlot_" + equipNames[i], equipIcons[i]);
            var rect = slot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -i * 72f);
        }
    }

    static void CreateStatsPanel(Transform parent)
    {
        var stats = CreateUIObject("StatsPanel", parent);
        stats.anchorMin = new Vector2(0f, 0f);
        stats.anchorMax = new Vector2(0f, 0f);
        stats.pivot = new Vector2(0f, 0f);
        stats.sizeDelta = new Vector2(280f, 390f);
        stats.anchoredPosition = new Vector2(0f, 20f);
        var statsBg = CreateUIObject("StatsBg", stats.transform);
        Stretch(statsBg);
        var statsBgImage = AddImage(
            statsBg.gameObject,
            new Color(1f, 1f, 1f, 0.8f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        statsBgImage.type = Image.Type.Sliced;

        var title = CreateUIObject("StatTitle", stats.transform);
        title.anchorMin = new Vector2(0f, 1f);
        title.anchorMax = new Vector2(1f, 1f);
        title.pivot = new Vector2(0.5f, 1f);
        title.sizeDelta = new Vector2(-20f, 28f);
        title.anchoredPosition = new Vector2(0f, -12f);
        AddText(title.gameObject, "Stats", 18, new Color(1f, 0.97f, 0.92f), TextAnchor.MiddleLeft);

        string[] barNames = { "Exp", "HP", "Stamina", "Endurance" };
        string[] barLabels = { "EXP", "HP", "Stamina", "Endurance" };
        Color[] barColors =
        {
            new Color(0.48f, 0.84f, 0.48f),
            new Color(0.96f, 0.62f, 0.65f),
            new Color(0.93f, 0.8f, 0.53f),
            new Color(0.48f, 0.84f, 0.48f)
        };

        for (int i = 0; i < barNames.Length; i++)
            CreateStatBar(stats.transform, "Stat_" + barNames[i], barLabels[i], barColors[i], new Vector2(10f, -44f - i * 58f));

        string[] plainNames = { "Attack", "Defense", "Crit", "MeleeCritDmg", "RangeCritDmg" };
        string[] plainLabels = { "Attack", "Defense", "Crit Rate", "Melee Crit Dmg", "Range Crit Dmg" };
        for (int i = 0; i < plainNames.Length; i++)
            CreatePlainStat(stats.transform, "Stat_" + plainNames[i], plainLabels[i], new Vector2(10f, -280f - i * 38f));
    }

    static void CreateItemDetailPanel(Transform parent)
    {
        var detail = CreateUIObject("ItemDetailPanel", parent);
        detail.anchorMin = new Vector2(0f, 0f);
        detail.anchorMax = new Vector2(0f, 1f);
        detail.pivot = new Vector2(0f, 0.5f);
        detail.sizeDelta = new Vector2(250f, 0f);
        detail.anchoredPosition = new Vector2(18f, 0f);

        var bg = CreateUIObject("DetailBg", detail.transform);
        Stretch(bg);
        var detailBgImage = AddImage(
            bg.gameObject,
            new Color(1f, 1f, 1f, 0.86f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        detailBgImage.type = Image.Type.Sliced;

        var previewBg = CreateUIObject("ItemPreviewBg", detail.transform);
        previewBg.anchorMin = new Vector2(0.5f, 1f);
        previewBg.anchorMax = new Vector2(0.5f, 1f);
        previewBg.pivot = new Vector2(0.5f, 1f);
        previewBg.sizeDelta = new Vector2(178f, 178f);
        previewBg.anchoredPosition = new Vector2(0f, -22f);
        AddImage(previewBg.gameObject, new Color(0.18f, 0.42f, 0.58f, 0.42f));

        var previewIcon = CreateUIObject("ItemPreviewIcon", previewBg.transform);
        Stretch(previewIcon, new Vector2(18f, 18f), new Vector2(-18f, -18f));
        AddImage(previewIcon.gameObject, new Color(1f, 1f, 1f, 0f));

        CreateTextLine(detail.transform, "ItemName", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -220f), new Vector2(210f, 32f), "No Item Selected", 22, Color.white);
        CreateTextLine(detail.transform, "ItemType", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(210f, 26f), "Type", 15, new Color(0.92f, 0.95f, 0.98f));
        CreateTextLine(detail.transform, "ItemDesc", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -296f), new Vector2(210f, 84f), "Item description", 14, new Color(0.9f, 0.94f, 0.97f), TextAnchor.UpperLeft);

        var priceGroup = CreateUIObject("ItemPriceGroup", detail.transform);
        priceGroup.anchorMin = new Vector2(0.5f, 0f);
        priceGroup.anchorMax = new Vector2(0.5f, 0f);
        priceGroup.pivot = new Vector2(0.5f, 0f);
        priceGroup.sizeDelta = new Vector2(210f, 30f);
        priceGroup.anchoredPosition = new Vector2(0f, 26f);

        var priceIcon = CreateUIObject("PriceIcon", priceGroup.transform);
        priceIcon.anchorMin = new Vector2(0f, 0.5f);
        priceIcon.anchorMax = new Vector2(0f, 0.5f);
        priceIcon.pivot = new Vector2(0f, 0.5f);
        priceIcon.sizeDelta = new Vector2(24f, 24f);
        priceIcon.anchoredPosition = new Vector2(8f, 0f);
        AddImage(priceIcon.gameObject, Color.white, false, LoadSprite("Assets/Games/GameJam/assets/UI/money.png"));

        CreateTextLine(priceGroup.transform, "PriceText", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(150f, 24f), "0", 18, new Color(1f, 0.92f, 0.48f), TextAnchor.MiddleLeft);
        CreateTextLine(detail.transform, "CompareHint", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(-20f, 20f), "Click an item to inspect", 12, new Color(0.92f, 0.95f, 0.98f, 0.8f));
    }

    static void CreateBagPanel(Transform parent)
    {
        var bagPanel = CreateUIObject("BagPanel", parent);
        bagPanel.anchorMin = new Vector2(0f, 0f);
        bagPanel.anchorMax = new Vector2(1f, 1f);
        bagPanel.offsetMin = new Vector2(286f, 122f);
        bagPanel.offsetMax = new Vector2(-18f, -102f);

        var frame = CreateUIObject("BagFrame", bagPanel.transform);
        Stretch(frame);
        var bagFrameImage = AddImage(
            frame.gameObject,
            new Color(1f, 1f, 1f, 0.95f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        bagFrameImage.type = Image.Type.Sliced;

        var pageLeft = CreatePageButton(bagPanel.transform, "Btn_PageLeft", new Vector2(0f, 0.5f), new Vector2(-18f, 0f), "<");
        var pageRight = CreatePageButton(bagPanel.transform, "Btn_PageRight", new Vector2(1f, 0.5f), new Vector2(18f, 0f), ">");
        pageLeft.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        pageRight.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        var viewport = CreateUIObject("BagGridViewport", bagPanel.transform);
        viewport.anchorMin = new Vector2(0f, 0f);
        viewport.anchorMax = new Vector2(1f, 1f);
        viewport.offsetMin = new Vector2(20f, 98f);
        viewport.offsetMax = new Vector2(-20f, -20f);
        var viewportImage = AddImage(viewport.gameObject, new Color(0.83f, 0.86f, 0.9f, 0.85f), false);
        viewportImage.type = Image.Type.Sliced;
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        var content = CreateUIObject("BagGridContent", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.sizeDelta = new Vector2(636f, 406f);
        content.anchoredPosition = new Vector2(18f, -18f);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(68f, 68f);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;

        for (int i = 0; i < 40; i++)
            CreateInventorySlot(content.transform, "Slot_" + i.ToString("00"), i >= 32);

        var hotbarPanel = CreateUIObject("HotbarPanel", bagPanel.transform);
        hotbarPanel.anchorMin = new Vector2(0f, 0f);
        hotbarPanel.anchorMax = new Vector2(1f, 0f);
        hotbarPanel.pivot = new Vector2(0.5f, 0f);
        hotbarPanel.sizeDelta = new Vector2(0f, 76f);
        hotbarPanel.anchoredPosition = new Vector2(0f, 8f);

        var hotbarFrame = CreateUIObject("HotbarFrame", hotbarPanel.transform);
        Stretch(hotbarFrame);
        var hotbarFrameImage = AddImage(
            hotbarFrame.gameObject,
            new Color(1f, 1f, 1f, 0.95f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/new_bg.png"));
        hotbarFrameImage.type = Image.Type.Sliced;

        var hotbarContent = CreateUIObject("HotbarContent", hotbarPanel.transform);
        hotbarContent.anchorMin = new Vector2(0.5f, 0.5f);
        hotbarContent.anchorMax = new Vector2(0.5f, 0.5f);
        hotbarContent.pivot = new Vector2(0.5f, 0.5f);
        hotbarContent.sizeDelta = new Vector2(610f, 56f);
        hotbarContent.anchoredPosition = Vector2.zero;
        var hotbarLayout = hotbarContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        hotbarLayout.spacing = 8f;
        hotbarLayout.childForceExpandWidth = false;
        hotbarLayout.childForceExpandHeight = false;
        hotbarLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < 8; i++)
            CreateHotbarSlot(hotbarContent.transform, "HotbarSlot_" + i, i + 1);
    }

    static void CreateBottomBar(Transform parent)
    {
        var bottomBar = CreateUIObject("BottomBar", parent);
        bottomBar.anchorMin = new Vector2(0f, 0f);
        bottomBar.anchorMax = new Vector2(1f, 0f);
        bottomBar.pivot = new Vector2(0.5f, 0f);
        bottomBar.sizeDelta = new Vector2(0f, 62f);
        bottomBar.anchoredPosition = new Vector2(0f, 22f);

        var currency = CreateUIObject("CurrencyGroup", bottomBar.transform);
        currency.anchorMin = new Vector2(1f, 0.5f);
        currency.anchorMax = new Vector2(1f, 0.5f);
        currency.pivot = new Vector2(1f, 0.5f);
        currency.sizeDelta = new Vector2(150f, 44f);
        currency.anchoredPosition = new Vector2(-18f, 0f);
        AddImage(CreateUIObject("MoneyBg", currency.transform).gameObject, new Color(0.2f, 0.45f, 0.63f, 0.65f));

        var moneyIcon = CreateUIObject("MoneyIcon", currency.transform);
        moneyIcon.anchorMin = new Vector2(0f, 0.5f);
        moneyIcon.anchorMax = new Vector2(0f, 0.5f);
        moneyIcon.pivot = new Vector2(0f, 0.5f);
        moneyIcon.sizeDelta = new Vector2(26f, 26f);
        moneyIcon.anchoredPosition = new Vector2(10f, 0f);
        AddImage(moneyIcon.gameObject, Color.white, false, LoadSprite("Assets/Games/GameJam/assets/UI/money.png"));

        CreateTextLine(currency.transform, "MoneyText", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(-44f, 26f), "0", 18, Color.white, TextAnchor.MiddleRight);

        CreateButton(bottomBar.transform, "Btn_Use", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(110f, 44f), "Sell", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_blue.png"));
        CreateButton(bottomBar.transform, "Btn_Split", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(144f, 0f), new Vector2(110f, 44f), "Split", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_white.png"));
        CreateButton(bottomBar.transform, "Btn_Sort", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(270f, 0f), new Vector2(110f, 44f), "Sort", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_green.png"));
        CreateButton(bottomBar.transform, "Btn_Discard", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(396f, 0f), new Vector2(110f, 44f), "Drop", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_red.png"));
        CreateButton(bottomBar.transform, "Btn_Unlock", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(522f, 0f), new Vector2(160f, 44f), "Unlock", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_white.png"));
    }

    static void CreateHintBar(Transform parent)
    {
        var hintBar = CreateUIObject("ControlHintBar", parent);
        hintBar.anchorMin = new Vector2(0f, 0f);
        hintBar.anchorMax = new Vector2(1f, 0f);
        hintBar.pivot = new Vector2(0.5f, 0f);
        hintBar.sizeDelta = new Vector2(0f, 24f);
        hintBar.anchoredPosition = new Vector2(0f, -8f);

        string[] names = { "Select", "Use", "QuickMove", "Split" };
        string[] labels = { "Mouse Select", "Mouse Use", "R Quick Move", "Shift + Click Split" };
        for (int i = 0; i < names.Length; i++)
        {
            var hint = CreateUIObject("Hint_" + names[i], hintBar.transform);
            hint.anchorMin = new Vector2(0f, 0.5f);
            hint.anchorMax = new Vector2(0f, 0.5f);
            hint.pivot = new Vector2(0f, 0.5f);
            hint.sizeDelta = new Vector2(i == 3 ? 260f : 180f, 22f);
            hint.anchoredPosition = new Vector2(90f + i * 210f, 0f);
            AddText(hint.gameObject, labels[i], 12, Color.white, TextAnchor.MiddleCenter);
            var text = hint.GetComponent<Text>();
            text.color = new Color(0.96f, 0.97f, 0.99f, 0.95f);
        }
    }

    static void CreateTooltip(Transform parent)
    {
        var tooltip = CreateUIObject("Tooltip", parent);
        tooltip.anchorMin = new Vector2(0f, 0f);
        tooltip.anchorMax = new Vector2(0f, 0f);
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.sizeDelta = new Vector2(260f, 156f);
        tooltip.anchoredPosition = new Vector2(780f, 600f);
        tooltip.gameObject.SetActive(false);

        var bg = CreateUIObject("TooltipBg", tooltip.transform);
        Stretch(bg);
        AddImage(bg.gameObject, new Color(0.11f, 0.15f, 0.2f, 0.97f));

        CreateTextLine(tooltip.transform, "TooltipName", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(-16f, 24f), "Item Name", 18, Color.white, TextAnchor.MiddleLeft);
        CreateTextLine(tooltip.transform, "TooltipType", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(-16f, 20f), "Item Type", 13, new Color(0.76f, 0.9f, 1f), TextAnchor.MiddleLeft);
        CreateTextLine(tooltip.transform, "TooltipDesc", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(-16f, 54f), "Item Description", 13, new Color(0.95f, 0.98f, 1f), TextAnchor.UpperLeft);
        CreateTextLine(tooltip.transform, "TooltipPrice", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(-16f, 20f), "Price", 13, new Color(1f, 0.92f, 0.48f), TextAnchor.MiddleLeft);
        CreateTextLine(tooltip.transform, "TooltipCompare", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-16f, 18f), "Compare", 12, new Color(0.75f, 0.95f, 0.8f), TextAnchor.MiddleLeft);
    }

    static void CreateSplitDialog(Transform parent)
    {
        var dialog = CreateUIObject("SplitDialog", parent);
        dialog.anchorMin = new Vector2(0.5f, 0.5f);
        dialog.anchorMax = new Vector2(0.5f, 0.5f);
        dialog.pivot = new Vector2(0.5f, 0.5f);
        dialog.sizeDelta = new Vector2(280f, 170f);
        dialog.anchoredPosition = Vector2.zero;
        dialog.gameObject.SetActive(false);

        var bg = CreateUIObject("DialogBg", dialog.transform);
        Stretch(bg);
        AddImage(bg.gameObject, new Color(0.12f, 0.18f, 0.24f, 0.98f));

        CreateTextLine(dialog.transform, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-20f, 28f), "Split Amount", 20, Color.white);

        var inputBg = CreateUIObject("InputBg", dialog.transform);
        inputBg.anchorMin = new Vector2(0.5f, 0.5f);
        inputBg.anchorMax = new Vector2(0.5f, 0.5f);
        inputBg.pivot = new Vector2(0.5f, 0.5f);
        inputBg.sizeDelta = new Vector2(140f, 40f);
        inputBg.anchoredPosition = new Vector2(0f, 8f);
        AddImage(inputBg.gameObject, new Color(0.22f, 0.3f, 0.38f, 1f));

        var inputFieldRect = CreateUIObject("InputField", inputBg.transform);
        Stretch(inputFieldRect, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        var text = AddText(inputFieldRect.gameObject, "1", 18, Color.white, TextAnchor.MiddleCenter);
        var field = inputFieldRect.gameObject.AddComponent<InputField>();
        field.textComponent = text;
        field.text = "1";

        CreateButton(dialog.transform, "Btn_Confirm", new Vector2(0.3f, 0f), new Vector2(0.3f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(90f, 34f), "Confirm", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_green.png"));
        CreateButton(dialog.transform, "Btn_Cancel", new Vector2(0.7f, 0f), new Vector2(0.7f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(90f, 34f), "Cancel", new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_white.png"));
    }

    static void CreateDragLayer(Transform parent)
    {
        var dragLayer = CreateUIObject("DragLayer", parent);
        Stretch(dragLayer);
        dragLayer.gameObject.SetActive(true);

        var dragIcon = CreateUIObject("DragIcon", dragLayer.transform);
        dragIcon.anchorMin = new Vector2(0f, 0f);
        dragIcon.anchorMax = new Vector2(0f, 0f);
        dragIcon.pivot = new Vector2(0.5f, 0.5f);
        dragIcon.sizeDelta = new Vector2(60f, 60f);
        dragIcon.anchoredPosition = new Vector2(0f, 0f);
        AddImage(dragIcon.gameObject, new Color(1f, 1f, 1f, 0.75f));
        dragIcon.gameObject.SetActive(false);
    }

    static GameObject CreateTab(Transform parent, string name, string label, bool selected, Sprite iconSprite)
    {
        var tab = CreateUIObject(name, parent);
        tab.sizeDelta = new Vector2(100f, 92f);
        var btn = tab.gameObject.AddComponent<Button>();
        btn.targetGraphic = AddImage(tab.gameObject, Color.clear);

        var bgNormal = CreateUIObject("Bg_Normal", tab.transform);
        bgNormal.anchorMin = new Vector2(0.5f, 0.5f);
        bgNormal.anchorMax = new Vector2(0.5f, 0.5f);
        bgNormal.pivot = new Vector2(0.5f, 0.5f);
        bgNormal.sizeDelta = new Vector2(80f, 80f);
        var normalImage = AddImage(
            bgNormal.gameObject,
            new Color(1f, 1f, 1f, selected ? 0f : 1f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/uimenu_bg.png"));
        normalImage.preserveAspect = true;

        var bgSelected = CreateUIObject("Bg_Selected", tab.transform);
        bgSelected.anchorMin = new Vector2(0.5f, 0.5f);
        bgSelected.anchorMax = new Vector2(0.5f, 0.5f);
        bgSelected.pivot = new Vector2(0.5f, 0.5f);
        bgSelected.sizeDelta = new Vector2(92f, 92f);
        var selectedImage = AddImage(
            bgSelected.gameObject,
            new Color(1f, 1f, 1f, selected ? 1f : 0f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/uimenu_bg_select.png"));
        selectedImage.preserveAspect = true;

        var icon = CreateUIObject("Icon", tab.transform);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(48f, 48f);
        icon.anchoredPosition = new Vector2(0f, 0f);
        var iconImage = AddImage(icon.gameObject, new Color(1f, 1f, 1f, 0.85f), false, iconSprite);
        iconImage.preserveAspect = true;

        var labelRect = CreateUIObject("Label", tab.transform);
        labelRect.anchorMin = new Vector2(0.5f, 0f);
        labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(100f, 28f);
        labelRect.anchoredPosition = new Vector2(0f, 0f);
        AddText(labelRect.gameObject, label, 16, Color.white, TextAnchor.MiddleCenter);
        AddOutline(labelRect.gameObject, new Color(0.24f, 0.45f, 0.7f, 0.9f), new Vector2(1f, -1f));

        var arrow = CreateUIObject("SelectArrow", tab.transform);
        arrow.anchorMin = new Vector2(0.5f, 0f);
        arrow.anchorMax = new Vector2(0.5f, 0f);
        arrow.pivot = new Vector2(0.5f, 0f);
        arrow.sizeDelta = new Vector2(18f, 14f);
        arrow.anchoredPosition = new Vector2(0f, -10f);
        AddImage(arrow.gameObject, new Color(1f, 0.88f, 0.42f, 0f));

        return tab.gameObject;
    }

    static GameObject CreateEquipSlot(Transform parent, string name, Sprite slotIcon)
    {
        var slot = CreateUIObject(name, parent);
        slot.sizeDelta = new Vector2(62f, 62f);
        var bg = CreateUIObject("Bg", slot.transform);
        Stretch(bg);
        var bgImage = AddImage(
            bg.gameObject,
            new Color(1f, 1f, 1f, 1f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/equip_bg.png"));
        bgImage.preserveAspect = true;

        var icon = CreateUIObject("Icon", slot.transform);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.pivot = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(22f, 22f);
        var hintIcon = AddImage(icon.gameObject, new Color(0.36f, 0.66f, 0.84f, 0.82f), false, slotIcon);
        hintIcon.preserveAspect = true;

        var itemIcon = CreateUIObject("ItemIcon", slot.transform);
        Stretch(itemIcon, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        AddImage(itemIcon.gameObject, new Color(1f, 1f, 1f, 0f), false);

        var selected = CreateUIObject("Selected", slot.transform);
        Stretch(selected);
        var selectedImage = AddImage(
            selected.gameObject,
            new Color(1f, 1f, 1f, 0f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/equip_bg_select.png"));
        selectedImage.preserveAspect = true;
        var lockNode = CreateUIObject("Lock", slot.transform);
        Stretch(lockNode);
        AddImage(lockNode.gameObject, new Color(0.2f, 0.2f, 0.2f, 0f), false);
        slot.gameObject.AddComponent<Button>();
        return slot.gameObject;
    }

    static void CreateStatBar(Transform parent, string name, string label, Color fillColor, Vector2 anchoredPosition)
    {
        var stat = CreateUIObject(name, parent);
        stat.anchorMin = new Vector2(0f, 1f);
        stat.anchorMax = new Vector2(0f, 1f);
        stat.pivot = new Vector2(0f, 1f);
        stat.sizeDelta = new Vector2(260f, 50f);
        stat.anchoredPosition = anchoredPosition;

        CreateTextLine(stat.transform, "Label", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(60f, 20f), label, 14, Color.white, TextAnchor.MiddleLeft);
        CreateTextLine(stat.transform, "Value", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(120f, 20f), "0/0", 14, new Color(1f, 0.97f, 0.92f), TextAnchor.MiddleRight);

        var barBg = CreateUIObject("BarBg", stat.transform);
        barBg.anchorMin = new Vector2(0f, 0f);
        barBg.anchorMax = new Vector2(1f, 0f);
        barBg.pivot = new Vector2(0.5f, 0f);
        barBg.sizeDelta = new Vector2(0f, 18f);
        barBg.anchoredPosition = new Vector2(0f, 4f);
        AddImage(barBg.gameObject, new Color(1f, 1f, 1f, 0.42f));

        var fill = CreateUIObject("Fill", barBg.transform);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(0.75f, 1f);
        fill.offsetMin = new Vector2(3f, 3f);
        fill.offsetMax = new Vector2(-3f, -3f);
        AddImage(fill.gameObject, fillColor);
    }

    static void CreatePlainStat(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        var stat = CreateUIObject(name, parent);
        stat.anchorMin = new Vector2(0f, 1f);
        stat.anchorMax = new Vector2(0f, 1f);
        stat.pivot = new Vector2(0f, 1f);
        stat.sizeDelta = new Vector2(260f, 24f);
        stat.anchoredPosition = anchoredPosition;

        CreateTextLine(stat.transform, "Label", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(140f, 20f), label, 14, Color.white, TextAnchor.MiddleLeft);
        CreateTextLine(stat.transform, "Value", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(110f, 20f), "0", 14, new Color(1f, 0.97f, 0.92f), TextAnchor.MiddleRight);
    }

    static void CreateInventorySlot(Transform parent, string name, bool locked)
    {
        var slot = CreateUIObject(name, parent);
        slot.sizeDelta = new Vector2(68f, 68f);
        var le = slot.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 68f;
        le.preferredHeight = 68f;

        var bg = CreateUIObject("Bg", slot.transform);
        Stretch(bg);
        var bgImage = AddImage(
            bg.gameObject,
            new Color(1f, 1f, 1f, 1f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/bg_item.png"));
        bgImage.preserveAspect = true;

        var frame = CreateUIObject("QualityFrame", slot.transform);
        Stretch(frame, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        AddImage(frame.gameObject, new Color(1f, 1f, 1f, 0f), false);

        var icon = CreateUIObject("Icon", slot.transform);
        Stretch(icon, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        AddImage(icon.gameObject, new Color(1f, 1f, 1f, 0f), false);

        var count = CreateUIObject("Count", slot.transform);
        count.anchorMin = new Vector2(1f, 0f);
        count.anchorMax = new Vector2(1f, 0f);
        count.pivot = new Vector2(1f, 0f);
        count.sizeDelta = new Vector2(30f, 18f);
        count.anchoredPosition = new Vector2(-4f, 4f);
        AddText(count.gameObject, string.Empty, 12, Color.white, TextAnchor.LowerRight);

        var lockNode = CreateUIObject("Lock", slot.transform);
        Stretch(lockNode, new Vector2(18f, 18f), new Vector2(-18f, -18f));
        var lockImage = AddImage(
            lockNode.gameObject,
            new Color(1f, 1f, 1f, locked ? 1f : 0f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/bg_item_lock.png"));
        lockImage.preserveAspect = true;

        var selected = CreateUIObject("Selected", slot.transform);
        Stretch(selected);
        var selectedImage = AddImage(
            selected.gameObject,
            new Color(1f, 1f, 1f, 0f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/bg_item_select.png"));
        selectedImage.preserveAspect = true;

        var hitArea = CreateUIObject("HitArea", slot.transform);
        Stretch(hitArea);
        var button = hitArea.gameObject.AddComponent<Button>();
        button.targetGraphic = AddImage(hitArea.gameObject, new Color(1f, 1f, 1f, 0f));
    }

    static void CreateHotbarSlot(Transform parent, string name, int keyIndex)
    {
        var slot = CreateUIObject(name, parent);
        slot.sizeDelta = new Vector2(68f, 56f);
        var le = slot.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 68f;
        le.preferredHeight = 56f;

        var bg = CreateUIObject("Bg", slot.transform);
        Stretch(bg);
        var bgImage = AddImage(
            bg.gameObject,
            new Color(1f, 1f, 1f, 1f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/equip_bg.png"));
        bgImage.preserveAspect = true;
        var frame = CreateUIObject("QualityFrame", slot.transform);
        Stretch(frame, new Vector2(2f, 2f), new Vector2(-2f, -2f));
        AddImage(frame.gameObject, new Color(1f, 1f, 1f, 0f), false);

        var icon = CreateUIObject("Icon", slot.transform);
        Stretch(icon, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        AddImage(icon.gameObject, new Color(1f, 1f, 1f, 0f), false);

        var count = CreateUIObject("Count", slot.transform);
        count.anchorMin = new Vector2(1f, 0f);
        count.anchorMax = new Vector2(1f, 0f);
        count.pivot = new Vector2(1f, 0f);
        count.sizeDelta = new Vector2(24f, 16f);
        count.anchoredPosition = new Vector2(-4f, 4f);
        AddText(count.gameObject, string.Empty, 11, Color.white, TextAnchor.LowerRight);

        var keyLabel = CreateUIObject("KeyLabel", slot.transform);
        keyLabel.anchorMin = new Vector2(0f, 1f);
        keyLabel.anchorMax = new Vector2(0f, 1f);
        keyLabel.pivot = new Vector2(0f, 1f);
        keyLabel.sizeDelta = new Vector2(16f, 14f);
        keyLabel.anchoredPosition = new Vector2(4f, -2f);
        AddText(keyLabel.gameObject, keyIndex.ToString(), 10, new Color(1f, 1f, 1f, 0.82f), TextAnchor.UpperLeft);

        var selected = CreateUIObject("Selected", slot.transform);
        Stretch(selected);
        var selectedImage = AddImage(
            selected.gameObject,
            new Color(1f, 1f, 1f, keyIndex == 1 ? 1f : 0f),
            false,
            LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/equip_bg_select.png"));
        selectedImage.preserveAspect = true;

        var hitArea = CreateUIObject("HitArea", slot.transform);
        Stretch(hitArea);
        var button = hitArea.gameObject.AddComponent<Button>();
        button.targetGraphic = AddImage(hitArea.gameObject, new Color(1f, 1f, 1f, 0f));
    }

    static GameObject CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 anchoredPosition, Vector2 size, string label, Color color, Sprite backgroundSprite = null)
    {
        var rect = CreateUIObject(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var bg = CreateUIObject("Bg", rect.transform);
        Stretch(bg);
        var image = AddImage(bg.gameObject, color, true, backgroundSprite);
        image.preserveAspect = backgroundSprite != null;

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 0.88f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        if (!string.IsNullOrEmpty(label))
        {
            var text = CreateUIObject("Label", rect.transform);
            Stretch(text);
            AddText(text.gameObject, label, 18, Color.white, TextAnchor.MiddleCenter);
        }

        return rect.gameObject;
    }

    static GameObject CreatePageButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, string arrowText)
    {
        var button = CreateButton(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(36f, 36f), string.Empty, new Color(1f, 1f, 1f, 1f), LoadSprite("Assets/Games/GameJam/assets/UI/Texture2D/button_white.png"));
        var arrow = CreateUIObject("Arrow", button.transform);
        Stretch(arrow);
        AddText(arrow.gameObject, arrowText, 20, new Color(0.52f, 0.84f, 0.25f), TextAnchor.MiddleCenter);
        return button;
    }

    static Text CreateTextLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        var rect = CreateUIObject(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return AddText(rect.gameObject, content, fontSize, color, alignment);
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Games/GameJam/Resources"))
            AssetDatabase.CreateFolder("Assets/Games/GameJam", "Resources");

        if (!AssetDatabase.IsValidFolder(UiFolder))
            AssetDatabase.CreateFolder("Assets/Games/GameJam/Resources", "GameJamUI");
    }

    static RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = 5;
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
    }

    static Font GetBuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            font = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
        return font;
    }

    static Text AddText(GameObject go, string content, int fontSize, Color color, TextAnchor alignment)
    {
        go.AddComponent<CanvasRenderer>();
        var text = go.AddComponent<Text>();
        text.font = GetBuiltinFont();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static Outline AddOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        return outline;
    }

    static Image AddImage(GameObject go, Color color, bool raycastTarget = true, Sprite sprite = null)
    {
        go.AddComponent<CanvasRenderer>();
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        if (sprite != null)
            image.sprite = sprite;
        return image;
    }

    static Sprite LoadSprite(params string[] assetPaths)
    {
        foreach (var path in assetPaths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }

        return null;
    }
}
