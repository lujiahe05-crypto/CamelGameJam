using System.Collections.Generic;

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
            fuelPerWood = 30f,
            maxFuelUnits = 40,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("furnace_glass", "玻璃", 25,
                    new Dictionary<string, int> { { "沙子", 8 } }, 45f, true),
                new GameJamRecipe("furnace_charcoal", "木炭", 1,
                    new Dictionary<string, int> { { "木材", 1 } }, 20f, true),
            }
        });

        Reg(new GameJamMachineDef
        {
            machineId = "切割机",
            displayName = "切割机",
            hasFuelSystem = false,
            fuelPerWood = 0,
            maxFuelUnits = 0,
            recipes = new List<GameJamRecipe>
            {
                new GameJamRecipe("cutter_plank", "木板", 1,
                    new Dictionary<string, int> { { "木材", 2 } }, 15f, false),
            }
        });
    }

    static void Reg(GameJamMachineDef def) => defs[def.machineId] = def;

    public static GameJamMachineDef Get(string machineId)
    {
        Init();
        return defs.TryGetValue(machineId, out var def) ? def : null;
    }

    public static bool IsMachine(string itemId)
    {
        Init();
        return defs.ContainsKey(itemId);
    }
}
