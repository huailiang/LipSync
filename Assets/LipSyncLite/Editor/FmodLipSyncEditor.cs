using UnityEditor;
using UnityEngine;

namespace LipSync
{
    [CustomEditor(typeof(FmodLipSync))]
    [CanEditMultipleObjects]
    public class FmodLipSyncEditor : Editor
    {
        private bool isAdvancedOptionsFoldOut;

        public override void OnInspectorGUI()
        {
            FmodLipSync targetLipSync = (FmodLipSync)target;
            serializedObject.UpdateIfRequiredOrScript();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("emiter"));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("recognizerLanguage"));
            string[] selectedVowels = null;
            switch (targetLipSync.recognizerLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    selectedVowels = AudioLipSync.vowelsJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    selectedVowels = AudioLipSync.vowelsCN;
                    break;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetBlendShapeObject"));
            EditorGUILayout.LabelField("Vowel Property Names");
            EditorGUILayout.BeginVertical(EditorStyles.textField);
            {
                SerializedProperty propertyNames = serializedObject.FindProperty("propertyNames");
                for (int i = 0; i < selectedVowels.Length; ++i)
                {
                    EditorGUILayout.PropertyField(propertyNames.GetArrayElementAtIndex(i), new GUIContent(selectedVowels[i]));
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("propertyMinValue"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("propertyMaxValue"));

            EditorGUILayout.Space();

            isAdvancedOptionsFoldOut = EditorGUILayout.Foldout(isAdvancedOptionsFoldOut, "Advanced Options");
            if (isAdvancedOptionsFoldOut)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("windowSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("amplitudeThreshold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moveTowardsSpeed"));
            }
            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Changes of settings at runtime must be applied manually using the button below.", MessageType.Warning);
                if (GUILayout.Button("Apply runtime changes"))
                {
                    targetLipSync.InitializeRecognizer();
                }
            }


            serializedObject.ApplyModifiedProperties();
        }
    }
}