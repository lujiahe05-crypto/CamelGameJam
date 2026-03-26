using UnityEngine;

public class SurvivalStats : MonoBehaviour
{
    public float Health { get; private set; } = 100f;
    public float Hunger { get; private set; } = 100f;
    public float Thirst { get; private set; } = 100f;
    public bool IsDead { get; private set; }

    const float HungerRate = 100f / 180f;  // Depletes in 180 seconds
    const float ThirstRate = 100f / 120f;  // Depletes in 120 seconds
    const float StarveDamage = 5f;         // HP/sec when hunger or thirst is 0

    void Update()
    {
        if (IsDead) return;

        // Decrease hunger and thirst
        Hunger = Mathf.Max(0, Hunger - HungerRate * Time.deltaTime);
        Thirst = Mathf.Max(0, Thirst - ThirstRate * Time.deltaTime);

        // Damage when starving/dehydrated
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
        // Respawn after delay
        Invoke(nameof(Respawn), 3f);
    }

    void Respawn()
    {
        Health = 100f;
        Hunger = 80f;
        Thirst = 80f;
        IsDead = false;

        // Move player back to raft
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
