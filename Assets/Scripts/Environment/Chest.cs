using UnityEngine;
using TMPro;

/// <summary>
/// World Chest — one prefab for all chests.
/// Player presses E nearby → game pauses → rarity is rolled (luck only) → ChestRewardUI opens.
/// When the reward is collected, souls burst out of the chest like a breaking
/// vase: count = baseOrbs × (orbLuckMultiplier × Luck) + player level, min 1.
///
/// SETUP:
///   1. Create a GameObject (box shape works fine as placeholder)
///   2. Add a Box Collider (solid — not trigger)
///   3. Add this script
///   4. Optionally add a world-space TMP_Text child for the interact prompt
///      and drag it into "Interact Prompt Text"
///   5. Assign a Renderer if you want the chest to grey out after opening
///   6. Assign soulPrefab (same prefab vases use)
///   7. Make sure ChestRewardUI is in the scene with UpgradePool assigned
///   8. Save as a Prefab — all chests use this one prefab
/// </summary>
public class Chest : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRadius = 2.5f;
    public TMP_Text interactPromptText;
    public string promptMessage = "[E] Open Chest";

    [Header("Soul Burst")]
    [Tooltip("Same soul prefab the vases use.")]
    public GameObject soulPrefab;
    [Tooltip("Base orb count, multiplied by (orbLuckMultiplier × Luck).")]
    public int baseOrbs = 3;
    [Tooltip("Luck factor: count = baseOrbs × (this × Luck) + player level.")]
    public float orbLuckMultiplier = 0.5f;
    public float minEjectForce = 3f;
    public float maxEjectForce = 8f;
    [Tooltip("Seconds before the burst souls become collectible — lets the explosion play out.")]
    public float soulAttractDelay = 0.5f;

    [Header("Visual (optional)")]
    [Tooltip("Assign the chest's Renderer to grey it out after opening")]
    public Renderer chestRenderer;

    [Header("State")]
    public bool isOpen = false;

    // ================================================================
    //  PRIVATE
    // ================================================================

    private bool playerInRange = false;

    private Transform PlayerTransform => PlayerRefs.PlayerTransform;
    private PlayerStats PlayerStats => PlayerRefs.I != null ? PlayerRefs.I.Stats : null;

    private void Start()
    {
        if (interactPromptText != null)
        {
            interactPromptText.text = promptMessage;
            interactPromptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        Transform player = PlayerTransform;
        if (isOpen || player == null) return;

        bool inRange = (transform.position - player.position).sqrMagnitude <= interactRadius * interactRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactPromptText != null)
                interactPromptText.gameObject.SetActive(playerInRange);
        }

        if (playerInRange && GameInput.InteractPressed && !GameTime.IsPaused)
            Open();
    }

    private void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (interactPromptText != null)
            interactPromptText.gameObject.SetActive(false);

        // Grey out to show it's been opened
        if (chestRenderer != null)
        {
            TintController tint = TintController.For(gameObject);
            tint.SetTargets(chestRenderer);
            Color c = chestRenderer.sharedMaterial != null ? chestRenderer.sharedMaterial.color : Color.white;
            tint.SetTint(this, new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a));
        }

        PlayerStats stats = PlayerStats;
        float luck = stats != null ? stats.luck : 0f;

        if (ChestRewardUI.Instance != null)
            ChestRewardUI.Instance.Show(luck, stats, this);
        else
            Debug.LogWarning("[Chest] ChestRewardUI.Instance not found in scene.");
    }

    // ================================================================
    //  SOUL BURST — called by ChestRewardUI when the reward is collected
    // ================================================================

    public void BurstSouls()
    {
        if (soulPrefab == null)
        {
            Debug.LogWarning("[Chest] No soulPrefab assigned — no souls burst out.", this);
            return;
        }

        PlayerStats stats = PlayerStats;
        float luck = stats != null ? stats.luck : 0f;
        PlayerLevelSystem levels = PlayerRefs.I != null ? PlayerRefs.I.LevelSystem : null;
        int level = levels != null ? levels.CurrentLevel : 1;
        int count = Mathf.Max(1, Mathf.RoundToInt(baseOrbs * (orbLuckMultiplier * luck) + level));

        // Upward explosion fan; souls stay uncollectible for soulAttractDelay
        // so the burst plays out before the Looter reels them in.
        Soul.Burst(soulPrefab, transform.position, count, 20f, 160f,
                   minEjectForce, maxEjectForce, soulAttractDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
