using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LipSync
{
    public class LpcEditor : EditorWindow
    {
        [MenuItem("XEditor/LpcEditor")]
        static void LpcShow()
        {
            var win = GetWindow<LpcEditor>();
            win.Show();
        }

        private AudioClip audioClip;
        private float[] audioBuffer;
        private int window, step, fs;
        private string info;

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            audioClip = (AudioClip) EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);

            if (GUILayout.Button("Analy"))
            {
                Normalize();
                var split = MakeFrame();
                Formant(split);
            }
            GUILayout.Space(10);
            if (!string.IsNullOrEmpty(info))
            {
                GUILayout.TextArea(info);
            }
            GUILayout.EndVertical();
        }


        private void Normalize()
        {
            float len = audioClip.length;
            fs = audioClip.frequency;
            int count = (int) (fs * audioClip.length);
            Debug.Log(fs + "  " + len);
            audioBuffer = new float[count];
            audioClip.GetData(audioBuffer, 0);
            float max = audioBuffer.Max();
            float min = audioBuffer.Min();
            Debug.Log("last: " + audioBuffer[count - 1] + " " + max + " " + min);
            max = Mathf.Max(max, -min);
            for (int i = 0; i < count; i++)
            {
                audioBuffer[i] = Mathf.Abs(audioBuffer[i] / max);
            }
        }


        private List<float[]> MakeFrame()
        {
            step = (15 * fs) / 1000;
            window = (30 * fs) / 1000;
            List<float[]> splitting = new List<float[]>();
            int i = 0;
            while (i <= audioBuffer.Length - window)
            {
                float[] arr = new float[window];
                for (int j = i; j < i + window; j++)
                {
                    arr[j - i] = audioBuffer[j];
                }
                splitting.Add(arr);
                i += step;
            }
            return splitting;
        }

        private float[] PreEmphasis(float[] x, float a)
        {
            var temp = new float[window];
            int i = 1;
            while (i <= window - 2)
            {
                temp[i - 1] = x[i] - a * x[i - 1];
                i++;
            }
            return temp;
        }

        private void Formant(List<float[]> splitting)
        {
            int i = 0;
            float a = 0.67f;
            var lpc = new LpcModel();
            info = String.Empty;
            while (i < splitting.Count())
            {
                var FL = PreEmphasis(splitting[i], a);
                var w = MathToolBox.GenerateWindow(window, MathToolBox.EWindowType.Hamming);
                for (int j = 0; j < window; j++)
                {
                    FL[i] = FL[i] * w[i];
                }
                var coefficients = lpc.EstimateLpcCoefficients(FL, 2 + fs / 1000);
                var formants = lpc.FindFormants(coefficients, fs);
                AppendInfo(i, formants);
                i++;
            }
        }


        private void AppendInfo(int idx, double[] formants)
        {
            info += idx.ToString("N2");
            for (int i = 0; i < formants.Length; i++)
            {
                info += formants[i].ToString("f3") + " ";
            }
        }
    }
}
