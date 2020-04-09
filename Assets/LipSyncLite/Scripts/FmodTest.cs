using FMODUnity;
using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class FmodTest : MonoBehaviour
{
    FMOD.DSP mixerHead;
    public FMOD.DSP m_FFTDsp;
    public const int WindowSize = 1024;

    FMOD.RESULT m_Result;
    FMOD.ChannelGroup master;


    [ContextMenu("test")]
    void Start()
    {
        InitDsp();
        Play();
    }

    void Play()
    {
        StudioEventEmitter emiter = GetComponent<StudioEventEmitter>();
        emiter.Play();
    }


    void InitDsp()
    {
        RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out m_FFTDsp);
        m_FFTDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWTYPE, (int)FMOD.DSP_FFT_WINDOW.HANNING);
        m_FFTDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, WindowSize * 2);
        RuntimeManager.CoreSystem.getMasterChannelGroup(out master);
        m_Result = master.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.HEAD, m_FFTDsp);
        m_Result = master.getDSP(0, out mixerHead);
        mixerHead.setMeteringEnabled(true, true);

        FMOD.SPEAKERMODE mode;
        int rate, raw;
        RuntimeManager.CoreSystem.getSoftwareFormat(out rate, out mode, out raw);
        Debug.Log("fmod audio rate: " + rate);
    }
    


    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space)) Play();
        IntPtr unmanagedData;
        uint length;
        m_FFTDsp.getParameterData((int)FMOD.DSP_FFT.SPECTRUMDATA, out unmanagedData, out length);
        FMOD.DSP_PARAMETER_FFT fftData = (FMOD.DSP_PARAMETER_FFT)Marshal.PtrToStructure(unmanagedData, typeof(FMOD.DSP_PARAMETER_FFT));
        var spectrum = fftData.spectrum;
        if (spectrum != null && spectrum.Length > 0)
        {
            Debug.Log(spectrum.Length + " " + length + "-" + VisualSpectrum(spectrum[0]));
        }
    }


    private string VisualSpectrum(float[] spectrum)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 20; i++)
        {
            sb.Append(spectrum[i].ToString("f3"));
        }
        return sb.ToString();
    }
}