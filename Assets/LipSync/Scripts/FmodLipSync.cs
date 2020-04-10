#if FMOD_LIVEUPDATE
using FMODUnity;
using UnityEngine;

namespace LipSync
{

    public class FmodLipSync : LipSync
    {
        public StudioEventEmitter emiter;
        private int rate;
        FMOD.DSP m_FFTDsp;
        FMOD.ChannelGroup master;
        FMOD.DSP mixerHead;

        
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
            UpdateForward();
        }
        
    }
}

#endif