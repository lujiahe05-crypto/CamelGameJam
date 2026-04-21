using UnityEngine;
using UnityEngine.UI;

public class FloatingResource : MonoBehaviour
{
    public ResourceType Type { get; private set; }

    float bobOffset;
    float driftAngle;
    Vector3 driftDir;
    const float DriftSpeed = 0.5f;
    const float BobAmplitude = 0.15f;
    const float BobSpeed = 1.5f;

    // Name label
    Canvas labelCanvas;
    Text labelText;

    public void Init(ResourceType type)
    {
        Type = type;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        driftAngle = Random.Range(0f, 360f);
        driftDir = new Vector3(Mathf.Cos(driftAngle * Mathf.Deg2Rad), 0, Mathf.Sin(driftAngle * Mathf.Deg2Rad));

        // Visual
        var mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.AddComponent<MeshRenderer>();

        switch (type)
        {
            case ResourceType.Wood:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.WoodMat;
                transform.localScale = new Vector3(0.8f, 0.3f, 0.4f);
                break;
            case ResourceType.Plastic:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.PlasticMat;
                transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
                break;
            case ResourceType.Coconut:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.CoconutMat;
                transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                break;
            case ResourceType.Beet:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.BeetMat;
                transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                break;
            case ResourceType.WaterBottle:
                mf.mesh = RaftGame.Instance.CubeMesh;
                mr.material = RaftGame.Instance.WaterBottleMat;
                transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
                break;
        }

        // Collider for hook detection
        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 1f;
        col.isTrigger = true;

        // Create floating name label
        CreateNameLabel();
    }

    void CreateNameLabel()
    {
        string labelStr = GetChineseName(Type);

        var canvasGo = new GameObject("Label");
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = new Vector3(0, 2.5f, 0);
        canvasGo.transform.localScale = Vector3.one * 0.02f;

        labelCanvas = canvasGo.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingOrder = 100;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        labelText = textGo.AddComponent<Text>();
        labelText.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28);
        labelText.fontSize = 28;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.color = GetLabelColor(Type);
        labelText.text = labelStr;
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
        // Float on water surface with bobbing
        float waterY = Ocean.GetWaveHeight(transform.position.x, transform.position.z);
        float bob = Mathf.Sin(Time.time * BobSpeed + bobOffset) * BobAmplitude;
        transform.position = new Vector3(
            transform.position.x + driftDir.x * DriftSpeed * Time.deltaTime,
            waterY + bob + 0.1f,
            transform.position.z + driftDir.z * DriftSpeed * Time.deltaTime
        );

        // Slow rotation
        transform.Rotate(Vector3.up, 15f * Time.deltaTime);

        // Billboard: make label face camera
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

    public void Collect()
    {
        var game = RaftGame.Instance;
        // All resources go into inventory now (including coconut)
        game.Inv.Add(Type);
        Destroy(gameObject);
    }

    public static string GetChineseName(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return "\u6728\u6750";          // 木材
            case ResourceType.Plastic: return "\u5851\u6599";        // 塑料
            case ResourceType.Coconut: return "\u6930\u5b50";        // 椰子
            case ResourceType.Beet: return "\u751c\u83dc";           // 甜菜
            case ResourceType.WaterBottle: return "\u77ff\u6cc9\u6c34"; // 矿泉水
            default: return "";
        }
    }

    static Color GetLabelColor(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Wood: return new Color(0.9f, 0.7f, 0.4f);
            case ResourceType.Plastic: return new Color(0.9f, 0.9f, 1f);
            case ResourceType.Coconut: return new Color(0.5f, 1f, 0.5f);
            case ResourceType.Beet: return new Color(1f, 0.5f, 0.6f);
            case ResourceType.WaterBottle: return new Color(0.5f, 0.85f, 1f);
            default: return Color.white;
        }
    }
}
