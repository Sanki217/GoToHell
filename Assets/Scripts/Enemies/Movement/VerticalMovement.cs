using UnityEngine;

public class VerticalMovement : EnemyMovement
{
  
    public float amplitude = 1f;
    public float speed = 1f;
    private Vector3 startPos;

    protected override void Awake()
    {
        base.Awake();
        startPos = transform.position;
    }

    public override void TickMovement()
    {
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
    }
}
