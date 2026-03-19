#if UNITY_EDITOR
using System.Linq;
using UnityEditor;

/// <summary>
/// Evita spam de "SerializedObjectNotCreatableException: Object at index 0 is null"
/// limpiando la selección cuando apunta a objetos destruidos (típico al entrar/salir de Play).
/// Solo afecta al Editor.
/// </summary>
[InitializeOnLoad]
public static class SelectionNullCleanup
{
    static SelectionNullCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += CleanupSelection;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Darle un tick para que Unity actualice la jerarquía/selección.
        EditorApplication.delayCall += CleanupSelection;
    }

    private static void CleanupSelection()
    {
        var current = Selection.objects;
        if (current == null || current.Length == 0)
            return;

        if (current.All(o => o != null))
            return;

        var filtered = current.Where(o => o != null).ToArray();
        Selection.objects = filtered;
    }
}
#endif
