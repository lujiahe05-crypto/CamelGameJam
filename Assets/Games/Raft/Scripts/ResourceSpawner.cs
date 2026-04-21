using UnityEngine;
using System.Collections.Generic;

public class ResourceSpawner : MonoBehaviour
{
    List<GameObject> activeResources = new List<GameObject>();
    float spawnTimer;

    void Update()
    {
        var config = RaftConfigTables.GetRefreshTable();
        float spawnInterval = Mathf.Max(0.1f, config.spawnInterval);

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0;
            CleanupFar();
            if (activeResources.Count < Mathf.Max(1, config.maxResources))
                SpawnResource();
        }
    }

    void SpawnResource()
    {
        var config = RaftConfigTables.GetRefreshTable();
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float minDist = Mathf.Max(0f, config.minSpawnDistance);
        float maxDist = Mathf.Max(minDist, config.maxSpawnDistance);
        float dist = Random.Range(minDist, maxDist);
        Vector3 pos = raftCenter + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
        pos.y = RaftGame.WaterLevel + 0.1f;

        var go = new GameObject("Resource");
        go.transform.position = pos;
        var res = go.AddComponent<FloatingResource>();

        res.Init(RaftConfigTables.RollRefreshResource());

        activeResources.Add(go);
    }

    void CleanupFar()
    {
        float despawnDist = Mathf.Max(0f, RaftConfigTables.GetRefreshTable().despawnDistance);
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        for (int i = activeResources.Count - 1; i >= 0; i--)
        {
            if (activeResources[i] == null)
            {
                activeResources.RemoveAt(i);
                continue;
            }
            float dist = Vector3.Distance(activeResources[i].transform.position, raftCenter);
            if (dist > despawnDist)
            {
                Destroy(activeResources[i]);
                activeResources.RemoveAt(i);
            }
        }
    }
}
