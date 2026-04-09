using UnityEngine;

public class PlacedWaterPurifier : MonoBehaviour, IInteractable
{
    public enum PurifierState { Empty, Purifying, Ready }

    public float purifyTime = 45f;

    PurifierState state = PurifierState.Empty;
    float purifyTimer;
    MeshRenderer mr;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (state == PurifierState.Purifying)
        {
            purifyTimer -= Time.deltaTime;
            if (purifyTimer <= 0f)
            {
                state = PurifierState.Ready;
                UpdateVisual();
            }
        }
    }

    public string GetInteractionHint(ItemType heldItem)
    {
        switch (state)
        {
            case PurifierState.Empty:
                if (heldItem == ItemType.SeawaterCup)
                    return "\u6309E\u5012\u5165\u6d77\u6c34";
                return "\u9700\u8981\u6d77\u6c34\u676f";
            case PurifierState.Purifying:
                return $"\u51c0\u5316\u4e2d... {Mathf.CeilToInt(purifyTimer)}\u79d2";
            case PurifierState.Ready:
                if (heldItem == ItemType.EmptyCup)
                    return "\u6309E\u6536\u96c6\u6de1\u6c34";
                return "\u6de1\u6c34\u5df2\u5c31\u7eea\uff0c\u9700\u8981\u7a7a\u676f\u5b50";
        }
        return null;
    }

    public void Interact(ItemType heldItem)
    {
        var inv = RaftGame.Instance.Inv;
        if (inv == null) return;

        switch (state)
        {
            case PurifierState.Empty:
                if (heldItem == ItemType.SeawaterCup)
                {
                    inv.Remove(ItemType.SeawaterCup, 1);
                    inv.Add(ItemType.EmptyCup, 1);
                    state = PurifierState.Purifying;
                    purifyTimer = purifyTime;
                    UpdateVisual();
                    RaftGame.Instance.UI.ShowToast("\u5df2\u5012\u5165\u6d77\u6c34\uff0c\u5f00\u59cb\u51c0\u5316");
                }
                break;
            case PurifierState.Ready:
                if (heldItem == ItemType.EmptyCup)
                {
                    inv.Remove(ItemType.EmptyCup, 1);
                    inv.Add(ItemType.FreshwaterCup, 1);
                    state = PurifierState.Empty;
                    UpdateVisual();
                    RaftGame.Instance.UI.ShowToast("\u5df2\u6536\u96c6\u6de1\u6c34");
                }
                break;
        }
    }

    void UpdateVisual()
    {
        if (mr == null) return;
        switch (state)
        {
            case PurifierState.Empty:
                mr.material.color = new Color(0.6f, 0.75f, 0.85f);
                break;
            case PurifierState.Purifying:
                mr.material.color = new Color(0.3f, 0.45f, 0.55f);
                break;
            case PurifierState.Ready:
                mr.material.color = new Color(0.3f, 0.75f, 0.95f);
                break;
        }
    }
}
