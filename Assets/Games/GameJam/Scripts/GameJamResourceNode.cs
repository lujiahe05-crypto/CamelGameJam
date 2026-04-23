using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public struct GameJamHarvestReward
{
    public string itemId;
    public int amount;

    public GameJamHarvestReward(string itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = amount;
    }
}

public class GameJamResourceNode : MonoBehaviour
{
    [Header("基础配置")]
    public string resourceName = "石块";
    public int maxHp = 1;
    public int amount = 1;
    public int num;
    public PortiaResourceDropConfig[] drops;

    [Header("刷新配置 (-1 = 不刷新)")]
    public float respawnTime = -1f;

    int hp;
    bool alive = true;
    Vector3 originalScale;
    List<GameJamHarvestReward> cachedRewards;

    void Start()
    {
        hp = maxHp;
        originalScale = transform.localScale;
    }

    public bool IsAlive => alive;
    public int Hp => hp;
    public int MaxHp => maxHp;

    public bool Hit()
    {
        if (!alive) return false;
        hp--;
        StartCoroutine(HitFeedback());
        if (hp <= 0)
        {
            alive = false;
            return true;
        }
        return false;
    }

    public List<GameJamHarvestReward> PeekHarvestRewards()
    {
        if (cachedRewards == null)
            cachedRewards = GenerateRewards();
        return cachedRewards;
    }

    public List<GameJamHarvestReward> Harvest()
    {
        var result = new List<GameJamHarvestReward>(PeekHarvestRewards());
        OnDepleted();
        return result;
    }

    public void OnDepleted()
    {
        if (respawnTime >= 0)
        {
            SetVisible(false);
            cachedRewards = null;
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    List<GameJamHarvestReward> GenerateRewards()
    {
        int totalAmount = Mathf.Max(1, num > 0 ? num : amount);
        if (drops == null || drops.Length == 0)
            return new List<GameJamHarvestReward> { new GameJamHarvestReward(resourceName, totalAmount) };

        var validDrops = new List<PortiaResourceDropConfig>();
        foreach (var drop in drops)
        {
            if (drop == null || string.IsNullOrWhiteSpace(drop.itemId))
                continue;
            validDrops.Add(drop);
        }

        if (validDrops.Count == 0)
            return new List<GameJamHarvestReward> { new GameJamHarvestReward(resourceName, totalAmount) };

        float totalWeight = 0f;
        foreach (var drop in validDrops)
            totalWeight += Mathf.Max(0f, drop.weight);

        if (num > 0)
            return RollRewardsByTotalAmount(validDrops, totalAmount, totalWeight);

        var rolledDrop = RollWeightedDrop(validDrops, totalWeight);
        return new List<GameJamHarvestReward>
        {
            new GameJamHarvestReward(rolledDrop.itemId, Mathf.Max(1, rolledDrop.amount))
        };
    }

    static List<GameJamHarvestReward> RollRewardsByTotalAmount(
        List<PortiaResourceDropConfig> validDrops,
        int totalAmount,
        float totalWeight)
    {
        var counts = new Dictionary<string, int>();

        if (totalWeight <= 0f)
        {
            AddReward(counts, validDrops[0].itemId, totalAmount);
            return BuildRewardList(validDrops, counts);
        }

        for (int i = 0; i < totalAmount; i++)
        {
            var selectedDrop = RollWeightedDrop(validDrops, totalWeight);
            AddReward(counts, selectedDrop.itemId, 1);
        }

        return BuildRewardList(validDrops, counts);
    }

    static PortiaResourceDropConfig RollWeightedDrop(List<PortiaResourceDropConfig> validDrops, float totalWeight)
    {
        if (totalWeight <= 0f)
            return validDrops[0];

        float roll = Random.value * totalWeight;
        foreach (var drop in validDrops)
        {
            roll -= Mathf.Max(0f, drop.weight);
            if (roll <= 0f)
                return drop;
        }

        return validDrops[validDrops.Count - 1];
    }

    static void AddReward(Dictionary<string, int> counts, string itemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            return;

        if (counts.ContainsKey(itemId))
            counts[itemId] += amount;
        else
            counts[itemId] = amount;
    }

    static List<GameJamHarvestReward> BuildRewardList(
        List<PortiaResourceDropConfig> validDrops,
        Dictionary<string, int> counts)
    {
        var results = new List<GameJamHarvestReward>();
        var added = new HashSet<string>();
        foreach (var drop in validDrops)
        {
            if (!added.Add(drop.itemId))
                continue;

            if (!counts.TryGetValue(drop.itemId, out var rewardAmount) || rewardAmount <= 0)
                continue;

            results.Add(new GameJamHarvestReward(drop.itemId, rewardAmount));
        }

        return results;
    }

    IEnumerator HitFeedback()
    {
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * 60f) * 0.05f * (1f - t / 0.2f);
            transform.localScale = originalScale * (1f + shake);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        hp = maxHp;
        alive = true;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = visible;
    }
}
