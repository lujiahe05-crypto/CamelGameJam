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

    void Start()
    {
        var canvas = GetComponent<Canvas>();

        // Load and instantiate HUD prefab
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

        // Load and instantiate BuildPanel prefab
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
        revivalHintText.text = "Waiting for Revival...";

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

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        UpdateCoinDisplay(game.Coins);

        // Smooth fade build panel
        if (buildPanelCanvasGroup != null)
        {
            float current = buildPanelCanvasGroup.alpha;
            float next = Mathf.MoveTowards(current, buildPanelFadeTarget, Time.deltaTime * 6f);
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
                dayText.text = $"Day {game.CurrentDay} - Press R to start night";
                break;
            case ThronefallGame.GamePhase.Night:
                dayText.text = $"Night {game.CurrentDay} - Survive!";
                break;
            case ThronefallGame.GamePhase.GameOver:
                dayText.text = "";
                break;
        }
    }

    public void ShowBuildPanel(TFBuildingNodeConfig config)
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Day)
            return;
        if (config == null) return;

        if (buildTitleText != null) buildTitleText.text = $"Build {config.buildingName}";
        if (buildDescText != null) buildDescText.text = config.description;
        if (buildStatIconText != null) buildStatIconText.text = config.statName;
        if (buildStatBeforeText != null) buildStatBeforeText.text = config.statBefore.ToString();
        if (buildStatAfterText != null) buildStatAfterText.text = config.statAfter.ToString();
        if (buildCostAmountText != null) buildCostAmountText.text = config.coinCost.ToString();

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
