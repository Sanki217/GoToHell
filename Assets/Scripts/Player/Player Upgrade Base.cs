using UnityEngine;

public abstract class PlayerUpgradeBase : MonoBehaviour
{
    [HideInInspector]
    public int level = 1;

    protected PlayerUpgradeManager manager;

    protected virtual void Awake()
    {
        manager = GetComponent<PlayerUpgradeManager>();
    }

    public virtual void OnUpgradeAdded() { }
    public virtual void OnUpgradeRemoved() { }

    public virtual void OnUpgradeLevelUp()
    {
        level++;
    }
}
