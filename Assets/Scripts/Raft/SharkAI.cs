using UnityEngine;

public class SharkAI : MonoBehaviour
{
    enum State { Patrol, Charge, Bite, Flee }
    State state = State.Patrol;

    const float PatrolSpeed = 4f;
    const float ChargeSpeed = 8f;
    const float FleeSpeed = 6f;
    const float PatrolRadius = 25f;
    const float ChargeDistance = 20f;
    const float BiteDistance = 3f;
    const float BiteDamage = 35f;
    const float PatrolDuration = 8f;
    const float FleeDuration = 5f;
    const float ChargeCooldown = 15f;

    float stateTimer;
    float cooldownTimer;
    Vector3 patrolTarget;
    RaftBlock targetBlock;
    GameObject visual;
    float swimDepth = -1.5f;

    void Start()
    {
        CreateVisual();
        // Start at a distance
        Vector3 startPos = new Vector3(PatrolRadius, swimDepth, 0);
        transform.position = startPos;
        PickPatrolTarget();
        state = State.Patrol;
    }

    void CreateVisual()
    {
        var sharkMesh = ProceduralMeshUtil.CreateSharkMesh();
        visual = ProceduralMeshUtil.CreatePrimitive("SharkBody", sharkMesh, RaftGame.Instance.SharkMat, transform);
        visual.transform.localScale = Vector3.one * 2f;
        visual.transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;
        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Charge:
                UpdateCharge();
                break;
            case State.Bite:
                // Brief pause
                if (stateTimer <= 0)
                {
                    state = State.Flee;
                    stateTimer = FleeDuration;
                    PickFleeTarget();
                }
                break;
            case State.Flee:
                UpdateFlee();
                break;
        }

        // Bob slightly
        float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
        transform.position = new Vector3(transform.position.x, swimDepth + bob, transform.position.z);
    }

    void UpdatePatrol()
    {
        MoveToward(patrolTarget, PatrolSpeed);

        float distToTarget = Vector2Distance(transform.position, patrolTarget);
        if (distToTarget < 2f || stateTimer <= 0)
        {
            PickPatrolTarget();
        }

        // Try to charge if cooldown is ready
        if (cooldownTimer <= 0)
        {
            var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
            float distToRaft = Vector2Distance(transform.position, raftCenter);
            if (distToRaft < ChargeDistance)
            {
                targetBlock = RaftGame.Instance.RaftMgr.GetNearestEdgeBlock(transform.position);
                if (targetBlock != null)
                {
                    state = State.Charge;
                }
            }
        }
    }

    void UpdateCharge()
    {
        if (targetBlock == null)
        {
            state = State.Patrol;
            PickPatrolTarget();
            return;
        }

        Vector3 target = targetBlock.transform.position;
        MoveToward(target, ChargeSpeed);

        float dist = Vector2Distance(transform.position, target);
        if (dist < BiteDistance)
        {
            // Bite!
            targetBlock.TakeDamage(BiteDamage);
            state = State.Bite;
            stateTimer = 0.5f;
            cooldownTimer = ChargeCooldown;
        }
    }

    void UpdateFlee()
    {
        MoveToward(patrolTarget, FleeSpeed);
        if (stateTimer <= 0)
        {
            state = State.Patrol;
            PickPatrolTarget();
        }
    }

    void MoveToward(Vector3 target, float speed)
    {
        Vector3 dir = new Vector3(target.x - transform.position.x, 0, target.z - transform.position.z);
        if (dir.sqrMagnitude > 0.01f)
        {
            dir.Normalize();
            transform.position += dir * speed * Time.deltaTime;

            // Face movement direction
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }
    }

    void PickPatrolTarget()
    {
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(PatrolRadius * 0.6f, PatrolRadius);
        patrolTarget = raftCenter + new Vector3(Mathf.Cos(angle) * dist, swimDepth, Mathf.Sin(angle) * dist);
        stateTimer = PatrolDuration;
    }

    void PickFleeTarget()
    {
        var raftCenter = RaftGame.Instance.RaftMgr.GetCenter();
        Vector3 awayDir = (transform.position - raftCenter).normalized;
        patrolTarget = transform.position + awayDir * 20f;
        patrolTarget.y = swimDepth;
    }

    float Vector2Distance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
