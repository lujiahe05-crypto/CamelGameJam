using System;
using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Wood,
    Plastic,
    Coconut,
    Beet,
    WaterBottle
}

public enum ItemType
{
    None,
    Hook,
    BuildHammer,
    Wood,
    Plastic,
    Coconut,
    Beet,
    WaterBottle
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

/// <summary>
/// Configurable consumable item definition.
/// </summary>
public class ConsumableConfig
{
    public float hungerRestore;
    public float thirstRestore;

    public ConsumableConfig(float hunger, float thirst)
    {
        hungerRestore = hunger;
        thirstRestore = thirst;
    }
}

public class Inventory : MonoBehaviour
{
    public const int SlotCount = 10;
    public InventorySlot[] slots = new InventorySlot[SlotCount];

    public int SelectedIndex { get; private set; } = 0;
    public event Action OnChanged;
    public event Action<int> OnSelectedChanged;

    // ========== Configurable consumable values ==========
    static Dictionary<ItemType, ConsumableConfig> consumables = new Dictionary<ItemType, ConsumableConfig>
    {
        { ItemType.Coconut,     new ConsumableConfig(15f, 20f) },
        { ItemType.Beet,        new ConsumableConfig(35f, 0f)  },
        { ItemType.WaterBottle, new ConsumableConfig(0f, 40f)  },
    };

    /// <summary>
    /// Change consumable restore values at runtime.
    /// </summary>
    public static void SetConsumableConfig(ItemType type, float hungerRestore, float thirstRestore)
    {
        consumables[type] = new ConsumableConfig(hungerRestore, thirstRestore);
    }

    public static bool IsConsumable(ItemType type)
    {
        return consumables.ContainsKey(type);
    }

    public static ConsumableConfig GetConsumableConfig(ItemType type)
    {
        return consumables.TryGetValue(type, out var cfg) ? cfg : null;
    }
    // ====================================================

    void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new InventorySlot();
    }

    public bool Add(ItemType type, int amount = 1)
    {
        if (type == ItemType.None) return false;

        bool isTool = (type == ItemType.Hook || type == ItemType.BuildHammer);

        if (isTool)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].type == type)
                    return false;
            }
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
            return false;
        }

        // Resource / consumable: stack in existing slot first
        for (int i = 0; i < SlotCount; i++)
        {
            if (slots[i].type == type)
            {
                slots[i].count += amount;
                OnChanged?.Invoke();
                return true;
            }
        }
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
        return false;
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
                    bool isTool = (type == ItemType.Hook || type == ItemType.BuildHammer);
                    if (!isTool)
                    {
                        slots[i].type = ItemType.None;
                        slots[i].count = 0;
                    }
                    else
                    {
                        slots[i].count = 1;
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

    // Backward-compatible ResourceType API
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
            case ResourceType.Beet: return ItemType.Beet;
            case ResourceType.WaterBottle: return ItemType.WaterBottle;
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

    /// <summary>
    /// Try to use (consume) the selected item. Returns true if consumed.
    /// </summary>
    public bool UseSelectedItem()
    {
        var slot = GetSelectedSlot();
        if (slot.IsEmpty) return false;

        var cfg = GetConsumableConfig(slot.type);
        if (cfg == null) return false;

        var surv = RaftGame.Instance.Survival;
        if (surv == null) return false;

        // Only consume if it would actually help
        bool wouldHelp = (cfg.hungerRestore > 0 && surv.Hunger < 100f)
                      || (cfg.thirstRestore > 0 && surv.Thirst < 100f);
        if (!wouldHelp) return false;

        surv.RestoreHunger(cfg.hungerRestore);
        surv.RestoreThirst(cfg.thirstRestore);
        Remove(slot.type, 1);
        return true;
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

        // Right-click to use/eat selected consumable
        if (Input.GetMouseButtonDown(1))
        {
            UseSelectedItem();
        }
    }

    public static string GetItemName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Hook: return "\u9497\u5b50";        // 钩子
            case ItemType.BuildHammer: return "\u5efa\u9020\u9524";  // 建造锤
            case ItemType.Wood: return "\u6728\u6750";        // 木材
            case ItemType.Plastic: return "\u5851\u6599";      // 塑料
            case ItemType.Coconut: return "\u6930\u5b50";      // 椰子
            case ItemType.Beet: return "\u751c\u83dc";         // 甜菜
            case ItemType.WaterBottle: return "\u77ff\u6cc9\u6c34";  // 矿泉水
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
            case ItemType.Beet: return new Color(0.7f, 0.15f, 0.3f);
            case ItemType.WaterBottle: return new Color(0.3f, 0.7f, 0.95f);
            default: return Color.clear;
        }
    }
}
