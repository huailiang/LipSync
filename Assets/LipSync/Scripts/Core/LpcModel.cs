using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LpcModel
{
    /// Estimate LPC polynomial coefficients from the signal
    /// Uses the Levinson-Durbin recursion algorithm
    /// - Returns: `len` + 1 autocorrelation coefficients for an all-pole model
    /// the first coefficient is 1.0 for perfect autocorrelation with zero offset
    public float[] EstimateLpcCoefficients(float[] samples, int len)
    {
        float[] correlations = new float[len + 1];
        float[] coefficients = new float[len + 1];
        if (samples.Length <= len)
        {
            float[] ret = new float[len + 1];
            for (int i = 0; i <= len; i++)
            {
                ret[i] = 1;
            }
            return ret;
        }
        for (int delay = 0; delay <= len; delay++)
        {
            float correlationSum = 0.0f;
            for (int sampleIndex = 0; sampleIndex < len - delay; sampleIndex++)
            {
                correlationSum += samples[sampleIndex] * samples[sampleIndex + delay];
            }
            correlations[delay] = correlationSum;
        }
        var modelError = correlations[0];
        coefficients[0] = 1.0f;
        for (int delay = 1; delay <= len; delay++)
        {
            var rcNum = 0.0f;
            for (int i = 1; i <= delay; i++)
            {
                rcNum -= coefficients[delay - i] * correlations[i];
            }
            coefficients[delay] = rcNum / modelError;

            for (int i = 1; i < delay / 2; i++)
            {
                var pci = coefficients[i] + coefficients[delay] * coefficients[delay - i];
                var pcki = coefficients[delay - i] + coefficients[delay] * coefficients[i];
                coefficients[i] = pci;
                coefficients[delay - 1] = pcki;
            }
            modelError *= 1.0f - coefficients[delay] * coefficients[delay];
        }
        return coefficients;
    }


    /// Synthesize the frequency response for the estimated LPC coefficients
    ///
    /// - Parameter coefficients: an all-pole LPC model
    /// - Parameter samplingRate: the sampling frequency in Hz
    /// - Parameter frequencies: the frequencies whose response you'd like to know
    /// - Returns: a response from 0 to 1 for each frequency you are interrogating
    public float[] SynthesizeResponseForLPC(float[] coefficients, int samplingRate, int[] frequencies)
    {
        float[] retval = new float[frequencies.Length];
        int index = 0;
        foreach (var frequency in frequencies)
        {
            var radians = frequency / (double)samplingRate * Mathf.PI * 2;
            Complex response = new Complex(0.0, 0.0);
            for (int i = 0; i < coefficients.Length; i++)
            {
                var c = coefficients[i];
                response += new Complex(c < 0 ? -c : c, i * radians);
            }
            float v = 20 * Mathf.Log10((float)(1.0 / response.abs));
            retval[index++] = v;
        }
        return retval;
    }

    /// Laguerre's method to find one root of the given complex polynomial
    /// Call this method repeatedly to find all the complex roots one by one
    /// Algorithm from Numerical Recipes in C by Press/Teutkolsky/Vetterling/Flannery
    public static Complex LaguerreRoot(Complex[] polynomial, Complex guess)
    {
        int m = polynomial.Length - 1;
        const int MR = 8;
        const int MT = 10;
        int maximumIterations = MR * MT;
        const double EPSS = 1.0e-7;
        double abx, abp, abm, err;
        var frac = new Double[] { 0.0, 0.5, 0.25, 0.75, 0.125, 0.375, 0.625, 0.875, 1.0 };
        Complex x = guess;
        for (int iteration = 1; iteration <= maximumIterations; iteration++)
        {
            Complex b = polynomial[m];
            err = b.abs;
            Complex d = Complex.zero;
            Complex f = Complex.zero;
            abx = x.abs;
            for (int j = m - 1; j >= 0; j--)
            {
                f = x * f + d;
                d = x * d + b;
                b = x * b + polynomial[j];
                err = b.abs + abx * err;
            }
            err *= EPSS; // estimate of round-off error in evaluating polynomial
            if (b.abs < err) return x;

            Complex g = d / b;
            Complex g2 = g * g;
            Complex h = g2 - 2.0 * f / b;
            Complex sq = ((m - 1) * (m * h - g2)).sqrt;
            Complex gp = g + sq;
            Complex gm = g - sq;
            abp = gp.abs;
            abm = gm.abs;
            if (abp < abm) gp = gm;

            var dx = Math.Max(abp, abm) > 0.0 ? m / gp
                : (1 + abx) * new Complex(Mathf.Cos(iteration), Mathf.Sin(iteration));
            Complex x1 = x - dx;
            if (x == x1)
            {
                return x; // converged
            }

            // Every so often we take a fractional step, to break any limit cycle (itself a rare occurrence)
            if (iteration % MT > 0)
            {
                x = x1;
            }
            else
            {
                x = x - frac[iteration / MT] * dx;
            }
        }
        Debug.LogError("Too many iterations in Laguerre, giving up");
        return Complex.zero;
    }

    public double[] FindFormants(float[] coefficients, int rate)
    {
        var complexPolynomial = coefficients.Select(x => new Complex(x, 0));
        return FindFormants(complexPolynomial.ToArray(), rate);
    }

    // Use Laguerre's method to find roots.
    /// - Parameter polynomial: coefficients of the input polynomial
    /// - Note: Does not implement root polishing, so accuracy may be impacted
    /// - Note: May include duplicated roots/formants
    public double[] FindFormants(Complex[] polynomial, int rate)
    {
        var EPS = 2.0e-6;
        List<Complex> roots = new List<Complex>();
        var deflatedPolynomial = polynomial;
        var modelOrder = polynomial.Length - 1;
        for (int j = modelOrder; j >= 1; j--)
        {
            var root = LaguerreRoot(deflatedPolynomial, Complex.zero);
            // If imaginary part is very small, ignore it
            if (Math.Abs(root.imag) < 2.0 * EPS * Math.Abs(root.real))
            {
                root.imag = 0.0;
            }
            roots.Add(root);

            // Perform forward deflation. Divide by the factor of the root found above
            var b = deflatedPolynomial[j];
            for (int jj = j - 1; jj >= 0; jj--)
            {
                var c = deflatedPolynomial[jj];
                deflatedPolynomial[jj] = b;
                b = root * b + c;
            }
        }

        Complex[] polishedRoots = new Complex[polynomial.Length];
        for (int i = 0; i < polynomial.Length; i++)
        {
            polishedRoots[i] = LaguerreRoot(polynomial, roots[i]);
        }

        //// Find real frequencies corresponding to all roots
        List<double> formantFrequencies = new List<double>();
        for (int i = 0; i < polishedRoots.Length; i++)
        {
            var t = polishedRoots[i].arg * rate / Math.PI / 2;
            formantFrequencies.Add(t);
        }
        formantFrequencies.Sort();
        return formantFrequencies.ToArray();
    }


    private static Complex RandomFloat(Complex low, Complex high)
    {
        float rand = 0.5f;// UnityEngine.Random.Range(0.0f, 1.0f);
        Complex d = new Complex(rand);
        Complex k = d * (high - low + 1);
        return low + k;
    }

    private static Complex Evalpoly(Complex x, Complex[] a, int n)
    {
        if (n < 0) n = 0;
        Complex p = a[n];
        for (int i = 1; i <= n; i++)
        {
            p = a[n - i] + p * x;
        }
        return p;
    }

    private static Complex[] PolyDerivative(Complex[] a, int n)
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

    private static Complex Laguerre(Complex[] a, int n, double tol)
    {
        const int IMAX = 10000;
        Complex x = RandomFloat(Complex.zero, new Complex(100));
        for (int i = 0; i < IMAX; i++)
        {
            Complex poly = Evalpoly(x, a, n);
            Complex[] fderivativetemp = PolyDerivative(a, n);
            Complex fderivative = Evalpoly(x, fderivativetemp, n - 1);
            Complex sderivative = Evalpoly(x, PolyDerivative(fderivativetemp, n - 1), n - 2);
            if (poly.abs < tol)
            {
                return x;
            }

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
            if (dx.abs < tol)
                return x;
        }
        Debug.LogError("ERROR: Too many iterations!");
        return Complex.zero;
    }

    private static Complex[] Deflate(Complex root, Complex[] a, int n)
    {
        Complex[] b = new Complex[n];
        b[n - 1] = a[n];
        for (int i = n - 2; i >= 0; i--)
        {
            b[i] = a[i + 1] + root * b[i + 1];
        }
        return b;
    }


    public static Complex[] FindRoots(Complex[] poly)
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
    public static double[] correlate(double[] a, double[] v)
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

}