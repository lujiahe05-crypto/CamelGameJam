using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameJamMachinePanel : MonoBehaviour
{
    GameJamMachine currentMachine;
    GameJamInventory inventory;

    GameObject canvasGo;
    GameObject panelGo;
    Text titleText;

    // Left column
    Transform recipeCardParent;
    RectTransform recipeCardRect;
    GameObject cardTemplate;
    List<GameObject> recipeCards = new List<GameObject>();
    List<Image> cardBgImages = new List<Image>();
    Sprite cardBgNormal;
    Sprite cardBgSelected;
    int selectedIndex = -1;
    GameJamRecipe selectedRecipe;
    List<GameJamRecipe> currentRecipes;

    // Right column - detail
    GameObject detailRoot;
    Text detailNameText;
    Text detailDescText;
    Image detailIcon;
    Text detailSourceText;
    Text detailPriceText;
    Transform matListParent;
    GameObject matTemplate;
    List<GameObject> matEntries = new List<GameObject>();

    // Bottom bar
    Button craftBtn;
    Text craftBtnText;
    Button collectBtn;
    Image progressFill;
    Text progressText;
    GameObject fuelSection;
    Image fuelIconImg;
    Text fuelAmountText;
    Text fuelTimeText;
    Button fuelBtn;

    bool isOpen;

    static readonly Color PanelBG = new Color(0.13f, 0.17f, 0.24f, 0.94f);
    static readonly Color TitleBG = new Color(0.78f, 0.40f, 0.10f);
    static readonly Color CloseBtnBG = new Color(0.82f, 0.22f, 0.18f);
    static readonly Color CardBG = new Color(0.82f, 0.85f, 0.80f, 0.92f);
    static readonly Color CardSelectedBG = new Color(0.93f, 0.95f, 0.90f, 0.98f);
    static readonly Color DetailInfoBG = new Color(0.92f, 0.93f, 0.90f, 0.95f);
    static readonly Color MatRowBG = new Color(0.85f, 0.87f, 0.83f, 0.95f);
    static readonly Color BottomBarBG = new Color(0.11f, 0.14f, 0.20f, 0.95f);
    static readonly Color ProgressBG = new Color(0.08f, 0.10f, 0.15f);
    static readonly Color ProgressFillColor = new Color(0.22f, 0.32f, 0.48f);
    static readonly Color GreenAccent = new Color(0.30f, 0.72f, 0.38f);
    static readonly Color CraftBtnColor = new Color(0.30f, 0.68f, 0.38f);
    static readonly Color FuelBtnColor = new Color(0.25f, 0.62f, 0.55f);
    static readonly Color TextDark = new Color(0.18f, 0.18f, 0.22f);
    static readonly Color TextLight = new Color(0.95f, 0.95f, 0.97f);
    static readonly Color TextMid = new Color(0.45f, 0.47f, 0.52f);
    static readonly Color TextDim = new Color(0.55f, 0.57f, 0.62f);
    static readonly Color GreenText = new Color(0.25f, 0.75f, 0.35f);
    static readonly Color RedText = new Color(0.88f, 0.28f, 0.22f);
    static readonly Color OrangeText = new Color(0.92f, 0.62f, 0.18f);
    static readonly Color BadgeBG = new Color(0.20f, 0.22f, 0.28f, 0.85f);

    public bool IsOpen => isOpen;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
    }

    void EnsureUI()
    {
        if (canvasGo == null)
        {
            cardBgNormal = Resources.Load<Sprite>("bg_gray");
            cardBgSelected = Resources.Load<Sprite>("bg_gray_select");
            BuildUI();
        }
    }

    void BuildUI()
    {
        canvasGo = GameJamUIPrefabHelper.TryLoadPrefab("MachinePanel");
        if (canvasGo != null)
        {
            FindReferences();
            return;
        }

        canvasGo = new GameObject("MachinePanel");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimmer = MakeRect("Dimmer", canvasGo.transform);
        Stretch(dimmer);
        dimmer.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        float pw = 840f, ph = 480f;
        panelGo = MakeRect("Panel", canvasGo.transform);
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(pw, ph);
        panelGo.AddComponent<Image>().color = PanelBG;

        BuildTitleBar();
        BuildLeftColumn();
        BuildRightColumn();
        BuildBottomBar();

        canvasGo.SetActive(false);
        GameJamUIPrefabHelper.SavePrefab(canvasGo, "MachinePanel");
    }

    void FindReferences()
    {
        panelGo = canvasGo.transform.Find("Panel").gameObject;

        var titleBg = panelGo.transform.Find("TitleBG");
        titleText = titleBg.Find("Title").GetComponent<Text>();

        var closeBtn = panelGo.transform.Find("Close").GetComponent<Button>();
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(Close);

        var leftCol = panelGo.transform.Find("LeftCol");
        var content = leftCol.Find("Scroll/VP/Content");
        recipeCardParent = content;
        recipeCardRect = content.GetComponent<RectTransform>();
        cardTemplate = content.Find("Card_0")?.gameObject;
        if (cardTemplate != null)
            cardTemplate.SetActive(false);
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i).gameObject;
            if (child != cardTemplate) Destroy(child);
        }

        detailRoot = panelGo.transform.Find("Detail").gameObject;
        var infoBox = detailRoot.transform.Find("InfoBox");
        detailNameText = infoBox.Find("Name").GetComponent<Text>();
        detailDescText = infoBox.Find("Desc").GetComponent<Text>();
        detailIcon = infoBox.Find("Icon").GetComponent<Image>();
        detailSourceText = infoBox.Find("Src").GetComponent<Text>();
        detailPriceText = infoBox.Find("Price").GetComponent<Text>();
        matListParent = infoBox.Find("MatArea");
        matTemplate = matListParent.Find("Mat_0")?.gameObject;
        if (matTemplate != null)
            matTemplate.SetActive(false);
        for (int i = matListParent.childCount - 1; i >= 0; i--)
        {
            var child = matListParent.GetChild(i).gameObject;
            if (child != matTemplate) Destroy(child);
        }
        detailRoot.SetActive(false);
        
        var infoBox2 = detailRoot.transform.Find("InfoBox2");
        
        var progBg = infoBox2.Find("ProgBG");
        progressFill = progBg.Find("Fill").GetComponent<Image>();
        progressText = progBg.Find("PText").GetComponent<Text>();

        fuelSection = infoBox2.Find("Fuel").gameObject;
        fuelIconImg = fuelSection.transform.Find("FIcon").GetComponent<Image>();
        fuelAmountText = fuelSection.transform.Find("FAmt").GetComponent<Text>();
        fuelTimeText = fuelSection.transform.Find("FTime").GetComponent<Text>();
        fuelBtn = fuelSection.transform.Find("FuelBtn").GetComponent<Button>();
        fuelBtn.onClick.RemoveAllListeners();
        fuelBtn.onClick.AddListener(OnAddFuel);
        fuelSection.SetActive(false);

        var bottom = panelGo.transform.Find("Bottom");

        craftBtn = bottom.Find("CraftBtn").GetComponent<Button>();
        craftBtn.onClick.RemoveAllListeners();
        craftBtn.onClick.AddListener(OnCraftClicked);
        craftBtnText = craftBtn.transform.Find("Lbl").GetComponent<Text>();

        collectBtn = bottom.Find("CollectBtn").GetComponent<Button>();
        collectBtn.onClick.RemoveAllListeners();
        collectBtn.onClick.AddListener(OnCollect);
        collectBtn.gameObject.SetActive(false);

        canvasGo.SetActive(false);
    }

    void BuildTitleBar()
    {
        var titleBg = MakeRect("TitleBG", panelGo.transform);
        var t = titleBg.GetComponent<RectTransform>();
        t.anchorMin = t.anchorMax = new Vector2(0, 1);
        t.pivot = new Vector2(0, 1);
        t.anchoredPosition = new Vector2(14, -10);
        t.sizeDelta = new Vector2(140, 34);
        titleBg.AddComponent<Image>().color = TitleBG;

        titleText = MakeText("Title", titleBg.transform, 17, TextAnchor.MiddleCenter, TextLight,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(titleText.gameObject);

        var closeGo = MakeRect("Close", panelGo.transform);
        var c = closeGo.GetComponent<RectTransform>();
        c.anchorMin = c.anchorMax = new Vector2(1, 1);
        c.pivot = new Vector2(1, 1);
        c.anchoredPosition = new Vector2(-10, -10);
        c.sizeDelta = new Vector2(34, 34);
        closeGo.AddComponent<Image>().color = CloseBtnBG;
        var cb = closeGo.AddComponent<Button>();
        cb.onClick.AddListener(Close);
        var cx = MakeText("X", closeGo.transform, 20, TextAnchor.MiddleCenter, TextLight,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(cx.gameObject);
        cx.text = "X";
    }

    void BuildLeftColumn()
    {
        float leftW = 268f;

        var col = MakeRect("LeftCol", panelGo.transform);
        var r = col.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 0);
        r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.offsetMin = new Vector2(14, 80);
        r.offsetMax = new Vector2(14 + leftW, -54);

        var hdr = MakeText("Header", col.transform, 14, TextAnchor.MiddleLeft, GreenAccent,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(4, 0), new Vector2(0, 24));
        hdr.text = "●  材料";

        var scrollGo = MakeRect("Scroll", col.transform);
        var sr = scrollGo.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = Vector2.one;
        sr.offsetMin = Vector2.zero;
        sr.offsetMax = new Vector2(0, -28);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var vp = MakeRect("VP", scrollGo.transform);
        Stretch(vp);
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeRect("Content", vp.transform);
        recipeCardRect = content.GetComponent<RectTransform>();
        recipeCardRect.anchorMin = new Vector2(0, 1);
        recipeCardRect.anchorMax = new Vector2(1, 1);
        recipeCardRect.pivot = new Vector2(0.5f, 1);
        recipeCardRect.sizeDelta = Vector2.zero;
        scroll.content = recipeCardRect;
        scroll.viewport = vp.GetComponent<RectTransform>();
        recipeCardParent = content.transform;

        cardTemplate = BuildCardTemplate(content.transform);
        cardTemplate.SetActive(false);
    }

    GameObject BuildCardTemplate(Transform parent)
    {
        float cardH = 62f;

        var card = MakeRect("Card_0", parent);
        var r = card.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1);
        r.anchorMax = new Vector2(1, 1);
        r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(0, cardH);

        card.AddComponent<Image>().color = CardBG;
        card.AddComponent<Button>().transition = Selectable.Transition.None;

        var iconGo = MakeRect("Icon", card.transform);
        var ic = iconGo.GetComponent<RectTransform>();
        ic.anchorMin = ic.anchorMax = new Vector2(0, 0.5f);
        ic.pivot = new Vector2(0, 0.5f);
        ic.anchoredPosition = new Vector2(8, 0);
        ic.sizeDelta = new Vector2(44, 44);
        iconGo.AddComponent<Image>();

        var badge = MakeRect("Badge", card.transform);
        var br2 = badge.GetComponent<RectTransform>();
        br2.anchorMin = br2.anchorMax = new Vector2(0, 0);
        br2.pivot = new Vector2(0, 0);
        br2.anchoredPosition = new Vector2(8, 4);
        br2.sizeDelta = new Vector2(24, 16);
        badge.AddComponent<Image>().color = BadgeBG;
        var bt = MakeText("Cnt", badge.transform, 10, TextAnchor.MiddleCenter, TextLight,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(bt.gameObject);

        MakeText("Name", card.transform, 15, TextAnchor.MiddleLeft, TextDark,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
            new Vector2(60, 0), new Vector2(-50, 0));

        MakeText("Amt", card.transform, 14, TextAnchor.MiddleRight, TextMid,
            new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
            new Vector2(-8, 0), new Vector2(40, 0));

        return card;
    }

    void BuildRightColumn()
    {
        float leftEnd = 292f;

        detailRoot = MakeRect("Detail", panelGo.transform);
        var r = detailRoot.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(leftEnd, 80);
        r.offsetMax = new Vector2(-14, -54);

        detailNameText = MakeText("Name", detailRoot.transform, 22, TextAnchor.UpperLeft, TextLight,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(14, -6), new Vector2(-14, 30));

        var infoBox = MakeRect("InfoBox", detailRoot.transform);
        var ib = infoBox.GetComponent<RectTransform>();
        ib.anchorMin = Vector2.zero;
        ib.anchorMax = Vector2.one;
        ib.offsetMin = new Vector2(8, 6);
        ib.offsetMax = new Vector2(-8, -40);
        infoBox.AddComponent<Image>().color = DetailInfoBG;

        detailDescText = MakeText("Desc", infoBox.transform, 13, TextAnchor.UpperLeft, TextMid,
            new Vector2(0.32f, 0.65f), new Vector2(1, 1), new Vector2(0, 1),
            new Vector2(8, -10), new Vector2(-10, 0));

        var iconGo = MakeRect("Icon", infoBox.transform);
        var ic = iconGo.GetComponent<RectTransform>();
        ic.anchorMin = new Vector2(0, 0.50f);
        ic.anchorMax = new Vector2(0.30f, 0.95f);
        ic.offsetMin = new Vector2(16, 0);
        ic.offsetMax = new Vector2(-8, -10);
        detailIcon = iconGo.AddComponent<Image>();
        detailIcon.preserveAspect = true;

        detailSourceText = MakeText("Src", infoBox.transform, 13, TextAnchor.MiddleLeft, TextMid,
            new Vector2(0.32f, 0.45f), new Vector2(1, 0.55f), new Vector2(0, 0.5f),
            new Vector2(8, 0), new Vector2(-10, 0));

        detailPriceText = MakeText("Price", infoBox.transform, 13, TextAnchor.MiddleLeft, TextMid,
            new Vector2(0.32f, 0.33f), new Vector2(1, 0.43f), new Vector2(0, 0.5f),
            new Vector2(8, 0), new Vector2(-10, 0));

        var matArea = MakeRect("MatArea", infoBox.transform);
        var ma = matArea.GetComponent<RectTransform>();
        ma.anchorMin = Vector2.zero;
        ma.anchorMax = new Vector2(1, 0.30f);
        ma.offsetMin = new Vector2(6, 6);
        ma.offsetMax = new Vector2(-6, -2);
        matListParent = matArea.transform;

        matTemplate = BuildMatTemplate(matArea.transform);
        matTemplate.SetActive(false);

        detailRoot.SetActive(false);
    }

    GameObject BuildMatTemplate(Transform parent)
    {
        var row = MakeRect("Mat_0", parent);
        var rr = row.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0, 1);
        rr.anchorMax = new Vector2(1, 1);
        rr.pivot = new Vector2(0, 1);
        rr.anchoredPosition = Vector2.zero;
        rr.sizeDelta = new Vector2(0, 32);
        row.AddComponent<Image>().color = MatRowBG;

        var miGo = MakeRect("MI", row.transform);
        var mi = miGo.GetComponent<RectTransform>();
        mi.anchorMin = mi.anchorMax = new Vector2(0, 0.5f);
        mi.pivot = new Vector2(0, 0.5f);
        mi.anchoredPosition = new Vector2(6, 0);
        mi.sizeDelta = new Vector2(24, 24);
        miGo.AddComponent<Image>();

        MakeText("MN", row.transform, 13, TextAnchor.MiddleLeft, TextDark,
            new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0.5f),
            new Vector2(34, 0), Vector2.zero);

        MakeText("MC", row.transform, 13, TextAnchor.MiddleRight, TextDark,
            new Vector2(0.6f, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
            new Vector2(-8, 0), Vector2.zero);

        return row;
    }

    void BuildBottomBar()
    {
        var bar = MakeRect("Bottom", panelGo.transform);
        var b = bar.GetComponent<RectTransform>();
        b.anchorMin = Vector2.zero;
        b.anchorMax = new Vector2(1, 0);
        b.pivot = new Vector2(0.5f, 0);
        b.sizeDelta = new Vector2(0, 76);
        bar.AddComponent<Image>().color = BottomBarBG;

        var craftGo = MakeRect("CraftBtn", bar.transform);
        var cr = craftGo.GetComponent<RectTransform>();
        cr.anchorMin = cr.anchorMax = new Vector2(0, 0.5f);
        cr.pivot = new Vector2(0, 0.5f);
        cr.anchoredPosition = new Vector2(20, 0);
        cr.sizeDelta = new Vector2(90, 40);
        craftGo.AddComponent<Image>().color = CraftBtnColor;
        craftBtn = craftGo.AddComponent<Button>();
        craftBtn.onClick.AddListener(OnCraftClicked);
        SetBtnColors(craftBtn, CraftBtnColor);
        craftBtnText = AddLabel(craftGo, 16, "制作");

        var colGo = MakeRect("CollectBtn", bar.transform);
        var col = colGo.GetComponent<RectTransform>();
        col.anchorMin = col.anchorMax = new Vector2(0, 0.5f);
        col.pivot = new Vector2(0, 0.5f);
        col.anchoredPosition = new Vector2(20, 0);
        col.sizeDelta = new Vector2(120, 40);
        colGo.AddComponent<Image>().color = GreenAccent;
        collectBtn = colGo.AddComponent<Button>();
        collectBtn.onClick.AddListener(OnCollect);
        SetBtnColors(collectBtn, GreenAccent);
        AddLabel(colGo, 16, "收取产品");
        colGo.SetActive(false);

        var progBg = MakeRect("ProgBG", bar.transform);
        var pb = progBg.GetComponent<RectTransform>();
        pb.anchorMin = new Vector2(0.16f, 0.5f);
        pb.anchorMax = new Vector2(0.62f, 0.5f);
        pb.sizeDelta = new Vector2(0, 30);
        progBg.AddComponent<Image>().color = ProgressBG;

        var fill = MakeRect("Fill", progBg.transform);
        var fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = new Vector2(0, 1);
        fr.sizeDelta = Vector2.zero;
        progressFill = fill.AddComponent<Image>();
        progressFill.color = ProgressFillColor;

        progressText = MakeText("PText", progBg.transform, 14, TextAnchor.MiddleCenter, TextLight,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(progressText.gameObject);

        fuelSection = MakeRect("Fuel", bar.transform);
        var fs = fuelSection.GetComponent<RectTransform>();
        fs.anchorMin = new Vector2(1, 0);
        fs.anchorMax = Vector2.one;
        fs.pivot = new Vector2(1, 0.5f);
        fs.offsetMin = new Vector2(-290, 4);
        fs.offsetMax = new Vector2(-12, -4);

        var fiGo = MakeRect("FIcon", fuelSection.transform);
        var fi = fiGo.GetComponent<RectTransform>();
        fi.anchorMin = fi.anchorMax = new Vector2(0, 0.5f);
        fi.pivot = new Vector2(0, 0.5f);
        fi.anchoredPosition = new Vector2(4, 4);
        fi.sizeDelta = new Vector2(26, 26);
        fuelIconImg = fiGo.AddComponent<Image>();

        fuelAmountText = MakeText("FAmt", fuelSection.transform, 13, TextAnchor.MiddleLeft, OrangeText,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(36, 7), new Vector2(140, 18));

        fuelTimeText = MakeText("FTime", fuelSection.transform, 11, TextAnchor.MiddleLeft, TextDim,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(36, -10), new Vector2(140, 16));

        var fbGo = MakeRect("FuelBtn", fuelSection.transform);
        var fb = fbGo.GetComponent<RectTransform>();
        fb.anchorMin = fb.anchorMax = new Vector2(1, 0.5f);
        fb.pivot = new Vector2(1, 0.5f);
        fb.anchoredPosition = Vector2.zero;
        fb.sizeDelta = new Vector2(82, 34);
        fbGo.AddComponent<Image>().color = FuelBtnColor;
        fuelBtn = fbGo.AddComponent<Button>();
        fuelBtn.onClick.AddListener(OnAddFuel);
        SetBtnColors(fuelBtn, FuelBtnColor);
        AddLabel(fbGo, 13, "补充燃料");

        fuelSection.SetActive(false);
    }

    public void Open(GameJamMachine machine)
    {
        if (isOpen) Close();
        EnsureUI();
        currentMachine = machine;
        isOpen = true;

        var pc = GetComponent<GameJamPlayerController>();
        if (pc != null) pc.enabled = false;

        var def = machine.GetDef();
        titleText.text = def != null ? def.displayName : machine.machineId;
        fuelSection.SetActive(def != null && def.hasFuelSystem);

        if (def != null && def.hasFuelSystem)
            GameJamArtLoader.ApplyItemIcon(fuelIconImg, def.fuelItemId ?? "木材", OrangeText);

        canvasGo.SetActive(true);
        RefreshAll();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        currentMachine = null;
        selectedRecipe = null;
        selectedIndex = -1;
        canvasGo.SetActive(false);

        var inv = GetComponent<GameJamInventory>();
        bool inventoryOpen = inv != null && inv.IsPanelOpen;
        var placer = GetComponent<GameJamBuildingPlacer>();
        bool placing = placer != null && placer.IsPlacing;

        if (!inventoryOpen && !placing)
        {
            var pc = GetComponent<GameJamPlayerController>();
            if (pc != null) pc.enabled = true;
        }
    }

    void Update()
    {
        if (!isOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        RefreshStatus();
    }

    void RefreshAll()
    {
        ClearRecipeCards();
        ClearMaterialEntries();
        selectedIndex = -1;
        selectedRecipe = null;

        if (currentMachine == null) return;

        var def = currentMachine.GetDef();
        string mid = currentMachine.machineId != null ? currentMachine.machineId.Trim() : "";
        currentRecipes = GameJamMachineDB.GetRecipesForMachine(mid);
        if ((currentRecipes == null || currentRecipes.Count == 0) && def != null && def.recipes != null)
            currentRecipes = def.recipes;

        if (currentRecipes == null || currentRecipes.Count == 0)
        {
            detailRoot.SetActive(false);
            progressText.text = "暂无可用配方";
            return;
        }

        float cardH = 62f, gap = 6f;
        for (int i = 0; i < currentRecipes.Count; i++)
            CreateRecipeCard(currentRecipes[i], i);

        recipeCardRect.sizeDelta = new Vector2(0, currentRecipes.Count * (cardH + gap) + 4f);

        SelectRecipe(0);
        RefreshStatus();
    }

    void CreateRecipeCard(GameJamRecipe recipe, int index)
    {
        if (cardTemplate == null) return;
        float cardH = 62f, gap = 6f;

        var card = Instantiate(cardTemplate, recipeCardParent);
        card.name = "Card_" + index;
        card.SetActive(true);

        var r = card.GetComponent<RectTransform>();
        r.anchoredPosition = new Vector2(0, -(index * (cardH + gap)));

        var bgImg = card.transform.Find("Bg").GetComponent<Image>();
        bgImg.sprite = cardBgNormal;
        bgImg.color = Color.white;
        cardBgImages.Add(bgImg);

        int idx = index;
        var btn = card.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SelectRecipe(idx));

        var outDef = GameJamItemDB.Get(recipe.outputItemId);
        string outName = outDef != null ? outDef.name : recipe.outputItemId;

        var icon = card.transform.Find("Icon").GetComponent<Image>();
        GameJamArtLoader.ApplyItemIcon(icon, recipe.outputItemId,
            outDef != null ? outDef.iconColor : Color.gray);

        // int owned = inventory != null && inventory.Model != null ? inventory.Model.GetTotalCount(recipe.outputItemId) : 0;
        // var badge = card.transform.Find("Badge").gameObject;
        // if (owned > 0)
        // {
        //     badge.SetActive(true);
        //     badge.transform.Find("Cnt").GetComponent<Text>().text = owned.ToString();
        // }
        // else
        // {
        //     badge.SetActive(false);
        //     badge.transform.Find("Cnt").GetComponent<Text>().text = "";
        // }

        card.transform.Find("Name").GetComponent<Text>().text = outName;
        card.transform.Find("Amt").GetComponent<Text>().text = $"x{recipe.outputAmount}";

        recipeCards.Add(card);
    }

    void SelectRecipe(int index)
    {
        if (currentRecipes == null || index < 0 || index >= currentRecipes.Count)
            return;

        selectedIndex = index;
        selectedRecipe = currentRecipes[index];

        for (int i = 0; i < cardBgImages.Count; i++)
        {
            if (cardBgImages[i] != null)
                cardBgImages[i].sprite = i == index ? cardBgSelected : cardBgNormal;
        }

        RefreshDetailPanel();
    }

    void RefreshDetailPanel()
    {
        if (selectedRecipe == null)
        {
            detailRoot.SetActive(false);
            return;
        }

        detailRoot.SetActive(true);

        var outDef = GameJamItemDB.Get(selectedRecipe.outputItemId);
        string outName = outDef != null ? outDef.name : selectedRecipe.outputItemId;

        detailNameText.text = outName;
        detailDescText.text = outDef != null ? outDef.description : "";
        GameJamArtLoader.ApplyItemIcon(detailIcon, selectedRecipe.outputItemId,
            outDef != null ? outDef.iconColor : Color.gray);

        var machineDef = currentMachine != null ? currentMachine.GetDef() : null;
        detailSourceText.text = "来源：" + (machineDef != null ? machineDef.displayName : "");
        detailPriceText.text = "出售价：" + (outDef != null ? outDef.sellPrice.ToString() : "0");
        
        RefreshMaterials();
    }

    void RefreshMaterials()
    {
        ClearMaterialEntries();
        if (selectedRecipe == null || matTemplate == null) return;

        int i = 0;
        foreach (var kv in selectedRecipe.materials)
        {
            var matDef = GameJamItemDB.Get(kv.Key);
            string matName = matDef != null ? matDef.name : kv.Key;
            int owned = inventory != null ? inventory.Model.GetTotalCount(kv.Key) : 0;
            int needed = kv.Value;
            bool enough = owned >= needed;

            var row = Instantiate(matTemplate, matListParent);
            row.name = "Mat_" + i;
            row.SetActive(true);
            var rr = row.GetComponent<RectTransform>();
            rr.anchoredPosition = new Vector2(0, -i * 34f);

            var matIcon = row.transform.Find("MI").GetComponent<Image>();
            GameJamArtLoader.ApplyItemIcon(matIcon, kv.Key,
                matDef != null ? matDef.iconColor : Color.gray);

            row.transform.Find("MN").GetComponent<Text>().text = matName;

            var mcText = row.transform.Find("MC").GetComponent<Text>();
            mcText.color = enough ? GreenText : RedText;
            mcText.text = $"{owned}/{needed}";

            matEntries.Add(row);
            i++;
        }
    }

    void RefreshStatus()
    {
        if (currentMachine == null) return;

        var def = currentMachine.GetDef();
        bool isCrafting = currentMachine.State == GameJamMachineState.Crafting;
        bool isComplete = currentMachine.State == GameJamMachineState.Complete;

        if (isCrafting)
        {
            float progress = 1f - currentMachine.CraftTimer / Mathf.Max(currentMachine.CraftTotal, 0.01f);
            string timeLeft = GameJamMachine.FormatTime(currentMachine.CraftTimer);

            var fillRect = progressFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(progress, 1);

            if (currentMachine.FuelPaused)
                progressText.text = $"燃料不足 {Mathf.RoundToInt(progress * 100)}%";
            else if (currentMachine.CraftCount > 1)
                progressText.text = $"{currentMachine.CraftIndex}/{currentMachine.CraftCount}  {timeLeft}";
            else
                progressText.text = timeLeft;

            if (currentMachine.QueueCount > 0)
                progressText.text += $"  (队列:{currentMachine.QueueCount})";
        }
        else if (isComplete)
        {
            var fillRect = progressFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(1, 1);

            var outDef = GameJamItemDB.Get(currentMachine.ProductItemId);
            string pName = outDef != null ? outDef.name : currentMachine.ProductItemId;
            progressText.text = $"完成! {pName} x{currentMachine.ProductAmount}";
        }
        else
        {
            var fillRect = progressFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(0, 1);
            progressText.text = "待机中";
        }

        craftBtn.gameObject.SetActive(!isComplete);
        collectBtn.gameObject.SetActive(isComplete);

        if (!isComplete && selectedRecipe != null)
        {
            bool canCraft = currentMachine.GetMaxCraftCount(selectedRecipe, inventory) >= 1;
            craftBtn.interactable = canCraft;
            craftBtnText.text = currentMachine.State == GameJamMachineState.Idle ? "制作" : "排队";
        }

        if (def != null && def.hasFuelSystem)
        {
            int units = currentMachine.GetFuelUnits();
            string fuelName = GetFuelItemName(def);
            fuelAmountText.text = $"{fuelName}: {units}/{def.maxFuelUnits}";
            fuelAmountText.color = units > 0 ? OrangeText : RedText;

            float totalSec = currentMachine.FuelTime;
            fuelTimeText.text = totalSec > 0 ? $"({GameJamMachine.FormatTime(totalSec)})" : "";
        }
    }

    void ClearRecipeCards()
    {
        foreach (var c in recipeCards)
            if (c != null) Destroy(c);
        recipeCards.Clear();
        cardBgImages.Clear();
    }

    void ClearMaterialEntries()
    {
        foreach (var e in matEntries)
            if (e != null) Destroy(e);
        matEntries.Clear();
    }

    void OnCraftClicked()
    {
        if (currentMachine == null || inventory == null || selectedRecipe == null) return;
        if (currentMachine.GetMaxCraftCount(selectedRecipe, inventory) < 1) return;

        if (currentMachine.State == GameJamMachineState.Idle)
            currentMachine.StartCraft(selectedRecipe, inventory, 1);
        else
            currentMachine.EnqueueCraft(selectedRecipe, inventory, 1);

        RefreshAll();
    }

    void OnCollect()
    {
        if (currentMachine == null || inventory == null) return;
        var (itemId, amount) = currentMachine.CollectProducts();
        if (itemId != null)
            inventory.Add(itemId, amount);
        RefreshAll();
    }

    void OnAddFuel()
    {
        if (currentMachine == null || inventory == null) return;
        if (currentMachine.AddFuel(inventory))
            RefreshStatus();
        else
            Toast.ShowToast($"没有{GetFuelItemName(currentMachine.GetDef())}!");
    }

    string GetFuelItemName(GameJamMachineDef def)
    {
        if (def == null || string.IsNullOrWhiteSpace(def.fuelItemId))
            return "木材";
        var itemDef = GameJamItemDB.Get(def.fuelItemId);
        return itemDef != null && !string.IsNullOrWhiteSpace(itemDef.name)
            ? itemDef.name : def.fuelItemId;
    }

    public void Cleanup()
    {
        if (canvasGo != null) Destroy(canvasGo);
        canvasGo = null;
    }

    void OnDestroy() => Cleanup();

    // --- Helpers ---

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static Text MakeText(string name, Transform parent, int size, TextAnchor align, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 sizeDelta)
    {
        var go = MakeRect(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var t = go.AddComponent<Text>();
        t.font = GetFont();
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static Text AddLabel(GameObject parent, int size, string text)
    {
        var t = MakeText("Lbl", parent.transform, size, TextAnchor.MiddleCenter, TextLight,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(t.gameObject);
        t.text = text;
        return t;
    }

    static void SetBtnColors(Button btn, Color normal)
    {
        var c = btn.colors;
        c.normalColor = normal;
        c.highlightedColor = normal * 1.15f;
        c.pressedColor = normal * 0.85f;
        c.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = c;
    }
}
