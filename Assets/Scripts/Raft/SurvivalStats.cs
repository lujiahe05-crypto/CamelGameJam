using UnityEngine;

public class SurvivalStats : MonoBehaviour
{
    public float Health { get; private set; } = 100f;
    public float Hunger { get; private set; } = 100f;
    public float Thirst { get; private set; } = 100f;
    public bool IsDead { get; private set; }

    // ========== Configurable rates ==========
    /// <summary>Hunger lost per second. Default: depletes in 180s.</summary>
    public float HungerRate = 100f / 180f;

    /// <summary>Thirst lost per second. Default: depletes in 120s.</summary>
    public float ThirstRate = 100f / 120f;

    /// <summary>HP damage per second when hunger or thirst is 0.</summary>
    public float StarveDamage = 5f;
    // =========================================

    void Update()
    {
        if (IsDead) return;

        Hunger = Mathf.Max(0, Hunger - HungerRate * Time.deltaTime);
        Thirst = Mathf.Max(0, Thirst - ThirstRate * Time.deltaTime);

        if (Hunger <= 0 || Thirst <= 0)
        {
            Health -= StarveDamage * Time.deltaTime;
            if (Health <= 0)
            {
                Health = 0;
                Die();
            }
        }
    }

    void Die()
    {
        IsDead = true;
        Invoke(nameof(Respawn), 3f);
    }

    void Respawn()
    {
        Health = 100f;
        Hunger = 80f;
        Thirst = 80f;
        IsDead = false;

        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        RaftGame.Instance.Player.transform.position = raftCenter + Vector3.up * 2f;
    }

    public void RestoreHunger(float amount)
    {
        Hunger = Mathf.Min(100f, Hunger + amount);
    }

    public void RestoreThirst(float amount)
    {
        Thirst = Mathf.Min(100f, Thirst + amount);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0, Health - amount);
        if (Health <= 0) Die();
    }
}
