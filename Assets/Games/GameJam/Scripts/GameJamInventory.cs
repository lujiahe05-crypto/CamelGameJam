using UnityEngine;
using System.Collections.Generic;

public class GameJamInventory : MonoBehaviour
{
    public GameJamInventoryModel Model { get; private set; }

    GameJamHotbarHUD hotbarHUD;
    GameJamInventoryPanel inventoryPanel;
    GameJamPlayerController playerController;
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
            TogglePanel();

        if (panelOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePanel();
            return;
        }

        if (!panelOpen)
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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ClosePanel()
    {
        panelOpen = false;
        inventoryPanel.Hide();
        if (playerController != null) playerController.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool IsPanelOpen => panelOpen;

    void OnDestroy()
    {
        if (hotbarHUD != null) hotbarHUD.Cleanup();
        if (inventoryPanel != null) inventoryPanel.Cleanup();
    }
}
