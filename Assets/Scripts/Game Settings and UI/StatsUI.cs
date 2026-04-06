using UnityEngine;
using TMPro;

/// <summary>
/// TAB-toggled two-column stats panel.
/// Left:  Level, Movement, Ammo, Combat
/// Right: Active Upgrades, Run History
///
/// Stats show as: BaseName  BaseValue (+BonusValue)
/// White = base, Green = positive bonus, Red = negative bonus.
/// </summary>
public class StatsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject statsPanel;

    [Tooltip("Left column — Level, Movement, Combat, Ammo")]
    public TMP_Text leftStatsText;

    [Tooltip("Right column — Active Upgrades, Run History")]
    public TMP_Text rightStatsText;

    [Tooltip("Legacy fallback — only used if left/right are not assigned")]
    public TMP_Text statsText;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;

    // Components
    private PlayerStats playerStats;
    private PlayerLevelSystem levelSystem;
    private PlayerHealth playerHealth;
    private PlayerEnergy playerEnergy;
    private PlayerUpgradeManager upgradeManager;
    private bool isVisible = false;

    // Base values — snapshot on Start, before any upgrades apply
    private int baseMaxHP;
    private float baseArrowDamage, baseDashDamage, baseCritChance, baseCritMultiplier;
    private float baseKnockback, baseLifesteal;
    private float baseBurn, baseFreeze, baseHoly, baseShock;
    private float baseMoveSpeed, baseJumpForce, baseDashDist, baseDashCost;
    private float baseDashInvinc, baseMaxEnergy, baseHoverDrain;
    private float baseChargeDrain, baseChargeDur, baseWallSlide, baseLootRange, baseLuck;
    private int baseMaxArrows;

    private void Start()
    {
        if (player == null) player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            playerHealth = player.GetComponent<PlayerHealth>();
            playerEnergy = player.GetComponent<PlayerEnergy>();
            levelSystem = player.GetComponent<PlayerLevelSystem>();
            upgradeManager = player.GetComponent<PlayerUpgradeManager>();
        }

        if (statsPanel != null) statsPanel.SetActive(false);

        // Snapshot base values before any upgrades are applied
        if (playerStats != null) SnapshotBase();
    }

    private void SnapshotBase()
    {
        var s = playerStats;
        baseMaxHP = s.maxHP;
        baseArrowDamage = s.arrowDamage;
        baseDashDamage = s.dashDamage;
        baseCritChance = s.critChance;
        baseCritMultiplier = s.critMultiplier;
        baseKnockback = s.knockbackForce;
        baseLifesteal = s.lifeSteal;
        baseBurn = s.burnStrength;
        baseFreeze = s.freezeStrength;
        baseHoly = s.holyStrength;
        baseShock = s.shockStrength;
        baseMoveSpeed = s.moveSpeed;
        baseJumpForce = s.jumpForce;
        baseDashDist = s.dashDistance;
        baseDashCost = s.dashCost;
        baseDashInvinc = s.dashInvincibilityWindow;
        baseMaxEnergy = s.maxEnergy;
        baseHoverDrain = s.hoverDrainRate;
        baseChargeDrain = s.arrowChargeDrainRate;
        baseChargeDur = s.arrowChargeDuration;
        baseWallSlide = s.wallSlideSpeed;
        baseLootRange = s.lootRange;
        baseLuck = s.luck;
        baseMaxArrows = s.MaxArrows;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (statsPanel != null) statsPanel.SetActive(isVisible);
        }

        if (!isVisible || playerStats == null) return;

        if (leftStatsText != null && rightStatsText != null)
        {
            leftStatsText.text = BuildLeft();
            rightStatsText.text = BuildRight();
        }
        else if (statsText != null)
        {
            statsText.text = BuildLeft() + "\n" + BuildRight();
        }
    }

    // ================================================================
    //  HELPERS — stat display with base + bonus
    // ================================================================

    /// <summary>Formats a float stat showing base value + bonus if any.</summary>
    private string Stat(string label, float current, float baseVal, string fmt = "F1")
    {
        string baseStr = current.ToString(fmt);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) < 0.001f)
            return $"{label,-18}<color=#FFFFFF>{baseStr}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus.ToString(fmt)})</color>"
            : $"<color=#FF4444>({bonus.ToString(fmt)})</color>";
        return $"{label,-18}<color=#FFFFFF>{baseStr}</color> {bonusStr}\n";
    }

    private string StatPct(string label, float current, float baseVal)
    {
        string baseStr = $"{current * 100f:F0}%";
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) < 0.0001f)
            return $"{label,-18}<color=#FFFFFF>{baseStr}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus * 100f:F0}%)</color>"
            : $"<color=#FF4444>({bonus * 100f:F0}%)</color>";
        return $"{label,-18}<color=#FFFFFF>{baseStr}</color> {bonusStr}\n";
    }

    private string StatInt(string label, int current, int baseVal)
    {
        int bonus = current - baseVal;
        if (bonus == 0)
            return $"{label,-18}<color=#FFFFFF>{current}</color>\n";
        string bonusStr = bonus > 0
            ? $"<color=#00FF66>(+{bonus})</color>"
            : $"<color=#FF4444>({bonus})</color>";
        return $"{label,-18}<color=#FFFFFF>{current}</color> {bonusStr}\n";
    }

    // ================================================================
    //  LEFT COLUMN — Level, Movement, Combat, Ammo
    // ================================================================

    private string BuildLeft()
    {
        var s = playerStats;
        var hp = playerHealth;

        // ── Primary Stats ─────────────────────────────────────────────
        string left = "<b><color=#FF88FF>══ PRIMARY STATS ══</color></b>\n";
        left += Stat("Agility", s.agility, 0f);
        left += Stat("Atk Damage", s.attackDamage, 0f);
        left += Stat("Ability Power", s.abilityPower, 0f);
        left += Stat("Luck", s.luck, 0f);
        left += Stat("Psyche", s.psyche, 0f);
        left += Stat("Health", s.health_stat, 0f);
        left += Stat("Size", s.size, 0f);
        left += Stat("Cooldown", s.cooldown, 0f);
        left += "\n";

        // ── Level ────────────────────────────────────────────────────
        left += "<b><color=#00FF99>══ LEVEL ══</color></b>\n";
        left += "<b><color=#00FF99>══ LEVEL ══</color></b>\n";
        if (levelSystem != null)
        {
            bool maxed = levelSystem.CurrentLevel >= PlayerLevelSystem.MaxLevel;
            left += $"{"Level",-18}<color=#FFFFFF>{(maxed ? "MAX" : levelSystem.CurrentLevel.ToString())}</color>\n";
            left += $"{"Current XP",-18}<color=#FFFFFF>{(maxed ? "—" : Mathf.FloorToInt(levelSystem.CurrentXP).ToString())}</color>\n";
            left += $"{"XP to Next",-18}<color=#FFFFFF>{(maxed ? "—" : Mathf.FloorToInt(levelSystem.XPToNextLevel).ToString())}</color>\n";
            left += $"{"XP Multiplier",-18}<color=#FFFFFF>{levelSystem.xpMultiplier:F2}×</color>\n";
        }
        left += "\n";

        // ── Movement ─────────────────────────────────────────────────
        left += "<b><color=#FFD700>══ MOVEMENT ══</color></b>\n";
        left += Stat("Move Speed", s.moveSpeed, baseMoveSpeed);
        left += Stat("Jump Force", s.jumpForce, baseJumpForce);
        left += Stat("Dash Distance", s.dashDistance, baseDashDist);
        left += Stat("Dash Cost", s.dashCost, baseDashCost);
        left += Stat("Dash Invinc", s.dashInvincibilityWindow, baseDashInvinc, "F2");
        left += Stat("Max Energy", s.maxEnergy, baseMaxEnergy);
        left += $"{"Energy",-18}<color=#FFFFFF>{(playerEnergy != null ? playerEnergy.currentEnergy : 0f):F0}</color>\n";
        left += Stat("Hover Drain/s", s.hoverDrainRate, baseHoverDrain);
        left += Stat("Charge Drain/s", s.arrowChargeDrainRate, baseChargeDrain);
        left += Stat("Charge Time", s.arrowChargeDuration, baseChargeDur, "F2");
        left += Stat("Wall Slide", s.wallSlideSpeed, baseWallSlide);
        left += Stat("Loot Range", s.lootRange, baseLootRange);
        left += Stat("Luck", s.luck, baseLuck);
        left += "\n";

        // ── Combat ───────────────────────────────────────────────────
        left += "<b><color=#FFD700>══ COMBAT ══</color></b>\n";
        left += $"{"HP",-18}<color=#FFFFFF>{(hp != null ? hp.CurrentHP : 0)} / {s.maxHP}</color>\n";
        left += Stat("Arrow Damage", s.arrowDamage, baseArrowDamage);
        left += Stat("Dash Damage", s.dashDamage, baseDashDamage);
        left += StatPct("Crit Chance", s.critChance, baseCritChance);
        left += StatPct("Crit Multi", s.critMultiplier, baseCritMultiplier);
        left += Stat("Knockback", s.knockbackForce, baseKnockback);
        left += StatPct("Lifesteal", s.lifeSteal, baseLifesteal);
        left += StatPct("Burn", s.burnStrength, baseBurn);
        left += StatPct("Freeze", s.freezeStrength, baseFreeze);
        left += StatPct("Holy", s.holyStrength, baseHoly);
        left += StatPct("Shock", s.shockStrength, baseShock);
        left += "\n";

        // ── Ammo ─────────────────────────────────────────────────────
        left += "<b><color=#FFD700>══ AMMO ══</color></b>\n";
        left += StatInt("Arrows", s.CurrentArrows, s.CurrentArrows); // current always white (changes constantly)
        left += $"{"Max Arrows",-18}";
        left += StatInt("", s.MaxArrows, baseMaxArrows).TrimStart();

        return left;
    }

    // ================================================================
    //  RIGHT COLUMN — Active Upgrades + Run History
    // ================================================================

    private string BuildRight()
    {
        var s = playerStats;

        // ── Active Upgrades ──────────────────────────────────────────
        string right = "<b><color=#FF99FF>══ ACTIVE UPGRADES ══</color></b>\n";
        if (upgradeManager != null)
        {
            bool any = false;
            foreach (var kv in upgradeManager.GetActiveUpgrades())
            {
                right += $"  {kv.Key,-24} <color=#AAFFAA>Lv {kv.Value}</color>\n";
                any = true;
            }
            if (!any) right += "  <color=#888888>None</color>\n";
        }
        right += "\n";

        // ── Run History ──────────────────────────────────────────────
        right +=
            "<b><color=#FF6666>══ HISTORY: MOVEMENT ══</color></b>\n" +
            $"{"Distance",-18}{s.totalDistance:F0}m\n" +
            $"{"Jumps",-18}{s.jumpsPerformed}\n" +
            $"{"Wall Slides",-18}{s.wallSlideCount} ({s.totalWallSlideDuration:F1}s)\n" +
            $"{"Hovers",-18}{s.hoverCount} ({s.totalHoverDuration:F1}s)\n" +
            $"{"Dashes",-18}{s.dashCount} (E:{s.dashesHitEnemy} W:{s.dashesHitWall})\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: COMBAT ══</color></b>\n" +
            $"{"Arrows Fired",-18}{s.totalArrowsFired} (L:{s.weakArrowsFired} M:{s.mediumArrowsFired} S:{s.chargedArrowsFired})\n" +
            $"{"Arrows Hit",-18}{s.arrowsHitEnemy}\n" +
            $"{"Kills",-18}{s.enemiesKilled}\n" +
            $"{"Dmg Dealt",-18}{s.totalDamageDealt:F0}\n" +
            $"{"Crits",-18}{s.critsLanded}\n" +
            $"{"Status",-18}B:{s.burnApplied} F:{s.freezeApplied} H:{s.holyApplied} S:{s.shockApplied}\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: RESOURCES ══</color></b>\n" +
            $"{"Souls",-18}{s.soulsCollected}\n" +
            $"{"XP",-18}{s.xpGained:F0}\n" +
            $"{"Energy",-18}{s.energyGainedTotal:F0}\n" +
            $"{"  Kills",-18}{s.energyFromKills:F0}\n" +
            $"{"  Falling",-18}{s.energyFromFalling:F0}\n" +
            $"{"  Lava",-18}{s.energyFromLava:F0}\n" +
            $"{"  Wall Slide",-18}{s.energyFromWallSlide:F0}\n" +
            "\n" +
            "<b><color=#FF6666>══ HISTORY: SURVIVAL ══</color></b>\n" +
            $"{"Dmg Taken",-18}{s.damageTaken:F0}\n" +
            $"{"Times Hit",-18}{s.timesHit}\n" +
            $"{"HP Restored",-18}{s.hpRestored:F0}\n" +
            $"{"Layers Done",-18}{s.layersCompleted}\n";

        return right;
    }
}