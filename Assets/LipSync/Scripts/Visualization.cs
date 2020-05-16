using UnityEngine;


/*
 * 均衡器可视化
 * https://baike.baidu.com/item/均衡器/3136238
 * 在线声谱分析
 *  https://bideyuanli.com/pp
 */
namespace LipSync
{
    public class Visualization
    {
        const int band_cnt = 8;

        float[] freqBand = new float[band_cnt];
        float[] bandBuffer = new float[band_cnt];
        float[] bufferDecrease = new float[band_cnt];
        float[] freqBandHighest = new float[band_cnt];
        float[] AudioBandBuffer = new float[band_cnt];

        Renderer[] renders = new Renderer[band_cnt];
        Material[] mat = new Material[band_cnt];


        public Visualization()
        {
            GameObject go = GameObject.Find("AudioVisulization");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                renders[i] = go.transform.GetChild(i).GetComponent<Renderer>();
                mat[i] = renders[i].material;
            }
        }

        public void Update(float[] samples)
        {
            MakeFrequencyBands(samples);
            BandBuffer();
            VisulizeBand();
        }


        void MakeFrequencyBands(float[] samples)
        {
            int index = 0;
            for (int i = 0; i < band_cnt; i++)
            {
                float average = 0;
                int sampleCount = (int)Mathf.Pow(2, i + 1);
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
                float v = Mathf.Max(0, AudioBandBuffer[i]);
                float y = v * 8 + 0.1f;
                if (!float.IsNaN(y))
                {
                    Vector3 scale = renders[i].transform.localScale;
                    renders[i].transform.localScale = new Vector3(scale.x, y, scale.z);
                    Color color = new Color(0, 0, v);
                    mat[i].SetColor("_EmissionColor", color);
                }
            }
        }
    }
}