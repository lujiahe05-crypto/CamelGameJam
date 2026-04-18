using UnityEngine;

public class ThronefallBuilding : MonoBehaviour, ICombatEntity
{
    public enum BuildingState { Normal, Ruined }

    BuildingState state = BuildingState.Normal;
    int buildingId;
    string buildingType;
    int maxHP;
    int currentHP;
    int atk;
    int def;
    float attackRange;
    float attackInterval;
    float attackTimer;
    float arrowSpeed;
    float arcHeight;
    float aoeRadius;
    int dailyYield;
    bool isTower;
    bool isMainBase;
    bool wasRuinedLastNight;

    GameObject visual;
    Material visualMat;
    Color originalColor;
    Vector3 originalVisualScale;
    Vector3 originalVisualLocalPos;
    BoxCollider buildingCollider;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => atk;
    public int DEF => def;
    public Vector3 Position => transform.position;
    public bool IsAlive => state == BuildingState.Normal && currentHP > 0;
    public bool IsMainBase => isMainBase;
    public int BuildingId => buildingId;
    public string BuildingType => buildingType;
    public int DailyYield => dailyYield;
    public bool IsRuined => state == BuildingState.Ruined;
    public bool WasRuinedLastNight { get => wasRuinedLastNight; set => wasRuinedLastNight = value; }

    public void MarkDawnProcessed() { wasRuinedLastNight = false; }

    public void Init(TFBuildingConfig config, bool isBase)
    {
        buildingId = config.buildingId;
        buildingType = config.buildingType ?? "";
        maxHP = config.maxHP;
        currentHP = config.maxHP;
        atk = config.atk;
        def = config.def;
        attackRange = config.attackRange;
        attackInterval = config.attackInterval;
        arrowSpeed = config.arrowSpeed;
        arcHeight = config.arcHeight;
        aoeRadius = config.aoeRadius;
        dailyYield = config.dailyYield;
        isTower = buildingType == "tower";
        isMainBase = isBase;
        state = BuildingState.Normal;
        wasRuinedLastNight = false;

        var game = ThronefallGame.Instance;
        if (game == null) return;

        CreateVisual(game, config);

        originalColor = visualMat.color;
        originalVisualScale = visual.transform.localScale;
        originalVisualLocalPos = visual.transform.localPosition;

        buildingCollider = gameObject.AddComponent<BoxCollider>();
        buildingCollider.center = visual.transform.localPosition;
        buildingCollider.size = visual.transform.localScale;

        game.CombatSys.RegisterEntity(this);
        if (isBase)
            game.CombatSys.SetMainBase(this);
    }

    void CreateVisual(ThronefallGame game, TFBuildingConfig config)
    {
        Material mat;
        Vector3 localPos;
        Vector3 scale;

        switch (config.buildingId)
        {
            case 10: // Longbow Tower
                mat = new Material(game.TowerMat);
                mat.color = new Color(0.45f, 0.3f, 0.15f);
                localPos = new Vector3(0, 2.5f, 0);
                scale = new Vector3(1f, 5f, 1f);
                break;
            case 11: // Fire Oil Tower
                mat = new Material(game.TowerMat);
                mat.color = new Color(0.55f, 0.25f, 0.15f);
                localPos = new Vector3(0, 1.75f, 0);
                scale = new Vector3(1.5f, 3.5f, 1.5f);
                break;
            case 20: // Fortified Wall
                mat = new Material(game.WallMat);
                mat.color = new Color(0.5f, 0.5f, 0.55f);
                localPos = new Vector3(0, 1.5f, 0);
                scale = new Vector3(3.5f, 3f, 1.2f);
                break;
            case 40: // Manor
                mat = new Material(game.BuildingMat);
                mat.color = new Color(0.65f, 0.5f, 0.3f);
                localPos = new Vector3(0, 1.25f, 0);
                scale = new Vector3(2.5f, 2.5f, 2.5f);
                break;
            default:
                switch (buildingType)
                {
                    case "base":
                        mat = new Material(game.BaseMat);
                        localPos = new Vector3(0, 1.5f, 0);
                        scale = new Vector3(3f, 3f, 3f);
                        break;
                    case "tower":
                        mat = new Material(game.TowerMat);
                        localPos = new Vector3(0, 2f, 0);
                        scale = new Vector3(1.2f, 4f, 1.2f);
                        break;
                    case "wall":
                        mat = new Material(game.WallMat);
                        localPos = new Vector3(0, 1f, 0);
                        scale = new Vector3(3f, 2f, 1f);
                        break;
                    case "economic":
                        mat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.45f, 0.25f));
                        localPos = new Vector3(0, 1f, 0);
                        scale = new Vector3(2f, 2f, 2f);
                        break;
                    case "barracks":
                        mat = ProceduralMeshUtil.CreateMaterial(new Color(0.6f, 0.2f, 0.15f));
                        localPos = new Vector3(0, 1.25f, 0);
                        scale = new Vector3(2f, 2.5f, 2f);
                        break;
                    default:
                        mat = new Material(game.BuildingMat);
                        localPos = new Vector3(0, 1f, 0);
                        scale = new Vector3(2f, 2f, 2f);
                        break;
                }
                break;
        }

        visualMat = mat;
        visual = ProceduralMeshUtil.CreatePrimitive("Visual", game.CubeMesh, visualMat, transform);
        visual.transform.localPosition = localPos;
        visual.transform.localScale = scale;
    }

    void Update()
    {
        if (state == BuildingState.Ruined) return;
        if (!isTower) return;

        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Night) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0) return;

        var target = game.CombatSys.FindNearestEnemy(Position, attackRange);
        if (target == null) return;

        attackTimer = attackInterval;
        int dmg = ThronefallCombatSystem.CalculateDamage(atk, target.DEF);

        Color? projColor = aoeRadius > 0f ? (Color?)new Color(1f, 0.5f, 0.1f) : null;
        ThronefallArrowProjectile.Spawn(
            Position + Vector3.up * 3.5f,
            target.Position,
            arrowSpeed > 0 ? arrowSpeed : 15f,
            arcHeight > 0 ? arcHeight : 4f,
            dmg, aoeRadius, projColor);
    }

    public void EnterRuined()
    {
        if (state == BuildingState.Ruined) return;
        state = BuildingState.Ruined;
        wasRuinedLastNight = true;
        currentHP = 0;

        if (visualMat != null)
            visualMat.color = new Color(0.25f, 0.25f, 0.25f);

        if (visual != null)
        {
            var s = originalVisualScale;
            visual.transform.localScale = new Vector3(s.x, s.y * 0.5f, s.z);
            visual.transform.localPosition = new Vector3(
                originalVisualLocalPos.x,
                originalVisualLocalPos.y * 0.5f,
                originalVisualLocalPos.z);
        }

        if (buildingCollider != null)
            buildingCollider.enabled = false;

        var game = ThronefallGame.Instance;
        if (game != null)
            game.CombatSys.UnregisterEntity(this);
    }

    public void RecoverFromRuined()
    {
        if (state != BuildingState.Ruined) return;
        state = BuildingState.Normal;
        currentHP = maxHP;

        if (visualMat != null)
            visualMat.color = originalColor;

        if (visual != null)
        {
            visual.transform.localScale = originalVisualScale;
            visual.transform.localPosition = originalVisualLocalPos;
        }

        if (buildingCollider != null)
        {
            buildingCollider.enabled = true;
            buildingCollider.center = originalVisualLocalPos;
            buildingCollider.size = originalVisualScale;
        }

        var game = ThronefallGame.Instance;
        if (game != null)
            game.CombatSys.RegisterEntity(this);
    }

    public void TakeDamage(int damage)
    {
        if (state == BuildingState.Ruined) return;
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (visualMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            visualMat.color = Color.Lerp(Color.red, originalColor, ratio);
        }

        if (currentHP <= 0)
        {
            if (isMainBase)
                Die();
            else
                EnterRuined();
        }
    }

    public void Die()
    {
        var game = ThronefallGame.Instance;
        if (game != null)
        {
            game.CombatSys.UnregisterEntity(this);
            if (isMainBase)
                game.OnBaseDead();
        }
        Destroy(gameObject);
    }

    public float GetHPRatio()
    {
        if (maxHP <= 0) return 1f;
        return (float)currentHP / maxHP;
    }
}
