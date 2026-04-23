using UnityEngine;

public class GameJamResourceNode : MonoBehaviour
{
    public string resourceName = "石块";
    public int amount = 1;
    public PortiaResourceDropConfig[] drops;

    public (string name, int amount) Harvest()
    {
        var result = RollDrop();
        Destroy(gameObject);
        return result;
    }

    (string name, int amount) RollDrop()
    {
        if (drops == null || drops.Length == 0)
            return (resourceName, amount);

        float totalWeight = 0f;
        foreach (var drop in drops)
        {
            if (drop == null || string.IsNullOrWhiteSpace(drop.itemId))
                continue;

            totalWeight += Mathf.Max(0f, drop.weight);
        }

        if (totalWeight <= 0f)
        {
            foreach (var drop in drops)
            {
                if (drop != null && !string.IsNullOrWhiteSpace(drop.itemId))
                    return (drop.itemId, Mathf.Max(1, drop.amount));
            }

            return (resourceName, amount);
        }

        float roll = Random.value * totalWeight;
        foreach (var drop in drops)
        {
            if (drop == null || string.IsNullOrWhiteSpace(drop.itemId))
                continue;

            roll -= Mathf.Max(0f, drop.weight);
            if (roll <= 0f)
                return (drop.itemId, Mathf.Max(1, drop.amount));
        }

        var last = drops[drops.Length - 1];
        return last != null && !string.IsNullOrWhiteSpace(last.itemId)
            ? (last.itemId, Mathf.Max(1, last.amount))
            : (resourceName, amount);
    }
}
