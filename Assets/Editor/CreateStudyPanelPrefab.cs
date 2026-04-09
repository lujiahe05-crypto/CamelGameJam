using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CreateStudyPanelPrefab
{
    [InitializeOnLoadMethod]
    static void AutoCreate()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/StudyPanel.prefab") == null)
            Create();
    }

    public static void Create()
    {
        EnsureFolder();

        // Root: 200x300 brown panel
        var root = CreateUIObject("StudyPanel", null);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(200, 300);
        AddImage(root.gameObject, new Color(0.42f, 0.33f, 0.22f, 0.95f));
        root.gameObject.AddComponent<CanvasGroup>();
        root.gameObject.SetActive(false);

        // Title: "把物品放进槽位来研究"
        var title = CreateUIObject("Title", root.transform);
        title.anchorMin = new Vector2(0, 1);
        title.anchorMax = new Vector2(1, 1);
        title.pivot = new Vector2(0.5f, 1);
        title.anchoredPosition = new Vector2(0, -4);
        title.sizeDelta = new Vector2(-8, 26);
        AddText(title.gameObject, "\u628A\u7269\u54C1\u653E\u8FDB\u69FD\u4F4D\u6765\u7814\u7A76", 12, new Color(1, 0.95f, 0.85f));

        // ResearchArea
        var resArea = CreateUIObject("ResearchArea", root.transform);
        resArea.anchorMin = new Vector2(0, 1);
        resArea.anchorMax = new Vector2(1, 1);
        resArea.pivot = new Vector2(0.5f, 1);
        resArea.anchoredPosition = new Vector2(0, -32);
        resArea.sizeDelta = new Vector2(-8, 52);

        // InputSlot (left)
        var slot = CreateUIObject("InputSlot", resArea.transform);
        slot.anchorMin = new Vector2(0, 0.5f);
        slot.anchorMax = new Vector2(0, 0.5f);
        slot.pivot = new Vector2(0, 0.5f);
        slot.anchoredPosition = new Vector2(4, 0);
        slot.sizeDelta = new Vector2(46, 46);
        var slotImg = AddImage(slot.gameObject, new Color(0.5f, 0.42f, 0.3f, 0.85f));
        var slotBtn = slot.gameObject.AddComponent<Button>();
        slotBtn.targetGraphic = slotImg;

        // InputSlot Icon
        var slotIcon = CreateUIObject("Icon", slot.transform);
        slotIcon.anchorMin = new Vector2(0.1f, 0.15f);
        slotIcon.anchorMax = new Vector2(0.9f, 0.85f);
        slotIcon.offsetMin = Vector2.zero;
        slotIcon.offsetMax = Vector2.zero;
        AddImage(slotIcon.gameObject, Color.clear, false);

        // InputSlot Count
        var slotCount = CreateUIObject("Count", slot.transform);
        slotCount.anchorMin = Vector2.zero;
        slotCount.anchorMax = Vector2.one;
        slotCount.offsetMin = new Vector2(2, 2);
        slotCount.offsetMax = new Vector2(-2, -2);
        AddText(slotCount.gameObject, "", 10, Color.white, TextAnchor.LowerLeft);

        // ResearchBtn (green)
        var resBtn = CreateUIObject("ResearchBtn", resArea.transform);
        resBtn.anchorMin = new Vector2(0, 0.5f);
        resBtn.anchorMax = new Vector2(0, 0.5f);
        resBtn.pivot = new Vector2(0, 0.5f);
        resBtn.anchoredPosition = new Vector2(56, 0);
        resBtn.sizeDelta = new Vector2(68, 30);
        var resBtnImg = AddImage(resBtn.gameObject, new Color(0.55f, 0.62f, 0.32f));
        var resBtnComp = resBtn.gameObject.AddComponent<Button>();
        resBtnComp.targetGraphic = resBtnImg;
        var resBtnColors = resBtnComp.colors;
        resBtnColors.normalColor = new Color(0.55f, 0.62f, 0.32f);
        resBtnColors.highlightedColor = new Color(0.62f, 0.7f, 0.38f);
        resBtnColors.pressedColor = new Color(0.68f, 0.76f, 0.42f);
        resBtnColors.disabledColor = new Color(0.35f, 0.35f, 0.3f, 0.6f);
        resBtnComp.colors = resBtnColors;

        var resBtnLabel = CreateUIObject("Label", resBtn.transform);
        resBtnLabel.anchorMin = Vector2.zero;
        resBtnLabel.anchorMax = Vector2.one;
        resBtnLabel.sizeDelta = Vector2.zero;
        AddText(resBtnLabel.gameObject, "\u7814\u7A76", 14, Color.white);

        // RecipeScroll
        var recipeScroll = CreateUIObject("RecipeScroll", root.transform);
        recipeScroll.anchorMin = new Vector2(0, 0);
        recipeScroll.anchorMax = new Vector2(1, 1);
        recipeScroll.offsetMin = new Vector2(4, 4);
        recipeScroll.offsetMax = new Vector2(-4, -88);

        AddImage(recipeScroll.gameObject, new Color(0.38f, 0.3f, 0.2f, 0.5f), false);
        var scrollRect = recipeScroll.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateUIObject("Viewport", recipeScroll.transform);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.sizeDelta = Vector2.zero;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        AddImage(viewport.gameObject, Color.white, false);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        var content = CreateUIObject("Content", viewport.transform);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = new Vector2(0, 0);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.padding = new RectOffset(2, 2, 2, 2);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = content;
        scrollRect.viewport = viewport;

        // RecipeRow_Template
        CreateRecipeRowTemplate(content.transform);

        // Save
        string path = "Assets/Resources/UI/StudyPanel.prefab";
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, path);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.Refresh();
        Debug.Log("StudyPanel prefab created at " + path);
    }

    static void CreateRecipeRowTemplate(Transform parent)
    {
        var row = CreateUIObject("RecipeRow_Template", parent);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 42;
        AddImage(row.gameObject, new Color(0.48f, 0.4f, 0.28f, 0.6f));

        // ItemIcon (left)
        var icon = CreateUIObject("ItemIcon", row.transform);
        icon.anchorMin = new Vector2(0, 0.5f);
        icon.anchorMax = new Vector2(0, 0.5f);
        icon.pivot = new Vector2(0, 0.5f);
        icon.anchoredPosition = new Vector2(3, 0);
        icon.sizeDelta = new Vector2(36, 36);
        AddImage(icon.gameObject, Color.clear, false);

        // Materials container (middle)
        var mats = CreateUIObject("Materials", row.transform);
        mats.anchorMin = new Vector2(0, 0);
        mats.anchorMax = new Vector2(1, 1);
        mats.offsetMin = new Vector2(42, 3);
        mats.offsetMax = new Vector2(-64, -3);
        var hlg = mats.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 2;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        for (int i = 0; i < 4; i++)
        {
            var mat = CreateUIObject("Mat_" + i, mats.transform);
            var matLE = mat.gameObject.AddComponent<LayoutElement>();
            matLE.preferredWidth = 28;
            AddImage(mat.gameObject, new Color(0.6f, 0.52f, 0.38f, 0.8f), false);

            var matIcon = CreateUIObject("Icon", mat.transform);
            matIcon.anchorMin = new Vector2(0.1f, 0.1f);
            matIcon.anchorMax = new Vector2(0.9f, 0.9f);
            matIcon.offsetMin = Vector2.zero;
            matIcon.offsetMax = Vector2.zero;
            AddImage(matIcon.gameObject, Color.clear, false);
        }

        // LearnBtn (right)
        var learnBtn = CreateUIObject("LearnBtn", row.transform);
        learnBtn.anchorMin = new Vector2(1, 0.5f);
        learnBtn.anchorMax = new Vector2(1, 0.5f);
        learnBtn.pivot = new Vector2(1, 0.5f);
        learnBtn.anchoredPosition = new Vector2(-3, 0);
        learnBtn.sizeDelta = new Vector2(56, 28);
        var learnImg = AddImage(learnBtn.gameObject, new Color(0.52f, 0.44f, 0.32f, 0.9f));
        var learnBtnComp = learnBtn.gameObject.AddComponent<Button>();
        learnBtnComp.targetGraphic = learnImg;
        var learnColors = learnBtnComp.colors;
        learnColors.normalColor = new Color(0.52f, 0.44f, 0.32f, 0.9f);
        learnColors.highlightedColor = new Color(0.6f, 0.52f, 0.38f);
        learnColors.pressedColor = new Color(0.65f, 0.56f, 0.42f);
        learnColors.disabledColor = new Color(0.35f, 0.35f, 0.3f, 0.6f);
        learnBtnComp.colors = learnColors;

        var learnLabel = CreateUIObject("Label", learnBtn.transform);
        learnLabel.anchorMin = Vector2.zero;
        learnLabel.anchorMax = Vector2.one;
        learnLabel.sizeDelta = Vector2.zero;
        AddText(learnLabel.gameObject, "\u5B66\u4E60", 12, Color.white);

        row.gameObject.SetActive(false);
    }

    // --- Helpers ---

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }
    }

    static RectTransform CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = 5;
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static Font GetBuiltinFont()
    {
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
            font = AssetDatabase.GetBuiltinExtraResource<Font>("Arial.ttf");
        return font;
    }

    static Text AddText(GameObject go, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        go.AddComponent<CanvasRenderer>();
        var text = go.AddComponent<Text>();
        text.font = GetBuiltinFont();
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1, -1);
        return text;
    }

    static Image AddImage(GameObject go, Color color, bool raycastTarget = true)
    {
        go.AddComponent<CanvasRenderer>();
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycastTarget;
        return img;
    }
}
