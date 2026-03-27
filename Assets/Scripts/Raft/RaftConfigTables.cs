using System;
using System.Text;
using UnityEngine;

[Serializable]
public class BuildingCostEntry
{
    public string itemType;
    public int amount;
}

[Serializable]
public class BuildingConfig
{
    public string buildingId;
    public string displayName;
    public BuildingCostEntry[] costs;
}

[Serializable]
public class BuildingTable
{
    public BuildingConfig[] buildings;
}

[Serializable]
public class ItemConfig
{
    public string itemType;
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
public class ResourceRefreshRule
{
    public string resourceType;
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
    public ResourceRefreshRule[] resources;
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

public static class RaftConfigTables
{
    public const string FoundationBuildingId = "raft_foundation";

    const string BuildingTablePath = "RaftConfigs/BuildingTable";
    const string ItemTablePath = "RaftConfigs/ItemTable";
    const string RefreshTablePath = "RaftConfigs/RefreshTable";
    const string SurvivalTablePath = "RaftConfigs/SurvivalTable";

    static BuildingTable buildingTable;
    static ItemTable itemTable;
    static RefreshTable refreshTable;
    static SurvivalTable survivalTable;

    public static BuildingTable BuildingTableData => buildingTable ?? (buildingTable = LoadTable(BuildingTablePath, CreateDefaultBuildingTable()));
    public static ItemTable ItemTableData => itemTable ?? (itemTable = LoadTable(ItemTablePath, CreateDefaultItemTable()));
    public static RefreshTable RefreshTableData => refreshTable ?? (refreshTable = LoadTable(RefreshTablePath, CreateDefaultRefreshTable()));
    public static SurvivalTable SurvivalTableData => survivalTable ?? (survivalTable = LoadTable(SurvivalTablePath, CreateDefaultSurvivalTable()));

    public static void Reload()
    {
        buildingTable = null;
        itemTable = null;
        refreshTable = null;
        survivalTable = null;
    }

    public static void ApplyItemConfigs()
    {
        if (ItemTableData.items == null) return;

        foreach (var entry in ItemTableData.items)
        {
            if (entry == null) continue;
            if (!TryParseItemType(entry.itemType, out var itemType))
            {
                Debug.LogWarning($"Unknown item type in ItemTable: {entry.itemType}");
                continue;
            }

            Inventory.SetConsumableConfig(itemType, entry.hungerRestore, entry.thirstRestore);
        }
    }

    public static BuildingConfig GetBuildingConfig(string buildingId)
    {
        if (BuildingTableData.buildings == null) return null;

        foreach (var entry in BuildingTableData.buildings)
        {
            if (entry != null && string.Equals(entry.buildingId, buildingId, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    public static ItemConfig GetItemConfig(ItemType itemType)
    {
        if (ItemTableData.items == null) return null;

        foreach (var entry in ItemTableData.items)
        {
            if (entry == null) continue;
            if (TryParseItemType(entry.itemType, out var parsedType) && parsedType == itemType)
                return entry;
        }

        return null;
    }

    public static bool CanAffordBuilding(Inventory inventory, string buildingId)
    {
        var config = GetBuildingConfig(buildingId);
        if (inventory == null || config == null || config.costs == null) return false;

        foreach (var cost in config.costs)
        {
            if (cost == null) continue;
            if (!TryParseItemType(cost.itemType, out var itemType))
            {
                Debug.LogWarning($"Unknown build cost item type: {cost.itemType}");
                return false;
            }

            if (inventory.GetCount(itemType) < Mathf.Max(0, cost.amount))
                return false;
        }

        return true;
    }

    public static bool ConsumeBuildingCost(Inventory inventory, string buildingId)
    {
        if (!CanAffordBuilding(inventory, buildingId))
            return false;

        var config = GetBuildingConfig(buildingId);
        if (config == null || config.costs == null) return false;

        foreach (var cost in config.costs)
        {
            if (cost == null) continue;
            if (TryParseItemType(cost.itemType, out var itemType) && cost.amount > 0)
                inventory.Remove(itemType, cost.amount);
        }

        return true;
    }

    public static string FormatBuildingCost(string buildingId)
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

            string itemName = cost.itemType;
            if (TryParseItemType(cost.itemType, out var itemType))
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
            if (!TryParseResourceType(entry.resourceType, out _)) continue;
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
            if (!TryParseResourceType(entry.resourceType, out var resourceType)) continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return resourceType;
        }

        return ResourceType.Wood;
    }

    static bool TryParseItemType(string raw, out ItemType itemType)
    {
        return Enum.TryParse(raw, true, out itemType);
    }

    static bool TryParseResourceType(string raw, out ResourceType resourceType)
    {
        return Enum.TryParse(raw, true, out resourceType);
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
                        new BuildingCostEntry { itemType = nameof(ItemType.Wood), amount = 1 }
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
                    itemType = nameof(ItemType.Beet),
                    displayName = "\u751c\u83dc",
                    hungerRestore = 35f,
                    thirstRestore = 0f
                },
                new ItemConfig
                {
                    itemType = nameof(ItemType.WaterBottle),
                    displayName = "\u77ff\u6cc9\u6c34",
                    hungerRestore = 0f,
                    thirstRestore = 40f
                },
                new ItemConfig
                {
                    itemType = nameof(ItemType.Coconut),
                    displayName = "\u6930\u5b50",
                    hungerRestore = 15f,
                    thirstRestore = 20f
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
                new ResourceRefreshRule { resourceType = nameof(ResourceType.Wood), weight = 30f },
                new ResourceRefreshRule { resourceType = nameof(ResourceType.Plastic), weight = 25f },
                new ResourceRefreshRule { resourceType = nameof(ResourceType.Coconut), weight = 15f },
                new ResourceRefreshRule { resourceType = nameof(ResourceType.Beet), weight = 15f },
                new ResourceRefreshRule { resourceType = nameof(ResourceType.WaterBottle), weight = 15f }
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
}
