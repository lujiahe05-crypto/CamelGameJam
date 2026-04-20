using UnityEngine;

public class ThronefallBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        transform.rotation = Quaternion.LookRotation(
            transform.position - cam.transform.position, cam.transform.up);
    }
}
