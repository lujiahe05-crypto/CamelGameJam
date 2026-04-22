using UnityEngine;

public class GameJamPickupFX : MonoBehaviour
{
    [Header("特效配置")]
    public Color flashColor = new Color(1f, 1f, 0.7f, 0.8f);
    public float flashDuration = 0.3f;
    public float flashRadius = 0.5f;

    public void Play(Vector3 position)
    {
        var go = new GameObject("PickupFlash");
        go.transform.position = position + Vector3.up * 0.5f;

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = flashColor;
        light.intensity = 2f;
        light.range = flashRadius * 3f;

        var fader = go.AddComponent<PickupFlashFader>();
        fader.duration = flashDuration;
    }
}

public class PickupFlashFader : MonoBehaviour
{
    public float duration = 0.3f;
    float timer;
    Light flashLight;

    void Start()
    {
        flashLight = GetComponent<Light>();
        timer = duration;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
            return;
        }
        if (flashLight != null)
            flashLight.intensity = 2f * (timer / duration);
    }
}
