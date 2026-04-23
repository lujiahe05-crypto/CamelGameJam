using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public struct GameJamDrop
{
    public string itemId;
    public int amount;
    [Range(0f, 1f)]
    public float chance;
}

public class GameJamResourceNode : MonoBehaviour
{
    [Header("基础配置")]
    public string resourceName = "石块";
    public int maxHp = 3;

    [Header("掉落配置")]
    public GameJamDrop[] drops;

    [Header("刷新配置 (-1 = 不刷新)")]
    public float respawnTime = -1f;

    int hp;
    bool alive = true;
    Vector3 originalScale;

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

    public List<(string itemId, int amount)> GetDrops()
    {
        var result = new List<(string, int)>();
        if (drops == null) return result;
        foreach (var drop in drops)
        {
            if (Random.value <= drop.chance)
                result.Add((drop.itemId, drop.amount));
        }
        if (result.Count == 0 && drops.Length > 0)
            result.Add((drops[0].itemId, drops[0].amount));
        return result;
    }

    public void OnDepleted()
    {
        if (respawnTime >= 0)
        {
            SetVisible(false);
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
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
