using UnityEngine;

public class GameJamInteraction : MonoBehaviour
{
    public float interactRadius = 2.5f;

    GameJamInventory inventory;
    GameJamInteractionUI ui;
    GameJamResourceNode currentTarget;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        ui = GetComponent<GameJamInteractionUI>();
    }

    void Update()
    {
        FindNearest();

        if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
        {
            var (name, amount) = currentTarget.Harvest();
            inventory.Add(name, amount);
            currentTarget = null;
            ui.Hide();
        }
    }

    void FindNearest()
    {
        var cols = Physics.OverlapSphere(transform.position, interactRadius);
        GameJamResourceNode nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var col in cols)
        {
            var node = col.GetComponent<GameJamResourceNode>();
            if (node == null) node = col.GetComponentInParent<GameJamResourceNode>();
            if (node == null) continue;

            float dist = Vector3.Distance(transform.position, node.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = node;
            }
        }

        if (nearest != currentTarget)
        {
            currentTarget = nearest;
            if (currentTarget != null)
                ui.Show(currentTarget.resourceName);
            else
                ui.Hide();
        }
    }
}
