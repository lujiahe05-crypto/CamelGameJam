using UnityEngine;
using System;
using System.Collections.Generic;

public class GameJamBuildingPlacer : MonoBehaviour
{
    public bool IsPlacing { get; private set; }
    public event Action OnPlacementEnd;

    Transform sceneRoot;
    Transform playerTransform;
    GameJamBuildPlaceUI placeUI;

    string currentItemId;
    GameJamBuildingDef currentDef;
    GameObject previewGo;
    GameObject gridMarkersGo;
    Renderer[] previewRenderers;
    List<Renderer> markerRenderers = new List<Renderer>();
    Material greenTransMat;
    Material redTransMat;
    Material greenMarkerMat;
    Material redMarkerMat;
    Mesh chevronMesh;

    int rotationStep;
    bool canPlace;

    const int GroundLayer = 8;
    const float CellSize = 1f;

    public void Init(Transform sceneRoot, Transform player)
    {
        this.sceneRoot = sceneRoot;
        this.playerTransform = player;

        placeUI = gameObject.AddComponent<GameJamBuildPlaceUI>();
        placeUI.Init();

        greenTransMat = GameJamBuildingDB.CreateTransparentMat(new Color(0.2f, 0.9f, 0.3f, 0.35f));
        redTransMat = GameJamBuildingDB.CreateTransparentMat(new Color(0.9f, 0.2f, 0.15f, 0.35f));
        greenMarkerMat = GameJamBuildingDB.CreateTransparentMat(new Color(0.3f, 1f, 0.4f, 0.8f));
        redMarkerMat = GameJamBuildingDB.CreateTransparentMat(new Color(1f, 0.3f, 0.25f, 0.8f));
        chevronMesh = GameJamBuildingDB.CreateChevronMesh();
    }

    public void EnterPlaceMode(string itemId)
    {
        if (IsPlacing) ExitPlaceMode(false);

        currentDef = GameJamBuildingDB.Get(itemId);
        if (currentDef == null) return;

        currentItemId = itemId;
        rotationStep = 0;
        canPlace = false;
        IsPlacing = true;

        CreatePreview();
        CreateGridMarkers();
        placeUI.Show();

    }

    public void ExitPlaceMode(bool placed)
    {
        IsPlacing = false;

        if (previewGo != null) { Destroy(previewGo); previewGo = null; }
        if (gridMarkersGo != null) { Destroy(gridMarkersGo); gridMarkersGo = null; }
        previewRenderers = null;
        markerRenderers.Clear();

        placeUI.Hide();
        OnPlacementEnd?.Invoke();
    }

    void Update()
    {
        if (!IsPlacing) return;

        UpdatePreviewPosition();
        ValidatePlacement();
        UpdateVisuals();

        if (Input.GetKeyDown(KeyCode.T))
            Rotate();

        if (Input.GetMouseButtonDown(0) && canPlace)
            PlaceBuilding();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            ExitPlaceMode(false);
    }

    void CreatePreview()
    {
        previewGo = GameJamBuildingDB.CreateBuildingMesh(currentItemId);
        previewGo.name = "BuildingPreview";

        previewRenderers = previewGo.GetComponentsInChildren<Renderer>();
        foreach (var r in previewRenderers)
            r.material = greenTransMat;

        foreach (var col in previewGo.GetComponentsInChildren<Collider>())
            Destroy(col);
    }

    void CreateGridMarkers()
    {
        gridMarkersGo = new GameObject("GridMarkers");
        markerRenderers.Clear();

        int gw = currentDef.gridW;
        int gh = currentDef.gridH;
        float halfW = gw * CellSize * 0.5f;
        float halfH = gh * CellSize * 0.5f;

        for (int x = 0; x <= gw; x++)
        for (int z = 0; z <= gh; z++)
        {
            bool onEdge = (x == 0 || x == gw || z == 0 || z == gh);
            if (!onEdge) continue;

            float px = x * CellSize - halfW;
            float pz = z * CellSize - halfH;

            Vector3 toCenter = new Vector3(-px, 0, -pz);
            if (toCenter.sqrMagnitude < 0.001f) continue;
            float angle = Mathf.Atan2(-toCenter.x, -toCenter.z) * Mathf.Rad2Deg;

            var marker = new GameObject("Chevron");
            marker.transform.SetParent(gridMarkersGo.transform);
            marker.transform.localPosition = new Vector3(px, 0.02f, pz);
            marker.transform.localRotation = Quaternion.Euler(0, angle, 0);

            var mf = marker.AddComponent<MeshFilter>();
            mf.mesh = chevronMesh;
            var mr = marker.AddComponent<MeshRenderer>();
            mr.material = greenMarkerMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            markerRenderers.Add(mr);
        }
    }

    void UpdatePreviewPosition()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 200f, 1 << GroundLayer))
        {
            float x = Mathf.Round(hit.point.x / CellSize) * CellSize;
            float z = Mathf.Round(hit.point.z / CellSize) * CellSize;
            var snapped = new Vector3(x, hit.point.y, z);

            previewGo.transform.position = snapped;
            previewGo.transform.rotation = Quaternion.Euler(0, rotationStep * 90f, 0);

            gridMarkersGo.transform.position = snapped;
            gridMarkersGo.transform.rotation = Quaternion.Euler(0, rotationStep * 90f, 0);
        }
    }

    void ValidatePlacement()
    {
        canPlace = true;

        if (previewGo == null) { canPlace = false; return; }

        var pos = previewGo.transform.position;
        var rot = previewGo.transform.rotation;

        int gw = currentDef.gridW;
        int gh = currentDef.gridH;

        if (rotationStep % 2 != 0)
        {
            gw = currentDef.gridH;
            gh = currentDef.gridW;
        }

        float hw = gw * CellSize * 0.5f;
        float hh = gh * CellSize * 0.5f;
        float checkY = currentDef.height * 0.5f;

        // ground coverage check
        float step = CellSize * 0.5f;
        for (float cx = -hw + step; cx <= hw - step + 0.01f; cx += step)
        {
            for (float cz = -hh + step; cz <= hh - step + 0.01f; cz += step)
            {
                var worldPt = pos + rot * new Vector3(cx, 0, cz);
                var rayOrigin = worldPt + Vector3.up * 5f;
                if (!Physics.Raycast(rayOrigin, Vector3.down, 10f, 1 << GroundLayer))
                {
                    canPlace = false;
                    return;
                }
            }
        }

        // overlap check
        var center = pos + Vector3.up * checkY;
        var halfExtents = new Vector3(hw - 0.05f, checkY - 0.05f, hh - 0.05f);
        int mask = ~((1 << GroundLayer) | (1 << 2));
        var overlaps = Physics.OverlapBox(center, halfExtents, rot, mask);
        foreach (var col in overlaps)
        {
            if (col.transform == playerTransform) continue;
            if (col.transform.IsChildOf(playerTransform)) continue;
            canPlace = false;
            return;
        }
    }

    void UpdateVisuals()
    {
        var mat = canPlace ? greenTransMat : redTransMat;
        var markerMat = canPlace ? greenMarkerMat : redMarkerMat;

        if (previewRenderers != null)
        {
            foreach (var r in previewRenderers)
                if (r != null) r.sharedMaterial = mat;
        }

        foreach (var r in markerRenderers)
            if (r != null) r.sharedMaterial = markerMat;
    }

    void Rotate()
    {
        rotationStep = (rotationStep + 1) % 4;
    }

    void PlaceBuilding()
    {
        var pos = previewGo.transform.position;
        var rot = previewGo.transform.rotation;

        var building = GameJamBuildingDB.CreateBuildingMesh(currentItemId);
        building.name = currentItemId;
        building.transform.SetParent(sceneRoot);
        building.transform.position = pos;
        building.transform.rotation = rot;

        int gw = currentDef.gridW;
        int gh = currentDef.gridH;
        if (rotationStep % 2 != 0) { gw = currentDef.gridH; gh = currentDef.gridW; }

        var bc = building.AddComponent<BoxCollider>();
        bc.size = new Vector3(gw * CellSize, currentDef.height, gh * CellSize);
        bc.center = new Vector3(0, currentDef.height * 0.5f, 0);

        if (GameJamMachineDB.IsMachine(currentItemId))
            building.AddComponent<GameJamMachine>().Init(currentItemId);

        if (currentItemId == "储物箱")
            building.AddComponent<GameJamStorageBox>();

        string placedItemId = currentItemId;
        ExitPlaceMode(true);

        var inv = GetComponent<GameJamInventory>();
        if (inv != null) inv.Remove(placedItemId, 1);
    }

    public void Cleanup()
    {
        if (IsPlacing) ExitPlaceMode(false);
        if (placeUI != null) placeUI.Cleanup();
    }
}
