using System.Collections.Generic;
using UnityEngine;

public class GameJamRecipe
{
    public string id;
    public string outputItemId;
    public int outputAmount;
    public Dictionary<string, int> materials;
    public float craftTime;
    public bool requiresFuel;

    public GameJamRecipe(string id, string output, int outputAmt,
        Dictionary<string, int> mats, float time, bool fuel)
    {
        this.id = id;
        outputItemId = output;
        outputAmount = outputAmt;
        materials = mats;
        craftTime = time;
        requiresFuel = fuel;
    }
}

public class GameJamMachineDef
{
    public string machineId;
    public string displayName;
    public bool hasFuelSystem;
    public string fuelItemId;
    public float fuelPerWood;
    public int maxFuelUnits;
    public List<GameJamRecipe> recipes;
}

public static class GameJamMachineDB
{
    static Dictionary<string, GameJamMachineDef> defs;

    static void Init()
    {
        if (defs != null) return;
        defs = new Dictionary<string, GameJamMachineDef>();

        Reg(new GameJamMachineDef
        {
            machineId = "民用熔炉",
            displayName = "民用熔炉",
            hasFuelSystem = true,
            fuelItemId = "木材",
            fuelPerWood = 30f,
            maxFuelUnits = 40,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("furnace_copper_ingot", "铜锭", 1,
                    new Dictionary<string, int> { { "铜矿", 2 } }, 25f, true),
                new GameJamRecipe("furnace_iron_ingot", "铁锭", 1,
                    new Dictionary<string, int> { { "铁矿", 2 } }, 30f, true),
                new GameJamRecipe("furnace_bronze_ingot", "青铜锭", 1,
                    new Dictionary<string, int> { { "铜锭", 1 }, { "铁锭", 1 } }, 35f, true),
                new GameJamRecipe("furnace_glass", "玻璃", 25,
                    new Dictionary<string, int> { { "沙子", 8 } }, 45f, true),
                new GameJamRecipe("furnace_charcoal", "木炭", 1,
                    new Dictionary<string, int> { { "木材", 1 } }, 20f, true),
            }
        });

        Reg(new GameJamMachineDef
        {
            machineId = "熔炉",
            displayName = "熔炉",
            hasFuelSystem = true,
            fuelItemId = "木材",
            fuelPerWood = 30f,
            maxFuelUnits = 10,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("1", "石砖", 1,
                    new Dictionary<string, int> { { "石头", 1 } }, 5f, true),
                new GameJamRecipe("3", "铜锭", 1,
                    new Dictionary<string, int> { { "铜矿", 1 } }, 3f, true),
            }
        });

        Reg(new GameJamMachineDef
        {
            machineId = "切割机",
            displayName = "切割机",
            hasFuelSystem = false,
            fuelItemId = null,
            fuelPerWood = 0f,
            maxFuelUnits = 0,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("cutter_plank", "木板", 1,
                    new Dictionary<string, int> { { "木材", 2 } }, 15f, false),
            }
        });

        Reg(new GameJamMachineDef
        {
            machineId = "工作台",
            displayName = "工作台",
            hasFuelSystem = false,
            fuelItemId = null,
            fuelPerWood = 0,
            maxFuelUnits = 0,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("bench_stone_pick", "石镐", 1,
                    new Dictionary<string, int> { { "石块", 10 }, { "木材", 5 } }, 10f, false),
                new GameJamRecipe("bench_copper_pick", "铜镐", 1,
                    new Dictionary<string, int> { { "铜锭", 3 }, { "木材", 5 } }, 15f, false),
                new GameJamRecipe("bench_copper_axe", "铜斧", 1,
                    new Dictionary<string, int> { { "铜锭", 3 }, { "木材", 8 } }, 15f, false),
                new GameJamRecipe("bench_iron_axe", "铁斧", 1,
                    new Dictionary<string, int> { { "铁锭", 3 }, { "木材", 8 } }, 20f, false),
                new GameJamRecipe("bench_leather", "皮甲", 1,
                    new Dictionary<string, int> { { "青铜锭", 5 }, { "木材", 3 } }, 25f, false),
                new GameJamRecipe("bench_bandage", "绷带", 2,
                    new Dictionary<string, int> { { "草药", 3 } }, 8f, false),
                new GameJamRecipe("bench_furnace", "民用熔炉", 1,
                    new Dictionary<string, int> { { "石块", 10 }, { "铜锭", 5 } }, 30f, false),
                new GameJamRecipe("bench_cutter", "切割机", 1,
                    new Dictionary<string, int> { { "铁锭", 8 }, { "木材", 10 } }, 30f, false),
                new GameJamRecipe("bench_storage", "储物箱", 1,
                    new Dictionary<string, int> { { "木板", 6 } }, 10f, false),
            }
        });

        ApplyConfigOverrides();
    }

    static void Reg(GameJamMachineDef def) => defs[def.machineId] = def;

    public static void Reload() => defs = null;

    static void ApplyConfigOverrides()
    {
        var table = PortiaConfigTables.MachineTableData;
        if (table == null || table.machines == null) return;

        foreach (var entry in table.machines)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.machineId))
                continue;

            defs.TryGetValue(entry.machineId, out var existing);

            var def = new GameJamMachineDef
            {
                machineId = entry.machineId,
                displayName = !string.IsNullOrWhiteSpace(entry.displayName)
                    ? entry.displayName
                    : existing != null ? existing.displayName : entry.machineId,
                hasFuelSystem = entry.hasFuelSystem,
                fuelItemId = entry.hasFuelSystem
                    ? (!string.IsNullOrWhiteSpace(entry.fuelItemId) ? entry.fuelItemId : existing != null ? existing.fuelItemId : null)
                    : null,
                fuelPerWood = entry.hasFuelSystem
                    ? (entry.fuelPerWood > 0f ? entry.fuelPerWood : existing != null ? existing.fuelPerWood : 0f)
                    : 0f,
                maxFuelUnits = entry.hasFuelSystem
                    ? (entry.maxFuelUnits > 0 ? entry.maxFuelUnits : existing != null ? existing.maxFuelUnits : 0)
                    : 0,
                recipes = BuildRecipes(entry, existing)
            };

            Reg(def);
        }
    }

    static List<GameJamRecipe> BuildRecipes(PortiaMachineConfig entry, GameJamMachineDef existing)
    {
        if (entry.recipes == null || entry.recipes.Length == 0)
            return existing != null ? existing.recipes : new List<GameJamRecipe>();

        var recipes = new List<GameJamRecipe>(entry.recipes.Length);
        foreach (var recipeEntry in entry.recipes)
        {
            if (recipeEntry == null || string.IsNullOrWhiteSpace(recipeEntry.recipeId) || string.IsNullOrWhiteSpace(recipeEntry.outputItemId))
            {
                Debug.LogWarning("Skipped invalid Portia recipe config entry.");
                continue;
            }

            var materials = new Dictionary<string, int>();
            if (recipeEntry.inputs != null)
            {
                foreach (var input in recipeEntry.inputs)
                {
                    if (input == null || string.IsNullOrWhiteSpace(input.itemId) || input.amount <= 0)
                        continue;

                    if (materials.ContainsKey(input.itemId))
                        materials[input.itemId] += input.amount;
                    else
                        materials[input.itemId] = input.amount;
                }
            }

            recipes.Add(new GameJamRecipe(
                recipeEntry.recipeId,
                recipeEntry.outputItemId,
                recipeEntry.outputAmount > 0 ? recipeEntry.outputAmount : 1,
                materials,
                Mathf.Max(0f, recipeEntry.craftTime),
                recipeEntry.requiresFuel));
        }

        return recipes;
    }

    public static GameJamMachineDef Get(string machineId)
    {
        Init();
        return defs.TryGetValue(machineId, out var def) ? def : null;
    }

    public static List<GameJamRecipe> GetRecipesForMachine(string machineId)
    {
        Init();
        if (defs.TryGetValue(machineId, out var def) && def != null && def.recipes != null && def.recipes.Count > 0)
            return def.recipes;

        switch (machineId)
        {
            case "组装台":
            case "工作台":
                return new List<GameJamRecipe>
                {
                    new GameJamRecipe("6", "熔炉", 1,
                        new Dictionary<string, int> { { "木材", 2 }, { "石头", 1 } }, 0f, false),
                    new GameJamRecipe("7", "切割机", 1,
                        new Dictionary<string, int> { { "石砖", 2 }, { "铜锭", 1 } }, 0f, false),
                };
            case "熔炉":
            case "民用熔炉":
                return new List<GameJamRecipe>
                {
                    new GameJamRecipe("1", "石砖", 1,
                        new Dictionary<string, int> { { "石头", 1 } }, 5f, true),
                    new GameJamRecipe("3", "铜锭", 1,
                        new Dictionary<string, int> { { "铜矿", 1 } }, 3f, true),
                };
            case "切割机":
                return new List<GameJamRecipe>
                {
                    new GameJamRecipe("2", "木板", 1,
                        new Dictionary<string, int> { { "木材", 1 } }, 5f, true),
                    new GameJamRecipe("5", "铜质板材", 1,
                        new Dictionary<string, int> { { "铜锭", 1 } }, 3f, true),
                };
            default:
                return new List<GameJamRecipe>();
        }
    }

    public static bool IsMachine(string itemId)
    {
        Init();
        return defs.ContainsKey(itemId);
    }
}
