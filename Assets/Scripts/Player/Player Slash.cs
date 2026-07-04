using UnityEngine;
using System.Collections;

/// <summary>
/// Player Slash — the Sword weapon's LMB attack.
/// Box collider extends from player center toward cursor, scaled by Size stat.
/// Cooldown reduced by Cooldown stat.
///
/// WEAPON MODES (decided by which components RunConfigApplier enables):
///   • Sword equipped → PlayerShooting disabled → slash fires on LMB directly.
///   • Bow equipped   → this component is disabled entirely (no slash; arrows
///     regenerate via ArrowRegenerator instead).
///   • Testing a scene directly (both enabled, prefab defaults) → legacy
///     behaviour: slash on LMB when the quiver is empty or Shift is held.
///
/// SETUP:
///   1. Add this script to the Player root
///   2. Create child GameObject "SlashCollider"
///   3. Add BoxCollider (Is Trigger = true) — used only for visuals/gizmos
///   4. Add Slash_Damage_Collider.cs to that child
///   5. Assign slashColliderObject in Inspector
/// </summary>
public class PlayerSlash : MonoBehaviour
{
    [Header("References")]
    public GameObject slashColliderObject;

    [Header("Base Values")]
    public float baseDamage = 5f;
    public float baseLength = 2.0f;
    public float baseWidth = 0.8f;
    public float baseCooldown = 0.8f;
    public float slashActiveDuration = 0.15f;

    [Header("Stat Scaling")]
    public float sizeScalePerPoint = 0.05f;
    public float cooldownReductionPerPoint = 0.05f;
    public float minCooldown = 0.2f;

    // ================================================================
    //  STATE
    // ================================================================

    private float cooldownTimer = 0f;
    private bool onCooldown = false;

    private PlayerStats playerStats;
    private PlayerShooting shooting;
    private PlayerStateController stateController;
    private Camera mainCam;

    // Geometry computed at slash time — read by Slash_Damage_Collider
    [HideInInspector] public Vector3 slashCenter;
    [HideInInspector] public Vector3 slashHalfExtents;
    [HideInInspector] public Quaternion slashRotation;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        shooting = GetComponent<PlayerShooting>();
        stateController = GetComponent<PlayerStateController>();
        mainCam = Camera.main;

        if (slashColliderObject != null)
            slashColliderObject.SetActive(false);
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        if (onCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) onCooldown = false;
        }

        if (stateController != null && !stateController.HasControl()) return;

        // Sword mode: PlayerShooting disabled/absent → slash is the primary LMB attack.
        bool slashIsPrimary = shooting == null || !shooting.enabled;

        // Legacy fallback mode (both components enabled): quiver empty or Shift held.
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        bool quiverEmpty = !slashIsPrimary && shooting.CurrentArrows <= 0;

        if ((!slashIsPrimary && !quiverEmpty && !shiftHeld) || onCooldown) return;
        if (!Input.GetMouseButtonDown(0)) return;

        TrySlash();
    }

    // ================================================================
    //  SLASH
    // ================================================================

    private void TrySlash()
    {
        Vector3 dir = GetCursorDirection();
        if (dir.sqrMagnitude < 0.001f) return;

        cooldownTimer = GetEffectiveCooldown();
        onCooldown = true;

        // Compute geometry
        float sizeBonus = playerStats != null ? playerStats.size * sizeScalePerPoint : 0f;
        float length = Mathf.Abs(baseLength + sizeBonus);
        float width = Mathf.Abs(baseWidth + sizeBonus);

        Vector3 center = transform.position + dir * (length * 0.5f);
        center.z = 0f;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        // Store for Slash_Damage_Collider to read
        slashCenter = center;
        slashHalfExtents = new Vector3(width * 0.5f, length * 0.5f, 0.5f);
        slashRotation = rotation;

        // Position visual collider object
        if (slashColliderObject != null)
        {
            slashColliderObject.transform.position = center;
            slashColliderObject.transform.rotation = rotation;
            slashColliderObject.transform.localScale = new Vector3(width, length, 1f);
        }

        // Run damage check IMMEDIATELY with explicit geometry — no physics sync needed
        var damageCollider = slashColliderObject?.GetComponent<Slash_Damage_Collider>();
        damageCollider?.DoSlash(center, slashHalfExtents, rotation, this);

        // Show the visual for slashActiveDuration
        StartCoroutine(ShowVisual());

        playerStats?.RecordSlashUsed();
    }

    private IEnumerator ShowVisual()
    {
        if (slashColliderObject == null) yield break;
        slashColliderObject.SetActive(true);
        yield return new WaitForSeconds(slashActiveDuration);
        slashColliderObject.SetActive(false);
    }

    // ================================================================
    //  HIT CALLBACKS (called by Slash_Damage_Collider)
    // ================================================================

    public void OnSlashHitEnemy(GameObject enemyGO)
    {
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy == null) return;

        float damage = playerStats != null ? playerStats.slashDamage : baseDamage;

        float finalDmg;
        bool isCrit;
        if (playerStats != null)
            (finalDmg, isCrit) = playerStats.RollDamage(damage, enemyGO);
        else { finalDmg = damage; isCrit = false; }

        Vector3 kbDir = enemyGO.transform.position - transform.position;
        kbDir.z = 0f;
        if (kbDir.sqrMagnitude < 0.001f) kbDir = Vector3.right;

        enemy.TakeDamage(
            Mathf.Max(1, Mathf.RoundToInt(finalDmg)),
            transform.position,
            kbDir.normalized,
            playerStats != null ? playerStats.knockbackForce : 0f,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        playerStats?.RecordSlashHitEnemy();
        playerStats?.RecordDamageDealt(finalDmg, DamageSource.Slash);
    }

    public void OnSlashHitDestructible(Vase vase, ExplosiveBarrel barrel)
    {
        float damage = playerStats != null ? playerStats.slashDamage : baseDamage;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(damage));

        vase?.TakeDamage(dmg);
        barrel?.TakeDamage(dmg);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private Vector3 GetCursorDirection()
    {
        if (mainCam == null) return Vector3.right;
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 worldMouse = mainCam.ScreenToWorldPoint(mouse);
        worldMouse.z = 0f;
        Vector3 dir = worldMouse - transform.position;
        dir.z = 0f;
        return dir.magnitude > 0.01f ? dir.normalized : Vector3.right;
    }

    private float GetEffectiveCooldown()
    {
        if (playerStats == null) return baseCooldown;
        float reduction = playerStats.cooldown * cooldownReductionPerPoint;
        return Mathf.Max(minCooldown, baseCooldown * (1f - reduction));
    }

    public bool IsOnCooldown() => onCooldown;
    public float GetCooldownRemaining() => onCooldown ? cooldownTimer : 0f;
    public float GetEffectiveCooldownPublic() => GetEffectiveCooldown();
}
