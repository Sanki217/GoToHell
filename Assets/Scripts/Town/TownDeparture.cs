using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hold the Descend key (default R / gamepad L3) anywhere in the town to
/// leave for a run. A fill image shows the hold progress; releasing early
/// cancels. Lives on the town HUD canvas.
/// </summary>
public class TownDeparture : MonoBehaviour
{
    [Tooltip("Seconds the key must be held.")]
    public float holdSeconds = 1.2f;
    [Tooltip("Image with Image Type = Filled (optional).")]
    public Image progressFill;
    [Tooltip("\"Hold R to descend\" hint (optional).")]
    public TMP_Text hintText;

    private float held;

    private void Start()
    {
        if (hintText != null) hintText.text = $"Hold {GameInput.BindingDisplay(GameAction.Descend)} to descend";
        SetFill(0f);
    }

    private void Update()
    {
        PlayerStateController state = PlayerRefs.I != null ? PlayerRefs.I.StateCtrl : null;
        bool canHold = !ModalUI.AnyOpen && !GameTime.IsPaused && (state == null || state.HasControl());

        if (canHold && GameInput.DescendHeld)
        {
            held += Time.unscaledDeltaTime;
            if (held >= holdSeconds)
            {
                held = 0f;
                if (TownManager.I != null) TownManager.I.Depart();
            }
        }
        else held = 0f;

        SetFill(held / Mathf.Max(0.01f, holdSeconds));
    }

    private void SetFill(float t)
    {
        if (progressFill == null) return;
        progressFill.fillAmount = t;
        progressFill.enabled = t > 0f;
    }
}
