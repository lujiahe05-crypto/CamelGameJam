using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public enum GameJamMachineState { Idle, Crafting, Complete }

public class GameJamMachine : MonoBehaviour
{
    public string machineId;
    public GameJamMachineState State { get; private set; }
    public float FuelTime { get; private set; }
    public float CraftTimer { get; private set; }
    public float CraftTotal { get; private set; }
    public GameJamRecipe CurrentRecipe { get; private set; }
    public string ProductItemId { get; private set; }
    public int ProductAmount { get; private set; }
    public bool FuelPaused { get; private set; }
    public int CraftCount { get; private set; }
    public int CraftIndex { get; private set; }

    Queue<(GameJamRecipe recipe, int count)> craftQueue = new Queue<(GameJamRecipe, int)>();
    public int QueueCount => craftQueue.Count;

    GameJamMachineDef def;
    GameObject floatingGo;
    Canvas floatingCanvas;
    Text floatingText;
    Image floatingIcon;

    public void Init(string id)
    {
        machineId = id;
        def = GameJamMachineDB.Get(id);
        if (def == null)
            Debug.LogWarning($"Missing GameJamMachineDef for machineId={id}");
        State = GameJamMachineState.Idle;
        BuildFloatingUI();
        UpdateFloatingUI();
    }

    public GameJamMachineDef GetDef() => def;

    void Update()
    {
        if (State == GameJamMachineState.Crafting)
        {
            if (def.hasFuelSystem && FuelTime <= 0)
            {
                FuelPaused = true;
            }
            else
            {
                FuelPaused = false;
                CraftTimer -= Time.deltaTime;
                if (def.hasFuelSystem)
                    FuelTime = Mathf.Max(0, FuelTime - Time.deltaTime);

                if (CraftCount > 1 && CurrentRecipe != null)
                {
                    float elapsed = CraftTotal - CraftTimer;
                    CraftIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / CurrentRecipe.craftTime) + 1, 1, CraftCount);
                }

                if (CraftTimer <= 0)
                {
                    State = GameJamMachineState.Complete;
                    CraftTimer = 0;
                }
            }
            UpdateFloatingUI();
        }

        if (floatingGo != null && floatingGo.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
                floatingGo.transform.rotation = cam.transform.rotation;
        }
    }

    public bool CanStartCraft(GameJamRecipe recipe, GameJamInventory inv, int count = 1)
    {
        if (State != GameJamMachineState.Idle) return false;
        count = Mathf.Max(1, count);
        foreach (var kv in recipe.materials)
        {
            if (inv.Model.GetTotalCount(kv.Key) < kv.Value * count) return false;
        }
        if (recipe.requiresFuel && def.hasFuelSystem && FuelTime <= 0) return false;
        return true;
    }

    public int GetMaxCraftCount(GameJamRecipe recipe, GameJamInventory inv)
    {
        int max = 999;
        foreach (var kv in recipe.materials)
        {
            int owned = inv.Model.GetTotalCount(kv.Key);
            int perCraft = kv.Value;
            if (perCraft <= 0) continue;
            max = Mathf.Min(max, owned / perCraft);
        }
        return Mathf.Max(0, max);
    }

    public void StartCraft(GameJamRecipe recipe, GameJamInventory inv, int count = 1)
    {
        count = Mathf.Max(1, count);
        foreach (var kv in recipe.materials)
            inv.Remove(kv.Key, kv.Value * count);

        CurrentRecipe = recipe;
        ProductItemId = recipe.outputItemId;
        ProductAmount = recipe.outputAmount * count;
        CraftCount = count;
        CraftIndex = 1;
        CraftTotal = recipe.craftTime * count;
        CraftTimer = CraftTotal;
        State = GameJamMachineState.Crafting;
        FuelPaused = false;
        UpdateFloatingUI();
    }

    public void EnqueueCraft(GameJamRecipe recipe, GameJamInventory inv, int count = 1)
    {
        count = Mathf.Max(1, count);
        foreach (var kv in recipe.materials)
            inv.Remove(kv.Key, kv.Value * count);

        craftQueue.Enqueue((recipe, count));
    }

    public (string itemId, int amount) CollectProducts()
    {
        if (State != GameJamMachineState.Complete) return (null, 0);
        var result = (ProductItemId, ProductAmount);
        ProductItemId = null;
        ProductAmount = 0;
        CurrentRecipe = null;
        CraftCount = 0;
        CraftIndex = 0;
        State = GameJamMachineState.Idle;

        if (craftQueue.Count > 0)
        {
            var (recipe, count) = craftQueue.Dequeue();
            CurrentRecipe = recipe;
            ProductItemId = recipe.outputItemId;
            ProductAmount = recipe.outputAmount * count;
            CraftCount = count;
            CraftIndex = 1;
            CraftTotal = recipe.craftTime * count;
            CraftTimer = CraftTotal;
            State = GameJamMachineState.Crafting;
            FuelPaused = false;
        }

        UpdateFloatingUI();
        return result;
    }

    public bool AddFuel(GameJamInventory inv)
    {
        if (def == null || !def.hasFuelSystem) return false;
        float fuelPerUnit = def.fuelPerWood > 0f ? def.fuelPerWood : 30f;

        string fuelItemId = string.IsNullOrWhiteSpace(def.fuelItemId) ? "木材" : def.fuelItemId;
        int currentUnits = Mathf.CeilToInt(FuelTime / fuelPerUnit);
        if (currentUnits >= def.maxFuelUnits) return false;
        if (inv.Model.GetTotalCount(fuelItemId) <= 0) return false;

        inv.Remove(fuelItemId, 1);
        FuelTime += fuelPerUnit;
        if (FuelPaused && State == GameJamMachineState.Crafting)
            FuelPaused = false;
        return true;
    }

    public int GetFuelUnits()
    {
        if (def == null || !def.hasFuelSystem) return 0;
        float fuelPerUnit = def.fuelPerWood > 0f ? def.fuelPerWood : 30f;
        return Mathf.CeilToInt(FuelTime / fuelPerUnit);
    }

    void BuildFloatingUI()
    {
        float h = 2.2f;
        var bDef = GameJamBuildingDB.Get(machineId);
        if (bDef != null) h = bDef.height + 0.6f;

        floatingGo = new GameObject("FloatingUI");
        floatingGo.transform.SetParent(transform);
        floatingGo.transform.localPosition = new Vector3(0, h, 0);

        floatingCanvas = floatingGo.AddComponent<Canvas>();
        floatingCanvas.renderMode = RenderMode.WorldSpace;
        floatingCanvas.sortingOrder = 30;
        var rt = floatingGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(2f, 0.6f);
        rt.localScale = Vector3.one * 0.01f;

        var bg = new GameObject("BG");
        bg.transform.SetParent(floatingGo.transform, false);
        var bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(floatingGo.transform, false);
        var iconRect = iconGo.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.offsetMin = new Vector2(5f, 5f);
        iconRect.offsetMax = new Vector2(55f, -5f);
        floatingIcon = iconGo.AddComponent<Image>();
        floatingIcon.color = Color.clear;

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(floatingGo.transform, false);
        var txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0f, 0f);
        txtRect.anchorMax = new Vector2(1f, 1f);
        txtRect.offsetMin = new Vector2(58f, 0);
        txtRect.offsetMax = new Vector2(-5f, 0);
        floatingText = txtGo.AddComponent<Text>();
        floatingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        floatingText.fontSize = 14;
        floatingText.alignment = TextAnchor.MiddleCenter;
        floatingText.color = Color.white;

        floatingGo.SetActive(false);
    }

    void UpdateFloatingUI()
    {
        if (floatingGo == null) return;

        switch (State)
        {
            case GameJamMachineState.Idle:
                floatingGo.SetActive(false);
                break;

            case GameJamMachineState.Crafting:
                floatingGo.SetActive(true);
                GameJamArtLoader.ApplyItemIcon(floatingIcon, ProductItemId, Color.gray);
                if (FuelPaused)
                    floatingText.text = "燃料不足";
                else if (CraftCount > 1)
                    floatingText.text = $"{CraftIndex}/{CraftCount} {FormatTime(CraftTimer)}";
                else
                    floatingText.text = FormatTime(CraftTimer);
                break;

            case GameJamMachineState.Complete:
                floatingGo.SetActive(true);
                GameJamArtLoader.ApplyItemIcon(floatingIcon, ProductItemId, Color.gray);
                floatingText.text = "合成完毕";
                break;
        }
    }

    public static string FormatTime(float seconds)
    {
        if (seconds <= 0) return "0秒";
        int s = Mathf.CeilToInt(seconds);
        if (s < 60) return $"{s}秒";
        int m = s / 60;
        s %= 60;
        if (m < 60) return $"{m}分{s:D2}秒";
        int h = m / 60;
        m %= 60;
        return $"{h}时{m:D2}分";
    }
}
