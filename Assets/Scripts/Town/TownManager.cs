using UnityEngine;

/// <summary>
/// The town hub between runs. One per Town scene.
///
///   • Makes sure a character is loaded (RunConfig.SetupTown). Opening the
///     Town scene directly in the editor picks the first playable character.
///   • Hands the player control straight away (no intro kick in town).
///   • Depart(): hold the Descend key anywhere (TownDeparture) or walk into
///     the exit (TownExit) → Pact Select → the run starts.
///   • RespawnPlayer(): after the loadout changes (weapon swap, smith level)
///     the player is swapped in place for a freshly built one — same spot,
///     same velocity, no scene reload.
/// </summary>
[DefaultExecutionOrder(-300)]   // before PlayerSpawner (-200): the loadout must be set first
public class TownManager : MonoBehaviour
{
    public static TownManager I { get; private set; }

    [Tooltip("Pact picker shown when leaving for a run.")]
    public PactSelectUI pactSelect;

    private PlayerStateController state;
    private bool departing;

    public CharacterData Character => RunConfig.I != null ? CharacterRoster.Get(RunConfig.I.characterId) : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => I = null;

    private void Awake()
    {
        I = this;
        GameTime.ResetAll();

        if (Character == null || !RunConfig.I.inTown)
        {
            // Opened directly (editor) — or arrived without SetupTown
            CharacterData c = Character;
            if (c == null)
                foreach (CharacterData ch in CharacterRoster.Characters)
                    if (ch.CanPlay) { c = ch; break; }

            if (c == null)
            {
                Debug.LogWarning("[TownManager] No playable character — create one in the main menu first.");
                return;
            }
            if (RunConfig.I == null) new GameObject("RunConfig").AddComponent<RunConfig>();
            RunConfig.I.SetupTown(c);
        }
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    private void Start()
    {
        state = PlayerRefs.I != null ? PlayerRefs.I.StateCtrl : null;
        if (state != null) state.EnableControl();
    }

    private void Update()
    {
        if (departing && pactSelect != null && pactSelect.IsOpen && GameInput.PausePressed)
            pactSelect.Close();

        // Pact picker closed without starting → give control back
        if (departing && (pactSelect == null || !pactSelect.IsOpen))
        {
            departing = false;
            ModalUI.Pop(this);
            if (state != null) state.RemoveBlocker(this);
        }
    }

    /// <summary>Leave for a run: pick a pact, then descend.</summary>
    public void Depart()
    {
        if (departing || Character == null) return;

        if (pactSelect == null)
        {
            Debug.LogWarning("[TownManager] No PactSelectUI assigned — starting without a pact.");
            StartRun(null);
            return;
        }

        departing = true;
        ModalUI.Push(this);
        if (state != null) state.AddBlocker(this);
        pactSelect.Open(Character, StartRun);
    }

    public void StartRun(PactDefinition pact)
    {
        CharacterData c = Character;
        if (c == null) return;
        SceneFlow.StartRun(c, pact);
    }

    /// <summary>
    /// Rebuilds the player with the current loadout (new weapon / smith level)
    /// right where they stand — the old player is swapped for a fresh one, so
    /// stats and kit are rebuilt cleanly with no scene reload.
    /// </summary>
    public void RespawnPlayer()
    {
        CharacterData c = Character;
        if (c == null) return;
        RunConfig.I.SetupTown(c);   // picks up the new weapon / level

        PlayerRefs old = PlayerRefs.I;
        PlayerSpawner spawner = FindFirstObjectByType<PlayerSpawner>();
        GameObject prefab = PlayerSpawner.PrefabFor(c.classId, spawner != null ? spawner.defaultPlayerPrefab : null);
        if (old == null || prefab == null)
        {
            SceneFlow.GoToTown(c);   // nothing to swap — fall back to a reload
            return;
        }

        Vector3 pos = old.T.position;
        Quaternion rot = old.T.rotation;
        Vector3 vel = old.TryGetComponent(out Rigidbody oldRb) && !oldRb.isKinematic ? oldRb.linearVelocity : Vector3.zero;

        old.Retire();                      // the fresh player becomes PlayerRefs.I
        old.gameObject.SetActive(false);   // gone this frame (triggers, input, camera target)
        Destroy(old.gameObject);

        GameObject fresh = Instantiate(prefab, pos, rot);
        if (fresh.TryGetComponent(out Rigidbody rb) && !rb.isKinematic) rb.linearVelocity = vel;
        state = fresh.GetComponent<PlayerStateController>();
        if (state != null) state.EnableControl();
    }
}
