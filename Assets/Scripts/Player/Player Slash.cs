using UnityEngine;
using System.Collections;

/// <summary>
/// Player Slash — activates on LMB when the quiver is empty.
/// A box collider extends from the player's center toward the cursor.
/// Width and length scale with the Size primary stat.
/// Cooldown reduced by the Cooldown primary stat.
///
/// SETUP:
///   1. Add this script to the Player root
///   2. Create a child GameObject "SlashCollider"
///   3. Add a BoxCollider (Is Trigger = true) to it
///   4. Assign slashColliderObject in Inspector
///   5. Tune baseDamage, baseLength, baseWidth, baseCooldown in Inspector
///
/// The slash collider is enabled for slashActiveDuration seconds, then disabled.
/// </summary>
public class PlayerSlash : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Child GameObject that holds the slash BoxCollider (trigger)")]
    public GameObject slashColliderObject;

    [Header("Base Values (before stat scaling)")]
    public float baseDamage = 5f;
    public float baseLength = 2.0f;  // distance from center to tip
    public float baseWidth = 0.8f;  // width of the slash box
    public float baseCooldown = 0.8f;  // seconds between slashes
    public float slashActiveDuration = 0.15f; // how long the collider stays active

    [Header("Stat Scaling")]
    [Tooltip("Length and width increase by this much per Size point")]
    public float sizeScalePerPoint = 0.05f;
    [Tooltip("Cooldown reduced by this fraction per Cooldown point (0.1 = 10%/point)")]
    public float cooldownReductionPerPoint = 0.05f;
    [Tooltip("Minimum cooldown regardless of Cooldown stat")]
    public float minCooldown = 0.2f;

    // ================================================================
    //  STATE
    // ================================================================

    private float cooldownTimer = 0f;
    private bool onCooldown = false;

    private PlayerStats playerStats;
    private PlayerShooting shooting;
    private PlayerStateController stateController;
    private BoxCollider slashCollider;
    private Camera mainCam;

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
        {
            slashCollider = slashColliderObject.GetComponent<BoxCollider>();
            slashColliderObject.SetActive(false);
        }
    }

    // ================================================================
    //  UPDATE
    // ================================================================

    private void Update()
    {
        // Cool down
        if (onCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) onCooldown = false;
        }

        if (stateController != null && !stateController.HasControl()) return;

        // Only activate when quiver is empty and LMB is pressed
        bool quiverEmpty = shooting != null && shooting.CurrentArrows <= 0;
        if (!quiverEmpty) return;
        if (onCooldown) return;
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

        // Start cooldown
        float effectiveCooldown = GetEffectiveCooldown();
        cooldownTimer = effectiveCooldown;
        onCooldown = true;

        // Position and orient the collider
        PositionSlashCollider(dir);

        // Activate for slashActiveDuration
        StartCoroutine(ActivateSlashCollider());

        playerStats?.RecordSlashUsed();
    }

    private void PositionSlashCollider(Vector3 dir)
    {
        if (slashColliderObject == null) return;

        float sizeBonus = playerStats != null ? playerStats.size * sizeScalePerPoint : 0f;
        float length = baseLength + sizeBonus;
        float width = baseWidth + sizeBonus;

        // Place collider: center = player position + dir * (length / 2)
        Vector3 center = transform.position + dir * (length / 2f);
        center.z = 0f;

        slashColliderObject.transform.position = center;

        // Rotate to face cursor
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        slashColliderObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Scale: Y = length (along direction), X = width (perpendicular)
        // Use Abs to prevent negative scale warning from BoxCollider
        slashColliderObject.transform.localScale = new Vector3(Mathf.Abs(width), Mathf.Abs(length), 1f);
    }

    private IEnumerator ActivateSlashCollider()
    {
        if (slashColliderObject == null) yield break;

        slashColliderObject.SetActive(true);
        yield return new WaitForSeconds(slashActiveDuration);
        slashColliderObject.SetActive(false);
    }

    // ================================================================
    //  COLLISION — called from SlashDamageCollider child script
    // ================================================================

    /// <summary>
    /// Called by SlashDamageCollider when it overlaps an enemy.
    /// </summary>
    public void OnSlashHitEnemy(GameObject enemyGO)
    {
        Enemy enemy = enemyGO.GetComponent<Enemy>();
        if (enemy == null) return;

        float damage = playerStats != null ? playerStats.slashDamage : baseDamage;

        // Roll for crit
        float finalDmg;
        bool isCrit;
        if (playerStats != null)
            (finalDmg, isCrit) = playerStats.RollDamage(damage, enemyGO);
        else { finalDmg = damage; isCrit = false; }

        Vector3 kbDir = (enemyGO.transform.position - transform.position);
        kbDir.z = 0f;
        if (kbDir.sqrMagnitude < 0.001f) kbDir = Vector3.right;

        float kbForce = playerStats != null ? playerStats.knockbackForce : 0f;

        enemy.TakeDamage(
            Mathf.Max(1, Mathf.RoundToInt(finalDmg)),
            transform.position,
            kbDir.normalized,
            kbForce,
            isCrit,
            isCrit ? FloatingTextManager.HitType.Critical : FloatingTextManager.HitType.Normal
        );

        playerStats?.RecordSlashHitEnemy();
        playerStats?.RecordDamageDealt(finalDmg, DamageSource.Slash);
    }

    /// <summary>Called by SlashDamageCollider when it hits a vase or barrel.</summary>
    public void OnSlashHitDestructible(GameObject destructible)
    {
        float damage = playerStats != null ? playerStats.slashDamage : baseDamage;
        int dmg = Mathf.Max(1, Mathf.RoundToInt(damage));

        destructible.GetComponent<Vase>()?.TakeDamage(dmg);
        destructible.GetComponent<ExplosiveBarrel>()?.TakeDamage(dmg);
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private Vector3 GetCursorDirection()
    {
        if (mainCam == null) return Vector3.right;

        // Screen → world position at Z=0
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

    public float GetCooldownRemaining() => onCooldown ? cooldownTimer : 0f;
    public float GetEffectiveCooldownPublic() => GetEffectiveCooldown();
    public bool IsOnCooldown() => onCooldown;
}