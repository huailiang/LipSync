using UnityEngine;

namespace LipSync
{
    public enum ERecognizerLanguage { Japanese, Chinese }

    public class LipSyncRecognizer
    {
        protected const int FILTER_SIZE = 7;
        protected const float FILTER_DEVIATION_SQUARE = 5.0f;
        protected const int FORMANT_COUNT = 1;

        protected string[] vowelsByFormantJP = { "i", "u", "e", "o", "a" };
        protected float[] vowelFormantFloorJP = { 0.0f, 250.0f, 300.0f, 450.0f, 600.0f };
        protected string[] vowelsByFormantCN = { "i", "v", "u", "e", "o", "a" };
        protected float[] vowelFormantFloorCN = { 0.0f, 100.0f, 250.0f, 300.0f, 450.0f, 600.0f };

        protected string[] currentVowels;
        protected float[] currentVowelFormantCeilValues;

        protected int windowSize;
        protected float amplitudeThreshold;

        protected float amplitudeSum;
        protected float[] smoothedAudioSpectrum;
        protected float[] peakValues;
        protected int[] peakPositions;
        protected float frequencyUnit;
        protected float[] formantArray;
        protected float[] gaussianFilter;

        public void Init(ERecognizerLanguage recognizingLanguage, int windowSize, float amplitudeThreshold)
        {
            switch (recognizingLanguage)
            {
                case ERecognizerLanguage.Japanese:
                    currentVowels = vowelsByFormantJP;
                    currentVowelFormantCeilValues = vowelFormantFloorJP;
                    break;
                case ERecognizerLanguage.Chinese:
                    currentVowels = vowelsByFormantCN;
                    currentVowelFormantCeilValues = vowelFormantFloorCN;
                    break;
            }

            this.windowSize = Mathf.ClosestPowerOfTwo(windowSize);
            this.amplitudeThreshold = amplitudeThreshold;

            this.smoothedAudioSpectrum = new float[this.windowSize];
            this.peakValues = new float[FORMANT_COUNT];
            this.peakPositions = new int[FORMANT_COUNT];
            this.formantArray = new float[FORMANT_COUNT];

            this.gaussianFilter = MathToolBox.GenerateGaussianFilter(FILTER_SIZE, FILTER_DEVIATION_SQUARE);
        }

    }
}