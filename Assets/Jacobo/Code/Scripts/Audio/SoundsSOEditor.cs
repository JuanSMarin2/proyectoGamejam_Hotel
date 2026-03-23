#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CustomEditor(typeof(SoundsSO))]
public class SoundsSOEditor : Editor
{
    private SerializedProperty soundsProp;
    private string newSoundNameDraft = string.Empty;

    private void OnEnable()
    {
        soundsProp = serializedObject.FindProperty("sounds");
        SyncTargetsWithEnumAndId();
    }

    [InitializeOnLoadMethod]
    private static void SyncAllSoundsSOAfterReload()
    {
        // Keep SO entries aligned after script compilation / enum updates.
        string[] guids = AssetDatabase.FindAssets("t:SoundsSO");
        bool anyDirty = false;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SoundsSO so = AssetDatabase.LoadAssetAtPath<SoundsSO>(path);
            if (so == null || so.sounds == null) continue;

            if (SyncOneAsset(so))
            {
                EditorUtility.SetDirty(so);
                anyDirty = true;
            }
        }

        if (anyDirty)
            AssetDatabase.SaveAssets();
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // Extra hook to ensure sync happens after domain reload in all editor states.
        SyncAllSoundsSOAfterReload();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (soundsProp == null)
            soundsProp = serializedObject.FindProperty("sounds");

        if (GUILayout.Button("Sync SO <-> Enum (now)"))
        {
            SyncTargetsWithEnumAndId();
            serializedObject.Update();
        }

        EditorGUILayout.HelpBox(
            "Enum-first workflow: use SoundType in code. 'Update enum' regenerates SoundValues.cs from current SoundsSO entries.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Create New Sound", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            newSoundNameDraft = EditorGUILayout.TextField("New Sound Name", newSoundNameDraft);

            GUI.enabled = !string.IsNullOrWhiteSpace(newSoundNameDraft);
            if (GUILayout.Button("Add + Update Enum", GUILayout.Width(150)))
            {
                AddNewSoundFromDraft();
                serializedObject.Update();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Update enum"))
            {
                UpdateEnumFromCurrentEntries();
                serializedObject.Update();
            }

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

        DrawSoundsListByName();
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSoundsListByName()
    {
        if (soundsProp == null)
            return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Sounds", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Entry"))
                soundsProp.arraySize++;

            GUI.enabled = soundsProp.arraySize > 0;
            if (GUILayout.Button("Remove Last"))
                soundsProp.DeleteArrayElementAtIndex(soundsProp.arraySize - 1);
            GUI.enabled = true;
        }

        EditorGUILayout.Space(2);

        for (int i = 0; i < soundsProp.arraySize; i++)
        {
            SerializedProperty element = soundsProp.GetArrayElementAtIndex(i);
            SerializedProperty typeProp = element.FindPropertyRelative("soundType");
            SerializedProperty idProp = element.FindPropertyRelative("id");

            string enumName = typeProp != null && typeProp.enumDisplayNames.Length > typeProp.enumValueIndex && typeProp.enumValueIndex >= 0
                ? typeProp.enumDisplayNames[typeProp.enumValueIndex]
                : "Unknown";

            string idName = idProp != null ? idProp.stringValue : string.Empty;
            string displayName = !string.IsNullOrWhiteSpace(idName) ? idName : enumName;

            element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, $"{i + 1}. {displayName}", true);
            if (!element.isExpanded)
                continue;

            EditorGUILayout.BeginVertical("box");

            if (typeProp != null)
                EditorGUILayout.PropertyField(typeProp);

            // Keep ID synchronized immediately when SoundType changes in inspector.
            if (typeProp != null && idProp != null && typeProp.enumValueIndex >= 0 && typeProp.enumValueIndex < typeProp.enumNames.Length)
            {
                string selectedEnumName = typeProp.enumNames[typeProp.enumValueIndex];
                if (!string.Equals(idProp.stringValue, selectedEnumName, StringComparison.Ordinal))
                    idProp.stringValue = selectedEnumName;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                if (idProp != null)
                    EditorGUILayout.PropertyField(idProp);
            }

            DrawPropertyIfExists(element, "category");
            DrawPropertyIfExists(element, "volume");
            DrawPropertyIfExists(element, "mixer");
            DrawPropertyIfExists(element, "randomVolumeMin");
            DrawPropertyIfExists(element, "randomVolumeMax");
            DrawPropertyIfExists(element, "minPitch");
            DrawPropertyIfExists(element, "maxPitch");
            DrawPropertyIfExists(element, "sounds");

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(90)))
                {
                    soundsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    private void AddNewSoundFromDraft()
    {
        string enumSafeName = ToValidEnumMember(newSoundNameDraft);
        if (string.IsNullOrWhiteSpace(enumSafeName))
        {
            EditorUtility.DisplayDialog("Create New Sound", "Invalid sound name.", "OK");
            return;
        }

        bool addedAny = false;

        foreach (UnityEngine.Object obj in targets)
        {
            SoundsSO so = obj as SoundsSO;
            if (so == null) continue;

            Undo.RecordObject(so, "Add New Sound Entry");

            if (so.sounds == null)
                so.sounds = new List<SoundList>();

            bool alreadyExists = false;
            for (int i = 0; i < so.sounds.Count; i++)
            {
                if (string.Equals(so.sounds[i].id, enumSafeName, StringComparison.Ordinal))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
                continue;

            so.sounds.Add(new SoundList
            {
                id = enumSafeName,
                category = "Default",
                volume = 1f,
                randomVolumeMin = 1f,
                randomVolumeMax = 1f,
                minPitch = 1f,
                maxPitch = 1f,
                sounds = Array.Empty<AudioClip>()
            });

            EditorUtility.SetDirty(so);
            addedAny = true;
        }

        if (!addedAny)
        {
            EditorUtility.DisplayDialog("Create New Sound", $"'{enumSafeName}' already exists.", "OK");
            return;
        }

        // Regenerate enum and re-sync all SO entries so soundType gets assigned.
        UpdateEnumFromCurrentEntries();
        SyncAllSoundsSOAfterReload();

        newSoundNameDraft = string.Empty;
    }

    private static void DrawPropertyIfExists(SerializedProperty parent, string relativePath)
    {
        SerializedProperty prop = parent.FindPropertyRelative(relativePath);
        if (prop != null)
            EditorGUILayout.PropertyField(prop, true);
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
            HashSet<SoundType> existingTypes = new HashSet<SoundType>();
            for (int i = 0; i < so.sounds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(so.sounds[i].id))
                    existingIds.Add(so.sounds[i].id);

                existingTypes.Add(so.sounds[i].soundType);
            }

            int added = 0;
            for (int i = 0; i < enumNames.Length; i++)
            {
                string id = enumNames[i];
                SoundType enumValue = (SoundType)Enum.Parse(typeof(SoundType), id);

                if (existingIds.Contains(id) || existingTypes.Contains(enumValue))
                    continue;

                so.sounds.Add(new SoundList
                {
                    soundType = enumValue,
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

    private void UpdateEnumFromCurrentEntries()
    {
        // Collect enum names from selected SO assets.
        List<string> names = new List<string>();
        HashSet<string> dedupe = new HashSet<string>(StringComparer.Ordinal);

        foreach (UnityEngine.Object obj in targets)
        {
            SoundsSO so = obj as SoundsSO;
            if (so == null || so.sounds == null) continue;

            Undo.RecordObject(so, "Update Enum From SoundsSO");

            for (int i = 0; i < so.sounds.Count; i++)
            {
                SoundList entry = so.sounds[i];

                string candidate = !string.IsNullOrWhiteSpace(entry.id)
                    ? entry.id.Trim()
                    : entry.soundType.ToString();

                string enumSafeName = ToValidEnumMember(candidate);
                if (string.IsNullOrWhiteSpace(enumSafeName))
                    continue;

                if (dedupe.Add(enumSafeName))
                    names.Add(enumSafeName);

                // Keep SO entry aligned to the enum-safe ID text.
                entry.id = enumSafeName;
                if (Enum.TryParse(enumSafeName, out SoundType parsedType))
                    entry.soundType = parsedType;
                so.sounds[i] = entry;
            }

            EditorUtility.SetDirty(so);
        }

        if (names.Count == 0)
        {
            EditorUtility.DisplayDialog("Update enum", "No valid sound entries were found to generate enum values.", "OK");
            return;
        }

        names.Sort(StringComparer.Ordinal);
        WriteSoundValuesEnumFile(names);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // After enum recompilation, ensure SO entries are immediately re-synced.
        SyncAllSoundsSOAfterReload();
    }

    private static string ToValidEnumMember(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string cleaned = raw.Trim();
        cleaned = Regex.Replace(cleaned, "[^a-zA-Z0-9_]", "_");
        cleaned = Regex.Replace(cleaned, "_+", "_");

        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        if (char.IsDigit(cleaned[0]))
            cleaned = "S_" + cleaned;

        return cleaned;
    }

    private static void WriteSoundValuesEnumFile(List<string> enumNames)
    {
        const string outputPath = "Assets/Jacobo/Code/Scripts/Audio/SoundValues.cs";

        StringBuilder sb = new StringBuilder(512);
        sb.AppendLine("public enum SoundType");
        sb.AppendLine("{");

        for (int i = 0; i < enumNames.Count; i++)
        {
            string suffix = i < enumNames.Count - 1 ? "," : string.Empty;
            sb.Append("    ").Append(enumNames[i]).AppendLine(suffix);
        }

        sb.AppendLine("}");

        string directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
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

            for (int i = 0; i < so.sounds.Count; i++)
            {
                SoundList entry = so.sounds[i];
                if (string.IsNullOrWhiteSpace(entry.id))
                    entry.id = entry.soundType.ToString();
                so.sounds[i] = entry;
            }

            EditorUtility.SetDirty(so);
        }

        AssetDatabase.SaveAssets();
    }

    private void SyncTargetsWithEnumAndId()
    {
        bool anyDirty = false;

        foreach (UnityEngine.Object obj in targets)
        {
            SoundsSO so = obj as SoundsSO;
            if (so == null) continue;

            Undo.RecordObject(so, "Sync SO <-> Enum");
            bool changed = SyncOneAsset(so);
            if (!changed) continue;

            anyDirty = true;
            EditorUtility.SetDirty(so);
        }

        if (anyDirty)
            AssetDatabase.SaveAssets();
    }

    private static bool SyncOneAsset(SoundsSO so)
    {
        if (so == null || so.sounds == null)
            return false;

        bool changed = false;

        for (int i = 0; i < so.sounds.Count; i++)
        {
            SoundList entry = so.sounds[i];
            SoundList original = entry;

            if (!string.IsNullOrWhiteSpace(entry.id) && Enum.TryParse(entry.id.Trim(), true, out SoundType parsed))
            {
                entry.soundType = parsed;
                entry.id = parsed.ToString();
            }
            else
            {
                entry.id = entry.soundType.ToString();
            }

            if (!entry.Equals(original))
            {
                so.sounds[i] = entry;
                changed = true;
            }
        }

        return changed;
    }
}

#endif