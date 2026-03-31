using UnityEngine;
using TMPro;

/// <summary>
/// World Chest — one prefab for all chests.
/// Player presses E nearby → game pauses → rarity is rolled (luck only) → ChestRewardUI opens.
///
/// SETUP:
///   1. Create a GameObject (box shape works fine as placeholder)
///   2. Add a Box Collider (solid — not trigger)
///   3. Add this script
///   4. Optionally add a world-space TMP_Text child for the interact prompt
///      and drag it into "Interact Prompt Text"
///   5. Assign a Renderer if you want the chest to grey out after opening
///   6. Make sure ChestRewardUI is in the scene with UpgradePool assigned
///   7. Save as a Prefab — all chests use this one prefab
/// </summary>
public class Chest : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRadius = 2.5f;
    public TMP_Text interactPromptText;
    public string promptMessage = "[E] Open Chest";

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
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerStats = player.GetComponent<PlayerStats>();
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
            ChestRewardUI.Instance.Show(luck, playerStats);
        else
            Debug.LogWarning("[Chest] ChestRewardUI.Instance not found in scene.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}