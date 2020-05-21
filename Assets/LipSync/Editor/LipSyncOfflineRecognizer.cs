using UnityEngine;

namespace LipSync.Editor
{
    public class LipSyncOfflineRecognizer : LipSyncRecognizer
    {
        private int shiftStepSize;
        private float[] windowArray;
        
        public LipSyncOfflineRecognizer(ERecognizerLanguage recognizingLanguage, float amplitudeThreshold, int windowSize, int shiftStepSize)
        {
            base.Init(recognizingLanguage, windowSize, amplitudeThreshold);
            this.shiftStepSize = shiftStepSize;
            this.windowArray = MathToolBox.GenerateWindow(windowSize, MathToolBox.EWindowType.Hamming);
        }

        public string[] RecognizeAllByAudioClip(AudioClip audioClip)
        {
            int recognizeSampleCount = Mathf.CeilToInt((float)(audioClip.samples) / (float)(shiftStepSize));
            string[] result = new string[recognizeSampleCount];
            float[] currentAudioData = new float[this.windowSize];
            float[] currentAudioSpectrum = new float[this.windowSize];

            for (int i = 0; i < recognizeSampleCount; ++i)
            {
                audioClip.GetData(currentAudioData, i * shiftStepSize);
                for (int j = 0; j < windowSize; ++j)
                {
                    currentAudioData[j] *= windowArray[j];
                }
                currentAudioSpectrum = MathToolBox.DiscreteCosineTransform(currentAudioData);

                amplitudeSum = 0.0f;
                for (int k = 0; k < windowSize; ++k)
                {
                    amplitudeSum += currentAudioSpectrum[k];
                }

                if (amplitudeSum >= amplitudeThreshold)
                {
                    MathToolBox.Convolute(currentAudioSpectrum, gaussianFilter, MathToolBox.EPaddleType.Repeat, smoothedAudioSpectrum);
                    MathToolBox.FindLocalLargestPeaks(smoothedAudioSpectrum, peakValues, peakPositions);
                    frequencyUnit = audioClip.frequency / windowSize;
                    for (int l = 0; l < formantArray.Length; ++l)
                    {
                        formantArray[l] = peakPositions[l] * frequencyUnit;
                    }

                    for (int m = 0; m < currentVowelFormantCeilValues.Length; ++m)
                    {
                        if (formantArray[0] > currentVowelFormantCeilValues[m])
                        {
                            result[i] = currentVowels[m];
                        }
                    }
                }
            }
            return result;
        }
    }

}