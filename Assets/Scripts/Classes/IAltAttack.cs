/// <summary>
/// A component that can serve as a class's Shift+LMB alt attack. The same
/// component may be a primary weapon for one class and a small alt for
/// another (PlayerSlash: Warrior sword vs Rogue kick) — RunConfigApplier
/// tells it which role it has this run.
/// </summary>
public interface IAltAttack
{
    void SetAltAttackMode(bool isAlt);
}
