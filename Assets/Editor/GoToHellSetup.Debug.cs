using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Debug overlay setup (Tools → Go To Hell → Debug).</summary>
public static partial class GoToHellSetup
{
    [MenuItem("Tools/Go To Hell/Debug/Add Debug Overlay (open scenes)")]
    public static void AddDebugOverlay()
    {
        int added = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            bool has = false;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<DebugOverlay>(true) != null) { has = true; break; }
            if (has) continue;

            var go = new GameObject("DebugOverlay");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<DebugOverlay>();
            Undo.RegisterCreatedObjectUndo(go, "Add Debug Overlay");
            EditorSceneManager.MarkSceneDirty(scene);
            added++;
        }
        Debug.Log(added > 0
            ? $"[GoToHellSetup] DebugOverlay added to {added} scene(s). Press ` in Play Mode to toggle. Save the scene(s)."
            : "[GoToHellSetup] Every open scene already has a DebugOverlay.");
    }
}
