using UnityEngine;

public class GameJamInteraction : MonoBehaviour
{
    public float interactRadius = 2.5f;

    GameJamInventory inventory;
    GameJamInteractionUI ui;
    GameJamMachinePanel machinePanel;
    GameJamPickupUI pickupUI;
    GameJamResourceNode currentTarget;
    GameJamMachine currentMachine;
    GameJamGroundPickup currentPickup;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        ui = GetComponent<GameJamInteractionUI>();
        machinePanel = gameObject.AddComponent<GameJamMachinePanel>();
        pickupUI = GetComponent<GameJamPickupUI>();
    }

    void Update()
    {
        if (machinePanel.IsOpen)
            return;

        FindNearest();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentMachine != null)
            {
                machinePanel.Open(currentMachine);
                ui.Hide();
            }
            else if (currentPickup != null)
            {
                if (!inventory.Model.CanAddItem(currentPickup.itemId, currentPickup.pickupAmount))
                {
                    Toast.ShowToast("背包已满，无法拾取！");
                }
                else
                {
                    var (id, name, amount) = currentPickup.DoPickup();
                    inventory.Add(id, amount);
                    currentPickup = null;
                    if (pickupUI != null) pickupUI.Hide();
                }
            }
            else if (currentTarget != null)
            {
                var (name, amount) = currentTarget.Harvest();
                inventory.Add(name, amount);
                currentTarget = null;
                currentMachine = null;
                ui.Hide();
            }
        }
    }

    void FindNearest()
    {
        float searchRadius = Mathf.Max(interactRadius, 5f);
        var cols = Physics.OverlapSphere(transform.position, searchRadius);
        GameJamResourceNode nearestNode = null;
        GameJamMachine nearestMachine = null;
        GameJamGroundPickup nearestPickup = null;
        float nearestDist = float.MaxValue;

        foreach (var col in cols)
        {
            var machine = col.GetComponent<GameJamMachine>();
            if (machine == null) machine = col.GetComponentInParent<GameJamMachine>();
            if (machine != null)
            {
                float dist = Vector3.Distance(transform.position, machine.transform.position);
                if (dist <= interactRadius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestMachine = machine;
                    nearestNode = null;
                    nearestPickup = null;
                }
                continue;
            }

            var pickup = col.GetComponent<GameJamGroundPickup>();
            if (pickup == null) pickup = col.GetComponentInParent<GameJamGroundPickup>();
            if (pickup != null && pickup.CanPickup())
            {
                float dist = Vector3.Distance(transform.position, pickup.transform.position);
                if (dist <= pickup.GetInteractRange() && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestPickup = pickup;
                    nearestNode = null;
                    nearestMachine = null;
                }
                continue;
            }

            var node = col.GetComponent<GameJamResourceNode>();
            if (node == null) node = col.GetComponentInParent<GameJamResourceNode>();
            if (node != null)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist <= interactRadius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestNode = node;
                    nearestMachine = null;
                    nearestPickup = null;
                }
            }
        }

        bool changed = nearestNode != currentTarget
            || nearestMachine != currentMachine
            || nearestPickup != currentPickup;
        currentTarget = nearestNode;
        currentMachine = nearestMachine;
        currentPickup = nearestPickup;

        if (changed)
        {
            if (currentPickup != null)
            {
                ui.Hide();
                if (pickupUI != null) pickupUI.Show();
            }
            else if (currentMachine != null)
            {
                if (pickupUI != null) pickupUI.Hide();
                var def = currentMachine.GetDef();
                string name = def != null ? def.displayName : currentMachine.machineId;
                ui.Show($"[E] 使用 {name}", true);
            }
            else if (currentTarget != null)
            {
                if (pickupUI != null) pickupUI.Hide();
                ui.Show(currentTarget.resourceName);
            }
            else
            {
                if (pickupUI != null) pickupUI.Hide();
                ui.Hide();
            }
        }
    }
}
