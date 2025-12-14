using UnityEngine;

public class EnemyType : MonoBehaviour
{
    private EnemyMovement movement;
    private EnemyBehavior behavior;

    private void Awake()
    {
        movement = GetComponent<EnemyMovement>();
        behavior = GetComponent<EnemyBehavior>();
    }

    private void FixedUpdate()
    {
        movement?.TickMovement();
        behavior?.TickBehavior();
    }
}
