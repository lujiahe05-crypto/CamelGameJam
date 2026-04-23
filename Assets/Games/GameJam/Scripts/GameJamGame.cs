using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

public class GameJamGame : MonoBehaviour
{
    public Action OnReturnToLobby;

    static readonly Vector3 SceneMainSpawnPoint = new Vector3(175.4f, 50.63f, -92.82f);

    GameObject sceneRoot;
    GameObject player;
    GameObject eventSystemGo;
    PortiaSettingsTable settings;
    bool isSceneMain;

    void Start()
    {
        PortiaConfigTables.Reload();
        GameJamItemDB.Reload();
        GameJamBuildingDB.Reload();
        GameJamMachineDB.Reload();

        settings = PortiaConfigTables.SettingsTableData;
        isSceneMain = SceneManager.GetActiveScene().name == "SceneMain";
        sceneRoot = new GameObject("GameJamScene");

        BuildScene();
        SpawnPlayer();
        SetupCamera();

        if (!isSceneMain)
            SetupLight();

        SetupEventSystem();
        GiveStartingInventory();
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
            var storagePanel = player != null ? player.GetComponent<GameJamStoragePanel>() : null;
            if (storagePanel != null && storagePanel.IsOpen) return;
            Cleanup();
        }
    }

    void BuildScene()
    {
        if (!isSceneMain)
        {
            CreateGround();
            CreateBoundaryWalls();
            CreateObstacles();
        }

        CreateResources();
        bool hasConfigNodes = settings.resourceNodes != null && settings.resourceNodes.Length > 0;
        if (!hasConfigNodes)
            CreateGroundPickups();
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
        if (settings.resourceNodes != null && settings.resourceNodes.Length > 0)
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
                string label = string.IsNullOrWhiteSpace(entry.label) ? itemId : entry.label;
                int hp = Mathf.Max(1, entry.amount);
                float respawn = (entry.amount <= 1 && entry.num <= 1) ? -1f : 120f;
                CreateResourceNode(label, mat, shape, scale, position, hp, respawn, entry.drops, Mathf.Max(0, entry.num));
            }
        }
        else
        {
            CreateDefaultResources();
        }

        if (settings.placedMachines != null && settings.placedMachines.Length > 0)
        {
            foreach (var entry in settings.placedMachines)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.machineId) || entry.position == null)
                    continue;

                CreatePlacedMachine(entry.machineId, entry.position.ToVector3());
            }
        }
        else
        {
            CreatePlacedMachine("民用熔炉", new Vector3(-2, 0, -5));
            CreatePlacedMachine("工作台", new Vector3(2, 0, -3));
        }
    }

    void CreateDefaultResources()
    {
        var stoneMat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.6f, 0.58f));
        var woodMat = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.33f, 0.15f));
        var ironMat = ProceduralMeshUtil.CreateMaterial(new Color(0.4f, 0.42f, 0.5f));
        var copperMat = ProceduralMeshUtil.CreateMaterial(new Color(0.72f, 0.45f, 0.2f));
        var sandMat = ProceduralMeshUtil.CreateMaterial(new Color(0.85f, 0.78f, 0.55f));
        var herbMat = ProceduralMeshUtil.CreateMaterial(new Color(0.2f, 0.65f, 0.3f));

        var stoneDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "石块", amount = 1, weight = 100f },
        };
        CreateResourceNode("石块", stoneMat, PrimitiveType.Sphere, new Vector3(1.2f, 0.8f, 1f),
            new Vector3(7, 0.4f, 10), 3, 120f, stoneDrops, 3);
        CreateResourceNode("石块", stoneMat, PrimitiveType.Sphere, new Vector3(1f, 0.7f, 0.9f),
            new Vector3(-3, 0.35f, 14), 3, 120f, stoneDrops, 3);
        CreateResourceNode("石块", stoneMat, PrimitiveType.Sphere, new Vector3(0.9f, 0.6f, 1.1f),
            new Vector3(16, 0.3f, -4), 3, 120f, stoneDrops, 3);
        CreateResourceNode("石块", stoneMat, PrimitiveType.Sphere, new Vector3(1.3f, 0.9f, 1.2f),
            new Vector3(-14, 0.45f, -10), 3, 120f, stoneDrops, 3);

        var woodDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "木材", amount = 1, weight = 100f },
        };
        CreateResourceNode("木材", woodMat, PrimitiveType.Cylinder, new Vector3(0.3f, 0.8f, 0.3f),
            new Vector3(10, 0.8f, 3), 2, 90f, woodDrops, 3);
        CreateResourceNode("木材", woodMat, PrimitiveType.Cylinder, new Vector3(0.35f, 0.9f, 0.35f),
            new Vector3(-9, 0.9f, 7), 2, 90f, woodDrops, 3);
        CreateResourceNode("木材", woodMat, PrimitiveType.Cylinder, new Vector3(0.25f, 0.7f, 0.25f),
            new Vector3(2, 0.7f, -18), 2, 90f, woodDrops, 3);

        var ironDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "铁矿", amount = 1, weight = 70f },
            new PortiaResourceDropConfig { itemId = "石块", amount = 1, weight = 30f },
        };
        CreateResourceNode("铁矿", ironMat, PrimitiveType.Sphere, new Vector3(0.8f, 0.8f, 0.8f),
            new Vector3(-17, 0.4f, 0), 4, 180f, ironDrops, 3);
        CreateResourceNode("铁矿", ironMat, PrimitiveType.Sphere, new Vector3(0.7f, 0.7f, 0.7f),
            new Vector3(14, 0.35f, 16), 4, 180f, ironDrops, 3);
        CreateResourceNode("铁矿", ironMat, PrimitiveType.Sphere, new Vector3(0.9f, 0.9f, 0.9f),
            new Vector3(0, 0.45f, -8), 4, 180f, ironDrops, 3);

        var copperDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "铜矿", amount = 1, weight = 70f },
            new PortiaResourceDropConfig { itemId = "石块", amount = 1, weight = 30f },
        };
        CreateResourceNode("铜矿", copperMat, PrimitiveType.Sphere, new Vector3(0.85f, 0.7f, 0.8f),
            new Vector3(8, 0.35f, -15), 3, 150f, copperDrops, 3);
        CreateResourceNode("铜矿", copperMat, PrimitiveType.Sphere, new Vector3(0.75f, 0.65f, 0.7f),
            new Vector3(-12, 0.32f, 5), 3, 150f, copperDrops, 3);
        CreateResourceNode("铜矿", copperMat, PrimitiveType.Sphere, new Vector3(0.9f, 0.75f, 0.85f),
            new Vector3(17, 0.38f, -12), 3, 150f, copperDrops, 3);

        var sandDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "沙子", amount = 3, weight = 100f },
        };
        CreateResourceNode("沙子", sandMat, PrimitiveType.Cube, new Vector3(1.2f, 0.3f, 1f),
            new Vector3(20, 0.15f, 3), 1, 60f, sandDrops);
        CreateResourceNode("沙子", sandMat, PrimitiveType.Cube, new Vector3(1f, 0.25f, 1.1f),
            new Vector3(-20, 0.12f, -12), 1, 60f, sandDrops);

        var herbDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "草药", amount = 2, weight = 100f },
        };
        CreateResourceNode("草药", herbMat, PrimitiveType.Sphere, new Vector3(0.4f, 0.5f, 0.4f),
            new Vector3(5, 0.25f, 16), 1, 45f, herbDrops);
        CreateResourceNode("草药", herbMat, PrimitiveType.Sphere, new Vector3(0.35f, 0.45f, 0.35f),
            new Vector3(-6, 0.22f, -18), 1, 45f, herbDrops);

        var buildDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "工作台", amount = 1, weight = 100f },
        };
        var benchMat = ProceduralMeshUtil.CreateMaterial(new Color(0.45f, 0.35f, 0.2f));
        CreateResourceNode("工作台", benchMat, PrimitiveType.Cube, new Vector3(0.6f, 0.6f, 0.6f),
            new Vector3(3, 0.3f, 5), 1, -1f, buildDrops);
        CreateResourceNode("工作台", benchMat, PrimitiveType.Cube, new Vector3(0.6f, 0.6f, 0.6f),
            new Vector3(-5, 0.3f, 3), 1, -1f, buildDrops);

        var boxDrops = new PortiaResourceDropConfig[] {
            new PortiaResourceDropConfig { itemId = "储物箱", amount = 1, weight = 100f },
        };
        var boxMat = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.38f, 0.2f));
        CreateResourceNode("储物箱", boxMat, PrimitiveType.Cube, new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(6, 0.25f, 8), 1, -1f, boxDrops);
        CreateResourceNode("储物箱", boxMat, PrimitiveType.Cube, new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-7, 0.25f, -6), 1, -1f, boxDrops);
    }

    void CreateResourceNode(string resName, Material mat, PrimitiveType shape,
        Vector3 scale, Vector3 pos, int hp, float respawn, PortiaResourceDropConfig[] drops, int num = 0)
    {
        var go = GameObject.CreatePrimitive(shape);
        go.name = resName;
        go.transform.SetParent(sceneRoot.transform);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;

        var node = go.AddComponent<GameJamResourceNode>();
        node.resourceName = resName;
        node.maxHp = hp;
        node.amount = hp;
        node.num = num;
        node.respawnTime = respawn;
        node.drops = drops;
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

        Vector3 spawnPos = isSceneMain ? SceneMainSpawnPoint : Vector3.zero;

        if (prefab != null)
        {
            player = Instantiate(prefab, spawnPos, Quaternion.identity, sceneRoot.transform);
        }
        else
        {
            player = ProceduralMeshUtil.CreatePrimitive("Player",
                ProceduralMeshUtil.CreateCube(0.6f, 1.6f, 0.6f),
                ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.6f, 0.9f)));
            player.transform.SetParent(sceneRoot.transform);
            player.transform.position = spawnPos;
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

        var anim = player.GetComponentInChildren<Animator>();
        if (anim != null && anim.GetComponent<GameJamAnimEventReceiver>() == null)
            anim.gameObject.AddComponent<GameJamAnimEventReceiver>();
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

    void CreateGroundPickups()
    {
        var woodMat = new Material(Shader.Find("Standard"));
        woodMat.color = new Color(0.5f, 0.33f, 0.15f);
        var stoneMat = new Material(Shader.Find("Standard"));
        stoneMat.color = new Color(0.65f, 0.63f, 0.58f);
        var ironMat = new Material(Shader.Find("Standard"));
        ironMat.color = new Color(0.45f, 0.42f, 0.48f);
        var copperPickupMat = new Material(Shader.Find("Standard"));
        copperPickupMat.color = new Color(0.72f, 0.45f, 0.2f);

        CreateWoodPickup(new Vector3(4, 0, 12), woodMat);
        CreateWoodPickup(new Vector3(-8, 0, 4), woodMat);
        CreateWoodPickup(new Vector3(12, 0, -10), woodMat);
        CreateWoodPickup(new Vector3(-15, 0, 14), woodMat);

        CreateStonePickup(new Vector3(9, 0, -6), stoneMat);
        CreateStonePickup(new Vector3(-11, 0, 11), stoneMat);
        CreateStonePickup(new Vector3(18, 0, 8), stoneMat);

        CreateIronPickup(new Vector3(-6, 0, -14), ironMat);
        CreateIronPickup(new Vector3(13, 0, 14), ironMat);
        CreateIronPickup(new Vector3(-18, 0, -6), ironMat);

        CreateCopperPickup(new Vector3(6, 0, -12), copperPickupMat);
        CreateCopperPickup(new Vector3(-14, 0, 8), copperPickupMat);
        CreateCopperPickup(new Vector3(16, 0, 6), copperPickupMat);
    }

    void CreateWoodPickup(Vector3 pos, Material mat)
    {
        var root = new GameObject("GroundPickup_木材");
        root.transform.SetParent(sceneRoot.transform);
        root.transform.position = pos;

        for (int i = 0; i < 3; i++)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = "Log";
            log.transform.SetParent(root.transform);
            float offsetX = (i - 1) * 0.25f + Random.Range(-0.05f, 0.05f);
            float offsetZ = Random.Range(-0.15f, 0.15f);
            log.transform.localPosition = new Vector3(offsetX, 0.06f, offsetZ);
            log.transform.localScale = new Vector3(0.1f, 0.35f, 0.1f);
            log.transform.localRotation = Quaternion.Euler(90, Random.Range(0f, 60f) - 30f, 0);
            log.GetComponent<Renderer>().material = mat;
            Destroy(log.GetComponent<Collider>());
        }

        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.1f, 0);
        bc.size = new Vector3(1f, 0.25f, 0.6f);

        var pickup = root.AddComponent<GameJamGroundPickup>();
        pickup.itemId = "木材";
        pickup.itemName = "木材";
        pickup.pickupAmount = 3;
        pickup.interactRange = 2.5f;
        pickup.respawnTime = 60f;

        root.AddComponent<GameJamPickupFX>();
    }

    void CreateStonePickup(Vector3 pos, Material mat)
    {
        var root = new GameObject("GroundPickup_石块");
        root.transform.SetParent(sceneRoot.transform);
        root.transform.position = pos;

        var stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stone.name = "StoneModel";
        stone.transform.SetParent(root.transform);
        stone.transform.localPosition = new Vector3(0, 0.2f, 0);
        stone.transform.localScale = new Vector3(0.6f, 0.4f, 0.55f);
        stone.GetComponent<Renderer>().material = mat;
        Destroy(stone.GetComponent<Collider>());

        var chip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chip.name = "Chip";
        chip.transform.SetParent(root.transform);
        chip.transform.localPosition = new Vector3(0.3f, 0.1f, 0.15f);
        chip.transform.localScale = new Vector3(0.25f, 0.2f, 0.25f);
        chip.GetComponent<Renderer>().material = mat;
        Destroy(chip.GetComponent<Collider>());

        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.2f, 0);
        bc.size = new Vector3(0.9f, 0.5f, 0.8f);

        var pickup = root.AddComponent<GameJamGroundPickup>();
        pickup.itemId = "石块";
        pickup.itemName = "石块";
        pickup.pickupAmount = 2;
        pickup.interactRange = 2.5f;
        pickup.respawnTime = 90f;

        root.AddComponent<GameJamPickupFX>();
    }

    void CreateIronPickup(Vector3 pos, Material mat)
    {
        var root = new GameObject("GroundPickup_铁矿");
        root.transform.SetParent(sceneRoot.transform);
        root.transform.position = pos;

        var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ore.name = "OreModel";
        ore.transform.SetParent(root.transform);
        ore.transform.localPosition = new Vector3(0, 0.18f, 0);
        ore.transform.localScale = new Vector3(0.45f, 0.35f, 0.4f);
        ore.transform.localRotation = Quaternion.Euler(0, 25, 8);
        ore.GetComponent<Renderer>().material = mat;
        Destroy(ore.GetComponent<Collider>());

        var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shard.name = "Shard";
        shard.transform.SetParent(root.transform);
        shard.transform.localPosition = new Vector3(-0.25f, 0.1f, 0.1f);
        shard.transform.localScale = new Vector3(0.2f, 0.2f, 0.18f);
        shard.transform.localRotation = Quaternion.Euler(10, -15, 5);
        shard.GetComponent<Renderer>().material = mat;
        Destroy(shard.GetComponent<Collider>());

        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.18f, 0);
        bc.size = new Vector3(0.8f, 0.4f, 0.7f);

        var pickup = root.AddComponent<GameJamGroundPickup>();
        pickup.itemId = "铁矿";
        pickup.itemName = "铁矿";
        pickup.pickupAmount = 1;
        pickup.interactRange = 2.5f;
        pickup.respawnTime = 120f;

        root.AddComponent<GameJamPickupFX>();
    }

    void CreateCopperPickup(Vector3 pos, Material mat)
    {
        var root = new GameObject("GroundPickup_铜矿");
        root.transform.SetParent(sceneRoot.transform);
        root.transform.position = pos;

        var ore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ore.name = "CopperModel";
        ore.transform.SetParent(root.transform);
        ore.transform.localPosition = new Vector3(0, 0.18f, 0);
        ore.transform.localScale = new Vector3(0.5f, 0.38f, 0.45f);
        ore.GetComponent<Renderer>().material = mat;
        Destroy(ore.GetComponent<Collider>());

        var chip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        chip.name = "Chip";
        chip.transform.SetParent(root.transform);
        chip.transform.localPosition = new Vector3(0.25f, 0.1f, -0.1f);
        chip.transform.localScale = new Vector3(0.22f, 0.18f, 0.2f);
        chip.GetComponent<Renderer>().material = mat;
        Destroy(chip.GetComponent<Collider>());

        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.18f, 0);
        bc.size = new Vector3(0.8f, 0.4f, 0.7f);

        var pickup = root.AddComponent<GameJamGroundPickup>();
        pickup.itemId = "铜矿";
        pickup.itemName = "铜矿";
        pickup.pickupAmount = 1;
        pickup.interactRange = 2.5f;
        pickup.respawnTime = 100f;

        root.AddComponent<GameJamPickupFX>();
    }
}
