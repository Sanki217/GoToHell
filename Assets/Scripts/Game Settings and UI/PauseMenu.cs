using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pause menu with a settings sub-panel (SettingsUI — graphics, gameplay, sound, controls).
///
/// Pausing is an owned GameTime pause + PlayerStateController blocker, so it
/// stacks safely on top of the level-up picker or chest roll: closing either
/// one never un-pauses the other.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Buttons")]
    public Button resumeButton;
    public Button restartButton;
    [Tooltip("Abandons the run and returns to the main menu (nothing is recorded, the character doesn't die).")]
    public Button quitToMenuButton;
    public Button settingsButton;
    public Button settingsBackButton;

    [Header("Settings — Camera Shake")]
    public Toggle cameraShakeToggle;
    [Tooltip("Slider controlling shake intensity (0–500%). Assign a UI Slider.")]
    public Slider shakeIntensitySlider;

    [Header("Settings — VSync")]
    [Tooltip("Toggle to enable/disable VSync (QualitySettings.vSyncCount).")]
    public Toggle vSyncToggle;

    [Header("Settings — UI Scale")]
    [Tooltip("Optional slider for the player's UI scale (75–150%). Persisted.")]
    public Slider uiScaleSlider;

    [Header("Player Reference")]
    public PlayerStateController playerState;

    private bool isPaused = false;

    public bool IsPaused => isPaused;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (playerState == null && PlayerRefs.I != null)
            playerState = PlayerRefs.I.StateCtrl;

        // Legacy widgets on this panel still work — they read/write GameSettings
        // (the full tabbed settings live in SettingsUI).
        if (cameraShakeToggle != null)
        {
            cameraShakeToggle.isOn = GameSettings.CameraShake;
            cameraShakeToggle.onValueChanged.AddListener(val => GameSettings.CameraShake = val);
        }
        if (shakeIntensitySlider != null)
        {
            shakeIntensitySlider.minValue = 0f;
            shakeIntensitySlider.maxValue = 5f;
            shakeIntensitySlider.value = GameSettings.ShakeIntensity;
            shakeIntensitySlider.onValueChanged.AddListener(val => GameSettings.ShakeIntensity = val);
        }
        if (uiScaleSlider != null)
        {
            uiScaleSlider.minValue = UIScaleManager.MinUserScale;
            uiScaleSlider.maxValue = UIScaleManager.MaxUserScale;
            uiScaleSlider.value = GameSettings.UiScale;
            uiScaleSlider.onValueChanged.AddListener(val => GameSettings.UiScale = val);
        }
        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = GameSettings.VSync;
            vSyncToggle.onValueChanged.AddListener(val => GameSettings.VSync = val);
        }
    }

    private void OnDisable()
    {
        // Never leave the game frozen if this menu disappears while open
        if (isPaused) Resume();
    }

    private void Update()
    {
        if (!GameInput.PausePressed) return;

        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
            return;
        }

        if (isPaused) Resume();
        else Pause();
    }

    public void Resume()
    {
        isPaused = false;
        GameTime.Unpause(this);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (playerState != null) playerState.RemoveBlocker(this);
    }

    public void Pause()
    {
        isPaused = true;
        GameTime.Pause(this);
        if (pausePanel != null) pausePanel.SetActive(true);
        if (playerState != null) playerState.AddBlocker(this);
    }

    /// <summary>Restart the whole run from Level 1 (roguelite restart, not a scene reload).</summary>
    public void Restart()
    {
        SceneFlow.RestartRun();
    }

    /// <summary>
    /// Leave the run: no run record, no meta currency, no achievements checked,
    /// and a hardcore character survives (abandoning isn't dying).
    /// </summary>
    public void QuitToMenu()
    {
        Resume();
        SceneFlow.GoToMainMenu();
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
}
