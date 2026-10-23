using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Town overlay: character name, class, level + XP, talent points, Brimstone.
/// Every field is optional — wire what the scene has.
/// </summary>
public class TownHUD : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text levelText;
    [Tooltip("XP progress inside the current level (optional).")]
    public Slider xpBar;
    public TMP_Text currencyText;

    private void OnEnable()
    {
        CharacterRoster.OnChanged += Refresh;
        MetaProgression.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        CharacterRoster.OnChanged -= Refresh;
        MetaProgression.OnChanged -= Refresh;
    }

    private void Start() => Refresh();

    private void Refresh()
    {
        CharacterData c = TownManager.I != null ? TownManager.I.Character : null;
        ClassDefinition cls = c != null ? Content.Classes.Get(c.classId) : null;
        CharacterProgressionConfig cfg = CharacterProgressionConfig.Get();

        if (nameText != null)
            nameText.text = c == null ? "" : $"{c.name}  —  {(cls != null ? cls.displayName : c.classId)}{(c.hardcore ? "  (Hardcore)" : "")}";

        if (levelText != null)
        {
            if (c == null) levelText.text = "";
            else
            {
                string tp = c.TalentPoints > 0 ? $"   ·   {c.TalentPoints} talent point{(c.TalentPoints == 1 ? "" : "s")}" : "";
                levelText.text = $"Level {c.level}   ·   XP {c.xp} / {cfg.XpToNext(c.level)}{tp}";
            }
        }

        if (xpBar != null)
        {
            xpBar.minValue = 0f;
            xpBar.maxValue = 1f;
            xpBar.value = c == null ? 0f : Mathf.Clamp01((float)c.xp / cfg.XpToNext(c.level));
        }

        if (currencyText != null)
            currencyText.text = $"{MetaProgression.Currency} {MetaProgression.CurrencyName}";
    }
}
