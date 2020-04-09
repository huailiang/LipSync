using System.Collections.Generic;
using UnityEngine;

namespace LipSync
{
    public enum ELipSyncMethod { Runtime, Baked }
    
    public class AudioLipSync : MonoBehaviour 
    {
        public static string[] vowelsJP = { "a", "i", "u", "e", "o" };
        public static string[] vowelsCN = { "a", "e", "i", "o", "u", "v" };
        public const int MAX_BLEND_VALUE_COUNT = 6;
        public ELipSyncMethod lipSyncMethod;
        public AudioSource audioSource;

        #region Fields for Runtime LipSync
        public ERecognizerLanguage recognizerLanguage;
        public SkinnedMeshRenderer targetBlendShapeObject;
        public string[] propertyNames = new string[MAX_BLEND_VALUE_COUNT];
        public float propertyMinValue = 0.0f;
        public float propertyMaxValue = 100.0f;
        
        public int windowSize = 1024;
        public float amplitudeThreshold = 0.01f;
        public float moveTowardsSpeed = 8;

        private LipSyncRuntimeRecognizer runtimeRecognizer;
        private string[] currentVowels;
        private Dictionary<string, int> vowelToIndexDict = new Dictionary<string, int>();
        private int[] propertyIndexs = new int[MAX_BLEND_VALUE_COUNT];
        private float blendValuesSum;

        private string recognizeResult;
        private float[] targetBlendValues = new float[MAX_BLEND_VALUE_COUNT];
        private float[] currentBlendValues = new float[MAX_BLEND_VALUE_COUNT];
        #endregion

        #region Fields for Baked LipSync
        public Animator targetAnimator;

        private int lastTimeSamples;
        #endregion


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
        }

        void Start()
        {
            InitializeRecognizer();
        }

        void Update()
        {
            if (lipSyncMethod == ELipSyncMethod.Runtime)
            {
                if (Input.GetKeyDown(KeyCode.Space)) audioSource.Play();

                recognizeResult = runtimeRecognizer.RecognizeByAudioSource(audioSource);
                for (int i = 0; i < targetBlendValues.Length; ++i)
                {
                    targetBlendValues[i] = 0.0f;
                }
                if (recognizeResult != null)
                {
                    targetBlendValues[vowelToIndexDict[recognizeResult]] = 1.0f;
                }
                blendValuesSum = 0.0f;
                for (int j = 0; j < currentBlendValues.Length; ++j)
                {
                    blendValuesSum += currentBlendValues[j];
                }

                for (int k = 0; k < currentBlendValues.Length; ++k)
                {
                    if (propertyIndexs[k] != -1)
                    {
                        currentBlendValues[k] = Mathf.MoveTowards(currentBlendValues[k], targetBlendValues[k], moveTowardsSpeed * Time.deltaTime);
                        targetBlendShapeObject.SetBlendShapeWeight(propertyIndexs[k], Mathf.Lerp(propertyMinValue, propertyMaxValue, currentBlendValues[k]));
                    }
                }
            }
            else if (lipSyncMethod == ELipSyncMethod.Baked)
            {
                if (audioSource.timeSamples < lastTimeSamples)
                {
                    if (audioSource.isPlaying)
                    {
                        targetAnimator.CrossFade(audioSource.clip.name + "_anim", 0f);
                    }
                }
                lastTimeSamples = audioSource.timeSamples;
            }
        }

        void OnValidate()
        {
            windowSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(windowSize, 32, 8192));
            amplitudeThreshold = Mathf.Max(0, amplitudeThreshold);
            moveTowardsSpeed = Mathf.Clamp(moveTowardsSpeed, 5, 25);
        }
    }
    
}
