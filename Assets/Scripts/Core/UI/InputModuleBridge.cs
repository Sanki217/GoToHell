using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Keeps every scene's EventSystem on the right UI input module without
/// editing scenes by hand: with the Input System active, an old
/// StandaloneInputModule is swapped for InputSystemUIInputModule on load, so
/// menus work with mouse, gamepad and touch.
///
/// Permanent fix (recommended once): Tools → Go To Hell → UI → Convert
/// EventSystems To Input System — then this bridge finds nothing to do.
/// </summary>
public static class InputModuleBridge
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FixAll();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FixAll();

    public static void FixAll()
    {
#if ENABLE_INPUT_SYSTEM
        foreach (EventSystem es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Fix(es.gameObject);
#endif
    }

    /// <summary>Swaps the legacy module on one EventSystem object. Returns true if it changed.</summary>
    public static bool Fix(GameObject eventSystem)
    {
#if ENABLE_INPUT_SYSTEM
        if (eventSystem == null || eventSystem.GetComponent<InputSystemUIInputModule>() != null) return false;
        if (!eventSystem.TryGetComponent(out StandaloneInputModule legacy)) return false;

        Object.DestroyImmediate(legacy);
        InputSystemUIInputModule module = eventSystem.AddComponent<InputSystemUIInputModule>();
        module.AssignDefaultActions();
        return true;
#else
        return false;
#endif
    }
}
