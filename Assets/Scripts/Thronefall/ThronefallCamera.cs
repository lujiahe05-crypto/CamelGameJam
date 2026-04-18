using UnityEngine;

public class ThronefallCamera : MonoBehaviour
{
    Transform target;
    Vector3 offset = new Vector3(0, 20, -12);
    float smoothSpeed = 5f;

    public void Init(Transform playerTransform)
    {
        target = playerTransform;
        var cam = Camera.main;
        cam.transform.position = target.position + offset;
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
    }

    void LateUpdate()
    {
        if (target == null) return;
        var cam = Camera.main;
        Vector3 desired = target.position + offset;
        cam.transform.position = Vector3.Lerp(cam.transform.position, desired, smoothSpeed * Time.deltaTime);
        cam.transform.rotation = Quaternion.Euler(55, 0, 0);
    }
}
