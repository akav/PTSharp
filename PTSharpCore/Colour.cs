using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]

    public struct Colour
    {
        public double r;
        public double g;
        public double b;

        public Colour(double R, double G, double B)
        {
            r = R;
            g = G;
            b = B;
        }

        public Colour(Colour c) : this(c.r, c.g, c.b) { }

        public static Colour Black = new Colour(0, 0, 0);
        public static Colour White = new Colour(1, 1, 1);

        public Vector ToVector()
        {
            return new Vector(r, g, b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Luminance()
        {
            return 0.2126 * this.r + 0.7152 * this.g + 0.0722 * this.b;
        }

        public static Colour NewColor(int r, int g, int b)
        {
            return new Colour((double)r / 255, (double)g / 255, (double)b / 255);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour HexColor(int x)
        {
            var red = ((x >> 16) & 0xff) / 255.0;
            var green = ((x >> 8) & 0xff) / 255.0;
            var blue = (x & 0xff) / 255.0;
            Colour color = new Colour(red, green, blue);
            return color.Pow(2.2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Pow(double b) => new Colour(Math.Pow(r, b), Math.Pow(g, b), Math.Pow(this.b, b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Mix(Colour b, double pct)
        {
            Colour a = MulScalar(1 - pct);
            b = b.MulScalar(pct);
            return a.Add(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour MulScalar(double b)
        {
            return new Colour(r * b, g * b, this.b * b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Add(Colour b)
        {
            return new Colour(r + b.r, g + b.g, this.b + b.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Sub(Colour b)
        {
            return new Colour(r - b.r, g - b.g, this.b - b.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Mul(Colour b)
        {
            return new Colour(r * b.r, g * b.g, this.b * b.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Div(Colour b)
        {
            return new Colour(r / b.r, g / b.g, this.b / b.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour DivScalar(double b)
        {
            return new Colour(r / b, g / b, this.b / b);
        }

        public Colour Min(Colour b)
        {
            return new Colour(Math.Min(r, b.r), Math.Min(g, b.g), Math.Min(this.b, b.b));
        }

        public Colour Max(Colour b) => new Colour(Math.Max(r, b.r), Math.Max(g, b.g), Math.Max(this.b, b.b));

        public double MinComponent() => Math.Min(Math.Min(r, g), b);

        public double MaxComponent() => Math.Max(Math.Max(r, g), b);

        public static int GetIntFromColor(double r, double g, double b)
        {
            int ri = Math.Max(0, Math.Min(255, (int)Math.Round(r * 255.0)));
            int gi = Math.Max(0, Math.Min(255, (int)Math.Round(g * 255.0)));
            int bi = Math.Max(0, Math.Min(255, (int)Math.Round(b * 255.0)));
            return (ri << 16) | (gi << 8) | bi;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator +(Colour c1, Colour c2)
        {
            return new Colour(c1.r + c2.r, c1.g + c2.g, c1.b + c2.b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour Kelvin(double K)
        {
            double red, green, blue;
            double temp = K / 100.0;

            if (temp <= 66)
            {
                red = 255;
                green = 99.4708025861 * Math.Log(temp) - 161.1195681661;

                if (temp <= 19)
                    blue = 0;
                else
                    blue = 138.5177312231 * Math.Log(temp - 10) - 305.0447927307;
            }
            else
            {
                red = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
                green = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
                blue = 255;
            }

            // Clamp RGB values to the range [0, 255]
            red = Math.Min(255, Math.Max(0, red));
            green = Math.Min(255, Math.Max(0, green));
            blue = Math.Min(255, Math.Max(0, blue));

            // Normalize to the range [0, 1] and return Colour instance
            return new Colour(red / 255.0, green / 255.0, blue / 255.0);
        }
    }
}
