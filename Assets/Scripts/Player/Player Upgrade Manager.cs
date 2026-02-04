using System.Collections.Generic;
using System;
using UnityEngine;

public class PlayerUpgradeManager : MonoBehaviour
{
    [SerializeField]
    private List<string> debugActiveUpgrades = new();

    private void Update()
    {
        debugActiveUpgrades.Clear();
        foreach (var kv in activeUpgrades)
        {
            debugActiveUpgrades.Add($"{kv.Key} (Lv {kv.Value.level})");
        }
    }

    // === EVENTS (Core Signals) ===
    public event Action<Vector3> OnArrowFired;
    public event Action<GameObject> OnArrowHitEnemy;

    public event Action OnDashStarted;
    public event Action OnDashEnded;

    public event Action OnWallSlideStart;
    public event Action<float> OnWallSlideTick;
    public event Action OnWallSlideEnd;

    public event Action OnHoverStart;
    public event Action<float> OnHoverTick;
    public event Action OnHoverEnd;

    public event Action<GameObject> OnEnemyKilled;
    public event Action<float> OnEnergyGained;
    public event Action<float> OnDamageTaken;

    // === UPGRADE STORAGE ===
    private Dictionary<string, PlayerUpgradeInstance> activeUpgrades =
        new Dictionary<string, PlayerUpgradeInstance>();

    // === EVENT TRIGGERS ===
    public void FireArrow(Vector3 direction) => OnArrowFired?.Invoke(direction);
    public void ArrowHitEnemy(GameObject enemy) => OnArrowHitEnemy?.Invoke(enemy);

    public void DashStart() => OnDashStarted?.Invoke();
    public void DashEnd() => OnDashEnded?.Invoke();

    public void WallSlideStart() => OnWallSlideStart?.Invoke();
    public void WallSlideTick(float deltaTime) => OnWallSlideTick?.Invoke(deltaTime);
    public void WallSlideEnd() => OnWallSlideEnd?.Invoke();

    public void HoverStart() => OnHoverStart?.Invoke();
    public void HoverTick(float deltaTime) => OnHoverTick?.Invoke(deltaTime);
    public void HoverEnd() => OnHoverEnd?.Invoke();

    public void EnemyKilled(GameObject enemy) => OnEnemyKilled?.Invoke(enemy);
    public void EnergyGained(float amount) => OnEnergyGained?.Invoke(amount);
    public void DamageTaken(float amount) => OnDamageTaken?.Invoke(amount);

    public void ApplyUpgrade(PlayerUpgrade upgrade)
    {
        if (!activeUpgrades.TryGetValue(upgrade.Id, out var instance))
        {
            instance = new PlayerUpgradeInstance(upgrade);
            activeUpgrades.Add(upgrade.Id, instance);
            upgrade.OnAdded(this);
        }

        instance.level++;
        upgrade.OnLevelUp(this, instance.level);

        Debug.Log($"[Upgrade] {upgrade.Id} → Level {instance.level}");
    }

}
