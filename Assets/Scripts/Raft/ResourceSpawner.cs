using UnityEngine;
using System.Collections.Generic;

public class ResourceSpawner : MonoBehaviour
{
    const int MaxResources = 20;
    const float SpawnInterval = 2.5f;
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

        // Spawn weights: 30% wood, 25% plastic, 15% coconut, 15% beet, 15% water bottle
        float roll = Random.value;
        ResourceType type;
        if (roll < 0.30f)
            type = ResourceType.Wood;
        else if (roll < 0.55f)
            type = ResourceType.Plastic;
        else if (roll < 0.70f)
            type = ResourceType.Coconut;
        else if (roll < 0.85f)
            type = ResourceType.Beet;
        else
            type = ResourceType.WaterBottle;

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
