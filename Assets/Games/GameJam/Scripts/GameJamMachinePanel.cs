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
    Transform recipeListParent;
    GameObject fuelSection;
    Text fuelText;
    Button fuelBtn;
    Text statusText;
    Button collectBtn;
    bool isOpen;

    static readonly Color PanelBG = new Color(0.08f, 0.09f, 0.1f, 0.95f);
    static readonly Color RowNormal = new Color(0.14f, 0.14f, 0.17f, 0.9f);
    static readonly Color RowHover = new Color(0.2f, 0.2f, 0.26f, 0.95f);
    static readonly Color BtnNormal = new Color(0.22f, 0.22f, 0.28f);
    static readonly Color BtnHighlight = new Color(0.3f, 0.3f, 0.4f);
    static readonly Color BtnDisabled = new Color(0.15f, 0.15f, 0.18f, 0.6f);
    static readonly Color AccentColor = new Color(0.37f, 0.42f, 0.82f);
    static readonly Color TextBright = new Color(0.95f, 0.95f, 0.97f);
    static readonly Color TextDim = new Color(0.65f, 0.65f, 0.7f);
    static readonly Color GreenText = new Color(0.3f, 0.85f, 0.4f);
    static readonly Color RedText = new Color(0.9f, 0.35f, 0.3f);
    static readonly Color OrangeText = new Color(1f, 0.7f, 0.25f);

    List<GameObject> recipeRows = new List<GameObject>();

    public bool IsOpen => isOpen;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        BuildUI();
    }

    void BuildUI()
    {
        canvasGo = new GameObject("MachineCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimmer = MakeRect("Dimmer", canvasGo.transform);
        Stretch(dimmer);
        dimmer.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        float panelW = 480f;
        float panelH = 520f;
        panelGo = MakeRect("Panel", canvasGo.transform);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelGo.AddComponent<Image>().color = PanelBG;

        // Title
        titleText = MakeText("Title", panelGo.transform, 22, TextAnchor.MiddleCenter, TextBright,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            Vector2.zero, new Vector2(0, 44));

        // Close button
        var closeBtn = MakeButton("CloseBtn", panelGo.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-8, -8), new Vector2(32, 32), "X", Close);

        // Status text (shows crafting progress or idle state)
        statusText = MakeText("Status", panelGo.transform, 16, TextAnchor.MiddleCenter, TextDim,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -44), new Vector2(-24, 28));

        // Collect button (shown when crafting complete)
        collectBtn = MakeButton("CollectBtn", panelGo.transform,
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -76), new Vector2(160, 36), "收取产品", OnCollect);
        SetButtonColors(collectBtn, AccentColor);
        collectBtn.gameObject.SetActive(false);

        // Fuel section
        fuelSection = MakeRect("FuelSection", panelGo.transform);
        var fuelRect = fuelSection.GetComponent<RectTransform>();
        fuelRect.anchorMin = new Vector2(0, 1);
        fuelRect.anchorMax = new Vector2(1, 1);
        fuelRect.pivot = new Vector2(0.5f, 1);
        fuelRect.anchoredPosition = new Vector2(0, -116);
        fuelRect.sizeDelta = new Vector2(-24, 36);
        fuelSection.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.8f);

        fuelText = MakeText("FuelText", fuelSection.transform, 14, TextAnchor.MiddleLeft, OrangeText,
            new Vector2(0, 0), new Vector2(0.6f, 1), new Vector2(0, 0.5f),
            new Vector2(12, 0), Vector2.zero);

        fuelBtn = MakeButton("FuelBtn", fuelSection.transform,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-6, 0), new Vector2(120, 28), "添加燃料(木材)", OnAddFuel);

        fuelSection.SetActive(false);

        // Recipe list scroll area
        var scrollGo = MakeRect("RecipeScroll", panelGo.transform);
        var scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(12, 48);
        scrollRect.offsetMax = new Vector2(-12, -156);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = MakeRect("Viewport", scrollGo.transform);
        Stretch(viewport);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeRect("Content", viewport.transform);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(0, 0, 4, 4);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        recipeListParent = content.transform;

        // Hints
        MakeText("Hints", panelGo.transform, 12, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.55f),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 6), new Vector2(0, 20)).text = "Esc 关闭  |  选择配方开始制作";

        canvasGo.SetActive(false);
    }

    public void Open(GameJamMachine machine)
    {
        if (isOpen) Close();
        currentMachine = machine;
        isOpen = true;

        var pc = GetComponent<GameJamPlayerController>();
        if (pc != null) pc.enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var def = machine.GetDef();
        titleText.text = def != null ? def.displayName : machine.machineId;
        UpdateFuelButtonLabel(def);

        fuelSection.SetActive(def != null && def.hasFuelSystem);

        RefreshAll();
        canvasGo.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        currentMachine = null;
        canvasGo.SetActive(false);

        var inv = GetComponent<GameJamInventory>();
        bool inventoryOpen = inv != null && inv.IsPanelOpen;
        var placer = GetComponent<GameJamBuildingPlacer>();
        bool placing = placer != null && placer.IsPlacing;

        if (!inventoryOpen && !placing)
        {
            var pc = GetComponent<GameJamPlayerController>();
            if (pc != null) pc.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
        ClearRecipeRows();
        if (currentMachine == null) return;

        var def = currentMachine.GetDef();
        if (def == null || def.recipes == null) return;

        foreach (var recipe in def.recipes)
            CreateRecipeRow(recipe);

        RefreshStatus();
    }

    void RefreshStatus()
    {
        if (currentMachine == null) return;

        var def = currentMachine.GetDef();

        switch (currentMachine.State)
        {
            case GameJamMachineState.Idle:
                statusText.text = "待机中 — 选择配方开始制作";
                statusText.color = TextDim;
                collectBtn.gameObject.SetActive(false);
                break;
            case GameJamMachineState.Crafting:
                float progress = 1f - currentMachine.CraftTimer / Mathf.Max(currentMachine.CraftTotal, 0.01f);
                string timeLeft = GameJamMachine.FormatTime(currentMachine.CraftTimer);
                if (currentMachine.FuelPaused)
                {
                    statusText.text = $"制作暂停 — 燃料不足 ({Mathf.RoundToInt(progress * 100)}%)";
                    statusText.color = RedText;
                }
                else
                {
                    statusText.text = $"制作中... {Mathf.RoundToInt(progress * 100)}%  剩余 {timeLeft}";
                    statusText.color = OrangeText;
                }
                collectBtn.gameObject.SetActive(false);
                break;
            case GameJamMachineState.Complete:
                var outDef = GameJamItemDB.Get(currentMachine.ProductItemId);
                string outName = outDef != null ? outDef.name : currentMachine.ProductItemId;
                statusText.text = $"制作完成! {outName} x{currentMachine.ProductAmount}";
                statusText.color = GreenText;
                collectBtn.gameObject.SetActive(true);
                break;
        }

        if (def != null && def.hasFuelSystem)
        {
            int units = currentMachine.GetFuelUnits();
            fuelText.text = $"燃料({GetFuelItemName(def)}): {units}/{def.maxFuelUnits}";
            fuelText.color = units > 0 ? OrangeText : RedText;
        }

        foreach (var row in recipeRows)
        {
            var btn = row.GetComponentInChildren<Button>();
            if (btn != null && btn.name == "CraftBtn")
                btn.interactable = currentMachine.State == GameJamMachineState.Idle;
        }
    }

    void CreateRecipeRow(GameJamRecipe recipe)
    {
        var row = MakeRect("Recipe_" + recipe.id, recipeListParent);
        row.AddComponent<LayoutElement>().preferredHeight = 90;
        row.AddComponent<Image>().color = RowNormal;

        var outDef = GameJamItemDB.Get(recipe.outputItemId);
        string outName = outDef != null ? outDef.name : recipe.outputItemId;
        Color outColor = outDef != null ? outDef.iconColor : Color.gray;

        // Output icon
        var iconGo = MakeRect("Icon", row.transform);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(12, 8);
        iconRect.sizeDelta = new Vector2(48, 48);
        iconGo.AddComponent<Image>().color = outColor;

        // Output name + amount
        MakeText("Name", row.transform, 16, TextAnchor.MiddleLeft, TextBright,
            new Vector2(0, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 1),
            new Vector2(70, 0), new Vector2(0, -8)).text = $"{outName} x{recipe.outputAmount}";

        // Craft time
        MakeText("Time", row.transform, 12, TextAnchor.MiddleLeft, TextDim,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(70, 8), new Vector2(120, 20)).text = $"耗时: {GameJamMachine.FormatTime(recipe.craftTime)}";

        // Materials
        float matX = 12;
        float matY = -48;
        foreach (var kv in recipe.materials)
        {
            var matDef = GameJamItemDB.Get(kv.Key);
            string matName = matDef != null ? matDef.name : kv.Key;
            int owned = inventory.Model.GetTotalCount(kv.Key);
            bool enough = owned >= kv.Value;

            var matGo = MakeRect("Mat", row.transform);
            var matRect = matGo.GetComponent<RectTransform>();
            matRect.anchorMin = new Vector2(0, 1);
            matRect.anchorMax = new Vector2(0, 1);
            matRect.pivot = new Vector2(0, 1);
            matRect.anchoredPosition = new Vector2(matX, matY);
            matRect.sizeDelta = new Vector2(140, 22);

            // Mat icon
            var matIcon = MakeRect("MIcon", matGo.transform);
            var miRect = matIcon.GetComponent<RectTransform>();
            miRect.anchorMin = new Vector2(0, 0);
            miRect.anchorMax = new Vector2(0, 1);
            miRect.pivot = new Vector2(0, 0.5f);
            miRect.anchoredPosition = Vector2.zero;
            miRect.sizeDelta = new Vector2(18, 0);
            matIcon.AddComponent<Image>().color = matDef != null ? matDef.iconColor : Color.gray;

            // Mat text
            var matText = MakeText("MText", matGo.transform, 12, TextAnchor.MiddleLeft,
                enough ? GreenText : RedText,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(22, 0), Vector2.zero);
            matText.text = $"{matName} {owned}/{kv.Value}";

            matX += 150;
            if (matX > 300)
            {
                matX = 12;
                matY -= 24;
            }
        }

        // Craft button
        var craftBtn = MakeButton("CraftBtn", row.transform,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-10, 8), new Vector2(80, 32), "制作", () => OnCraft(recipe));

        bool canCraft = currentMachine.CanStartCraft(recipe, inventory);
        craftBtn.interactable = canCraft && currentMachine.State == GameJamMachineState.Idle;

        recipeRows.Add(row);
    }

    void ClearRecipeRows()
    {
        foreach (var row in recipeRows)
            if (row != null) Destroy(row);
        recipeRows.Clear();
    }

    void OnCraft(GameJamRecipe recipe)
    {
        if (currentMachine == null || inventory == null) return;
        if (!currentMachine.CanStartCraft(recipe, inventory)) return;

        currentMachine.StartCraft(recipe, inventory);
        RefreshAll();
    }

    void OnCollect()
    {
        if (currentMachine == null || inventory == null) return;
        var (itemId, amount) = currentMachine.CollectProducts();
        if (itemId != null)
        {
            inventory.Add(itemId, amount);
        }
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

    void UpdateFuelButtonLabel(GameJamMachineDef def)
    {
        if (fuelBtn == null) return;

        var label = fuelBtn.GetComponentInChildren<Text>();
        if (label != null)
            label.text = $"添加燃料({GetFuelItemName(def)})";
    }

    string GetFuelItemName(GameJamMachineDef def)
    {
        if (def == null || string.IsNullOrWhiteSpace(def.fuelItemId))
            return "木材";

        var itemDef = GameJamItemDB.Get(def.fuelItemId);
        return itemDef != null && !string.IsNullOrWhiteSpace(itemDef.name)
            ? itemDef.name
            : def.fuelItemId;
    }

    public void Cleanup()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    void OnDestroy()
    {
        Cleanup();
    }

    // --- UI Helpers ---

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
    }

    static Font GetFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static Text MakeText(string name, Transform parent, int fontSize, TextAnchor align, Color color,
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
        txt.raycastTarget = false;
        return txt;
    }

    Button MakeButton(string name, Transform parent,
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
        img.color = BtnNormal;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor = new Color(0.18f, 0.18f, 0.24f);
        colors.disabledColor = BtnDisabled;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txtGo = MakeRect("Text", go.transform);
        var tRect = txtGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
        var txt = txtGo.AddComponent<Text>();
        txt.font = GetFont();
        txt.fontSize = 14;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = TextBright;
        txt.text = label;

        return btn;
    }

    static void SetButtonColors(Button btn, Color normalColor)
    {
        var colors = btn.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = normalColor * 1.2f;
        colors.pressedColor = normalColor * 0.8f;
        btn.colors = colors;
        btn.GetComponent<Image>().color = normalColor;
    }
}
