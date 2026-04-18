using System;
using UnityEngine;

[Serializable]
public class TFBuildingNodeConfig
{
    public int nodeId;
    public string buildingName;
    public string description;
    public string statName;
    public int statBefore;
    public int statAfter;
    public int coinCost;
    public int maxHP;
    public int atk;
    public int def;
    public float attackRange;
    public float attackInterval;
}

[Serializable]
public class TFBuildingNodeTable
{
    public TFBuildingNodeConfig[] nodes;
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
    const string BuildingNodeTablePath = "ThronefallConfigs/BuildingNodeTable";
    const string MonsterTablePath = "ThronefallConfigs/MonsterTable";
    const string WaveTablePath = "ThronefallConfigs/WaveTable";
    const string HeroTablePath = "ThronefallConfigs/HeroTable";

    static TFBuildingNodeTable buildingNodeTable;
    static TFMonsterTable monsterTable;
    static TFWaveTable waveTable;
    static TFHeroTable heroTable;

    public static TFBuildingNodeTable BuildingNodeTableData =>
        buildingNodeTable ?? (buildingNodeTable = LoadTable(BuildingNodeTablePath, CreateDefaultBuildingNodeTable()));

    public static TFMonsterTable MonsterTableData =>
        monsterTable ?? (monsterTable = LoadTable(MonsterTablePath, CreateDefaultMonsterTable()));

    public static TFWaveTable WaveTableData =>
        waveTable ?? (waveTable = LoadTable(WaveTablePath, CreateDefaultWaveTable()));

    public static TFHeroTable HeroTableData =>
        heroTable ?? (heroTable = LoadTable(HeroTablePath, CreateDefaultHeroTable()));

    public static TFHeroConfig GetHeroConfig() => HeroTableData?.hero;

    public static void Reload()
    {
        buildingNodeTable = null;
        monsterTable = null;
        waveTable = null;
        heroTable = null;
    }

    public static TFBuildingNodeConfig GetBuildingNodeConfig(int nodeId)
    {
        if (BuildingNodeTableData.nodes == null) return null;
        foreach (var node in BuildingNodeTableData.nodes)
        {
            if (node != null && node.nodeId == nodeId)
                return node;
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

    static TFBuildingNodeTable CreateDefaultBuildingNodeTable()
    {
        return new TFBuildingNodeTable
        {
            nodes = new[]
            {
                new TFBuildingNodeConfig { nodeId = 1, buildingName = "Arrow Tower", description = "Shoots arrows at nearby enemies", statName = "ATK", statBefore = 0, statAfter = 15, coinCost = 50, maxHP = 200, atk = 15, def = 2, attackRange = 10f, attackInterval = 1.5f },
                new TFBuildingNodeConfig { nodeId = 2, buildingName = "Wall", description = "Blocks enemy path to the base", statName = "HP", statBefore = 0, statAfter = 400, coinCost = 30, maxHP = 400, atk = 0, def = 5, attackRange = 0f, attackInterval = 0f },
                new TFBuildingNodeConfig { nodeId = 3, buildingName = "Castle Center", description = "Your main base - protect it!", statName = "HP", statBefore = 100, statAfter = 300, coinCost = 0, maxHP = 300, atk = 0, def = 3, attackRange = 0f, attackInterval = 0f }
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
