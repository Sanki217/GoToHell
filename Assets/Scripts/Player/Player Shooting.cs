using UnityEngine;

[RequireComponent(typeof(PlayerUpgradeManager))]
public class PlayerShooting : MonoBehaviour
{
    // ================= ARROW CONFIG =================

    [Header("Arrow Prefabs")]
    public GameObject mainArrowPrefab;
    public GameObject extraArrowPrefab; // ← ADD THIS


    [Header("Shooting Point")]
    public Transform shootPoint;

    [Header("Ammo")]
    public int maxArrows = 3;
    private int currentArrows;
    public GameObject[] arrowDots;

    [Header("Arrow Speed")]
    public float baseArrowSpeed = 15f; // WEAK shot speed (100%)

    [Header("Stickable Layers")]
    public LayerMask stickableLayers;

    // ================= AIMING =================

    [Header("Aiming Line")]
    public LineRenderer lineRenderer;
    public float aimLineLength = 20f;

    // ================= CHARGING =================

    [Header("Charge Shot")]
    public float maxChargeMultiplier = 3f;     // 300%
    public float chargeDuration = 1.5f;
    public float timeSlowDuration = 2f;
    public float minTimeScale = 0.2f;

    [Header("Debug / Inspector")]
    [Range(0f, 1f)]
    public float chargeNormalized;              // 0 → 1
    public float currentSpeedMultiplier = 1f;

    // ================= STATE =================

    private bool isCharging;
    private float chargeTimer;

    private Camera mainCam;
    private CameraFollow cam;
    private PlayerUpgradeManager upgradeManager;

    private float originalCamZ;

    // =================================================

    void Start()
    {
        upgradeManager = GetComponent<PlayerUpgradeManager>();

        mainCam = Camera.main;
        cam = mainCam.GetComponent<CameraFollow>();
        originalCamZ = cam.transform.position.z;

        currentArrows = maxArrows;
        UpdateArrowUI();

        if (!lineRenderer)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (!GetComponent<PlayerStateController>().HasControl())
            return;

        UpdateAimingLine();
        HandleChargeInput();
    }

    // ================= INPUT =================

    void HandleChargeInput()
    {
        if (currentArrows <= 0)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            isCharging = true;
            chargeTimer = 0f;
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            ChargeTick();
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            FireChargedArrow();
            ResetCharge();
        }
    }

    // ================= CHARGE =================

    void ChargeTick()
    {
        chargeTimer += Time.unscaledDeltaTime;

        chargeNormalized = Mathf.Clamp01(chargeTimer / chargeDuration);
        currentSpeedMultiplier = Mathf.Lerp(1f, maxChargeMultiplier, chargeNormalized);

        // ---- Time Slow ----
        float slowT = Mathf.Clamp01(chargeTimer / timeSlowDuration);
        Time.timeScale = Mathf.Lerp(1f, minTimeScale, slowT);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // ---- Camera Zoom ----
        float targetZ = Mathf.Lerp(originalCamZ, originalCamZ + 3f, slowT);
        Vector3 camPos = cam.transform.position;
        cam.transform.position = new Vector3(camPos.x, camPos.y, targetZ);
    }

    void ResetCharge()
    {
        isCharging = false;
        chargeTimer = 0f;
        chargeNormalized = 0f;
        currentSpeedMultiplier = 1f;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        Vector3 camPos = cam.transform.position;
        cam.transform.position = new Vector3(camPos.x, camPos.y, originalCamZ);
    }

    // ================= SHOOTING =================

    void FireChargedArrow()
    {
        Vector3 mouseWorld = GetMouseWorld();
        Vector3 dir = (mouseWorld - shootPoint.position).normalized;

        // ---- MAIN ARROW (consumes ammo) ----
        SpawnArrow(
            mainArrowPrefab,
            dir,
            currentSpeedMultiplier,
            consumeAmmo: true
        );

        // ---- UPGRADE EVENTS ----
        if (chargeNormalized <= 0f)
        {
            upgradeManager.FireWeakArrow(dir, currentSpeedMultiplier);
        }
        else if (chargeNormalized < 1f)
        {
            upgradeManager.FireMediumArrow(dir, chargeNormalized);
        }
        else
        {
            upgradeManager.FireChargedArrow(dir, currentSpeedMultiplier);
        }

        cam.Shake(0.12f, 0.08f);
    }

    // ================= GENERIC ARROW SPAWN =================
    // Used by upgrades (free arrows, different prefabs, etc.)

    public void SpawnArrow(
        GameObject prefab,
        Vector3 dir,
        float speedMultiplier,
        bool consumeAmmo)
    {
        if (consumeAmmo && currentArrows <= 0)
            return;

        GameObject arrow = Instantiate(prefab, shootPoint.position, Quaternion.identity);
        Arrow a = arrow.GetComponent<Arrow>();

        a.Initialize(dir.normalized, stickableLayers);
        a.speed = baseArrowSpeed * speedMultiplier;

        if (consumeAmmo)
        {
            currentArrows--;
            UpdateArrowUI();
        }
    }

    // ================= HELPERS =================

    Vector3 GetMouseWorld()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        plane.Raycast(ray, out float dist);
        return ray.GetPoint(dist);
    }

    void UpdateAimingLine()
    {
        Vector3 mouseWorld = GetMouseWorld();
        Vector3 dir = (mouseWorld - shootPoint.position).normalized;

        lineRenderer.SetPosition(0, shootPoint.position);
        lineRenderer.SetPosition(1, shootPoint.position + dir * aimLineLength);
    }

    void UpdateArrowUI()
    {
        for (int i = 0; i < arrowDots.Length; i++)
            arrowDots[i].SetActive(i < currentArrows);
    }

    public void RestoreArrow()
    {
        currentArrows = Mathf.Clamp(currentArrows + 1, 0, maxArrows);
        UpdateArrowUI();
    }

    public bool HasMaxArrows() => currentArrows >= maxArrows;

    public void SpawnExtraArrow(Vector3 dir, float speedMultiplier)
    {
        if (extraArrowPrefab == null)
        {
            Debug.LogWarning("Extra Arrow Prefab not assigned!");
            return;
        }

        GameObject arrow = Instantiate(extraArrowPrefab, shootPoint.position, Quaternion.identity);
        Arrow a = arrow.GetComponent<Arrow>();

        a.Initialize(dir.normalized, stickableLayers);
        a.speed = baseArrowSpeed * speedMultiplier;
    }

}
