using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    None,
    Hook,
    BuildHammer,
    Wood,
    Plastic,
    Coconut
}

[System.Serializable]
public class InventorySlot
{
    public ItemType type;
    public int count;

    public bool IsEmpty => type == ItemType.None || count <= 0;

    public InventorySlot()
    {
        type = ItemType.None;
        count = 0;
    }

    public InventorySlot(ItemType t, int c)
    {
        type = t;
        count = c;
    }
}

public class Inventory : MonoBehaviour
{
    public const int SlotCount = 10;
    public InventorySlot[] slots = new InventorySlot[SlotCount];

    public int SelectedIndex { get; private set; } = 0;
    public event Action OnChanged;
    public event Action<int> OnSelectedChanged;

    void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new InventorySlot();
    }

    /// <summary>
    /// Add item to inventory. Tools (Hook, BuildHammer) don't stack beyond 1.
    /// Resources stack in existing slot or first empty slot.
    /// </summary>
    public bool Add(ItemType type, int amount = 1)
    {
        if (type == ItemType.None) return false;

        bool isTool = (type == ItemType.Hook || type == ItemType.BuildHammer);

        if (isTool)
        {
            // Check if already have this tool
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].type == type)
                    return false; // already have it
            }
            // Find first empty slot
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i].type = type;
                    slots[i].count = 1;
                    OnChanged?.Invoke();
                    return true;
                }
            }
            return false; // no room
        }

        // Resource: try to stack in existing slot first
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].type == type)
            {
                slots[i].count += amount;
                OnChanged?.Invoke();
                return true;
            }
        }
        // Find first empty slot
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].type = type;
                slots[i].count = amount;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false; // full
    }

    public bool Remove(ItemType type, int amount = 1)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].type == type)
            {
                slots[i].count -= amount;
                if (slots[i].count <= 0)
                {
                    // Don't remove tools when count hits 0
                    bool isTool = (type == ItemType.Hook || type == ItemType.BuildHammer);
                    if (!isTool)
                    {
                        slots[i].type = ItemType.None;
                        slots[i].count = 0;
                    }
                    else
                    {
                        slots[i].count = 1; // tools always stay at 1
                    }
                }
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public int GetCount(ItemType type)
    {
        int total = 0;
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].type == type)
                total += slots[i].count;
        }
        return total;
    }

    /// Keep old ResourceType API working for existing code (shark, survival, etc.)
    public void Add(ResourceType type, int amount = 1)
    {
        Add(ResourceToItem(type), amount);
    }

    public void Remove(ResourceType type, int amount = 1)
    {
        Remove(ResourceToItem(type), amount);
    }

    public int GetCount(ResourceType type)
    {
        return GetCount(ResourceToItem(type));
    }

    public static ItemType ResourceToItem(ResourceType r)
    {
        switch (r)
        {
            case ResourceType.Wood: return ItemType.Wood;
            case ResourceType.Plastic: return ItemType.Plastic;
            case ResourceType.Coconut: return ItemType.Coconut;
            default: return ItemType.None;
        }
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= SlotCount) return;
        SelectedIndex = index;
        OnSelectedChanged?.Invoke(index);
    }

    public InventorySlot GetSelectedSlot()
    {
        return slots[SelectedIndex];
    }

    public ItemType GetSelectedItemType()
    {
        return slots[SelectedIndex].type;
    }

    void Update()
    {
        // Number keys 1-9,0 to select slots
        for (int i = 0; i < 10; i++)
        {
            KeyCode key = (i < 9) ? (KeyCode.Alpha1 + i) : KeyCode.Alpha0;
            if (Input.GetKeyDown(key))
            {
                SelectSlot(i);
            }
        }

        // Scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.01f)
        {
            SelectSlot((SelectedIndex - 1 + SlotCount) % SlotCount);
        }
        else if (scroll < -0.01f)
        {
            SelectSlot((SelectedIndex + 1) % SlotCount);
        }
    }

    public static string GetItemName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Hook: return "Hook";
            case ItemType.BuildHammer: return "Hammer";
            case ItemType.Wood: return "Wood";
            case ItemType.Plastic: return "Plastic";
            case ItemType.Coconut: return "Coconut";
            default: return "";
        }
    }

    public static Color GetItemColor(ItemType type)
    {
        switch (type)
        {
            case ItemType.Hook: return new Color(0.5f, 0.5f, 0.5f);
            case ItemType.BuildHammer: return new Color(0.7f, 0.5f, 0.2f);
            case ItemType.Wood: return new Color(0.55f, 0.35f, 0.15f);
            case ItemType.Plastic: return new Color(0.85f, 0.85f, 0.9f);
            case ItemType.Coconut: return new Color(0.3f, 0.65f, 0.2f);
            default: return Color.clear;
        }
    }
}
