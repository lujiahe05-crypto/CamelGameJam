using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ThronefallAlly : MonoBehaviour, ICombatEntity
{
    public enum CommandState { Stationed, Following }
    enum CombatState { Idle, Engaging, Attacking, Returning }
    enum LifeState { Alive, Dead }

    string unitType;
    int maxHP;
    int currentHP;
    int atk;
    int def;
    float moveSpeed;
    float attackRange;
    float attackInterval;
    float attackTimer;

    CommandState commandState = CommandState.Stationed;
    CombatState combatState = CombatState.Idle;
    LifeState lifeState = LifeState.Alive;

    Vector3 anchorPoint;
    int formationIndex;
    bool isSelected;

    ThronefallEnemy currentTarget;
    float retargetTimer;
    const float RetargetInterval = 0.4f;
    const float DeaggroDistance = 15f;

    // Archer
    float arrowSpeed, arcHeight, kiteDistance;

    // Knight
    float chargeSpeed, chargeDuration, chargeMultiplier, chargeCooldown;
    float chargeTimer;
    bool isCharging;
    float chargeCooldownTimer;
    Vector3 chargeDirection;
    HashSet<ICombatEntity> chargeHitTargets = new HashSet<ICombatEntity>();

    System.Action onDeath;
    NavMeshAgent agent;

    GameObject visual;
    Material visualMat;
    Color originalColor;
    GameObject selectionIndicator;
    ThronefallEntityHPBar hpBar;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => atk;
    public int DEF => def;
    public Vector3 Position => transform.position;
    public bool IsAlive => lifeState != LifeState.Dead;
    public bool IsSelected => isSelected;
    public string UnitType => unitType;

    public void Init(TFAllyUnitConfig unitConfig, Vector3 rally, System.Action onDeathCallback)
    {
        unitType = unitConfig.unitType;
        maxHP = unitConfig.maxHP;
        currentHP = maxHP;
        atk = unitConfig.atk;
        def = unitConfig.def;
        moveSpeed = unitConfig.moveSpeed;
        attackRange = unitConfig.attackRange;
        attackInterval = unitConfig.attackInterval;
        anchorPoint = rally;
        onDeath = onDeathCallback;

        arrowSpeed = unitConfig.arrowSpeed;
        arcHeight = unitConfig.arcHeight;
        kiteDistance = unitConfig.kiteDistance;

        chargeSpeed = unitConfig.chargeSpeed;
        chargeDuration = unitConfig.chargeDuration;
        chargeMultiplier = unitConfig.chargeMultiplier;
        chargeCooldown = unitConfig.chargeCooldown;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        Color visualColor;
        float scale;
        switch (unitType)
        {
            case "archer":
                visualColor = new Color(0.3f, 0.8f, 0.3f);
                scale = 0.6f;
                break;
            case "knight":
                visualColor = new Color(0.4f, 0.3f, 0.8f);
                scale = 0.9f;
                break;
            default:
                visualColor = new Color(0.2f, 0.7f, 0.6f);
                scale = 0.7f;
                break;
        }

        visualMat = ProceduralMeshUtil.CreateMaterial(visualColor);
        visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
        visual.transform.localPosition = new Vector3(0, scale * 0.5f, 0);
        visual.transform.localScale = new Vector3(scale, scale, scale);
        originalColor = visualColor;

        var col = gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0, scale * 0.5f, 0);
        col.size = new Vector3(scale, scale, scale);

        CreateSelectionIndicator(scale);

        game.CombatSys.RegisterEntity(this);

        agent = gameObject.AddComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.angularSpeed = 0;
        agent.acceleration = 50f;
        agent.stoppingDistance = 0.5f;
        agent.radius = 0.4f;
        agent.height = 1f;
        agent.updateRotation = false;
        agent.autoRepath = true;

        hpBar = gameObject.AddComponent<ThronefallEntityHPBar>();
        hpBar.Init(transform, scale + 0.3f, 80f);
    }

    void CreateSelectionIndicator(float unitScale)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        selectionIndicator = ProceduralMeshUtil.CreatePrimitive(
            "SelectionRing", game.CubeMesh,
            ProceduralMeshUtil.CreateMaterial(new Color(1f, 0.9f, 0.2f, 0.5f), true),
            transform);
        selectionIndicator.transform.localPosition = new Vector3(0, 0.05f, 0);
        selectionIndicator.transform.localScale = new Vector3(unitScale + 0.4f, 0.05f, unitScale + 0.4f);
        selectionIndicator.SetActive(false);
    }

    void Update()
    {
        if (lifeState == LifeState.Dead) return;
        var game = ThronefallGame.Instance;
        if (game == null) return;

        if (isCharging)
        {
            UpdateCharge();
            return;
        }

        if (chargeCooldownTimer > 0)
            chargeCooldownTimer -= Time.deltaTime;

        if (game.CurrentPhase == ThronefallGame.GamePhase.Night)
        {
            Vector3 anchor = GetCurrentAnchor();
            CombatAI(anchor);
        }
        else
        {
            if (commandState == CommandState.Following)
            {
                Vector3 heroTarget = GetFollowPosition();
                float dist = Vector3.Distance(Position, heroTarget);
                if (dist > 1.5f)
                    MoveToward(heroTarget);
            }
            else
            {
                ReturnToAnchor();
            }
            combatState = CombatState.Idle;
            currentTarget = null;
        }
    }

    Vector3 GetCurrentAnchor()
    {
        if (commandState == CommandState.Following)
        {
            return GetFollowPosition();
        }
        return anchorPoint;
    }

    Vector3 GetFollowPosition()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.Player == null) return anchorPoint;
        Vector3 offset = Quaternion.Euler(0, formationIndex * 45f, 0) * Vector3.back * 3f;
        return game.Player.Position + offset;
    }

    void CombatAI(Vector3 anchor)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        retargetTimer -= Time.deltaTime;

        switch (combatState)
        {
            case CombatState.Idle:
                if (retargetTimer <= 0)
                {
                    retargetTimer = RetargetInterval;
                    currentTarget = game.CombatSys.FindNearestEnemy(Position, attackRange * 2f);
                }
                if (currentTarget != null && currentTarget.IsAlive)
                {
                    combatState = CombatState.Engaging;
                }
                else
                {
                    ReturnToAnchorCombat(anchor);
                }
                break;

            case CombatState.Engaging:
                if (currentTarget == null || !currentTarget.IsAlive)
                {
                    combatState = CombatState.Idle;
                    currentTarget = null;
                    break;
                }

                float distToAnchor = Vector3.Distance(Position, anchor);
                if (distToAnchor > DeaggroDistance)
                {
                    combatState = CombatState.Returning;
                    currentTarget = null;
                    break;
                }

                float distToTarget = Vector3.Distance(Position, currentTarget.Position);
                if (distToTarget <= attackRange)
                {
                    combatState = CombatState.Attacking;
                    attackTimer = 0;
                    if (agent != null && agent.isOnNavMesh) agent.ResetPath();

                    if (unitType == "knight" && chargeCooldownTimer <= 0 && distToTarget > 1f)
                    {
                        Vector3 dir = (currentTarget.Position - Position).normalized;
                        StartCharge(dir);
                    }
                }
                else
                {
                    if (unitType == "archer" && kiteDistance > 0 && distToTarget < kiteDistance * 0.5f)
                    {
                        Vector3 awayDir = (Position - currentTarget.Position).normalized;
                        MoveToward(Position + awayDir * 2f);
                    }
                    else
                    {
                        MoveToward(currentTarget.Position);
                    }
                }
                break;

            case CombatState.Attacking:
                if (currentTarget == null || !currentTarget.IsAlive)
                {
                    combatState = CombatState.Idle;
                    currentTarget = null;
                    break;
                }

                float attackDist = Vector3.Distance(Position, currentTarget.Position);
                if (attackDist > attackRange * 1.3f)
                {
                    combatState = CombatState.Engaging;
                    break;
                }

                if (unitType == "archer" && kiteDistance > 0 && attackDist < kiteDistance * 0.4f)
                {
                    Vector3 awayDir = (Position - currentTarget.Position).normalized;
                    MoveToward(Position + awayDir * 2f);
                }

                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0)
                {
                    attackTimer = attackInterval;
                    DoAttack(currentTarget);
                }

                if (retargetTimer <= 0)
                {
                    retargetTimer = RetargetInterval;
                    var closer = game.CombatSys.FindNearestEnemy(Position, attackRange);
                    if (closer != null && closer != currentTarget)
                        currentTarget = closer;
                }
                break;

            case CombatState.Returning:
                float retDist = Vector3.Distance(Position, anchor);
                if (retDist < 3f)
                {
                    combatState = CombatState.Idle;
                }
                else
                {
                    MoveToward(anchor);
                }
                break;
        }
    }

    void DoAttack(ThronefallEnemy target)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        switch (unitType)
        {
            case "archer":
                int arrowDmg = ThronefallCombatSystem.CalculateDamage(atk, target.DEF);
                ThronefallArrowProjectile.Spawn(
                    Position + Vector3.up * 1f,
                    target.Position,
                    arrowSpeed, arcHeight, arrowDmg);
                break;

            case "knight":
                if (chargeCooldownTimer <= 0)
                {
                    Vector3 dir = (target.Position - Position).normalized;
                    StartCharge(dir);
                }
                else
                {
                    game.CombatSys.ApplyDamage(this, target);
                }
                break;

            default:
                game.CombatSys.ApplyDamage(this, target);
                break;
        }
    }

    void StartCharge(Vector3 dir)
    {
        if (agent != null) agent.enabled = false;
        isCharging = true;
        chargeTimer = 0;
        chargeDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
        chargeCooldownTimer = chargeCooldown;
        chargeHitTargets.Clear();
    }

    void UpdateCharge()
    {
        chargeTimer += Time.deltaTime;
        if (chargeTimer >= chargeDuration)
        {
            isCharging = false;
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(transform.position);
            }
            return;
        }

        transform.position += chargeDirection * chargeSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(chargeDirection);

        var colliders = Physics.OverlapSphere(Position + Vector3.up * 0.5f, 1.2f);
        foreach (var col in colliders)
        {
            var enemy = col.GetComponent<ThronefallEnemy>();
            if (enemy != null && enemy.IsAlive && !chargeHitTargets.Contains(enemy))
            {
                chargeHitTargets.Add(enemy);
                int dmg = Mathf.RoundToInt(
                    ThronefallCombatSystem.CalculateDamage(atk, enemy.DEF) * chargeMultiplier);
                if (dmg > 0)
                    enemy.TakeDamage(dmg);
            }
        }
    }

    void ReturnToAnchor()
    {
        float dist = Vector3.Distance(Position, anchorPoint);
        if (dist > 2f)
            MoveToward(anchorPoint);
    }

    void ReturnToAnchorCombat(Vector3 anchor)
    {
        float dist = Vector3.Distance(Position, anchor);
        if (dist > 2f)
            MoveToward(anchor);
    }

    void MoveToward(Vector3 target)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.SetDestination(target);
        UpdateRotation();
    }

    void UpdateRotation()
    {
        Vector3 vel = (agent != null && agent.enabled) ? agent.velocity : Vector3.zero;
        vel.y = 0;
        if (vel.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(vel),
                8f * Time.deltaTime);
        }
    }

    public void SetSelected(bool sel)
    {
        isSelected = sel;
        if (selectionIndicator != null)
            selectionIndicator.SetActive(sel);
    }

    public void SetFollowing(int formIdx)
    {
        commandState = CommandState.Following;
        formationIndex = formIdx;
        combatState = CombatState.Idle;
        currentTarget = null;
    }

    public void SetStationed(Vector3 pos)
    {
        commandState = CommandState.Stationed;
        anchorPoint = pos;
        combatState = CombatState.Idle;
        currentTarget = null;
    }

    public CommandState GetCommandState() => commandState;

    public void TakeDamage(int damage)
    {
        if (lifeState == LifeState.Dead) return;
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (visualMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            visualMat.color = Color.Lerp(Color.white, originalColor, ratio);
        }

        if (hpBar != null)
            hpBar.UpdateHP((float)currentHP / maxHP);

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (lifeState == LifeState.Dead) return;
        lifeState = LifeState.Dead;

        if (agent != null) agent.enabled = false;

        var game = ThronefallGame.Instance;
        if (game != null)
        {
            game.CombatSys.UnregisterEntity(this);
            if (game.CmdSys != null)
                game.CmdSys.UnregisterAlly(this);
        }

        onDeath?.Invoke();
        Destroy(gameObject, 0.1f);
    }
}
