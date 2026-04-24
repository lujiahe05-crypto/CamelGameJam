using UnityEngine;
using UnityEngine.UI;

public class GameJamBuildPlaceUI : MonoBehaviour
{
    const string KeyBgTexturePath = "Assets/Games/GameJam/assets/UI/Texture2D/button_cannot.png";

    GameObject canvasGo;
    GameObject panelGo;

    public void Init()
    {
    }

    void EnsureUI()
    {
        if (canvasGo == null)
        {
            BuildUI();
            panelGo.SetActive(false);
        }
    }

    void BuildUI()
    {
        canvasGo = GameJamUIPrefabHelper.TryLoadPrefab("BuildPlacePanel");
        if (canvasGo != null)
        {
            FindReferences();
            ApplyKeyBackgrounds();
            return;
        }

        canvasGo = new GameObject("BuildPlacePanel");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        panelGo = MakeRect("HintPanel", canvasGo.transform);
        var panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(200f, 100f);
        panelRect.anchoredPosition = new Vector2(32f, 32f);

        CreateHintRow(panelGo.transform, 50f, "T", "旋转");
        CreateHintRow(panelGo.transform, 6f, "LMB", "放下");
        GameJamUIPrefabHelper.SavePrefab(canvasGo, "BuildPlacePanel");
    }

    void FindReferences()
    {
        panelGo = canvasGo.transform.Find("HintPanel").gameObject;
    }

    void ApplyKeyBackgrounds()
    {
        if (panelGo == null)
            return;

        var images = panelGo.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            var image = images[i];
            if (image != null && image.gameObject.name == "KeyBG")
                ApplyKeyBackground(image);
        }
    }

    static void ApplyKeyBackground(Image image)
    {
        if (image == null)
            return;

        var sprite = GameJamArtLoader.LoadSprite(KeyBgTexturePath);
        if (sprite != null)
            image.sprite = sprite;

        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
    }

    void CreateHintRow(Transform parent, float y, string keyLabel, string actionLabel)
    {
        var row = MakeRect("Row", parent);
        var rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(0f, 0f);
        rowRect.sizeDelta = new Vector2(0, 40f);
        rowRect.anchoredPosition = new Vector2(0, y);

        var keyBg = MakeRect("KeyBG", row.transform);
        var kbRect = keyBg.GetComponent<RectTransform>();
        kbRect.anchorMin = new Vector2(0f, 0.5f);
        kbRect.anchorMax = new Vector2(0f, 0.5f);
        kbRect.pivot = new Vector2(0f, 0.5f);
        kbRect.sizeDelta = new Vector2(42f, 36f);
        kbRect.anchoredPosition = new Vector2(0, 0);

        var kbImg = keyBg.AddComponent<Image>();
        ApplyKeyBackground(kbImg);

        var outline = MakeRect("Outline", keyBg.transform);
        var olRect = outline.GetComponent<RectTransform>();
        olRect.anchorMin = Vector2.zero;
        olRect.anchorMax = Vector2.one;
        olRect.sizeDelta = Vector2.zero;
        var olImg = outline.AddComponent<Outline>();
        var olImgBase = outline.AddComponent<Image>();
        olImgBase.color = Color.clear;
        olImg.effectColor = new Color(0.7f, 0.72f, 0.75f);
        olImg.effectDistance = new Vector2(1.5f, -1.5f);

        var keyText = MakeRect("KeyText", keyBg.transform);
        var ktRect = keyText.GetComponent<RectTransform>();
        ktRect.anchorMin = Vector2.zero;
        ktRect.anchorMax = Vector2.one;
        ktRect.sizeDelta = Vector2.zero;
        var kt = keyText.AddComponent<Text>();
        kt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        kt.fontSize = keyLabel.Length > 1 ? 14 : 20;
        kt.fontStyle = FontStyle.Bold;
        kt.alignment = TextAnchor.MiddleCenter;
        kt.color = new Color(0.25f, 0.28f, 0.3f);
        kt.text = keyLabel;

        var label = MakeRect("Label", row.transform);
        var lRect = label.GetComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0f, 0.5f);
        lRect.anchorMax = new Vector2(0f, 0.5f);
        lRect.pivot = new Vector2(0f, 0.5f);
        lRect.sizeDelta = new Vector2(100f, 36f);
        lRect.anchoredPosition = new Vector2(52f, 0);
        var lt = label.AddComponent<Text>();
        lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        lt.fontSize = 22;
        lt.alignment = TextAnchor.MiddleLeft;
        lt.color = new Color(0.9f, 0.92f, 0.95f);
        lt.text = actionLabel;
        var ltOutline = label.AddComponent<Outline>();
        ltOutline.effectColor = new Color(0, 0, 0, 0.5f);
        ltOutline.effectDistance = new Vector2(1, -1);
    }

    public void Show()
    {
        EnsureUI();
        panelGo.SetActive(true);
    }

    public void Hide()
    {
        if (panelGo != null) panelGo.SetActive(false);
    }

    public void Cleanup()
    {
        if (canvasGo != null) Destroy(canvasGo);
    }

    void OnDestroy() => Cleanup();

    static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return go;
    }
}
