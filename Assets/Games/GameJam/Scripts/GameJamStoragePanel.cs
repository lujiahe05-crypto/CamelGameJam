using UnityEngine;
using UnityEngine.UI;

public class GameJamStoragePanel : MonoBehaviour
{
    GameJamStorageBox currentBox;
    GameJamInventory inventory;

    GameObject canvasGo;
    GameObject panelGo;
    Text titleText;
    bool isOpen;

    static readonly Color PanelBG = new Color(0.08f, 0.09f, 0.1f, 0.95f);
    static readonly Color SlotEmpty = new Color(0.14f, 0.14f, 0.17f, 0.9f);
    static readonly Color SlotHover = new Color(0.22f, 0.22f, 0.28f);
    static readonly Color BtnNormal = new Color(0.22f, 0.22f, 0.28f);
    static readonly Color BtnHighlight = new Color(0.3f, 0.3f, 0.4f);
    static readonly Color AccentColor = new Color(0.37f, 0.42f, 0.82f);
    static readonly Color TextBright = new Color(0.95f, 0.95f, 0.97f);
    static readonly Color TextDim = new Color(0.65f, 0.65f, 0.7f);
    static readonly Color SeparatorColor = new Color(0.25f, 0.25f, 0.3f);

    GameObject[] storageSlotGOs;
    GameObject[] playerSlotGOs;

    const int StorageCols = 5;
    const int StorageRows = 4;
    const int PlayerCols = 6;
    const int PlayerRows = 4;
    const float SlotSize = 56f;
    const float SlotSpacing = 4f;

    public bool IsOpen => isOpen;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        BuildUI();
    }

    void BuildUI()
    {
        canvasGo = new GameObject("StorageCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 115;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dimmer = MakeRect("Dimmer", canvasGo.transform);
        Stretch(dimmer);
        dimmer.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        float panelW = 760f;
        float panelH = 420f;
        panelGo = MakeRect("Panel", canvasGo.transform);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);
        panelGo.AddComponent<Image>().color = PanelBG;

        float leftW = 340f;
        float rightW = 390f;

        // === Left: Storage ===
        titleText = MakeText("Title", panelGo.transform, 18, TextAnchor.MiddleLeft, TextBright,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -12), new Vector2(300, 30));

        storageSlotGOs = new GameObject[GameJamStorageBox.SlotCount];
        float storageGridTop = -48f;
        float storageGridLeft = 20f;
        for (int i = 0; i < GameJamStorageBox.SlotCount; i++)
        {
            int col = i % StorageCols;
            int row = i / StorageCols;
            float x = storageGridLeft + col * (SlotSize + SlotSpacing);
            float y = storageGridTop - row * (SlotSize + SlotSpacing);
            storageSlotGOs[i] = CreateSlot(panelGo.transform, x, y, i, true);
        }

        // Separator line
        var sep = MakeRect("Sep", panelGo.transform);
        var sepRect = sep.GetComponent<RectTransform>();
        sepRect.anchorMin = new Vector2(0, 0);
        sepRect.anchorMax = new Vector2(0, 1);
        sepRect.pivot = new Vector2(0.5f, 0.5f);
        sepRect.anchoredPosition = new Vector2(leftW, 0);
        sepRect.sizeDelta = new Vector2(2, -24);
        sep.AddComponent<Image>().color = SeparatorColor;

        // === Right: Player Inventory ===
        MakeText("InvTitle", panelGo.transform, 18, TextAnchor.MiddleLeft, TextBright,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(leftW + 18, -12), new Vector2(200, 30)).text = "背包";

        playerSlotGOs = new GameObject[GameJamInventoryModel.MainSlotCount];
        float playerGridTop = -48f;
        float playerGridLeft = leftW + 18;
        for (int i = 0; i < GameJamInventoryModel.MainSlotCount; i++)
        {
            int col = i % PlayerCols;
            int row = i / PlayerCols;
            float x = playerGridLeft + col * (SlotSize + SlotSpacing);
            float y = playerGridTop - row * (SlotSize + SlotSpacing);
            playerSlotGOs[i] = CreateSlot(panelGo.transform, x, y, i, false);
        }

        // Bottom buttons
        var takeAllBtn = MakeButton("TakeAllBtn", panelGo.transform,
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(20, 16), new Vector2(120, 34), "全部取出", OnTakeAll);
        SetButtonColors(takeAllBtn, AccentColor);

        // Close button
        var closeBtn = MakeButton("CloseBtn", panelGo.transform,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-8, -8), new Vector2(32, 32), "X", Close);

        // Hint text
        MakeText("Hint", panelGo.transform, 12, TextAnchor.MiddleCenter, TextDim,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 16), new Vector2(500, 20)).text =
            "点击左侧物品取出到背包 | 点击右侧物品存入储物箱 | Esc 关闭";

        canvasGo.SetActive(false);
    }

    GameObject CreateSlot(Transform parent, float x, float y, int index, bool isStorage)
    {
        var slot = MakeRect("Slot_" + index, parent);
        var rect = slot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(SlotSize, SlotSize);

        var bg = slot.AddComponent<Image>();
        bg.color = SlotEmpty;

        var iconGo = MakeRect("Icon", slot.transform);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.sizeDelta = Vector2.zero;
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.clear;
        icon.raycastTarget = false;

        var countText = MakeText("Count", slot.transform, 12, TextAnchor.LowerRight, TextBright,
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
            new Vector2(-2, 2), Vector2.zero);
        countText.raycastTarget = false;

        var btn = slot.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = SlotEmpty;
        colors.highlightedColor = SlotHover;
        colors.pressedColor = new Color(0.18f, 0.18f, 0.24f);
        btn.colors = colors;

        int capturedIndex = index;
        bool capturedIsStorage = isStorage;
        btn.onClick.AddListener(() => OnSlotClick(capturedIndex, capturedIsStorage));

        return slot;
    }

    void OnSlotClick(int index, bool isStorage)
    {
        if (currentBox == null || inventory == null) return;

        if (isStorage)
        {
            var (itemId, count) = currentBox.TakeItem(index);
            if (itemId != null)
            {
                if (inventory.Model.CanAddItem(itemId, count))
                {
                    inventory.Add(itemId, count);
                }
                else
                {
                    currentBox.AddItem(itemId, count);
                    Toast.ShowToast("背包已满！");
                }
            }
        }
        else
        {
            var slot = inventory.Model.mainSlots[index];
            if (slot.IsEmpty) return;
            string itemId = slot.itemId;
            int count = slot.count;
            if (currentBox.AddItem(itemId, count))
            {
                inventory.Model.RemoveAt(false, index);
            }
            else
            {
                Toast.ShowToast("储物箱已满！");
            }
        }

        RefreshAll();
    }

    void OnTakeAll()
    {
        if (currentBox == null || inventory == null) return;

        for (int i = 0; i < currentBox.slots.Length; i++)
        {
            if (currentBox.slots[i].IsEmpty) continue;
            string itemId = currentBox.slots[i].itemId;
            int count = currentBox.slots[i].count;
            if (inventory.Model.CanAddItem(itemId, count))
            {
                var (id, amt) = currentBox.TakeItem(i);
                if (id != null) inventory.Add(id, amt);
            }
            else
            {
                Toast.ShowToast("背包已满，部分物品无法取出！");
                break;
            }
        }
        RefreshAll();
    }

    public void Open(GameJamStorageBox box)
    {
        if (isOpen) Close();
        currentBox = box;
        isOpen = true;

        var pc = GetComponent<GameJamPlayerController>();
        if (pc != null) pc.enabled = false;
        RefreshAll();
        canvasGo.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        currentBox = null;
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
    }

    void RefreshAll()
    {
        if (currentBox == null) return;

        titleText.text = $"储物箱 ({currentBox.UsedCount()}/{GameJamStorageBox.SlotCount})";

        for (int i = 0; i < storageSlotGOs.Length; i++)
            RefreshSlot(storageSlotGOs[i], i < currentBox.slots.Length ? currentBox.slots[i] : null);

        for (int i = 0; i < playerSlotGOs.Length; i++)
            RefreshSlot(playerSlotGOs[i], i < inventory.Model.mainSlots.Length ? inventory.Model.mainSlots[i] : null);
    }

    void RefreshSlot(GameObject slotGO, GameJamInventorySlot slot)
    {
        var icon = slotGO.transform.Find("Icon").GetComponent<Image>();
        var count = slotGO.transform.Find("Count").GetComponent<Text>();

        if (slot == null || slot.IsEmpty)
        {
            icon.color = Color.clear;
            count.text = "";
        }
        else
        {
            var def = GameJamItemDB.Get(slot.itemId);
            icon.color = def != null ? def.iconColor : Color.gray;
            count.text = slot.count > 1 ? slot.count.ToString() : "";
        }
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

    static Font GetFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    static Text MakeText(string name, Transform parent, int fontSize, TextAnchor align, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
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
        go.AddComponent<Image>().color = BtnNormal;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = BtnNormal;
        colors.highlightedColor = BtnHighlight;
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

    public void Cleanup()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    void OnDestroy() => Cleanup();
}
