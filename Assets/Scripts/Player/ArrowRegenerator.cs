using UnityEngine;

/// <summary>
/// Bow weapon component — regenerates one arrow every secondsPerArrow while
/// the quiver is below max. Replaces the empty-quiver slash fallback now that
/// slash belongs to the Sword weapon.
///
/// Uses PlayerShooting.RestoreArrow() so regenerated arrows behave exactly like
/// picked-up arrows (UI + refill events, e.g. New Sharp Set).
///
/// Disabled by default on the player prefab — enabled by RunConfigApplier when
/// the Bow is the selected weapon (listed in its weaponComponentNames).
/// </summary>
public class ArrowRegenerator : MonoBehaviour
{
    [Header("Regeneration")]
    public float secondsPerArrow = 3f;

    private PlayerShooting shooting;
    private float timer;

    private void Start()
    {
        shooting = GetComponent<PlayerShooting>();
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (shooting == null || shooting.HasMaxArrows())
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;
        if (timer >= secondsPerArrow)
        {
            timer = 0f;
            shooting.RestoreArrow();
        }
    }
}
