using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioVisualization : MonoBehaviour
{
    AudioSource audioSource;
    private const int band_cnt = 8;
    float[] samples = new float[512];
    float[] freqBand = new float[band_cnt];
    float[] bandBuffer = new float[band_cnt];
    float[] bufferDecrease = new float[band_cnt];
    float[] freqBandHighest = new float[band_cnt];
    public static float[] AudioBandBuffer = new float[band_cnt];
    
    public float startScale, scaleMultiplier;
    public MeshRenderer[] renders = new MeshRenderer[band_cnt];
    Material[] mat = new Material[band_cnt];


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        for (int i = 0; i < band_cnt; i++)
        {
            mat[i] = renders[i].material;
        }
    }

    void Update()
    {
        audioSource.GetSpectrumData(samples, 0, FFTWindow.Hamming);
        MakeFrequencyBands();
        BandBuffer();
        VisulizeBand();
    }


    void MakeFrequencyBands()
    {
        int index = 0;
        for (int i = 0; i < band_cnt; i++)
        {
            float average = 0;
            int sampleCount = (int) Mathf.Pow(2, i + 1);
            if (i == (band_cnt - 1))
            {
                sampleCount += 2;
            }

            for (int j = 0; j < sampleCount; j++)
            {
                average += samples[index] * (index + 1);
                index++;
            }

            average /= index;
            freqBand[i] = average * 10;
        }
    }

    void BandBuffer()
    {
        for (int i = 0; i < band_cnt; i++)
        {
            if (freqBand[i] > bandBuffer[i])
            {
                bandBuffer[i] = freqBand[i];
                bufferDecrease[i] = 0.005f;
            }

            if (freqBand[i] < bandBuffer[i])
            {
                bandBuffer[i] -= bufferDecrease[i];
                bufferDecrease[i] *= 1.2f;
            }
        }

        for (int i = 0; i < band_cnt; i++)
        {
            if (freqBand[i] > freqBandHighest[i])
            {
                freqBandHighest[i] = freqBand[i];
            }

            AudioBandBuffer[i] = bandBuffer[i] / freqBandHighest[i];
        }
    }
    
    
    void VisulizeBand()
    {
        for (int i = 0; i < band_cnt; i++)
        {
            float y = (AudioVisualization.AudioBandBuffer[i]) * scaleMultiplier + startScale;
            if (!float.IsNaN(y))
            {
                Vector3 scale = renders[i].transform.localScale;
                renders[i].transform.localScale = new Vector3(scale.x, y, scale.z);
                float v = AudioVisualization.AudioBandBuffer[i];
                Color color = new Color(v, v, v);
                mat[i].SetColor("_EmissionColor", color);
            }
        }
        
    }
}