using UnityEngine;
using System.Collections.Generic;

public class ResourceSpawner : MonoBehaviour
{
    const int MaxResources = 15;
    const float SpawnInterval = 3f;
    const float MinDist = 15f;
    const float MaxDist = 40f;
    const float DespawnDist = 60f;

    List<GameObject> activeResources = new List<GameObject>();
    float spawnTimer;

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= SpawnInterval)
        {
            spawnTimer = 0;
            CleanupFar();
            if (activeResources.Count < MaxResources)
                SpawnResource();
        }
    }

    void SpawnResource()
    {
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(MinDist, MaxDist);
        Vector3 pos = raftCenter + new Vector3(Mathf.Cos(angle) * dist, 0, Mathf.Sin(angle) * dist);
        pos.y = RaftGame.WaterLevel + 0.1f;

        var go = new GameObject("Resource");
        go.transform.position = pos;
        var res = go.AddComponent<FloatingResource>();

        // Random type: 40% wood, 35% plastic, 25% coconut
        float roll = Random.value;
        ResourceType type = roll < 0.4f ? ResourceType.Wood
                          : roll < 0.75f ? ResourceType.Plastic
                          : ResourceType.Coconut;
        res.Init(type);

        activeResources.Add(go);
    }

    void CleanupFar()
    {
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        for (int i = activeResources.Count - 1; i >= 0; i--)
        {
            if (activeResources[i] == null)
            {
                activeResources.RemoveAt(i);
                continue;
            }
            float dist = Vector3.Distance(activeResources[i].transform.position, raftCenter);
            if (dist > DespawnDist)
            {
                Destroy(activeResources[i]);
                activeResources.RemoveAt(i);
            }
        }
    }
}
