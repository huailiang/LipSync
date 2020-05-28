using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LipSync
{
    public class LipSync : MonoBehaviour
    {
        public static string[] vowelsJP = { "a", "i", "u", "e", "o" };
        public static string[] vowelsCN = { "a", "e", "i", "o", "u", "v" };

        protected const int MAX_BLEND_VALUE_COUNT = 6;
        public const string recdPat = "Assets/LipSync/Editor/recd.txt";


        public ERecognizerLanguage recognizerLanguage;
        public SkinnedMeshRenderer targetBlendShapeObject;
        public string[] propertyNames = new string[MAX_BLEND_VALUE_COUNT];
        public float propertyMinValue = 0.0f;
        public float propertyMaxValue = 100.0f;

        public int windowSize = 1024;
        public float amplitudeThreshold = 0.01f;
        public float moveTowardsSpeed = 8;

        protected LipSyncRuntimeRecognizer runtimeRecognizer;
        protected string[] currentVowels;
        protected Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
        protected int[] propertyIndexs = new int[MAX_BLEND_VALUE_COUNT];

        protected string recognizeResult;
        protected float[] targetBlendValues = new float[MAX_BLEND_VALUE_COUNT];
        protected float[] currentBlendValues = new float[MAX_BLEND_VALUE_COUNT];
        private Visualization visualization;
        public Text recognizeText;


        public void InitializeRecognizer()
        {
            switch (recognizerLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    currentVowels = vowelsJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    currentVowels = vowelsCN;
                    break;
            }
            for (int i = 0; i < currentVowels.Length; ++i)
            {
                vowelToIndexDict[currentVowels[i]] = i;
                propertyIndexs[i] = targetBlendShapeObject.sharedMesh.GetBlendShapeIndex(propertyNames[i]);
            }
            runtimeRecognizer = new LipSyncRuntimeRecognizer(recognizerLanguage, windowSize, amplitudeThreshold);
            visualization = new Visualization();
        }

        void OnValidate()
        {
            windowSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(windowSize, 32, 8192));
            amplitudeThreshold = Mathf.Max(0, amplitudeThreshold);
            moveTowardsSpeed = Mathf.Clamp(moveTowardsSpeed, 5, 25);
        }


        protected void UpdateForward()
        {
            for (int i = 0; i < targetBlendValues.Length; ++i)
            {
                targetBlendValues[i] = 0.0f;
            }
            if (recognizeResult != null)
            {
                targetBlendValues[vowelToIndexDict[recognizeResult]] = 1.0f;
            }
            for (int k = 0; k < currentBlendValues.Length; ++k)
            {
                if (propertyIndexs[k] != -1)
                {
                    currentBlendValues[k] = Mathf.MoveTowards(currentBlendValues[k], targetBlendValues[k], moveTowardsSpeed * Time.deltaTime);
                    targetBlendShapeObject.SetBlendShapeWeight(propertyIndexs[k], Mathf.Lerp(propertyMinValue, propertyMaxValue, currentBlendValues[k]));
                }
            }
            visualization.Update(runtimeRecognizer.playingAudioSpectrum);
            if (recognizeText) recognizeText.text = "RecognizeResult: " + recognizeResult;
        }
    }

}