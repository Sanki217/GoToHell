using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds simple clickable list rows (Button + label) for data-driven menus
/// (character slots, pacts, meta shop). Pass a template to use your own styled
/// row prefab — it needs a Button and a TMP_Text somewhere inside. Without a
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

        TMP_Text label = CreateLabel(row.transform, 26, TextAlignmentOptions.MidlineLeft);
        RectTransform rt = label.rectTransform;
        rt.offsetMin = new Vector2(20f, 6f);
        rt.offsetMax = new Vector2(-20f, -6f);
        return (button, label);
    }

    /// <summary>
    /// A "label  [−] [+]" row (stat allocation). The buttons sit on the right.
    /// </summary>
    public static (TMP_Text label, Button minus, Button plus) CreateStepper(Transform parent, float height = 56f)
    {
        var (row, label) = Create(parent, null, height);
        row.interactable = false;
        label.rectTransform.offsetMax = new Vector2(-150f, -6f);

        Button minus = SmallButton(row.transform, "−", new Vector2(-95f, 0f));
        Button plus  = SmallButton(row.transform, "+", new Vector2(-35f, 0f));
        return (label, minus, plus);
    }

    private static Button SmallButton(Transform parent, string text, Vector2 pos)
    {
        var go = new GameObject(text == "+" ? "Plus" : "Minus", typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(48f, 44f);
        rt.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.3f, 0.28f, 0.34f, 1f);
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;

        TMP_Text t = CreateLabel(go.transform, 30, TextAlignmentOptions.Center);
        t.text = text;
        return b;
    }

    private static TMP_Text CreateLabel(Transform parent, float size, TextAlignmentOptions align)
    {
        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.layer = parent.gameObject.layer;
        textGo.transform.SetParent(parent, false);
        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.alignment = align;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
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
