using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Colour
    {
        //public double r;
        //public double g;
        //public double b;

        Vector256<double> colourVector;

        public double r
        {
            get { return colourVector.GetElement(0); }
            set { colourVector = colourVector.WithElement(0, value); }
        }

        public double g
        {
            get { return colourVector.GetElement(1); }
            set { colourVector = colourVector.WithElement(1, value); }
        }

        public double b
        {
            get { return colourVector.GetElement(2); }
            set { colourVector = colourVector.WithElement(2, value); }
        }

        public Colour(double R, double G, double B, double A = 1)
        {
            colourVector = Vector256.Create(R, G, B, A);
        }

        public Colour(Vector256<double> vector)
        {
            colourVector = vector;
        }

        public Colour(Colour c) : this(c.r, c.g, c.b) { }
                
        public static Colour Black = new(0, 0, 0);
        public static Colour White = new(1, 1, 1);

        public Colour() { }

        public Vector256<double> ToVector256()
        {
            return colourVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator +(Colour c1, Colour c2)
        {
            return new Colour(Avx2.Add(c1.colourVector, c2.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator +(Colour c1, double c2)
        {
            var addVector = Vector256.Create(c2, c2, c2, 0.0);
            return new Colour(Avx2.Add(c1.colourVector, addVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator +(double c1, Colour c2)
        {
            var addVector = Vector256.Create(c1, c1, c1, 0.0);
            return new Colour(Avx2.Add(c2.colourVector, addVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator -(Colour c1, Colour c2)
        {
            return new Colour(Avx2.Subtract(c1.colourVector, c2.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator *(Colour c1, Colour c2)
        {
            return new Colour(Avx2.Multiply(c1.colourVector, c2.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator *(double c, Colour c2)
        {
            var mulVector = Vector256.Create(c, c, c, 0.0);
            return new Colour(Avx2.Add(c2.colourVector, mulVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator *(Colour c1, double c)
        {
            var mulVector = Vector256.Create(c, c, c, 0.0);
            return new Colour(Avx2.Add(c1.colourVector, mulVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator /(Colour c1, double c)
        {
            var mulVector = Vector256.Create(c, c, c, 0.0);
            return new Colour(Avx2.Divide(c1.colourVector, mulVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator /(Colour c1, Colour c2)
        {
            return new Colour(Avx2.Divide(c1.colourVector, c2.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Colour operator -(Colour c1)
        {
            return new Colour(Avx2.Subtract(Vector256<double>.Zero, c1.colourVector));
        }
                
        public Vector ToVector()
        {
            return new Vector(r, g, b);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Luminance()
        {
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
            var mulVector = Vector256.Create(b, b, b, 0.0);
            return new Colour(Avx2.Multiply(colourVector, mulVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Add(Colour b)
        {
            return new Colour(Avx2.Add(colourVector, b.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Sub(Colour b)
        {
            return new Colour(Avx2.Subtract(colourVector, b.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Mul(Colour b) 
        {
            return new Colour(Avx2.Multiply(colourVector, b.colourVector));          
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour Div(Colour b) 
        {
            return new Colour(Avx2.Divide(colourVector, b.colourVector));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Colour DivScalar(double b)
        {
            var divVector = Vector256.Create(b, b, b, 0.0);
            return new Colour(Avx2.Divide(colourVector, divVector));
        }

        public Colour Min(Colour b)
        {
            return new(Math.Min(r, b.r), Math.Min(g, b.g), Math.Min(this.b, b.b));
        }

        public Colour Max(Colour b) => new(Math.Max(r, b.r), Math.Max(g, b.g), Math.Max(this.b, b.b));

        public double MinComponent() => Math.Min(Math.Min(r, g), b);

        public double MaxComponent() => Math.Max(Math.Max(r, g), b);

        public static int GetIntFromColor(double r, double g, double b)
        {
            int ri = Math.Max(0, Math.Min(255, (int)Math.Round(r * 255.0)));
            int gi = Math.Max(0, Math.Min(255, (int)Math.Round(g * 255.0)));
            int bi = Math.Max(0, Math.Min(255, (int)Math.Round(b * 255.0)));
            return (ri << 16) | (gi << 8) | bi;
        }
    }
}