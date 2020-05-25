using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LipSync.Editor
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
        private LpcModel model;
        private float[] audioBuffer;
        private int window, step, fs;
        private string info;

        private void OnEnable()
        {
            if (model == null)
            {
                model = new LpcModel();
            }
        }

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
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("root"))
            {
                float[] poly = new float[] {-8, 12, -6, 1};
                var ret = model.FindRoots(poly);
                foreach (var it in ret)
                {
                    Debug.Log(it);
                }
            }
            if (GUILayout.Button("c-root"))
            {
                double[] poly = new Double[] {-8, 12, -6, 1};
                var roots = model.FindCRoots(poly);
                for (int i = 0; i < roots.Length; i++)
                {
                    Debug.Log("i: " + roots[i]);
                }
            }
            if (GUILayout.Button("correlate"))
            {
                var a = new float[] { 3, 1, 2, 4, 3, 5, 6, 5, 6, 2 };
                var v = new float[] { 3, 1, 4, 2 };
                var t = model.Correlate(a, v);
                string str = "";
                for (int i = 0; i < t.Length; i++)
                {
                    str += t[i] + " ";
                }
                Debug.Log(str);
            }
            if (GUILayout.Button("toeplitz"))
            {
                var c = new double[]
                {
                    4, -2.6, 1.7, 4.3, 11, 21, 1.3, -3, 4, 11, 9, -4, 7, 12, 0.3, -7.0
                };
                ToeplitzMtrix toeplitzMtrix = new ToeplitzMtrix(c);
                Debug.Log(toeplitzMtrix);
                var t = toeplitzMtrix.Inverse();
                int n = (int) Math.Sqrt((double) t.Length);
                string msg = "size: " + n;
                for (int i = 0; i < n; i++)
                {
                    msg += "\n";
                    for (int j = 0; j < n; j++) msg += t[i, j].ToString("f3") + "\t";
                }
                Debug.Log(msg);
            }
            GUILayout.EndHorizontal();
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
            info = String.Empty;
            List<double[]> ret = new List<double[]>();
            while (i < splitting.Count())
            {
                float[] FL = PreEmphasis(splitting[i], a);
                float[] w = MathToolBox.GenerateWindow(window, MathToolBox.EWindowType.Hamming);
                for (int j = 0; j < window; j++)
                {
                    FL[j] = FL[j] * w[j];
                }
                var coefficients = model.Estimate(FL, 2 + fs / 1000);
                var rts = model.FindCRoots(coefficients);
                rts = rts.Where(x => x.imag >= 0).ToArray();
                var frqs = rts.Select(x => x.arg * (fs / (2 * Mathf.PI))).ToList();
                frqs.Sort();
                double[] fmts = {frqs[1], frqs[2], frqs[3]};
                Debug.Log(frqs[1] + " " + frqs[2] + " " + frqs[3]);
                ret.Add(fmts);
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
