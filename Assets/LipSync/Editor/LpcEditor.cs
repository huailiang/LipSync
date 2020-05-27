using System;
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
        private Texture2D tex;

        private void OnEnable()
        {
            if (model == null)
            {
                model = new LpcModel();
                window = 30;
                step = 15;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            audioClip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", audioClip, typeof(AudioClip), false);
            GUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.textField);
            {
                if (audioClip)
                {
                    var pat = AssetDatabase.GetAssetPath(audioClip);
                    pat = pat.Substring(pat.LastIndexOf('/') + 1);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(pat);
                    EditorGUILayout.LabelField(string.Format("时 长  : {0:f2}", audioClip.length));
                    EditorGUILayout.LabelField("声 道  : " + audioClip.channels);
                    int steps = CulSteps(out var w, out var s);
                    EditorGUILayout.LabelField("帧 数  : " + steps);
                    EditorGUILayout.LabelField(string.Format("窗 口  : {0}帧， {1:f2}秒", w, s));
                    EditorGUILayout.LabelField("采样率 : " + audioClip.frequency);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                    tex = AssetPreview.GetAssetPreview(audioClip);
                    GUIContent content = new GUIContent(tex, "wave");
                    EditorGUILayout.LabelField(content, GUILayout.MinHeight(120));
                    EditorGUILayout.EndHorizontal();
                    model.fs = audioClip.frequency;
                }
            }
            EditorGUILayout.EndVertical();
            window = EditorGUILayout.IntField("window", window);
            step = EditorGUILayout.IntField("step", step);
            GUILayout.Space(4);
            if (GUILayout.Button("Analy"))
            {
                Normalize();
                var rst = model.Analy(audioBuffer, window, step);
                for (int i = 0; i < rst.Count; i++)
                {
                    Debug.Log(string.Format("{0}: {1:f2} {2:f2} {3:f2}", i, rst[i][0], rst[i][1], rst[i][2]));
                }
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("root"))
            {
                float[] poly = new float[] { -4, 0, 1 };
                var ret = model.FindRoots(poly);
                foreach (var it in ret)
                {
                    Debug.Log(it);
                }
            }
            if (GUILayout.Button("c-root"))
            {
                double[] poly = new Double[] { 4, 0, 1 };
                var roots = model.FindCRoots(poly);
                for (int i = 0; i < roots.Length; i++)
                {
                    Debug.Log("i: " + roots[i]);
                }
            }
            if (GUILayout.Button("correlate"))
            {
                var a = new float[] { 0.3f, 0.1f, 0.2f, 0.4f, 0.3f, 0.5f, -1.6f, -2.5f, 1.6f, 3.2f, 1.34f, -4.1f, -5.34f };
                var t = model.Correlate(a, a);
                string str = "";
                for (int i = 0; i < t.Length; i++)
                {
                    str += t[i].ToString("f3") + " ";
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
                int n = (int)Math.Sqrt((double)t.Length);
                string msg = "size: " + n;
                for (int i = 0; i < n; i++)
                {
                    msg += "\n";
                    for (int j = 0; j < n; j++) msg += t[i, j].ToString("f3") + "\t";
                }
                Debug.Log(msg);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }


        private void Normalize()
        {
            float len = audioClip.length;
            fs = audioClip.frequency;
            int count = (int)(fs * audioClip.length);
            Debug.Log(fs + "  " + len);
            audioBuffer = new float[count];
            audioClip.GetData(audioBuffer, 0);
            float max = audioBuffer.Max();
            float min = audioBuffer.Min();
            max = Mathf.Max(max, -min);
            for (int i = 0; i < count; i++)
            {
                audioBuffer[i] = Mathf.Abs(audioBuffer[i] / max);
            }
        }


        private int CulSteps(out float w, out float sec)
        {
            int step = 0;
            float i = 0;
            int hz = audioClip.frequency;
            float ax = hz / 1000.0f;
            float win2 = window * ax;
            float step2 = this.step * ax;
            float last = hz * audioClip.length - win2;
            while (i < last)
            {
                step++;
                i += step2;
            }
            // 一个窗口的帧数
            w = window * ax;
            // 一个窗口的对应秒数
            sec = w / hz;

            return step;
        }

    }

}
