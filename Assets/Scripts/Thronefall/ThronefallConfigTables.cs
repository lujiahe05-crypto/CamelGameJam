using System;
using UnityEngine;

[Serializable]
public class TFBuildingConfig
{
    public int buildingId;
    public string buildingName;
    public string description;
    public string buildingType;
    public int coinCost;
    public int maxHP;
    public int atk;
    public int def;
    public float attackRange;
    public float attackInterval;
    public float arrowSpeed;
    public float arcHeight;
    public float aoeRadius;
    public int dailyYield;
    public int recruitCost;
    public int maxRecruits;
    public int[] upgradeIds;
    public string branchIcon;
    public string allyUnitType;
}

[Serializable]
public class TFBuildingTable
{
    public TFBuildingConfig[] buildings;
}

[Serializable]
public class TFAllyUnitConfig
{
    public string unitType;
    public string unitName;
    public int maxHP;
    public int atk;
    public int def;
    public float moveSpeed;
    public float attackRange;
    public float attackInterval;
    public float respawnTime;
    public float arrowSpeed;
    public float arcHeight;
    public float kiteDistance;
    public float chargeSpeed;
    public float chargeDuration;
    public float chargeMultiplier;
    public float chargeCooldown;
}

[Serializable]
public class TFAllyUnitTable
{
    public TFAllyUnitConfig[] units;
}

[Serializable]
public class TFMonsterConfig
{
    public int monsterId;
    public string monsterName;
    public int maxHP;
    public int atk;
    public int def;
    public float moveSpeed;
    public float attackRange;
    public float attackInterval;
    public string iconLabel;
}

[Serializable]
public class TFMonsterTable
{
    public TFMonsterConfig[] monsters;
}

[Serializable]
public class TFSerializedVector3
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
public class TFSpawnAction
{
    public int monsterId;
    public int count;
    public float spawnInterval;
}

[Serializable]
public class TFSpawnPointConfig
{
    public TFSerializedVector3 position;
    public TFSpawnAction[] actions;
}

[Serializable]
public class TFWaveConfig
{
    public int dayNumber;
    public TFSpawnPointConfig[] spawnPoints;
}

[Serializable]
public class TFWaveTable
{
    public int dailyBaseCoins;
    public int startingCoins;
    public TFWaveConfig[] waves;
}

[Serializable]
public class TFSkillConfig
{
    public string skillId;
    public float cooldown;
    public float damageMultiplier;
    public float accelTime;
    public float decelTime;
    public float maxSpeed;
    public int arrowCount;
    public float spreadAngle;
    public float arrowSpeed;
    public float arcHeight;
}

[Serializable]
public class TFWeaponConfig
{
    public string weaponId;
    public string weaponName;
    public int atk;
    public int def;
    public float attackRange;
    public float attackInterval;
    public float arrowSpeed;
    public float arcHeight;
    public TFSkillConfig skill;
}

[Serializable]
public class TFHeroConfig
{
    public int maxHP;
    public float moveSpeed;
    public float reviveTime;
    public TFWeaponConfig[] weapons;
}

[Serializable]
public class TFHeroTable
{
    public TFHeroConfig hero;
}

public static class ThronefallConfigTables
{
    const string BuildingTablePath = "ThronefallConfigs/BuildingTable";
    const string AllyUnitTablePath = "ThronefallConfigs/AllyUnitTable";
    const string MonsterTablePath = "ThronefallConfigs/MonsterTable";
    const string WaveTablePath = "ThronefallConfigs/WaveTable";
    const string HeroTablePath = "ThronefallConfigs/HeroTable";

    static TFBuildingTable buildingTable;
    static TFAllyUnitTable allyUnitTable;
    static TFMonsterTable monsterTable;
    static TFWaveTable waveTable;
    static TFHeroTable heroTable;

    public static TFBuildingTable BuildingTableData =>
        buildingTable ?? (buildingTable = LoadTable(BuildingTablePath, CreateDefaultBuildingTable()));

    public static TFAllyUnitTable AllyUnitTableData =>
        allyUnitTable ?? (allyUnitTable = LoadTable(AllyUnitTablePath, CreateDefaultAllyUnitTable()));

    public static TFMonsterTable MonsterTableData =>
        monsterTable ?? (monsterTable = LoadTable(MonsterTablePath, CreateDefaultMonsterTable()));

    public static TFWaveTable WaveTableData =>
        waveTable ?? (waveTable = LoadTable(WaveTablePath, CreateDefaultWaveTable()));

    public static TFHeroTable HeroTableData =>
        heroTable ?? (heroTable = LoadTable(HeroTablePath, CreateDefaultHeroTable()));

    public static TFHeroConfig GetHeroConfig() => HeroTableData?.hero;

    public static void Reload()
    {
        buildingTable = null;
        allyUnitTable = null;
        monsterTable = null;
        waveTable = null;
        heroTable = null;
    }

    public static TFBuildingConfig GetBuildingConfig(int buildingId)
    {
        if (BuildingTableData.buildings == null) return null;
        foreach (var b in BuildingTableData.buildings)
        {
            if (b != null && b.buildingId == buildingId)
                return b;
        }
        return null;
    }

    public static TFAllyUnitConfig GetAllyUnitConfig(string unitType)
    {
        if (string.IsNullOrEmpty(unitType)) return null;
        if (AllyUnitTableData.units == null) return null;
        foreach (var u in AllyUnitTableData.units)
        {
            if (u != null && u.unitType == unitType)
                return u;
        }
        return null;
    }

    public static TFMonsterConfig GetMonsterConfig(int monsterId)
    {
        if (MonsterTableData.monsters == null) return null;
        foreach (var monster in MonsterTableData.monsters)
        {
            if (monster != null && monster.monsterId == monsterId)
                return monster;
        }
        return null;
    }

    public static TFWaveConfig GetWaveConfig(int dayNumber)
    {
        if (WaveTableData.waves == null) return null;
        foreach (var wave in WaveTableData.waves)
        {
            if (wave != null && wave.dayNumber == dayNumber)
                return wave;
        }
        return null;
    }

    public static int GetTotalMonsterCount(TFSpawnPointConfig spawnPoint)
    {
        if (spawnPoint == null || spawnPoint.actions == null) return 0;
        int total = 0;
        foreach (var action in spawnPoint.actions)
        {
            if (action != null) total += action.count;
        }
        return total;
    }

    public static string GetSpawnPointIconLabel(TFSpawnPointConfig spawnPoint)
    {
        if (spawnPoint == null || spawnPoint.actions == null || spawnPoint.actions.Length == 0)
            return "?";
        var firstAction = spawnPoint.actions[0];
        var config = GetMonsterConfig(firstAction.monsterId);
        return config != null ? config.iconLabel : "?";
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

    static TFBuildingTable CreateDefaultBuildingTable()
    {
        return new TFBuildingTable
        {
            buildings = new[]
            {
                new TFBuildingConfig { buildingId = 1, buildingName = "Arrow Tower", description = "Shoots arrows at nearby enemies", buildingType = "tower", coinCost = 50, maxHP = 200, atk = 15, def = 2, attackRange = 10f, attackInterval = 1.5f, arrowSpeed = 15f, arcHeight = 4f, upgradeIds = new[] { 10, 11 } },
                new TFBuildingConfig { buildingId = 2, buildingName = "Wall", description = "Blocks enemy path to the base", buildingType = "wall", coinCost = 30, maxHP = 400, def = 5, upgradeIds = new[] { 20 } },
                new TFBuildingConfig { buildingId = 3, buildingName = "Castle Center", description = "Your main base - protect it!", buildingType = "base", coinCost = 0, maxHP = 300, def = 3 },
                new TFBuildingConfig { buildingId = 4, buildingName = "House", description = "Produces gold each morning", buildingType = "economic", coinCost = 40, maxHP = 150, dailyYield = 15, upgradeIds = new[] { 40 } },
                new TFBuildingConfig { buildingId = 5, buildingName = "Spearman Barracks", description = "Recruit spearmen to fight for you", buildingType = "barracks", coinCost = 60, maxHP = 250, recruitCost = 20, maxRecruits = 3, allyUnitType = "spearman" },
                new TFBuildingConfig { buildingId = 6, buildingName = "Archer Barracks", description = "Recruit archers for ranged support", buildingType = "barracks", coinCost = 70, maxHP = 200, recruitCost = 25, maxRecruits = 2, allyUnitType = "archer" },
                new TFBuildingConfig { buildingId = 7, buildingName = "Knight Barracks", description = "Recruit knights with charge attack", buildingType = "barracks", coinCost = 80, maxHP = 300, recruitCost = 30, maxRecruits = 2, allyUnitType = "knight" },
                new TFBuildingConfig { buildingId = 10, buildingName = "Longbow Tower", description = "Extended range, powerful single target", buildingType = "tower", coinCost = 40, maxHP = 250, atk = 20, def = 2, attackRange = 15f, attackInterval = 2f, arrowSpeed = 18f, arcHeight = 5f, branchIcon = "LB" },
                new TFBuildingConfig { buildingId = 11, buildingName = "Fire Oil Tower", description = "Short range area damage", buildingType = "tower", coinCost = 40, maxHP = 250, atk = 25, def = 2, attackRange = 8f, attackInterval = 2.5f, arrowSpeed = 10f, arcHeight = 6f, aoeRadius = 3f, branchIcon = "FO" },
                new TFBuildingConfig { buildingId = 20, buildingName = "Fortified Wall", description = "Extremely tough barrier", buildingType = "wall", coinCost = 25, maxHP = 700, def = 8 },
                new TFBuildingConfig { buildingId = 40, buildingName = "Manor", description = "Produces more gold each morning", buildingType = "economic", coinCost = 30, maxHP = 200, dailyYield = 25 }
            }
        };
    }

    static TFAllyUnitTable CreateDefaultAllyUnitTable()
    {
        return new TFAllyUnitTable
        {
            units = new[]
            {
                new TFAllyUnitConfig { unitType = "spearman", unitName = "Spearman", maxHP = 80, atk = 12, def = 5, moveSpeed = 3f, attackRange = 1.8f, attackInterval = 1f, respawnTime = 15f },
                new TFAllyUnitConfig { unitType = "archer", unitName = "Archer", maxHP = 40, atk = 15, def = 1, moveSpeed = 3.5f, attackRange = 10f, attackInterval = 1.5f, respawnTime = 18f, arrowSpeed = 15f, arcHeight = 3f, kiteDistance = 7f },
                new TFAllyUnitConfig { unitType = "knight", unitName = "Knight", maxHP = 60, atk = 18, def = 3, moveSpeed = 4f, attackRange = 2f, attackInterval = 1.2f, respawnTime = 20f, chargeSpeed = 12f, chargeDuration = 0.6f, chargeMultiplier = 2f, chargeCooldown = 8f }
            }
        };
    }

    static TFMonsterTable CreateDefaultMonsterTable()
    {
        return new TFMonsterTable
        {
            monsters = new[]
            {
                new TFMonsterConfig { monsterId = 1, monsterName = "Goblin", maxHP = 30, atk = 8, def = 1, moveSpeed = 3.5f, attackRange = 1.5f, attackInterval = 1f, iconLabel = "G" },
                new TFMonsterConfig { monsterId = 2, monsterName = "Orc", maxHP = 80, atk = 15, def = 4, moveSpeed = 2f, attackRange = 2f, attackInterval = 1.5f, iconLabel = "O" },
                new TFMonsterConfig { monsterId = 3, monsterName = "Skeleton", maxHP = 50, atk = 12, def = 2, moveSpeed = 2.8f, attackRange = 1.5f, attackInterval = 1.2f, iconLabel = "S" }
            }
        };
    }

    static TFWaveTable CreateDefaultWaveTable()
    {
        return new TFWaveTable
        {
            dailyBaseCoins = 40,
            startingCoins = 100,
            waves = new[]
            {
                new TFWaveConfig
                {
                    dayNumber = 1,
                    spawnPoints = new[]
                    {
                        new TFSpawnPointConfig
                        {
                            position = new TFSerializedVector3 { x = 30, y = 0, z = 0 },
                            actions = new[] { new TFSpawnAction { monsterId = 1, count = 3, spawnInterval = 1.5f } }
                        }
                    }
                },
                new TFWaveConfig
                {
                    dayNumber = 2,
                    spawnPoints = new[]
                    {
                        new TFSpawnPointConfig
                        {
                            position = new TFSerializedVector3 { x = 30, y = 0, z = 0 },
                            actions = new[] { new TFSpawnAction { monsterId = 1, count = 5, spawnInterval = 1.2f } }
                        },
                        new TFSpawnPointConfig
                        {
                            position = new TFSerializedVector3 { x = -30, y = 0, z = 0 },
                            actions = new[] { new TFSpawnAction { monsterId = 1, count = 3, spawnInterval = 1.5f } }
                        }
                    }
                },
                new TFWaveConfig
                {
                    dayNumber = 3,
                    spawnPoints = new[]
                    {
                        new TFSpawnPointConfig
                        {
                            position = new TFSerializedVector3 { x = 30, y = 0, z = 0 },
                            actions = new[]
                            {
                                new TFSpawnAction { monsterId = 1, count = 4, spawnInterval = 1f },
                                new TFSpawnAction { monsterId = 2, count = 2, spawnInterval = 2f }
                            }
                        },
                        new TFSpawnPointConfig
                        {
                            position = new TFSerializedVector3 { x = 0, y = 0, z = 30 },
                            actions = new[] { new TFSpawnAction { monsterId = 3, count = 4, spawnInterval = 1.2f } }
                        }
                    }
                }
            }
        };
    }

    static TFHeroTable CreateDefaultHeroTable()
    {
        return new TFHeroTable
        {
            hero = new TFHeroConfig
            {
                maxHP = 100,
                moveSpeed = 8f,
                reviveTime = 10f,
                weapons = new[]
                {
                    new TFWeaponConfig
                    {
                        weaponId = "spear", weaponName = "Spear",
                        atk = 20, def = 3, attackRange = 2.5f, attackInterval = 0.8f,
                        skill = new TFSkillConfig
                        {
                            skillId = "thrust", cooldown = 8f, damageMultiplier = 1.5f,
                            accelTime = 0.5f, decelTime = 0.5f, maxSpeed = 20f
                        }
                    },
                    new TFWeaponConfig
                    {
                        weaponId = "bow", weaponName = "Bow",
                        atk = 12, def = 1, attackRange = 12f, attackInterval = 1.2f,
                        arrowSpeed = 15f, arcHeight = 4f,
                        skill = new TFSkillConfig
                        {
                            skillId = "volley", cooldown = 12f, damageMultiplier = 0.8f,
                            arrowCount = 5, spreadAngle = 60f, arrowSpeed = 15f, arcHeight = 5f
                        }
                    }
                }
            }
        };
    }
}
