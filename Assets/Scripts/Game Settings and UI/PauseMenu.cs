using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Esc pause menu with a settings sub-panel (camera shake, VSync).
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
    public Button settingsButton;
    public Button settingsBackButton;

    [Header("Settings — Camera Shake")]
    public Toggle cameraShakeToggle;
    [Tooltip("Slider controlling shake intensity (0–2). Assign a UI Slider.")]
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
    private CameraFollow cameraFollow;

    public bool IsPaused => isPaused;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (playerState == null && PlayerRefs.I != null)
            playerState = PlayerRefs.I.StateCtrl;

        cameraFollow = PlayerRefs.CamFollow;

        // Camera shake toggle
        if (cameraShakeToggle != null && cameraFollow != null)
        {
            cameraShakeToggle.isOn = cameraFollow.shakeEnabled;
            cameraShakeToggle.onValueChanged.AddListener(val => cameraFollow.shakeEnabled = val);
        }

        // Shake intensity slider
        if (shakeIntensitySlider != null && cameraFollow != null)
        {
            shakeIntensitySlider.minValue = 0f;
            shakeIntensitySlider.maxValue = 2f;
            shakeIntensitySlider.value = cameraFollow.shakeIntensity;
            shakeIntensitySlider.onValueChanged.AddListener(val => cameraFollow.shakeIntensity = val);
        }

        // UI scale (UIScaleManager applies it to every canvas immediately)
        if (uiScaleSlider != null)
        {
            uiScaleSlider.minValue = UIScaleManager.MinUserScale;
            uiScaleSlider.maxValue = UIScaleManager.MaxUserScale;
            uiScaleSlider.value = UIScaleManager.UserScale;
            uiScaleSlider.onValueChanged.AddListener(val => UIScaleManager.UserScale = val);
        }

        // VSync toggle — on = vSyncCount 1, off = 0. Persisted; default OFF
        // (applied at boot by GameSettingsBootstrap).
        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vSyncToggle.onValueChanged.AddListener(val =>
            {
                QualitySettings.vSyncCount = val ? 1 : 0;
                PlayerPrefs.SetInt(GameSettingsBootstrap.VSyncPref, val ? 1 : 0);
                PlayerPrefs.Save();
            });
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
