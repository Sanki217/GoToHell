using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bow: hold LMB to charge (time slows, camera zooms), release to fire one of
/// three arrow tiers. Finite quiver with HUD dots.
///
/// Charge slow-mo is an owned GameTime request, so it composes with hit stop
/// and never un-pauses a menu. Losing control mid-charge (menu, level-up,
/// resurrection) cancels the charge cleanly — no stuck slow-mo or zoom.
/// </summary>
[RequireComponent(typeof(PlayerUpgradeManager))]
public class PlayerShooting : MonoBehaviour
{
    [Header("Arrow Prefabs — one per charge tier")]
    public GameObject lightArrowPrefab;
    public GameObject mediumArrowPrefab;
    public GameObject strongArrowPrefab;
    public GameObject mainArrowPrefab;   // fallback if tier prefab is null

    [Header("Shooting Point")]
    public Transform shootPoint;

    [Header("Ammo")]
    public int maxArrows = 3;
    private int currentArrows;
    public int CurrentArrows => currentArrows;

    public GameObject[] arrowDots;

    [Header("Arrow Dot Tinting")]
    [Tooltip("Color used to tint the last remaining dot while Dead Man's Hand is primed.")]
    public Color deadMansHandDotColor = new Color(1f, 0.15f, 0.15f);

    // Cached original colors for each dot (Image or SpriteRenderer), filled on Start.
    private Color[] arrowDotOriginalColors;
    private bool deadMansHandPrimed = false;

    [Header("Arrow Speed")]
    public float baseArrowSpeed = 15f;

    [Header("Stickable Layers")]
    public LayerMask stickableLayers;

    [Header("Aiming Line")]
    public LineRenderer lineRenderer;
    public float aimLineLength = 20f;

    [Header("Charge Shot")]
    public float maxChargeMultiplier = 3f;
    public float chargeDuration = 1.5f;
    public float timeSlowDuration = 2f;
    public float minTimeScale = 0.2f;

    [Header("Charge Damage Scaling")]
    [Tooltip("Extra damage multiplier applied per 1% of charge. " +
             "2.0 = 100% charge adds 200% of base damage on top (total 3× base). " +
             "1.0 was the old behaviour.")]
    public float chargeDamageMultiplierPerPercent = 2f;

    [Header("Charge Energy Cost — default, overridden by PlayerStats at runtime")]
    public float chargeEnergyPerSecond = 5f;

    [Header("Charge UI")]
    public Slider chargeSlider;
    public TMPro.TMP_Text chargePercentText;

    [Header("Camera Zoom")]
    public float cameraZoomAmount = 3f;
    public float cameraZoomInTime = 0.4f;
    public float cameraZoomOutTime = 0.3f;

    [Header("Debug / Inspector")]
    [Range(0f, 1f)]
    public float chargeNormalized;
    public float currentSpeedMultiplier = 1f;

    private bool isCharging;
    private float chargeTimer;
    private float energySpentThisCharge;

    private float camZoomT = 0f;
    private bool isZoomingIn = false;
    private bool isZoomingOut = false;

    private CameraFollow cam;
    private PlayerUpgradeManager upgradeManager;
    private PlayerEnergy playerEnergy;
    private PlayerStats playerStats;
    private float originalCamZ;

    // Live values read from PlayerStats each frame
    private float ChargeDrainRate =>
        playerStats != null ? playerStats.arrowChargeDrainRate : chargeEnergyPerSecond;

    // Lower arrowChargeDuration = reaches 100% faster
    private float ChargeDuration =>
        playerStats != null ? playerStats.arrowChargeDuration : chargeDuration;

    private const float LightMax = 0.25f;
    private const float MediumMax = 0.75f;

    private PlayerStateController stateCtrl;

    void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();
        playerEnergy = GetComponent<PlayerEnergy>();
        playerStats = GetComponent<PlayerStats>();
        stateCtrl = GetComponent<PlayerStateController>();

        cam = PlayerRefs.CamFollow;
        if (cam != null) originalCamZ = cam.transform.position.z;

        // Spawned-prefab support: pull scene HUD refs not wired on the prefab
        if (HUDRefs.I != null)
        {
            if (arrowDots == null || arrowDots.Length == 0) arrowDots = HUDRefs.I.arrowDots;
            if (chargeSlider == null) chargeSlider = HUDRefs.I.chargeSlider;
            if (chargePercentText == null) chargePercentText = HUDRefs.I.chargePercentText;
        }

        currentArrows = maxArrows;
        CacheArrowDotColors();
        UpdateArrowUI();

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null) lineRenderer.positionCount = 2;

        if (chargeSlider != null) chargeSlider.gameObject.SetActive(false);
        if (chargePercentText != null) chargePercentText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (stateCtrl != null && !stateCtrl.HasControl())
        {
            // Menu / level-up / resurrection took over mid-charge: drop the
            // charge (no shot) so slow-mo and zoom never get stuck.
            if (isCharging) CancelCharge();
            return;
        }
        UpdateAimingLine();
        HandleChargeInput();
        UpdateCameraZoom();
    }

    private void OnDisable()
    {
        if (isCharging) CancelCharge();
        GameTime.ClearSlowMo(this);
    }

    void HandleChargeInput()
    {
        // When quiver is empty, LMB is handled by PlayerSlash — do nothing here
        if (currentArrows <= 0) return;

        // Shift+LMB routes to PlayerSlash as a forced slash — don't begin a charge
        if (GameInput.AttackPressed && GameInput.AltAttackHeld) return;

        if (GameInput.AttackPressed)
        {
            isCharging = true;
            isZoomingIn = true;
            isZoomingOut = false;
            chargeTimer = 0f;
            energySpentThisCharge = 0f;
            ShowChargeUI(true);
        }

        if (GameInput.AttackHeld && isCharging)
        {
            ChargeTick();

            float energyCost = ChargeDrainRate * Time.unscaledDeltaTime;
            if (playerEnergy.SpendEnergy(energyCost))
            {
                energySpentThisCharge += energyCost;
            }
            else
            {
                if (playerStats != null) playerStats.RecordEnergySpent(energySpentThisCharge, EnergySpentSource.Charging);
                if (playerStats != null) playerStats.RecordChargeCancelled();
                if (upgradeManager != null) upgradeManager.ArrowChargeCancelledByEnergy(chargeNormalized);
                FireArrow();
                ResetCharge();
            }
        }

        if (GameInput.AttackReleased && isCharging)
        {
            if (playerStats != null) playerStats.RecordEnergySpent(energySpentThisCharge, EnergySpentSource.Charging);
            FireArrow();
            ResetCharge();
        }
    }

    void ChargeTick()
    {
        chargeTimer += Time.unscaledDeltaTime;
        // ChargeDuration reads from PlayerStats.arrowChargeDuration — lower = faster
        chargeNormalized = Mathf.Clamp01(chargeTimer / ChargeDuration);
        currentSpeedMultiplier = Mathf.Lerp(1f, maxChargeMultiplier, chargeNormalized);

        float slowT = Mathf.Clamp01(chargeTimer / timeSlowDuration);
        GameTime.SetSlowMo(this, Mathf.Lerp(1f, minTimeScale, slowT));

        UpdateChargeUI();
    }

    void UpdateCameraZoom()
    {
        // Don't zoom while the camera is locked to a merchant/zone anchor
        if (cam == null || cam.IsLocked) return;

        if (isZoomingIn)
        {
            camZoomT += Time.unscaledDeltaTime / cameraZoomInTime;
            camZoomT = Mathf.Clamp01(camZoomT);
            float easedT = Mathf.SmoothStep(0f, 1f, camZoomT);
            SetCamZ(Mathf.Lerp(originalCamZ, originalCamZ + cameraZoomAmount, easedT));
            if (camZoomT >= 1f) isZoomingIn = false;
        }
        else if (isZoomingOut)
        {
            camZoomT -= Time.unscaledDeltaTime / cameraZoomOutTime;
            camZoomT = Mathf.Clamp01(camZoomT);
            float easedT = Mathf.SmoothStep(0f, 1f, camZoomT);
            SetCamZ(Mathf.Lerp(originalCamZ, originalCamZ + cameraZoomAmount, easedT));
            if (camZoomT <= 0f) isZoomingOut = false;
        }
    }

    void SetCamZ(float z)
    {
        Vector3 pos = cam.transform.position;
        cam.transform.position = new Vector3(pos.x, pos.y, z);
    }

    void ResetCharge()
    {
        isCharging = false;
        isZoomingIn = false;
        isZoomingOut = true;
        chargeTimer = 0f;
        chargeNormalized = 0f;
        currentSpeedMultiplier = 1f;
        energySpentThisCharge = 0f;
        GameTime.ClearSlowMo(this);
        ShowChargeUI(false);
    }

    /// <summary>Abort a charge without firing (control lost). Spent energy is still recorded.</summary>
    private void CancelCharge()
    {
        if (playerStats != null && energySpentThisCharge > 0f)
            playerStats.RecordEnergySpent(energySpentThisCharge, EnergySpentSource.Charging);
        ResetCharge();
    }

    /// <summary>Arrow prefab for a tier, falling back to mainArrowPrefab. Used by upgrades too.</summary>
    public GameObject GetArrowPrefab(ArrowFireType tier)
    {
        GameObject p = tier switch
        {
            ArrowFireType.Medium  => mediumArrowPrefab,
            ArrowFireType.Charged => strongArrowPrefab,
            _                     => lightArrowPrefab,
        };
        return p != null ? p : mainArrowPrefab;
    }

    void FireArrow()
    {
        Vector3 dir = GameInput.AimDirectionFrom(shootPoint.position, transform.right);

        ArrowFireType fireType = chargeNormalized <= LightMax  ? ArrowFireType.Weak
                               : chargeNormalized <= MediumMax ? ArrowFireType.Medium
                               : ArrowFireType.Charged;
        GameObject prefab = GetArrowPrefab(fireType);

        SpawnArrow(prefab, dir, currentSpeedMultiplier,
                   consumeAmmo: true, fireType: fireType, chargeAmount: chargeNormalized);

        if (playerStats != null) playerStats.RecordArrowFired(fireType);

        if (upgradeManager != null)
        {
            switch (fireType)
            {
                case ArrowFireType.Weak: upgradeManager.FireWeakArrow(dir, currentSpeedMultiplier); break;
                case ArrowFireType.Medium: upgradeManager.FireMediumArrow(dir, chargeNormalized); break;
                case ArrowFireType.Charged: upgradeManager.FireChargedArrow(dir, currentSpeedMultiplier); break;
            }
        }

        if (cam != null) cam.Shake(0.12f, 0.08f);
    }

    public void SpawnArrow(GameObject prefab, Vector3 dir, float speedMultiplier,
                           bool consumeAmmo,
                           ArrowFireType fireType = ArrowFireType.Weak,
                           float chargeAmount = 0f)
    {
        if (consumeAmmo && currentArrows <= 0) return;
        if (prefab == null)
        {
            Debug.LogWarning("[PlayerShooting] SpawnArrow called with null prefab.");
            return;
        }

        GameObject arrowGO = Instantiate(prefab, shootPoint.position, Quaternion.identity);
        if (!arrowGO.TryGetComponent(out Arrow a))
        {
            Debug.LogWarning("[PlayerShooting] Arrow prefab has no Arrow component.", prefab);
            Destroy(arrowGO);
            return;
        }
        a.Initialize(dir.normalized, stickableLayers);
        a.speed = baseArrowSpeed * speedMultiplier;
        a.fireType = fireType;
        a.chargeAmount = chargeAmount;
        a.chargeDamageMultiplierPerPercent = chargeDamageMultiplierPerPercent;

        if (consumeAmmo)
        {
            currentArrows--;
            UpdateArrowUI();
        }
    }

    void ShowChargeUI(bool visible)
    {
        if (chargeSlider != null) chargeSlider.gameObject.SetActive(visible);
        if (chargePercentText != null) chargePercentText.gameObject.SetActive(visible);
    }

    void UpdateChargeUI()
    {
        if (chargeSlider != null)
            chargeSlider.value = chargeNormalized;

        if (chargePercentText != null)
        {
            int percent = Mathf.RoundToInt(chargeNormalized * 100f);
            string tier = chargeNormalized <= LightMax ? "Light" :
                             chargeNormalized <= MediumMax ? "Medium" : "Strong";
            chargePercentText.text = $"{percent}% ({tier})";
        }
    }

    void UpdateAimingLine()
    {
        if (lineRenderer == null) return;
        Vector3 dir = GameInput.AimDirectionFrom(shootPoint.position, transform.right);
        lineRenderer.SetPosition(0, shootPoint.position);
        lineRenderer.SetPosition(1, shootPoint.position + dir * aimLineLength);
    }

    void UpdateArrowUI()
    {
        EnsureArrowDotCapacity();

        for (int i = 0; i < arrowDots.Length; i++)
        {
            if (arrowDots[i] == null) continue;
            arrowDots[i].SetActive(i < currentArrows);
        }
        ApplyDotTints();
    }

    /// <summary>
    /// The quiver can grow mid-run (Better Quiver shop item). The dot UI was
    /// a fixed Inspector array, so extra arrows were invisible — the quiver
    /// LOOKED infinite. This clones the last dot to match maxArrows.
    /// </summary>
    private void EnsureArrowDotCapacity()
    {
        if (arrowDots == null || arrowDots.Length == 0) return;
        if (maxArrows <= arrowDots.Length) return;

        int oldCount = arrowDots.Length;
        var newDots = new GameObject[maxArrows];
        var newColors = new Color[maxArrows];

        for (int i = 0; i < oldCount; i++)
        {
            newDots[i] = arrowDots[i];
            newColors[i] = (arrowDotOriginalColors != null && i < arrowDotOriginalColors.Length)
                ? arrowDotOriginalColors[i] : Color.white;
        }

        GameObject template = arrowDots[oldCount - 1];
        // Offset new dots by the spacing between the last two existing dots
        // (skip if a LayoutGroup is doing the positioning).
        Vector3 step = Vector3.zero;
        bool hasLayoutGroup = template.transform.parent != null &&
                              template.transform.parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null;
        if (!hasLayoutGroup && oldCount >= 2 && arrowDots[oldCount - 2] != null)
            step = template.transform.localPosition - arrowDots[oldCount - 2].transform.localPosition;

        for (int i = oldCount; i < maxArrows; i++)
        {
            GameObject clone = Instantiate(template, template.transform.parent);
            clone.name = $"ArrowDot {i + 1}";
            if (!hasLayoutGroup)
                clone.transform.localPosition = template.transform.localPosition + step * (i - (oldCount - 1));
            newDots[i] = clone;
            newColors[i] = newColors[oldCount - 1];
        }

        arrowDots = newDots;
        arrowDotOriginalColors = newColors;
    }

    private void CacheArrowDotColors()
    {
        if (arrowDots == null) return;
        arrowDotOriginalColors = new Color[arrowDots.Length];
        for (int i = 0; i < arrowDots.Length; i++)
        {
            if (arrowDots[i] == null) { arrowDotOriginalColors[i] = Color.white; continue; }
            var img = arrowDots[i].GetComponent<Image>();
            if (img != null) { arrowDotOriginalColors[i] = img.color; continue; }
            var sr = arrowDots[i].GetComponent<SpriteRenderer>();
            if (sr != null) { arrowDotOriginalColors[i] = sr.color; continue; }
            arrowDotOriginalColors[i] = Color.white;
        }
    }

    private void ApplyDotTints()
    {
        if (arrowDots == null || arrowDotOriginalColors == null) return;
        int lastActiveIdx = currentArrows - 1;
        for (int i = 0; i < arrowDots.Length; i++)
        {
            if (arrowDots[i] == null) continue;
            Color target = (deadMansHandPrimed && i == lastActiveIdx)
                ? deadMansHandDotColor
                : arrowDotOriginalColors[i];

            var img = arrowDots[i].GetComponent<Image>();
            if (img != null) { img.color = target; continue; }
            var sr = arrowDots[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = target;
        }
    }

    /// <summary>Toggle Dead Man's Hand visual priming — tints the last remaining arrow dot red.</summary>
    public void SetDeadMansHandPrimed(bool primed)
    {
        if (deadMansHandPrimed == primed) return;
        deadMansHandPrimed = primed;
        ApplyDotTints();
    }

    public void RestoreArrow()
    {
        currentArrows = Mathf.Clamp(currentArrows + 1, 0, maxArrows);
        UpdateArrowUI();
    }

    /// <summary>Change quiver size (shop/upgrades). Grows the HUD dots and clamps the current count.</summary>
    public void SetMaxArrows(int value)
    {
        maxArrows = Mathf.Max(0, value);
        currentArrows = Mathf.Clamp(currentArrows, 0, maxArrows);
        UpdateArrowUI();
    }

    /// <summary>Set current arrows directly — used by PlayerStats Inspector setter.</summary>
    public void SetCurrentArrows(int value)
    {
        currentArrows = Mathf.Clamp(value, 0, maxArrows);
        UpdateArrowUI();
    }

    public bool HasMaxArrows() => currentArrows >= maxArrows;
}
