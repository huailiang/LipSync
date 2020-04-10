#if FMOD_LIVEUPDATE
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace LipSync
{

    public class FmodLipSync : LipSync
    {
        public StudioEventEmitter emiter;
        private int rate;
        FMOD.DSP m_FFTDsp;
        FMOD.ChannelGroup master;
        FMOD.DSP mixerHead;

        struct Record
        {
            internal char ch;
            internal float[] blend;

            public override string ToString()
            {
                string b = "";
                int last = blend.Length - 1;
                for (int i = 0; i < last; i++)
                {
                    b += blend[i].ToString("f3") + '\t';
                }
                b += blend[last].ToString("f3");
                return ch.ToString() + '\t' + b;
            }
        }

        List<Record> records = new List<Record>();


        void Start()
        {
            Application.targetFrameRate = 60;
            InitializeRecognizer();
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
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                records.Clear();
                emiter.Play();
                FMOD.SPEAKERMODE mode;
                int raw;
                RuntimeManager.CoreSystem.getSoftwareFormat(out rate, out mode, out raw);
                records.Clear();
            }
            if (emiter.IsPlaying())
            {
                recognizeResult = runtimeRecognizer.RecognizeByAudioSource(m_FFTDsp, rate);
                RecordingFmod(recognizeResult);
                UpdateForward();
            }
            else if (records.Count > 0)
            {
                OutputRecd();
                records.Clear();
            }
        }


        void RecordingFmod(string word)
        {
            char ch = string.IsNullOrEmpty(word) ? '-' : word[0];
            var rcd = new Record() { ch = ch };
            int cnt = currentVowels.Length;
            rcd.blend = new float[cnt];
            for (int i = 0; i < cnt; i++)
            {
                rcd.blend[i] = targetBlendShapeObject.GetBlendShapeWeight(propertyIndexs[i]);
            }
            records.Add(rcd);
        }

        /// <summary>
        /// 运行时导出数据
        /// </summary>
        void OutputRecd()
        {
            using (FileStream writer = new FileStream("recd.txt", FileMode.Create, FileAccess.ReadWrite))
            {
                StreamWriter sw = new StreamWriter(writer, Encoding.Unicode);

                for (int i = 0; i < records.Count; i++)
                {
                    sw.WriteLine(records[i]);
                }
                sw.Close();
            }
        }

    }
}

#endif