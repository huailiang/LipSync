using System;

public struct Complex : IEquatable<Complex>
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

    private static readonly Complex in_zero = new Complex(0.0);
    private static readonly Complex in__one = new Complex(1.0);

    public Complex(double real)
    {
        this.real = real;
        this.imag = 0;
    }

    public Complex(double real, double imag)
    {
        this.real = real;
        this.imag = imag;
    }

    public static Complex zero
    {
        get { return Complex.in_zero; }
    }

    public static Complex one
    {
        get { return Complex.in__one; }
    }

    public double abs
    {
        get
        {
            double v = real * real + imag * imag;
            return Math.Sqrt(v);
        }
        set
        {
            var f = value / abs;
            real = real * f;
            imag = imag * f;
        }
    }

    // If no errors occur, returns the phase angle of z in the interval [−π; π].
    public double arg
    {
        get
        {
            return Math.Atan2(imag, real);
        }
        set
        {
            var m = abs;
            real = m * Math.Cos(value);
            imag = m * Math.Sin(value);
        }
    }

    public double norm
    {
        get
        {
            return real * real + imag * imag;
        }
    }

    /// <summary>
    /// 共轭复数
    /// </summary>
    public Complex conj
    {
        get
        {
            return new Complex(real, -imag);
        }
    }

    /// <summary>
    /// 参考： https://wenku.baidu.com/view/5bd7d21ee518964bcf847c8d.html
    /// </summary>
    public Complex sqrt
    {
        get
        {
            var d = Math.Sqrt(norm);
            var r = Math.Sqrt((real + d) / 2.0);
            if (imag < 0.0)
            {
                return new Complex(r, -Math.Sqrt((-real + d) / 2.0));
            }
            else
            {
                return new Complex(r, Math.Sqrt((-real + d) / 2.0));
            }
        }
    }

    public Complex cos
    {
        get
        {
            double r = Math.Cos(real) * Math.Cosh(imag);
            double i = -Math.Sin(real) * Math.Sinh(imag);
            return new Complex(r, i);
        }
    }

    public Complex sin
    {
        get
        {
            double r = Math.Sin(real) * Math.Cosh(imag);
            double i = Math.Cos(real) * Math.Sinh(imag);
            return new Complex(r, i);
        }
    }

    public Complex tan
    {
        get { return sin / cos; }
    }

    public static Complex operator +(double l, Complex r)
    {
        return new Complex(l + r.real, l + r.imag);
    }

    public static Complex operator +(Complex l, Complex r)
    {
        return new Complex(l.real + r.real, l.imag + r.imag);
    }

    public static Complex operator +(Complex l, double r)
    {
        return new Complex(l.real + r, l.imag);
    }

    public static Complex operator -(Complex l, Complex r)
    {
        return new Complex(l.real - r.real, l.imag - r.imag);
    }

    public static Complex operator -(Complex l, double r)
    {
        return new Complex(l.real - r, l.imag);
    }

    public static Complex operator *(Complex l, Complex r)
    {
        var real = l.real * r.real - l.imag * r.imag;
        var imag = l.real * r.imag + l.imag * r.real;
        return new Complex(real, imag);
    }

    public static Complex operator *(Complex r, double ax)
    {
        return new Complex(ax * r.real, ax * r.imag);
    }

    public static Complex operator *(double ax, Complex r)
    {
        return new Complex(ax * r.real, ax * r.imag);
    }

    public static Complex operator /(Complex l, Complex r)
    {
        return (l.real * r.conj) / r.norm;
    }

    public static Complex operator /(Complex l, double r)
    {
        return new Complex(l.real / r, l.imag / r);
    }

    public static Complex operator /(double l, Complex r)
    {
        var cpx = new Complex(l, 0);
        return cpx / r;
    }

    public static bool operator ==(Complex l, Complex r)
    {
        return l.real == r.real && l.imag == r.imag;
    }

    public static bool operator !=(Complex l, Complex r)
    {
        return l.real != r.real || l.imag != r.imag;
    }

    public override string ToString()
    {
        return $"{real:f3}, {imag:f3}i";
    }
}
