using UnityEngine;

public class SurvivalStats : MonoBehaviour
{
    public float Health { get; private set; } = 100f;
    public float Hunger { get; private set; } = 100f;
    public float Thirst { get; private set; } = 100f;
    public bool IsDead { get; private set; }

    public float MaxHealth { get; private set; } = 100f;
    public float MaxHunger { get; private set; } = 100f;
    public float MaxThirst { get; private set; } = 100f;

    // ========== Configurable rates ==========
    /// <summary>Hunger lost per second. Default: depletes in 180s.</summary>
    public float HungerRate = 100f / 180f;

    /// <summary>Thirst lost per second. Default: depletes in 120s.</summary>
    public float ThirstRate = 100f / 120f;

    /// <summary>HP damage per second when hunger or thirst is 0.</summary>
    public float StarveDamage = 5f;
    public float RespawnDelay = 3f;
    public float RespawnHealth = 100f;
    public float RespawnHunger = 80f;
    public float RespawnThirst = 80f;
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
        Invoke(nameof(Respawn), RespawnDelay);
    }

    void Respawn()
    {
        Health = Mathf.Clamp(RespawnHealth, 0, MaxHealth);
        Hunger = Mathf.Clamp(RespawnHunger, 0, MaxHunger);
        Thirst = Mathf.Clamp(RespawnThirst, 0, MaxThirst);
        IsDead = false;

        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        RaftGame.Instance.Player.transform.position = raftCenter + Vector3.up * 2f;
    }

    public void ApplyConfig(SurvivalTable config)
    {
        if (config == null) return;

        MaxHealth = Mathf.Max(1f, config.maxHealth);
        MaxHunger = Mathf.Max(1f, config.maxHunger);
        MaxThirst = Mathf.Max(1f, config.maxThirst);

        HungerRate = Mathf.Max(0f, config.hungerRate);
        ThirstRate = Mathf.Max(0f, config.thirstRate);
        StarveDamage = Mathf.Max(0f, config.starveDamage);
        RespawnDelay = Mathf.Max(0f, config.respawnDelay);
        RespawnHealth = Mathf.Clamp(config.respawnHealth, 0f, MaxHealth);
        RespawnHunger = Mathf.Clamp(config.respawnHunger, 0f, MaxHunger);
        RespawnThirst = Mathf.Clamp(config.respawnThirst, 0f, MaxThirst);

        Health = Mathf.Clamp(config.initialHealth, 0f, MaxHealth);
        Hunger = Mathf.Clamp(config.initialHunger, 0f, MaxHunger);
        Thirst = Mathf.Clamp(config.initialThirst, 0f, MaxThirst);
    }

    public void RestoreHunger(float amount)
    {
        Hunger = Mathf.Min(MaxHunger, Hunger + amount);
    }

    public void RestoreThirst(float amount)
    {
        Thirst = Mathf.Min(MaxThirst, Thirst + amount);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Max(0, Health - amount);
        if (Health <= 0) Die();
    }
}
