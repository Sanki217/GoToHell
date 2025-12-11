using UnityEngine;

public class KeepDistance : EnemyBehavior
{
    public float preferredDistance = 4f;
    public float speed = 2f;

    public override void TickBehavior()
    {
        if (!player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist < preferredDistance - 0.5f)
        {
            // move away
            Vector3 dir = (transform.position - player.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
        }
        else if (dist > preferredDistance + 0.5f)
        {
            // move closer
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
        }
    }
}