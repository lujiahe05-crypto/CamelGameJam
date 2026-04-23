using UnityEngine;

public class GameJamInteraction : MonoBehaviour
{
    public float interactRadius = 2.5f;

    GameJamInventory inventory;
    GameJamInteractionUI ui;
    GameJamMachinePanel machinePanel;
    GameJamStoragePanel storagePanel;
    GameJamPickupUI pickupUI;
    GameJamResourceNode currentTarget;
    GameJamMachine currentMachine;
    GameJamGroundPickup currentPickup;
    GameJamStorageBox currentStorage;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        ui = GetComponent<GameJamInteractionUI>();
        machinePanel = gameObject.AddComponent<GameJamMachinePanel>();
        storagePanel = gameObject.AddComponent<GameJamStoragePanel>();
        pickupUI = GetComponent<GameJamPickupUI>();
    }

    void Update()
    {
        if (machinePanel.IsOpen || storagePanel.IsOpen)
            return;

        FindNearest();

        if (Input.GetKeyDown(KeyCode.E))
            HandleInteract();

        if (Input.GetKeyDown(KeyCode.X))
            HandleDismantle();
    }

    void HandleInteract()
    {
        if (currentMachine != null)
        {
            machinePanel.Open(currentMachine);
            ui.Hide();
        }
        else if (currentStorage != null)
        {
            storagePanel.Open(currentStorage);
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
            bool destroyed = currentTarget.Hit();
            if (destroyed)
            {
                var drops = currentTarget.GetDrops();
                foreach (var (itemId, amount) in drops)
                    inventory.Add(itemId, amount);
                currentTarget.OnDepleted();
                currentTarget = null;
                ui.Hide();
            }
            else
            {
                ui.Show($"[E] 采集 {currentTarget.resourceName} ({currentTarget.Hp}/{currentTarget.MaxHp})", true);
            }
        }
    }

    void HandleDismantle()
    {
        if (currentMachine != null)
        {
            if (currentMachine.State != GameJamMachineState.Idle)
            {
                Toast.ShowToast("机器正在工作，无法拆除！");
                return;
            }
            string itemId = currentMachine.machineId;
            Destroy(currentMachine.gameObject);
            inventory.Add(itemId, 1);
            currentMachine = null;
            ui.Hide();
            Toast.ShowToast("建筑已拆除");
        }
        else if (currentStorage != null)
        {
            if (currentStorage.HasItems())
            {
                Toast.ShowToast("储物箱内有物品，请先取出！");
                return;
            }
            Destroy(currentStorage.gameObject);
            inventory.Add("储物箱", 1);
            currentStorage = null;
            ui.Hide();
            Toast.ShowToast("建筑已拆除");
        }
    }

    void FindNearest()
    {
        float searchRadius = Mathf.Max(interactRadius, 5f);
        var cols = Physics.OverlapSphere(transform.position, searchRadius);
        GameJamResourceNode nearestNode = null;
        GameJamMachine nearestMachine = null;
        GameJamGroundPickup nearestPickup = null;
        GameJamStorageBox nearestStorage = null;
        float nearestDist = float.MaxValue;

        foreach (var col in cols)
        {
            // Machine
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
                    nearestStorage = null;
                }
                continue;
            }

            // Storage box
            var storage = col.GetComponent<GameJamStorageBox>();
            if (storage == null) storage = col.GetComponentInParent<GameJamStorageBox>();
            if (storage != null)
            {
                float dist = Vector3.Distance(transform.position, storage.transform.position);
                if (dist <= interactRadius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestStorage = storage;
                    nearestNode = null;
                    nearestPickup = null;
                    nearestMachine = null;
                }
                continue;
            }

            // Ground pickup
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
                    nearestStorage = null;
                }
                continue;
            }

            // Resource node
            var node = col.GetComponent<GameJamResourceNode>();
            if (node == null) node = col.GetComponentInParent<GameJamResourceNode>();
            if (node != null && node.IsAlive)
            {
                float dist = Vector3.Distance(transform.position, node.transform.position);
                if (dist <= interactRadius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestNode = node;
                    nearestMachine = null;
                    nearestPickup = null;
                    nearestStorage = null;
                }
            }
        }

        bool changed = nearestNode != currentTarget
            || nearestMachine != currentMachine
            || nearestPickup != currentPickup
            || nearestStorage != currentStorage;
        currentTarget = nearestNode;
        currentMachine = nearestMachine;
        currentPickup = nearestPickup;
        currentStorage = nearestStorage;

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
                ui.Show($"[E] 使用 {name}  [X] 拆除", true);
            }
            else if (currentStorage != null)
            {
                if (pickupUI != null) pickupUI.Hide();
                ui.Show("[E] 使用 储物箱  [X] 拆除", true);
            }
            else if (currentTarget != null)
            {
                if (pickupUI != null) pickupUI.Hide();
                ui.Show($"[E] 采集 {currentTarget.resourceName} ({currentTarget.Hp}/{currentTarget.MaxHp})", true);
            }
            else
            {
                if (pickupUI != null) pickupUI.Hide();
                ui.Hide();
            }
        }
    }
}
