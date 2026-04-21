using UnityEngine;

public class RaftGame : MonoBehaviour
{
    public static RaftGame Instance { get; private set; }
    public System.Action OnReturnToLobby;

    // References to all systems
    GameObject rootContainer;
    GameObject ocean;
    GameObject player;
    GameObject canvasGo;

    // Shared references
    public RaftManager RaftMgr { get; private set; }
    public Inventory Inv { get; private set; }
    public SurvivalStats Survival { get; private set; }
    public RaftUI UI { get; private set; }
    public PlayerController Player { get; private set; }

    // Shared meshes/materials
    public Mesh CubeMesh { get; private set; }
    public Material WoodMat { get; private set; }
    public Material PlasticMat { get; private set; }
    public Material CoconutMat { get; private set; }
    public Material BeetMat { get; private set; }
    public Material WaterBottleMat { get; private set; }
    public Material WaterMat { get; private set; }
    public Material SharkMat { get; private set; }
    public Material GhostMat { get; private set; }

    public const float WaterLevel = 0f;

    void Awake()
    {
        Instance = this;
        rootContainer = new GameObject("RaftGameRoot");
    }

    void Start()
    {
        CreateSharedAssets();
        SetupCamera();
        SetupLighting();
        CreateOcean();
        CreateRaft();
        CreatePlayer();
        CreateSystems();
        ApplyConfig();
        CreateUI();
        GiveStartingItems();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ApplyConfig()
    {
        RaftConfigTables.Reload();

        Survival.ApplyConfig(RaftConfigTables.GetSurvivalTable());
        RaftConfigTables.ApplyItemConfigs();
    }

    void CreateSharedAssets()
    {
        CubeMesh = ProceduralMeshUtil.CreateCube();

        WoodMat = ProceduralMeshUtil.CreateMaterial(new Color(0.55f, 0.35f, 0.15f));
        PlasticMat = ProceduralMeshUtil.CreateMaterial(new Color(0.85f, 0.85f, 0.9f));
        CoconutMat = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.65f, 0.2f));
        BeetMat = ProceduralMeshUtil.CreateMaterial(new Color(0.7f, 0.15f, 0.3f));
        WaterBottleMat = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.7f, 0.95f, 0.8f), true);
        SharkMat = ProceduralMeshUtil.CreateMaterial(new Color(0.45f, 0.5f, 0.55f));
        GhostMat = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.9f, 0.3f, 0.4f), true);

        WaterMat = ProceduralMeshUtil.CreateMaterial(new Color(0.1f, 0.35f, 0.6f, 0.7f), true);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        cam.orthographic = false;
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 300f;
        cam.backgroundColor = new Color(0.5f, 0.75f, 0.95f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void SetupLighting()
    {
        var lightGo = new GameObject("Sun");
        lightGo.transform.SetParent(rootContainer.transform);
        lightGo.transform.rotation = Quaternion.Euler(45, -30, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientLight = new Color(0.4f, 0.5f, 0.6f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    void CreateOcean()
    {
        var oceanMesh = ProceduralMeshUtil.CreatePlane(300, 300, 40, 40);
        ocean = ProceduralMeshUtil.CreatePrimitive("Ocean", oceanMesh, WaterMat, rootContainer.transform);
        ocean.transform.position = new Vector3(0, WaterLevel, 0);
        ocean.AddComponent<Ocean>();
    }

    void CreateRaft()
    {
        var raftGo = new GameObject("Raft");
        raftGo.transform.SetParent(rootContainer.transform);
        RaftMgr = raftGo.AddComponent<RaftManager>();
    }

    void CreatePlayer()
    {
        player = new GameObject("Player");
        player.transform.SetParent(rootContainer.transform);
        player.transform.position = new Vector3(0.5f, 2f, 0.5f);

        Player = player.AddComponent<PlayerController>();
        player.AddComponent<HookThrower>();
    }

    void CreateSystems()
    {
        var invGo = new GameObject("Inventory");
        invGo.transform.SetParent(rootContainer.transform);
        Inv = invGo.AddComponent<Inventory>();

        var survGo = new GameObject("Survival");
        survGo.transform.SetParent(rootContainer.transform);
        Survival = survGo.AddComponent<SurvivalStats>();

        var spawnGo = new GameObject("ResourceSpawner");
        spawnGo.transform.SetParent(rootContainer.transform);
        spawnGo.AddComponent<ResourceSpawner>();

        var sharkGo = new GameObject("Shark");
        sharkGo.transform.SetParent(rootContainer.transform);
        sharkGo.AddComponent<SharkAI>();

        var interactGo = new GameObject("InteractionSystem");
        interactGo.transform.SetParent(rootContainer.transform);
        interactGo.AddComponent<InteractionSystem>();
    }

    void CreateUI()
    {
        canvasGo = new GameObject("RaftCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        UI = canvasGo.AddComponent<RaftUI>();

        // Crafting & Storage panels (on same canvas)
        canvasGo.AddComponent<RaftCraftingUI>();
        canvasGo.AddComponent<RaftStorageUI>();
    }

    void GiveStartingItems()
    {
        Inv.Add(ItemType.Hook);
        Inv.Add(ItemType.BuildHammer);
        Inv.Add(ItemType.Wood, 5);
        // Give some starting food/drink for testing
        Inv.Add(ItemType.Beet, 2);
        Inv.Add(ItemType.WaterBottle, 2);
        Inv.SelectSlot(0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Cleanup();
            OnReturnToLobby?.Invoke();
        }
    }

    public void Cleanup()
    {
        Instance = null;
        if (rootContainer != null) Destroy(rootContainer);
        if (canvasGo != null) Destroy(canvasGo);
        Destroy(gameObject);
    }
}
