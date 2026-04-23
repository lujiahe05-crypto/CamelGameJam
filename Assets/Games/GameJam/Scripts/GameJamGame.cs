using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameJamGame : MonoBehaviour
{
    public Action OnReturnToLobby;

    GameObject sceneRoot;
    GameObject player;
    GameObject eventSystemGo;
    PortiaSettingsTable settings;

    void Start()
    {
        PortiaConfigTables.Reload();
        GameJamItemDB.Reload();
        GameJamBuildingDB.Reload();
        GameJamMachineDB.Reload();

        settings = PortiaConfigTables.SettingsTableData;
        sceneRoot = new GameObject("GameJamScene");
        BuildScene();
        SpawnPlayer();
        SetupCamera();
        SetupLight();
        SetupEventSystem();
        GiveStartingInventory();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var inv = player != null ? player.GetComponent<GameJamInventory>() : null;
            if (inv != null && inv.IsPanelOpen) return;
            var placer = player != null ? player.GetComponent<GameJamBuildingPlacer>() : null;
            if (placer != null && placer.IsPlacing) return;
            var machinePanel = player != null ? player.GetComponent<GameJamMachinePanel>() : null;
            if (machinePanel != null && machinePanel.IsOpen) return;
            Cleanup();
        }
    }

    void BuildScene()
    {
        CreateGround();
        CreateBoundaryWalls();
        CreateObstacles();
        CreateResources();
    }

    void CreateGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(sceneRoot.transform);
        ground.transform.localScale = new Vector3(settings.groundScaleX, 1f, settings.groundScaleZ);
        ground.layer = 8;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.35f, 0.55f, 0.25f);
        ground.GetComponent<Renderer>().material = mat;
    }

    void CreateBoundaryWalls()
    {
        float half = settings.boundaryHalfSize;
        float wallHeight = settings.boundaryWallHeight;
        float wallThick = settings.boundaryWallThickness;
        var wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = new Color(0.45f, 0.4f, 0.35f);

        CreateWall("WallN", new Vector3(0, wallHeight * 0.5f, half), new Vector3(half * 2f, wallHeight, wallThick), wallMat);
        CreateWall("WallS", new Vector3(0, wallHeight * 0.5f, -half), new Vector3(half * 2f, wallHeight, wallThick), wallMat);
        CreateWall("WallE", new Vector3(half, wallHeight * 0.5f, 0), new Vector3(wallThick, wallHeight, half * 2f), wallMat);
        CreateWall("WallW", new Vector3(-half, wallHeight * 0.5f, 0), new Vector3(wallThick, wallHeight, half * 2f), wallMat);
    }

    void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(sceneRoot.transform);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = mat;
    }

    void CreateObstacles()
    {
        var stoneMat = new Material(Shader.Find("Standard"));
        stoneMat.color = new Color(0.5f, 0.5f, 0.5f);

        var woodMat = new Material(Shader.Find("Standard"));
        woodMat.color = new Color(0.55f, 0.35f, 0.2f);

        var brickMat = new Material(Shader.Find("Standard"));
        brickMat.color = new Color(0.6f, 0.45f, 0.35f);

        CreateWall("Wall_A", new Vector3(8, 1.5f, 5), new Vector3(6, 3, 0.5f), brickMat);
        CreateWall("Wall_B", new Vector3(-6, 1f, -8), new Vector3(4, 2, 0.5f), brickMat);
        CreateWall("Wall_C", new Vector3(3, 1f, -12), new Vector3(0.5f, 2, 5), brickMat);

        CreateBox("Box_1", new Vector3(5, 0.5f, -3), 1f, woodMat);
        CreateBox("Box_2", new Vector3(6, 0.5f, -3), 1f, woodMat);
        CreateBox("Box_3", new Vector3(5.5f, 1.5f, -3), 1f, woodMat);
        CreateBox("Box_4", new Vector3(-10, 0.5f, 10), 1f, woodMat);

        CreateRock("Rock_1", new Vector3(-4, 0.6f, 6), new Vector3(2, 1.2f, 1.5f), stoneMat);
        CreateRock("Rock_2", new Vector3(12, 0.5f, -7), new Vector3(1.5f, 1f, 1.5f), stoneMat);
        CreateRock("Rock_3", new Vector3(-8, 0.75f, -3), new Vector3(2.5f, 1.5f, 2f), stoneMat);

        TryLoadDecorativePrefabs();
    }

    void CreateBox(string name, Vector3 pos, float size, Material mat)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(sceneRoot.transform);
        box.transform.position = pos;
        box.transform.localScale = Vector3.one * size;
        box.GetComponent<Renderer>().material = mat;
    }

    void CreateRock(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = name;
        rock.transform.SetParent(sceneRoot.transform);
        rock.transform.position = pos;
        rock.transform.localScale = scale;
        rock.GetComponent<Renderer>().material = mat;
    }

    void TryLoadDecorativePrefabs()
    {
#if UNITY_EDITOR
        var stonePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Games/GameJam/Model/interactive/stone/stone_interactive_01.prefab");
        if (stonePrefab != null)
        {
            SpawnDecor(stonePrefab, new Vector3(15, 0, 12));
            SpawnDecor(stonePrefab, new Vector3(-12, 0, -15));
        }

        var boxPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Games/GameJam/Model/itembox/objects_box_baoxiang01_Anim_Play.prefab");
        if (boxPrefab != null)
            SpawnDecor(boxPrefab, new Vector3(-15, 0, 5));
#endif
    }

    void SpawnDecor(GameObject prefab, Vector3 pos)
    {
        var go = Instantiate(prefab, pos, Quaternion.identity, sceneRoot.transform);
        foreach (var col in go.GetComponentsInChildren<Collider>())
            col.enabled = true;

        if (go.GetComponentInChildren<Collider>() == null)
        {
            var bc = go.AddComponent<BoxCollider>();
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                bc.center = go.transform.InverseTransformPoint(bounds.center);
                bc.size = bounds.size;
            }
        }
    }

    void CreateResources()
    {
        if (settings.resourceNodes != null)
        {
            foreach (var entry in settings.resourceNodes)
            {
                if (entry == null)
                    continue;

                string itemId = PortiaConfigTables.GetPrimaryResourceItemId(entry);
                if (string.IsNullOrWhiteSpace(itemId))
                    continue;

                var shape = PrimitiveType.Cube;
                if (!string.IsNullOrWhiteSpace(entry.shape) &&
                    PortiaConfigTables.TryParsePrimitiveType(entry.shape, out var parsedShape))
                {
                    shape = parsedShape;
                }

                Vector3 scale = entry.scale != null ? entry.scale.ToVector3() : Vector3.one;
                Vector3 position = entry.position != null ? entry.position.ToVector3() : Vector3.zero;
                var mat = BuildResourceMaterial(entry, itemId);
                CreateResourceNode(entry, itemId, mat, shape, scale, position);
            }
        }

        if (settings.placedMachines != null)
        {
            foreach (var entry in settings.placedMachines)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.machineId) || entry.position == null)
                    continue;

                CreatePlacedMachine(entry.machineId, entry.position.ToVector3());
            }
        }
    }

    void CreateResourceNode(
        PortiaResourceNodeConfig entry,
        string itemId,
        Material mat,
        PrimitiveType shape,
        Vector3 scale,
        Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = string.IsNullOrWhiteSpace(entry.label) ? itemId : entry.label;
        go.transform.SetParent(sceneRoot.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;

        var node = go.AddComponent<GameJamResourceNode>();
        node.resourceName = string.IsNullOrWhiteSpace(entry.label) ? itemId : entry.label;
        node.amount = Mathf.Max(1, entry.amount);
        node.drops = entry.drops;
    }

    void CreatePlacedMachine(string machineId, Vector3 pos)
    {
        var building = GameJamBuildingDB.CreateBuildingMesh(machineId);
        building.name = machineId;
        building.transform.SetParent(sceneRoot.transform);
        building.transform.position = pos;

        var bDef = GameJamBuildingDB.Get(machineId);
        if (bDef != null)
        {
            var bc = building.AddComponent<BoxCollider>();
            bc.size = new Vector3(bDef.gridW, bDef.height, bDef.gridH);
            bc.center = new Vector3(0, bDef.height * 0.5f, 0);
        }

        building.AddComponent<GameJamMachine>().Init(machineId);
    }

    void SpawnPlayer()
    {
        GameObject prefab = null;

#if UNITY_EDITOR
        prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Games/GameJam/Model/actor/Npc_Oaks.prefab");
#endif

        if (prefab != null)
        {
            player = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity, sceneRoot.transform);
        }
        else
        {
            player = ProceduralMeshUtil.CreatePrimitive("Player",
                ProceduralMeshUtil.CreateCube(0.6f, 1.6f, 0.6f),
                ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.6f, 0.9f)));
            player.transform.SetParent(sceneRoot.transform);
            player.transform.position = new Vector3(0, 0.8f, 0);
        }

        player.name = "Player";

        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.6f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.8f, 0);

        var controller = player.AddComponent<GameJamPlayerController>();
        controller.moveSpeed = settings.playerMoveSpeed;
        controller.jumpHeight = settings.playerJumpHeight;
        controller.gravity = settings.playerGravity;
        controller.turnSmoothTime = settings.playerTurnSmoothTime;

        player.AddComponent<GameJamInventory>();
        player.AddComponent<GameJamInteractionUI>();
        var interaction = player.AddComponent<GameJamInteraction>();
        interaction.interactRadius = settings.interactRadius;

        var placer = player.AddComponent<GameJamBuildingPlacer>();
        placer.Init(sceneRoot.transform, player.transform);

#if UNITY_EDITOR
        var animCtrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Games/GameJam/gamemodules/animation/animator/Anim_Medium_Oaks.controller");
        if (animCtrl != null)
        {
            var animator = player.GetComponent<Animator>();
            if (animator == null) animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = animCtrl;
        }
#endif
    }

    Material BuildResourceMaterial(PortiaResourceNodeConfig entry, string itemId)
    {
        Color color = entry.color != null ? entry.color.ToColor() : GetDefaultResourceColor(itemId);
        return ProceduralMeshUtil.CreateMaterial(color);
    }

    Color GetDefaultResourceColor(string itemId)
    {
        var itemDef = GameJamItemDB.Get(itemId);
        return itemDef != null ? itemDef.iconColor : Color.gray;
    }

    void GiveStartingInventory()
    {
        var inv = player != null ? player.GetComponent<GameJamInventory>() : null;
        if (inv == null || settings.initialInventory == null)
            return;

        foreach (var entry in settings.initialInventory)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.itemId) || entry.amount <= 0)
                continue;

            inv.Add(entry.itemId, entry.amount);
        }
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }

        cam.transform.SetParent(null);
        cam.orthographic = false;
        cam.fieldOfView = 45;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.clearFlags = CameraClearFlags.Skybox;

        var follow = cam.GetComponent<GameJamCamera>();
        if (follow == null) follow = cam.gameObject.AddComponent<GameJamCamera>();
        follow.target = player.transform;

        cam.transform.position = player.transform.position + follow.offset;
        cam.transform.LookAt(player.transform.position + Vector3.up);
    }

    void SetupLight()
    {
        var lightGo = new GameObject("DirectionalLight");
        lightGo.transform.SetParent(sceneRoot.transform);
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.4f, 0.45f, 0.5f);
    }

    void Cleanup()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (sceneRoot != null) Destroy(sceneRoot);
        if (eventSystemGo != null) Destroy(eventSystemGo);

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<GameJamCamera>();
            if (follow != null) Destroy(follow);
        }

        Destroy(gameObject);
        OnReturnToLobby?.Invoke();
    }

    void SetupEventSystem()
    {
        if (EventSystem.current == null)
        {
            eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();
        }
    }
}
