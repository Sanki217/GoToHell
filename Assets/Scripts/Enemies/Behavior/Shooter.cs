using UnityEngine;

public class Shooter : EnemyBehavior
{
    public GameObject projectilePrefab;
    public float shootInterval = 2f;
    private float timer = 0f;

    public override void TickBehavior()
    {
        if (!player) return;

        timer += Time.deltaTime;
        if (timer >= shootInterval)
        {
            timer = 0f;

            Vector3 dir = (player.position - transform.position).normalized;
            Instantiate(projectilePrefab, transform.position, Quaternion.LookRotation(dir));
        }
    }
}
