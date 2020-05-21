using UnityEditor;
using UnityEngine;

namespace LipSync.Editor
{
    public class LipSyncEditor : UnityEditor.Editor
    {

        protected bool isAdvancedOptionsFoldOut;

        protected void GUIVowel(LipSync sync)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fftWindow"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recognizeText"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recognizerLanguage"));
            string[] selectedVowels = null;
            switch (sync.recognizerLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    selectedVowels = LipSync.vowelsJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    selectedVowels = LipSync.vowelsCN;
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
        }

        protected void GUIAdvanceOptions(LipSync sync)
        {
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
                    sync.InitializeRecognizer();
                }
            }
        }
    }


    [CustomEditor(typeof(AudioLipSync))]
    [CanEditMultipleObjects]
    public class AudioLipSyncEditor : LipSyncEditor
    {

        public override void OnInspectorGUI()
        {
            AudioLipSync targetLipSync = (AudioLipSync)target;
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("lipSyncMethod"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"));
           
            EditorGUILayout.Space();
            if (targetLipSync.lipSyncMethod == ELipSyncMethod.Runtime)
            {
                GUIVowel(targetLipSync);
                GUIAdvanceOptions(targetLipSync);
            }
            else if (targetLipSync.lipSyncMethod == ELipSyncMethod.Baked)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("targetAnimator"));

                if (GUILayout.Button("LipSync Baker") == true)
                {
                    BakingEditorWindow window = EditorWindow.GetWindow<BakingEditorWindow>("LipSync Baker");
                    window.Show();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#if FMOD_LIVEUPDATE
    [CustomEditor(typeof(FmodLipSync))]
    [CanEditMultipleObjects]
    public class FmodLipSyncEditor : LipSyncEditor
    {

        public override void OnInspectorGUI()
        {
            FmodLipSync targetLipSync = (FmodLipSync)target;
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("emiter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("save"));
            EditorGUILayout.Space();
            
            GUIVowel(targetLipSync);
            GUIAdvanceOptions(targetLipSync);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}