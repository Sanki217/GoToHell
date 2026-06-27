using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One row in the stat allocator: a stat, its current value, and +/- buttons.
/// Reports to the CharacterCreator which owns the shared point pool.
/// </summary>
public class StatAllocatorRow : MonoBehaviour
{
    [Header("Which stat this row controls")]
    public PrimaryStat stat;

    [Header("UI")]
    public TMP_Text valueText;
    public Button   plusButton;
    public Button   minusButton;

    private CharacterCreator creator;
    private int value;

    public int Value => value;

    public void Init(CharacterCreator owner)
    {
        creator = owner;
        value   = 0;

        if (plusButton  != null) plusButton.onClick.AddListener(OnPlus);
        if (minusButton != null) minusButton.onClick.AddListener(OnMinus);

        Refresh();
    }

    private void OnPlus()
    {
        if (creator != null && creator.TrySpendPoint())
        {
            value++;
            Refresh();
        }
    }

    private void OnMinus()
    {
        if (value > 0)
        {
            value--;
            creator?.RefundPoint();
            Refresh();
        }
    }

    private void Refresh()
    {
        if (valueText != null) valueText.text = value.ToString();
    }
}