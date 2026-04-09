using UnityEngine;

public class PlacedStorageBox : MonoBehaviour, IInteractable
{
    public const int StorageSlotCount = 12;
    public InventorySlot[] slots = new InventorySlot[StorageSlotCount];

    void Awake()
    {
        for (int i = 0; i < StorageSlotCount; i++)
            slots[i] = new InventorySlot();
    }

    public string GetInteractionHint(ItemType heldItem)
    {
        return "\u6309E\u6253\u5f00\u50a8\u7269\u7bb1";
    }

    public void Interact(ItemType heldItem)
    {
        if (RaftGame.Instance.UI != null)
            RaftGame.Instance.UI.OpenStorageUI(this);
    }

    public bool Add(ItemType type, int amount = 1)
    {
        if (type == ItemType.None) return false;

        // Stack into existing slot first
        for (int i = 0; i < StorageSlotCount; i++)
        {
            if (slots[i].type == type)
            {
                slots[i].count += amount;
                return true;
            }
        }
        // Find empty slot
        for (int i = 0; i < StorageSlotCount; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].type = type;
                slots[i].count = amount;
                return true;
            }
        }
        return false;
    }

    public bool Remove(ItemType type, int amount = 1)
    {
        for (int i = 0; i < StorageSlotCount; i++)
        {
            if (slots[i].type == type)
            {
                slots[i].count -= amount;
                if (slots[i].count <= 0)
                {
                    slots[i].type = ItemType.None;
                    slots[i].count = 0;
                }
                return true;
            }
        }
        return false;
    }
}
