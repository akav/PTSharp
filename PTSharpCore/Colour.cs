using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Colour
    {
        public double r;
        public double g;
        public double b;

        public Colour(Colour c)
        {
            r = c.r;
            g = c.g;
            b = c.b;
        }

        public Colour(double R, double G, double B)
        {
            r = R;
            g = G;
            b = B;
        }

        public static Colour Black = new(0, 0, 0);
        public static Colour White = new(1, 1, 1);
        public static Colour Red = new(1, 0, 0);
        public static Colour Green = new(0, 1, 0);
        public static Colour Blue = new(0, 0, 1);


        public Colour() { }

        public static Colour operator +(Colour c1, Colour c2)
        {
            return new Colour(c1.r + c2.r, c1.g + c2.g, c1.b + c2.b);
        }

        public static Colour operator +(Colour c1, double c2)
        {
            return new Colour(c1.r + c2, c1.g + c2, c1.b + c2);
        }

        public static Colour operator +(double c1, Colour c2)
        {
            return new Colour(c1 + c2.r, c1 + c2.g, c1 + c2.b);
        }

        public static Colour operator -(Colour c1, Colour c2)
        {
            return new Colour(c1.r - c2.r, c1.g - c2.g, c1.b - c2.b);
        }

        public static Colour operator *(Colour c1, Colour c2)
        {
            return new Colour(c1.r * c2.r, c1.g * c2.g, c1.b * c2.b);
        }

        public static Colour operator *(double c, Colour c2)
        {
            return new Colour(c * c2.r, c * c2.g, c * c2.b);
        }

        public static Colour operator *(Colour c1, double c)
        {
            return new Colour(c * c1.r, c * c1.g, c * c1.b);
        }

        public static Colour operator /(Colour c1, double c)
        {
            return new Colour(c1.r / c, c1.g / c, c1.b / c);
        }

        public static Colour operator /(Colour c1, Colour c2)
        {
            return new Colour(c1.r / c2.r, c1.g / c2.g, c1.b / c2.b);
        }

        public static Colour operator -(Colour c1)
        {
            return new Colour(-c1.r, -c1.g, -c1.b);
        }

        public static Colour operator +(Colour c1)
        {
            return new Colour(c1.r, c1.g, c1.b);
        }

        public Vector ToVector()
        {
            return new Vector(r, g, b);
        }

        public double Luminance()
        {
            // Calculate the luminance using the sRGB luminance coefficients
            return 0.2126 * this.r + 0.7152 * this.g + 0.0722 * this.b;
        }
        public static Colour NewColor(int r, int g, int b) => new Colour((double)r / 65535, (double)g / 65535, (double)b / 65535);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Colour HexColor(int x)
        {
            var red = ((x >> 16) & 0xff) / 255.0f;
            var green = ((x >> 8) & 0xff) / 255.0f;
            var blue = (x & 0xff) / 255.0f;
            Colour color = new(red, green, blue);
            return color.Pow(2.2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Colour Pow(double b) => new Colour(Math.Pow(r, b), Math.Pow(g, b), Math.Pow(this.b, b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static int getIntFromColor(double red, double green, double blue)
        {
            if (double.IsNaN(red))
                red = 0;
            if (double.IsNaN(green))
                green = 0;
            if (double.IsNaN(blue))
                blue = 0;

            var r = (int)(256 * Math.Clamp(red, 0.0, 0.999));
            var g = (int)(256 * Math.Clamp(green, 0.0, 0.999));
            var b = (int)(256 * Math.Clamp(blue, 0.0, 0.999));
            return 255 << 24 | r << 16 | g << 8 | b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Colour Kelvin(double K)
        {
            double red, green, blue;
            double a, b, c, x;
            // red
            if (K >= 6600)
            {
                a = 351.97690566805693F;
                b = 0.114206453784165F;
                c = -40.25366309332127F;
                x = K / 100 - 55;

                red = a + b * x + c * Math.Log(x);
            }
            else
            {
                red = 255;
            }
            if (K >= 6600)
            {
                a = 325.4494125711974F;
                b = 0.07943456536662342F;
                c = -28.0852963507957F;
                x = K / 100 - 50;
                green = a + b * x + c * Math.Log(x);
            }
            else if (K >= 1000)
            {
                a = -155.25485562709179F;
                b = -0.44596950469579133F;
                c = 104.49216199393888F;
                x = K / 100 - 2;
                green = a + b * x + c * Math.Log(x);
            }
            else
            {
                green = 0;
            }
            if (K >= 6600)
            {
                blue = 255;
            }
            else if (K >= 2000)
            {
                a = -254.76935184120902F;
                b = 0.8274096064007395F;
                c = 115.67994401066147F;
                x = K / 100 - 10;

                blue = a + b * x + c * Math.Log(x);

            }
            else
            {
                blue = 0;
            }
            red = Math.Min(1, red / 255.0f);
            green = Math.Min(1, green / 255.0f);
            blue = Math.Min(1, blue / 255.0f);
            return new Colour(red, green, blue);
        }

        public Colour Mix(Colour b, double pct)
        {
            Colour a = MulScalar(1 - pct);
            b = b.MulScalar(pct);
            return a.Add(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Colour MulScalar(double b) => new(r * b, g * b, this.b * b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Add(Colour b) => new(this.r + b.r, this.g + b.g, this.b + b.b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Sub(Colour b) => new(r - b.r, g - b.g, this.b - b.b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Mul(Colour b) => new(r * b.r, g * b.g, this.b * b.b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Div(Colour b) => new(r / b.r, g / b.g, this.b / b.b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour DivScalar(double b) => new(r / b, g / b, this.b / b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Min(Colour b) => new(Math.Min(r, b.r), Math.Min(g, b.g), Math.Min(this.b, b.b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Max(Colour b) => new(Math.Max(r, b.r), Math.Max(g, b.g), Math.Max(this.b, b.b));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]            
        public double MinComponent() => Math.Min(Math.Min(r, g), b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MaxComponent() => Math.Max(Math.Max(r, g), b);
    }
}
