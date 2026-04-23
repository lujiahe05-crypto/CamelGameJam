using UnityEngine;
using System.Collections.Generic;

public class GameJamInventory : MonoBehaviour
{
    public GameJamInventoryModel Model { get; private set; }

    GameJamHotbarHUD hotbarHUD;
    GameJamInventoryPanel inventoryPanel;
    GameJamPlayerController playerController;
    GameJamBuildingPlacer buildingPlacer;
    bool panelOpen;

    void Awake()
    {
        Model = new GameJamInventoryModel();
    }

    void Start()
    {
        playerController = GetComponent<GameJamPlayerController>();

        hotbarHUD = gameObject.AddComponent<GameJamHotbarHUD>();
        hotbarHUD.Init(Model);

        inventoryPanel = gameObject.AddComponent<GameJamInventoryPanel>();
        inventoryPanel.Init(Model);
        inventoryPanel.Hide();

        buildingPlacer = GetComponent<GameJamBuildingPlacer>();

        Model.OnSelectedHotbarChanged += OnHotbarSelectionChanged;
        if (buildingPlacer != null)
            buildingPlacer.OnPlacementEnd += OnPlacementEnd;
    }

    public void Add(string name, int amount)
    {
        Model.AddItem(name, amount);
        Toast.ShowToast($"+{amount} {name}");
    }

    public void AddRange(IEnumerable<GameJamHarvestReward> rewards)
    {
        if (rewards == null)
            return;

        var mergedRewards = new Dictionary<string, int>();
        foreach (var reward in rewards)
        {
            if (string.IsNullOrWhiteSpace(reward.itemId) || reward.amount <= 0)
                continue;

            if (mergedRewards.ContainsKey(reward.itemId))
                mergedRewards[reward.itemId] += reward.amount;
            else
                mergedRewards[reward.itemId] = reward.amount;
        }

        foreach (var entry in mergedRewards)
        {
            if (Model.AddItem(entry.Key, entry.Value))
                Toast.ShowToast($"+{entry.Value} {entry.Key}");
        }
    }

    public bool Remove(string name, int amount)
    {
        return Model.RemoveItem(name, amount);
    }

    public bool SellItem(string itemId, int amount)
    {
        var def = GameJamItemDB.Get(itemId);
        if (def == null || def.sellPrice <= 0) return false;
        if (Model.GetTotalCount(itemId) < amount) return false;

        Model.RemoveItem(itemId, amount);
        int totalGold = def.sellPrice * amount;
        Model.AddGold(totalGold);
        Toast.ShowToast($"售出 {def.name} x{amount}，获得 {totalGold} 金币");
        return true;
    }

    public Dictionary<string, int> GetAll()
    {
        var result = new Dictionary<string, int>();
        foreach (var s in Model.mainSlots)
        {
            if (s.IsEmpty) continue;
            if (result.ContainsKey(s.itemId)) result[s.itemId] += s.count;
            else result[s.itemId] = s.count;
        }
        foreach (var s in Model.hotbarSlots)
        {
            if (s.IsEmpty) continue;
            if (result.ContainsKey(s.itemId)) result[s.itemId] += s.count;
            else result[s.itemId] = s.count;
        }
        return result;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (buildingPlacer != null && buildingPlacer.IsPlacing)
                buildingPlacer.ExitPlaceMode(false);
            TogglePanel();
            return;
        }

        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
            return;
        }

        if (!panelOpen && !(buildingPlacer != null && buildingPlacer.IsPlacing))
        {
            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    Model.SelectHotbar(i);
                    break;
                }
            }
        }
    }

    void OnHotbarSelectionChanged(int index)
    {
        if (panelOpen) return;
        if (buildingPlacer == null) return;

        var slot = Model.hotbarSlots[index];
        if (!slot.IsEmpty && GameJamBuildingDB.IsBuilding(slot.itemId))
        {
            buildingPlacer.EnterPlaceMode(slot.itemId);
        }
        else
        {
            if (buildingPlacer.IsPlacing)
                buildingPlacer.ExitPlaceMode(false);
        }
    }

    void OnPlacementEnd()
    {
        UpdateCursorState();
    }

    void TogglePanel()
    {
        if (panelOpen) ClosePanel();
        else OpenPanel();
    }

    void OpenPanel()
    {
        panelOpen = true;
        inventoryPanel.Show();
        if (playerController != null) playerController.enabled = false;
        UpdateCursorState();
    }

    void ClosePanel()
    {
        panelOpen = false;
        inventoryPanel.Hide();
        if (playerController != null) playerController.enabled = true;
        UpdateCursorState();
    }

    void UpdateCursorState()
    {
    }

    public bool IsPanelOpen => panelOpen;

    void OnDestroy()
    {
        if (hotbarHUD != null) hotbarHUD.Cleanup();
        if (inventoryPanel != null) inventoryPanel.Cleanup();
        if (buildingPlacer != null) buildingPlacer.Cleanup();
    }
}
