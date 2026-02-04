public abstract class PlayerUpgrade
{
    public abstract string Id { get; }

    // Called once when first acquired
    public virtual void OnAdded(PlayerUpgradeManager mgr) { }

    // Called every time upgrade level increases
    public virtual void OnLevelUp(PlayerUpgradeManager mgr, int newLevel) { }
}
