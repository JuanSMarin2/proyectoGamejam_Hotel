#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrafficCar), true)]
public class TrafficCarEditor : Editor
{
    private bool showAdvanced;
    private bool showCurve;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawImportant();

        EditorGUILayout.Space(6);
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Avanzado", true);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            DrawAdvancedTrafficCarFields();
            DrawCurvedTrafficCarFieldsIfPresent();
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawImportant()
    {
        EditorGUILayout.LabelField("Importante", EditorStyles.boldLabel);

        DrawProperty("baseSpeedMin");
        DrawProperty("baseSpeedMax");

        EditorGUILayout.Space(4);
        DrawProperty("spriteRenderer");
        DrawProperty("spriteOptions");

        EditorGUILayout.Space(4);
        DrawProperty("obstacleLayerMask");
        DrawProperty("carLayerMask");

        EditorGUILayout.Space(4);
        DrawProperty("preferredDirection");
        DrawProperty("forwardWeight");

        EditorGUILayout.Space(4);
        DrawProperty("obstacleDetectDistance");
        DrawProperty("separationRadius");
    }

    private void DrawAdvancedTrafficCarFields()
    {
        EditorGUILayout.LabelField("Tráfico / Steering", EditorStyles.boldLabel);

        DrawProperty("obstacleRayAngles");
        DrawProperty("rayOriginOffset");
        DrawProperty("obstacleDetectInterval");
        DrawProperty("avoidanceWeight");
        DrawProperty("unstuckSideBias");

        EditorGUILayout.Space(4);
        DrawProperty("separationInterval");
        DrawProperty("separationWeight");

        EditorGUILayout.Space(4);
        DrawProperty("trafficDetectDistance");
        DrawProperty("trafficDetectInterval");
        DrawProperty("desiredGap");
        DrawProperty("followGain");
        DrawProperty("matchLeadCarSpeed");
        DrawProperty("accelerationTime");
        DrawProperty("decelerationTime");

        EditorGUILayout.Space(4);
        DrawProperty("directionSmoothTime");
        DrawProperty("maxSteeringMagnitude");
    }

    private void DrawCurvedTrafficCarFieldsIfPresent()
    {
        // Estos campos existen solo en CurvedTrafficCar.
        SerializedProperty curveSource = serializedObject.FindProperty("curveSource");
        if (curveSource == null)
            return;

        EditorGUILayout.Space(6);
        showCurve = EditorGUILayout.Foldout(showCurve, "Curva (CurvedTrafficCar)", true);
        if (!showCurve)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(curveSource);
        DrawProperty("curveIntensity");
        DrawProperty("curveFrequency");
        DrawProperty("curveDirectionSmoothTime");
        DrawProperty("perlinSeed");
        DrawProperty("timeOffset");
        EditorGUI.indentLevel--;
    }

    private void DrawProperty(string name)
    {
        SerializedProperty prop = serializedObject.FindProperty(name);
        if (prop != null)
            EditorGUILayout.PropertyField(prop);
    }
}
#endif
