using System.Collections.Generic;
using UnityEngine;

public class ThronefallBuildSystem : MonoBehaviour
{
    public class BuildNode
    {
        public GameObject gameObject;
        public int nodeId;
        public bool isBuilt;
    }

    List<BuildNode> allNodes = new List<BuildNode>();
    bool buildingEnabled = true;

    public void CreateBuildNode(Vector3 position, int nodeId, Transform parent)
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;

        var go = new GameObject($"BuildNode_{allNodes.Count}");
        go.transform.SetParent(parent);
        go.transform.position = position;

        // Semi-transparent green cube visual
        var visual = ProceduralMeshUtil.CreatePrimitive("NodeVisual", game.CubeMesh, game.BuildNodeMat, go.transform);
        visual.transform.localPosition = new Vector3(0, 0.5f, 0);
        visual.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        // Trigger collider for player detection
        var trigger = go.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 3.5f;
        trigger.center = new Vector3(0, 1f, 0);

        // Marker for identification
        var marker = go.AddComponent<ThronefallBuildNodeMarker>();
        marker.nodeIndex = allNodes.Count;

        var node = new BuildNode
        {
            gameObject = go,
            nodeId = nodeId,
            isBuilt = false
        };
        allNodes.Add(node);
    }

    public TFBuildingNodeConfig GetNodeConfig(int nodeIndex)
    {
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return null;
        var node = allNodes[nodeIndex];
        if (node.isBuilt) return null;
        return ThronefallConfigTables.GetBuildingNodeConfig(node.nodeId);
    }

    public void TryBuild(int nodeIndex)
    {
        if (!buildingEnabled) return;
        if (nodeIndex < 0 || nodeIndex >= allNodes.Count) return;

        var node = allNodes[nodeIndex];
        if (node.isBuilt) return;

        var config = ThronefallConfigTables.GetBuildingNodeConfig(node.nodeId);
        if (config == null) return;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        if (game.Coins < config.coinCost) return;

        // Build!
        game.Coins -= config.coinCost;

        Vector3 pos = node.gameObject.transform.position;

        // Disable node visual and trigger but keep the GO for reference
        node.gameObject.SetActive(false);
        node.isBuilt = true;

        // Spawn building
        var buildingGo = new GameObject($"Building_{config.buildingName}");
        buildingGo.transform.SetParent(game.RootContainer);
        buildingGo.transform.position = pos;
        var building = buildingGo.AddComponent<ThronefallBuilding>();
        building.Init(config, false);

        // Hide build panel
        if (game.UI != null)
            game.UI.HideBuildPanel();
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
}
