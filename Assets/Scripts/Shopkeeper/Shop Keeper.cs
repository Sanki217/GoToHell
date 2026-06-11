using UnityEngine;
using System.Collections.Generic;

public class ShopKeeper : MonoBehaviour
{
    [Header("Item Pool — assign item prefabs here")]
    public List<GameObject> itemPrefabs;

    [Header("Slots — assign 3 child Transforms")]
    public Transform[] itemSlots = new Transform[3];

    private void Start()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0) return;

        foreach (Transform slot in itemSlots)
        {
            if (slot == null) continue;
            int idx = Random.Range(0, itemPrefabs.Count);
            Instantiate(itemPrefabs[idx], slot.position, slot.rotation, slot);
        }
    }
}