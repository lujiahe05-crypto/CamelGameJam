using UnityEngine;
using System.Collections;

public class GameJamInteraction : MonoBehaviour
{
    public float interactRadius = 2.5f;

    static readonly string[] SequenceTriggers = { "Cuttree", "Stone", "Saw" };

    GameJamInventory inventory;
    GameJamInteractionUI ui;
    GameJamMachinePanel machinePanel;
    GameJamStoragePanel storagePanel;
    GameJamPickupUI pickupUI;
    Animator animator;
    GameJamPlayerController playerController;
    GameJamResourceNode currentTarget;
    GameJamMachine currentMachine;
    GameJamGroundPickup currentPickup;
    GameJamStorageBox currentStorage;

    bool isGathering;
    bool wasPanelOpen;
    Coroutine gatherCoroutine;
    GameJamResourceNode gatherTarget;
    string currentGatherTrigger;
    string currentGatherToolId;
    int restoreHotbarIndex = -1;
    int movedToolMainIndex = -1;
    int movedToolHotbarIndex = -1;

    void Start()
    {
        inventory = GetComponent<GameJamInventory>();
        ui = GetComponent<GameJamInteractionUI>();
        machinePanel = gameObject.AddComponent<GameJamMachinePanel>();
        storagePanel = gameObject.AddComponent<GameJamStoragePanel>();
        pickupUI = GetComponent<GameJamPickupUI>();
        playerController = GetComponent<GameJamPlayerController>();
        if (playerController != null)
            animator = playerController.Animator;
    }

    void Update()
    {
        bool panelOpen = machinePanel.IsOpen || storagePanel.IsOpen;
        if (panelOpen)
        {
            wasPanelOpen = true;
            return;
        }

        if (wasPanelOpen)
        {
            wasPanelOpen = false;
            ClearTargets();
            FindNearest();
        }

        if (isGathering)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StopGathering();
                return;
            }

            if (gatherTarget == null || !gatherTarget.IsAlive)
            {
                StopGathering();
                return;
            }

            float dist = Vector3.Distance(transform.position, gatherTarget.transform.position);
            if (dist > interactRadius * 1.5f)
            {
                StopGathering();
                return;
            }

            return;
        }

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
            return;
        }

        if (currentStorage != null)
        {
            if (animator != null)
                animator.SetTrigger("Treasure");

            storagePanel.Open(currentStorage);
            ui.Hide();
            return;
        }

        if (currentPickup != null)
        {
            if (!inventory.Model.CanAddItem(currentPickup.itemId, currentPickup.pickupAmount))
            {
                Toast.ShowToast("Bag is full.");
                return;
            }

            if (animator != null)
                animator.SetTrigger("Collection");

            var (id, _, amount) = currentPickup.DoPickup();
            inventory.Add(id, amount);
            currentPickup = null;
            if (pickupUI != null)
                pickupUI.Hide();
            return;
        }

        if (currentTarget != null)
            StartGathering(currentTarget);
    }

    void StartGathering(GameJamResourceNode target)
    {
        isGathering = true;
        gatherTarget = target;
        PrepareGatherTool(target.gatherAnim);

        FaceTarget(target.transform.position);

        string trigger = GetAnimTrigger(target.gatherAnim);
        currentGatherTrigger = trigger;
        if (animator != null)
        {
            animator.SetTrigger(trigger);
            if (IsSequenceAnim(trigger))
                animator.SetBool("IsWorkingLoop", true);
        }

        if (playerController != null)
            playerController.enabled = false;

        gatherCoroutine = StartCoroutine(GatherRoutine(target, trigger));
    }

    void StopGathering()
    {
        isGathering = false;
        gatherTarget = null;

        if (gatherCoroutine != null)
        {
            StopCoroutine(gatherCoroutine);
            gatherCoroutine = null;
        }

        if (animator != null)
        {
            animator.SetBool("IsWorkingLoop", false);
            if (currentGatherTrigger != null)
            {
                animator.ResetTrigger(currentGatherTrigger);
                currentGatherTrigger = null;
            }
            animator.CrossFade("Idle", 0.15f, 0);
        }

        if (playerController != null)
            playerController.enabled = true;

        RestoreGatherToolSelection();
    }

    IEnumerator GatherRoutine(GameJamResourceNode target, string trigger)
    {
        bool isSeq = IsSequenceAnim(trigger);
        float baseInterval = isSeq ? 0.8f : 0.6f;
        float speedMult = GetToolSpeedMultiplier(target.gatherAnim, currentGatherToolId);
        float hitInterval = baseInterval / speedMult;

        yield return new WaitForSeconds(isSeq ? 0.5f : 0.3f);

        while (target != null && target.IsAlive)
        {
            var rewards = target.PeekHarvestRewards();
            if (!inventory.Model.CanAddItems(rewards))
            {
                Toast.ShowToast("Bag space is not enough.");
                break;
            }

            bool destroyed = target.Hit();
            if (destroyed)
            {
                inventory.AddRange(target.Harvest());
                break;
            }

            string speedLabel = speedMult > 1f ? $" (x{speedMult:F1})" : "";
            ui.Show($"Gather {target.resourceName} ({target.Hp}/{target.MaxHp}){speedLabel}", true);

            if (!isSeq)
                break;

            yield return new WaitForSeconds(hitInterval);
        }

        StopGathering();
        ui.Hide();
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    static string GetAnimTrigger(GameJamGatherAnim anim)
    {
        switch (anim)
        {
            case GameJamGatherAnim.CutTree: return "Cuttree";
            case GameJamGatherAnim.Mine: return "Stone";
            case GameJamGatherAnim.Saw: return "Saw";
            case GameJamGatherAnim.Drill: return "Drill";
            case GameJamGatherAnim.Dig: return "Dig";
            case GameJamGatherAnim.Gather: return "Sow";
            default: return "Stone";
        }
    }

    static bool IsSequenceAnim(string trigger)
    {
        for (int i = 0; i < SequenceTriggers.Length; i++)
        {
            if (SequenceTriggers[i] == trigger)
                return true;
        }

        return false;
    }

    void PrepareGatherTool(GameJamGatherAnim gatherAnim)
    {
        currentGatherToolId = null;
        restoreHotbarIndex = -1;
        movedToolMainIndex = -1;
        movedToolHotbarIndex = -1;

        if (inventory == null || inventory.Model == null)
            return;

        restoreHotbarIndex = inventory.Model.selectedHotbar;

        int hotbarToolIndex = inventory.FindHotbarToolSlotForGather(gatherAnim);
        if (hotbarToolIndex >= 0)
        {
            inventory.SelectHotbarSlot(hotbarToolIndex);
            currentGatherToolId = inventory.GetSelectedHotbarItemId();
            return;
        }

        int mainBagToolIndex = inventory.FindMainBagToolSlotForGather(gatherAnim);
        if (mainBagToolIndex < 0)
            return;

        int targetHotbarIndex = inventory.FindEmptyHotbarSlot();
        if (targetHotbarIndex < 0)
            targetHotbarIndex = Mathf.Clamp(restoreHotbarIndex, 0, GameJamInventoryModel.HotbarSlotCount - 1);

        inventory.MoveMainSlotToHotbar(mainBagToolIndex, targetHotbarIndex);
        inventory.SelectHotbarSlot(targetHotbarIndex);

        movedToolMainIndex = mainBagToolIndex;
        movedToolHotbarIndex = targetHotbarIndex;
        currentGatherToolId = inventory.GetSelectedHotbarItemId();
    }

    void RestoreGatherToolSelection()
    {
        if (inventory == null)
            return;

        if (movedToolMainIndex >= 0 && movedToolHotbarIndex >= 0)
            inventory.MoveHotbarSlotToMain(movedToolHotbarIndex, movedToolMainIndex);

        if (restoreHotbarIndex >= 0 && inventory.Model != null)
            inventory.SelectHotbarSlot(restoreHotbarIndex);

        currentGatherToolId = null;
        restoreHotbarIndex = -1;
        movedToolMainIndex = -1;
        movedToolHotbarIndex = -1;
    }

    float GetToolSpeedMultiplier(GameJamGatherAnim gatherAnim, string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
            return 1f;

        if (!GameJamItemDB.IsToolForGatherAnim(toolId, gatherAnim))
            return 1f;

        switch (gatherAnim)
        {
            case GameJamGatherAnim.Mine:
                if (toolId == "\u94DC\u9550") return 2f;
                if (toolId == "\u77F3\u9550") return 1.5f;
                break;

            case GameJamGatherAnim.CutTree:
                if (toolId == "\u94C1\u65A7") return 2f;
                if (toolId == "\u94DC\u65A7") return 1.5f;
                break;

            case GameJamGatherAnim.Saw:
            case GameJamGatherAnim.Drill:
                return 1.75f;
        }

        return 1.5f;
    }

    void HandleDismantle()
    {
        if (currentMachine != null)
        {
            if (currentMachine.State != GameJamMachineState.Idle)
            {
                Toast.ShowToast("Machine is working.");
                return;
            }

            string itemId = currentMachine.machineId;
            Destroy(currentMachine.gameObject);
            inventory.Add(itemId, 1);
            currentMachine = null;
            ui.Hide();
            Toast.ShowToast("Building removed.");
            return;
        }

        if (currentStorage != null)
        {
            if (currentStorage.HasItems())
            {
                Toast.ShowToast("Storage is not empty.");
                return;
            }

            Destroy(currentStorage.gameObject);
            inventory.Add("\u50A8\u7269\u7BB1", 1);
            currentStorage = null;
            ui.Hide();
            Toast.ShowToast("Building removed.");
        }
    }

    void ClearTargets()
    {
        currentTarget = null;
        currentMachine = null;
        currentPickup = null;
        currentStorage = null;
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

        if (!changed)
            return;

        if (currentPickup != null)
        {
            ui.Hide();
            if (pickupUI != null)
                pickupUI.Show();
            return;
        }

        if (currentMachine != null)
        {
            if (pickupUI != null)
                pickupUI.Hide();

            var def = currentMachine.GetDef();
            string name = def != null ? def.displayName : currentMachine.machineId;
            ui.Show($"[E] Use {name}  [X] Remove", true);
            return;
        }

        if (currentStorage != null)
        {
            if (pickupUI != null)
                pickupUI.Hide();

            ui.Show("[E] Use Storage Box  [X] Remove", true);
            return;
        }

        if (currentTarget != null)
        {
            if (pickupUI != null)
                pickupUI.Hide();

            ui.Show($"[E] Gather {currentTarget.resourceName} ({currentTarget.Hp}/{currentTarget.MaxHp})", true);
            return;
        }

        if (pickupUI != null)
            pickupUI.Hide();
        ui.Hide();
    }
}
