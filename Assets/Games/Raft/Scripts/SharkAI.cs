using UnityEngine;
using UnityEngine.UI;

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

    // Name label
    Canvas labelCanvas;

    void Start()
    {
        CreateVisual();
        CreateNameLabel();
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

    void CreateNameLabel()
    {
        var canvasGo = new GameObject("SharkLabel");
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = new Vector3(0, 3f, 0);
        canvasGo.transform.localScale = Vector3.one * 0.025f;

        labelCanvas = canvasGo.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingOrder = 100;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 30);
        text.fontSize = 30;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = new Color(1f, 0.3f, 0.3f);
        text.text = "\u9ca8\u9c7c";  // 鲨鱼
        text.fontStyle = FontStyle.Bold;

        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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

        float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
        transform.position = new Vector3(transform.position.x, swimDepth + bob, transform.position.z);

        // Billboard label
        if (labelCanvas != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                labelCanvas.transform.rotation = Quaternion.LookRotation(
                    labelCanvas.transform.position - cam.transform.position);
            }
        }
    }

    void UpdatePatrol()
    {
        MoveToward(patrolTarget, PatrolSpeed);

        float distToTarget = Vector2Distance(transform.position, patrolTarget);
        if (distToTarget < 2f || stateTimer <= 0)
        {
            PickPatrolTarget();
        }

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
