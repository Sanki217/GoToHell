using UnityEngine;

/// <summary>
/// Marker added automatically by Pool.Spawn — links a pooled instance back to
/// its source prefab so Pool.Despawn knows which pool to return it to.
/// Never add this manually.
/// </summary>
public class PooledInstance : MonoBehaviour
{
    [HideInInspector] public GameObject sourcePrefab;
}
