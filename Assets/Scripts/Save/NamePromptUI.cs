using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level 1 only: asks for the player's name when the scene loads, before the
/// "press any button" start gate. Confirm writes the name into RunConfig and
/// saves it via SaveManager so it's pre-filled next run.
///
/// Holds GameStartSequence disabled while the panel is open so typing can't
/// trigger the any-key start; re-enables it on confirm (its Start/Update only
/// run from that point).
/// </summary>
public class NamePromptUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_InputField nameInput;
    public Button confirmButton;

    [Header("Flow")]
    [Tooltip("Auto-resolved from the scene when left empty.")]
    public GameStartSequence startSequence;

    private void Awake()
    {
        if (startSequence == null)
            startSequence = Object.FindFirstObjectByType<GameStartSequence>(FindObjectsInactive.Include);

        // Keep the any-key start gate dormant and its prompt text hidden
        // while the name panel is open.
        if (startSequence != null)
        {
            startSequence.enabled = false;
            if (startSequence.pressAnyButtonText != null)
                startSequence.pressAnyButtonText.SetActive(false);
        }
    }

    private void Start()
    {
        // Freeze the player for the whole prompt — GameStartSequence would
        // normally do this in its Start, which is deferred while disabled.
        if (PlayerRefs.I != null && PlayerRefs.I.StateCtrl != null)
            PlayerRefs.I.StateCtrl.DisableControl();

        if (nameInput != null)
        {
            nameInput.text = SaveManager.LastPlayerName;
            nameInput.onValueChanged.AddListener(_ => RefreshConfirmButton());
        }
        if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        if (panel != null) panel.SetActive(true);

        if (nameInput != null) nameInput.ActivateInputField();
        RefreshConfirmButton();
    }

    private void RefreshConfirmButton()
    {
        if (confirmButton != null)
            confirmButton.interactable = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text);
    }

    private void Confirm()
    {
        string chosen = nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text)
            ? nameInput.text.Trim()
            : "Sinner";

        if (RunConfig.I != null) RunConfig.I.playerName = chosen;
        SaveManager.SetLastPlayerName(chosen);

        if (panel != null) panel.SetActive(false);

        // Hand over to the any-key start gate
        if (startSequence != null)
        {
            if (startSequence.pressAnyButtonText != null)
                startSequence.pressAnyButtonText.SetActive(true);
            startSequence.enabled = true;
        }
    }
}
