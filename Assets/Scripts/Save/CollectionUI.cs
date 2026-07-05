using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Collection scene controller. Five tabs — Classes, Weapons, Pacts, Upgrades
/// (grid of CollectionItemUI tiles) and Leaderboard (run history ranked by
/// max depth reached). Locked grid entries are darkened + "???"; upgrades are
/// always visible.
///
/// Data sources (new content shows up automatically):
///   Classes  → Resources/Classes/  (ClassDefinition assets)
///   Weapons  → Resources/Weapons/  (WeaponDefinition assets)
///   Pacts    → Resources/Pacts/    (PactDefinition assets)
///   Upgrades → Resources/UpgradePool.asset (PlayerUpgradePool prefab list)
///   Leaderboard → SaveManager.Data.runHistory
/// </summary>
public class CollectionUI : MonoBehaviour
{
    [Header("Grid (Classes/Weapons/Pacts/Upgrades)")]
    public GameObject gridScroll;       // the whole grid scroll view
    public Transform contentContainer;
    public GameObject itemPrefab;       // has CollectionItemUI

    [Header("Leaderboard")]
    public GameObject leaderboardPanel; // its own scroll view
    public TMP_Text leaderboardText;
    public int leaderboardMaxRows = 25;

    [Header("Tabs")]
    public Button classesTabButton;
    public Button weaponsTabButton;
    public Button pactsTabButton;
    public Button upgradesTabButton;
    public Button leaderboardTabButton;
    public Button backButton;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();
    private Dictionary<string, string> classNames;
    private Dictionary<string, string> weaponNames;

    // ================================================================
    //  INIT
    // ================================================================

    private void Start()
    {
        if (classesTabButton     != null) classesTabButton.onClick.AddListener(ShowClasses);
        if (weaponsTabButton     != null) weaponsTabButton.onClick.AddListener(ShowWeapons);
        if (pactsTabButton       != null) pactsTabButton.onClick.AddListener(ShowPacts);
        if (upgradesTabButton    != null) upgradesTabButton.onClick.AddListener(ShowUpgrades);
        if (leaderboardTabButton != null) leaderboardTabButton.onClick.AddListener(ShowLeaderboard);
        if (backButton           != null) backButton.onClick.AddListener(SceneFlow.GoToMainMenu);

        ShowClasses();
    }

    // ================================================================
    //  GRID TABS
    // ================================================================

    private void ShowClasses()
    {
        ShowGridMode();
        foreach (ClassDefinition def in Resources.LoadAll<ClassDefinition>("Classes"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsClassUnlocked(def.classId);
            Spawn(def.sprite, def.displayName, unlocked);
        }
    }

    private void ShowWeapons()
    {
        ShowGridMode();
        foreach (WeaponDefinition def in Resources.LoadAll<WeaponDefinition>("Weapons"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsWeaponUnlocked(def.weaponId);
            Spawn(def.sprite, def.displayName, unlocked);
        }
    }

    private void ShowPacts()
    {
        ShowGridMode();
        foreach (PactDefinition def in Resources.LoadAll<PactDefinition>("Pacts"))
        {
            if (def == null) continue;
            bool unlocked = def.unlockedByDefault || SaveManager.IsPactUnlocked(def.pactId);
            Spawn(def.icon, def.displayName, unlocked);
        }
    }

    private void ShowUpgrades()
    {
        ShowGridMode();

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
    //  LEADERBOARD TAB
    // ================================================================

    private void ShowLeaderboard()
    {
        Clear();
        if (gridScroll != null) gridScroll.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(true);
        if (leaderboardText == null) return;

        List<RunStats> runs = new List<RunStats>(SaveManager.Data.runHistory);
        if (runs.Count == 0)
        {
            leaderboardText.text = "No runs yet — go fall down some layers.";
            return;
        }

        runs.Sort((a, b) => b.maxDepthReached.CompareTo(a.maxDepthReached));

        StringBuilder sb = new StringBuilder();
        int rows = Mathf.Min(runs.Count, leaderboardMaxRows);
        for (int i = 0; i < rows; i++)
        {
            RunStats r = runs[i];
            string name = string.IsNullOrEmpty(r.playerName) ? "Sinner" : r.playerName;
            sb.Append($"<b>#{i + 1}</b>   {name}   ")
              .Append($"<color=#BBBBBB>{ClassName(r.classId)} / {WeaponName(r.weaponId)}</color>   ")
              .Append($"<color=#66FFFF>{r.maxDepthReached:F0} m</color>");
            if (r.victory) sb.Append("   <color=#FFD24D>★</color>");
            sb.Append('\n');
        }

        leaderboardText.text = sb.ToString();
    }

    private string ClassName(string id)
    {
        if (classNames == null)
        {
            classNames = new Dictionary<string, string>();
            foreach (ClassDefinition d in Resources.LoadAll<ClassDefinition>("Classes"))
                if (d != null && !string.IsNullOrEmpty(d.classId)) classNames[d.classId] = d.displayName;
        }
        return !string.IsNullOrEmpty(id) && classNames.TryGetValue(id, out string n) ? n : "—";
    }

    private string WeaponName(string id)
    {
        if (weaponNames == null)
        {
            weaponNames = new Dictionary<string, string>();
            foreach (WeaponDefinition d in Resources.LoadAll<WeaponDefinition>("Weapons"))
                if (d != null && !string.IsNullOrEmpty(d.weaponId)) weaponNames[d.weaponId] = d.displayName;
        }
        return !string.IsNullOrEmpty(id) && weaponNames.TryGetValue(id, out string n) ? n : "—";
    }

    // ================================================================
    //  HELPERS
    // ================================================================

    private void ShowGridMode()
    {
        Clear();
        if (gridScroll != null) gridScroll.SetActive(true);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

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
