using UnityEngine;

public class HorizontalMovement : EnemyMovement
{
    [Header("Movement")]
    public float maxSpeed = 3f;
    public float acceleration = 5f;

    [Header("Turning")]
    public float turnBrake = 20f;
    public bool instantTurn = false;

    [Header("Detection")]
    public HorizontalSensor sensor;

    private float direction = 1f;   // 1 = right, -1 = left
    private float currentSpeed = 0f;

    protected override void Awake()
    {
        base.Awake();

        if (sensor != null)
        {
            sensor.OnHitLeft += HandleHitLeft;
            sensor.OnHitRight += HandleHitRight;
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

        transform.position += Vector3.right * currentSpeed * Time.deltaTime;
    }

    private void HandleHitLeft()
    {
        Turn(1f);
    }

    private void HandleHitRight()
    {
        Turn(-1f);
    }

    private void Turn(float newDirection)
    {
        direction = newDirection;

        if (instantTurn)
        {
            currentSpeed = 0f;
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                turnBrake * Time.deltaTime
            );
        }
    }
}
