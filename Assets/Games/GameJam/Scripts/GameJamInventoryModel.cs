using System;
using System.Collections.Generic;

public class GameJamInventorySlot
{
    public string itemId;
    public int count;

    public bool IsEmpty => string.IsNullOrEmpty(itemId) || count <= 0;

    public void Clear()
    {
        itemId = null;
        count = 0;
    }

    public void Set(string id, int amount)
    {
        itemId = id;
        count = amount;
    }
}

public class GameJamInventoryModel
{
    public const int MainSlotCount = 24;
    public const int HotbarSlotCount = 8;

    public GameJamInventorySlot[] mainSlots;
    public GameJamInventorySlot[] hotbarSlots;
    public int gold;
    public int selectedHotbar;

    public event Action<int> OnMainSlotChanged;
    public event Action<int> OnHotbarSlotChanged;
    public event Action OnGoldChanged;
    public event Action<int> OnSelectedHotbarChanged;

    public GameJamInventoryModel()
    {
        mainSlots = new GameJamInventorySlot[MainSlotCount];
        hotbarSlots = new GameJamInventorySlot[HotbarSlotCount];
        for (int i = 0; i < MainSlotCount; i++)
            mainSlots[i] = new GameJamInventorySlot();
        for (int i = 0; i < HotbarSlotCount; i++)
            hotbarSlots[i] = new GameJamInventorySlot();
    }

    public bool AddItem(string itemId, int amount)
    {
        var def = GameJamItemDB.Get(itemId);
        if (def == null) return false;

        int remaining = amount;

        remaining = FillExisting(hotbarSlots, itemId, def.maxStack, remaining,
            i => OnHotbarSlotChanged?.Invoke(i));
        if (remaining <= 0) return true;

        remaining = FillExisting(mainSlots, itemId, def.maxStack, remaining,
            i => OnMainSlotChanged?.Invoke(i));
        if (remaining <= 0) return true;

        remaining = FillEmpty(hotbarSlots, itemId, def.maxStack, remaining,
            i => OnHotbarSlotChanged?.Invoke(i));
        if (remaining <= 0) return true;

        remaining = FillEmpty(mainSlots, itemId, def.maxStack, remaining,
            i => OnMainSlotChanged?.Invoke(i));

        return remaining <= 0;
    }

    int FillExisting(GameJamInventorySlot[] slots, string itemId, int maxStack,
        int remaining, Action<int> notify)
    {
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (slots[i].itemId != itemId) continue;
            int space = maxStack - slots[i].count;
            if (space <= 0) continue;
            int add = Math.Min(space, remaining);
            slots[i].count += add;
            remaining -= add;
            notify(i);
        }
        return remaining;
    }

    int FillEmpty(GameJamInventorySlot[] slots, string itemId, int maxStack,
        int remaining, Action<int> notify)
    {
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int add = Math.Min(maxStack, remaining);
            slots[i].Set(itemId, add);
            remaining -= add;
            notify(i);
        }
        return remaining;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (GetTotalCount(itemId) < amount) return false;

        int remaining = amount;

        for (int i = mainSlots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (mainSlots[i].itemId != itemId) continue;
            int take = Math.Min(mainSlots[i].count, remaining);
            mainSlots[i].count -= take;
            remaining -= take;
            if (mainSlots[i].count <= 0) mainSlots[i].Clear();
            OnMainSlotChanged?.Invoke(i);
        }

        for (int i = hotbarSlots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            if (hotbarSlots[i].itemId != itemId) continue;
            int take = Math.Min(hotbarSlots[i].count, remaining);
            hotbarSlots[i].count -= take;
            remaining -= take;
            if (hotbarSlots[i].count <= 0) hotbarSlots[i].Clear();
            OnHotbarSlotChanged?.Invoke(i);
        }

        return true;
    }

    public void RemoveAt(bool isHotbar, int index)
    {
        var slots = isHotbar ? hotbarSlots : mainSlots;
        if (index < 0 || index >= slots.Length) return;
        slots[index].Clear();
        if (isHotbar) OnHotbarSlotChanged?.Invoke(index);
        else OnMainSlotChanged?.Invoke(index);
    }

    public void MoveSlot(bool fromHotbar, int fromIdx, bool toHotbar, int toIdx)
    {
        var srcArr = fromHotbar ? hotbarSlots : mainSlots;
        var dstArr = toHotbar ? hotbarSlots : mainSlots;
        if (fromIdx < 0 || fromIdx >= srcArr.Length) return;
        if (toIdx < 0 || toIdx >= dstArr.Length) return;

        var src = srcArr[fromIdx];
        var dst = dstArr[toIdx];

        if (src.IsEmpty) return;

        if (dst.IsEmpty)
        {
            dst.Set(src.itemId, src.count);
            src.Clear();
        }
        else if (dst.itemId == src.itemId)
        {
            var def = GameJamItemDB.Get(src.itemId);
            int maxStack = def != null ? def.maxStack : 999;
            int space = maxStack - dst.count;
            if (space >= src.count)
            {
                dst.count += src.count;
                src.Clear();
            }
            else if (space > 0)
            {
                dst.count += space;
                src.count -= space;
            }
            else
            {
                Swap(src, dst);
            }
        }
        else
        {
            Swap(src, dst);
        }

        if (fromHotbar) OnHotbarSlotChanged?.Invoke(fromIdx);
        else OnMainSlotChanged?.Invoke(fromIdx);
        if (toHotbar) OnHotbarSlotChanged?.Invoke(toIdx);
        else OnMainSlotChanged?.Invoke(toIdx);
    }

    void Swap(GameJamInventorySlot a, GameJamInventorySlot b)
    {
        string tmpId = a.itemId;
        int tmpCount = a.count;
        a.Set(b.itemId, b.count);
        b.Set(tmpId, tmpCount);
    }

    public void SplitStack(bool fromHotbar, int fromIdx, int splitAmount, bool toHotbar, int toIdx)
    {
        var srcArr = fromHotbar ? hotbarSlots : mainSlots;
        var dstArr = toHotbar ? hotbarSlots : mainSlots;
        if (fromIdx < 0 || fromIdx >= srcArr.Length) return;
        if (toIdx < 0 || toIdx >= dstArr.Length) return;

        var src = srcArr[fromIdx];
        var dst = dstArr[toIdx];

        if (src.IsEmpty || src.count <= 1) return;
        if (!dst.IsEmpty) return;

        splitAmount = Math.Min(splitAmount, src.count - 1);
        if (splitAmount <= 0) return;

        dst.Set(src.itemId, splitAmount);
        src.count -= splitAmount;

        if (fromHotbar) OnHotbarSlotChanged?.Invoke(fromIdx);
        else OnMainSlotChanged?.Invoke(fromIdx);
        if (toHotbar) OnHotbarSlotChanged?.Invoke(toIdx);
        else OnMainSlotChanged?.Invoke(toIdx);
    }

    public void Sort()
    {
        var allItems = new List<(string id, int count)>();
        foreach (var s in mainSlots)
        {
            if (!s.IsEmpty)
                allItems.Add((s.itemId, s.count));
            s.Clear();
        }

        MergeStacks(allItems);

        allItems.Sort((a, b) =>
        {
            var defA = GameJamItemDB.Get(a.id);
            var defB = GameJamItemDB.Get(b.id);
            if (defA == null || defB == null) return 0;
            int cmp = defA.type.CompareTo(defB.type);
            if (cmp != 0) return cmp;
            cmp = defB.rarity.CompareTo(defA.rarity);
            if (cmp != 0) return cmp;
            return string.Compare(a.id, b.id, StringComparison.Ordinal);
        });

        int idx = 0;
        foreach (var item in allItems)
        {
            if (idx >= mainSlots.Length) break;
            mainSlots[idx].Set(item.id, item.count);
            idx++;
        }

        for (int i = 0; i < mainSlots.Length; i++)
            OnMainSlotChanged?.Invoke(i);
    }

    void MergeStacks(List<(string id, int count)> items)
    {
        var merged = new Dictionary<string, int>();
        foreach (var item in items)
        {
            if (merged.ContainsKey(item.id))
                merged[item.id] += item.count;
            else
                merged[item.id] = item.count;
        }

        items.Clear();
        foreach (var kv in merged)
        {
            var def = GameJamItemDB.Get(kv.Key);
            int maxStack = def != null ? def.maxStack : 999;
            int remaining = kv.Value;
            while (remaining > 0)
            {
                int amt = Math.Min(remaining, maxStack);
                items.Add((kv.Key, amt));
                remaining -= amt;
            }
        }
    }

    public int GetTotalCount(string itemId)
    {
        int total = 0;
        foreach (var s in mainSlots)
            if (s.itemId == itemId) total += s.count;
        foreach (var s in hotbarSlots)
            if (s.itemId == itemId) total += s.count;
        return total;
    }

    public bool CanAddItem(string itemId, int amount)
    {
        var def = GameJamItemDB.Get(itemId);
        if (def == null) return false;

        int space = 0;
        foreach (var s in hotbarSlots)
        {
            if (s.itemId == itemId) space += def.maxStack - s.count;
            else if (s.IsEmpty) space += def.maxStack;
        }
        foreach (var s in mainSlots)
        {
            if (s.itemId == itemId) space += def.maxStack - s.count;
            else if (s.IsEmpty) space += def.maxStack;
        }
        return space >= amount;
    }

    public void SelectHotbar(int index)
    {
        if (index < 0 || index >= HotbarSlotCount) return;
        if (selectedHotbar == index) return;
        selectedHotbar = index;
        OnSelectedHotbarChanged?.Invoke(index);
    }

    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke();
    }
}
