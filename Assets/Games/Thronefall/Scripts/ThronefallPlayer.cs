using System.Collections.Generic;
using UnityEngine;

public class ThronefallBuildNodeMarker : MonoBehaviour
{
    public int nodeIndex;
}

public class ThronefallPlayer : MonoBehaviour, ICombatEntity
{
    enum HeroState { Alive, Thrusting, Dead }

    // Config
    int maxHP;
    int currentHP;
    float moveSpeed;
    float reviveTime;
    TFWeaponConfig[] weapons;
    int currentWeaponIndex;

    TFWeaponConfig CurrentWeapon => weapons[currentWeaponIndex];

    // Combat
    float attackCooldownTimer;
    float retargetTimer;
    const float RetargetInterval = 0.2f;
    ThronefallEnemy lockedTarget;

    // Skill
    float skillCooldownTimer;
    float skillCooldownTotal;

    // Thrust
    HeroState heroState;
    float thrustTimer;
    float thrustAccelTime;
    float thrustDecelTime;
    float thrustDuration;
    float thrustMaxSpeed;
    float thrustDamageMultiplier;
    Vector3 thrustDirection;
    HashSet<ThronefallEnemy> thrustHitEnemies = new HashSet<ThronefallEnemy>();

    // Death
    float reviveTimer;

    // Components
    CharacterController cc;
    ThronefallBuildNodeMarker currentNearMarker;

    // Visuals
    GameObject bodyVisual;
    GameObject horseVisual;
    GameObject soulVisual;
    GameObject spearVisual;
    GameObject bowVisual;
    Material bodyMat;

    // UI
    ThronefallHeroUI heroUI;

    // ICombatEntity
    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public int ATK => weapons != null && currentWeaponIndex < weapons.Length ? weapons[currentWeaponIndex].atk : 0;
    public int DEF => weapons != null && currentWeaponIndex < weapons.Length ? weapons[currentWeaponIndex].def : 0;
    public Vector3 Position => transform.position;
    public bool IsAlive => heroState != HeroState.Dead;

    void Start()
    {
        var game = ThronefallGame.Instance;
        var config = ThronefallConfigTables.GetHeroConfig();

        maxHP = config.maxHP;
        currentHP = maxHP;
        moveSpeed = config.moveSpeed;
        reviveTime = config.reviveTime;
        weapons = config.weapons;
        currentWeaponIndex = 0;
        heroState = HeroState.Alive;

        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1f, 0);

        bodyMat = new Material(game.PlayerMat);
        bodyVisual = ProceduralMeshUtil.CreatePrimitive("PlayerBody", game.CubeMesh, bodyMat, transform);
        bodyVisual.transform.localPosition = new Vector3(0, 1f, 0);
        bodyVisual.transform.localScale = new Vector3(0.8f, 2f, 0.8f);

        horseVisual = ProceduralMeshUtil.CreatePrimitive("Horse", game.CubeMesh, game.PlayerMat, transform);
        horseVisual.transform.localPosition = new Vector3(0, 0.5f, -0.5f);
        horseVisual.transform.localScale = new Vector3(0.6f, 1f, 1.2f);

        soulVisual = ProceduralMeshUtil.CreatePrimitive("Soul", game.CubeMesh, game.SoulMat, transform);
        soulVisual.transform.localPosition = new Vector3(0, 1.5f, 0);
        soulVisual.transform.localScale = new Vector3(1f, 1.5f, 1f);
        soulVisual.SetActive(false);

        spearVisual = ProceduralMeshUtil.CreatePrimitive("Spear", game.CubeMesh, game.SpearMat, transform);
        spearVisual.transform.localPosition = new Vector3(0.5f, 1.5f, 0.3f);
        spearVisual.transform.localScale = new Vector3(0.12f, 0.12f, 1.8f);

        bowVisual = ProceduralMeshUtil.CreatePrimitive("Bow", game.CubeMesh, game.BowMat, transform);
        bowVisual.transform.localPosition = new Vector3(-0.5f, 1.2f, 0.2f);
        bowVisual.transform.localScale = new Vector3(0.1f, 0.8f, 0.4f);
        bowVisual.SetActive(false);

        heroUI = gameObject.AddComponent<ThronefallHeroUI>();
        heroUI.Init(transform);

        if (game.Cam != null)
            game.Cam.Init(transform);

        game.CombatSys.RegisterEntity(this);

        if (game.UI != null)
            game.UI.UpdateWeaponLabel(CurrentWeapon.weaponName);
    }

    void Update()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase == ThronefallGame.GamePhase.GameOver)
        {
            if (heroState == HeroState.Dead && game != null && game.UI != null)
                game.UI.HideRevivalCountdown();
            return;
        }

        if (heroState == HeroState.Dead)
        {
            UpdateDeath();
            return;
        }

        // Branch panel takes priority over all input
        if (game.UI != null && game.UI.IsBranchPanelOpen)
        {
            UpdateBranchPanelInput();
            return;
        }

        UpdateMovement();
        UpdateAutoAttack();
        UpdateSkillInput();
        UpdateWeaponSwitch();
        UpdateBuildInteraction();

        if (heroUI != null)
        {
            heroUI.UpdateHPBar((float)currentHP / maxHP);
            heroUI.UpdateSkillRing(skillCooldownTimer, skillCooldownTotal);
        }
    }

    void UpdateBranchPanelInput()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.UI == null) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            game.UI.NavigateBranch(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            game.UI.NavigateBranch(1);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            int selectedId = game.UI.GetSelectedBranchId();
            if (selectedId >= 0 && currentNearMarker != null)
                game.BuildSys.TryBranchUpgrade(currentNearMarker.nodeIndex, selectedId);
            game.UI.HideBranchPanel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            game.UI.HideBranchPanel();
    }

    void UpdateMovement()
    {
        if (heroState == HeroState.Thrusting)
        {
            UpdateThrust();
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(h, 0, v).normalized;

        Vector3 move = dir * moveSpeed;
        move.y = -9.8f;
        cc.Move(move * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                20f * Time.deltaTime);
    }

    void UpdateAutoAttack()
    {
        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Night) return;

        attackCooldownTimer -= Time.deltaTime;
        retargetTimer -= Time.deltaTime;

        if (retargetTimer <= 0)
        {
            retargetTimer = RetargetInterval;
            lockedTarget = game.CombatSys.FindNearestEnemy(Position, CurrentWeapon.attackRange);
        }

        if (lockedTarget == null || !lockedTarget.IsAlive)
        {
            lockedTarget = game.CombatSys.FindNearestEnemy(Position, CurrentWeapon.attackRange);
            if (lockedTarget == null) return;
        }

        if (attackCooldownTimer > 0) return;
        attackCooldownTimer = CurrentWeapon.attackInterval;

        if (CurrentWeapon.weaponId == "spear")
            DoSpearAttack(lockedTarget);
        else
            DoBowAttack(lockedTarget);
    }

    void DoSpearAttack(ThronefallEnemy target)
    {
        int damage = ThronefallCombatSystem.CalculateDamage(ATK, target.DEF);
        target.TakeDamage(damage);

        Vector3 dir = (target.Position - Position).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        ThronefallVFXHelper.SpawnSpearTrail(Position + Vector3.up, dir, 2f);
    }

    void DoBowAttack(ThronefallEnemy target)
    {
        int damage = ThronefallCombatSystem.CalculateDamage(ATK, target.DEF);
        ThronefallArrowProjectile.Spawn(
            Position + Vector3.up * 2f,
            target.Position,
            CurrentWeapon.arrowSpeed,
            CurrentWeapon.arcHeight,
            damage);
    }

    void UpdateSkillInput()
    {
        if (skillCooldownTimer > 0)
            skillCooldownTimer -= Time.deltaTime;
        if (skillCooldownTimer < 0) skillCooldownTimer = 0;

        if (heroState != HeroState.Alive) return;

        if (Input.GetKeyDown(KeyCode.Q) && skillCooldownTimer <= 0)
        {
            var skill = CurrentWeapon.skill;
            if (skill == null) return;
            skillCooldownTimer = skill.cooldown;
            skillCooldownTotal = skill.cooldown;

            if (skill.skillId == "thrust")
                StartThrust(skill);
            else if (skill.skillId == "volley")
                DoVolley(skill);
        }
    }

    void StartThrust(TFSkillConfig skill)
    {
        heroState = HeroState.Thrusting;
        thrustTimer = 0;
        thrustAccelTime = skill.accelTime;
        thrustDecelTime = skill.decelTime;
        thrustDuration = skill.accelTime + skill.decelTime;
        thrustMaxSpeed = skill.maxSpeed;
        thrustDamageMultiplier = skill.damageMultiplier;
        thrustDirection = transform.forward;
        thrustHitEnemies.Clear();
    }

    void UpdateThrust()
    {
        thrustTimer += Time.deltaTime;
        if (thrustTimer >= thrustDuration)
        {
            heroState = HeroState.Alive;
            return;
        }

        float speed;
        if (thrustTimer < thrustAccelTime)
            speed = (thrustTimer / thrustAccelTime) * thrustMaxSpeed;
        else
            speed = (1f - (thrustTimer - thrustAccelTime) / thrustDecelTime) * thrustMaxSpeed;

        Vector3 move = thrustDirection * speed;
        move.y = -9.8f;
        cc.Move(move * Time.deltaTime);

        var colliders = Physics.OverlapSphere(Position + Vector3.up, 1.2f);
        foreach (var col in colliders)
        {
            var enemy = col.GetComponent<ThronefallEnemy>();
            if (enemy != null && enemy.IsAlive && !thrustHitEnemies.Contains(enemy))
            {
                thrustHitEnemies.Add(enemy);
                int dmg = Mathf.RoundToInt(
                    ThronefallCombatSystem.CalculateDamage(ATK, enemy.DEF) * thrustDamageMultiplier);
                enemy.TakeDamage(dmg);
            }
        }

        ThronefallVFXHelper.SpawnSpearTrail(Position + Vector3.up, thrustDirection, 1.5f);
    }

    void DoVolley(TFSkillConfig skill)
    {
        Vector3 forward = transform.forward;
        float halfSpread = skill.spreadAngle * 0.5f;
        int count = Mathf.Max(1, skill.arrowCount);

        for (int i = 0; i < count; i++)
        {
            float angle;
            if (count == 1)
                angle = 0;
            else
                angle = Mathf.Lerp(-halfSpread, halfSpread, (float)i / (count - 1));

            Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 target = Position + dir * CurrentWeapon.attackRange;

            int dmg = Mathf.RoundToInt(ATK * skill.damageMultiplier);
            ThronefallArrowProjectile.Spawn(
                Position + Vector3.up * 2f, target,
                skill.arrowSpeed, skill.arcHeight, dmg);
        }
    }

    void UpdateWeaponSwitch()
    {
        if (heroState == HeroState.Thrusting) return;
        if (weapons == null || weapons.Length <= 1) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
            attackCooldownTimer = 0;
            lockedTarget = null;
            UpdateWeaponVisuals();

            var game = ThronefallGame.Instance;
            if (game != null && game.UI != null)
                game.UI.UpdateWeaponLabel(CurrentWeapon.weaponName);
        }
    }

    void UpdateWeaponVisuals()
    {
        bool isSpear = CurrentWeapon.weaponId == "spear";
        if (spearVisual != null) spearVisual.SetActive(isSpear);
        if (bowVisual != null) bowVisual.SetActive(!isSpear);
    }

    void UpdateBuildInteraction()
    {
        var game = ThronefallGame.Instance;
        if (game == null) return;
        if (game.CurrentPhase != ThronefallGame.GamePhase.Day) return;
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        if (currentNearMarker == null) return;

        int nodeIndex = currentNearMarker.nodeIndex;
        var actionType = game.BuildSys.GetNodeActionType(nodeIndex);

        switch (actionType)
        {
            case ThronefallBuildSystem.NodeActionType.Build:
                game.BuildSys.TryBuild(nodeIndex);
                break;
            case ThronefallBuildSystem.NodeActionType.Upgrade:
                game.BuildSys.TryUpgrade(nodeIndex);
                break;
            case ThronefallBuildSystem.NodeActionType.BranchUpgrade:
                var options = game.BuildSys.GetBranchConfigs(nodeIndex);
                if (options != null && game.UI != null)
                    game.UI.ShowBranchPanel(options);
                break;
            case ThronefallBuildSystem.NodeActionType.Recruit:
                game.BuildSys.TryRecruit(nodeIndex);
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        if (heroState == HeroState.Dead) return;
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (bodyMat != null)
        {
            float ratio = (float)currentHP / maxHP;
            bodyMat.color = Color.Lerp(new Color(1f, 0.3f, 0.3f), new Color(0.2f, 0.4f, 0.9f), ratio);
        }

        if (currentHP <= 0)
            Die();
    }

    public void Die()
    {
        if (heroState == HeroState.Dead) return;
        heroState = HeroState.Dead;

        if (bodyVisual != null) bodyVisual.SetActive(false);
        if (horseVisual != null) horseVisual.SetActive(false);
        if (spearVisual != null) spearVisual.SetActive(false);
        if (bowVisual != null) bowVisual.SetActive(false);
        if (soulVisual != null) soulVisual.SetActive(true);

        if (heroUI != null) heroUI.SetVisible(false);

        currentNearMarker = null;
        var game = ThronefallGame.Instance;
        if (game != null && game.UI != null)
        {
            game.UI.HideBuildPanel();
            game.UI.ShowRevivalCountdown(reviveTime);
        }

        reviveTimer = reviveTime;

        var mainBase = game != null ? game.CombatSys.GetMainBase() : null;
        if (mainBase != null && mainBase.IsAlive)
            reviveTimer = reviveTime;
    }

    void UpdateDeath()
    {
        reviveTimer -= Time.deltaTime;

        var game = ThronefallGame.Instance;
        if (game != null && game.UI != null)
            game.UI.UpdateRevivalCountdown(reviveTimer);

        if (reviveTimer <= 0)
            Revive();
    }

    void Revive()
    {
        var game = ThronefallGame.Instance;
        var mainBase = game != null ? game.CombatSys.GetMainBase() : null;
        Vector3 revivePos = mainBase != null && mainBase.IsAlive
            ? mainBase.Position + new Vector3(2, 0, 0)
            : Vector3.zero;

        heroState = HeroState.Alive;
        currentHP = maxHP;

        cc.enabled = false;
        transform.position = revivePos;
        cc.enabled = true;

        if (bodyVisual != null) bodyVisual.SetActive(true);
        if (horseVisual != null) horseVisual.SetActive(true);
        if (soulVisual != null) soulVisual.SetActive(false);
        UpdateWeaponVisuals();

        if (bodyMat != null)
            bodyMat.color = new Color(0.2f, 0.4f, 0.9f);

        if (heroUI != null) heroUI.SetVisible(true);

        if (game != null && game.UI != null)
            game.UI.HideRevivalCountdown();

        attackCooldownTimer = 0;
    }

    void OnTriggerEnter(Collider other)
    {
        if (heroState == HeroState.Dead) return;
        var marker = other.GetComponent<ThronefallBuildNodeMarker>();
        if (marker == null) return;

        currentNearMarker = marker;

        var game = ThronefallGame.Instance;
        if (game == null || game.CurrentPhase != ThronefallGame.GamePhase.Day || game.UI == null)
            return;

        int nodeIndex = marker.nodeIndex;
        var actionType = game.BuildSys.GetNodeActionType(nodeIndex);

        switch (actionType)
        {
            case ThronefallBuildSystem.NodeActionType.Build:
                var buildConfig = game.BuildSys.GetActionConfig(nodeIndex);
                if (buildConfig != null)
                    game.UI.ShowBuildPanel(buildConfig);
                break;
            case ThronefallBuildSystem.NodeActionType.Upgrade:
                var upgradeConfig = game.BuildSys.GetActionConfig(nodeIndex);
                if (upgradeConfig != null)
                    game.UI.ShowBuildPanel(upgradeConfig);
                break;
            case ThronefallBuildSystem.NodeActionType.BranchUpgrade:
                var currentConfig = game.BuildSys.GetCurrentBuildingConfig(nodeIndex);
                if (currentConfig != null)
                    game.UI.ShowBuildPanel(currentConfig);
                break;
            case ThronefallBuildSystem.NodeActionType.Recruit:
                var recConfig = game.BuildSys.GetCurrentBuildingConfig(nodeIndex);
                if (recConfig != null)
                {
                    int count = game.BuildSys.GetRecruitCount(nodeIndex);
                    game.UI.ShowBuildPanelForRecruit(recConfig, count);
                }
                break;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var marker = other.GetComponent<ThronefallBuildNodeMarker>();
        if (marker == null) return;

        if (currentNearMarker == marker)
        {
            currentNearMarker = null;
            var game = ThronefallGame.Instance;
            if (game != null && game.UI != null && !game.UI.IsBranchPanelOpen)
                game.UI.HideBuildPanel();
        }
    }
}
