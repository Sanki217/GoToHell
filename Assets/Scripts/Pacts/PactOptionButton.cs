using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single pact option button in the Character Creator's pact list.
/// Spawned by CharacterCreator for each unlocked pact.
/// </summary>
public class PactOptionButton : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameLabel;
    public TMP_Text descriptionLabel;
    public Image    iconImage;
    public Button   button;
    public GameObject selectedHighlight;

    private PactDefinition pact;
    private CharacterCreator creator;

    public PactDefinition Pact => pact;

    public void Setup(PactDefinition def, CharacterCreator owner)
    {
        pact    = def;
        creator = owner;

        if (nameLabel != null)        nameLabel.text = def.displayName;
        if (descriptionLabel != null) descriptionLabel.text = def.description;
        if (iconImage != null)
        {
            iconImage.sprite  = def.icon;
            iconImage.enabled = def.icon != null;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => creator.SelectPact(this));
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(selected);
    }
}