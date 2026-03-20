#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundsSO))]
public class SoundsSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "IDs are now independent from enum order. You can add/reorder sounds freely. Use 'category' to group entries.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Missing Enum Entries"))
            {
                CreateMissingEnumEntries();
                serializedObject.Update();
            }

            if (GUILayout.Button("Sort by ID (A-Z)"))
            {
                SortEntriesById();
                serializedObject.Update();
            }
        }

        EditorGUILayout.Space(4);

        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();
    }

    private void CreateMissingEnumEntries()
    {
        string[] enumNames = Enum.GetNames(typeof(SoundType));

        foreach (UnityEngine.Object obj in targets)
        {
            SoundsSO so = obj as SoundsSO;
            if (so == null) continue;

            Undo.RecordObject(so, "Create Missing Enum Entries");

            if (so.sounds == null)
                so.sounds = new List<SoundList>();

            HashSet<string> existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < so.sounds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(so.sounds[i].id))
                    existingIds.Add(so.sounds[i].id);
            }

            int added = 0;
            for (int i = 0; i < enumNames.Length; i++)
            {
                string id = enumNames[i];
                if (existingIds.Contains(id))
                    continue;

                so.sounds.Add(new SoundList
                {
                    id = id,
                    category = "Default",
                    volume = 1f,
                    randomVolumeMin = 1f,
                    randomVolumeMax = 1f,
                    minPitch = 1f,
                    maxPitch = 1f,
                    sounds = Array.Empty<AudioClip>()
                });

                added++;
            }

            if (added > 0)
                EditorUtility.SetDirty(so);
        }

        AssetDatabase.SaveAssets();
    }

    private void SortEntriesById()
    {
        foreach (UnityEngine.Object obj in targets)
        {
            SoundsSO so = obj as SoundsSO;
            if (so == null || so.sounds == null || so.sounds.Count <= 1)
                continue;

            Undo.RecordObject(so, "Sort Sounds By ID");

            so.sounds.Sort((a, b) =>
                string.Compare(a.id ?? string.Empty, b.id ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            EditorUtility.SetDirty(so);
        }

        AssetDatabase.SaveAssets();
    }
}

#endif