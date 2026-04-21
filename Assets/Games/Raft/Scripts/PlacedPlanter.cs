using UnityEngine;

public class PlacedPlanter : MonoBehaviour, IInteractable
{
    public enum PlanterState { Empty, Growing, Harvestable }

    public float growTime = 60f;
    public int harvestAmount = 3;

    PlanterState state = PlanterState.Empty;
    float growTimer;
    MeshRenderer mr;

    void Start()
    {
        mr = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (state == PlanterState.Growing)
        {
            growTimer -= Time.deltaTime;
            if (growTimer <= 0f)
            {
                state = PlanterState.Harvestable;
                UpdateVisual();
            }
        }
    }

    public string GetInteractionHint(ItemType heldItem)
    {
        switch (state)
        {
            case PlanterState.Empty:
                var inv = RaftGame.Instance.Inv;
                int beetCount = inv != null ? inv.GetCount(ItemType.Beet) : 0;
                if (beetCount > 0)
                    return "\u6309E\u79cd\u690d\u751c\u83dc (\u9700\u89811\u751c\u83dc)";
                else
                    return "\u9700\u8981\u751c\u83dc\u624d\u80fd\u79cd\u690d";
            case PlanterState.Growing:
                return $"\u751f\u957f\u4e2d... {Mathf.CeilToInt(growTimer)}\u79d2";
            case PlanterState.Harvestable:
                return $"\u6309E\u6536\u83b7\u751c\u83dc (x{harvestAmount})";
        }
        return null;
    }

    public void Interact(ItemType heldItem)
    {
        var inv = RaftGame.Instance.Inv;
        if (inv == null) return;

        switch (state)
        {
            case PlanterState.Empty:
                if (inv.GetCount(ItemType.Beet) > 0)
                {
                    inv.Remove(ItemType.Beet, 1);
                    state = PlanterState.Growing;
                    growTimer = growTime;
                    UpdateVisual();
                    RaftGame.Instance.UI.ShowToast("\u5f00\u59cb\u79cd\u690d\u751c\u83dc");
                }
                break;
            case PlanterState.Harvestable:
                inv.Add(ItemType.Beet, harvestAmount);
                state = PlanterState.Empty;
                UpdateVisual();
                RaftGame.Instance.UI.ShowToast($"\u6536\u83b7\u4e86 {harvestAmount} \u4e2a\u751c\u83dc");
                break;
        }
    }

    void UpdateVisual()
    {
        if (mr == null) return;
        switch (state)
        {
            case PlanterState.Empty:
                mr.material.color = new Color(0.45f, 0.3f, 0.15f);
                break;
            case PlanterState.Growing:
                mr.material.color = new Color(0.35f, 0.45f, 0.2f);
                break;
            case PlanterState.Harvestable:
                mr.material.color = new Color(0.2f, 0.7f, 0.15f);
                break;
        }
    }
}
