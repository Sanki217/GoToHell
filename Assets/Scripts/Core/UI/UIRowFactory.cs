using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clones a row template for data-driven lists (pacts, meta upgrades,
/// classes) — lists whose length depends on how many assets exist.
///
/// The template is a normal object in the scene (a disabled "RowTemplate"
/// child of the list) — style it in the hierarchy like any other button and
/// every row follows. It needs a Button and a TMP_Text somewhere inside.
/// </summary>
public static class UIRowFactory
{
    // Plain default-Unity look: white buttons, grey for selected, faded for locked
    public static readonly Color RowColor      = Color.white;
    public static readonly Color SelectedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    public static readonly Color DisabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);

    public static (Button button, TMP_Text label) Create(Transform parent, GameObject template)
    {
        if (template == null)
        {
            Debug.LogError($"[UIRowFactory] No row template assigned for list '{(parent != null ? parent.name : "?")}'. " +
                           "Assign the RowTemplate object in the Inspector.");
            return (null, null);
        }

        GameObject go = Object.Instantiate(template, parent, false);
        go.name = template.name.Replace("Template", "");
        go.SetActive(true);
        return (go.GetComponentInChildren<Button>(true), go.GetComponentInChildren<TMP_Text>(true));
    }

    public static void SetColor(Button b, Color c)
    {
        if (b != null && b.targetGraphic != null) b.targetGraphic.color = c;
    }

    /// <summary>Removes generated rows, keeping the template.</summary>
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
