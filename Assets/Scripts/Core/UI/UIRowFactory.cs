using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds simple clickable list rows (Button + label) for data-driven menus
/// (pact picker, meta shop). Pass a template to use your own styled row
/// prefab — it needs a Button and a TMP_Text somewhere inside. Without a
/// template a plain row is created in code, so menus work before any art.
/// </summary>
public static class UIRowFactory
{
    public static readonly Color RowColor      = new Color(0.16f, 0.15f, 0.20f, 1f);
    public static readonly Color SelectedColor = new Color(0.55f, 0.16f, 0.12f, 1f);
    public static readonly Color DisabledColor = new Color(0.10f, 0.10f, 0.12f, 1f);

    public static (Button button, TMP_Text label) Create(Transform parent, GameObject template, float height = 90f)
    {
        if (template != null)
        {
            GameObject go = Object.Instantiate(template, parent, false);
            go.SetActive(true);
            return (go.GetComponentInChildren<Button>(true), go.GetComponentInChildren<TMP_Text>(true));
        }

        var row = new GameObject("Row", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        row.layer = parent != null ? parent.gameObject.layer : 0;
        row.transform.SetParent(parent, false);
        row.GetComponent<LayoutElement>().minHeight = height;
        Image bg = row.GetComponent<Image>();
        bg.color = RowColor;
        Button button = row.GetComponent<Button>();
        button.targetGraphic = bg;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.layer = row.layer;
        textGo.transform.SetParent(row.transform, false);
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(20f, 6f);
        rt.offsetMax = new Vector2(-20f, -6f);
        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 26;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;

        return (button, label);
    }

    public static void SetColor(Button b, Color c)
    {
        if (b != null && b.targetGraphic != null) b.targetGraphic.color = c;
    }

    public static void Clear(Transform content, GameObject keep = null)
    {
        if (content == null) return;
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            if (child == keep) continue;
            Object.Destroy(child);
        }
    }
}
