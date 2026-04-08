using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Player Reference")]
    public PlayerStateController playerState;

    private bool isPaused = false;
    private CameraFollow cameraFollow;

    private void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (playerState == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerState = p.GetComponent<PlayerStateController>();
        }

        cameraFollow = Camera.main?.GetComponent<CameraFollow>();

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

        // VSync toggle — on = vSyncCount 1, off = 0
        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = QualitySettings.vSyncCount > 0;
            vSyncToggle.onValueChanged.AddListener(val =>
                QualitySettings.vSyncCount = val ? 1 : 0);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        playerState?.EnableControl();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
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

    public bool IsPaused => isPaused;
}