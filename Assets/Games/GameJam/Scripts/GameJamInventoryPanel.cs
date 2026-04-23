using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameJamInventoryPanel : MonoBehaviour
{
    public GameJamInventoryModel Model { get; private set; }
    public event Action CloseRequested;

    GameObject canvasGo;

    Image[] mainSlotBGs;
    Image[] mainSlotIcons;
    Text[] mainSlotCounts;
    Image[] mainSlotBorders;
    Image[] mainSlotLocks;
    Image[] mainSlotSelecteds;

    Image[] hotbarSlotBGs;
    Image[] hotbarSlotIcons;
    Text[] hotbarSlotCounts;
    Image[] hotbarSlotBorders;
    Image[] hotbarSlotSelecteds;

    GameObject detailGo;
    Image detailIcon;
    Text detailName;
    Text detailType;
    Text detailDesc;
    Text detailPrice;
    Image detailRarityBar;

    Text goldText;
    Button unlockBtn;
    Text unlockBtnText;
    Text characterNameText;
    Image characterPortrait;
    Image[] equipItemIcons;
    Text[] statBarLabels;
    Text[] statBarValues;
    Image[] statBarFills;
    Text[] plainStatLabels;
    Text[] plainStatValues;

    GameObject splitDialogGo;
    InputField splitInput;
    int splitFromIndex;
    bool splitFromHotbar;

    int selectedIndex = -1;
    bool selectedIsHotbar;

    GameObject tooltipGo;
    Text tooltipNameText;
    Text tooltipTypeText;
    Text tooltipDescText;
    Text tooltipPriceText;
    Text tooltipCompareText;

    static readonly Color SlotEmpty = new Color(0.15f, 0.15f, 0.18f, 0.9f);
    static readonly Color SlotFilled = new Color(0.2f, 0.2f, 0.24f, 0.95f);
    static readonly Color SlotSelected = new Color(0.98f, 0.86f, 0.38f, 0.42f);
    static readonly Color BorderDefault = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color LockedColor = new Color(0.2f, 0.2f, 0.22f, 0.72f);
    static readonly Color TextDim = new Color(0.78f, 0.84f, 0.9f, 0.92f);
    static readonly Color TextBright = new Color(0.98f, 0.98f, 1f, 1f);
    const float SlotSize = 64f;

    public void Init(GameJamInventoryModel model)
    {
        Model = model;
        BindEvents();
    }

    void EnsureUI()
    {
        if (canvasGo == null)
            BuildUI();
    }

    void BuildUI()
    {
        canvasGo = GameJamUIPrefabHelper.TryLoadPrefab("InventoryPanel");
        if (canvasGo != null)
        {
            EnsureEventSystem();
            FindLegacyReferences();
            canvasGo.SetActive(false);
            return;
        }

        Debug.LogError("Inventory UI prefab not found. Expected InventoryWindow or InventoryPanel in Resources.");
    }

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }

    void FindLegacyReferences()
    {
        detailGo = FindRequired("Panel/Detail").gameObject;
        detailIcon = FindRequired("Panel/Detail/Icon").GetComponent<Image>();
        detailName = FindRequired("Panel/Detail/Name").GetComponent<Text>();
        detailRarityBar = FindRequired("Panel/Detail/RarityBar").GetComponent<Image>();
        detailType = FindRequired("Panel/Detail/RarityBar/TypeText").GetComponent<Text>();
        detailDesc = FindRequired("Panel/Detail/Desc").GetComponent<Text>();
        detailPrice = FindRequired("Panel/Detail/Price").GetComponent<Text>();
        detailGo.SetActive(false);

        int mainCount = GameJamInventoryModel.MainSlotCount;
        mainSlotBGs = new Image[mainCount];
        mainSlotIcons = new Image[mainCount];
        mainSlotCounts = new Text[mainCount];
        mainSlotBorders = new Image[mainCount];
        mainSlotLocks = null;
        mainSlotSelecteds = null;

        var mainGrid = FindRequired("Panel/MainGrid");
        var mTemplate = mainGrid.Find("MSlot_0");
        if (mTemplate == null)
            throw new MissingReferenceException("Missing slot template in InventoryPanel: MSlot_0");

        var mTemplateRect = mTemplate.GetComponent<RectTransform>();
        float mSlotW = mTemplateRect.sizeDelta.x;
        float mSlotH = mTemplateRect.sizeDelta.y;
        float mGap = 6f;
        int columns = 8;

        for (int i = 0; i < mainCount; i++)
        {
            Transform slot;
            if (i == 0)
            {
                slot = mTemplate;
            }
            else
            {
                string slotName = "MSlot_" + i;
                slot = mainGrid.Find(slotName);
                if (slot == null)
                {
                    slot = Instantiate(mTemplate.gameObject, mainGrid).transform;
                    slot.name = slotName;
                    var rect = slot.GetComponent<RectTransform>();
                    int row = i / columns;
                    int col = i % columns;
                    rect.anchoredPosition = new Vector2(col * (mSlotW + mGap), -row * (mSlotH + mGap));
                }
            }

            mainSlotBorders[i] = slot.GetComponent<Image>();
            mainSlotBGs[i] = slot.Find("Inner").GetComponent<Image>();
            mainSlotIcons[i] = slot.Find("Inner/Icon").GetComponent<Image>();
            mainSlotCounts[i] = slot.Find("Inner/Count").GetComponent<Text>();
            ConfigureSlotHandler(slot, slot, false, i);
        }

        int hotbarCount = GameJamInventoryModel.HotbarSlotCount;
        hotbarSlotBGs = new Image[hotbarCount];
        hotbarSlotIcons = new Image[hotbarCount];
        hotbarSlotCounts = new Text[hotbarCount];
        hotbarSlotBorders = new Image[hotbarCount];
        hotbarSlotSelecteds = null;

        var hotbarRow = FindRequired("Panel/HotbarRow");
        var hTemplate = hotbarRow.Find("HSlot_0");
        if (hTemplate == null)
            throw new MissingReferenceException("Missing hotbar slot template in InventoryPanel: HSlot_0");

        var hTemplateRect = hTemplate.GetComponent<RectTransform>();
        float hSlotW = hTemplateRect.sizeDelta.x;
        float hGap = 6f;

        for (int i = 0; i < hotbarCount; i++)
        {
            Transform slot;
            if (i == 0)
            {
                slot = hTemplate;
            }
            else
            {
                string slotName = "HSlot_" + i;
                slot = hotbarRow.Find(slotName);
                if (slot == null)
                {
                    slot = Instantiate(hTemplate.gameObject, hotbarRow).transform;
                    slot.name = slotName;
                    var rect = slot.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(i * (hSlotW + hGap), 0f);
                }
            }

            hotbarSlotBorders[i] = slot.GetComponent<Image>();
            hotbarSlotBGs[i] = slot.Find("Inner").GetComponent<Image>();
            hotbarSlotIcons[i] = slot.Find("Inner/Icon").GetComponent<Image>();
            hotbarSlotCounts[i] = slot.Find("Inner/Count").GetComponent<Text>();
            ConfigureSlotHandler(slot, slot, true, i);
        }

        goldText = FindRequired("Panel/BottomBar/GoldBar/Gold").GetComponent<Text>();
        BindButton("Panel/BottomBar/SortBtn", OnSortClicked);
        BindButton("Panel/BottomBar/DiscardBtn", OnDiscardClicked);
        BindButton("Panel/BottomBar/SellBtn", OnSellClicked);

        var unlockTransform = FindOptional("Panel/UnlockBtn");
        if (unlockTransform != null)
        {
            unlockBtn = unlockTransform.GetComponent<Button>();
            unlockBtn.onClick.RemoveAllListeners();
            unlockBtn.onClick.AddListener(OnUnlockClicked);
            unlockBtnText = unlockTransform.Find("Text").GetComponent<Text>();
        }

        splitDialogGo = FindRequired("SplitDialog").gameObject;
        splitInput = FindRequired("SplitDialog/Input").GetComponent<InputField>();
        splitInput.textComponent = FindRequired("SplitDialog/Input/Text").GetComponent<Text>();
        BindButton("SplitDialog/SplitOK", OnSplitConfirm);
        BindButton("SplitDialog/SplitCancel", HideSplitDialog);
        splitDialogGo.SetActive(false);

        tooltipGo = FindRequired("SlotTooltip").gameObject;
        tooltipNameText = FindRequired("SlotTooltip/TName").GetComponent<Text>();
        tooltipGo.SetActive(false);
    }

    void ConfigureSlotHandler(Transform slotRoot, Transform interactiveTarget, bool isHotbar, int index)
    {
        var target = interactiveTarget != null ? interactiveTarget.gameObject : slotRoot.gameObject;
        var graphic = target.GetComponent<Graphic>();
        if (graphic == null)
        {
            var image = target.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
        }

        var handler = target.GetComponent<GameJamSlotDragHandler>();
        if (handler == null)
            handler = target.AddComponent<GameJamSlotDragHandler>();
        handler.isHotbar = isHotbar;
        handler.slotIndex = index;
        handler.panel = this;
    }

    void BindButton(string path, UnityEngine.Events.UnityAction action)
    {
        var button = FindRequired(path).GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    Transform FindRequired(string path)
    {
        var node = canvasGo.transform.Find(path);
        if (node == null)
            throw new MissingReferenceException("Inventory UI is missing required path: " + path);
        return node;
    }

    Transform FindOptional(string path)
    {
        return canvasGo == null ? null : canvasGo.transform.Find(path);
    }

    void BindEvents()
    {
        Model.OnMainSlotChanged += RefreshMainSlot;
        Model.OnHotbarSlotChanged += RefreshHotbarSlot;
        Model.OnGoldChanged += RefreshGold;
        Model.OnSlotsUnlocked += OnSlotsUnlocked;
    }

    void RefreshMainSlot(int index)
    {
        if (mainSlotBGs == null || index < 0 || index >= mainSlotBGs.Length)
            return;

        bool selected = !selectedIsHotbar && selectedIndex == index;
        bool unlocked = Model.IsSlotUnlocked(index);

        if (mainSlotLocks != null)
            mainSlotLocks[index].color = SetAlpha(mainSlotLocks[index].color, unlocked ? 0f : 0.85f);

        if (!unlocked)
        {
            mainSlotBGs[index].color = LockedColor;
            GameJamArtLoader.ClearIcon(mainSlotIcons[index]);
            mainSlotCounts[index].text = string.Empty;
            // if (mainSlotBorders[index] != null)
            //     mainSlotBorders[index].color = new Color(1f, 1f, 1f, 0.08f);
            SetSelectedOverlay(mainSlotSelecteds, index, false);
            return;
        }

        RefreshSlotVisuals(
            Model.mainSlots[index],
            mainSlotBGs[index],
            mainSlotIcons[index],
            mainSlotCounts[index],
            mainSlotBorders[index],
            mainSlotSelecteds,
            index,
            selected);
    }

    void RefreshHotbarSlot(int index)
    {
        if (hotbarSlotBGs == null || index < 0 || index >= hotbarSlotBGs.Length)
            return;

        RefreshSlotVisuals(
            Model.hotbarSlots[index],
            hotbarSlotBGs[index],
            hotbarSlotIcons[index],
            hotbarSlotCounts[index],
            hotbarSlotBorders[index],
            hotbarSlotSelecteds,
            index,
            selectedIsHotbar && selectedIndex == index);
    }

    void RefreshSlotVisuals(
        GameJamInventorySlot slot,
        Image bg,
        Image icon,
        Text count,
        Image border,
        Image[] selectedOverlays,
        int slotIndex,
        bool selected)
    {
        if (slot.IsEmpty)
        {
            bg.color = (selected ? SlotSelected : SlotEmpty);
            GameJamArtLoader.ClearIcon(icon);
            count.text = string.Empty;
            // if (border != null)
            //     border.color = selected ? new Color(1f, 0.92f, 0.45f, 0.72f) : BorderDefault;
            SetSelectedOverlay(selectedOverlays, slotIndex, selected);
            return;
        }

        var def = GameJamItemDB.Get(slot.itemId);
        bg.color = SlotFilled;
        GameJamArtLoader.ApplyItemIcon(icon, slot.itemId, Color.white);
        count.text = slot.count > 1 ? slot.count.ToString() : string.Empty;

        if (border != null)
        {
            // var rarityColor = def != null ? GameJamItemDB.GetRarityColor(def.rarity) : BorderDefault;
            // border.color = selected
            //     ? SetAlpha(rarityColor, 0.95f)
            //     : SetAlpha(rarityColor, def != null && def.rarity > GameJamRarity.Common ? 0.52f : 0.18f);
            if (selected)
                border.sprite = Resources.Load<Sprite>("bg_blue_select");
            else
                border.sprite = Resources.Load<Sprite>("bg_item");
        }

        SetSelectedOverlay(selectedOverlays, slotIndex, selected);
    }

    void SetSelectedOverlay(Image[] overlays, int index, bool selected)
    {
        if (overlays == null || index < 0 || index >= overlays.Length || overlays[index] == null)
            return;

        overlays[index].color = SetAlpha(overlays[index].color, selected ? 0.38f : 0f);
    }

    static Color SetAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    public void RefreshAllSlots()
    {
        if (canvasGo == null)
            return;

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
            goldText.text = Model.gold.ToString();

        RefreshUnlockButton();
    }

    void RefreshUnlockButton()
    {
        if (unlockBtn == null || unlockBtnText == null)
            return;

        int cost = Model.GetUnlockCost();
        if (cost < 0)
        {
            unlockBtn.interactable = false;
            unlockBtnText.text = "Full";
            return;
        }

        unlockBtn.interactable = Model.gold >= cost;
        unlockBtnText.text = "Unlock " + cost + "g";
    }

    void SetBarStat(int index, string label, int value, int max)
    {
        if (index < 0 || statBarLabels == null || index >= statBarLabels.Length)
            return;

        statBarLabels[index].text = label;
        statBarValues[index].text = value + "/" + max;

        if (statBarFills[index] != null)
        {
            float ratio = max <= 0 ? 0f : Mathf.Clamp01((float)value / max);
            var rect = statBarFills[index].rectTransform;
            rect.anchorMax = new Vector2(ratio, 1f);
            rect.offsetMin = new Vector2(3f, 3f);
            rect.offsetMax = new Vector2(-3f, -3f);
        }
    }

    void SetPlainStat(int index, string label, int value)
    {
        if (index < 0 || plainStatLabels == null || index >= plainStatLabels.Length)
            return;

        plainStatLabels[index].text = label;
        plainStatValues[index].text = value.ToString();
    }

    void RefreshEquipPreview()
    {
        if (equipItemIcons == null)
            return;

        int displayIndex = 0;
        for (int i = 0; i < equipItemIcons.Length; i++)
            GameJamArtLoader.ClearIcon(equipItemIcons[i]);

        FillEquipPreviewFromSlots(Model.hotbarSlots, ref displayIndex);
        FillEquipPreviewFromSlots(Model.mainSlots, ref displayIndex, Model.unlockedMainSlots);
    }

    void FillEquipPreviewFromSlots(GameJamInventorySlot[] slots, ref int displayIndex, int limit = -1)
    {
        if (limit < 0)
            limit = slots.Length;

        for (int i = 0; i < limit && i < slots.Length && displayIndex < equipItemIcons.Length; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty)
                continue;

            var def = GameJamItemDB.Get(slot.itemId);
            if (def == null)
                continue;

            if (def.type != GameJamItemType.Equipment && def.type != GameJamItemType.Tool)
                continue;

            GameJamArtLoader.ApplyItemIcon(equipItemIcons[displayIndex], slot.itemId, Color.white);
            displayIndex++;
        }
    }

    int CountOccupied(GameJamInventorySlot[] slots, int limit)
    {
        int count = 0;
        for (int i = 0; i < limit && i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
                count++;
        }
        return count;
    }

    int CountByType(GameJamItemType type)
    {
        int total = 0;
        total += CountByType(Model.hotbarSlots, type, Model.hotbarSlots.Length);
        total += CountByType(Model.mainSlots, type, Model.unlockedMainSlots);
        return total;
    }

    int CountByType(GameJamInventorySlot[] slots, GameJamItemType type, int limit)
    {
        int total = 0;
        for (int i = 0; i < limit && i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty)
                continue;

            var def = GameJamItemDB.Get(slot.itemId);
            if (def != null && def.type == type)
                total += slot.count;
        }
        return total;
    }

    public void SelectSlot(bool isHotbar, int index)
    {
        if (!isHotbar && !Model.IsSlotUnlocked(index))
            return;

        int previousIndex = selectedIndex;
        bool previousIsHotbar = selectedIsHotbar;

        selectedIndex = index;
        selectedIsHotbar = isHotbar;

        if (previousIndex >= 0)
        {
            if (previousIsHotbar)
                RefreshHotbarSlot(previousIndex);
            else
                RefreshMainSlot(previousIndex);
        }

        if (isHotbar)
            RefreshHotbarSlot(index);
        else
            RefreshMainSlot(index);

        RefreshDetail();
    }

    public void ClearSelection()
    {
        int previousIndex = selectedIndex;
        bool previousIsHotbar = selectedIsHotbar;
        selectedIndex = -1;

        if (previousIndex >= 0)
        {
            if (previousIsHotbar)
                RefreshHotbarSlot(previousIndex);
            else
                RefreshMainSlot(previousIndex);
        }

        if (detailGo != null)
            detailGo.SetActive(false);
    }

    void RefreshDetail()
    {
        if (detailGo == null)
            return;

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
        detailType.text = GameJamItemDB.GetTypeName(def.type) + " / " + GameJamItemDB.GetRarityName(def.rarity);
        detailDesc.text = def.description;
        detailPrice.text = "Sell: " + def.sellPrice + "g";

        if (detailRarityBar != null)
        {
            var rarityColor = GameJamItemDB.GetRarityColor(def.rarity);
            detailRarityBar.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
        }
    }

    public void ShowSplitDialog(bool isHotbar, int slotIndex)
    {
        if (splitDialogGo == null || splitInput == null)
            return;

        splitFromHotbar = isHotbar;
        splitFromIndex = slotIndex;

        var slots = isHotbar ? Model.hotbarSlots : Model.mainSlots;
        int half = Mathf.Max(1, slots[slotIndex].count / 2);
        splitInput.text = half.ToString();
        splitDialogGo.SetActive(true);
    }

    void HideSplitDialog()
    {
        if (splitDialogGo != null)
            splitDialogGo.SetActive(false);
    }

    void OnSplitClicked()
    {
        if (selectedIndex < 0)
            return;

        var slots = selectedIsHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (selectedIndex >= slots.Length)
            return;

        if (!slots[selectedIndex].IsEmpty && slots[selectedIndex].count > 1)
            ShowSplitDialog(selectedIsHotbar, selectedIndex);
    }

    void OnSplitConfirm()
    {
        if (splitInput == null)
        {
            HideSplitDialog();
            return;
        }

        int amount;
        if (!int.TryParse(splitInput.text, out amount) || amount <= 0)
        {
            HideSplitDialog();
            return;
        }

        int emptyIndex = -1;
        bool emptyIsHotbar = false;

        for (int i = 0; i < Model.mainSlots.Length; i++)
        {
            if (!Model.IsSlotUnlocked(i))
                continue;
            if (Model.mainSlots[i].IsEmpty)
            {
                emptyIndex = i;
                break;
            }
        }

        if (emptyIndex < 0)
        {
            for (int i = 0; i < Model.hotbarSlots.Length; i++)
            {
                if (Model.hotbarSlots[i].IsEmpty)
                {
                    emptyIndex = i;
                    emptyIsHotbar = true;
                    break;
                }
            }
        }

        if (emptyIndex >= 0)
        {
            Model.SplitStack(splitFromHotbar, splitFromIndex, amount, emptyIsHotbar, emptyIndex);
            RefreshAllSlots();
        }

        HideSplitDialog();
    }

    void OnSortClicked()
    {
        Model.Sort();
        ClearSelection();
    }

    void OnDiscardClicked()
    {
        if (selectedIndex < 0)
            return;

        Model.RemoveAt(selectedIsHotbar, selectedIndex);
        ClearSelection();
    }

    void OnSellClicked()
    {
        if (selectedIndex < 0)
            return;

        var slots = selectedIsHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (selectedIndex >= slots.Length || slots[selectedIndex].IsEmpty)
            return;

        var slot = slots[selectedIndex];
        var def = GameJamItemDB.Get(slot.itemId);
        if (def == null || def.sellPrice <= 0)
        {
            Toast.ShowToast("This item cannot be sold.");
            return;
        }

        int amount = slot.count;
        int totalGold = def.sellPrice * amount;
        Model.RemoveItem(slot.itemId, amount);
        Model.AddGold(totalGold);
        Toast.ShowToast("Sold " + def.name + " x" + amount + " for " + totalGold + "g");
        ClearSelection();
    }

    void OnUnlockClicked()
    {
        if (Model.UnlockRow())
        {
            Toast.ShowToast("Unlocked a new inventory row.");
            RefreshAllSlots();
        }
    }

    void OnSlotsUnlocked()
    {
        RefreshAllSlots();
    }

    void OnCloseClicked()
    {
        CloseRequested?.Invoke();
    }

    public void ShowSlotTooltip(bool isHotbar, int index, RectTransform slotRect)
    {
        if (tooltipGo == null)
            return;

        var slots = isHotbar ? Model.hotbarSlots : Model.mainSlots;
        if (index < 0 || index >= slots.Length || slots[index].IsEmpty)
            return;

        var def = GameJamItemDB.Get(slots[index].itemId);
        if (def == null)
            return;

        tooltipNameText.text = def.name;
        if (tooltipTypeText != null)
            tooltipTypeText.text = GameJamItemDB.GetTypeName(def.type) + " / " + GameJamItemDB.GetRarityName(def.rarity);
        if (tooltipDescText != null)
            tooltipDescText.text = def.description;
        if (tooltipPriceText != null)
            tooltipPriceText.text = "Price " + def.sellPrice + "g";
        if (tooltipCompareText != null)
            tooltipCompareText.text = selectedIndex >= 0 ? "Selected item ready to compare" : "Click to inspect";

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, slotRect.position),
            null,
            out worldPos);
        tooltipGo.GetComponent<RectTransform>().position = worldPos + new Vector3(SlotSize * 0.55f, SlotSize * 0.55f, 0f);
        tooltipGo.SetActive(true);
    }

    public void HideSlotTooltip()
    {
        if (tooltipGo != null)
            tooltipGo.SetActive(false);
    }

    public void Show()
    {
        EnsureUI();
        if (canvasGo == null)
            return;

        canvasGo.SetActive(true);
        RefreshAllSlots();
    }

    public void Hide()
    {
        if (canvasGo != null)
            canvasGo.SetActive(false);

        HideSplitDialog();
        HideSlotTooltip();
        ClearSelection();
    }

    public void Cleanup()
    {
        if (canvasGo != null)
            Destroy(canvasGo);
    }

    void OnDestroy()
    {
        Cleanup();
    }
}
