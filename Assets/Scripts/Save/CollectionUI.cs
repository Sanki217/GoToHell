using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Collection scene controller. Four tabs — Classes, Weapons, Pacts, Upgrades —
/// each filling a grid with CollectionItemUI entries (locked ones darkened + "???").
/// Upgrades are always visible (never locked).
///
/// Data sources (no Inspector lists to maintain — new content shows up automatically):
///   Classes  → Resources/Classes/  (ClassDefinition assets)
///   Weapons  → Resources/Weapons/  (WeaponDefinition assets)
///   Pacts    → Resources/Pacts/    (PactDefinition assets)
///   Upgrades → Resources/UpgradePool.asset (PlayerUpgradePool prefab list)
/// </summary>
public class CollectionUI : MonoBehaviour
{
    [Header("Content")]
    public Transform contentContainer;
    public GameObject itemPrefab;   // has CollectionItemUI

    [Header("Tabs")]
    public Button classesTabButton;
    public Button weaponsTabButton;
    public Button pactsTabButton;
    public Button upgradesTabButton;
    public Button backButton;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        if (classesTabButton  != null) classesTabButton.onClick.AddListener(ShowClasses);
        if (weaponsTabButton  != null) weaponsTabButton.onClick.AddListener(ShowWeapons);
        if (pactsTabButton    != null) pactsTabButton.onClick.AddListener(ShowPacts);
        if (upgradesTabButton != null) upgradesTabButton.onClick.AddListener(ShowUpgrades);
        if (backButton        != null) backButton.onClick.AddListener(SceneFlow.GoToMainMenu);

        ShowClasses();
    }

    // ================================================================
    //  TABS
    // ================================================================

    private void ShowClasses()
    {
        Clear();
        foreach (ClassDefinition def in Resources.LoadAll<ClassDefinition>("Classes"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsClassUnlocked(def.classId);
            Spawn(def.sprite, def.displayName, unlocked);
        }
    }

    private void ShowWeapons()
    {
        Clear();
        foreach (WeaponDefinition def in Resources.LoadAll<WeaponDefinition>("Weapons"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsWeaponUnlocked(def.weaponId);
            Spawn(def.sprite, def.displayName, unlocked);
        }
    }

    private void ShowPacts()
    {
        Clear();
        foreach (PactDefinition def in Resources.LoadAll<PactDefinition>("Pacts"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsPactUnlocked(def.pactId);
            Spawn(def.icon, def.displayName, unlocked);
        }
    }

    private void ShowUpgrades()
    {
        Clear();

        PlayerUpgradePool pool = Resources.Load<PlayerUpgradePool>("UpgradePool");
        if (pool == null)
        {
            Debug.LogWarning("[CollectionUI] No PlayerUpgradePool at Resources/UpgradePool.");
            return;
        }

        foreach (GameObject prefab in pool.upgradePrefabs)
        {
            if (prefab == null) continue;
            PlayerUpgrade upgrade = prefab.GetComponent<PlayerUpgrade>();
            if (upgrade == null) continue;

            // Upgrades are always visible — never locked
            Spawn(upgrade.icon, upgrade.displayName, true);
        }
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void Clear()
    {
        foreach (GameObject go in spawnedItems)
            if (go != null) Destroy(go);
        spawnedItems.Clear();
    }

    private void Spawn(Sprite icon, string displayName, bool unlocked)
    {
        if (contentContainer == null || itemPrefab == null) return;

        GameObject go = Instantiate(itemPrefab, contentContainer);
        spawnedItems.Add(go);

        CollectionItemUI item = go.GetComponent<CollectionItemUI>();
        if (item != null) item.Setup(icon, displayName, unlocked);
    }
}
