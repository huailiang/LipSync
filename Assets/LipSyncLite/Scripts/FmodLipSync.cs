using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

namespace LipSync
{

    public class FmodLipSync : MonoBehaviour
    {
        const int MAX_BLEND_VALUE_COUNT = 6;
        public StudioEventEmitter emiter;
        private int rate;
        FMOD.DSP m_FFTDsp;
        FMOD.ChannelGroup master;
        FMOD.DSP mixerHead;

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


        public void InitializeRecognizer()
        {
            switch (recognizerLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    currentVowels = AudioLipSync.vowelsJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    currentVowels = AudioLipSync.vowelsCN;
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

            emiter.Play();
            InitDsp();
        }

        void InitDsp()
        {
            RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out m_FFTDsp);
            m_FFTDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWTYPE, (int)FMOD.DSP_FFT_WINDOW.HANNING);
            m_FFTDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, windowSize);
            RuntimeManager.CoreSystem.getMasterChannelGroup(out master);
            var m_Result = master.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, m_FFTDsp);
            m_Result = master.getDSP(0, out mixerHead);
            mixerHead.setMeteringEnabled(true, true);

            FMOD.SPEAKERMODE mode;
            int raw;
            RuntimeManager.CoreSystem.getSoftwareFormat(out rate, out mode, out raw);
            Debug.Log("fmod audio rate: " + rate);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space)) emiter.Play();

            recognizeResult = runtimeRecognizer.RecognizeByAudioSource(m_FFTDsp, rate);
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

        void OnValidate()
        {
            windowSize = Mathf.ClosestPowerOfTwo(Mathf.Clamp(windowSize, 32, 8192));
            amplitudeThreshold = Mathf.Max(0, amplitudeThreshold);
            moveTowardsSpeed = Mathf.Clamp(moveTowardsSpeed, 5, 25);
        }
    }
}