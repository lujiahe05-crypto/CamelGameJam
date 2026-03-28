using System;
using System.Text;
using UnityEngine;

[Serializable]
public class ItemAmountEntry
{
    public int itemTypeId;
    public int amount;
}

[Serializable]
public class BuildingConfig
{
    public int buildingId;
    public string displayName;
    public ItemAmountEntry[] costs;
}

[Serializable]
public class BuildingTable
{
    public BuildingConfig[] buildings;
}

[Serializable]
public class ItemConfig
{
    public int itemTypeId;
    public string displayName;
    public float hungerRestore;
    public float thirstRestore;
}

[Serializable]
public class ItemTable
{
    public ItemConfig[] items;
}

[Serializable]
public class RefreshRule
{
    public int itemTypeId;
    public float weight;
}

[Serializable]
public class RefreshTable
{
    public int maxResources = 20;
    public float spawnInterval = 2.5f;
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 40f;
    public float despawnDistance = 60f;
    public RefreshRule[] resources;
}

[Serializable]
public class SurvivalTable
{
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float initialHealth = 100f;
    public float initialHunger = 100f;
    public float initialThirst = 100f;
    public float hungerRate = 100f / 180f;
    public float thirstRate = 100f / 120f;
    public float starveDamage = 5f;
    public float respawnDelay = 3f;
    public float respawnHealth = 100f;
    public float respawnHunger = 80f;
    public float respawnThirst = 80f;
}

[Serializable]
public class SynthesisRecipe
{
    public string recipeId;
    public string displayName;
    public ItemAmountEntry[] inputs;
    public int outputItemTypeId;
    public int outputAmount = 1;
}

[Serializable]
public class SynthesisTable
{
    public SynthesisRecipe[] recipes;
}

public static class RaftConfigTables
{
    public const int FoundationBuildingId = 1;

    const string BuildingTablePath = "RaftConfigs/BuildingTable";
    const string ItemTablePath = "RaftConfigs/ItemTable";
    const string RefreshTablePath = "RaftConfigs/RefreshTable";
    const string SurvivalTablePath = "RaftConfigs/SurvivalTable";
    const string SynthesisTablePath = "RaftConfigs/SynthesisTable";

    static BuildingTable buildingTable;
    static ItemTable itemTable;
    static RefreshTable refreshTable;
    static SurvivalTable survivalTable;
    static SynthesisTable synthesisTable;

    public static BuildingTable BuildingTableData => buildingTable ?? (buildingTable = LoadTable(BuildingTablePath, CreateDefaultBuildingTable()));
    public static ItemTable ItemTableData => itemTable ?? (itemTable = LoadTable(ItemTablePath, CreateDefaultItemTable()));
    public static RefreshTable RefreshTableData => refreshTable ?? (refreshTable = LoadTable(RefreshTablePath, CreateDefaultRefreshTable()));
    public static SurvivalTable SurvivalTableData => survivalTable ?? (survivalTable = LoadTable(SurvivalTablePath, CreateDefaultSurvivalTable()));
    public static SynthesisTable SynthesisTableData => synthesisTable ?? (synthesisTable = LoadTable(SynthesisTablePath, CreateDefaultSynthesisTable()));

    public static void Reload()
    {
        buildingTable = null;
        itemTable = null;
        refreshTable = null;
        survivalTable = null;
        synthesisTable = null;
    }

    public static void ApplyItemConfigs()
    {
        if (ItemTableData.items == null) return;

        foreach (var entry in ItemTableData.items)
        {
            if (entry == null) continue;
            if (!TryGetItemTypeById(entry.itemTypeId, out var itemType))
            {
                Debug.LogWarning($"Unknown item type id in ItemTable: {entry.itemTypeId}");
                continue;
            }

            Inventory.SetConsumableConfig(itemType, entry.hungerRestore, entry.thirstRestore);
        }
    }

    public static bool TryGetItemTypeById(int itemTypeId, out ItemType itemType)
    {
        if (Enum.IsDefined(typeof(ItemType), itemTypeId))
        {
            itemType = (ItemType)itemTypeId;
            return true;
        }

        itemType = ItemType.None;
        return false;
    }

    public static int GetItemTypeId(ItemType itemType)
    {
        return (int)itemType;
    }

    public static bool TryGetResourceTypeByItemTypeId(int itemTypeId, out ResourceType resourceType)
    {
        if (TryGetItemTypeById(itemTypeId, out var itemType))
        {
            switch (itemType)
            {
                case ItemType.Wood:
                    resourceType = ResourceType.Wood;
                    return true;
                case ItemType.Plastic:
                    resourceType = ResourceType.Plastic;
                    return true;
                case ItemType.Coconut:
                    resourceType = ResourceType.Coconut;
                    return true;
                case ItemType.Beet:
                    resourceType = ResourceType.Beet;
                    return true;
                case ItemType.WaterBottle:
                    resourceType = ResourceType.WaterBottle;
                    return true;
            }
        }

        resourceType = ResourceType.Wood;
        return false;
    }

    public static BuildingConfig GetBuildingConfig(int buildingId)
    {
        if (BuildingTableData.buildings == null) return null;

        foreach (var entry in BuildingTableData.buildings)
        {
            if (entry != null && entry.buildingId == buildingId)
                return entry;
        }

        return null;
    }

    public static ItemConfig GetItemConfig(ItemType itemType)
    {
        if (ItemTableData.items == null) return null;

        int itemTypeId = GetItemTypeId(itemType);
        foreach (var entry in ItemTableData.items)
        {
            if (entry != null && entry.itemTypeId == itemTypeId)
                return entry;
        }

        return null;
    }

    public static SynthesisTable GetSynthesisTable()
    {
        return SynthesisTableData;
    }

    public static bool CanAffordBuilding(Inventory inventory, int buildingId)
    {
        var config = GetBuildingConfig(buildingId);
        if (inventory == null || config == null || config.costs == null) return false;

        foreach (var cost in config.costs)
        {
            if (cost == null) continue;
            if (!TryGetItemTypeById(cost.itemTypeId, out var itemType))
            {
                Debug.LogWarning($"Unknown build cost item type id: {cost.itemTypeId}");
                return false;
            }

            if (inventory.GetCount(itemType) < Mathf.Max(0, cost.amount))
                return false;
        }

        return true;
    }

    public static bool ConsumeBuildingCost(Inventory inventory, int buildingId)
    {
        if (!CanAffordBuilding(inventory, buildingId))
            return false;

        var config = GetBuildingConfig(buildingId);
        if (config == null || config.costs == null) return false;

        foreach (var cost in config.costs)
        {
            if (cost == null || cost.amount <= 0) continue;
            if (TryGetItemTypeById(cost.itemTypeId, out var itemType))
                inventory.Remove(itemType, cost.amount);
        }

        return true;
    }

    public static string FormatBuildingCost(int buildingId)
    {
        var config = GetBuildingConfig(buildingId);
        if (config == null || config.costs == null || config.costs.Length == 0)
            return "\u65e0";

        var builder = new StringBuilder();
        for (int i = 0; i < config.costs.Length; i++)
        {
            var cost = config.costs[i];
            if (cost == null) continue;

            if (builder.Length > 0)
                builder.Append(" + ");

            string itemName = cost.itemTypeId.ToString();
            if (TryGetItemTypeById(cost.itemTypeId, out var itemType))
                itemName = Inventory.GetItemName(itemType);

            builder.Append(cost.amount);
            builder.Append(itemName);
        }

        return builder.Length == 0 ? "\u65e0" : builder.ToString();
    }

    public static RefreshTable GetRefreshTable()
    {
        return RefreshTableData;
    }

    public static SurvivalTable GetSurvivalTable()
    {
        return SurvivalTableData;
    }

    public static ResourceType RollRefreshResource()
    {
        var table = RefreshTableData;
        if (table.resources == null || table.resources.Length == 0)
            return ResourceType.Wood;

        float totalWeight = 0f;
        foreach (var entry in table.resources)
        {
            if (entry == null) continue;
            if (!TryGetResourceTypeByItemTypeId(entry.itemTypeId, out _)) continue;
            if (entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return ResourceType.Wood;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in table.resources)
        {
            if (entry == null || entry.weight <= 0f) continue;
            if (!TryGetResourceTypeByItemTypeId(entry.itemTypeId, out var resourceType)) continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return resourceType;
        }

        return ResourceType.Wood;
    }

    static T LoadTable<T>(string resourcePath, T fallback) where T : class
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogWarning($"Missing config table at Resources/{resourcePath}. Using defaults.");
            return fallback;
        }

        var table = JsonUtility.FromJson<T>(asset.text);
        if (table == null)
        {
            Debug.LogWarning($"Invalid config table at Resources/{resourcePath}. Using defaults.");
            return fallback;
        }

        return table;
    }

    static BuildingTable CreateDefaultBuildingTable()
    {
        return new BuildingTable
        {
            buildings = new[]
            {
                new BuildingConfig
                {
                    buildingId = FoundationBuildingId,
                    displayName = "\u6728\u7b4f\u5730\u57fa",
                    costs = new[]
                    {
                        new ItemAmountEntry { itemTypeId = GetItemTypeId(ItemType.Wood), amount = 1 }
                    }
                }
            }
        };
    }

    static ItemTable CreateDefaultItemTable()
    {
        return new ItemTable
        {
            items = new[]
            {
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.Hook),
                    displayName = "\u9497\u5b50",
                    hungerRestore = 0f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.BuildHammer),
                    displayName = "\u5efa\u9020\u9524",
                    hungerRestore = 0f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.Wood),
                    displayName = "\u6728\u6750",
                    hungerRestore = 0f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.Plastic),
                    displayName = "\u5851\u6599",
                    hungerRestore = 0f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.Coconut),
                    displayName = "\u6930\u5b50",
                    hungerRestore = 15f,
                    thirstRestore = 20f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.Beet),
                    displayName = "\u751c\u83dc",
                    hungerRestore = 35f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemTypeId = GetItemTypeId(ItemType.WaterBottle),
                    displayName = "\u77ff\u6cc9\u6c34",
                    hungerRestore = 0f,
                    thirstRestore = 40f
                }
            }
        };
    }

    static RefreshTable CreateDefaultRefreshTable()
    {
        return new RefreshTable
        {
            maxResources = 20,
            spawnInterval = 2.5f,
            minSpawnDistance = 15f,
            maxSpawnDistance = 40f,
            despawnDistance = 60f,
            resources = new[]
            {
                new RefreshRule { itemTypeId = GetItemTypeId(ItemType.Wood), weight = 30f },
                new RefreshRule { itemTypeId = GetItemTypeId(ItemType.Plastic), weight = 25f },
                new RefreshRule { itemTypeId = GetItemTypeId(ItemType.Coconut), weight = 15f },
                new RefreshRule { itemTypeId = GetItemTypeId(ItemType.Beet), weight = 15f },
                new RefreshRule { itemTypeId = GetItemTypeId(ItemType.WaterBottle), weight = 15f }
            }
        };
    }

    static SurvivalTable CreateDefaultSurvivalTable()
    {
        return new SurvivalTable
        {
            maxHealth = 100f,
            maxHunger = 100f,
            maxThirst = 100f,
            initialHealth = 100f,
            initialHunger = 100f,
            initialThirst = 100f,
            hungerRate = 100f / 180f,
            thirstRate = 100f / 120f,
            starveDamage = 5f,
            respawnDelay = 3f,
            respawnHealth = 100f,
            respawnHunger = 80f,
            respawnThirst = 80f
        };
    }

    static SynthesisTable CreateDefaultSynthesisTable()
    {
        return new SynthesisTable
        {
            recipes = new[]
            {
                new SynthesisRecipe
                {
                    recipeId = "craft_water_bottle",
                    displayName = "\u5408\u6210\u77ff\u6cc9\u6c34",
                    inputs = new[]
                    {
                        new ItemAmountEntry { itemTypeId = GetItemTypeId(ItemType.Plastic), amount = 2 },
                        new ItemAmountEntry { itemTypeId = GetItemTypeId(ItemType.Coconut), amount = 1 }
                    },
                    outputItemTypeId = GetItemTypeId(ItemType.WaterBottle),
                    outputAmount = 1
                }
            }
        };
    }
}
