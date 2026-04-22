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

    public bool Remove(string name, int amount)
    {
        return Model.RemoveItem(name, amount);
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
        bool needCursor = panelOpen || (buildingPlacer != null && buildingPlacer.IsPlacing);
        Cursor.lockState = needCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = needCursor;
    }

    public bool IsPanelOpen => panelOpen;

    void OnDestroy()
    {
        if (hotbarHUD != null) hotbarHUD.Cleanup();
        if (inventoryPanel != null) inventoryPanel.Cleanup();
        if (buildingPlacer != null) buildingPlacer.Cleanup();
    }
}
