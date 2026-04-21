using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RaftCraftingUI : MonoBehaviour
{
    GameObject panel;
    Button closeBtn;
    Button craftBtn;
    Text recipeNameText;
    Transform costListParent;
    GameObject costItemTemplate;
    Transform recipeContentParent;
    GameObject recipeItemTemplate;

    Button[] tabButtons;
    string[] categories = { "survival", "storage", "tool" };
    string currentCategory;

    SynthesisRecipe selectedRecipe;
    List<GameObject> recipeInstances = new List<GameObject>();
    List<GameObject> costInstances = new List<GameObject>();

    bool isOpen;

    void Start()
    {
        var prefab = Resources.Load<GameObject>("UI/CraftingPanel");
        panel = Instantiate(prefab, transform, false);
        var t = panel.transform;

        closeBtn = t.Find("CloseBtn").GetComponent<Button>();
        closeBtn.onClick.AddListener(Close);

        craftBtn = t.Find("DetailPanel/CraftBtn").GetComponent<Button>();
        craftBtn.onClick.AddListener(OnCraft);

        recipeNameText = t.Find("DetailPanel/RecipeName").GetComponent<Text>();

        costListParent = t.Find("DetailPanel/CostList");
        costItemTemplate = costListParent.Find("CostItem_Template").gameObject;

        recipeContentParent = t.Find("RecipeScroll/Viewport/Content");
        recipeItemTemplate = recipeContentParent.Find("RecipeItem_Template").gameObject;

        // Tab buttons
        tabButtons = new Button[3];
        tabButtons[0] = t.Find("CategoryBar/Tab_survival").GetComponent<Button>();
        tabButtons[1] = t.Find("CategoryBar/Tab_storage").GetComponent<Button>();
        tabButtons[2] = t.Find("CategoryBar/Tab_tool").GetComponent<Button>();

        tabButtons[0].onClick.AddListener(() => SwitchCategory("survival"));
        tabButtons[1].onClick.AddListener(() => SwitchCategory("storage"));
        tabButtons[2].onClick.AddListener(() => SwitchCategory("tool"));

        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) Close();
            else Open();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();

        if (isOpen)
            RefreshCraftButton();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        panel.SetActive(true);
        RaftUI.IsUIOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SwitchCategory("survival");
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        RaftUI.IsUIOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void SwitchCategory(string category)
    {
        currentCategory = category;
        selectedRecipe = null;
        recipeNameText.text = "";

        // Highlight active tab
        for (int i = 0; i < categories.Length; i++)
        {
            var img = tabButtons[i].GetComponent<Image>();
            img.color = (categories[i] == category)
                ? new Color(0.6f, 0.45f, 0.25f)
                : new Color(0.45f, 0.32f, 0.18f);
        }

        RefreshRecipeList();
        ClearCostList();
    }

    void RefreshRecipeList()
    {
        // Clear old
        foreach (var go in recipeInstances)
            Destroy(go);
        recipeInstances.Clear();

        var recipes = RaftConfigTables.GetRecipesByCategory(currentCategory);
        var inv = RaftGame.Instance.Inv;

        foreach (var recipe in recipes)
        {
            var go = Instantiate(recipeItemTemplate, recipeContentParent);
            go.SetActive(true);
            go.transform.Find("Name").GetComponent<Text>().text = recipe.displayName;

            // Set icon color based on output item
            var iconImg = go.transform.Find("Icon").GetComponent<Image>();
            if (RaftConfigTables.TryGetItemTypeById(recipe.outputItemTypeId, out var outType))
                iconImg.color = Inventory.GetItemColor(outType);
            else
                iconImg.color = Color.gray;

            bool canAfford = RaftConfigTables.CanAffordRecipe(inv, recipe);
            var statusText = go.transform.Find("Status").GetComponent<Text>();
            statusText.text = canAfford ? "\u2714" : "";
            statusText.color = canAfford ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);

            var captured = recipe;
            go.GetComponent<Button>().onClick.AddListener(() => SelectRecipe(captured));
            recipeInstances.Add(go);
        }
    }

    void SelectRecipe(SynthesisRecipe recipe)
    {
        selectedRecipe = recipe;
        recipeNameText.text = recipe.displayName;
        RefreshCostList();
        RefreshCraftButton();
    }

    void RefreshCostList()
    {
        ClearCostList();
        if (selectedRecipe == null || selectedRecipe.inputs == null) return;

        var inv = RaftGame.Instance.Inv;
        foreach (var input in selectedRecipe.inputs)
        {
            if (input == null) continue;
            var go = Instantiate(costItemTemplate, costListParent);
            go.SetActive(true);

            string itemName = RaftConfigTables.GetItemDisplayName(input.itemTypeId);
            go.transform.Find("Name").GetComponent<Text>().text = itemName;

            // Set icon color for cost material
            var iconImg = go.transform.Find("Icon").GetComponent<Image>();
            if (RaftConfigTables.TryGetItemTypeById(input.itemTypeId, out var itemType))
            {
                iconImg.color = Inventory.GetItemColor(itemType);
                int owned = inv.GetCount(itemType);
                var amountText = go.transform.Find("Amount").GetComponent<Text>();
                amountText.text = $"{owned}/{input.amount}";
                amountText.color = owned >= input.amount
                    ? new Color(0.3f, 1f, 0.3f)
                    : new Color(1f, 0.3f, 0.3f);
            }
            else
            {
                iconImg.color = Color.gray;
                go.transform.Find("Amount").GetComponent<Text>().text = $"0/{input.amount}";
            }

            costInstances.Add(go);
        }
    }

    void ClearCostList()
    {
        foreach (var go in costInstances)
            Destroy(go);
        costInstances.Clear();
    }

    void RefreshCraftButton()
    {
        if (selectedRecipe == null)
        {
            craftBtn.interactable = false;
            return;
        }
        craftBtn.interactable = RaftConfigTables.CanAffordRecipe(RaftGame.Instance.Inv, selectedRecipe);
    }

    void OnCraft()
    {
        if (selectedRecipe == null) return;
        var inv = RaftGame.Instance.Inv;

        if (RaftConfigTables.ConsumeRecipeInputs(inv, selectedRecipe))
        {
            if (RaftConfigTables.TryGetItemTypeById(selectedRecipe.outputItemTypeId, out var outputType))
            {
                inv.Add(outputType, selectedRecipe.outputAmount);
                if (RaftGame.Instance.UI != null)
                    RaftGame.Instance.UI.ShowToast($"\u5236\u9020\u4e86 {selectedRecipe.displayName}");
            }
        }

        // Refresh
        RefreshRecipeList();
        RefreshCostList();
        RefreshCraftButton();
    }
}
