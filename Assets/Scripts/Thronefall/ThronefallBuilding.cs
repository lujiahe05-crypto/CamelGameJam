using UnityEngine;

public class ThronefallBuilding : MonoBehaviour, ICombatEntity
{
    int maxHP;
    int currentHP;
    int atk;
    int def;
    float attackRange;
    float attackInterval;
    float attackTimer;
    bool isTower;
    bool isMainBase;
    int nodeId;

    GameObject visual;
    Material visualMat;
    Color originalColor;

    // ICombatEntity
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => atk;
    public int DEF => def;
    public Vector3 Position => transform.position;
    public bool IsAlive => currentHP > 0;
    public bool IsMainBase => isMainBase;

    public void Init(TFBuildingNodeConfig config, bool isBase)
    {
        nodeId = config.nodeId;
        maxHP = config.maxHP;
        currentHP = config.maxHP;
        atk = config.atk;
        def = config.def;
        attackRange = config.attackRange;
        attackInterval = config.attackInterval;
        isTower = config.attackRange > 0 && config.atk > 0;
        isMainBase = isBase;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        // Create visual based on building type
        if (isBase)
        {
            visualMat = new Material(game.BaseMat);
            visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
            visual.transform.localPosition = new Vector3(0, 1.5f, 0);
            visual.transform.localScale = new Vector3(3f, 3f, 3f);
        }
        else if (isTower)
        {
            visualMat = new Material(game.TowerMat);
            visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
            visual.transform.localPosition = new Vector3(0, 2f, 0);
            visual.transform.localScale = new Vector3(1.2f, 4f, 1.2f);
        }
        else
        {
            // Wall
            visualMat = new Material(game.WallMat);
            visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.transform.localScale = new Vector3(3f, 2f, 1f);
        }

        originalColor = visualMat.color;

        var col = gameObject.AddComponent<BoxCollider>();
        col.center = visual.transform.localPosition;
        col.size = visual.transform.localScale;

        game.CombatSys.RegisterEntity(this);
        if (isBase)
            game.CombatSys.SetMainBase(this);
    }

    void Update()
    {
        if (!IsAlive) return;
        if (!isTower) return;

        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Night) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0) return;

        var target = game.CombatSys.FindNearestEnemy(Position, attackRange);
        if (target == null) return;

        attackTimer = attackInterval;
        game.CombatSys.ApplyDamage(this, target);

        // Visual projectile effect
        ShowProjectile(target.Position);
    }

    void ShowProjectile(Vector3 targetPos)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var projGo = ProceduralMeshUtil.CreatePrimitive("Projectile", game.CubeMesh, game.ProjectileMat, null);
        projGo.transform.position = transform.position + Vector3.up * 3.5f;
        projGo.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        var proj = projGo.AddComponent<SimpleProjectile>();
        proj.target = targetPos + Vector3.up;
        proj.speed = 20f;
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        // Flash red
        if (visualMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            visualMat.color = Color.Lerp(Color.red, originalColor, ratio);
        }

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        var game = ThronefallGame.Instance;
        if (game != null)
        {
            game.CombatSys.UnregisterEntity(this);
            if (isMainBase)
                game.OnBaseDead();
        }
        Destroy(gameObject);
    }
}

public class SimpleProjectile : MonoBehaviour
{
    public Vector3 target;
    public float speed = 20f;

    void Update()
    {
        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude < 0.5f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += dir.normalized * speed * Time.deltaTime;
    }
}
