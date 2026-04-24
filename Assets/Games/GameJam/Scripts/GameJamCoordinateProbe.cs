using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class GameJamCoordinateProbe : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F8;
    public KeyCode copyHitKey = KeyCode.F9;
    public KeyCode copyPlayerKey = KeyCode.F10;
    public LayerMask raycastMask = Physics.DefaultRaycastLayers;
    public float raycastDistance = 500f;

    Transform target;
    GameObject canvasGo;
    GameObject panelGo;
    Text infoText;
    bool visible = true;
    Vector3 currentHitPoint;
    bool hasHitPoint;

    public void Init(Transform target)
    {
        this.target = target;
    }

    void Start()
    {
        // BuildUI();
        // RefreshVisibility();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
            RefreshVisibility();
        }

        UpdateHitPoint();
        UpdateText();

        if (Input.GetKeyDown(copyHitKey))
            CopyCurrentPoint(hasHitPoint ? currentHitPoint : target != null ? target.position : Vector3.zero, "已复制命中点坐标");

        if (Input.GetKeyDown(copyPlayerKey))
            CopyCurrentPoint(target != null ? target.position : Vector3.zero, "已复制玩家坐标");
    }

    void BuildUI()
    {
        canvasGo = new GameObject("CoordinateProbeCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 140;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);

        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);

        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(520f, 220f);
        rect.anchoredPosition = new Vector2(-16f, -16f);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(panelGo.transform, false);
        infoText = txtGo.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 20;
        infoText.alignment = TextAnchor.UpperLeft;
        infoText.color = Color.white;
        infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
        infoText.verticalOverflow = VerticalWrapMode.Overflow;

        var txtRect = txtGo.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(16f, 16f);
        txtRect.offsetMax = new Vector2(-16f, -16f);
    }

    void UpdateHitPoint()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            hasHitPoint = false;
            return;
        }

        var ray = cam.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, raycastDistance, raycastMask, QueryTriggerInteraction.Ignore))
        {
            currentHitPoint = hit.point;
            hasHitPoint = true;
            return;
        }

        var groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out var enter))
        {
            currentHitPoint = ray.GetPoint(enter);
            hasHitPoint = true;
            return;
        }

        hasHitPoint = false;
    }

    void UpdateText()
    {
        if (infoText == null)
            return;

        Vector3 playerPos = target != null ? target.position : Vector3.zero;
        string hitPosText = hasHitPoint ? FormatVector(currentHitPoint) : "无";
        string hitConfigText = hasHitPoint ? FormatConfigVector(currentHitPoint) : "无";

        infoText.text =
            "坐标探针\n" +
            $"玩家坐标: {FormatVector(playerPos)}\n" +
            $"玩家配置: {FormatConfigVector(playerPos)}\n\n" +
            $"屏幕中心命中点: {hitPosText}\n" +
            $"命中点配置: {hitConfigText}\n\n" +
            $"[{toggleKey}] 显示/隐藏  [{copyHitKey}] 复制命中点  [{copyPlayerKey}] 复制玩家坐标";
    }

    void RefreshVisibility()
    {
        if (panelGo != null)
            panelGo.SetActive(visible);
    }

    void CopyCurrentPoint(Vector3 point, string toast)
    {
        GUIUtility.systemCopyBuffer = FormatConfigVector(point);
        Toast.ShowToast(toast);
    }

    static string FormatVector(Vector3 value)
    {
        return $"({FormatNumber(value.x)}, {FormatNumber(value.y)}, {FormatNumber(value.z)})";
    }

    static string FormatConfigVector(Vector3 value)
    {
        return $"new PortiaVector3Data {{ x = {FormatNumber(value.x)}f, y = {FormatNumber(value.y)}f, z = {FormatNumber(value.z)}f }}";
    }

    static string FormatNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    void OnDestroy()
    {
        if (canvasGo != null)
            Destroy(canvasGo);
    }
}
