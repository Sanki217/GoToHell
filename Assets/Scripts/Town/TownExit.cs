using UnityEngine;

/// <summary>
/// The town gate: walking into this trigger starts leaving for a run
/// (Pact Select → descend). Needs a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TownExit : MonoBehaviour
{
    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerStats>() == null) return;
        if (TownManager.I != null) TownManager.I.Depart();
    }
}
