using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main-menu meta shop: spend meta currency on permanent MetaUpgradeDefinition
/// ranks. Rows are built from Resources/Meta, grouped by category; click a
/// row to see details, press Buy to rank it up.
///
/// Build a default panel: Tools → Go To Hell → Progression → Build Pact + Meta Shop UI
/// (Main Menu scene).
/// </summary>
public class MetaShopUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    [Tooltip("Parent for the rows (give it a VerticalLayoutGroup).")]
    public RectTransform content;
    [Tooltip("Optional styled row prefab (Button + TMP_Text). Empty = plain rows.")]
    public GameObject rowTemplate;
    public TMP_Text currencyText;
    public TMP_Text detailText;
    public Button buyButton;
    public TMP_Text buyButtonLabel;
    public Button refundButton;
    public Button closeButton;

    private readonly List<(MetaUpgradeDefinition def, Button button, TMP_Text label)> rows =
        new List<(MetaUpgradeDefinition, Button, TMP_Text)>();
    private MetaUpgradeDefinition selected;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (buyButton != null) buyButton.onClick.AddListener(BuySelected);
        if (refundButton != null) refundButton.onClick.AddListener(() => MetaProgression.RefundAll());
        if (rowTemplate != null) rowTemplate.SetActive(false);
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable() => MetaProgression.OnChanged += Refresh;
    private void OnDisable() => MetaProgression.OnChanged -= Refresh;

    public void Open()
    {
        if (panel != null) panel.SetActive(true);
        Rebuild();
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Rebuild()
    {
        UIRowFactory.Clear(content, rowTemplate);
        rows.Clear();

        string category = null;
        foreach (MetaUpgradeDefinition def in Content.MetaUpgrades.All)
        {
            if (MetaProgression.IsHidden(def)) continue;

            if (def.category != category)
            {
                category = def.category;
                var (hb, hl) = UIRowFactory.Create(content, rowTemplate, 50f);
                if (hb != null) { hb.interactable = false; UIRowFactory.SetColor(hb, Color.clear); }
                if (hl != null) hl.text = $"<color=#FFB347><b>{category?.ToUpperInvariant()}</b></color>";
            }

            var (button, label) = UIRowFactory.Create(content, rowTemplate);
            MetaUpgradeDefinition captured = def;
            if (button != null) button.onClick.AddListener(() => Select(captured));
            rows.Add((def, button, label));
        }

        if (selected == null && rows.Count > 0) selected = rows[0].def;
        if (rows.Count == 0 && detailText != null)
            detailText.text = "No meta upgrades yet.\nCreate MetaUpgradeDefinition assets in Assets/Resources/Meta.";
        Refresh();
    }

    private void Select(MetaUpgradeDefinition def)
    {
        selected = def;
        Refresh();
    }

    private void BuySelected()
    {
        if (selected != null) MetaProgression.TryBuy(selected);
    }

    private void Refresh()
    {
        string currency = MetaProgression.CurrencyName;
        if (currencyText != null) currencyText.text = $"{MetaProgression.Currency} {currency}";

        foreach (var (def, button, label) in rows)
        {
            int rank = MetaProgression.GetRank(def);
            bool maxed = rank >= def.maxRank;
            if (label != null)
                label.text = $"<b>{def.displayName}</b>   {rank}/{def.maxRank}" +
                             (maxed ? "   <color=#66FF66>MAX</color>" : $"   <color=#FFB347>{MetaProgression.NextCost(def)}</color>");
            UIRowFactory.SetColor(button, def == selected ? UIRowFactory.SelectedColor : UIRowFactory.RowColor);
        }

        if (selected == null) return;

        int r = MetaProgression.GetRank(selected);
        bool canBuy = MetaProgression.CanBuy(selected, out string reason);

        if (detailText != null)
        {
            string now = r > 0 ? selected.Describe(r) : "—";
            string next = r < selected.maxRank ? selected.Describe(r + 1) : "";
            detailText.text =
                $"<b>{selected.displayName}</b>\n{selected.description}\n\n" +
                $"<color=#AAAAAA>Now:</color>\n{now}" +
                (next.Length > 0 ? $"\n\n<color=#AAAAAA>Next rank:</color>\n{next}" : "");
        }

        if (buyButton != null) buyButton.interactable = canBuy;
        if (buyButtonLabel != null)
            buyButtonLabel.text = canBuy ? $"BUY  ({MetaProgression.NextCost(selected)})" : reason.ToUpperInvariant();
    }
}
