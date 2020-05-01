using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LipSync
{
    public class LipSyncRuntimeRecognizer : LipSyncRecognizer
    {
        private float[] playingAudioData;
        public float[] playingAudioSpectrum;

        public LipSyncRuntimeRecognizer(ERecognizerLanguage recognizingLanguage, int windowSize, float amplitudeThreshold)
        {
            base.Init(recognizingLanguage, windowSize, amplitudeThreshold);
            this.playingAudioSpectrum = new float[this.windowSize];
        }

        private void Recognize(ref string result, int sampleRate)
        {
            amplitudeSum = 0.0f;
            for (int i = 0; i < playingAudioSpectrum.Length; ++i)
            {
                amplitudeSum += playingAudioSpectrum[i];
            }
            if (amplitudeSum >= amplitudeThreshold)
            {
                MathToolBox.Convolute(playingAudioSpectrum, gaussianFilter, MathToolBox.EPaddleType.Repeat, smoothedAudioSpectrum);
                MathToolBox.FindLocalLargestPeaks(smoothedAudioSpectrum, peakValues, peakPositions);
                frequencyUnit = sampleRate / windowSize;
                for (int i = 0; i < formantArray.Length; ++i)
                {
                    formantArray[i] = peakPositions[i] * frequencyUnit;
                }

                for (int i = 0; i < currentVowelFormantCeilValues.Length; ++i)
                {
                    if (formantArray[0] > currentVowelFormantCeilValues[i])
                    {
                        result = currentVowels[i];
                    }
                }
            }
        }

        public string RecognizeByAudioSource(AudioSource audioSource, FFTWindow window)
        {
            string result = null;
            audioSource.GetSpectrumData(playingAudioSpectrum, 0, window);

            if (audioSource.isPlaying)
            {
                Recognize(ref result, audioSource.clip.frequency);
            }
            return result;
        }

#if FMOD_LIVEUPDATE
        public string RecognizeByAudioSource(FMOD.DSP m_FFTDsp, int rate)
        {
            string result = null;
            IntPtr unmanagedData;
            uint length;
            m_FFTDsp.getParameterData((int)FMOD.DSP_FFT.SPECTRUMDATA, out unmanagedData, out length);
            FMOD.DSP_PARAMETER_FFT fftData = (FMOD.DSP_PARAMETER_FFT)Marshal.PtrToStructure(unmanagedData, typeof(FMOD.DSP_PARAMETER_FFT));
            if (fftData.spectrum != null && fftData.spectrum.Length > 0)
            {
                playingAudioSpectrum = fftData.spectrum[0];
                Recognize(ref result, rate);
            }
            return result;
        }
#endif
    }
}