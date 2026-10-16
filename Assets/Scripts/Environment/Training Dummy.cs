using UnityEngine;

/// <summary>
/// Hinged practice target. Never dies: HP regenerates after regenDelay
/// seconds without hits, and every hit swings it with torque.
/// Physics-driven (HingeJoint), so it opts out of Enemy's body constraints.
/// </summary>
public class TrainingDummy : Enemy
{
    [Header("Training Dummy")]
    public int dummyMaxHealth = 1000;
    public float regenDelay = 1f;

    [Header("Swing Physics")]
    public float torquePerDamage = 2f;
    public float springStrength = 15f;
    public float swingDamping = 1.5f;

    private Rigidbody rb;
    private HingeJoint hinge;
    private int currentHP;
    private float lastHitTime = -999f;
    private bool regenPending = false;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
        currentHP = dummyMaxHealth;
    }

    /// <summary>The dummy swings on its hinge — leave its rigidbody untouched.</summary>
    protected override void ConfigureBody(Rigidbody body) { }

    private void FixedUpdate()
    {
        if (rb == null || hinge == null) return;

        float angle = hinge.angle;
        float springTorque = -angle * springStrength;
        float dampingTorque = -rb.angularVelocity.z * Mathf.Rad2Deg * swingDamping;
        rb.AddTorque(new Vector3(0f, 0f, springTorque + dampingTorque), ForceMode.Acceleration);
    }

    private void Update()
    {
        if (regenPending && Time.time - lastHitTime >= regenDelay)
        {
            currentHP = dummyMaxHealth;
            regenPending = false;
        }
    }

    public override bool TakeDamage(int amount, Vector3 hitPosition, Vector3 knockbackDir,
                                    float knockbackForce, bool isCrit, FloatingTextManager.HitType hitType)
    {
        currentHP -= amount;
        lastHitTime = Time.time;
        regenPending = true;

        FloatingTextManager.Show(amount, hitPosition,
            isCrit ? FloatingTextManager.HitType.Critical : hitType);

        PlayerRefs refs = PlayerRefs.I;
        if (refs != null && refs.Killstreak != null) refs.Killstreak.RegisterDamageDealt();

        float torqueZ = -knockbackDir.x * amount * torquePerDamage;
        if (Mathf.Approximately(torqueZ, 0f))
            torqueZ = -amount * torquePerDamage;

        if (rb != null)
            rb.AddTorque(new Vector3(0f, 0f, torqueZ), ForceMode.Impulse);

        return false;   // the dummy never dies
    }
}
