using UnityEngine;
using UnityEngine.UI;

public class RaftUI : MonoBehaviour
{
    // Survival bars
    Text healthText, hungerText, thirstText;
    Image healthBar, hungerBar, thirstBar;

    // Crosshair
    Image crosshair;

    // Hint & death
    Text hintText, deathText;

    // Toast message (consume feedback)
    Text toastText;
    float toastTimer;

    // Hotbar (10 slots)
    RectTransform hotbarRoot;
    Image[] slotBGs = new Image[Inventory.SlotCount];
    Image[] slotIcons = new Image[Inventory.SlotCount];
    Text[] slotTexts = new Text[Inventory.SlotCount];
    RectTransform selectionFrameRect;

    // Build panel
    GameObject buildPanel;
    Text buildTitleText;
    Text buildCostText;
    bool buildPanelVisible;

    // Status text (top right)
    Text statusText;

    const float SlotSize = 64f;
    const float SlotGap = 4f;

    void Start()
    {
        var canvas = GetComponent<Canvas>().transform;

        CreateCrosshair(canvas);

        // Survival bars - bottom left (above hotbar)
        healthBar = CreateBar(canvas, new Vector2(20, 150), new Color(0.9f, 0.2f, 0.2f), out healthText, "\u8840\u91cf");      // 琛€閲?
        hungerBar = CreateBar(canvas, new Vector2(20, 120), new Color(0.8f, 0.6f, 0.1f), out hungerText, "\u9965\u997f\u503c"); // 楗ラタ鍊?
        thirstBar = CreateBar(canvas, new Vector2(20, 90), new Color(0.2f, 0.5f, 0.9f), out thirstText, "\u53e3\u6e34\u503c"); // 鍙ｆ复鍊?

        // Status text - top right
        statusText = CreateUIText(canvas, "StatusText",
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(300, 100),
            20, Color.white, TextAnchor.UpperRight);

        // Hint text - top left
        hintText = CreateUIText(canvas, "HintText",
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(600, 160),
            17, new Color(1, 1, 1, 0.6f), TextAnchor.UpperLeft);
        hintText.text = "WASD \u79fb\u52a8 | Space \u8df3\u8dc3 | 1-0 \u9009\u62e9\u7269\u54c1 | \u6eda\u8f6e\u5207\u6362\n"
                      + "\u5de6\u952e \u4f7f\u7528\u5de5\u5177 | \u53f3\u952e \u98df\u7528\u9009\u4e2d\u98df\u7269\n"
                      + "\u9497\u5b50: \u6295\u63b7\u6253\u635e | \u5efa\u9020\u9524: \u653e\u7f6e\u5730\u57fa\n"
                      + "ESC \u8fd4\u56de\u5927\u5385";
        // WASD 绉诲姩 | Space 璺宠穬 | 1-0 閫夋嫨鐗╁搧 | 婊氳疆鍒囨崲
        // 宸﹂敭 浣跨敤宸ュ叿 | 鍙抽敭 椋熺敤閫変腑鐗╁搧
        // 閽╁瓙: 鎶曟幏鎵撴崬 | 寤洪€犻敜: 鏀剧疆鍦板熀
        // ESC 杩斿洖澶у巺

        // Death text
        deathText = CreateUIText(canvas, "DeathText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(500, 100),
            48, new Color(1, 0.2f, 0.2f), TextAnchor.MiddleCenter);
        deathText.text = "";

        // Toast text (center bottom, above hotbar)
        toastText = CreateUIText(canvas, "Toast",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 100), new Vector2(400, 40),
            20, new Color(0.3f, 1f, 0.3f), TextAnchor.MiddleCenter);
        toastText.text = "";

        CreateHotbar(canvas);
        CreateBuildPanel(canvas);
    }

    void CreateCrosshair(Transform parent)
    {
        var go = new GameObject("Crosshair");
        go.transform.SetParent(parent, false);
        crosshair = go.AddComponent<Image>();
        crosshair.color = new Color(1, 1, 1, 0.7f);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(4, 4);
    }

    void CreateHotbar(Transform canvas)
    {
        var rootGo = new GameObject("Hotbar");
        rootGo.transform.SetParent(canvas, false);
        hotbarRoot = rootGo.AddComponent<RectTransform>();
        hotbarRoot.anchorMin = new Vector2(0.5f, 0);
        hotbarRoot.anchorMax = new Vector2(0.5f, 0);
        hotbarRoot.pivot = new Vector2(0.5f, 0);
        float totalWidth = Inventory.SlotCount * (SlotSize + SlotGap) - SlotGap;
        hotbarRoot.sizeDelta = new Vector2(totalWidth, SlotSize + 20);
        hotbarRoot.anchoredPosition = new Vector2(0, 10);

        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            float x = i * (SlotSize + SlotGap) - totalWidth / 2f + SlotSize / 2f;
            CreateSlot(hotbarRoot, i, x);
        }

        // Selection frame (border only)
        var frameGo = new GameObject("SelectionFrame");
        frameGo.transform.SetParent(hotbarRoot, false);
        var frameRect = frameGo.AddComponent<RectTransform>();
        frameRect.sizeDelta = new Vector2(SlotSize + 6, SlotSize + 6);
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        CreateBorderFrame(frameGo.transform, SlotSize + 6, SlotSize + 6, 3, new Color(1, 0.9f, 0.3f, 0.9f));
        selectionFrameRect = frameRect;
    }

    void CreateBorderFrame(Transform parent, float w, float h, float thickness, Color color)
    {
        CreateBorderPiece(parent, "Top", new Vector2(0, h / 2 - thickness / 2), new Vector2(w, thickness), color);
        CreateBorderPiece(parent, "Bot", new Vector2(0, -h / 2 + thickness / 2), new Vector2(w, thickness), color);
        CreateBorderPiece(parent, "Left", new Vector2(-w / 2 + thickness / 2, 0), new Vector2(thickness, h), color);
        CreateBorderPiece(parent, "Right", new Vector2(w / 2 - thickness / 2, 0), new Vector2(thickness, h), color);
    }

    void CreateBorderPiece(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Border_" + name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    void CreateSlot(RectTransform parent, int index, float x)
    {
        var slotGo = new GameObject("Slot_" + index);
        slotGo.transform.SetParent(parent, false);
        slotBGs[index] = slotGo.AddComponent<Image>();
        slotBGs[index].color = new Color(0, 0, 0, 0.5f);
        var slotRect = slotGo.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.5f, 0.5f);
        slotRect.anchorMax = new Vector2(0.5f, 0.5f);
        slotRect.pivot = new Vector2(0.5f, 0.5f);
        slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        slotRect.anchoredPosition = new Vector2(x, 0);

        // Item icon
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        slotIcons[index] = iconGo.AddComponent<Image>();
        slotIcons[index].color = Color.clear;
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.25f);
        iconRect.anchorMax = new Vector2(0.9f, 0.85f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        // Item name + count
        slotTexts[index] = CreateUIText(slotGo.transform, "Text",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            11, Color.white, TextAnchor.LowerCenter);
        var textRect = slotTexts[index].GetComponent<RectTransform>();
        textRect.offsetMin = new Vector2(2, 2);
        textRect.offsetMax = new Vector2(-2, -2);

        // Key number label
        string keyLabel = (index < 9) ? (index + 1).ToString() : "0";
        var keyText = CreateUIText(slotGo.transform, "Key",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            10, new Color(1, 1, 1, 0.4f), TextAnchor.UpperLeft);
        var keyRect = keyText.GetComponent<RectTransform>();
        keyRect.offsetMin = new Vector2(3, 3);
        keyRect.offsetMax = new Vector2(-3, -3);
        keyText.text = keyLabel;
    }

    void CreateBuildPanel(Transform canvas)
    {
        buildPanel = new GameObject("BuildPanel");
        buildPanel.transform.SetParent(canvas, false);
        var panelImg = buildPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);
        var panelRect = buildPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0, 100);
        panelRect.sizeDelta = new Vector2(320, 120);

        Text titleText = buildTitleText = CreateUIText(buildPanel.transform, "Title",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
            new Vector2(0, -5), new Vector2(300, 30),
            22, new Color(1, 0.9f, 0.3f), TextAnchor.MiddleCenter);
        titleText.text = "\u5efa\u9020: \u6728\u7b50\u5730\u57fa";  // 寤洪€? 鏈ㄧ瓘鍦板熀

        var btnGo = new GameObject("BuildBtn");
        btnGo.transform.SetParent(buildPanel.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.3f, 0.5f, 0.3f, 0.8f);
        var btnRect = btnGo.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.1f, 0.1f);
        btnRect.anchorMax = new Vector2(0.9f, 0.55f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        buildCostText = CreateUIText(btnGo.transform, "Cost",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            16, Color.white, TextAnchor.MiddleCenter);

        var instrText = CreateUIText(buildPanel.transform, "Instr",
            new Vector2(0, 0.55f), new Vector2(1, 0.75f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            13, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
        var instrRect = instrText.GetComponent<RectTransform>();
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;
        instrText.text = "\u7784\u51c6\u6728\u7b50\u65c1\u6c34\u9762\uff0c\u5de6\u952e\u653e\u7f6e";  // 鐬勫噯鏈ㄧ瓘鏃佹按闈紝宸﹂敭鏀剧疆

        buildPanel.SetActive(false);
    }

    public void ShowToast(string msg, float duration = 2f)
    {
        toastText.text = msg;
        toastTimer = duration;
    }

    void Update()
    {
        if (RaftGame.Instance == null) return;

        UpdateSurvivalBars();
        UpdateHotbar();
        UpdateBuildPanel();
        ApplyConfiguredBuildPanelText();
        UpdateStatus();
        UpdateDeath();
        UpdateToast();
    }

    void UpdateSurvivalBars()
    {
        var surv = RaftGame.Instance.Survival;
        if (surv == null) return;

        SetBarFill(healthBar, surv.Health / surv.MaxHealth);
        SetBarFill(hungerBar, surv.Hunger / surv.MaxHunger);
        SetBarFill(thirstBar, surv.Thirst / surv.MaxThirst);

        healthText.text = "\u8840\u91cf " + Mathf.CeilToInt(surv.Health);  // 琛€閲?
        hungerText.text = "\u9965\u997f " + Mathf.CeilToInt(surv.Hunger); // 楗ラタ
        thirstText.text = "\u53e3\u6e34 " + Mathf.CeilToInt(surv.Thirst); // 鍙ｆ复
    }

    void UpdateHotbar()
    {
        var inv = RaftGame.Instance.Inv;
        if (inv == null) return;

        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            var slot = inv.slots[i];
            if (slot.IsEmpty)
            {
                slotIcons[i].color = Color.clear;
                slotTexts[i].text = "";
            }
            else
            {
                slotIcons[i].color = Inventory.GetItemColor(slot.type);
                string name = Inventory.GetItemName(slot.type);
                bool isTool = (slot.type == ItemType.Hook || slot.type == ItemType.BuildHammer);
                slotTexts[i].text = isTool ? name : $"{name}\n{slot.count}";
            }

            slotBGs[i].color = (i == inv.SelectedIndex)
                ? new Color(0.15f, 0.15f, 0.15f, 0.7f)
                : new Color(0, 0, 0, 0.5f);
        }

        // Move selection frame
        if (selectionFrameRect != null)
        {
            float totalWidth = Inventory.SlotCount * (SlotSize + SlotGap) - SlotGap;
            float x = inv.SelectedIndex * (SlotSize + SlotGap) - totalWidth / 2f + SlotSize / 2f;
            selectionFrameRect.anchoredPosition = new Vector2(x, 0);
        }
    }

    void UpdateBuildPanel()
    {
        var inv = RaftGame.Instance.Inv;
        bool hammerSelected = inv.GetSelectedItemType() == ItemType.BuildHammer;

        if (hammerSelected != buildPanelVisible)
        {
            buildPanelVisible = hammerSelected;
            buildPanel.SetActive(hammerSelected);
        }

        if (hammerSelected)
        {
            bool canBuild = RaftConfigTables.CanAffordBuilding(inv, RaftManager.FoundationBuildingId);
            // 鏈ㄧ瓘鍦板熀 [娑堣€? 1鏈ㄦ潗] (鎷ユ湁: X)
            buildCostText.text = $"\u6728\u7b50\u5730\u57fa  [\u6d88\u8017: {RaftConfigTables.FormatBuildingCost(RaftManager.FoundationBuildingId)}]";
            buildCostText.color = canBuild ? Color.white : new Color(1, 0.3f, 0.3f);
        }
    }

    void ApplyConfiguredBuildPanelText()
    {
        if (!buildPanelVisible || buildCostText == null)
            return;

        var inv = RaftGame.Instance.Inv;
        if (inv == null)
            return;

        var buildingConfig = RaftConfigTables.GetBuildingConfig(RaftManager.FoundationBuildingId);
        string buildingName = buildingConfig != null ? buildingConfig.displayName : "\u6728\u7b4f\u5730\u57fa";
        bool canBuild = RaftConfigTables.CanAffordBuilding(inv, RaftManager.FoundationBuildingId);
        string costText = RaftConfigTables.FormatBuildingCost(RaftManager.FoundationBuildingId);

        if (buildTitleText != null)
            buildTitleText.text = "\u5efa\u9020: " + buildingName;

        buildCostText.text = $"{buildingName}  [\u6d88\u8017: {costText}]";
        buildCostText.color = canBuild ? Color.white : new Color(1, 0.3f, 0.3f);
    }

    void UpdateStatus()
    {
        var inv = RaftGame.Instance.Inv;
        var raftMgr = RaftGame.Instance.RaftMgr;
        var selectedType = inv.GetSelectedItemType();
        string tool = Inventory.GetItemName(selectedType);
        if (string.IsNullOrEmpty(tool)) tool = "\u7a7a";  // 绌?

        string line1 = $"\u624b\u6301: {tool}";  // 鎵嬫寔:
        string line2 = $"\u6728\u7b50: {raftMgr.BlockCount} \u5757";  // 鏈ㄧ瓘: N 鍧?

        // Show consumable restore preview
        var cfg = Inventory.GetConsumableConfig(selectedType);
        if (cfg != null)
        {
            string preview = "[\u53f3\u952e\u98df\u7528]";  // [鍙抽敭椋熺敤]
            if (cfg.hungerRestore > 0)
                preview += $" \u9965\u997f\u503c+{cfg.hungerRestore:0}";  // 楗ラタ鍊?X
            if (cfg.thirstRestore > 0)
                preview += $" \u53e3\u6e34\u503c+{cfg.thirstRestore:0}";  // 鍙ｆ复鍊?X
            line1 += "\n" + preview;
        }

        statusText.text = line1 + "\n" + line2;
    }

    void UpdateDeath()
    {
        var surv = RaftGame.Instance.Survival;
        deathText.text = surv.IsDead ? "\u4f60\u6b7b\u4e86\n\u6b63\u5728\u590d\u6d3b..." : "";
        // 浣犳浜?/ 姝ｅ湪澶嶆椿...
    }

    void UpdateToast()
    {
        if (toastTimer > 0)
        {
            toastTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(toastTimer);
            toastText.color = new Color(toastText.color.r, toastText.color.g, toastText.color.b, alpha);
            if (toastTimer <= 0)
                toastText.text = "";
        }
    }

    void SetBarFill(Image bar, float fill)
    {
        var rect = bar.GetComponent<RectTransform>();
        rect.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
    }

    Image CreateBar(Transform parent, Vector2 offset, Color barColor, out Text label, string labelStr)
    {
        var bgGo = new GameObject(labelStr + "BarBG");
        bgGo.transform.SetParent(parent, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.5f);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(0, 0);
        bgRect.pivot = new Vector2(0, 0);
        bgRect.anchoredPosition = offset;
        bgRect.sizeDelta = new Vector2(220, 22);

        var fillGo = new GameObject(labelStr + "BarFill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = barColor;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        fillRect.pivot = new Vector2(0, 0.5f);

        label = CreateUIText(bgGo.transform, labelStr + "Label",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero,
            14, Color.white, TextAnchor.MiddleCenter);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return fillImg;
    }

    Text CreateUIText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        return text;
    }
}

