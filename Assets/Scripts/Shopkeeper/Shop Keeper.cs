using UnityEngine;
using System.Collections.Generic;

public class ShopKeeper : MonoBehaviour
{
    [Header("Item Pool")]
    public List<GameObject> itemPrefabs;

    [Header("Slots")]
    public Transform[] itemSlots = new Transform[3];

    private void Start()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0) return;

        List<GameObject> shuffled = new List<GameObject>(itemPrefabs);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null || i >= shuffled.Count) continue;
            Instantiate(shuffled[i], itemSlots[i].position, itemSlots[i].rotation, itemSlots[i]);
        }
    }
}