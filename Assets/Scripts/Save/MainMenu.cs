using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Put on a manager object in the Main Menu scene.
///
/// Play       → Character Select (8 slots) → [Create Character] → Pact Select → run
/// Meta Shop  → MetaShopUI — permanent upgrades for meta currency (optional)
/// Settings   → toggles the settings panel
/// Collection → Collection scene
/// Exit       → quits the game
///
/// Build the character/pact/meta panels with Tools → Go To Hell → Characters →
/// Build Main Menu Flow UI (wires the fields below automatically).
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button collectionButton;
    public Button exitButton;

    [Header("Run Flow")]
    public CharacterSelectUI characterSelect;

    [Header("Meta Shop (optional)")]
    public Button metaShopButton;
    public MetaShopUI metaShop;
    [Tooltip("Shows the meta currency balance.")]
    public TMP_Text currencyText;

    [Header("Settings Panel (optional)")]
    public GameObject settingsPanel;
    public Button settingsBackButton;

    private void Start()
    {
        // Back in the menu = no run in progress
        if (RunConfig.I != null) RunConfig.I.Reset();

        if (playButton != null)         playButton.onClick.AddListener(OnPlay);
        if (settingsButton != null)     settingsButton.onClick.AddListener(OnSettings);
        if (collectionButton != null)   collectionButton.onClick.AddListener(OnCollection);
        if (exitButton != null)         exitButton.onClick.AddListener(OnExit);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
        if (metaShopButton != null && metaShop != null) metaShopButton.onClick.AddListener(metaShop.Open);
        if (characterSelect != null)    characterSelect.OnStartRun += StartRun;

        if (settingsPanel != null) settingsPanel.SetActive(false);

        MetaProgression.OnChanged += RefreshCurrency;
        RefreshCurrency();
    }

    private void OnDestroy()
    {
        MetaProgression.OnChanged -= RefreshCurrency;
        if (characterSelect != null) characterSelect.OnStartRun -= StartRun;
    }

    private void RefreshCurrency()
    {
        if (currencyText != null)
            currencyText.text = $"{MetaProgression.Currency} {MetaProgression.CurrencyName}";
    }

    private void OnPlay()
    {
        if (characterSelect != null) characterSelect.Open();
        else Debug.LogWarning("[MainMenu] No CharacterSelectUI wired — run Tools → Go To Hell → Characters → Build Main Menu Flow UI.");
    }

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

    /// <summary>Character + pact chosen → configure the run and load Level 1.</summary>
    private void StartRun(CharacterData character, PactDefinition pact)
    {
        if (character == null || !character.CanPlay) return;

        if (RunConfig.I == null) new GameObject("RunConfig").AddComponent<RunConfig>();
        RunConfig.I.SetupRun(character, pact);
        SceneFlow.StartRunAtLevel1();
    }
}
