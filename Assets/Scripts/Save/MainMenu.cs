using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Put on a manager object in the Main Menu scene.
/// Wire the 4 buttons in the Inspector.
///
/// Play     → Character Creator
/// Settings → toggles the settings panel (kept as-is for now)
/// Collection → Collection scene
/// Exit     → quits the game
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button collectionButton;
    public Button exitButton;

    [Header("Settings Panel (optional)")]
    public GameObject settingsPanel;
    public Button settingsBackButton;

    private void Start()
    {
        // Starting a fresh run from the menu wipes any leftover run config
        if (RunConfig.I != null) RunConfig.I.Reset();

        if (playButton != null)       playButton.onClick.AddListener(OnPlay);
        if (settingsButton != null)   settingsButton.onClick.AddListener(OnSettings);
        if (collectionButton != null) collectionButton.onClick.AddListener(OnCollection);
        if (exitButton != null)       exitButton.onClick.AddListener(OnExit);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void OnPlay()       => SceneFlow.GoToCharacterCreator();
    private void OnCollection() => SceneFlow.GoToCollection();
    private void OnExit()       => SceneFlow.QuitGame();

    private void OnSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
}
