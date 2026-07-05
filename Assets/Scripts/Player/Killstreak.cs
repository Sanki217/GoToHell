using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Kill streak system.
///
/// RULES:
///   - Streak starts when 3 enemies are killed within 3 seconds of each other.
///   - Any damage dealt to an enemy resets the timer (not just kills).
///   - Each kill in the streak adds +2% damage and +1% energy regen (stacks).
///   - On expiry: streak ends, souls equal to kill count spawn at player.
///
/// GRACE PERIOD:
///   - When the countdown hits 0, the streak doesn't end immediately.
///   - Instead it enters a 1-second grace period where the panel flashes.
///   - Any kill or damage dealt during the grace period revives the streak fully.
///   - This gives the player a satisfying window to extend a streak they feel they "earned".
///
/// SETUP:
///   1. Add this script to the Player GameObject.
///   2. Assign streakPanel (parent canvas group or just GameObject to show/hide).
///   3. Assign streakLabel (TMP_Text) for "KILL STREAK x12" display.
///   4. Assign timerSlider (Slider) for the countdown bar.
///   5. Assign soulPrefab - the same Soul prefab used by Enemy.cs.
/// </summary>
public class KillStreak : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Timer duration in seconds. Any damage dealt resets this.")]
    public float streakDuration = 3f;
    [Tooltip("How many kills within the window before the streak activates.")]
    public int killsToActivate = 3;
    [Tooltip("Damage bonus per kill in streak (0.02 = 2%).")]
    public float damagePerKill = 0.02f;
    [Tooltip("Energy regen bonus per kill in streak (0.01 = 1%).")]
    public float energyRegenPerKill = 0.01f;

    [Header("Grace Period")]
    [Tooltip("Extra seconds the streak lingers after hitting 0, flashing to signal the player.")]
    public float graceDuration = 1f;
    [Tooltip("How fast the panel flashes during the grace period (seconds per toggle).")]
    public float flashInterval = 0.12f;

    [Header("UI")]
    public GameObject streakPanel;
    public TMP_Text streakLabel;
    public Slider timerSlider;

    [Header("Soul Reward")]
    public GameObject soulPrefab;

    // ================================================================
    //  STATE - accessible by PlayerStats for damage / energy scaling
    // ================================================================

    public bool IsActive { get; private set; } = false;
    public int StreakCount { get; private set; } = 0;

    /// <summary>Multiplier to apply on top of base damage. 1.0 = no bonus.</summary>
    public float DamageMultiplier => IsActive ? (1f + StreakCount * damagePerKill) : 1f;

    /// <summary>Additional energy regen multiplier. 1.0 = no bonus.</summary>
    public float EnergyRegenMultiplier => IsActive ? (1f + StreakCount * energyRegenPerKill) : 1f;

    // ================================================================
    //  PRIVATE
    // ================================================================

    private float timer = 0f;
    private bool isInGracePeriod = false;
    private float graceTimer = 0f;
    private float flashTimer = 0f;
    private bool flashVisible = true;

    // Kills tracked before streak activates (to count toward the initial 3)
    private Queue<float> recentKillTimes = new Queue<float>();

    private void Start()
    {
        if (streakPanel != null) streakPanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsActive) return;

        if (isInGracePeriod)
        {
            graceTimer -= Time.deltaTime;
            flashTimer -= Time.deltaTime;

            if (flashTimer <= 0f)
            {
                flashVisible = !flashVisible;
                if (streakPanel != null) streakPanel.SetActive(flashVisible);
                flashTimer = flashInterval;
            }

            if (graceTimer <= 0f)
                EndStreak();

            return;
        }

        timer -= Time.deltaTime;

        // Update UI
        if (timerSlider != null)
            timerSlider.value = Mathf.Clamp01(timer / streakDuration);

        if (timer <= 0f)
            BeginGracePeriod();
    }

    // ================================================================
    //  PUBLIC API - called from Enemy.cs and damage sources
    // ================================================================

    /// <summary>Call when an enemy is killed. Increments streak or starts it.</summary>
    public void RegisterKill()
    {
        if (!IsActive)
        {
            float now = Time.time;
            recentKillTimes.Enqueue(now);

            // Discard kills outside the window
            while (recentKillTimes.Count > 0 && now - recentKillTimes.Peek() > streakDuration)
                recentKillTimes.Dequeue();

            if (recentKillTimes.Count >= killsToActivate)
            {
                // Activate - all queued kills count toward the streak
                int initialCount = recentKillTimes.Count;
                recentKillTimes.Clear();
                ActivateStreak(initialCount);
            }
        }
        else
        {
            // Revive from grace period if needed, then add to streak
            ResetTimer();
            if (isInGracePeriod)
                ExitGracePeriod();

            StreakCount++;
            UpdateLabel();
        }
    }

    /// <summary>Call whenever damage is dealt to any enemy. Resets the countdown.</summary>
    public void RegisterDamageDealt()
    {
        if (!IsActive) return;

        ResetTimer();
        if (isInGracePeriod)
            ExitGracePeriod();
    }

    // ================================================================
    //  PRIVATE
    // ================================================================

    private void ActivateStreak(int initialKills)
    {
        IsActive = true;
        StreakCount = initialKills;
        timer = streakDuration;

        if (streakPanel != null) streakPanel.SetActive(true);
        UpdateLabel();

        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f;
        }
    }

    private void ResetTimer()
    {
        timer = streakDuration;
        if (timerSlider != null) timerSlider.value = 1f;
    }

    private void BeginGracePeriod()
    {
        isInGracePeriod = true;
        graceTimer = graceDuration;
        flashTimer = 0f;   // start flashing immediately
        flashVisible = true;

        if (timerSlider != null) timerSlider.value = 0f;
    }

    private void ExitGracePeriod()
    {
        isInGracePeriod = false;
        flashVisible = true;
        if (streakPanel != null) streakPanel.SetActive(true);
    }

    private void EndStreak()
    {
        int soulsToSpawn = StreakCount;

        IsActive = false;
        StreakCount = 0;
        timer = 0f;
        isInGracePeriod = false;
        flashVisible = true;
        recentKillTimes.Clear();

        if (streakPanel != null) streakPanel.SetActive(false);

        SpawnStreakSouls(soulsToSpawn);
    }

    private void SpawnStreakSouls(int count)
    {
        if (soulPrefab == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            GameObject s = Pool.Spawn(soulPrefab, transform.position, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(4f, 9f));
            }
        }
    }

    private void UpdateLabel()
    {
        if (streakLabel != null)
            streakLabel.text = $"KILL STREAK x{StreakCount}";
    }
}
