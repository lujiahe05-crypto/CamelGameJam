using UnityEngine;

public class HookThrower : MonoBehaviour
{
    const float ThrowForce = 25f;
    const float MaxDist = 30f;
    const float RetractSpeed = 20f;

    enum HookState { Ready, Flying, Retracting }
    HookState state = HookState.Ready;

    GameObject hookObj;
    LineRenderer line;
    Rigidbody hookRb;
    FloatingResource caughtResource;
    Vector3 hookStartPos;

    void Start()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = 0.03f;
        line.endWidth = 0.03f;
        line.material = new Material(Shader.Find("Unlit/Color"));
        line.material.color = new Color(0.6f, 0.5f, 0.3f);
        line.positionCount = 2;
        line.enabled = false;
    }

    void Update()
    {
        if (RaftUI.IsUIOpen) return;

        // Only allow throwing when Hook is the selected item
        bool hookSelected = RaftGame.Instance.Inv.GetSelectedItemType() == ItemType.Hook;

        switch (state)
        {
            case HookState.Ready:
                if (hookSelected && Input.GetMouseButtonDown(0))
                    ThrowHook();
                break;

            case HookState.Flying:
                UpdateFlying();
                break;

            case HookState.Retracting:
                UpdateRetracting();
                break;
        }

        UpdateLine();
    }

    void ThrowHook()
    {
        var cam = Camera.main;
        Vector3 throwDir = cam.transform.forward;
        hookStartPos = cam.transform.position + cam.transform.forward * 0.5f + cam.transform.up * -0.3f;

        hookObj = new GameObject("Hook");
        var mf = hookObj.AddComponent<MeshFilter>();
        mf.mesh = RaftGame.Instance.CubeMesh;
        var mr = hookObj.AddComponent<MeshRenderer>();
        mr.material = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.5f, 0.5f));
        hookObj.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        hookObj.transform.position = hookStartPos;

        hookRb = hookObj.AddComponent<Rigidbody>();
        hookRb.mass = 0.5f;
        hookRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        hookRb.velocity = throwDir * ThrowForce;

        var col = hookObj.AddComponent<SphereCollider>();
        col.radius = 1f;
        col.isTrigger = true;
        var detector = hookObj.AddComponent<HookCollisionDetector>();
        detector.thrower = this;

        state = HookState.Flying;
        line.enabled = true;
        caughtResource = null;
    }

    void UpdateFlying()
    {
        if (hookObj == null)
        {
            ResetHook();
            return;
        }

        float dist = Vector3.Distance(transform.position, hookObj.transform.position);
        if (dist > MaxDist || hookObj.transform.position.y < RaftGame.WaterLevel - 2f)
        {
            StartRetract();
        }
    }

    void UpdateRetracting()
    {
        if (hookObj == null)
        {
            ResetHook();
            return;
        }

        Vector3 handPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f;
        Vector3 dir = (handPos - hookObj.transform.position).normalized;
        hookObj.transform.position += dir * RetractSpeed * Time.deltaTime;

        if (caughtResource != null)
            caughtResource.transform.position = hookObj.transform.position;

        float dist = Vector3.Distance(handPos, hookObj.transform.position);
        if (dist < 1.5f)
        {
            if (caughtResource != null)
                caughtResource.Collect();

            ResetHook();
        }
    }

    void StartRetract()
    {
        if (hookRb != null)
        {
            Destroy(hookRb);
            hookRb = null;
        }
        state = HookState.Retracting;
    }

    void ResetHook()
    {
        if (hookObj != null) Destroy(hookObj);
        hookObj = null;
        hookRb = null;
        caughtResource = null;
        state = HookState.Ready;
        line.enabled = false;
    }

    public void OnHookHitResource(FloatingResource resource)
    {
        if (state != HookState.Flying || caughtResource != null) return;
        caughtResource = resource;
        StartRetract();
    }

    void UpdateLine()
    {
        if (!line.enabled || hookObj == null) return;
        Vector3 handPos = Camera.main.transform.position + Camera.main.transform.forward * 0.5f + Camera.main.transform.up * -0.3f;
        line.SetPosition(0, handPos);
        line.SetPosition(1, hookObj.transform.position);
    }
}

public class HookCollisionDetector : MonoBehaviour
{
    public HookThrower thrower;

    void OnTriggerEnter(Collider other)
    {
        var resource = other.GetComponent<FloatingResource>();
        if (resource != null && thrower != null)
        {
            thrower.OnHookHitResource(resource);
        }
    }
}
