public class Upgrade_Hover_Regen : PlayerUpgrade
{
    public override string Id => "Hover_Regen";

    private int level;
    private PlayerEnergy energy;

    public override void OnAdded(PlayerUpgradeManager mgr)
    {
        energy = mgr.GetComponent<PlayerEnergy>();
        mgr.OnHoverTick += Tick;
    }

    public override void OnLevelUp(PlayerUpgradeManager mgr, int newLevel)
    {
        level = newLevel;
    }

    private void Tick(float dt)
    {
        if (energy)
            energy.RestoreEnergy(level * dt * 2f);
    }
}
