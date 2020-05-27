using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LipSync.Editor
{
    public class BakingEditorWindow : EditorWindow
    {
        public string animatorName;
        public ERecognizerLanguage recognizerLanguage;
        public string targetRelativePath;
        public string[] propertyNames = new string[6];
        public float propertyMinValue = 0.0f;
        public float propertyMaxValue = 100.0f;

        public int windowSize = 1024;
        public int shiftStepSize = 512;
        public float amplitudeThreshold = 0.01f;
        public float moveTowardsSpeed = 8;

        private string audioClipInputPath;
        private string audioClipOutputPath;
        private List<AudioClip> audioClipToBake = new List<AudioClip>();
        private string[] currentVowels;

        private Vector2 mainScroll;
        private Vector2 audioClipListScroll;
        private bool isAdvancedOptionsFoldOut;

        void OnGUI()
        {
            SerializedObject serializedEditorWindow = new SerializedObject(this);
            serializedEditorWindow.UpdateIfRequiredOrScript();
            {
                mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
                {
                    EditorGUILayout.LabelField("", GUILayout.Height(2));

                    EditorGUILayout.LabelField("AudioClip Input Folder Path", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(audioClipInputPath, EditorStyles.textArea);
                    if (GUILayout.Button("Browse...", GUILayout.Width(100)))
                    {
                        LoadAudioClips();
                    }
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.BeginVertical(GUILayout.Width(100));
                        {
                            EditorGUILayout.LabelField("AudioClip List", EditorStyles.boldLabel);
                            audioClipListScroll = EditorGUILayout.BeginScrollView(audioClipListScroll,
                                EditorStyles.textField, GUILayout.MinHeight(100), GUILayout.MaxHeight(250));
                            foreach (AudioClip a in audioClipToBake)
                            {
                                EditorGUILayout.LabelField(a.name);
                            }
                            EditorGUILayout.EndScrollView();
                        }
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.BeginVertical(GUILayout.MaxWidth(600));
                        {
                            EditorGUILayout.LabelField("Animation Property Setting", EditorStyles.boldLabel);
                            EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("animatorName"));
                            EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("recognizerLanguage"));
                            EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("targetRelativePath"));
                            EditorGUILayout.Space();

                            EditorGUILayout.LabelField("Vowel Property Names");
                            EditorGUILayout.BeginVertical(EditorStyles.textField);
                            {
                                SerializedProperty propertyNames = serializedEditorWindow.FindProperty("propertyNames");
                                switch (recognizerLanguage)
                                {
                                    case ERecognizerLanguage.Japanese:
                                        currentVowels = LipSync.vowelsJP;
                                        break;
                                    case ERecognizerLanguage.Chinese:
                                        currentVowels = LipSync.vowelsCN;
                                        break;
                                }
                                for (int i = 0; i < currentVowels.Length; ++i)
                                {
                                    EditorGUILayout.PropertyField(propertyNames.GetArrayElementAtIndex(i),
                                        new GUIContent(currentVowels[i]));
                                }
                            }
                            EditorGUILayout.EndVertical();
                            EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("propertyMinValue"));
                            EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("propertyMaxValue"));
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();

                    isAdvancedOptionsFoldOut = EditorGUILayout.Foldout(isAdvancedOptionsFoldOut, "Advanced Options");
                    if (isAdvancedOptionsFoldOut)
                    {
                        EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("windowSize"));
                        EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("shiftStepSize"));
                        EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("amplitudeThreshold"));
                        EditorGUILayout.PropertyField(serializedEditorWindow.FindProperty("moveTowardsSpeed"));
                    }
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("Bake")) Bake();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            serializedEditorWindow.ApplyModifiedProperties();
            this.Repaint();
        }

        private void LoadAudioClips()
        {
            string tempPath = EditorUtility.OpenFolderPanel("Select a asset folder containing AudioClips",
                Application.dataPath, "");
            if (tempPath.IndexOf(Application.dataPath, 0, StringComparison.Ordinal) == 0)
            {
                audioClipInputPath = tempPath.Substring(Application.dataPath.Length - "Assets".Length);

                string[] audioClipAssetNames = Directory.GetFiles(tempPath, "*.*", SearchOption.AllDirectories);
                audioClipToBake.Clear();
                foreach (string s in audioClipAssetNames)
                {
                    AudioClip clip =
                        AssetDatabase.LoadAssetAtPath<AudioClip>(
                            s.Substring(Application.dataPath.Length - "Assets".Length));
                    if (clip != null) audioClipToBake.Add(clip);
                }
            }
            else if (tempPath.Length != 0)
            {
                EditorUtility.DisplayDialog("Invalid path",
                    "This folder is not contained in the asset. Please make sure you choose a folder inside the \"Assets\" folder.",
                    "OK");
            }
        }

        private void Bake()
        {
            if (audioClipToBake.Count == 0)
            {
                EditorUtility.DisplayDialog("No AudioClip found",
                    "You have not select a folder containing AudioClip. Please first specify a valid folder.", "OK");
            }
            else
            {
                int animDataCount = 0;
                for (int i = 0; i < propertyNames.Length; ++i)
                {
                    if (!string.IsNullOrEmpty(propertyNames[i]))
                    {
                        ++animDataCount;
                    }
                    else
                    {
                        break;
                    }
                }
                if (animDataCount < currentVowels.Length)
                {
                    EditorUtility.DisplayDialog("Incomplete animation data",
                        "There is incomplete animation data. Please make sure you have filled all required data in the Animation Property Setting.",
                        "OK");
                }
                else
                {
                    string path = "";
                    path = EditorUtility.SaveFolderPanel("Save generated Animator to...", Application.dataPath, "");
                    if (path.IndexOf(Application.dataPath, 0, StringComparison.Ordinal) == 0)
                    {
                        path = path.Substring(Application.dataPath.Length - "Assets".Length);
                        if (AssetDatabase.IsValidFolder(path + "/GeneratedClips") == false)
                        {
                            AssetDatabase.CreateFolder(path, "GeneratedClips");
                        }

                        LipSyncOfflineRecognizer recognizer = new LipSyncOfflineRecognizer(recognizerLanguage,
                            amplitudeThreshold, windowSize, shiftStepSize);

                        Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
                        for (int i = 0; i < currentVowels.Length; ++i)
                        {
                            vowelToIndexDict[currentVowels[i]] = i;
                        }

                        List<AnimationClip> tempClipList = new List<AnimationClip>(audioClipToBake.Count);
                        for (int j = 0; j < audioClipToBake.Count; ++j)
                        {
                            AnimationClip clip = new AnimationClip();
                            AnimationCurve[] curveArray = new AnimationCurve[currentVowels.Length];
                            for (int jj = 0; jj < currentVowels.Length; ++jj)
                            {
                                curveArray[jj] = new AnimationCurve();
                            }

                            float[] targetBlendValues = new float[currentVowels.Length];
                            float[] currentBlendValues = new float[currentVowels.Length];
                            string[] recognizeResult = recognizer.RecognizeAllByAudioClip(audioClipToBake[j]);
                            float timeUnit = 1024.0f * (1.0f / (float) audioClipToBake[j].frequency);

                            for (int k = 0; k < recognizeResult.Length; ++k)
                            {
                                for (int kk = 0; kk < currentVowels.Length; ++kk)
                                {
                                    targetBlendValues[kk] = 0;
                                }
                                if (recognizeResult[k] != null)
                                {
                                    targetBlendValues[vowelToIndexDict[recognizeResult[k]]] = 1.0f;
                                }
                                for (int kk = 0; kk < currentVowels.Length; ++kk)
                                {
                                    currentBlendValues[kk] = Mathf.MoveTowards(currentBlendValues[kk],
                                        targetBlendValues[kk], moveTowardsSpeed * timeUnit);
                                    Keyframe keyframe = new Keyframe(timeUnit * k,
                                        Mathf.Lerp(propertyMinValue, propertyMaxValue, currentBlendValues[kk]));
                                    curveArray[kk].AddKey(keyframe);
                                }
                            }

                            for (int jj = 0; jj < currentVowels.Length; ++jj)
                            {
                                Keyframe keyframe = new Keyframe(timeUnit * recognizeResult.Length, 0);
                                curveArray[jj].AddKey(keyframe);
                            }

                            for (int l = 0; l < currentVowels.Length; ++l)
                            {
                                clip.SetCurve(targetRelativePath, typeof(SkinnedMeshRenderer),
                                    "blendShape." + propertyNames[l], curveArray[l]);
                            }
                            tempClipList.Add(clip);
                        }

                        for (int m = 0; m < tempClipList.Count; ++m)
                        {
                            AssetDatabase.CreateAsset(tempClipList[m],
                                path + "/GeneratedClips/" + audioClipToBake[m].name + "_anim.anim");
                        }

                        EditorUtility.DisplayDialog("Complete Baking",
                            "LipSync baking is completed. Please check the specified folder for result.", "OK");
                    }
                }
            }
        }

        void OnValidate()
        {
            windowSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(windowSize, 32, 8192));
            shiftStepSize = Mathf.Clamp(shiftStepSize, 1, windowSize);
            amplitudeThreshold = Mathf.Max(0, amplitudeThreshold);
            moveTowardsSpeed = Mathf.Clamp(moveTowardsSpeed, 5, 25);
        }
    }
}
