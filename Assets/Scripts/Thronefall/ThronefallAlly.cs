using UnityEngine;

public class ThronefallAlly : MonoBehaviour, ICombatEntity
{
    int maxHP;
    int currentHP;
    int atk;
    int def;
    float moveSpeed;
    float attackRange;
    float attackInterval;
    float attackTimer;
    Vector3 rallyPoint;

    enum State { Idle, Chasing, Attacking, Dead }
    State state = State.Idle;

    ThronefallEnemy currentTarget;
    float retargetTimer;
    const float RetargetInterval = 0.4f;

    GameObject visual;
    Material visualMat;
    Color originalColor;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => atk;
    public int DEF => def;
    public Vector3 Position => transform.position;
    public bool IsAlive => state != State.Dead;

    public void Init(TFBuildingConfig barracksConfig, Vector3 rally)
    {
        maxHP = barracksConfig.allyMaxHP > 0 ? barracksConfig.allyMaxHP : 50;
        currentHP = maxHP;
        atk = barracksConfig.allyAtk > 0 ? barracksConfig.allyAtk : 10;
        def = barracksConfig.allyDef;
        moveSpeed = barracksConfig.allyMoveSpeed > 0 ? barracksConfig.allyMoveSpeed : 3f;
        attackRange = barracksConfig.allyAttackRange > 0 ? barracksConfig.allyAttackRange : 1.8f;
        attackInterval = barracksConfig.allyAttackInterval > 0 ? barracksConfig.allyAttackInterval : 1f;
        rallyPoint = rally;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        visualMat = ProceduralMeshUtil.CreateMaterial(new Color(0.2f, 0.7f, 0.6f));
        visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
        visual.transform.localPosition = new Vector3(0, 0.35f, 0);
        visual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        originalColor = visualMat.color;

        var col = gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.35f, 0);
        col.size = new Vector3(0.7f, 0.7f, 0.7f);

        game.CombatSys.RegisterEntity(this);
    }

    void Update()
    {
        if (state == State.Dead) return;
        var game = ThronefallGame.Instance;
        if (game == null) return;
        if (game.CurrentPhase != ThronefallGame.GamePhase.Night)
        {
            ReturnToRally();
            return;
        }

        retargetTimer -= Time.deltaTime;
        if (retargetTimer <= 0)
        {
            retargetTimer = RetargetInterval;
            currentTarget = game.CombatSys.FindNearestEnemy(Position, attackRange * 2f);
        }

        if (currentTarget == null || !currentTarget.IsAlive)
        {
            currentTarget = game.CombatSys.FindNearestEnemy(Position, attackRange * 2f);
            if (currentTarget == null)
            {
                state = State.Idle;
                ReturnToRally();
                return;
            }
        }

        float dist = Vector3.Distance(Position, currentTarget.Position);

        if (dist <= attackRange)
        {
            state = State.Attacking;
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                attackTimer = attackInterval;
                game.CombatSys.ApplyDamage(this, currentTarget);
            }
        }
        else
        {
            state = State.Chasing;
            MoveToward(currentTarget.Position);
        }
    }

    void ReturnToRally()
    {
        float dist = Vector3.Distance(Position, rallyPoint);
        if (dist > 2f)
            MoveToward(rallyPoint);
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

        if (visualMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            visualMat.color = Color.Lerp(Color.white, originalColor, ratio);
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

        Destroy(gameObject, 0.1f);
    }
}
