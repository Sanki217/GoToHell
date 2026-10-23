/// <summary>
/// A window a town station opens (weapon rack, smith, shrine, pact picker…).
/// TownStation opens it, freezes the player while IsOpen, and lets go when
/// the panel closes itself (its own Close/Back button).
/// </summary>
public interface ITownPanel
{
    bool IsOpen { get; }
    void Open();
    void Close();
}
