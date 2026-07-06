using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Scene-side registry of HUD references for the spawned player prefab.
///
/// The player is instantiated at runtime (one prefab per class), so its
/// components can't hold direct Inspector references to scene UI. Instead,
/// put this on a scene object (e.g. the HUD canvas), wire the UI here once,
/// and player components pull whatever they're missing in Start:
///   PlayerHealth   → hpSlider, hpText
///   PlayerEnergy   → energyText
///   PlayerShooting → arrowDots, chargeSlider, chargePercentText
///   Killstreak     → streakPanel, streakLabel, streakTimerSlider
///
/// A player placed directly in the scene with its own references still wins —
/// fallbacks only fill fields that are null/empty.
/// </summary>
[DefaultExecutionOrder(-150)]
public class HUDRefs : MonoBehaviour
{
    public static HUDRefs I { get; private set; }

    [Header("Health")]
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("Energy")]
    public TMP_Text energyText;

    [Header("Quiver / Charge (Bow)")]
    public GameObject[] arrowDots;
    public Slider chargeSlider;
    public TMP_Text chargePercentText;

    [Header("Killstreak")]
    public GameObject streakPanel;
    public TMP_Text streakLabel;
    public Slider streakTimerSlider;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Debug.LogWarning("[HUDRefs] Duplicate HUDRefs — destroying the extra one.", this);
            Destroy(this);
            return;
        }
        I = this;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }
}
