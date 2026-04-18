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
