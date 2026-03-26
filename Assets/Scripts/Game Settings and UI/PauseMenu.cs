using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Pause menu. ESC toggles pause.
///
/// Setup:
///   1. Create a UI Panel called "PausePanel" in your Canvas
///   2. Inside it add three Buttons: ResumeButton, RestartButton, SettingsButton
///   3. Add this script to any GameObject (e.g. GameManager or Canvas)
///   4. Wire up all fields in the Inspector
///   5. The SettingsPanel is a second panel you show when Settings is clicked
///      (leave it empty for now — wire it up when settings are built)
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;   // optional — leave null for now

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button settingsBackButton;  // back button inside settings panel (optional)

    [Header("Player Reference")]
    public PlayerStateController playerState;  // drag the Player here

    private bool isPaused = false;

    private void Start()
    {
        // Hide both panels at start
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Wire up buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(CloseSettings);

        // Auto-find player if not assigned
        if (playerState == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerState = p.GetComponent<PlayerStateController>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If settings panel is open, close it first
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (isPaused) Resume();
            else Pause();
        }
    }

    // ================================================================
    //  PUBLIC API — callable from buttons
    // ================================================================

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Re-enable player control
        playerState?.EnableControl();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);

        // Disable player control while paused
        playerState?.DisableControl();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    // ================================================================
    //  PUBLIC READ
    // ================================================================

    public bool IsPaused => isPaused;
}