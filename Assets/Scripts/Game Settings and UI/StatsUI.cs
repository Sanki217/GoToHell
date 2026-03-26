using UnityEngine;
using TMPro;

/// <summary>
/// TAB-toggled stats panel. Shows all live stats and run history.
///
/// Setup:
///   1. Create a UI Panel in your Canvas, name it "StatsPanel"
///   2. Add a TMP_Text child inside it for the content
///   3. Add this script to any GameObject in the scene
///   4. Drag StatsPanel into the statsPanel field
///   5. Drag the TMP_Text into the statsText field
///   6. Drag the Player into the player field
/// </summary>
public class StatsUI : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject statsPanel;
    public TMP_Text statsText;

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.Tab;

    private PlayerStats playerStats;
    private PlayerHealth playerHealth;
    private PlayerEnergy playerEnergy;
    private PlayerShooting playerShooting;
    private bool isVisible = false;

    private void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
            playerHealth = player.GetComponent<PlayerHealth>();
            playerEnergy = player.GetComponent<PlayerEnergy>();
            playerShooting = player.GetComponent<PlayerShooting>();
        }

        if (statsPanel != null)
            statsPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (statsPanel != null)
                statsPanel.SetActive(isVisible);
        }

        if (isVisible && statsText != null && playerStats != null)
            statsText.text = BuildStatString();
    }

    private string BuildStatString()
    {
        var s = playerStats;
        var hp = playerHealth;
        var en = playerEnergy;

        return
            "<b><color=#FFD700>══ COMBAT STATS ══</color></b>\n" +
            $"HP:                {(hp != null ? hp.CurrentHP : 0)} / {s.maxHP}\n" +
            $"Arrow Damage:      {s.arrowDamage:F1}\n" +
            $"Dash Damage:       {s.dashDamage:F1}\n" +
            $"Crit Chance:       {s.critChance * 100f:F1}%\n" +
            $"Crit Multiplier:   {s.critMultiplier * 100f:F0}%\n" +
            $"Knockback Force:   {s.knockbackForce:F1}\n" +
            $"Burn Strength:     {s.burnStrength * 100f:F0}%\n" +
            $"Freeze Strength:   {s.freezeStrength * 100f:F0}%\n" +
            $"Holy Strength:     {s.holyStrength * 100f:F0}%\n" +
            $"Shock Strength:    {s.shockStrength * 100f:F0}%\n" +
            "\n" +
            "<b><color=#FFD700>══ MOVEMENT STATS ══</color></b>\n" +
            $"Move Speed:        {s.moveSpeed:F1}\n" +
            $"Jump Force:        {s.jumpForce:F1}\n" +
            $"Max Jumps:         {s.maxJumps}\n" +
            $"Dash Distance:     {s.dashDistance:F1}\n" +
            $"Dash Cost:         {s.dashCost:F1}\n" +
            $"Max Energy:        {s.maxEnergy:F1}\n" +
            $"Current Energy:    {(en != null ? en.currentEnergy : 0f):F0}\n" +
            $"Hover Drain/s:     {s.hoverDrainRate:F1}\n" +
            $"Charge Drain/s:    {s.arrowChargeDrainRate:F1}\n" +
            $"Wall Slide Speed:  {s.wallSlideSpeed:F1}\n" +
            "\n" +
            "<b><color=#FFD700>══ AMMO ══</color></b>\n" +
            $"Arrows:            {s.CurrentArrows} / {s.MaxArrows}\n" +
            "\n" +
            "<b><color=#FF6666>══ RUN HISTORY: MOVEMENT ══</color></b>\n" +
            $"Total Distance:    {s.totalDistance:F0}m\n" +
            $"Jumps:             {s.jumpsPerformed}\n" +
            $"Wall Slides:       {s.wallSlideCount}  ({s.totalWallSlideDuration:F1}s)\n" +
            $"Hovers:            {s.hoverCount}  ({s.totalHoverDuration:F1}s)\n" +
            $"Dashes:            {s.dashCount}  (hit enemy: {s.dashesHitEnemy}  wall: {s.dashesHitWall})\n" +
            "\n" +
            "<b><color=#FF6666>══ RUN HISTORY: COMBAT ══</color></b>\n" +
            $"Arrows Fired:      {s.totalArrowsFired}  (L:{s.weakArrowsFired} M:{s.mediumArrowsFired} S:{s.chargedArrowsFired} X:{s.extraArrowsFired})\n" +
            $"Arrows Hit Enemy:  {s.arrowsHitEnemy}\n" +
            $"Enemies Killed:    {s.enemiesKilled}\n" +
            $"Total Dmg Dealt:   {s.totalDamageDealt:F0}\n" +
            $"Crits Landed:      {s.critsLanded}\n" +
            $"Burn Applied:      {s.burnApplied}\n" +
            $"Freeze Applied:    {s.freezeApplied}\n" +
            $"Holy Applied:      {s.holyApplied}\n" +
            $"Shock Applied:     {s.shockApplied}\n" +
            "\n" +
            "<b><color=#FF6666>══ RUN HISTORY: SURVIVAL ══</color></b>\n" +
            $"Damage Taken:      {s.damageTaken:F0}\n" +
            $"Times Hit:         {s.timesHit}\n" +
            $"HP Restored:       {s.hpRestored:F0}\n" +
            $"Layers Completed:  {s.layersCompleted}\n" +
            $"Revive Used:       {s.reviveUsed}\n" +
            "\n" +
            "<b><color=#FF6666>══ RUN HISTORY: RESOURCES ══</color></b>\n" +
            $"Souls Collected:   {s.soulsCollected}\n" +
            $"XP Gained:         {s.xpGained:F0}\n" +
            $"Energy Gained:     {s.energyGainedTotal:F0}\n" +
            $"  From Kills:      {s.energyFromKills:F0}\n" +
            $"  From Falling:    {s.energyFromFalling:F0}\n" +
            $"  From Lava:       {s.energyFromLava:F0}\n" +
            $"  From WallSlide:  {s.energyFromWallSlide:F0}\n" +
            $"Chests Opened:     Common:{s.chestsOpenedCommon}  Rare:{s.chestsOpenedRare}  Leg:{s.chestsOpenedLegendary}\n";
    }
}