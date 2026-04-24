using System.Collections.Generic;
using UnityEngine;

public class GameJamPlanter : MonoBehaviour
{
    enum PlanterState
    {
        Empty,
        Growing,
        Harvestable
    }

    readonly List<GameObject> stageVisuals = new List<GameObject>();

    PlanterState state = PlanterState.Empty;
    float growthTimer;
    int stageIndex = -1;
    Transform cropRoot;

    GameJamCropDef Crop => GameJamCropDB.DefaultCrop;

    void Awake()
    {
        cropRoot = new GameObject("CropRoot").transform;
        cropRoot.SetParent(transform, false);
        cropRoot.localPosition = new Vector3(0f, 0.15f, 0f);
    }

    void Update()
    {
        if (state != PlanterState.Growing)
            return;

        growthTimer = Mathf.Max(0f, growthTimer - Time.deltaTime);
        RefreshVisual();
        if (growthTimer <= 0f)
        {
            state = PlanterState.Harvestable;
            RefreshVisual(force: true);
        }
    }

    public string GetInteractionHint(GameJamInventory inventory)
    {
        switch (state)
        {
            case PlanterState.Empty:
                int seedCount = inventory != null ? inventory.Model.GetTotalCount(Crop.seedItemId) : 0;
                return seedCount > 0
                    ? $"[E] Plant {Crop.displayName}"
                    : $"Need {Crop.seedItemId}";
            case PlanterState.Growing:
                return $"[E] {Crop.displayName} Growing ({Mathf.CeilToInt(growthTimer)}s)";
            case PlanterState.Harvestable:
                return $"[E] Harvest {Crop.displayName}";
            default:
                return null;
        }
    }

    public void Interact(GameJamInventory inventory)
    {
        if (inventory == null)
            return;

        switch (state)
        {
            case PlanterState.Empty:
                if (!inventory.Remove(Crop.seedItemId, 1))
                {
                    Toast.ShowToast($"Need {Crop.seedItemId}");
                    return;
                }

                growthTimer = Crop.growDuration;
                state = PlanterState.Growing;
                stageIndex = -1;
                RefreshVisual(force: true);
                Toast.ShowToast($"Planted {Crop.displayName}");
                break;

            case PlanterState.Harvestable:
                if (!inventory.Model.CanAddItems(Crop.harvestRewards))
                {
                    Toast.ShowToast("Bag is full.");
                    return;
                }

                inventory.AddRange(Crop.harvestRewards);
                state = PlanterState.Empty;
                growthTimer = 0f;
                stageIndex = -1;
                RefreshVisual(force: true);
                Toast.ShowToast($"Harvested {Crop.displayName}");
                break;
        }
    }

    public bool CanDismantle()
    {
        return true;
    }

    void RefreshVisual(bool force = false)
    {
        int targetStage = -1;
        if (state != PlanterState.Empty && Crop.stages != null)
        {
            float progress = state == PlanterState.Harvestable
                ? 1f
                : Mathf.Clamp01(1f - growthTimer / Crop.growDuration);

            for (int i = 0; i < Crop.stages.Length; i++)
            {
                if (progress >= Crop.stages[i].progress)
                    targetStage = i;
            }
        }

        if (!force && targetStage == stageIndex)
            return;

        stageIndex = targetStage;
        RebuildVisual();
    }

    void RebuildVisual()
    {
        ClearVisual();
        if (stageIndex < 0 || Crop.stages == null || stageIndex >= Crop.stages.Length)
            return;

        var stage = Crop.stages[stageIndex];
        var visual = GameJamArtLoader.InstantiatePrefab(stage.prefabPath) ?? CreateFallbackVisual();
        visual.name = $"CropStage_{stageIndex}";
        visual.transform.SetParent(cropRoot, false);
        visual.transform.localPosition = stage.localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = stage.localScale;
        AlignVisualToStageAnchor(visual, stage.localPosition);

        foreach (var collider in visual.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        stageVisuals.Add(visual);
    }

    void ClearVisual()
    {
        for (int i = 0; i < stageVisuals.Count; i++)
        {
            if (stageVisuals[i] != null)
                Destroy(stageVisuals[i]);
        }

        stageVisuals.Clear();
    }

    void AlignVisualToStageAnchor(GameObject visual, Vector3 localAnchor)
    {
        if (visual == null || cropRoot == null)
            return;

        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 actualBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 desiredBottomCenter = cropRoot.TransformPoint(localAnchor);
        Vector3 delta = desiredBottomCenter - actualBottomCenter;
        if (delta.sqrMagnitude > 0.000001f)
            visual.transform.position += delta;
    }

    static GameObject CreateFallbackVisual()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.GetComponent<Renderer>().sharedMaterial = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.75f, 0.3f));
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        return go;
    }

    void OnDestroy()
    {
        ClearVisual();
    }
}
