using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    [Header("UI")]
    public CanvasGroup fade;
    public GameObject pressAnyButtonText;

    private bool waitingForStart = true;
    private bool runStarted = false;

    void Start()
    {
        player.DisableControl();
        ResetPlayerToStart();
    }

    void Update()
    {
        if (waitingForStart && Input.anyKeyDown)
        {
            waitingForStart = false;
            pressAnyButtonText.SetActive(false);
            StartCoroutine(StartRunRoutine());
        }
    }

    IEnumerator StartRunRoutine()
    {
        // Wait before kick
        yield return new WaitForSeconds(delayBeforeKick);

        // Allow external kick movement
        player.allowKickMovement = true;

        // Kick player to the right
        playerRb.AddForce(Vector3.right * kickForce, ForceMode.Impulse);

        // Wait before giving control
        yield return new WaitForSeconds(delayBeforeControl);

        player.EnableControl();
        runStarted = true;
    }

    public void PlayerDied()
    {
        if (!runStarted) return;
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        runStarted = false;
        player.DisableControl();

        // Freeze time
        Time.timeScale = 0f;

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f, 1f));

        // Respawn player
        ResetPlayerToStart();

        // Unfreeze
        Time.timeScale = 1f;

        // Fade from black
        yield return StartCoroutine(Fade(1f, 0f, 1f));

        waitingForStart = true;
        pressAnyButtonText.SetActive(true);
    }

    void ResetPlayerToStart()
    {
        player.DisableControl();
        player.allowKickMovement = false;
        playerRb.linearVelocity = Vector3.zero;
        player.transform.position = startPoint.position;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
    }
}
