using UnityEngine;

public class GameJamResourceNode : MonoBehaviour
{
    public string resourceName = "石块";
    public int amount = 1;

    public (string name, int amount) Harvest()
    {
        var result = (resourceName, amount);
        Destroy(gameObject);
        return result;
    }
}
