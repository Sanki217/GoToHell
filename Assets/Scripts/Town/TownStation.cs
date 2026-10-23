using TMPro;
using UnityEngine;

/// <summary>
/// Something to interact with in the town (weapon rack, smith, shrine…).
/// Needs a trigger collider. Walk up → the prompt shows "[E] Smith" → press
/// Interact → its panel opens and the player freezes until the panel closes
/// (its Close button, or Esc).
///
/// The panel is any component implementing ITownPanel (WeaponRackUI,
/// SmithUI, MetaShopUI…), assigned in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TownStation : MonoBehaviour
{
    public string stationName = "Station";
    [Tooltip("Component implementing ITownPanel (WeaponRackUI, SmithUI, MetaShopUI…).")]
    public MonoBehaviour panel;
    [Tooltip("World-space text above the station (optional).")]
    public TMP_Text prompt;

    private ITownPanel Panel => panel as ITownPanel;
    private bool inRange;
    private bool open;
    private PlayerStateController state;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        if (panel != null && Panel == null)
            Debug.LogError($"[TownStation] '{name}': {panel.GetType().Name} doesn't implement ITownPanel.", this);
        RefreshPrompt();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        inRange = true;
        RefreshPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        inRange = false;
        RefreshPrompt();
    }

    private static bool IsPlayer(Collider c) => c.CompareTag("Player") || c.GetComponentInParent<PlayerStats>() != null;

    private void Update()
    {
        if (open)
        {
            if (Panel == null || !Panel.IsOpen) { CloseDone(); return; }
            if (GameInput.PausePressed) Panel.Close();
            return;
        }

        if (!inRange || Panel == null || ModalUI.AnyOpen || GameTime.IsPaused) return;
        if (!GameInput.InteractPressed) return;

        state = PlayerRefs.I != null ? PlayerRefs.I.StateCtrl : null;
        if (state != null && !state.HasControl()) return;

        open = true;
        ModalUI.Push(this);
        if (state != null) state.AddBlocker(this);
        Panel.Open();
        RefreshPrompt();
    }

    private void CloseDone()
    {
        open = false;
        ModalUI.Pop(this);
        if (state != null) state.RemoveBlocker(this);
        RefreshPrompt();
    }

    private void OnDisable()
    {
        if (open) CloseDone();
    }

    private void RefreshPrompt()
    {
        if (prompt == null) return;
        prompt.gameObject.SetActive(inRange && !open);
        prompt.text = $"[{GameInput.BindingDisplay(GameAction.Interact)}] {stationName}";
    }
}
