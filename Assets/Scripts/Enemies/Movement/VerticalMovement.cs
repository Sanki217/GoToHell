using UnityEngine;

public class VerticalMovement : EnemyMovement
{
    [Header("Movement")]
    public float maxSpeed = 3f;
    public float acceleration = 5f;

    [Header("Detection")]
    public VerticalSensor sensor;  // reference to the sensor child

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
        // accelerate smoothly toward target velocity
        float targetSpeed = direction * maxSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        // move by velocity
        transform.position += Vector3.up * currentSpeed * Time.deltaTime;
    }

    private void HandleHitCeiling()
    {
        direction = -1f;
    }

    private void HandleHitGround()
    {
        direction = 1f;
    }
}
