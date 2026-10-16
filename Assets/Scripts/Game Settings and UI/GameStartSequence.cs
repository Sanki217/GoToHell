using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Level intro gate: holds the player until any key, kicks them into the
/// level, then hands over control. Also provides the legacy death fallback
/// (fade + restart) for scenes tested without a RunSummaryUI.
/// </summary>
public class GameStartSequence : MonoBehaviour
{
    [Header("Setup")]
    public PlayerStateController player;
    public Rigidbody playerRb;
    public Transform startPoint;

    [Header("Start Sequence Settings")]
    public float delayBeforeKick = 1f;
    public float kickForce = 15f;
    public float delayBeforeControl = 1f;

    [Header("Death Settings")]
    public float fadeDuration = 1f;
    public float reloadDelay = 1f;

    [Header("UI")]
    public Image blackScreen;              // ← SINGLE IMAGE, NO CANVAS GROUP
    public GameObject pressAnyButtonText;

    private bool waitingForStart = true;

    void Start()
    {
        // Spawned-prefab support: resolve the player via PlayerRefs when unassigned
        if (player == null && PlayerRefs.I != null)
            player = PlayerRefs.I.StateCtrl;
        if (playerRb == null && player != null)
            playerRb = player.GetComponent<Rigidbody>();

        if (player == null)
        {
            Debug.LogWarning("[GameStartSequence] No player found — start sequence disabled.");
            enabled = false;
            return;
        }

        player.DisableControl();

        // Ensure blackscreen starts invisible
        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 0f;
            blackScreen.color = c;
        }
    }

    void Update()
    {
        if (!waitingForStart || !GameInput.AnyKeyPressed) return;
        if (GameTime.IsPaused) return;   // e.g. pause menu open over the intro

        waitingForStart = false;
        if (pressAnyButtonText != null) pressAnyButtonText.SetActive(false);
        StartCoroutine(StartRunRoutine());
    }

    IEnumerator StartRunRoutine()
    {
        yield return new WaitForSeconds(delayBeforeKick);

        player.allowKickMovement = true;
        if (playerRb != null)
            playerRb.AddForce(Vector3.right * kickForce, ForceMode.Impulse);

        yield return new WaitForSeconds(delayBeforeControl);

        player.EnableControl();
    }

    // Called when player dies and no RunSummaryUI exists (scene testing)
    public void PlayerDied()
    {
        StartCoroutine(DeathFadeAndReload());
    }

    IEnumerator DeathFadeAndReload()
    {
        player.DisableControl();
        if (playerRb != null && !playerRb.isKinematic) playerRb.linearVelocity = Vector3.zero;

        GameTime.Pause(this);

        // Fade the single Image alpha from 0 → 1
        if (blackScreen != null)
            yield return StartCoroutine(FadeImage(0f, 1f, fadeDuration));

        yield return new WaitForSecondsRealtime(reloadDelay);

        SceneFlow.ReloadActiveScene();   // resets time + run-scoped state
    }

    IEnumerator FadeImage(float from, float to, float duration)
    {
        float t = 0f;
        Color c = blackScreen.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float alpha = Mathf.Lerp(from, to, t / duration);
            blackScreen.color = new Color(c.r, c.g, c.b, alpha);

            yield return null;
        }

        // Ensure final alpha is exact
        blackScreen.color = new Color(c.r, c.g, c.b, to);
    }
}
