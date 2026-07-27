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
    private Transform playerTransform;
    private PlayerStats playerStats;

    private void Start()
    {
        var refs = PlayerRefs.I;
        if (refs != null)
        {
            playerTransform = refs.T;
            playerStats = refs.Stats;
        }

        if (interactPromptText != null)
        {
            interactPromptText.text = promptMessage;
            interactPromptText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isOpen || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;
            if (interactPromptText != null)
                interactPromptText.gameObject.SetActive(playerInRange);
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
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
            Color c = chestRenderer.material.color;
            chestRenderer.material.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f);
        }

        float luck = playerStats != null ? playerStats.luck : 0f;

        if (ChestRewardUI.Instance != null)
            ChestRewardUI.Instance.Show(luck, playerStats, this);
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

        float luck = playerStats != null ? playerStats.luck : 0f;
        int level = PlayerRefs.I?.LevelSystem != null ? PlayerRefs.I.LevelSystem.CurrentLevel : 1;
        int count = Mathf.Max(1, Mathf.RoundToInt(baseOrbs * (orbLuckMultiplier * luck) + level));

        // Upward explosion fan; souls stay uncollectible for soulAttractDelay
        // so the burst plays out before the Looter reels them in.
        Vector3 pos = transform.position;
        for (int i = 0; i < count; i++)
        {
            GameObject s = Pool.Spawn(soulPrefab, pos, Quaternion.identity);
            Soul soul = s.GetComponent<Soul>();
            if (soul != null)
            {
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f).normalized;
                soul.Initialize(dir, Random.Range(minEjectForce, maxEjectForce), soulAttractDelay);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
