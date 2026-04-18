using UnityEngine;

public class ThronefallEnemy : MonoBehaviour, ICombatEntity
{
    int maxHP;
    int currentHP;
    int atk;
    int def;
    float moveSpeed;
    float attackRange;
    float attackInterval;
    float attackTimer;
    int monsterId;

    enum State { Moving, Attacking, Dead }
    State state = State.Moving;

    ICombatEntity currentTarget;
    float retargetTimer;
    const float RetargetInterval = 0.5f;

    GameObject visual;
    Material visualMat;
    Color originalColor;

    ThronefallWaveSystem waveSystem;

    // ICombatEntity
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => atk;
    public int DEF => def;
    public Vector3 Position => transform.position;
    public bool IsAlive => state != State.Dead;

    public void Init(TFMonsterConfig config, ThronefallWaveSystem waveSys)
    {
        monsterId = config.monsterId;
        maxHP = config.maxHP;
        currentHP = config.maxHP;
        atk = config.atk;
        def = config.def;
        moveSpeed = config.moveSpeed;
        attackRange = config.attackRange;
        attackInterval = config.attackInterval;
        waveSystem = waveSys;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        // Visual based on monster type
        Material mat;
        float scale;
        switch (config.monsterId)
        {
            case 2: // Orc - larger, green
                mat = game.EnemyOrcMat;
                scale = 1.4f;
                break;
            case 3: // Skeleton - white/bone
                mat = game.EnemySkeletonMat;
                scale = 1.0f;
                break;
            default: // Goblin - small, red
                mat = game.EnemyMat;
                scale = 0.8f;
                break;
        }

        visualMat = new Material(mat);
        visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
        visual.transform.localPosition = new Vector3(0, scale * 0.5f, 0);
        visual.transform.localScale = new Vector3(scale, scale, scale);
        originalColor = visualMat.color;

        var col = gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, scale * 0.5f, 0);
        col.size = new Vector3(scale, scale, scale);

        game.CombatSys.RegisterEntity(this);
    }

    void Update()
    {
        if (state == State.Dead) return;
        var game = ThronefallGame.Instance;
        if (game == null) return;

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0)
        {
            retargetTimer = RetargetInterval;
            EvaluateTarget();
        }

        if (currentTarget == null || !currentTarget.IsAlive)
        {
            EvaluateTarget();
            if (currentTarget == null) return;
        }

        float dist = Vector3.Distance(Position, currentTarget.Position);

        switch (state)
        {
            case State.Moving:
                if (dist <= attackRange)
                {
                    state = State.Attacking;
                    attackTimer = 0;
                }
                else
                {
                    MoveToward(currentTarget.Position);
                }
                break;

            case State.Attacking:
                if (!currentTarget.IsAlive)
                {
                    state = State.Moving;
                    EvaluateTarget();
                    break;
                }

                if (dist > attackRange * 1.5f)
                {
                    state = State.Moving;
                    break;
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    attackTimer = attackInterval;
                    game.CombatSys.ApplyDamage(this, currentTarget);
                }
                break;
        }
    }

    void EvaluateTarget()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var mainBase = game.CombatSys.GetMainBase();
        if (mainBase == null || !mainBase.IsAlive)
        {
            currentTarget = null;
            return;
        }

        // Raycast toward base to detect walls
        Vector3 dirToBase = (mainBase.Position - Position).normalized;
        if (Physics.Raycast(Position + Vector3.up * 0.5f, dirToBase, out RaycastHit hit, 4f))
        {
            var building = hit.collider.GetComponent<ThronefallBuilding>();
            if (building != null && building.IsAlive && !building.IsMainBase)
            {
                currentTarget = building;
                return;
            }
        }

        currentTarget = mainBase;
    }

    void MoveToward(Vector3 target)
    {
        Vector3 dir = target - Position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) return;

        dir.Normalize();
        transform.position += dir * moveSpeed * Time.deltaTime;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            8f * Time.deltaTime);
    }

    public void TakeDamage(int damage)
    {
        if (state == State.Dead) return;
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        // Flash
        if (visualMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            visualMat.color = Color.Lerp(new Color(1f, 1f, 1f), originalColor, ratio);
        }

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (state == State.Dead) return;
        state = State.Dead;

        var game = ThronefallGame.Instance;
        if (game != null)
            game.CombatSys.UnregisterEntity(this);

        if (waveSystem != null)
            waveSystem.OnEnemyDied(this);

        Destroy(gameObject, 0.1f);
    }
}
