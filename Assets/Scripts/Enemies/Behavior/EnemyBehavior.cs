using UnityEngine;

public abstract class EnemyBehavior : MonoBehaviour
{
    protected Transform player;
    protected Enemy enemy;

    protected virtual void Awake()
    {
        enemy = GetComponent<Enemy>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    public abstract void TickBehavior();
}
