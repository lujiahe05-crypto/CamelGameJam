using UnityEngine;
using System.Collections;

public class GameJamGroundPickup : MonoBehaviour
{
    [Header("道具配置")]
    public string itemId = "木材";
    public string itemName = "木材";
    public int pickupAmount = 1;

    [Header("交互配置")]
    public float interactRange = 2.5f;

    [Header("刷新配置 (-1 = 永不刷新)")]
    public float respawnTime = 30f;

    [Header("反馈 (可选)")]
    public AudioClip pickupSound;

    bool available = true;
    GameObject modelRoot;

    void Awake()
    {
        modelRoot = gameObject;
    }

    public bool CanPickup() => available;

    public float GetInteractRange() => interactRange;

    public (string itemId, string itemName, int amount) DoPickup()
    {
        if (!available) return (null, null, 0);
        available = false;

        var fx = GetComponent<GameJamPickupFX>();
        if (fx != null) fx.Play(transform.position);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        SetModelVisible(false);

        if (respawnTime >= 0)
            StartCoroutine(RespawnRoutine());

        return (itemId, itemName, pickupAmount);
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        available = true;
        SetModelVisible(true);
    }

    void SetModelVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = visible;
    }
}
