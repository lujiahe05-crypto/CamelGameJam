using UnityEngine;
using UnityEngine.UI;

public class GameJamWorldLabel : MonoBehaviour
{
    [SerializeField] string labelText = "标签";
    [SerializeField] float yOffset = 0f;
    [SerializeField] Color textColor = new Color(1f, 1f, 0.55f);

    Canvas labelCanvas;

    void Start()
    {
        CreateLabel();
    }

    void CreateLabel()
    {
        var canvasGo = new GameObject("Label");
        canvasGo.transform.SetParent(transform);
        canvasGo.transform.localPosition = new Vector3(0, yOffset, 0);
        canvasGo.transform.localScale = Vector3.one * 0.02f;

        labelCanvas = canvasGo.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;
        labelCanvas.sortingOrder = 100;

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 28);
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = textColor;
        text.text = labelText;
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
        if (labelCanvas != null && Camera.main != null)
            labelCanvas.transform.rotation = Camera.main.transform.rotation;
    }
}
