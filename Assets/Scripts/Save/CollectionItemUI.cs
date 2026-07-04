using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One tile in the Collection grid: icon + name.
/// Locked → icon silhouetted (dark tint), name replaced with "???",
/// optional overlay object activated (e.g. a padlock).
/// </summary>
public class CollectionItemUI : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public GameObject lockedOverlay;

    [Header("Locked Display")]
    public Color lockedTint = new Color(0.08f, 0.08f, 0.08f, 1f);
    public string lockedName = "???";

    public void Setup(Sprite icon, string displayName, bool unlocked)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.color = unlocked ? Color.white : lockedTint;
        }

        if (nameText != null)
            nameText.text = unlocked ? displayName : lockedName;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);
    }
}
