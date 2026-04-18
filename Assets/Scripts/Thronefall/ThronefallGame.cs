using UnityEngine;

public class ThronefallGame : MonoBehaviour
{
    public static ThronefallGame Instance { get; private set; }
    public System.Action OnReturnToLobby;

    public enum GamePhase { Day, Night, GameOver }
    public GamePhase CurrentPhase { get; private set; }
    public int CurrentDay { get; private set; }
    public int Coins { get; set; }

    GameObject rootContainer;
    GameObject canvasGo;

    public ThronefallPlayer Player { get; private set; }
    public ThronefallCamera Cam { get; private set; }
    public ThronefallBuildSystem BuildSys { get; private set; }
    public ThronefallCombatSystem CombatSys { get; private set; }
    public ThronefallWaveSystem WaveSys { get; private set; }
    public ThronefallCommandSystem CmdSys { get; private set; }
    public ThronefallUI UI { get; private set; }

    public Mesh CubeMesh { get; private set; }
    public Material PlayerMat { get; private set; }
    public Material BuildNodeMat { get; private set; }
    public Material BuildingMat { get; private set; }
    public Material WallMat { get; private set; }
    public Material EnemyMat { get; private set; }
    public Material EnemyOrcMat { get; private set; }
    public Material EnemySkeletonMat { get; private set; }
    public Material GroundMat { get; private set; }
    public Material BaseMat { get; private set; }
    public Material TowerMat { get; private set; }
    public Material ProjectileMat { get; private set; }
    public Material SoulMat { get; private set; }
    public Material SpearMat { get; private set; }
    public Material BowMat { get; private set; }
    public Material ThrustTrailMat { get; private set; }
    public Material HouseMat { get; private set; }
    public Material BarracksMat { get; private set; }
    public Material AllyMat { get; private set; }
    public Material ArcherAllyMat { get; private set; }
    public Material KnightAllyMat { get; private set; }
    public Material SelectionRingMat { get; private set; }

    void Awake()
    {
        Instance = this;
        rootContainer = new GameObject("ThronefallRoot");
    }

    void Start()
    {
        ThronefallConfigTables.Reload();
        CreateSharedAssets();
        SetupCamera();
        SetupLighting();
        CreateGround();
        CreatePlayer();
        CreateSystems();
        CreateUI();
        SetupInitialWorld();

        Coins = ThronefallConfigTables.WaveTableData.startingCoins;
        StartDay(1);
    }

    void CreateSharedAssets()
    {
        CubeMesh = ProceduralMeshUtil.CreateCube();

        PlayerMat = ProceduralMeshUtil.CreateMaterial(new Color(0.2f, 0.4f, 0.9f));
        BuildNodeMat = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.9f, 0.3f, 0.4f), true);
        BuildingMat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.6f, 0.65f));
        WallMat = ProceduralMeshUtil.CreateMaterial(new Color(0.45f, 0.45f, 0.5f));
        EnemyMat = ProceduralMeshUtil.CreateMaterial(new Color(0.9f, 0.2f, 0.15f));
        EnemyOrcMat = ProceduralMeshUtil.CreateMaterial(new Color(0.4f, 0.7f, 0.2f));
        EnemySkeletonMat = ProceduralMeshUtil.CreateMaterial(new Color(0.85f, 0.85f, 0.8f));
        GroundMat = ProceduralMeshUtil.CreateMaterial(new Color(0.25f, 0.5f, 0.2f));
        BaseMat = ProceduralMeshUtil.CreateMaterial(new Color(0.9f, 0.75f, 0.3f));
        TowerMat = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.35f, 0.2f));
        ProjectileMat = ProceduralMeshUtil.CreateMaterial(new Color(1f, 0.9f, 0.2f));
        SoulMat = ProceduralMeshUtil.CreateMaterial(new Color(1f, 1f, 1f, 0.4f), true);
        SpearMat = ProceduralMeshUtil.CreateMaterial(new Color(0.35f, 0.35f, 0.4f));
        BowMat = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.3f, 0.15f));
        ThrustTrailMat = ProceduralMeshUtil.CreateMaterial(new Color(0.5f, 0.7f, 1f, 0.5f), true);
        HouseMat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.45f, 0.25f));
        BarracksMat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.2f, 0.15f));
        AllyMat = ProceduralMeshUtil.CreateMaterial(new Color(0.2f, 0.7f, 0.6f));
        ArcherAllyMat = ProceduralMeshUtil.CreateMaterial(new Color(0.3f, 0.8f, 0.3f));
        KnightAllyMat = ProceduralMeshUtil.CreateMaterial(new Color(0.4f, 0.3f, 0.8f));
        SelectionRingMat = ProceduralMeshUtil.CreateMaterial(new Color(1f, 0.9f, 0.2f, 0.5f), true);
    }

    void SetupCamera()
    {
        var cam = Camera.main;
        cam.transform.SetParent(null);
        cam.orthographic = false;
        cam.fieldOfView = 45;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 500f;
        cam.backgroundColor = new Color(0.5f, 0.75f, 0.95f);
        cam.clearFlags = CameraClearFlags.SolidColor;

        var camGo = cam.gameObject;
        Cam = camGo.AddComponent<ThronefallCamera>();
    }

    void SetupLighting()
    {
        var lightGo = new GameObject("Sun");
        lightGo.transform.SetParent(rootContainer.transform);
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;

        RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }

    void CreateGround()
    {
        var groundMesh = ProceduralMeshUtil.CreatePlane(80, 80, 1, 1);
        var ground = ProceduralMeshUtil.CreatePrimitive("Ground", groundMesh, GroundMat, rootContainer.transform);
        ground.transform.position = new Vector3(0, -0.05f, 0);
        var col = ground.AddComponent<BoxCollider>();
        col.size = new Vector3(80, 0.1f, 80);
        col.center = Vector3.zero;
    }

    void CreatePlayer()
    {
        var playerGo = new GameObject("Player");
        playerGo.transform.SetParent(rootContainer.transform);
        playerGo.transform.position = new Vector3(0, 0, -5);
        Player = playerGo.AddComponent<ThronefallPlayer>();
    }

    void CreateSystems()
    {
        var combatGo = new GameObject("CombatSystem");
        combatGo.transform.SetParent(rootContainer.transform);
        CombatSys = combatGo.AddComponent<ThronefallCombatSystem>();

        var buildGo = new GameObject("BuildSystem");
        buildGo.transform.SetParent(rootContainer.transform);
        BuildSys = buildGo.AddComponent<ThronefallBuildSystem>();

        var waveGo = new GameObject("WaveSystem");
        waveGo.transform.SetParent(rootContainer.transform);
        WaveSys = waveGo.AddComponent<ThronefallWaveSystem>();

        var cmdGo = new GameObject("CommandSystem");
        cmdGo.transform.SetParent(rootContainer.transform);
        CmdSys = cmdGo.AddComponent<ThronefallCommandSystem>();
    }

    void CreateUI()
    {
        canvasGo = new GameObject("ThronefallCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        UI = canvasGo.AddComponent<ThronefallUI>();
    }

    void SetupInitialWorld()
    {
        // Castle Center (main base)
        var baseConfig = ThronefallConfigTables.GetBuildingConfig(3);
        if (baseConfig != null)
        {
            var baseGo = new GameObject("CastleCenter");
            baseGo.transform.SetParent(rootContainer.transform);
            baseGo.transform.position = Vector3.zero;
            var building = baseGo.AddComponent<ThronefallBuilding>();
            building.Init(baseConfig, true);
        }

        // Wall nodes (inner ring)
        BuildSys.CreateBuildNode(new Vector3(6, 0, 0), 2, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(-6, 0, 0), 2, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(0, 0, 6), 2, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(0, 0, -6), 2, rootContainer.transform);

        // Tower nodes (outer corners)
        BuildSys.CreateBuildNode(new Vector3(10, 0, 10), 1, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(-10, 0, -10), 1, rootContainer.transform);

        // House nodes (behind castle)
        BuildSys.CreateBuildNode(new Vector3(-5, 0, -4), 4, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(5, 0, -4), 4, rootContainer.transform);

        // Barracks nodes
        BuildSys.CreateBuildNode(new Vector3(0, 0, -8), 5, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(-8, 0, -6), 6, rootContainer.transform);
        BuildSys.CreateBuildNode(new Vector3(8, 0, -6), 7, rootContainer.transform);
    }

    public void StartDay(int day)
    {
        CurrentPhase = GamePhase.Day;
        CurrentDay = day;

        // Dawn recovery
        BuildSys.DawnRecover();

        // Economic production + daily base coins (not on day 1)
        if (day > 1)
        {
            Coins += BuildSys.CalculateDawnIncome();
            Coins += ThronefallConfigTables.WaveTableData.dailyBaseCoins;
        }

        BuildSys.SetBuildingEnabled(true);

        var waveConfig = ThronefallConfigTables.GetWaveConfig(day);
        if (waveConfig == null)
        {
            var lastWave = ThronefallConfigTables.WaveTableData.waves;
            if (lastWave != null && lastWave.Length > 0)
                waveConfig = lastWave[lastWave.Length - 1];
        }

        if (waveConfig != null && UI != null)
            UI.CreateWaveWarnings(waveConfig);

        if (UI != null) UI.UpdateDayLabel();
    }

    public void StartNight()
    {
        CurrentPhase = GamePhase.Night;
        BuildSys.SetBuildingEnabled(false);
        if (UI != null)
        {
            UI.HideBuildPanel();
            UI.ClearWaveWarnings();
            UI.UpdateDayLabel();
        }

        var waveConfig = ThronefallConfigTables.GetWaveConfig(CurrentDay);
        if (waveConfig == null)
        {
            var lastWave = ThronefallConfigTables.WaveTableData.waves;
            if (lastWave != null && lastWave.Length > 0)
                waveConfig = lastWave[lastWave.Length - 1];
        }

        if (waveConfig != null)
            WaveSys.StartWave(waveConfig);
    }

    public void OnAllMonstersDead()
    {
        if (CurrentPhase == GamePhase.GameOver) return;
        StartDay(CurrentDay + 1);
    }

    public void OnBaseDead()
    {
        if (CurrentPhase == GamePhase.GameOver) return;
        CurrentPhase = GamePhase.GameOver;
        BuildSys.SetBuildingEnabled(false);
        if (UI != null)
        {
            UI.ShowGameOver();
            UI.ClearWaveWarnings();
        }
    }

    void Update()
    {
        // Block input while branch panel is open (except branch panel handles its own)
        if (UI != null && UI.IsBranchPanelOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cleanup();
            OnReturnToLobby?.Invoke();
            return;
        }

        if (CurrentPhase == GamePhase.Day && Input.GetKeyDown(KeyCode.R))
        {
            StartNight();
        }
    }

    public void Cleanup()
    {
        if (UI != null && UI.IsBranchPanelOpen)
            UI.HideBranchPanel();

        Instance = null;
        if (Cam != null) Destroy(Cam);
        if (rootContainer != null) Destroy(rootContainer);
        if (canvasGo != null) Destroy(canvasGo);
        Destroy(gameObject);
    }

    public Transform RootContainer => rootContainer != null ? rootContainer.transform : null;
}
