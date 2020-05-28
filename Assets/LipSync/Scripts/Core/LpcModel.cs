using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LipSync
{
    public class LpcModel
    {
        public int window, step, fs;
        private float[] audioBuffer;


        public double[] Estimate(float[] signal, int order)
        {
            if (order > signal.Length)
            {
                throw new Exception("Input signal must have a lenght >= lpc order");
            }
            if (order <= 0)
            {
                throw new Exception("lpc order must greater 0");
            }

            int p = order + 1;
            var nx = Math.Min(p, signal.Length);
            var x = Correlate(signal, signal);

            var r1 = new double[nx - 1];
            var r2 = new double[nx - 1];
            for (int i = 0; i < nx; i++)
            {
                if (i > 0)
                {
                    r1[i - 1] = x[signal.Length - 1 + i];
                }
                if (i < nx - 1)
                {
                    r2[i] = -x[signal.Length - 1 + i];
                }
            }
            ToeplitzMtrix mtrix = new ToeplitzMtrix(r1);
            var inv = mtrix.Inverse();
            if (inv != null)
                mtrix = new ToeplitzMtrix(inv);
            var phi = mtrix.Dot(r2);
            var ret = new double[phi.Length + 1];
            ret[0] = 1;
            for (int i = 0; i < phi.Length; i++)
            {
                ret[i + 1] = phi[i];
            }
            return ret;
        }

        private Complex RandomFloat(Complex low, Complex high)
        {
            float rand = UnityEngine.Random.Range(0.0f, 1.0f);
            Complex d = new Complex(rand);
            Complex k = d * (high - low + 1);
            return low + k;
        }

        private Complex Evalpoly(Complex x, Complex[] a, int n)
        {
            if (n < 0) n = 0;
            Complex p = a[n];
            for (int i = 1; i <= n; i++)
            {
                p = a[n - i] + p * x;
            }
            return p;
        }

        private Complex[] PolyDerivative(Complex[] a, int n)
        {
            if (n == 0)
            {
                return new Complex[] { Complex.zero };
            }
            Complex[] b = new Complex[n];
            b[0] = a[1];
            for (int i = 1; i < n; i++)
            {
                b[i] = a[i + 1] * (i + 1);
            }
            return b;
        }

        private Complex Laguerre(Complex[] a, int n, double tol)
        {
            const int IMAX = 10000;
            Complex x = RandomFloat(Complex.zero, new Complex(100));
            for (int i = 0; i < IMAX; i++)
            {
                Complex poly = Evalpoly(x, a, n);
                Complex[] fderivativetemp = PolyDerivative(a, n);
                Complex fderivative = Evalpoly(x, fderivativetemp, n - 1);
                Complex sderivative = Evalpoly(x, PolyDerivative(fderivativetemp, n - 1), n - 2);
                if (poly.abs < tol) return x;

                Complex g = fderivative / poly;
                Complex h = g * g - sderivative / poly;
                Complex f = ((n - 1) * (n * h - g * g)).sqrt;
                Complex dx = Complex.zero;
                if ((g + f).abs > (g - f).abs)
                {
                    dx = n / (g + f);
                }
                else
                {
                    dx = n / (g - f);
                }
                x = x - dx;
                if (dx.abs < tol) return x;
            }
            Debug.LogError("ERROR: Too many iterations!");
            return Complex.zero;
        }

        private Complex[] Deflate(Complex root, Complex[] a, int n)
        {
            Complex[] b = new Complex[n];
            b[n - 1] = a[n];
            for (int i = n - 2; i >= 0; i--)
            {
                b[i] = a[i + 1] + root * b[i + 1];
            }
            return b;
        }

        public Complex[] FindRoots(float[] poly)
        {
            int len = poly.Length;
            Complex[] cs = new Complex[len];
            for (int i = 0; i < len; i++) cs[len - i - 1] = new Complex(poly[i]);
            return FindRoots(cs);
        }

        public Complex[] FindCRoots(IEnumerable<double> dpoly)
        {
            int len = dpoly.Count();
            int len2 = (len - 1) * 2;
            double[] ret = new double[len2];
            MathToolBox.poly_roots(len, dpoly.Reverse().ToArray(), ret);
            Complex[] cpx = new Complex[len - 1];
            for (int i = 0; i < len - 1; i++)
            {
                cpx[i] = new Complex(ret[2 * i], ret[2 * i + 1]);
            }
            return cpx;
        }

        private Complex[] FindRoots(Complex[] poly)
        {
            int n = poly.Length - 1;
            int N = n;
            Complex[] ret = new Complex[N];
            for (int i = 0; i < N; i++)
            {
                ret[i] = Laguerre(poly, n, 1e-13);
                poly = Deflate(ret[i], poly, n--);
            }
            return ret;
        }


        /// <summary>
        /// 线性相关Linear Cross-Correlation
        /// 实现类似于numpy.correlate
        /// mode = "full"
        /// 参考：https://fanyublog.com/2015/11/16/corr_python
        /// </summary>
        /// <param name="a">Input sequences.</param>
        /// <param name="v">Input sequences.</param>
        /// <returns>iscrete cross-correlation of `a` and `v`</returns>
        public double[] Correlate(float[] a, float[] v)
        {
            if (a.Length < v.Length)
            {
                throw new Exception("correlate error, a is larger than v");
            }

            int n = a.Length + v.Length - 1;
            double[] ret = new double[n];
            for (int i = 0; i < n; i++)
            {
                ret[i] = 0;
                int d = v.Length - 1 - i;
                if (d >= 0)
                {
                    int j = 0;
                    while (d + j < v.Length)
                    {
                        ret[i] += a[j] * v[d + j];
                        j++;
                    }
                }
                else
                {
                    int j = -d;
                    int t = 0;
                    while (t < v.Length && j < a.Length)
                    {
                        ret[i] += v[t++] * a[j];
                        j++;
                    }
                }
            }
            return ret;
        }


        private List<float[]> MakeFrame()
        {
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

        private List<double[]> Formants(List<float[]> splitting)
        {
            int i = 0;
            float a = 0.67f;
            List<double[]> ret = new List<double[]>();
            while (i < splitting.Count())
            {
                float[] FL = PreEmphasis(splitting[i], a);
                float[] w = MathToolBox.GenerateWindow(window, MathToolBox.EWindowType.Hamming);
                for (int j = 0; j < window; j++)
                {
                    FL[j] = FL[j] * w[j];
                }
                Debug.Log(i);
                var coefficients = Estimate(FL, 2 + fs / 1000);
                var rts = FindCRoots(coefficients).Where(x => x.imag >= 0.0);
                var frqs = rts.Select(x => x.arg * (fs / (2 * Mathf.PI))).ToList();
                frqs.Sort();
                double[] fmts = { frqs[1], frqs[2] };
                ret.Add(fmts);
                i++;
            }
            return ret;
        }

        public List<double[]> Analy(float[] buffer, int window, int step)
        {
            this.step = (step * fs) / 1000;
            this.window = (window * fs) / 1000;
            this.audioBuffer = buffer;
            var split = MakeFrame();
            return Formants(split);
        }

       
    }
}
