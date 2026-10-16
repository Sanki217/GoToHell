using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// TAB-toggled two-column stats panel.
/// Left:  Primary Stats, Level, Movement, Combat, Ammo
/// Right: Active Upgrades, Run History
///
/// HOVER TOOLTIPS:
///   - Hover on a primary stat name → shows which secondary stats it affects.
///   - Hover on an upgrade name   → shows the full upgrade card (rarity, description, stat bonuses).
///
/// Both use TMP &lt;link&gt; tags and TMP_TextUtilities.FindIntersectingLink
/// so they work on pure text without spawning extra GameObjects.
///
/// PERF: the panel text is rebuilt at refreshRate Hz (not every frame) with a
/// reused StringBuilder — the old per-frame string concatenation allocated
/// tens of KB of garbage every frame while the panel was open.
///
/// TOOLTIP SETUP:
///   1. Create a child Panel under statsPanel named "Tooltip".
///   2. Add a TMP_Text child named "TooltipText".
///   3. Optionally add an Image child named "TooltipBG" (CanvasGroup for fade).
///   4. Wire tooltipPanel + tooltipText in the Inspector.
///   5. Tooltip auto-follows the mouse and shows on hover.
/// </summary>
public class StatsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject statsPanel;

    [Tooltip("Left column — Primary Stats, Level, Movement, Combat, Ammo")]
    public TMP_Text leftStatsText;

    [Tooltip("Right column — Active Upgrades, Run History")]
    public TMP_Text rightStatsText;

    [Tooltip("Legacy fallback — only used if left/right are not assigned")]
    public TMP_Text statsText;

    [Header("Tooltip")]
    [Tooltip("Panel that shows the hover tooltip. Auto-hidden when not hovered.")]
    public GameObject tooltipPanel;
    [Tooltip("Text element inside the tooltip panel.")]
    public TMP_Text tooltipText;

    [Header("Settings")]
    [Tooltip("Text rebuilds per second while the panel is open (unscaled time, works while paused).")]
    public float refreshRate = 5f;

    // Components
    private PlayerStats playerStats;
    private PlayerLevelSystem levelSystem;
    private PlayerHealth playerHealth;
    private PlayerEnergy playerEnergy;
    private PlayerUpgradeManager upgradeManager;
    private bool isVisible = false;
    private float refreshTimer;
    private readonly StringBuilder sb = new StringBuilder(4096);

    // Tooltip state
    private RectTransform tooltipRect;
    private Canvas rootCanvas;
    private string lastHoveredLink = "";

    // Base values — snapshot on Start, before any upgrades apply
    private int baseMaxHP;
    private float baseArrowDamage, baseSlashDamage, baseDashDamage, baseCritChance, baseCritMultiplier;
    private float baseKnockback, baseLifesteal;
    private float baseBurn, baseFreeze, baseHoly, baseShock;
    private float baseMoveSpeed, baseJumpForce, baseDashDist, baseDashCost;
    private float baseDashInvinc, baseMaxEnergy, baseHoverDrain;
    private float baseChargeDrain, baseChargeDur, baseWallSlide, baseLootRange, baseLuck;
    private int baseMaxArrows;

    private void Start()
    {
        ResolvePlayer();

        if (statsPanel != null) statsPanel.SetActive(false);
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
    }

    private bool ResolvePlayer()
    {
        if (playerStats != null) return true;
        var refs = PlayerRefs.I;
        if (player == null && refs != null) player = refs.gameObject;

        if (refs != null)
        {
            playerStats = refs.Stats;
            playerHealth = refs.Health;
            playerEnergy = refs.Energy;
            levelSystem = refs.LevelSystem;
            upgradeManager = refs.Upgrades;
        }

        // Snapshot base values before any upgrades are applied
        if (playerStats != null) SnapshotBase();
        return playerStats != null;
    }

    private void SnapshotBase()
    {
        var s = playerStats;
        baseMaxHP = s.maxHP;
        baseArrowDamage = s.arrowDamage;
        baseSlashDamage = s.slashDamage;
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
        if (GameInput.ToggleStatsPressed)   // Tab / gamepad Select — rebindable
        {
            isVisible = !isVisible;
            if (statsPanel != null) statsPanel.SetActive(isVisible);
            if (!isVisible && tooltipPanel != null) tooltipPanel.SetActive(false);
            refreshTimer = 0f;   // rebuild immediately on open
        }

        if (!isVisible || !ResolvePlayer()) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 1f / Mathf.Max(1f, refreshRate);
            RebuildText();
        }

        HandleHover();
    }

    private void RebuildText()
    {
        if (leftStatsText != null && rightStatsText != null)
        {
            leftStatsText.text = BuildLeft();
            rightStatsText.text = BuildRight();
            leftStatsText.ForceMeshUpdate();
            rightStatsText.ForceMeshUpdate();
        }
        else if (statsText != null)
        {
            statsText.text = BuildLeft() + "\n" + BuildRight();
            statsText.ForceMeshUpdate();
        }
    }

    // ================================================================
    //  HOVER DETECTION — TMP link tags
    // ================================================================

    private void HandleHover()
    {
        if (tooltipPanel == null || tooltipText == null) return;

        string hoveredLink = DetectLink(leftStatsText);
        if (string.IsNullOrEmpty(hoveredLink))
            hoveredLink = DetectLink(rightStatsText);

        if (!string.IsNullOrEmpty(hoveredLink))
        {
            if (hoveredLink != lastHoveredLink)
            {
                lastHoveredLink = hoveredLink;
                tooltipText.text = BuildTooltipContent(hoveredLink);
                tooltipPanel.SetActive(true);
            }
            PositionTooltip();
        }
        else
        {
            if (tooltipPanel.activeSelf)
            {
                tooltipPanel.SetActive(false);
                lastHoveredLink = "";
            }
        }
    }

    private string DetectLink(TMP_Text textComponent)
    {
        if (textComponent == null) return null;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            textComponent, GameInput.PointerScreen, null);

        if (linkIndex >= 0 && linkIndex < textComponent.textInfo.linkCount)
            return textComponent.textInfo.linkInfo[linkIndex].GetLinkID();

        return null;
    }

    private void PositionTooltip()
    {
        if (tooltipRect == null || rootCanvas == null) return;

        Vector2 mousePos = GameInput.PointerScreen;
        Vector2 offset = new Vector2(15f, -15f);

        // Keep tooltip on screen
        float w = tooltipRect.rect.width * rootCanvas.scaleFactor;
        float h = tooltipRect.rect.height * rootCanvas.scaleFactor;

        if (mousePos.x + offset.x + w > Screen.width)
            offset.x = -w - 15f;
        if (mousePos.y + offset.y - h < 0f)
            offset.y = h + 15f;

        tooltipRect.position = mousePos + offset;
    }

    // ================================================================
    //  TOOLTIP CONTENT
    // ================================================================

    private string BuildTooltipContent(string linkId)
    {
        // Primary stat tooltip — linkId starts with "stat_"
        if (linkId.StartsWith("stat_"))
        {
            string statName = linkId.Substring(5);
            return GetPrimaryStatTooltip(statName);
        }

        // Upgrade tooltip — linkId is the upgrade's UpgradeId
        return GetUpgradeTooltip(linkId);
    }

    private string GetPrimaryStatTooltip(string statName)
    {
        switch (statName)
        {
            case "Agility":
                return "<b><color=#FF9933>Agility</color></b>\n\n" +
                       "Affects:\n" +
                       "  Move Speed\n" +
                       "  Jump Force\n" +
                       "  Dash Distance\n" +
                       "  Wall Slide Speed";
            case "AttackDamage":
                return "<b><color=#FF4444>Attack Damage</color></b>\n\n" +
                       "Affects:\n" +
                       "  Arrow Damage\n" +
                       "  Slash Damage\n" +
                       "  Dash Damage\n" +
                       "  Crit Multiplier\n" +
                       "  Knockback Force";
            case "Luck":
                return "<b><color=#AAFF44>Luck</color></b>\n\n" +
                       "Affects:\n" +
                       "  Crit Chance\n" +
                       "  Upgrade Rarity Rolls\n" +
                       "  Stat Bonus Ranges\n" +
                       "  Loot Range";
            case "Psyche":
                return "<b><color=#FF66CC>Psyche</color></b>\n\n" +
                       "Affects:\n" +
                       "  Burn Strength\n" +
                       "  Freeze Strength\n" +
                       "  Holy Strength\n" +
                       "  Shock Strength\n" +
                       "  Lifesteal\n" +
                       "  Ability Damage Scaling";
            case "Health":
                return "<b><color=#44FF88>Health</color></b>\n\n" +
                       "Affects:\n" +
                       "  Max HP\n" +
                       "  Max Energy";
            case "Size":
                return "<b><color=#AAAAAA>Size</color></b>\n\n" +
                       "Affects:\n" +
                       "  AoE Radius\n" +
                       "  Gravity Arrow Pull Radius\n" +
                       "  Explosion Radius";
            case "Cooldown":
                return "<b><color=#44FFEE>Cooldown</color></b>\n\n" +
                       "Affects:\n" +
                       "  Charge Duration\n" +
                       "  Charge Drain Rate\n" +
                       "  Hover Drain Rate\n" +
                       "  Dash Cost";
            default:
                return statName;
        }
    }

    private string GetUpgradeTooltip(string upgradeId)
    {
        if (upgradeManager == null) return upgradeId;

        PlayerUpgrade upgrade = upgradeManager.GetUpgrade(upgradeId);
        AppliedUpgradeInfo info = upgradeManager.GetUpgradeInfo(upgradeId);

        if (upgrade == null) return upgradeId;

        // Rarity color
        UpgradeRarity rarity = info != null ? info.rarity : upgrade.rarity;
        Color rarityColor = UpgradeRarityRoller.GetRarityColor(rarity);
        string rarityHex = ColorUtility.ToHtmlStringRGB(rarityColor);
        string rarityName = rarity.ToString().ToUpper();

        // Build tooltip
        string tip = $"<b><color=#{rarityHex}>{upgrade.displayName}</color></b>\n";
        tip += $"<color=#{rarityHex}>{rarityName}</color>  <color=#888888>{upgrade.category}</color>\n\n";

        // Dynamic description with current stats
        string desc = upgrade.GetDynamicDescription(playerStats, null);
        if (string.IsNullOrEmpty(desc)) desc = upgrade.description;
        tip += desc;

        // Stat bonuses
        if (info?.statBonuses != null && info.statBonuses.Count > 0)
        {
            tip += "\n\n<color=#AAFFAA>Stat Bonuses:</color>\n";
            foreach (var bonus in info.statBonuses)
                tip += $"  <color=#00FF88>+{bonus.value:F1}</color> {UpgradeStatBonus.GetName(bonus.stat)}\n";
        }

        return tip;
    }

    // ================================================================
    //  HELPERS — stat display with base + bonus (append into sb)
    // ================================================================

    private const string White = "<color=#FFFFFF>";
    private const string EndColor = "</color>";

    private void Label(string label) => sb.Append(label.PadRight(18));

    private void Bonus(float bonus, string formatted)
    {
        sb.Append(bonus > 0 ? " <color=#00FF66>(+" : " <color=#FF4444>(")
          .Append(formatted).Append(")").Append(EndColor);
    }

    private void Stat(string label, float current, float baseVal, string fmt = "F1")
    {
        Label(label);
        sb.Append(White).Append(current.ToString(fmt)).Append(EndColor);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) >= 0.001f) Bonus(bonus, bonus.ToString(fmt));
        sb.Append('\n');
    }

    private void StatPct(string label, float current, float baseVal)
    {
        Label(label);
        sb.Append(White).Append((current * 100f).ToString("F0")).Append('%').Append(EndColor);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) >= 0.0001f) Bonus(bonus, (bonus * 100f).ToString("F0") + "%");
        sb.Append('\n');
    }

    private void StatInt(string label, int current, int baseVal)
    {
        Label(label);
        sb.Append(White).Append(current).Append(EndColor);
        int bonus = current - baseVal;
        if (bonus != 0) Bonus(bonus, bonus.ToString());
        sb.Append('\n');
    }

    /// <summary>Primary stat line whose label is a TMP link (hover → tooltip).
    /// Padding is applied to the visible label, not the markup, so columns line up.</summary>
    private void LinkedStat(string label, string linkId, float current, float baseVal, string fmt = "F1")
    {
        sb.Append("<link=\"stat_").Append(linkId).Append("\">").Append(label).Append("</link>");
        sb.Append(' ', Mathf.Max(1, 18 - label.Length));
        sb.Append(White).Append(current.ToString(fmt)).Append(EndColor);
        float bonus = current - baseVal;
        if (Mathf.Abs(bonus) >= 0.001f) Bonus(bonus, bonus.ToString(fmt));
        sb.Append('\n');
    }

    private void Line(string label, string value)
    {
        Label(label);
        sb.Append(value).Append('\n');
    }

    private void Header(string hexColor, string title)
    {
        sb.Append("<b><color=#").Append(hexColor).Append(">== ").Append(title).Append(" ==</color></b>\n");
    }

    // ================================================================
    //  LEFT COLUMN — Level, Movement, Combat, Ammo
    // ================================================================

    private string BuildLeft()
    {
        var s = playerStats;
        var hp = playerHealth;
        sb.Clear();

        // -- Primary Stats (hoverable) --
        Header("FF88FF", "PRIMARY STATS");
        LinkedStat("Agility", "Agility", s.agility, 0f);
        LinkedStat("Atk Damage", "AttackDamage", s.attackDamage, 0f);
        LinkedStat("Luck", "Luck", s.luck, 0f);
        LinkedStat("Psyche", "Psyche", s.psyche, 0f);
        LinkedStat("Health", "Health", s.health_stat, 0f);
        LinkedStat("Size", "Size", s.size, 0f);
        LinkedStat("Cooldown", "Cooldown", s.cooldown, 0f);
        sb.Append('\n');

        // -- Level --
        Header("00FF99", "LEVEL");
        if (levelSystem != null)
        {
            bool maxed = levelSystem.CurrentLevel >= PlayerLevelSystem.MaxLevel;
            Line("Level", White + (maxed ? "MAX" : levelSystem.CurrentLevel.ToString()) + EndColor);
            Line("Current XP", White + (maxed ? "\u2014" : Mathf.FloorToInt(levelSystem.CurrentXP).ToString()) + EndColor);
            Line("XP to Next", White + (maxed ? "\u2014" : Mathf.FloorToInt(levelSystem.XPToNextLevel).ToString()) + EndColor);
            Line("XP Multiplier", White + levelSystem.xpMultiplier.ToString("F2") + "x" + EndColor);
        }
        sb.Append('\n');

        // -- Movement --
        Header("FFD700", "MOVEMENT");
        Stat("Move Speed", s.moveSpeed, baseMoveSpeed);
        Stat("Jump Force", s.jumpForce, baseJumpForce);
        Stat("Dash Distance", s.dashDistance, baseDashDist);
        Stat("Dash Cost", s.dashCost, baseDashCost);
        Stat("Dash Invinc", s.dashInvincibilityWindow, baseDashInvinc, "F2");
        Stat("Max Energy", s.maxEnergy, baseMaxEnergy);
        Line("Energy", White + (playerEnergy != null ? playerEnergy.currentEnergy : 0f).ToString("F0") + EndColor);
        Stat("Hover Drain/s", s.hoverDrainRate, baseHoverDrain);
        Stat("Charge Drain/s", s.arrowChargeDrainRate, baseChargeDrain);
        Stat("Charge Time", s.arrowChargeDuration, baseChargeDur, "F2");
        Stat("Wall Slide", s.wallSlideSpeed, baseWallSlide);
        Stat("Loot Range", s.lootRange, baseLootRange);
        Stat("Luck", s.luck, baseLuck);
        sb.Append('\n');

        // -- Combat --
        Header("FFD700", "COMBAT");
        Line("HP", White + (hp != null ? hp.CurrentHP : 0) + " / " + s.maxHP + EndColor);
        Stat("Arrow Damage", s.arrowDamage, baseArrowDamage);
        Stat("Dash Damage", s.dashDamage, baseDashDamage);
        StatPct("Crit Chance", s.critChance, baseCritChance);
        StatPct("Crit Multi", s.critMultiplier, baseCritMultiplier);
        Stat("Knockback", s.knockbackForce, baseKnockback);
        StatPct("Lifesteal", s.lifeSteal, baseLifesteal);
        StatPct("Burn", s.burnStrength, baseBurn);
        StatPct("Freeze", s.freezeStrength, baseFreeze);
        StatPct("Holy", s.holyStrength, baseHoly);
        StatPct("Shock", s.shockStrength, baseShock);
        sb.Append('\n');

        // -- Ammo --
        Header("FFD700", "AMMO");
        StatInt("Arrows", s.CurrentArrows, s.CurrentArrows);
        StatInt("Max Arrows", s.MaxArrows, baseMaxArrows);

        return sb.ToString();
    }

    // ================================================================
    //  RIGHT COLUMN — Active Upgrades + Run History
    // ================================================================

    private string BuildRight()
    {
        RunStats h = playerStats.history;
        sb.Clear();

        // -- Active Upgrades (hoverable via TMP links) --
        Header("FF99FF", "ACTIVE UPGRADES");
        if (upgradeManager != null)
        {
            bool any = false;
            foreach (var id in upgradeManager.GetActiveUpgradeIds())
            {
                PlayerUpgrade upgrade = upgradeManager.GetUpgrade(id);
                string name = upgrade != null ? upgrade.displayName : id;
                sb.Append("  <link=\"").Append(id).Append("\"><color=#AAFFAA>").Append(name)
                  .Append("</color></link>\n");
                any = true;
            }
            if (!any) sb.Append("  <color=#888888>None</color>\n");
        }
        sb.Append('\n');

        // -- Run History --
        Header("FF6666", "HISTORY: MOVEMENT");
        Line("Distance", h.totalDistance.ToString("F0") + "m");
        Line("Jumps", h.jumpsPerformed.ToString());
        Line("Wall Slides", $"{h.wallSlideCount} ({h.totalWallSlideDuration:F1}s)");
        Line("Hovers", $"{h.hoverCount} ({h.totalHoverDuration:F1}s)");
        Line("Dashes", $"{h.dashCount} (E:{h.dashesHitEnemy} W:{h.dashesHitWall})");
        sb.Append('\n');

        Header("FF6666", "HISTORY: COMBAT");
        Line("Arrows Fired", $"{h.totalArrowsFired} (L:{h.weakArrowsFired} M:{h.mediumArrowsFired} S:{h.chargedArrowsFired})");
        Line("Arrows Hit", h.arrowsHitEnemy.ToString());
        Line("Kills", h.enemiesKilled.ToString());
        Line("Dmg Dealt", h.totalDamageDealt.ToString("F0"));
        Line("Crits", h.critsLanded.ToString());
        Line("Status", $"B:{h.burnApplied} F:{h.freezeApplied} H:{h.holyApplied} S:{h.shockApplied}");
        sb.Append('\n');

        Header("FF6666", "HISTORY: RESOURCES");
        Line("Souls", h.soulsCollected.ToString());
        Line("XP", h.xpGained.ToString("F0"));
        Line("Energy", h.energyGainedTotal.ToString("F0"));
        Line("  Kills", h.energyFromKills.ToString("F0"));
        Line("  Falling", h.energyFromFalling.ToString("F0"));
        Line("  Lava", h.energyFromLava.ToString("F0"));
        Line("  Wall Slide", h.energyFromWallSlide.ToString("F0"));
        sb.Append('\n');

        Header("FF6666", "HISTORY: SURVIVAL");
        Line("Dmg Taken", h.damageTaken.ToString("F0"));
        Line("Times Hit", h.timesHit.ToString());
        Line("HP Restored", h.hpRestored.ToString("F0"));
        Line("Extra Life", PlayerHealth.HasExtraLife ? "<color=#66FF66>YES</color>" : "<color=#888888>no</color>");
        Line("Layers Done", h.layersCompleted.ToString());

        return sb.ToString();
    }
}
