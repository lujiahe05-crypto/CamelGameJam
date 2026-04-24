using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameJamGroundPickup : MonoBehaviour
{
    [Header("道具配置")]
    public string itemId = "木材";
    public string itemName = "木材";
    public int pickupAmount = 1;

    [Header("交互配置")]
    public float interactRange = 2.5f;

    [Header("刷新配置 (-1 = 永不刷新)")]
    public float respawnTime = 30f;

    [Header("自动消失 (秒, <=0 不消失)")]
    public float autoDestroyTime = -1f;

    [Header("反馈 (可选)")]
    public AudioClip pickupSound;

    bool available = true;
    GameObject modelRoot;
    Canvas labelCanvas;

    void Awake()
    {
        modelRoot = gameObject;
    }

    void Start()
    {
        CreateNameLabel();
        if (autoDestroyTime > 0f)
            StartCoroutine(AutoDestroyRoutine());
    }

    void CreateNameLabel()
    {
        var canvasGo = new GameObject("Label");
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = new Vector3(0, 1.2f, 0);
        canvasGo.transform.localScale = Vector3.one * 0.02f;

        labelCanvas = canvasGo.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingOrder = 100;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var labelText = textGo.AddComponent<Text>();
        labelText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28);
        labelText.fontSize = 28;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.color = new Color(1f, 0.95f, 0.6f);
        labelText.text = itemName;
        labelText.fontStyle = FontStyle.Bold;

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
        if (labelCanvas != null && Camera.main != null)
            labelCanvas.transform.rotation = Camera.main.transform.rotation;
    }

    public bool CanPickup() => available;

    public float GetInteractRange() => interactRange;

    public (string itemId, string itemName, int amount) DoPickup()
    {
        if (!available) return (null, null, 0);
        available = false;

        var fx = GetComponent<GameJamPickupFX>();
        if (fx != null) fx.Play(transform.position);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        SetModelVisible(false);

        if (respawnTime >= 0)
            StartCoroutine(RespawnRoutine());

        return (itemId, itemName, pickupAmount);
    }

    IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);
        available = true;
        SetModelVisible(true);
    }

    IEnumerator AutoDestroyRoutine()
    {
        yield return new WaitForSeconds(autoDestroyTime);
        if (available)
            Destroy(gameObject);
    }

    void SetModelVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = visible;
        if (labelCanvas != null)
            labelCanvas.gameObject.SetActive(visible);
    }
}
