using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ThronefallUI : MonoBehaviour
{
    Text coinAmountText;
    Text dayText;
    GameObject gameOverPanel;
    RectTransform waveWarningContainer;

    GameObject buildPanelRoot;
    CanvasGroup buildPanelCanvasGroup;
    Text buildTitleText;
    Text buildDescText;
    Text buildStatIconText;
    Text buildStatBeforeText;
    Text buildStatAfterText;
    Text buildCostAmountText;

    float buildPanelFadeTarget;
    List<ThronefallWaveWarning> activeWarnings = new List<ThronefallWaveWarning>();

    GameObject revivalPanel;
    Text revivalCountdownText;
    Text revivalHintText;
    Text weaponLabelText;

    // Branch upgrade panel
    GameObject branchOverlay;
    GameObject branchPanel;
    Text branchTitleText;
    Text branchDescText;
    List<GameObject> branchButtons = new List<GameObject>();
    List<Image> branchButtonBGs = new List<Image>();
    List<Text> branchButtonLabels = new List<Text>();
    TFBuildingConfig[] branchConfigs;
    int selectedBranchIndex;
    bool branchPanelOpen;

    // Selection rect
    GameObject selectionRectGo;
    RectTransform selectionRectRT;

    public bool IsBranchPanelOpen => branchPanelOpen;

    void Start()
    {
        var hudPrefab = Resources.Load<GameObject>("UI/ThronefallHUD");
        if (hudPrefab != null)
        {
            var hud = Instantiate(hudPrefab, transform, false);
            coinAmountText = FindText(hud.transform, "CoinPanel/CoinAmount");
            dayText = FindText(hud.transform, "DayLabel/DayText");
            gameOverPanel = FindChild(hud.transform, "GameOverPanel");
            var warnContainer = hud.transform.Find("WaveWarningContainer");
            if (warnContainer != null)
                waveWarningContainer = warnContainer.GetComponent<RectTransform>();
        }

        var buildPrefab = Resources.Load<GameObject>("UI/ThronefallBuildPanel");
        if (buildPrefab != null)
        {
            buildPanelRoot = Instantiate(buildPrefab, transform, false);
            buildPanelCanvasGroup = buildPanelRoot.GetComponent<CanvasGroup>();
            if (buildPanelCanvasGroup == null)
                buildPanelCanvasGroup = buildPanelRoot.AddComponent<CanvasGroup>();

            buildTitleText = FindText(buildPanelRoot.transform, "Title");
            buildDescText = FindText(buildPanelRoot.transform, "Description");
            buildStatIconText = FindText(buildPanelRoot.transform, "StatRow/StatIcon");
            buildStatBeforeText = FindText(buildPanelRoot.transform, "StatRow/StatBefore");
            buildStatAfterText = FindText(buildPanelRoot.transform, "StatRow/StatAfter");
            buildCostAmountText = FindText(buildPanelRoot.transform, "CostRow/CostAmount");

            buildPanelRoot.SetActive(false);
            buildPanelCanvasGroup.alpha = 0;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        CreateRevivalPanel();
        CreateWeaponLabel();
        CreateBranchPanel();
        CreateSelectionRect();
    }

    void CreateRevivalPanel()
    {
        revivalPanel = new GameObject("RevivalPanel");
        revivalPanel.transform.SetParent(transform, false);
        var rect = revivalPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 140);
        rect.anchoredPosition = Vector2.zero;

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(revivalPanel.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        bgGo.AddComponent<CanvasRenderer>();
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.7f);
        bgImg.raycastTarget = false;

        var countGo = new GameObject("Countdown");
        countGo.transform.SetParent(revivalPanel.transform, false);
        var countRect = countGo.AddComponent<RectTransform>();
        countRect.anchorMin = new Vector2(0, 0.35f);
        countRect.anchorMax = new Vector2(1, 1);
        countRect.offsetMin = Vector2.zero;
        countRect.offsetMax = Vector2.zero;
        countGo.AddComponent<CanvasRenderer>();
        revivalCountdownText = countGo.AddComponent<Text>();
        revivalCountdownText.font = Font.CreateDynamicFontFromOSFont("Arial", 48);
        revivalCountdownText.fontSize = 48;
        revivalCountdownText.fontStyle = FontStyle.Bold;
        revivalCountdownText.color = Color.white;
        revivalCountdownText.alignment = TextAnchor.MiddleCenter;
        revivalCountdownText.raycastTarget = false;
        revivalCountdownText.text = "10";

        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(revivalPanel.transform, false);
        var hintRect = hintGo.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0.4f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        hintGo.AddComponent<CanvasRenderer>();
        revivalHintText = hintGo.AddComponent<Text>();
        revivalHintText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
        revivalHintText.fontSize = 20;
        revivalHintText.color = new Color(0.7f, 0.7f, 0.7f);
        revivalHintText.alignment = TextAnchor.MiddleCenter;
        revivalHintText.raycastTarget = false;
        revivalHintText.text = "等待复活...";

        revivalPanel.SetActive(false);
    }

    void CreateWeaponLabel()
    {
        var go = new GameObject("WeaponLabel");
        go.transform.SetParent(transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0);
        rect.anchoredPosition = new Vector2(30, 30);
        rect.sizeDelta = new Vector2(200, 40);
        go.AddComponent<CanvasRenderer>();
        weaponLabelText = go.AddComponent<Text>();
        weaponLabelText.font = Font.CreateDynamicFontFromOSFont("Arial", 22);
        weaponLabelText.fontSize = 22;
        weaponLabelText.fontStyle = FontStyle.Bold;
        weaponLabelText.color = Color.white;
        weaponLabelText.alignment = TextAnchor.MiddleLeft;
        weaponLabelText.raycastTarget = false;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(1, -1);
    }

    void CreateBranchPanel()
    {
        // Full-screen overlay
        branchOverlay = new GameObject("BranchOverlay");
        branchOverlay.transform.SetParent(transform, false);
        var overlayRect = branchOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        branchOverlay.AddComponent<CanvasRenderer>();
        var overlayImg = branchOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.7f);
        overlayImg.raycastTarget = false;

        // Center panel
        branchPanel = new GameObject("BranchPanel");
        branchPanel.transform.SetParent(branchOverlay.transform, false);
        var panelRect = branchPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 350);
        panelRect.anchoredPosition = Vector2.zero;
        branchPanel.AddComponent<CanvasRenderer>();
        var panelImg = branchPanel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panelImg.raycastTarget = false;

        // Title
        var titleGo = new GameObject("BranchTitle");
        titleGo.transform.SetParent(branchPanel.transform, false);
        var titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = new Vector2(20, 0);
        titleRect.offsetMax = new Vector2(-20, 0);
        titleGo.AddComponent<CanvasRenderer>();
        branchTitleText = titleGo.AddComponent<Text>();
        branchTitleText.font = Font.CreateDynamicFontFromOSFont("Arial", 30);
        branchTitleText.fontSize = 30;
        branchTitleText.fontStyle = FontStyle.Bold;
        branchTitleText.color = Color.white;
        branchTitleText.alignment = TextAnchor.MiddleCenter;
        branchTitleText.raycastTarget = false;

        // Description
        var descGo = new GameObject("BranchDesc");
        descGo.transform.SetParent(branchPanel.transform, false);
        var descRect = descGo.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.4f);
        descRect.anchorMax = new Vector2(1, 0.7f);
        descRect.offsetMin = new Vector2(30, 0);
        descRect.offsetMax = new Vector2(-30, 0);
        descGo.AddComponent<CanvasRenderer>();
        branchDescText = descGo.AddComponent<Text>();
        branchDescText.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        branchDescText.fontSize = 18;
        branchDescText.color = new Color(0.75f, 0.75f, 0.75f);
        branchDescText.alignment = TextAnchor.MiddleCenter;
        branchDescText.raycastTarget = false;

        // Hint text
        var hintGo = new GameObject("BranchHint");
        hintGo.transform.SetParent(branchPanel.transform, false);
        var hintRect = hintGo.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0.1f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        hintGo.AddComponent<CanvasRenderer>();
        var hintText = hintGo.AddComponent<Text>();
        hintText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        hintText.fontSize = 14;
        hintText.color = new Color(0.5f, 0.5f, 0.5f);
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.text = "左/右选择  |  空格确认  |  Esc取消";
        hintText.raycastTarget = false;

        branchOverlay.SetActive(false);
    }

    void CreateSelectionRect()
    {
        selectionRectGo = new GameObject("SelectionRect");
        selectionRectGo.transform.SetParent(transform, false);
        selectionRectRT = selectionRectGo.AddComponent<RectTransform>();
        selectionRectRT.anchorMin = Vector2.zero;
        selectionRectRT.anchorMax = Vector2.zero;
        selectionRectGo.AddComponent<CanvasRenderer>();
        var img = selectionRectGo.AddComponent<Image>();
        img.color = new Color(0.3f, 0.6f, 1f, 0.2f);
        img.raycastTarget = false;
        selectionRectGo.SetActive(false);
    }

    public void ShowSelectionRect(Vector2 start, Vector2 end)
    {
        if (selectionRectGo == null) return;
        selectionRectGo.SetActive(true);
        Vector2 min = Vector2.Min(start, end);
        Vector2 max = Vector2.Max(start, end);
        selectionRectRT.offsetMin = min;
        selectionRectRT.offsetMax = max;
    }

    public void HideSelectionRect()
    {
        if (selectionRectGo != null)
            selectionRectGo.SetActive(false);
    }

    public void ShowBranchPanel(TFBuildingConfig[] options)
    {
        if (options == null || options.Length == 0) return;

        branchConfigs = options;
        selectedBranchIndex = 0;

        // Clear old buttons
        foreach (var btn in branchButtons)
        {
            if (btn != null) Destroy(btn);
        }
        branchButtons.Clear();
        branchButtonBGs.Clear();
        branchButtonLabels.Clear();

        // Create button row container
        float totalWidth = options.Length * 110f;
        float startX = -totalWidth / 2f + 55f;

        for (int i = 0; i < options.Length; i++)
        {
            var btnGo = new GameObject($"BranchBtn_{i}");
            btnGo.transform.SetParent(branchPanel.transform, false);
            var btnRect = btnGo.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.15f);
            btnRect.anchorMax = new Vector2(0.5f, 0.15f);
            btnRect.sizeDelta = new Vector2(100, 100);
            btnRect.anchoredPosition = new Vector2(startX + i * 110f, 40f);

            btnGo.AddComponent<CanvasRenderer>();
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.25f);
            btnImg.raycastTarget = false;

            // Icon/Name label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5, 5);
            labelRect.offsetMax = new Vector2(-5, -5);
            labelGo.AddComponent<CanvasRenderer>();
            var labelText = labelGo.AddComponent<Text>();
            labelText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            labelText.fontSize = 16;
            labelText.fontStyle = FontStyle.Bold;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;

            string label = !string.IsNullOrEmpty(options[i].branchIcon)
                ? options[i].branchIcon
                : options[i].buildingName;
            labelText.text = label;

            // Cost label
            var costGo = new GameObject("Cost");
            costGo.transform.SetParent(btnGo.transform, false);
            var costRect = costGo.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(1, 0.25f);
            costRect.offsetMin = Vector2.zero;
            costRect.offsetMax = Vector2.zero;
            costGo.AddComponent<CanvasRenderer>();
            var costText = costGo.AddComponent<Text>();
            costText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            costText.fontSize = 12;
            costText.color = new Color(1f, 0.85f, 0.2f);
            costText.alignment = TextAnchor.MiddleCenter;
            costText.text = options[i].coinCost.ToString();
            costText.raycastTarget = false;

            branchButtons.Add(btnGo);
            branchButtonBGs.Add(btnImg);
            branchButtonLabels.Add(labelText);
        }

        UpdateBranchSelection();
        branchOverlay.SetActive(true);
        branchPanelOpen = true;
        Time.timeScale = 0f;
    }

    public void HideBranchPanel()
    {
        branchOverlay.SetActive(false);
        branchPanelOpen = false;
        branchConfigs = null;
        Time.timeScale = 1f;
    }

    public void NavigateBranch(int delta)
    {
        if (branchConfigs == null || branchConfigs.Length == 0) return;
        selectedBranchIndex = (selectedBranchIndex + delta + branchConfigs.Length) % branchConfigs.Length;
        UpdateBranchSelection();
    }

    void UpdateBranchSelection()
    {
        if (branchConfigs == null) return;

        var selected = branchConfigs[selectedBranchIndex];
        if (branchTitleText != null)
            branchTitleText.text = selected.buildingName;
        if (branchDescText != null)
            branchDescText.text = selected.description ?? "";

        for (int i = 0; i < branchButtonBGs.Count; i++)
        {
            if (i == selectedBranchIndex)
            {
                branchButtonBGs[i].color = new Color(0.8f, 0.65f, 0.1f);
                branchButtons[i].transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            }
            else
            {
                branchButtonBGs[i].color = new Color(0.2f, 0.2f, 0.25f);
                branchButtons[i].transform.localScale = Vector3.one;
            }
        }
    }

    public int GetSelectedBranchId()
    {
        if (branchConfigs == null || selectedBranchIndex < 0 || selectedBranchIndex >= branchConfigs.Length)
            return -1;
        return branchConfigs[selectedBranchIndex].buildingId;
    }

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        UpdateCoinDisplay(game.Coins);

        if (buildPanelCanvasGroup != null)
        {
            float current = buildPanelCanvasGroup.alpha;
            float next = Mathf.MoveTowards(current, buildPanelFadeTarget, Time.unscaledDeltaTime * 6f);
            buildPanelCanvasGroup.alpha = next;

            if (buildPanelFadeTarget > 0 && !buildPanelRoot.activeSelf)
                buildPanelRoot.SetActive(true);
            else if (next <= 0.01f && buildPanelFadeTarget <= 0 && buildPanelRoot.activeSelf)
                buildPanelRoot.SetActive(false);
        }
    }

    public void UpdateCoinDisplay(int amount)
    {
        if (coinAmountText != null)
            coinAmountText.text = amount.ToString();
    }

    public void UpdateDayLabel()
    {
        if (dayText == null) return;
        var game = ThronefallGame.Instance;
        if (game == null) return;

        switch (game.CurrentPhase)
        {
            case ThronefallGame.GamePhase.Day:
                dayText.text = $"第 {game.CurrentDay} 天 - 按R开始夜晚";
                break;
            case ThronefallGame.GamePhase.Night:
                dayText.text = $"第 {game.CurrentDay} 夜 - 存活！";
                break;
            case ThronefallGame.GamePhase.GameOver:
                dayText.text = "";
                break;
        }
    }

    public void ShowBuildPanel(TFBuildingConfig config)
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Day)
            return;
        if (config == null) return;

        if (buildTitleText != null) buildTitleText.text = config.buildingName;
        if (buildDescText != null) buildDescText.text = config.description ?? "";

        string statName = "";
        string statBefore = "";
        string statAfter = "";
        switch (config.buildingType)
        {
            case "tower":
                statName = "攻击";
                statAfter = config.atk.ToString();
                break;
            case "wall":
                statName = "防御";
                statAfter = config.def.ToString();
                break;
            case "economic":
                statName = "产出";
                statAfter = config.dailyYield.ToString();
                break;
            case "barracks":
                statName = "招募";
                statAfter = config.maxRecruits.ToString();
                break;
            default:
                statName = "生命";
                statAfter = config.maxHP.ToString();
                break;
        }

        if (buildStatIconText != null) buildStatIconText.text = statName;
        if (buildStatBeforeText != null) buildStatBeforeText.text = statBefore;
        if (buildStatAfterText != null) buildStatAfterText.text = statAfter;
        if (buildCostAmountText != null) buildCostAmountText.text = config.coinCost.ToString();

        buildPanelFadeTarget = 1f;
    }

    public void ShowBuildPanelForRecruit(TFBuildingConfig config, int currentCount)
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Day)
            return;
        if (config == null) return;

        var unitConfig = ThronefallConfigTables.GetAllyUnitConfig(config.allyUnitType);
        string unitName = unitConfig != null ? unitConfig.unitName : "士兵";
        int unitAtk = unitConfig != null ? unitConfig.atk : 0;

        if (buildTitleText != null) buildTitleText.text = $"招募{unitName}";
        if (buildDescText != null) buildDescText.text = $"数量: {currentCount}/{config.maxRecruits}";
        if (buildStatIconText != null) buildStatIconText.text = "攻击";
        if (buildStatBeforeText != null) buildStatBeforeText.text = "";
        if (buildStatAfterText != null) buildStatAfterText.text = unitAtk.ToString();
        if (buildCostAmountText != null) buildCostAmountText.text = config.recruitCost.ToString();

        buildPanelFadeTarget = 1f;
    }

    public void HideBuildPanel()
    {
        buildPanelFadeTarget = 0f;
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        if (dayText != null)
            dayText.text = "";
        HideRevivalCountdown();
        if (branchPanelOpen)
            HideBranchPanel();
    }

    public void ShowRevivalCountdown(float totalTime)
    {
        if (revivalPanel != null)
            revivalPanel.SetActive(true);
        UpdateRevivalCountdown(totalTime);
    }

    public void UpdateRevivalCountdown(float timeRemaining)
    {
        if (revivalCountdownText != null)
            revivalCountdownText.text = Mathf.CeilToInt(Mathf.Max(0, timeRemaining)).ToString();
    }

    public void HideRevivalCountdown()
    {
        if (revivalPanel != null)
            revivalPanel.SetActive(false);
    }

    public void UpdateWeaponLabel(string weaponName)
    {
        if (weaponLabelText != null)
            weaponLabelText.text = weaponName;
    }

    public void CreateWaveWarnings(TFWaveConfig waveConfig)
    {
        ClearWaveWarnings();
        if (waveConfig == null || waveConfig.spawnPoints == null) return;
        if (waveWarningContainer == null) return;

        foreach (var sp in waveConfig.spawnPoints)
        {
            if (sp == null) continue;
            int totalCount = ThronefallConfigTables.GetTotalMonsterCount(sp);
            string iconLabel = ThronefallConfigTables.GetSpawnPointIconLabel(sp);

            var warningGo = new GameObject("WaveWarning");
            warningGo.transform.SetParent(waveWarningContainer, false);
            var warning = warningGo.AddComponent<ThronefallWaveWarning>();
            warning.Init(sp.position.ToVector3(), totalCount, iconLabel, waveWarningContainer);
            activeWarnings.Add(warning);
        }
    }

    public void ClearWaveWarnings()
    {
        foreach (var w in activeWarnings)
        {
            if (w != null && w.gameObject != null)
                Destroy(w.gameObject);
        }
        activeWarnings.Clear();
    }

    Text FindText(Transform root, string path)
    {
        var t = root.Find(path);
        return t != null ? t.GetComponent<Text>() : null;
    }

    GameObject FindChild(Transform root, string path)
    {
        var t = root.Find(path);
        return t != null ? t.gameObject : null;
    }
}
