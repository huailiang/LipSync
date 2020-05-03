using System;
using System.Collections.Generic;
using UnityEngine;


public class LpcModel
{
    struct Complex
    {
        public bool Equals(Complex other)
        {
            return real.Equals(other.real) && imag.Equals(other.imag);
        }

        public override bool Equals(object obj)
        {
            return obj is Complex other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (real.GetHashCode() * 397) ^ imag.GetHashCode();
            }
        }

        public double real, imag;

        private static readonly Complex zeroVector = new Complex(0.0f, 0.0f);

        public Complex(double real, double imag)
        {
            this.real = real;
            this.imag = imag;
        }

        public static Complex zero
        {
            get
            {
                return Complex.zeroVector;
            }
        }

        public double abs
        {
            get
            {
                double v = real * real + imag * imag;
                return Math.Sqrt(v);
            }
        }

        // If no errors occur, returns the phase angle of z in the interval [−π; π].
        public double arg
        {
            get
            {
                return Math.Atan2(imag, real);
            }
        }

        public static Complex operator +(Complex l, Complex r)
        {
            return new Complex(l.real + r.real, l.imag + r.imag);
        }

        public static Complex operator -(Complex l, Complex r)
        {
            return new Complex(l.real - r.real, l.imag - r.imag);
        }

        public static Complex operator *(Complex l, Complex r)
        {
            return new Complex(l.real * r.real, l.imag * r.imag);
        }

        public static Complex operator *(double ax, Complex r)
        {
            return new Complex(ax * r.real, ax * r.imag);
        }

        public static Complex operator /(Complex l, Complex r)
        {
            return new Complex(l.real / r.real, l.imag / r.imag);
        }

        public static Complex operator /(double l, Complex r)
        {
            return new Complex(l / r.real, l / r.imag);
        }

        public static bool operator ==(Complex l, Complex r)
        {
            return l.real == r.real && l.imag == r.imag;
        }

        public static bool operator !=(Complex l, Complex r)
        {
            return l.real != r.real || l.imag != r.imag;
        }
    }

    double[] estimateLpcCoefficients(float[] samples, int sampleRate, int modelLength)
    {
        double[] correlations = new double[modelLength];
        double[] coefficients = new double[modelLength];
        double modelError;
        if (samples.Length < modelLength)
        {
            double[] ret = new double[modelLength + 1];
            for (int i = 0; i < modelLength + 1; i++)
            {
                ret[i] = 1;
            }

            return ret;
        }

        for (int delay = 0; delay < modelLength; delay++)
        {
            double correlationSum = 0.0;
            for (int sampleIndex = 0; sampleIndex < samples.Length - delay; sampleIndex++)
            {
                correlationSum += samples[sampleIndex] * samples[sampleIndex + delay];
            }

            correlations[delay] = correlationSum;
        }

        modelError = correlations[0];
        coefficients[0] = 1.0;

        for (int delay = 1; delay < modelLength; delay++)
        {
            var rcNum = 0.0;
            for (int i = 1; i < delay; i++)
            {
                rcNum -= coefficients[delay - i] * correlations[i];
            }

            coefficients[delay] = rcNum / modelError;
            //
            // for (int i = 0; i < UPPER; i++)
            // {
            //     
            // }
        }

        return coefficients;
    }


    double[] synthesizeResponseForLPC(double[] coefficients, int samplingRate, int[] frequencies)
    {
        double[] retval = new double[frequencies.Length];
        int index = 0;
        foreach (var frequency in frequencies)
        {
            var radians = frequency / (double) samplingRate * Mathf.PI * 2;
            Complex response = new Complex(0.0, 0.0);
            for (int i = 0; i < coefficients.Length; i++)
            {
                var c = coefficients[i];
                response += new Complex(c < 0 ? -c : c, i * radians);
            }

            double v = 20 * Mathf.Log10((float) (1.0 / response.abs));
            retval[index++] = v;
        }

        return retval;
    }


    Complex laguerreRoot(Complex[] polynomial, Complex guess)
    {
        var m = polynomial.Length - 1;
        var MR = 8;
        var MT = 10;
        var maximumIterations = MR * MT;
        var EPSS = 1.0e-7;
        double abx, abp, abm, err;
        Complex dx, x1, b, d, f, g, h, sq, gp, gm, g2;
        var frac = new Double[] {0.0, 0.5, 0.25, 0.75, 0.125, 0.375, 0.625, 0.875, 1.0};
        var x = guess;
        for (int iteration = 1; iteration < maximumIterations; iteration++)
        {
            b = polynomial[m];
            err = b.abs;
            d = new Complex(0.0, 0.0);
            f = new Complex(0.0, 0.0);
            abx = x.abs;


            for (int j = m - 1; j >= 0; j--)
            {
                f = x * f + d;
                d = x * d + b;
                b = x * b + polynomial[j];
                err = b.abs + abx * err;
            }

            err *= EPSS; // estimate of round-off error in evaluating polynomial
            if (b.abs < err)
            {
                return x;
            }

            g = d / b;
            g2 = g * g;
            h = g2 - 2.0 * f / b;
            sq = Math.Sqrt(m - 1) * (m * h - g2);
            gp = g + sq;
            gm = g - sq;
            abp = gp.abs;
            abm = gm.abs;
            if (abp < abm)
            {
                gp = gm;
            }

            dx = Math.Max(abp, abm) > 0.0
                ? m / gp
                : (1 + abx) * new Complex(Mathf.Cos(iteration), Mathf.Sin(iteration));
            x1 = x - dx;
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

        Debug.Log("Too many iterations in Laguerre, giving up");
        return new Complex();
    }

    double[] findFormants(Complex[] polynomial, int rate)
    {
        var EPS = 2.0e-6;

        List<Complex> roots = new List<Complex>();
        var deflatedPolynomial = polynomial;
        var modelOrder = polynomial.Length - 1;
        for (int j = modelOrder; j >= 1; j--)
        {
            var root = laguerreRoot(deflatedPolynomial, Complex.zero);

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
            polishedRoots[i] = laguerreRoot(polynomial, roots[i]);
        }

        //// Find real frequencies corresponding to all roots
        List<double> formantFrequencies=new List<double>();
        for (int i = 0; i < polishedRoots.Length; i++)
        {
            var t = polishedRoots[i].arg * rate / Math.PI / 2;
            formantFrequencies.Add(t);
        }
        
        formantFrequencies.Sort();
        return formantFrequencies.ToArray();
    }
}
