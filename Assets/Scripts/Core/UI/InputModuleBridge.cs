using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Editor helper: swaps an EventSystem's StandaloneInputModule for
/// InputSystemUIInputModule (mouse, gamepad and touch). Used by
/// Tools → Go To Hell → UI → Convert EventSystems To Input System — the
/// change is saved in the scene, nothing happens at runtime.
/// </summary>
public static class InputModuleBridge
{
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
