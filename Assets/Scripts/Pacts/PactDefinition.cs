using UnityEngine;

/// <summary>
/// A pact: a game-changing run modifier the player can select at character creation.
/// This is the minimal definition — display data + stable id. Gameplay effects
/// are layered on in Step 5.
///
/// Create via: Assets → Create → GoToHell → Pact
/// </summary>
[CreateAssetMenu(menuName = "GoToHell/Pact")]
public class PactDefinition : ScriptableObject
{
    [Tooltip("Stable unique ID used in save data. Never change once shipped.")]
    public string pactId;

    public string displayName;

    [TextArea(2, 5)]
    public string description;

    public Sprite icon;
}