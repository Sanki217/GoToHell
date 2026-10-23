using System;
using UnityEngine;

/// <summary>
/// Base for a class's ACTIVE skill (F / gamepad LB by default — rebindable as
/// "ClassSkill"). One subclass per skill, one file each. The class asset
/// names the component (ClassDefinition.activeSkillComponentName);
/// RunConfigApplier enables only that one.
///
/// Cooldown shrinks with the Cooldown stat. Class run-upgrades tune a skill
/// through the modifier fields (radius, duration, cooldown, potency) — they
/// stack multiplicatively / additively and reset with the player every scene.
/// </summary>
public abstract class ClassSkill : MonoBehaviour, IDebugDrawable
{
    [Header("Skill")]
    public string skillName = "Skill";
    [Tooltip("Seconds between uses before the Cooldown stat.")]
    public float baseCooldown = 12f;
    [Tooltip("Energy spent per use (0 = free).")]
    public float energyCost = 0f;
    [Tooltip("Cooldown reduction per Cooldown stat point (0.03 = 3%).")]
    public float cooldownReductionPerPoint = 0.03f;
    [Tooltip("Cooldown never drops below this fraction of the base.")]
    public float minCooldownFraction = 0.4f;
    [Tooltip("Radius growth per Size point (0.03 = 3%).")]
    public float radiusPerSize = 0.03f;

    [Header("Feedback")]
    public Color effectColor = new Color(1f, 1f, 1f, 0.35f);
    [Tooltip("Pop a \"<skill> ready\" text over the player when the cooldown ends.")]
    public bool announceReady = true;

    // ---- Run-upgrade modifiers (class upgrades write these) -------------
    [NonSerialized] public float radiusMultiplier = 1f;
    [NonSerialized] public float cooldownMultiplier = 1f;
    [NonSerialized] public float potencyMultiplier = 1f;
    [NonSerialized] public float durationBonus = 0f;

    /// <summary>Fired whenever any class skill is used.</summary>
    public static event Action<ClassSkill> OnUsed;

    protected PlayerStats stats;
    protected PlayerHealth health;
    protected PlayerEnergy energy;
    protected PlayerUpgradeManager upgrades;
    private PlayerStateController state;
    private float readyAt;
    private bool announced = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics() => OnUsed = null;

    protected virtual void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        energy = GetComponent<PlayerEnergy>();
        upgrades = GetComponent<PlayerUpgradeManager>();
        state = GetComponent<PlayerStateController>();
    }

    public float Cooldown
    {
        get
        {
            float cd = stats != null ? stats.cooldown : 0f;
            float factor = Mathf.Max(minCooldownFraction, 1f - cd * cooldownReductionPerPoint);
            return baseCooldown * factor * cooldownMultiplier;
        }
    }

    public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
    public bool IsReady => Time.time >= readyAt;
    /// <summary>0 = just used, 1 = ready.</summary>
    public float ReadyFraction => Cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / Cooldown);

    protected float Size => stats != null ? stats.size : 0f;
    protected float Scaled(float baseRadius) => baseRadius * radiusMultiplier * (1f + radiusPerSize * Size);

    protected virtual void Update()
    {
        if (!announced && IsReady)
        {
            announced = true;
            if (announceReady) FloatingTextManager.ShowText($"{skillName} ready", transform.position + Vector3.up * 1.5f);
        }

        if (state != null && !state.HasControl()) return;
        if (!GameInput.ClassSkillPressed || !IsReady) return;

        if (energyCost > 0f && energy != null && !energy.SpendEnergy(energyCost))
        {
            energy.FlashInsufficient();
            return;
        }

        if (!Activate()) return;
        readyAt = Time.time + Cooldown;
        announced = false;
        OnUsed?.Invoke(this);
    }

    /// <summary>Do the skill. Return false if it could not be used (no cooldown is spent).</summary>
    protected abstract bool Activate();

    /// <summary>Plain-text description for menus.</summary>
    public virtual string Describe() => skillName;

    /// <summary>Unscaled base radius shown by the debug overlay (0 = none).</summary>
    protected virtual float DebugRadius => 0f;

    public void DrawDebug()
    {
        Vector3 p = transform.position;
        float r = DebugRadius > 0f ? Scaled(DebugRadius) : 0f;
        if (r > 0f) DebugDraw.Range(p, r, DebugColors.Player, skillName);
        DebugDraw.Label(p + Vector3.up * 1.6f,
            IsReady ? $"{skillName} READY (cd {Cooldown:0.0}s)" : $"{skillName} {CooldownRemaining:0.0}s",
            DebugColors.Player, 3);
    }

    protected void Shake(float magnitude, float duration)
    {
        CameraFollow cam = PlayerRefs.CamFollow;
        if (cam != null) cam.Shake(magnitude, duration);
    }
}
