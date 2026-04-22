using UnityEngine;

public class GameJamInteraction : MonoBehaviour
{
    public float interactRadius = 2.5f;

    GameJamInventory inventory;
    GameJamInteractionUI ui;
    GameJamMachinePanel machinePanel;
    GameJamResourceNode currentTarget;
    GameJamMachine currentMachine;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        ui = GetComponent<GameJamInteractionUI>();
        machinePanel = gameObject.AddComponent<GameJamMachinePanel>();
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
        var cols = Physics.OverlapSphere(transform.position, interactRadius);
        GameJamResourceNode nearestNode = null;
        GameJamMachine nearestMachine = null;
        float nearestDist = float.MaxValue;

        foreach (var col in cols)
        {
            var machine = col.GetComponent<GameJamMachine>();
            if (machine == null) machine = col.GetComponentInParent<GameJamMachine>();
            if (machine != null)
            {
                float dist = Vector3.Distance(transform.position, machine.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestMachine = machine;
                    nearestNode = null;
                }
                continue;
            }

            var node = col.GetComponent<GameJamResourceNode>();
            if (node == null) node = col.GetComponentInParent<GameJamResourceNode>();
            if (node != null)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestNode = node;
                    nearestMachine = null;
                }
            }
        }

        bool changed = nearestNode != currentTarget || nearestMachine != currentMachine;
        currentTarget = nearestNode;
        currentMachine = nearestMachine;

        if (changed)
        {
            if (currentMachine != null)
            {
                var def = currentMachine.GetDef();
                string name = def != null ? def.displayName : currentMachine.machineId;
                ui.Show($"[E] 使用 {name}", true);
            }
            else if (currentTarget != null)
                ui.Show(currentTarget.resourceName);
            else
                ui.Hide();
        }
    }
}
