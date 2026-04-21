using UnityEngine;
using System.Collections.Generic;

public class RaftManager : MonoBehaviour
{
    public const int FoundationBuildingId = RaftConfigTables.FoundationBuildingId;

    Dictionary<Vector2Int, RaftBlock> blocks = new Dictionary<Vector2Int, RaftBlock>();

    // Building
    GameObject ghostBlock;
    Vector2Int ghostGridPos;
    bool ghostValid;

    // Kinematic buoyancy
    float bobVelocity;
    float tiltX, tiltZ;

    /// <summary>
    /// Build mode is active when the player holds the BuildHammer.
    /// </summary>
    public bool IsBuildMode
    {
        get
        {
            if (RaftGame.Instance == null || RaftGame.Instance.Inv == null) return false;
            return RaftGame.Instance.Inv.GetSelectedItemType() == ItemType.BuildHammer;
        }
    }

    void Start()
    {
        // Create initial 2x2 raft
        for (int x = 0; x < 2; x++)
            for (int z = 0; z < 2; z++)
                AddBlock(new Vector2Int(x, z));

        transform.position = new Vector3(0, 0.15f, 0);

        CreateGhostBlock();
    }

    void CreateGhostBlock()
    {
        ghostBlock = ProceduralMeshUtil.CreatePrimitive("Ghost", RaftGame.Instance.CubeMesh, RaftGame.Instance.GhostMat);
        ghostBlock.transform.localScale = new Vector3(2f, 0.3f, 2f);
        ghostBlock.SetActive(false);
    }

    public void AddBlock(Vector2Int gridPos)
    {
        if (blocks.ContainsKey(gridPos)) return;

        var go = new GameObject("RaftBlock_" + gridPos);
        go.transform.SetParent(transform);
        go.transform.localPosition = new Vector3(gridPos.x * 2f, 0, gridPos.y * 2f);

        var block = go.AddComponent<RaftBlock>();
        block.GridPos = gridPos;
        block.Init();
        blocks[gridPos] = block;
    }

    public void RemoveBlock(Vector2Int gridPos)
    {
        if (!blocks.TryGetValue(gridPos, out var block)) return;
        if (blocks.Count <= 1) return;

        blocks.Remove(gridPos);
        Destroy(block.gameObject);
    }

    public RaftBlock GetNearestEdgeBlock(Vector3 worldPos)
    {
        RaftBlock nearest = null;
        float minDist = float.MaxValue;

        foreach (var kv in blocks)
        {
            bool isEdge = false;
            var dirs = new Vector2Int[] {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            foreach (var d in dirs)
            {
                if (!blocks.ContainsKey(kv.Key + d))
                {
                    isEdge = true;
                    break;
                }
            }

            if (!isEdge) continue;

            float dist = Vector3.Distance(kv.Value.transform.position, worldPos);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = kv.Value;
            }
        }

        return nearest;
    }

    public Vector3 GetCenter()
    {
        if (blocks.Count == 0) return transform.position;
        Vector3 sum = Vector3.zero;
        foreach (var b in blocks.Values)
            sum += b.transform.position;
        return sum / blocks.Count;
    }

    void Update()
    {
        UpdateBuoyancy();

        if (RaftUI.IsUIOpen)
        {
            ghostBlock.SetActive(false);
            return;
        }

        var selectedType = RaftGame.Instance.Inv.GetSelectedItemType();

        if (IsBuildMode)
        {
            ghostBlock.SetActive(true);
            UpdateBuildMode();
        }
        else if (IsPlaceableItem(selectedType))
        {
            ghostBlock.SetActive(true);
            UpdatePlacementMode(selectedType);
        }
        else
        {
            ghostBlock.SetActive(false);
        }
    }

    void UpdateBuoyancy()
    {
        float avgWaveY = 0f;
        float frontWave = 0f, backWave = 0f, leftWave = 0f, rightWave = 0f;
        int count = 0;

        foreach (var kv in blocks)
        {
            Vector3 worldPos = kv.Value.transform.position;
            float waveY = Ocean.GetWaveHeight(worldPos.x, worldPos.z);
            avgWaveY += waveY;
            count++;

            if (kv.Key.y > 0) frontWave += waveY; else backWave += waveY;
            if (kv.Key.x > 0) rightWave += waveY; else leftWave += waveY;
        }

        if (count == 0) return;
        avgWaveY /= count;

        float targetY = avgWaveY + 0.15f;
        float currentY = transform.position.y;
        float springForce = (targetY - currentY) * 5f;
        bobVelocity += springForce * Time.deltaTime;
        bobVelocity *= 0.95f;
        float newY = currentY + bobVelocity * Time.deltaTime;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        float tiltTargetX = (frontWave - backWave) * 2f;
        float tiltTargetZ = (leftWave - rightWave) * 2f;
        tiltX = Mathf.Lerp(tiltX, Mathf.Clamp(tiltTargetX, -5f, 5f), Time.deltaTime * 3f);
        tiltZ = Mathf.Lerp(tiltZ, Mathf.Clamp(tiltTargetZ, -5f, 5f), Time.deltaTime * 3f);
        transform.rotation = Quaternion.Euler(tiltX, 0, tiltZ);
    }

    void UpdateBuildMode()
    {
        var cam = Camera.main;
        var ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        float waterY = transform.position.y;
        Plane waterPlane = new Plane(Vector3.up, new Vector3(0, waterY, 0));

        if (waterPlane.Raycast(ray, out float dist) && dist < 15f)
        {
            Vector3 hitPoint = ray.GetPoint(dist);

            Vector3 localPoint = transform.InverseTransformPoint(hitPoint);
            int gx = Mathf.RoundToInt(localPoint.x / 2f);
            int gz = Mathf.RoundToInt(localPoint.z / 2f);
            ghostGridPos = new Vector2Int(gx, gz);

            // Placement rules: not overlapping, must be adjacent to existing block
            ghostValid = !blocks.ContainsKey(ghostGridPos) && HasAdjacentBlock(ghostGridPos);

            Vector3 ghostWorldPos = transform.TransformPoint(new Vector3(gx * 2f, 0, gz * 2f));
            ghostBlock.transform.position = ghostWorldPos;
            ghostBlock.SetActive(true);

            var mr = ghostBlock.GetComponent<MeshRenderer>();
            mr.material.color = ghostValid
                ? new Color(0.3f, 0.9f, 0.3f, 0.4f)
                : new Color(0.9f, 0.3f, 0.3f, 0.4f);

            // Left click to place — costs 1 wood
            if (ghostValid && Input.GetMouseButtonDown(0))
            {
                var inv = RaftGame.Instance.Inv;
                if (RaftConfigTables.ConsumeBuildingCost(inv, FoundationBuildingId))
                    AddBlock(ghostGridPos);
            }
        }
        else
        {
            ghostBlock.SetActive(false);
        }
    }

    bool HasAdjacentBlock(Vector2Int pos)
    {
        return blocks.ContainsKey(pos + Vector2Int.up)
            || blocks.ContainsKey(pos + Vector2Int.down)
            || blocks.ContainsKey(pos + Vector2Int.left)
            || blocks.ContainsKey(pos + Vector2Int.right);
    }

    public int BlockCount => blocks.Count;

    public static bool IsPlaceableItem(ItemType type)
    {
        return type == ItemType.Planter || type == ItemType.WaterPurifier || type == ItemType.StorageBox;
    }

    public static int GetBuildingIdForItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Planter: return 2;
            case ItemType.WaterPurifier: return 3;
            case ItemType.StorageBox: return 4;
            default: return 0;
        }
    }

    void UpdatePlacementMode(ItemType selectedType)
    {
        var cam = Camera.main;
        var ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            var block = hit.collider.GetComponent<RaftBlock>();
            if (block == null) block = hit.collider.GetComponentInParent<RaftBlock>();

            if (block != null && !block.HasBuilding)
            {
                Vector3 ghostPos = block.transform.position + new Vector3(0, 0.5f, 0);
                ghostBlock.transform.position = ghostPos;
                ghostBlock.transform.localScale = new Vector3(1.0f, 0.5f, 1.0f);
                ghostBlock.SetActive(true);

                var mr = ghostBlock.GetComponent<MeshRenderer>();
                mr.material.color = new Color(0.3f, 0.9f, 0.3f, 0.4f);

                if (Input.GetMouseButtonDown(0))
                {
                    var inv = RaftGame.Instance.Inv;
                    int buildingId = GetBuildingIdForItem(selectedType);
                    if (RaftConfigTables.ConsumeBuildingCost(inv, buildingId))
                    {
                        SpawnBuilding(block, buildingId, selectedType);
                    }
                }
            }
            else
            {
                ghostBlock.SetActive(false);
            }
        }
        else
        {
            ghostBlock.SetActive(false);
        }
    }

    void SpawnBuilding(RaftBlock block, int buildingId, ItemType itemType)
    {
        var config = RaftConfigTables.GetBuildingConfig(buildingId);
        string displayName = config != null ? config.displayName : Inventory.GetItemName(itemType);

        var buildingGo = new GameObject("Building_" + displayName);

        // Visual mesh
        var mf = buildingGo.AddComponent<MeshFilter>();
        mf.mesh = RaftGame.Instance.CubeMesh;
        var mr = buildingGo.AddComponent<MeshRenderer>();

        // Collider for interaction raycast
        var col = buildingGo.AddComponent<BoxCollider>();
        col.size = Vector3.one;

        switch (itemType)
        {
            case ItemType.Planter:
                buildingGo.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
                mr.material = ProceduralMeshUtil.CreateMaterial(new Color(0.45f, 0.3f, 0.15f));
                buildingGo.AddComponent<PlacedPlanter>();
                break;
            case ItemType.WaterPurifier:
                buildingGo.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                mr.material = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.75f, 0.85f));
                buildingGo.AddComponent<PlacedWaterPurifier>();
                break;
            case ItemType.StorageBox:
                buildingGo.transform.localScale = new Vector3(1.0f, 0.6f, 1.0f);
                mr.material = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.35f, 0.1f));
                buildingGo.AddComponent<PlacedStorageBox>();
                break;
        }

        block.SetBuilding(buildingId, buildingGo);

        if (RaftGame.Instance.UI != null)
            RaftGame.Instance.UI.ShowToast("\u5df2\u653e\u7f6e " + displayName);
    }
}
