using System.Collections.Generic;
using UnityEngine;

public class ThronefallBuildSystem : MonoBehaviour
{
    public enum NodeActionType { None, Build, Upgrade, BranchUpgrade, Recruit }

    public class BuildNode
    {
        public GameObject gameObject;
        public int initialBuildingId;
        public int currentBuildingId;
        public ThronefallBuilding building;
        public ThronefallBuildNodeUI nodeUI;
        public GameObject nodeVisual;
        public List<ThronefallAlly> allies = new List<ThronefallAlly>();
    }

    List<BuildNode> allNodes = new List<BuildNode>();
    bool buildingEnabled = true;

    public void CreateBuildNode(Vector3 position, int buildingId, Transform parent)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var go = new GameObject($"BuildNode_{allNodes.Count}");
        go.transform.SetParent(parent);
        go.transform.position = position;

        var nodeVisual = ProceduralMeshUtil.CreatePrimitive("NodeVisual", game.CubeMesh, game.BuildNodeMat, go.transform);
        nodeVisual.transform.localPosition = new Vector3(0, 0.5f, 0);
        nodeVisual.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3.5f;
        trigger.center = new Vector3(0, 1f, 0);

        var marker = go.AddComponent<ThronefallBuildNodeMarker>();
        marker.nodeIndex = allNodes.Count;

        var nodeUI = go.AddComponent<ThronefallBuildNodeUI>();
        nodeUI.Init(go.transform);

        var node = new BuildNode
        {
            gameObject = go,
            initialBuildingId = buildingId,
            currentBuildingId = 0,
            nodeVisual = nodeVisual,
            nodeUI = nodeUI
        };
        allNodes.Add(node);
    }

    public NodeActionType GetNodeActionType(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return NodeActionType.None;
        var node = allNodes[nodeIndex];

        if (node.currentBuildingId == 0)
            return NodeActionType.Build;

        var config = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (config == null) return NodeActionType.None;

        if (config.buildingType == "barracks" && node.building != null && !node.building.IsRuined)
        {
            int maxRecruits = config.maxRecruits > 0 ? config.maxRecruits : 3;
            CleanDeadAllies(node);
            if (node.allies.Count < maxRecruits)
                return NodeActionType.Recruit;
        }

        if (config.upgradeIds != null && config.upgradeIds.Length > 0 && node.building != null && !node.building.IsRuined)
        {
            if (config.upgradeIds.Length == 1)
                return NodeActionType.Upgrade;
            else
                return NodeActionType.BranchUpgrade;
        }

        return NodeActionType.None;
    }

    public TFBuildingConfig GetActionConfig(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return null;
        var node = allNodes[nodeIndex];

        if (node.currentBuildingId == 0)
            return ThronefallConfigTables.GetBuildingConfig(node.initialBuildingId);

        var config = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (config == null) return null;

        if (config.upgradeIds != null && config.upgradeIds.Length == 1)
            return ThronefallConfigTables.GetBuildingConfig(config.upgradeIds[0]);

        return config;
    }

    public TFBuildingConfig[] GetBranchConfigs(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return null;
        var node = allNodes[nodeIndex];

        var config = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (config == null || config.upgradeIds == null || config.upgradeIds.Length <= 1) return null;

        var results = new List<TFBuildingConfig>();
        foreach (var id in config.upgradeIds)
        {
            var bc = ThronefallConfigTables.GetBuildingConfig(id);
            if (bc != null) results.Add(bc);
        }
        return results.Count > 0 ? results.ToArray() : null;
    }

    public TFBuildingConfig GetCurrentBuildingConfig(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return null;
        var node = allNodes[nodeIndex];
        if (node.currentBuildingId == 0) return null;
        return ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
    }

    public bool TryBuild(int nodeIndex)
    {
        if (!buildingEnabled) return false;
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return false;

        var node = allNodes[nodeIndex];
        if (node.currentBuildingId != 0) return false;

        var config = ThronefallConfigTables.GetBuildingConfig(node.initialBuildingId);
        if (config == null) return false;

        var game = ThronefallGame.Instance;
        if (game == null) return false;
        if (game.Coins < config.coinCost) return false;

        game.Coins -= config.coinCost;
        SpawnBuilding(node, config);

        if (node.nodeVisual != null)
            node.nodeVisual.SetActive(false);

        if (game.UI != null)
            game.UI.HideBuildPanel();

        return true;
    }

    public bool TryUpgrade(int nodeIndex)
    {
        if (!buildingEnabled) return false;
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return false;

        var node = allNodes[nodeIndex];
        if (node.currentBuildingId == 0 || node.building == null) return false;

        var currentConfig = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (currentConfig == null || currentConfig.upgradeIds == null || currentConfig.upgradeIds.Length != 1)
            return false;

        var upgradeConfig = ThronefallConfigTables.GetBuildingConfig(currentConfig.upgradeIds[0]);
        if (upgradeConfig == null) return false;

        var game = ThronefallGame.Instance;
        if (game == null) return false;
        if (game.Coins < upgradeConfig.coinCost) return false;

        game.Coins -= upgradeConfig.coinCost;
        ReplaceBuilding(node, upgradeConfig);

        if (game.UI != null)
            game.UI.HideBuildPanel();

        return true;
    }

    public bool TryBranchUpgrade(int nodeIndex, int targetBuildingId)
    {
        if (!buildingEnabled) return false;
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return false;

        var node = allNodes[nodeIndex];
        if (node.currentBuildingId == 0 || node.building == null) return false;

        var currentConfig = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (currentConfig == null || currentConfig.upgradeIds == null) return false;

        bool validTarget = false;
        foreach (var id in currentConfig.upgradeIds)
        {
            if (id == targetBuildingId) { validTarget = true; break; }
        }
        if (!validTarget) return false;

        var upgradeConfig = ThronefallConfigTables.GetBuildingConfig(targetBuildingId);
        if (upgradeConfig == null) return false;

        var game = ThronefallGame.Instance;
        if (game == null) return false;
        if (game.Coins < upgradeConfig.coinCost) return false;

        game.Coins -= upgradeConfig.coinCost;
        ReplaceBuilding(node, upgradeConfig);

        if (game.UI != null)
            game.UI.HideBuildPanel();

        return true;
    }

    public bool TryRecruit(int nodeIndex)
    {
        if (!buildingEnabled) return false;
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return false;

        var node = allNodes[nodeIndex];
        if (node.currentBuildingId == 0 || node.building == null) return false;
        if (node.building.IsRuined) return false;

        var config = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        if (config == null || config.buildingType != "barracks") return false;

        var game = ThronefallGame.Instance;
        if (game == null) return false;

        int maxRecruits = config.maxRecruits > 0 ? config.maxRecruits : 3;
        CleanDeadAllies(node);
        if (node.allies.Count >= maxRecruits) return false;

        int cost = config.recruitCost > 0 ? config.recruitCost : 20;
        if (game.Coins < cost) return false;

        game.Coins -= cost;

        Vector3 rallyPoint = node.gameObject.transform.position + new Vector3(0, 0, 2f);
        Vector3 spawnOffset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));

        var allyGo = new GameObject($"Ally_{node.allies.Count}");
        allyGo.transform.SetParent(game.RootContainer);
        allyGo.transform.position = rallyPoint + spawnOffset;

        var ally = allyGo.AddComponent<ThronefallAlly>();
        ally.Init(config, rallyPoint);
        node.allies.Add(ally);

        return true;
    }

    void SpawnBuilding(BuildNode node, TFBuildingConfig config)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        Vector3 pos = node.gameObject.transform.position;

        var buildingGo = new GameObject($"Building_{config.buildingName}");
        buildingGo.transform.SetParent(game.RootContainer);
        buildingGo.transform.position = pos;

        var building = buildingGo.AddComponent<ThronefallBuilding>();
        building.Init(config, false);

        node.currentBuildingId = config.buildingId;
        node.building = building;
    }

    void ReplaceBuilding(BuildNode node, TFBuildingConfig newConfig)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        Vector3 pos = node.gameObject.transform.position;

        if (node.building != null)
        {
            game.CombatSys.UnregisterEntity(node.building);
            Destroy(node.building.gameObject);
            node.building = null;
        }

        var buildingGo = new GameObject($"Building_{newConfig.buildingName}");
        buildingGo.transform.SetParent(game.RootContainer);
        buildingGo.transform.position = pos;

        var building = buildingGo.AddComponent<ThronefallBuilding>();
        building.Init(newConfig, false);

        node.currentBuildingId = newConfig.buildingId;
        node.building = building;
    }

    public void DawnRecover()
    {
        foreach (var node in allNodes)
        {
            if (node.building == null) continue;

            if (node.building.BuildingType == "economic")
                node.building.WasRuinedLastNight = node.building.IsRuined;

            if (node.building.IsRuined)
                node.building.RecoverFromRuined();
        }
    }

    public int CalculateDawnIncome()
    {
        int total = 0;
        foreach (var node in allNodes)
        {
            if (node.building == null) continue;
            if (node.building.BuildingType != "economic") continue;
            if (node.building.WasRuinedLastNight) continue;
            total += node.building.DailyYield;
        }

        foreach (var node in allNodes)
        {
            if (node.building != null)
                node.building.MarkDawnProcessed();
        }

        return total;
    }

    public void SetBuildingEnabled(bool enabled)
    {
        buildingEnabled = enabled;
        if (!enabled)
        {
            var game = ThronefallGame.Instance;
            if (game != null && game.UI != null)
                game.UI.HideBuildPanel();
        }
    }

    public bool IsBuildingEnabled => buildingEnabled;

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        bool isDay = game.CurrentPhase == ThronefallGame.GamePhase.Day;

        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            if (node.nodeUI == null) continue;

            var actionType = GetNodeActionType(i);

            if (isDay && actionType != NodeActionType.None)
            {
                string icon = "";
                int cost = 0;

                switch (actionType)
                {
                    case NodeActionType.Build:
                        icon = "H";
                        var buildConfig = ThronefallConfigTables.GetBuildingConfig(node.initialBuildingId);
                        cost = buildConfig != null ? buildConfig.coinCost : 0;
                        break;
                    case NodeActionType.Upgrade:
                        icon = "U";
                        var curConfig = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
                        if (curConfig != null && curConfig.upgradeIds != null && curConfig.upgradeIds.Length == 1)
                        {
                            var upConfig = ThronefallConfigTables.GetBuildingConfig(curConfig.upgradeIds[0]);
                            cost = upConfig != null ? upConfig.coinCost : 0;
                        }
                        break;
                    case NodeActionType.BranchUpgrade:
                        icon = "U";
                        var branchCurConfig = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
                        if (branchCurConfig != null && branchCurConfig.upgradeIds != null && branchCurConfig.upgradeIds.Length > 0)
                        {
                            var firstBranch = ThronefallConfigTables.GetBuildingConfig(branchCurConfig.upgradeIds[0]);
                            cost = firstBranch != null ? firstBranch.coinCost : 0;
                        }
                        break;
                    case NodeActionType.Recruit:
                        icon = "R";
                        var recConfig = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
                        cost = recConfig != null ? recConfig.recruitCost : 20;
                        break;
                }

                bool canAfford = game.Coins >= cost;
                node.nodeUI.UpdateNodeStatus(icon, cost, canAfford, true);
            }
            else
            {
                node.nodeUI.UpdateNodeStatus("", 0, false, false);
            }

            bool showHP = node.building != null && !node.building.IsRuined &&
                          node.building.GetHPRatio() < 1f;
            float hpRatio = node.building != null ? node.building.GetHPRatio() : 1f;
            node.nodeUI.UpdateHPBar(hpRatio, showHP);
        }

        foreach (var node in allNodes)
            CleanDeadAllies(node);
    }

    void CleanDeadAllies(BuildNode node)
    {
        for (int i = node.allies.Count - 1; i >= 0; i--)
        {
            if (node.allies[i] == null || !node.allies[i].IsAlive)
                node.allies.RemoveAt(i);
        }
    }

    public int GetRecruitCount(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return 0;
        var node = allNodes[nodeIndex];
        CleanDeadAllies(node);
        return node.allies.Count;
    }

    public int GetMaxRecruits(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return 0;
        var node = allNodes[nodeIndex];
        var config = ThronefallConfigTables.GetBuildingConfig(node.currentBuildingId);
        return config != null ? config.maxRecruits : 0;
    }
}
