using UnityEngine;
using UnityEngine.UI;

public class RaftStorageUI : MonoBehaviour
{
    const int DisplaySlots = 20;

    GameObject panel;
    Button closeBtn;

    // Selected item info
    Image selectedIcon;
    Text selectedName;
    Text selectedDesc;

    // Grid slots
    Image[] slotIcons = new Image[DisplaySlots];
    Text[] slotTexts = new Text[DisplaySlots];
    Button[] slotButtons = new Button[DisplaySlots];

    int selectedSlotIndex = -1;
    bool isOpen;

    void Start()
    {
        var prefab = Resources.Load<GameObject>("UI/StoragePanel");
        panel = Instantiate(prefab, transform, false);
        var t = panel.transform;

        closeBtn = t.Find("CloseBtn").GetComponent<Button>();
        closeBtn.onClick.AddListener(Close);

        // Selected item info area
        var infoArea = t.Find("InfoArea");
        selectedIcon = infoArea.Find("SelectedIcon").GetComponent<Image>();
        selectedName = infoArea.Find("SelectedName").GetComponent<Text>();
        selectedDesc = infoArea.Find("SelectedDesc").GetComponent<Text>();

        // Bind grid slots
        var grid = t.Find("Grid");
        for (int i = 0; i < DisplaySlots; i++)
        {
            var slot = grid.Find("Slot_" + i);
            slotIcons[i] = slot.Find("Icon").GetComponent<Image>();
            slotTexts[i] = slot.Find("Text").GetComponent<Text>();
            slotButtons[i] = slot.GetComponent<Button>();
            int idx = i;
            slotButtons[i].onClick.AddListener(() => OnClickSlot(idx));
        }

        ClearSelectedInfo();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (isOpen) Close();
            else Open();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (isOpen)
            RefreshSlots();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        panel.SetActive(true);
        RaftUI.IsUIOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        selectedSlotIndex = -1;
        ClearSelectedInfo();
        RefreshSlots();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        RaftUI.IsUIOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void RefreshSlots()
    {
        var inv = RaftGame.Instance.Inv;

        for (int i = 0; i < DisplaySlots; i++)
        {
            if (i < Inventory.SlotCount)
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
                    bool isTool = (slot.type == ItemType.Hook || slot.type == ItemType.BuildHammer);
                    slotTexts[i].text = isTool ? "" : slot.count.ToString();
                }

                // Highlight selected slot
                var slotImg = slotButtons[i].GetComponent<Image>();
                slotImg.color = (i == selectedSlotIndex)
                    ? new Color(0.6f, 0.45f, 0.28f)
                    : new Color(0.35f, 0.25f, 0.15f, 0.8f);
            }
            else
            {
                // Empty locked slots beyond inventory size
                slotIcons[i].color = Color.clear;
                slotTexts[i].text = "";
                slotButtons[i].interactable = false;
                slotButtons[i].GetComponent<Image>().color = new Color(0.25f, 0.18f, 0.1f, 0.4f);
            }
        }

        // Refresh selected item info
        if (selectedSlotIndex >= 0 && selectedSlotIndex < Inventory.SlotCount)
        {
            var slot = inv.slots[selectedSlotIndex];
            if (!slot.IsEmpty)
            {
                selectedIcon.color = Inventory.GetItemColor(slot.type);
                selectedName.text = Inventory.GetItemName(slot.type);
                selectedDesc.text = GetItemDescription(slot.type);
            }
            else
            {
                ClearSelectedInfo();
            }
        }
    }

    void OnClickSlot(int index)
    {
        if (index >= Inventory.SlotCount) return;
        var inv = RaftGame.Instance.Inv;
        var slot = inv.slots[index];

        if (slot.IsEmpty)
        {
            selectedSlotIndex = -1;
            ClearSelectedInfo();
            return;
        }

        selectedSlotIndex = index;
        selectedIcon.color = Inventory.GetItemColor(slot.type);
        selectedName.text = Inventory.GetItemName(slot.type);
        selectedDesc.text = GetItemDescription(slot.type);
    }

    void ClearSelectedInfo()
    {
        selectedIcon.color = Color.clear;
        selectedName.text = "";
        selectedDesc.text = "";
    }

    static string GetItemDescription(ItemType type)
    {
        switch (type)
        {
            case ItemType.Hook: return "\u7528\u6765\u6253\u635e\u6d77\u4e0a\u6f02\u6d6e\u7269";
            case ItemType.BuildHammer: return "\u7528\u6765\u6269\u5efa\u4f60\u7684\u6728\u7b50";
            case ItemType.Wood: return "\u57fa\u7840\u5efa\u7b51\u6750\u6599";
            case ItemType.Plastic: return "\u5408\u6210\u5404\u79cd\u5de5\u5177\u7684\u6750\u6599";
            case ItemType.Coconut: return "\u53ef\u4ee5\u98df\u7528\uff0c\u6062\u590d\u9965\u997f\u548c\u53e3\u6e34";
            case ItemType.Beet: return "\u53ef\u4ee5\u98df\u7528\uff0c\u5927\u91cf\u6062\u590d\u9965\u997f\u503c";
            case ItemType.WaterBottle: return "\u997e\u6c34\uff0c\u6062\u590d\u53e3\u6e34\u503c";
            case ItemType.Planter: return "\u79cd\u690d\u4f5c\u7269\u7684\u5bb9\u5668";
            case ItemType.WaterPurifier: return "\u5c06\u6d77\u6c34\u8f6c\u5316\u4e3a\u6de1\u6c34";
            case ItemType.StorageBox: return "\u5b58\u50a8\u7269\u54c1\u7684\u5bb9\u5668";
            case ItemType.EmptyCup: return "\u7a7a\u676f\u5b50\uff0c\u53ef\u4ee5\u88c5\u6d77\u6c34";
            case ItemType.SeawaterCup: return "\u88c5\u6ee1\u6d77\u6c34\uff0c\u9700\u8981\u51c0\u5316";
            case ItemType.FreshwaterCup: return "\u53ef\u4ee5\u76f4\u63a5\u996e\u7528\u7684\u6de1\u6c34";
            default: return "";
        }
    }
}
