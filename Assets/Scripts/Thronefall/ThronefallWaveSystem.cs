using System.Collections.Generic;
using UnityEngine;

public class ThronefallWaveSystem : MonoBehaviour
{
    class SpawnProcess
    {
        public Vector3 spawnPosition;
        public TFSpawnAction[] actions;
        public int currentActionIndex;
        public int spawnedInCurrentAction;
        public float timer;
        public bool complete;
    }

    List<ThronefallEnemy> aliveEnemies = new List<ThronefallEnemy>();
    List<SpawnProcess> activeProcesses = new List<SpawnProcess>();
    bool isSpawning;
    bool allSpawned;

    public void StartWave(TFWaveConfig waveConfig)
    {
        aliveEnemies.Clear();
        activeProcesses.Clear();
        isSpawning = true;
        allSpawned = false;

        if (waveConfig == null || waveConfig.spawnPoints == null) return;

        foreach (var sp in waveConfig.spawnPoints)
        {
            if (sp == null || sp.actions == null || sp.actions.Length == 0) continue;

            var process = new SpawnProcess
            {
                spawnPosition = sp.position.ToVector3(),
                actions = sp.actions,
                currentActionIndex = 0,
                spawnedInCurrentAction = 0,
                timer = 0.5f, // small delay before first spawn
                complete = false
            };
            activeProcesses.Add(process);
        }
    }

    void Update()
    {
        if (!isSpawning) return;

        bool anyActive = false;
        foreach (var process in activeProcesses)
        {
            if (process.complete) continue;
            anyActive = true;

            process.timer -= Time.deltaTime;
            if (process.timer > 0) continue;

            var action = process.actions[process.currentActionIndex];
            SpawnEnemy(action.monsterId, process.spawnPosition);
            process.spawnedInCurrentAction++;

            if (process.spawnedInCurrentAction >= action.count)
            {
                process.currentActionIndex++;
                process.spawnedInCurrentAction = 0;

                if (process.currentActionIndex >= process.actions.Length)
                {
                    process.complete = true;
                    continue;
                }

                process.timer = process.actions[process.currentActionIndex].spawnInterval;
            }
            else
            {
                process.timer = action.spawnInterval;
            }
        }

        if (!anyActive && !allSpawned)
        {
            allSpawned = true;
        }

        // Clean up null references
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
                aliveEnemies.RemoveAt(i);
        }

        if (allSpawned && aliveEnemies.Count == 0)
        {
            isSpawning = false;
            var game = ThronefallGame.Instance;
            if (game != null)
                game.OnAllMonstersDead();
        }
    }

    void SpawnEnemy(int monsterId, Vector3 position)
    {
        var config = ThronefallConfigTables.GetMonsterConfig(monsterId);
        if (config == null) return;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        var go = new GameObject($"Enemy_{config.monsterName}");
        go.transform.SetParent(game.RootContainer);
        // Slight random offset to prevent stacking
        Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        go.transform.position = position + offset;

        var enemy = go.AddComponent<ThronefallEnemy>();
        enemy.Init(config, this);
        aliveEnemies.Add(enemy);
    }

    public void OnEnemyDied(ThronefallEnemy enemy)
    {
        aliveEnemies.Remove(enemy);
    }

    public int GetAliveEnemyCount()
    {
        return aliveEnemies.Count;
    }
}
