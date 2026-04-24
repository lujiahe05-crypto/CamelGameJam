using System;
using UnityEngine;

[Serializable]
public class PortiaColorData
{
    public float r = 1f;
    public float g = 1f;
    public float b = 1f;
    public float a = 1f;

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}

[Serializable]
public class PortiaVector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class PortiaItemConfig
{
    public string itemId;
    public string displayName;
    public string description;
    public string itemType;
    public string rarity;
    public int maxStack = 99;
    public int sellPrice;
    public PortiaColorData iconColor;
    public string iconPath;
    public string prefabPath;
}

[Serializable]
public class PortiaItemTable
{
    public PortiaItemConfig[] items;
}

[Serializable]
public class PortiaBuildingConfig
{
    public string itemId;
    public int gridW = 1;
    public int gridH = 1;
    public float height = 1f;
    public string prefabPath;
}

[Serializable]
public class PortiaBuildingTable
{
    public PortiaBuildingConfig[] buildings;
}

[Serializable]
public class PortiaRecipeInputConfig
{
    public string itemId;
    public int amount;
}

[Serializable]
public class PortiaRecipeConfig
{
    public string recipeId;
    public string outputItemId;
    public int outputAmount = 1;
    public float craftTime = 1f;
    public bool requiresFuel;
    public PortiaRecipeInputConfig[] inputs;
}

[Serializable]
public class PortiaMachineConfig
{
    public string machineId;
    public string displayName;
    public bool hasFuelSystem;
    public string fuelItemId;
    public float fuelPerWood = 30f;
    public int maxFuelUnits = 40;
    public PortiaRecipeConfig[] recipes;
}

[Serializable]
public class PortiaMachineTable
{
    public PortiaMachineConfig[] machines;
}

[Serializable]
public class PortiaInventoryGrantConfig
{
    public string itemId;
    public int amount;
}

[Serializable]
public class PortiaResourceDropConfig
{
    public string itemId;
    public int amount = 1;
    public float weight = 1f;
}

[Serializable]
public class PortiaResourceNodeConfig
{
    public string label;
    public string itemId;
    public int amount = 1;
    public int num;
    public string shape = nameof(PrimitiveType.Cube);
    public PortiaVector3Data scale;
    public PortiaVector3Data position;
    public PortiaColorData color;
    public PortiaResourceDropConfig[] drops;
    public string prefabPath;
}

[Serializable]
public class PortiaPlacedMachineConfig
{
    public string machineId;
    public PortiaVector3Data position;
}

[Serializable]
public class PortiaSettingsTable
{
    public float groundScaleX = 5f;
    public float groundScaleZ = 5f;
    public float boundaryHalfSize = 25f;
    public float boundaryWallHeight = 2f;
    public float boundaryWallThickness = 0.5f;
    public float playerMoveSpeed = 6f;
    public float playerJumpHeight = 1.2f;
    public float playerGravity = -20f;
    public float playerTurnSmoothTime = 0.1f;
    public float interactRadius = 2.5f;
    public PortiaInventoryGrantConfig[] initialInventory;
    public PortiaResourceNodeConfig[] resourceNodes;
    public PortiaPlacedMachineConfig[] placedMachines;
}

public static class PortiaConfigTables
{
    const string ItemTablePath = "PortiaConfigs/ItemTable";
    const string BuildingTablePath = "PortiaConfigs/BuildingTable";
    const string MachineTablePath = "PortiaConfigs/MachineTable";
    const string SettingsTablePath = "PortiaConfigs/SettingsTable";

    static PortiaItemTable itemTable;
    static PortiaBuildingTable buildingTable;
    static PortiaMachineTable machineTable;
    static PortiaSettingsTable settingsTable;

    public static PortiaItemTable ItemTableData =>
        itemTable ?? (itemTable = LoadTable(ItemTablePath, new PortiaItemTable { items = Array.Empty<PortiaItemConfig>() }));

    public static PortiaBuildingTable BuildingTableData =>
        buildingTable ?? (buildingTable = LoadTable(BuildingTablePath, new PortiaBuildingTable { buildings = Array.Empty<PortiaBuildingConfig>() }));

    public static PortiaMachineTable MachineTableData =>
        machineTable ?? (machineTable = LoadTable(MachineTablePath, new PortiaMachineTable { machines = Array.Empty<PortiaMachineConfig>() }));

    public static PortiaSettingsTable SettingsTableData =>
        settingsTable ?? (settingsTable = LoadTable(SettingsTablePath, CreateDefaultSettingsTable()));

    public static void Reload()
    {
        itemTable = null;
        buildingTable = null;
        machineTable = null;
        settingsTable = null;
    }

    public static bool TryParseItemType(string raw, out GameJamItemType itemType)
    {
        return Enum.TryParse(raw, true, out itemType);
    }

    public static bool TryParseRarity(string raw, out GameJamRarity rarity)
    {
        return Enum.TryParse(raw, true, out rarity);
    }

    public static bool TryParsePrimitiveType(string raw, out PrimitiveType primitiveType)
    {
        return Enum.TryParse(raw, true, out primitiveType);
    }

    public static string GetPrimaryResourceItemId(PortiaResourceNodeConfig entry)
    {
        if (entry == null)
            return null;

        if (!string.IsNullOrWhiteSpace(entry.itemId))
            return entry.itemId;

        if (entry.drops != null)
        {
            foreach (var drop in entry.drops)
            {
                if (drop != null && !string.IsNullOrWhiteSpace(drop.itemId))
                    return drop.itemId;
            }
        }

        return null;
    }

    static T LoadTable<T>(string resourcePath, T fallback) where T : class
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
            return fallback;

        var table = JsonUtility.FromJson<T>(asset.text);
        if (table == null)
        {
            Debug.LogWarning($"Invalid Portia config table at Resources/{resourcePath}. Using defaults.");
            return fallback;
        }

        return table;
    }

    static PortiaSettingsTable CreateDefaultSettingsTable()
    {
        return new PortiaSettingsTable
        {
            initialInventory = Array.Empty<PortiaInventoryGrantConfig>(),
            resourceNodes = Array.Empty<PortiaResourceNodeConfig>(),
            placedMachines = Array.Empty<PortiaPlacedMachineConfig>()
        };
    }
}
