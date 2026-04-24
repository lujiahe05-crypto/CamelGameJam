using System.Collections.Generic;
using UnityEngine;

public class GameJamBuildingDef
{
    public string itemId;
    public int gridW;
    public int gridH;
    public float height;
    public string prefabName;
}

public static class GameJamBuildingDB
{
    static Dictionary<string, GameJamBuildingDef> defs;

    static void Init()
    {
        if (defs != null) return;
        defs = new Dictionary<string, GameJamBuildingDef>();

        Reg(new GameJamBuildingDef { itemId = "工作台", gridW = 3, gridH = 2, height = 1.5f });
        Reg(new GameJamBuildingDef { itemId = "组装台", gridW = 3, gridH = 2, height = 1.5f });
        Reg(new GameJamBuildingDef { itemId = "民用熔炉", gridW = 2, gridH = 2, height = 1.8f });
        Reg(new GameJamBuildingDef { itemId = "熔炉", gridW = 2, gridH = 2, height = 1.8f });
        Reg(new GameJamBuildingDef { itemId = "切割机", gridW = 3, gridH = 2, height = 1.4f });
        Reg(new GameJamBuildingDef { itemId = "储物箱", gridW = 1, gridH = 1, height = 0.8f });
        Reg(new GameJamBuildingDef
        {
            itemId = GameJamCropDB.PlanterItemId,
            gridW = 2,
            gridH = 1,
            height = 1f,
            prefabPath = "Games/GameJam/assets/Model/itemmall/ItemMall_PlantBox_01.prefab"
        });

        ApplyConfigOverrides();
    }

    static void Reg(GameJamBuildingDef def) => defs[def.itemId] = def;

    public static void Reload() => defs = null;

    static void ApplyConfigOverrides()
    {
        var table = PortiaConfigTables.BuildingTableData;
        if (table == null || table.buildings == null) return;

        foreach (var entry in table.buildings)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
                continue;

            defs.TryGetValue(entry.itemId, out var existing);
            Reg(new GameJamBuildingDef
            {
                itemId = entry.itemId,
                gridW = entry.gridW > 0 ? entry.gridW : existing != null ? existing.gridW : 1,
                gridH = entry.gridH > 0 ? entry.gridH : existing != null ? existing.gridH : 1,
                height = entry.height > 0f ? entry.height : existing != null ? existing.height : 1f,
                prefabName = !string.IsNullOrWhiteSpace(entry.prefabName) ? entry.prefabName : existing != null ? existing.prefabName : null
            });
        }
    }

    public static GameJamBuildingDef Get(string itemId)
    {
        Init();
        return defs.TryGetValue(itemId, out var def) ? def : null;
    }

    public static bool IsBuilding(string itemId)
    {
        Init();
        return defs.ContainsKey(itemId);
    }

    public static bool HasConfiguredPrefab(string itemId)
    {
        Init();
        if (defs.TryGetValue(itemId, out var buildingDef) && !string.IsNullOrWhiteSpace(buildingDef.prefabName))
            return true;

        var itemDef = GameJamItemDB.Get(itemId);
        return itemDef != null && !string.IsNullOrWhiteSpace(itemDef.prefabName);
    }

    public static GameObject CreateBuildingMesh(string itemId)
    {
        string displayName = itemId;
        var buildingDef = Get(itemId);
        var itemDef = GameJamItemDB.Get(itemId);
        if (itemDef != null && !string.IsNullOrWhiteSpace(itemDef.name))
            displayName = itemDef.name;

        string prefabName = buildingDef != null && !string.IsNullOrWhiteSpace(buildingDef.prefabName)
            ? buildingDef.prefabName
            : itemDef != null ? itemDef.prefabName : null;
        var configuredPrefab = GameJamArtLoader.InstantiatePrefabByName(prefabName);
        if (configuredPrefab != null)
        {
            configuredPrefab.name = displayName;
            RemoveAllColliders(configuredPrefab);
            return configuredPrefab;
        }

        switch (displayName)
        {
            case "工作台":
            case "组装台":
                return CreateWorkbench(displayName);
            case "民用熔炉":
            case "熔炉":
                return CreateFurnace(displayName);
            case "切割机":
                return CreateCutter();
            case "储物箱":
                return CreateStorageBox();
            case "种植盆":
                return CreatePlanterBox();
            default:
                return CreateFallbackBox(displayName);
        }
    }

    static GameObject CreateWorkbench(string displayName)
    {
        var root = new GameObject(displayName);
        var mainColor = new Color(0.45f, 0.35f, 0.2f);
        var topColor = new Color(0.5f, 0.4f, 0.25f);

        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "Top";
        top.transform.SetParent(root.transform);
        top.transform.localPosition = new Vector3(0, 0.85f, 0);
        top.transform.localScale = new Vector3(2.8f, 0.15f, 1.6f);
        top.GetComponent<Renderer>().material = CreateMat(topColor);

        float legInsetX = 1.2f;
        float legInsetZ = 0.6f;
        for (int xi = -1; xi <= 1; xi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg";
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(xi * legInsetX, 0.4f, zi * legInsetZ);
            leg.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
            leg.GetComponent<Renderer>().material = CreateMat(mainColor);
        }

        var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.name = "BackPanel";
        back.transform.SetParent(root.transform);
        back.transform.localPosition = new Vector3(0, 1.2f, -0.7f);
        back.transform.localScale = new Vector3(2.8f, 0.6f, 0.08f);
        back.GetComponent<Renderer>().material = CreateMat(mainColor);

        var vice = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vice.name = "Vice";
        vice.transform.SetParent(root.transform);
        vice.transform.localPosition = new Vector3(0.8f, 1.1f, 0);
        vice.transform.localScale = new Vector3(0.4f, 0.35f, 0.3f);
        vice.GetComponent<Renderer>().material = CreateMat(new Color(0.5f, 0.5f, 0.55f));

        RemoveAllColliders(root);
        return root;
    }

    static GameObject CreateFurnace(string displayName)
    {
        var root = new GameObject(displayName);
        var stoneColor = new Color(0.55f, 0.5f, 0.45f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.55f, 0);
        body.transform.localScale = new Vector3(1.6f, 1.1f, 1.6f);
        body.GetComponent<Renderer>().material = CreateMat(stoneColor);

        var chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.name = "Chimney";
        chimney.transform.SetParent(root.transform);
        chimney.transform.localPosition = new Vector3(-0.4f, 1.4f, -0.4f);
        chimney.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
        chimney.GetComponent<Renderer>().material = CreateMat(stoneColor * 0.8f);

        var mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mouth.name = "Mouth";
        mouth.transform.SetParent(root.transform);
        mouth.transform.localPosition = new Vector3(0, 0.4f, 0.78f);
        mouth.transform.localScale = new Vector3(0.7f, 0.5f, 0.1f);
        mouth.GetComponent<Renderer>().material = CreateMat(new Color(0.15f, 0.08f, 0.05f));

        var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glow.name = "Glow";
        glow.transform.SetParent(root.transform);
        glow.transform.localPosition = new Vector3(0, 0.35f, 0.72f);
        glow.transform.localScale = new Vector3(0.5f, 0.3f, 0.05f);
        glow.GetComponent<Renderer>().material = CreateMat(new Color(0.9f, 0.4f, 0.1f));

        RemoveAllColliders(root);
        return root;
    }

    static GameObject CreateStorageBox()
    {
        var root = new GameObject("储物箱");
        var boxColor = new Color(0.5f, 0.38f, 0.2f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.3f, 0);
        body.transform.localScale = new Vector3(0.85f, 0.6f, 0.65f);
        body.GetComponent<Renderer>().material = CreateMat(boxColor);

        var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid";
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.65f, 0);
        lid.transform.localScale = new Vector3(0.9f, 0.1f, 0.7f);
        lid.GetComponent<Renderer>().material = CreateMat(boxColor * 1.1f);

        var buckle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buckle.name = "Buckle";
        buckle.transform.SetParent(root.transform);
        buckle.transform.localPosition = new Vector3(0, 0.5f, 0.33f);
        buckle.transform.localScale = new Vector3(0.15f, 0.12f, 0.04f);
        buckle.GetComponent<Renderer>().material = CreateMat(new Color(0.7f, 0.6f, 0.3f));

        RemoveAllColliders(root);
        return root;
    }

    static GameObject CreatePlanterBox()
    {
        var root = new GameObject("种植盆");
        var woodColor = new Color(0.46f, 0.31f, 0.18f);
        var soilColor = new Color(0.2f, 0.14f, 0.09f);

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        box.transform.SetParent(root.transform);
        box.transform.localPosition = new Vector3(0f, 0.24f, 0f);
        box.transform.localScale = new Vector3(1.4f, 0.48f, 0.75f);
        box.GetComponent<Renderer>().material = CreateMat(woodColor);

        var soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        soil.name = "Soil";
        soil.transform.SetParent(root.transform);
        soil.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        soil.transform.localScale = new Vector3(1.18f, 0.12f, 0.55f);
        soil.GetComponent<Renderer>().material = CreateMat(soilColor);

        RemoveAllColliders(root);
        return root;
    }

    static GameObject CreateCutter()
    {
        var root = new GameObject("切割机");
        var metalColor = new Color(0.5f, 0.5f, 0.55f);
        var woodColor = new Color(0.45f, 0.35f, 0.2f);

        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Table";
        table.transform.SetParent(root.transform);
        table.transform.localPosition = new Vector3(0, 0.45f, 0);
        table.transform.localScale = new Vector3(2.6f, 0.1f, 1.6f);
        table.GetComponent<Renderer>().material = CreateMat(woodColor);

        for (int xi = -1; xi <= 1; xi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg";
            leg.transform.SetParent(root.transform);
            leg.transform.localPosition = new Vector3(xi * 1.1f, 0.2f, zi * 0.65f);
            leg.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
            leg.GetComponent<Renderer>().material = CreateMat(metalColor);
        }

        var blade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        blade.name = "Blade";
        blade.transform.SetParent(root.transform);
        blade.transform.localPosition = new Vector3(0, 0.75f, 0);
        blade.transform.localRotation = Quaternion.Euler(90, 0, 0);
        blade.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        blade.GetComponent<Renderer>().material = CreateMat(new Color(0.6f, 0.6f, 0.65f));

        var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        guard.name = "Guard";
        guard.transform.SetParent(root.transform);
        guard.transform.localPosition = new Vector3(0, 0.9f, -0.6f);
        guard.transform.localScale = new Vector3(0.8f, 0.6f, 0.06f);
        guard.GetComponent<Renderer>().material = CreateMat(metalColor);

        var motor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        motor.name = "Motor";
        motor.transform.SetParent(root.transform);
        motor.transform.localPosition = new Vector3(-1.0f, 0.65f, 0);
        motor.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
        motor.GetComponent<Renderer>().material = CreateMat(new Color(0.35f, 0.35f, 0.38f));

        RemoveAllColliders(root);
        return root;
    }

    static GameObject CreateFallbackBox(string name)
    {
        var root = new GameObject(name);
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.5f, 0);
        body.GetComponent<Renderer>().material = CreateMat(Color.gray);
        RemoveAllColliders(root);
        return root;
    }

    static Material CreateMat(Color color)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    static void RemoveAllColliders(GameObject root)
    {
        foreach (var col in root.GetComponentsInChildren<Collider>())
            Object.Destroy(col);
    }

    public static Material CreateTransparentMat(Color color)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = color;
        return mat;
    }

    public static Mesh CreateChevronMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.04f, 0, 0.1f),
            new Vector3(0.14f, 0, 0),
            new Vector3(-0.04f, 0, -0.1f),
            new Vector3(0.04f, 0, 0.06f),
            new Vector3(0.04f, 0, -0.06f)
        };
        mesh.triangles = new int[]
        {
            0, 1, 3,
            4, 1, 2
        };
        mesh.normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };
        mesh.RecalculateBounds();
        return mesh;
    }
}
