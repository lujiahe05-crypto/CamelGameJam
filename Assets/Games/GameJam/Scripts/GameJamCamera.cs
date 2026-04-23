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

    float yaw;
    float pitch;
    float distance;
    bool initialized;

    void Start()
    {
        InitFromOffset();
    }

    void InitFromOffset()
    {
        distance = offset.magnitude;
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
            distance -= scroll * zoomSensitivity * distance * 0.5f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        var rot = Quaternion.Euler(pitch, yaw, 0);
        var dir = rot * Vector3.back;
        var lookTarget = target.position + Vector3.up * 1f;
        var desired = lookTarget + dir * distance;

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        transform.LookAt(lookTarget);
    }
}
