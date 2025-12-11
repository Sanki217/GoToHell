using UnityEngine;

public abstract class EnemyMovement : MonoBehaviour
{
    protected Transform player;
    protected Enemy enemy;

    protected virtual void Awake()
    {
        enemy = GetComponent<Enemy>();
        player = GameObject.FindWithTag("Player")?.transform;
    }

    public abstract void TickMovement();
}
