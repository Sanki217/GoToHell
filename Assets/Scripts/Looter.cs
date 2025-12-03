using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class Looter : MonoBehaviour
{
    [Header("Looter (child of Player)")]
    public Transform playerTransform; // usually parent
    private PlayerInventory playerInventory;

    // option: auto-detect orb components in trigger enter
    void Start()
    {
        if (playerTransform == null && transform.parent != null)
            playerTransform = transform.parent;

        if (playerTransform != null)
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Orb>(out Orb orb))
        {
            // tell the orb to attract to the player
            orb.StartAttract(playerTransform, playerInventory);
        }
    }

    // no need for OnTriggerExit for this implementation
}
