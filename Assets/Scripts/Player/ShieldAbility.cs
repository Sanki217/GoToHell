using UnityEngine;

/// <summary>
/// Warrior RMB skill — directional block.
/// Hold RMB to raise a shield toward the cursor. While blocking, damage coming
/// from within the block arc is negated (checked by PlayerHealth via
/// IsBlockingFrom). Drains energy per second; blocking ends at 0 energy.
///
/// Arc: baseArcDegrees (180°) + arcDegreesPerSizePoint × Size, capped at max.
///
/// Disabled by default on the player prefab — enabled by RunConfigApplier when
/// the selected class's skillComponentName is "ShieldAbility".
/// Skill upgrades (bash, reflect, ...) come later via the upgrade event bus.
/// </summary>
public class ShieldAbility : MonoBehaviour
{
    [Header("Block Settings")]
    public float energyPerSecond = 10f;
    public float baseArcDegrees = 180f;
    public float arcDegreesPerSizePoint = 5f;
    public float maxArcDegrees = 360f;

    [Header("Projectile Reflection")]
    [Tooltip("Energy paid per reflected projectile. If the player can't afford it, the projectile is absorbed instead.")]
    public float reflectEnergyCost = 5f;

    [Header("Contact Counter")]
    [Tooltip("Enemies whose contact hit is blocked take Attack Damage × this, plus knockback.")]
    public float contactDamageMultiplier = 0.4f;

    [Header("Visual (optional)")]
    [Tooltip("Child object shown while blocking, rotated toward the cursor (e.g. a curved quad).")]
    public GameObject shieldVisual;

    // ── state ───────────────────────────────────────────────────────
    private bool isBlocking;
    private Vector3 blockDirection = Vector3.right;

    private PlayerEnergy energy;
    private PlayerStats playerStats;
    private PlayerStateController stateController;
    private Camera mainCam;

    public bool IsBlocking => isBlocking;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        energy = GetComponent<PlayerEnergy>();
        playerStats = GetComponent<PlayerStats>();
        stateController = GetComponent<PlayerStateController>();
        mainCam = Camera.main;

        if (shieldVisual != null) shieldVisual.SetActive(false);
    }

    private void OnDisable()
    {
        SetBlocking(false);
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        bool wantBlock = Input.GetMouseButton(1)
                      && (stateController == null || stateController.HasControl())
                      && energy != null && energy.currentEnergy > 0f;

        if (wantBlock)
        {
            blockDirection = GetCursorDirection();
            energy.DrainEnergy(energyPerSecond * Time.deltaTime);
            if (energy.currentEnergy <= 0f) wantBlock = false;
        }

        SetBlocking(wantBlock);

        if (isBlocking && shieldVisual != null)
        {
            float angle = Mathf.Atan2(blockDirection.y, blockDirection.x) * Mathf.Rad2Deg - 90f;
            shieldVisual.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // ================================================================
    //  PUBLIC API  (read by PlayerHealth)
    // ================================================================

    /// <summary>Current total block arc in degrees (Size-scaled).</summary>
    public float CurrentArcDegrees
    {
        get
        {
            float sizeBonus = playerStats != null ? playerStats.size * arcDegreesPerSizePoint : 0f;
            return Mathf.Clamp(baseArcDegrees + sizeBonus, 0f, maxArcDegrees);
        }
    }

    /// <summary>
    /// Pays the reflect cost for one projectile. Called by EnemyProjectile
    /// when it hits a raised shield. False = can't afford it (absorb instead).
    /// </summary>
    public bool TryPayReflectCost()
    {
        return isBlocking && energy != null && energy.SpendEnergy(reflectEnergyCost);
    }

    /// <summary>
    /// Called by PlayerHealth when a directional hit was blocked. If the
    /// source is an enemy (contact damage), counter with damage + knockback.
    /// </summary>
    public void OnBlockedHit(GameObject source)
    {
        if (source == null) return;

        Enemy enemy = source.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        float attack = playerStats != null ? playerStats.attackDamage : 5f;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(attack * contactDamageMultiplier));

        Vector3 dir = enemy.transform.position - transform.position;
        dir.z = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = blockDirection;

        enemy.TakeDamage(dmg, enemy.transform.position, dir.normalized,
                         playerStats != null ? playerStats.knockbackForce : 5f,
                         false, FloatingTextManager.HitType.Normal);
    }

    /// <summary>True if currently blocking AND the source lies within the block arc.</summary>
    public bool IsBlockingFrom(Vector3 sourcePosition)
    {
        if (!isBlocking) return false;

        Vector3 toSource = sourcePosition - transform.position;
        toSource.z = 0f;
        if (toSource.sqrMagnitude < 0.0001f) return true;   // point-blank — count as blocked

        return Vector3.Angle(blockDirection, toSource.normalized) <= CurrentArcDegrees * 0.5f;
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void SetBlocking(bool value)
    {
        if (isBlocking == value) return;
        isBlocking = value;
        if (shieldVisual != null) shieldVisual.SetActive(value);
    }

    private Vector3 GetCursorDirection()
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return blockDirection;

        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 worldMouse = mainCam.ScreenToWorldPoint(mouse);
        worldMouse.z = 0f;
        Vector3 dir = worldMouse - transform.position;
        dir.z = 0f;
        return dir.magnitude > 0.01f ? dir.normalized : blockDirection;
    }
}
