using UnityEngine;
using UnityEngine.UI;

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

    public bool CanStartCraft(GameJamRecipe recipe, GameJamInventory inv)
    {
        if (State != GameJamMachineState.Idle) return false;
        foreach (var kv in recipe.materials)
        {
            if (inv.Model.GetTotalCount(kv.Key) < kv.Value) return false;
        }
        if (recipe.requiresFuel && def.hasFuelSystem && FuelTime <= 0) return false;
        return true;
    }

    public void StartCraft(GameJamRecipe recipe, GameJamInventory inv)
    {
        foreach (var kv in recipe.materials)
            inv.Remove(kv.Key, kv.Value);

        CurrentRecipe = recipe;
        ProductItemId = recipe.outputItemId;
        ProductAmount = recipe.outputAmount;
        CraftTotal = recipe.craftTime;
        CraftTimer = recipe.craftTime;
        State = GameJamMachineState.Crafting;
        FuelPaused = false;
        UpdateFloatingUI();
    }

    public (string itemId, int amount) CollectProducts()
    {
        if (State != GameJamMachineState.Complete) return (null, 0);
        var result = (ProductItemId, ProductAmount);
        ProductItemId = null;
        ProductAmount = 0;
        CurrentRecipe = null;
        State = GameJamMachineState.Idle;
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
                var outDef = GameJamItemDB.Get(ProductItemId);
                floatingIcon.color = outDef != null ? outDef.iconColor : Color.gray;
                if (FuelPaused)
                    floatingText.text = "燃料不足";
                else
                    floatingText.text = FormatTime(CraftTimer);
                break;

            case GameJamMachineState.Complete:
                floatingGo.SetActive(true);
                var pDef = GameJamItemDB.Get(ProductItemId);
                floatingIcon.color = pDef != null ? pDef.iconColor : Color.gray;
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
