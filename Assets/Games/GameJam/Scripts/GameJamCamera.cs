using UnityEngine;

public class GameJamCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 8, -6);
    public float smoothSpeed = 8f;
    public float dragSensitivity = 0.3f;
    public float zoomSensitivity = 2f;
    public float minDistance = 3f;
    public float maxDistance = 20f;
    public float minPitch = 10f;
    public float maxPitch = 80f;
    public float collisionRadius = 0.25f;
    public float collisionPadding = 0.1f;

    float yaw;
    float pitch;
    float distance;
    float baseDistance;
    float desiredDistance;
    bool initialized;
    bool useTeleportZoom;

    void Start()
    {
        InitFromOffset();
    }

    void InitFromOffset()
    {
        distance = offset.magnitude;
        baseDistance = distance;
        desiredDistance = distance;
        pitch = Mathf.Asin(offset.y / distance) * Mathf.Rad2Deg;
        yaw = Mathf.Atan2(-offset.x, -offset.z) * Mathf.Rad2Deg;
        initialized = true;
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (!initialized) InitFromOffset();

        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * dragSensitivity * 10f;
            pitch -= Input.GetAxis("Mouse Y") * dragSensitivity * 10f;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            desiredDistance -= scroll * zoomSensitivity * distance * 0.5f;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
            baseDistance = desiredDistance;
            useTeleportZoom = false;
        }

        if (useTeleportZoom && HasMovementInput())
        {
            useTeleportZoom = false;
            desiredDistance = baseDistance;
        }

        distance = Mathf.Lerp(distance, desiredDistance, smoothSpeed * Time.deltaTime);
        var rot = Quaternion.Euler(pitch, yaw, 0);
        var dir = rot * Vector3.back;
        var lookTarget = target.position + Vector3.up * 1f;
        var desired = lookTarget + dir * distance;
        desired = ResolveCameraCollision(lookTarget, desired);

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(lookTarget);
    }

    public void ApplyTeleportZoom(float distanceScale)
    {
        if (!initialized)
            InitFromOffset();

        baseDistance = Mathf.Clamp(baseDistance, minDistance, maxDistance);
        desiredDistance = Mathf.Clamp(baseDistance * distanceScale, minDistance, maxDistance);
        distance = desiredDistance;
        useTeleportZoom = true;
    }

    public bool IsCloseToDesiredPose(float positionThreshold = 0.08f, float distanceThreshold = 0.08f)
    {
        if (target == null)
            return true;
        if (!initialized)
            InitFromOffset();

        var rot = Quaternion.Euler(pitch, yaw, 0);
        var dir = rot * Vector3.back;
        var lookTarget = target.position + Vector3.up * 1f;
        var desired = lookTarget + dir * desiredDistance;
        desired = ResolveCameraCollision(lookTarget, desired);

        bool positionReady = Vector3.Distance(transform.position, desired) <= positionThreshold;
        bool distanceReady = Mathf.Abs(distance - desiredDistance) <= distanceThreshold;
        return positionReady && distanceReady;
    }

    bool HasMovementInput()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.01f ||
               Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.01f;
    }

    Vector3 ResolveCameraCollision(Vector3 lookTarget, Vector3 desiredPosition)
    {
        var toDesired = desiredPosition - lookTarget;
        float castDistance = toDesired.magnitude;
        if (castDistance <= 0.001f)
            return desiredPosition;

        var direction = toDesired / castDistance;
        var hits = Physics.SphereCastAll(lookTarget, collisionRadius, direction, castDistance, ~0, QueryTriggerInteraction.Ignore);
        float nearestDistance = castDistance;
        bool blocked = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider == null)
                continue;
            if (target != null && hit.collider.transform.IsChildOf(target))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                blocked = true;
            }
        }

        if (!blocked)
            return desiredPosition;

        float safeDistance = Mathf.Max(0.2f, nearestDistance - collisionPadding);
        return lookTarget + direction * safeDistance;
    }
}
