using System.Runtime.InteropServices;
using System.Security;
using UnityEngine;

namespace LipSync
{
#if !UNITY_IPHONE
    [SuppressUnmanagedCodeSecurity]
#endif
    public class MathToolBox
    {
        public enum EPaddleType
        {
            /// <summary>
            /// Paddle with zeros.
            /// </summary>
            Zero = 0,

            /// <summary>
            /// Repeat the first value on the left and the last value on the right.
            /// </summary>
            Repeat = 1,

            /// <summary>
            /// Loop the array.
            /// </summary>
            Loop = 2
        }

        public enum EWindowType
        {
            Rectangular,
            Triangle,
            Hamming,
            Hanning,
            BlackMan,
            BlackmanHarris
        }

        /// <summary>
        /// 离散余弦变换 DCT, 类似于离散傅里叶变换(DFT, Discrete Fourier Transform),但是只使用实数
        /// </summary>
        /// <param name="data">Source data.</param>
        public static float[] DiscreteCosineTransform(float[] data)
        {
            float[] result = new float[data.Length];
            float sumCos;
            for (int m = 0; m < data.Length; ++m)
            {
                sumCos = 0.0f;
                for (int k = 0; k < data.Length; ++k)
                {
                    sumCos += data[k] * Mathf.Cos((Mathf.PI / data.Length) * m * (k + 0.5f));
                }
                result[m] = (sumCos > 0) ? sumCos : -sumCos;
            }

            return result;
        }

        /// <summary>
        /// Convolute data and filter. Result is sent to output, which must not be shorter than data.
        /// </summary>
        /// <param name="output">Array to store output. Must not be shorter than data.</param>
        /// <param name="data">Source data array.</param>
        /// <param name="filter">Filter array.</param>
        /// <param name="paddleType">Paddle type.</param>
        public static void Convolute(float[] data, float[] filter, EPaddleType paddleType, float[] output)
        {
            int filterMiddlePoint = Mathf.FloorToInt(filter.Length / 2);
            for (int n = 0; n < data.Length; ++n)
            {
                output[n] = 0.0f;
                for (int m = 0; m < filter.Length; ++m)
                {
                    output[n] += MathToolBox.GetValueFromArray(data, n - filterMiddlePoint + m, paddleType) *
                                 filter[filter.Length - m - 1];
                }
            }
        }

        /// <summary>
        /// Find (length of peakvalue) local largest peak(s). 
        /// </summary>
        /// <param name="data">Source data.</param>
        /// <param name="peakValue">Array to store peak values.</param>
        /// <param name="peakPosition">Array to store peak values' positions.</param>
        public static void FindLocalLargestPeaks(float[] data,  float[] peakValue, int[] peakPosition)
        {
            int peakNum = 0;
            float lastPeak = 0.0f;
            int lastPeakPosition = 0;
            bool isIncreasing = false;
            bool isPeakIncreasing = false;
            
            for (int i = 0; i < data.Length - 1; ++i)
            {
                if (data[i] < data[i + 1])
                {
                    isIncreasing = true;
                }
                else
                {
                    if (isIncreasing)
                    {
                        if (lastPeak < data[i]) // Peak found. 
                        {
                            isPeakIncreasing = true;
                        }
                        else
                        {
                            if (isPeakIncreasing)
                            {
                                // Local largest peak found. 
                                peakValue[peakNum] = lastPeak;
                                peakPosition[peakNum] = lastPeakPosition;
                                ++peakNum;
                            }

                            isPeakIncreasing = false;
                        }

                        lastPeak = data[i];
                        lastPeakPosition = i;
                    }
                    isIncreasing = false;
                }
               
                if (peakNum >= peakValue.Length)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 生成高斯滤波器， 其是一种线性平滑滤波，可以去除高斯噪声
        /// Make sure not to call this function too frequently. Recommend caching the result.
        /// </summary>
        /// <param name="size">Size of the filter.</param>
        /// <param name="deviationSquare">Deviation's square.</param>
        public static float[] GenerateGaussianFilter(int size, float deviationSquare)
        {
            float[] result = new float[size];

            float sum = 0.0f;
            float mu = (float)(size - 1) / 2;
            for (int i = 0; i < size; ++i)
            {
                float param = -((i - mu) * (i - mu)) / (2 * deviationSquare);
                result[i] = Mathf.Exp(param);
                sum += result[i];
            }

            for (int j = 0; j < size; ++j)
            {
                result[j] /= sum;
            }

            return result;
        }

        /*
         * https://docs.scipy.org/doc/scipy-0.19.1/reference/signal.html
         */
        public static float[] GenerateWindow(int size, EWindowType windowType)
        {
            float[] result = new float[size];
            switch (windowType)
            {
                case EWindowType.Rectangular:
                    for (int i = 0; i < size; ++i) result[i] = 1.0f;
                    break;
                case EWindowType.Triangle:
                    for (int i = 0; i < size; ++i)
                        result[i] = i < size / 2.0f ? 2 * i / (size - 2) : (2 * size - 4 - 2 * i) / (size - 2);
                    break;
                case EWindowType.Hamming:
                    for (int i = 0; i < size; ++i)
                        result[i] = 0.54f - 0.46f * Mathf.Cos((2 * Mathf.PI * i) / (size - 1));
                    break;
                case EWindowType.Hanning:
                    for (int i = 0; i < size; ++i)
                        result[i] = 0.5f - 0.5f * Mathf.Cos((2 * Mathf.PI * i) / (size - 1));
                    break;
                case EWindowType.BlackMan:
                    for (int i = 0; i < size; ++i)
                        result[i] = (float)(0.42f - 0.5 * Mathf.Cos((2 * Mathf.PI * i) / size)) +
                                    (float)(0.08 * Mathf.Cos(4 * Mathf.PI * i) / size);
                    break;
                case EWindowType.BlackmanHarris:
                    for (int i = 0; i < size; ++i)
                        result[i] = (float)((0.35875 - 0.48829 * Mathf.Cos((2 * Mathf.PI * i) / size)) +
                                             (0.14128 * Mathf.Cos(4 * Mathf.PI * i) / size) -
                                             (0.01168 * Mathf.Cos(6 * Mathf.PI * i / size)));
                    break;
                default:
                    Debug.LogWarning("not implement yet now");
                    break;
            }

            return result;
        }

        public static float GetValueFromArray(float[] data, int index, EPaddleType paddleType)
        {
            if (index >= 0 && index < data.Length)
            {
                return data[index];
            }
            else
            {
                switch (paddleType)
                {
                    case EPaddleType.Zero:
                        return 0;
                    case EPaddleType.Repeat:
                        return index < 0 ? data[0] : data[data.Length - 1];
                    case EPaddleType.Loop:
                        int actualIndex = index;
                        while (actualIndex < 0)
                        {
                            actualIndex += data.Length;
                        }

                        actualIndex %= data.Length;
                        return data[actualIndex];
                    default:
                        return 0;
                }
            }
        }
        
#if UNITY_IPHONE
#if UNITY_EDITOR
        const string ZSolverDLL = "ZSolver";
#else
        const string ZSolverDLL = "__Internal";
#endif
#else
        const string ZSolverDLL = "ZSolver";
#endif

        [DllImport(ZSolverDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void poly_roots(int size, double[] c, double[] z);
    }
}