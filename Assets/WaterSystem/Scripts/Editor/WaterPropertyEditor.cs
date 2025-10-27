﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomEditor(typeof(Water))]
public class WaterPropertyEditor : UnityEditor.Editor
{
    private bool renderingFoldout = true;
    private bool simulationFoldout = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //geometry
        SerializedProperty viewerProperty = serializedObject.FindProperty("viewer");
        SerializedProperty materialProperty = serializedObject.FindProperty("waterMaterial");
        SerializedProperty updateMaterialProperty = serializedObject.FindProperty("updateMaterialProperties");
        SerializedProperty lengthScaleProperty = serializedObject.FindProperty("lengthScale");
        SerializedProperty vertexDensityProperty = serializedObject.FindProperty("vertexDensity");
        SerializedProperty clipMapLevelsProperty = serializedObject.FindProperty("clipLevels");
        SerializedProperty skirtSizeProperty = serializedObject.FindProperty("skirtSize");
        SerializedProperty sssStrengthProperty = serializedObject.FindProperty("sssStrength");
        SerializedProperty sssScaleProperty = serializedObject.FindProperty("sssScale");
        SerializedProperty sssBaseProperty = serializedObject.FindProperty("sssBase");
        SerializedProperty visibilityProperty = serializedObject.FindProperty("visibility");
        SerializedProperty foamStrengthProperty = serializedObject.FindProperty("foamStrength");
        SerializedProperty contactFoamProperty = serializedObject.FindProperty("contactFoamStrength");
        SerializedProperty reflectionStrengthProperty = serializedObject.FindProperty("reflectionStrength");
        SerializedProperty normalFadeFarProperty = serializedObject.FindProperty("normalFadeFar");
        SerializedProperty foamFadeFarProperty = serializedObject.FindProperty("foamFadeFar");
        SerializedProperty geoScaleProperty = serializedObject.FindProperty("geomteryScale");

        //lut
        SerializedProperty colorsPresetProperty = serializedObject.FindProperty("colorsPreset");

        //wave simulation
        SerializedProperty fftSizeLevelProperty = serializedObject.FindProperty("sizeLevel");
        SerializedProperty recalculateProperty = serializedObject.FindProperty("alwaysRecalculateInitials");
        SerializedProperty waveSettingsProperty = serializedObject.FindProperty("wavesSettings");
        
        SerializedProperty scale0Property = serializedObject.FindProperty("lengthScale0");
        SerializedProperty scale1Property = serializedObject.FindProperty("lengthScale1");
        SerializedProperty scale2Property = serializedObject.FindProperty("lengthScale2");

        // Rendering category
        renderingFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(renderingFoldout, "Rendering");
        if (renderingFoldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(viewerProperty, new GUIContent("Viewer"));
            EditorGUILayout.PropertyField(materialProperty, new GUIContent("Material"));
            EditorGUILayout.PropertyField(lengthScaleProperty, new GUIContent("Length Scale"));
            //EditorGUILayout.PropertyField(updateMaterialProperty, new GUIContent("UpdateMaterial"));
            EditorGUILayout.PropertyField(clipMapLevelsProperty, new GUIContent("Clip Map Levels"));
            EditorGUILayout.PropertyField(vertexDensityProperty, new GUIContent("Vertex Density"));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(colorsPresetProperty, new GUIContent("Colors Preset"));
            if (colorsPresetProperty.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                CreateEditor((ColorsPreset)colorsPresetProperty.objectReferenceValue).OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(sssStrengthProperty, new GUIContent("Subsurface Strength"));
            EditorGUILayout.PropertyField(sssBaseProperty, new GUIContent("Subsurface Base"));
            EditorGUILayout.PropertyField(sssScaleProperty, new GUIContent("Subsurface Displacement Scale"));

            EditorGUILayout.PropertyField(visibilityProperty, new GUIContent("Visibility"));

            EditorGUILayout.PropertyField(foamStrengthProperty, new GUIContent("Foam Strength"));
            EditorGUILayout.PropertyField(contactFoamProperty, new GUIContent("Contact Foam"));
            EditorGUILayout.PropertyField(reflectionStrengthProperty, new GUIContent("Reflection Strength"));

            EditorGUILayout.PropertyField(normalFadeFarProperty, new GUIContent("Normal Fade Far"));
            EditorGUILayout.PropertyField(foamFadeFarProperty, new GUIContent("Foam Fade Far"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(geoScaleProperty, new GUIContent("Geomtery Scale"));

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(20);

        EditorGUILayout.PropertyField(waveSettingsProperty, new GUIContent("Wave Settings"));
        if (waveSettingsProperty.objectReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            CreateEditor((WavesSettings)waveSettingsProperty.objectReferenceValue).OnInspectorGUI();
            EditorGUI.indentLevel--;
        }

        //// Simulation category
        //simulationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(simulationFoldout, "Simulation");
        //if (simulationFoldout)
        //{
        //    //EditorGUI.indentLevel++;

        //    //EditorGUILayout.Space();

        //    EditorGUILayout.PropertyField(foamFadeFarProperty, new GUIContent("Foam Fade Far"));

        //    EditorGUILayout.PropertyField(waveSettingsProperty, new GUIContent("Wave Settings"));
        //    if (waveSettingsProperty.objectReferenceValue != null)
        //    {
        //        //EditorGUI.indentLevel++;
        //        CreateEditor((WavesSettings)waveSettingsProperty.objectReferenceValue).OnInspectorGUI();
        //        //EditorGUI.indentLevel--;
        //    }
        //    //EditorGUILayout.Space();

        //    //EditorGUI.indentLevel--;
        //}
        //EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
     }
 }


#endif