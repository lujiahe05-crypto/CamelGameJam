using UnityEngine;
using UnityEngine.UI;

public class RaftUI : MonoBehaviour
{
    // Survival status (icon + value style, bottom-right)
    Text healthValueText, hungerValueText, thirstValueText;

    // Crosshair
    Image crosshair;

    // Hint & death
    Text hintText, deathText;

    // Toast message
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

    // Status text
    Text statusText;

    // Interaction hint
    Text interactionHintText;

    public static bool IsUIOpen { get; set; }

    const float SlotSize = 64f;
    const float SlotGap = 4f;

    void Start()
    {
        var canvas = GetComponent<Canvas>().transform;

        var prefab = Resources.Load<GameObject>("UI/RaftPanel");
        var panel = Instantiate(prefab, canvas, false);
        var t = panel.transform;

        // Crosshair
        crosshair = t.Find("Crosshair").GetComponent<Image>();

        // Survival status (new icon+value style)
        var survStatus = t.Find("SurvivalStatus");
        healthValueText = survStatus.Find("HealthRow/Value").GetComponent<Text>();
        hungerValueText = survStatus.Find("HungerRow/Value").GetComponent<Text>();
        thirstValueText = survStatus.Find("ThirstRow/Value").GetComponent<Text>();

        // Texts
        statusText = t.Find("StatusText").GetComponent<Text>();
        hintText = t.Find("HintText").GetComponent<Text>();
        deathText = t.Find("DeathText").GetComponent<Text>();
        toastText = t.Find("Toast").GetComponent<Text>();
        interactionHintText = t.Find("InteractionHint").GetComponent<Text>();

        // Hotbar
        hotbarRoot = t.Find("Hotbar").GetComponent<RectTransform>();
        for (int i = 0; i < Inventory.SlotCount; i++)
        {
            var slot = hotbarRoot.Find("Slot_" + i);
            slotBGs[i] = slot.GetComponent<Image>();
            slotIcons[i] = slot.Find("Icon").GetComponent<Image>();
            slotTexts[i] = slot.Find("Text").GetComponent<Text>();
        }
        selectionFrameRect = hotbarRoot.Find("SelectionFrame").GetComponent<RectTransform>();

        // Build panel
        buildPanel = t.Find("BuildPanel").gameObject;
        buildTitleText = buildPanel.transform.Find("Title").GetComponent<Text>();
        buildCostText = buildPanel.transform.Find("BuildBtn/Cost").GetComponent<Text>();
    }

    public void ShowToast(string msg, float duration = 2f)
    {
        toastText.text = msg;
        toastTimer = duration;
    }

    public void SetInteractionHint(string hint)
    {
        if (interactionHintText != null)
            interactionHintText.text = hint ?? "";
    }

    public void OpenStorageUI(PlacedStorageBox box)
    {
        var storageUI = GetComponent<RaftStorageUI>();
        if (storageUI != null)
            storageUI.Open();
    }

    void Update()
    {
        if (RaftGame.Instance == null) return;

        UpdateSurvivalStatus();
        UpdateHotbar();
        UpdateBuildPanel();
        ApplyConfiguredBuildPanelText();
        UpdateStatus();
        UpdateDeath();
        UpdateToast();
    }

    void UpdateSurvivalStatus()
    {
        var surv = RaftGame.Instance.Survival;
        if (surv == null) return;

        healthValueText.text = Mathf.CeilToInt(surv.Health).ToString();
        hungerValueText.text = Mathf.CeilToInt(surv.Hunger).ToString();
        thirstValueText.text = Mathf.CeilToInt(surv.Thirst).ToString();
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
        if (string.IsNullOrEmpty(tool)) tool = "\u7a7a";

        string line1 = $"\u624b\u6301: {tool}";
        string line2 = $"\u6728\u7b50: {raftMgr.BlockCount} \u5757";

        var cfg = Inventory.GetConsumableConfig(selectedType);
        if (cfg != null)
        {
            string preview = "[\u53f3\u952e\u98df\u7528]";
            if (cfg.hungerRestore > 0)
                preview += $" \u9965\u997f\u503c+{cfg.hungerRestore:0}";
            if (cfg.thirstRestore > 0)
                preview += $" \u53e3\u6e34\u503c+{cfg.thirstRestore:0}";
            line1 += "\n" + preview;
        }

        statusText.text = line1 + "\n" + line2;
    }

    void UpdateDeath()
    {
        var surv = RaftGame.Instance.Survival;
        deathText.text = surv.IsDead ? "\u4f60\u6b7b\u4e86\n\u6b63\u5728\u590d\u6d3b..." : "";
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
}
