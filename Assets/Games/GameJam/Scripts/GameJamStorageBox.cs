using System;

public class GameJamStorageBox : UnityEngine.MonoBehaviour
{
    public const int SlotCount = 20;

    public GameJamInventorySlot[] slots;
    public event Action<int> OnSlotChanged;

    void Awake()
    {
        slots = new GameJamInventorySlot[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            slots[i] = new GameJamInventorySlot();
    }

    public bool AddItem(string itemId, int amount)
    {
        var def = GameJamItemDB.Get(itemId);
        if (def == null) return false;

        int remaining = amount;

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].itemId != itemId) continue;
            int space = def.maxStack - slots[i].count;
            if (space <= 0) continue;
            int add = Math.Min(space, remaining);
            slots[i].count += add;
            remaining -= add;
            OnSlotChanged?.Invoke(i);
        }

        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int add = Math.Min(def.maxStack, remaining);
            slots[i].Set(itemId, add);
            remaining -= add;
            OnSlotChanged?.Invoke(i);
        }

        return remaining <= 0;
    }

    public (string itemId, int count) TakeItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return (null, 0);
        var slot = slots[slotIndex];
        if (slot.IsEmpty) return (null, 0);

        var result = (slot.itemId, slot.count);
        slot.Clear();
        OnSlotChanged?.Invoke(slotIndex);
        return result;
    }

    public bool HasItems()
    {
        foreach (var s in slots)
            if (!s.IsEmpty) return true;
        return false;
    }

    public int UsedCount()
    {
        int count = 0;
        foreach (var s in slots)
            if (!s.IsEmpty) count++;
        return count;
    }
}
