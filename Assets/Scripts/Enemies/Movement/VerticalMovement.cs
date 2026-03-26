using UnityEngine;

public class VerticalMovement : EnemyMovement
{
    [Header("Movement")]
    public float maxSpeed = 3f;
    public float acceleration = 5f;

    [Header("Turning")]
    public float turnBrake = 20f;
    public bool instantTurn = false;

    [Header("Detection")]
    public VerticalSensor sensor;

    [Header("Y Bounds — set to the Y positions of ceiling and floor")]
    public float minY = -500f;
    public float maxY = 500f;
    public bool useBounds = true;

    private float direction = 1f;   // 1 = up, -1 = down
    private float currentSpeed = 0f;

    protected override void Awake()
    {
        base.Awake();

        if (sensor != null)
        {
            sensor.OnHitCeiling += HandleHitCeiling;
            sensor.OnHitGround += HandleHitGround;
        }
    }

    public override void TickMovement()
    {
        float targetSpeed = direction * maxSpeed;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            acceleration * Time.deltaTime
        );

        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        // Hard clamp to bounds
        if (useBounds)
        {
            Vector3 pos = transform.position;

            if (pos.y <= minY && direction < 0f)
            {
                Turn(1f);
                pos.y = minY;
            }
            else if (pos.y >= maxY && direction > 0f)
            {
                Turn(-1f);
                pos.y = maxY;
            }

            transform.position = pos;
        }
    }

    private void HandleHitCeiling() => Turn(-1f);
    private void HandleHitGround() => Turn(1f);

    private void Turn(float newDirection)
    {
        direction = newDirection;

        if (instantTurn)
        {
            currentSpeed = 0f;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, turnBrake * Time.deltaTime);
        }
    }
}