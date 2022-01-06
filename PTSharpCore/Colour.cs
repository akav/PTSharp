using System;

namespace PTSharpCore
{
    public class Colour
    {
        internal float r;
        internal float g;
        internal float b;

        public Colour(Colour c)
        {
            r = c.r;
            g = c.g;
            b = c.b;
        }

        public Colour(float R, float G, float B)
        {
            r = R;
            g = G;
            b = B;
        }

        public static Colour Black = new Colour(0, 0, 0);
        public static Colour White = new Colour(1, 1, 1);

        public Colour() { }

        public static Colour operator +(Colour c1, Colour c2)
        {
            return new Colour(c1.r + c2.r, c1.g + c2.g, c1.b + c2.b);
        }

        public static Colour operator -(Colour c1, Colour c2)
        {
            return new Colour(c1.r - c2.r, c1.g - c2.g, c1.b - c2.b);
        }

        public static Colour operator *(Colour c1, Colour c2)
        {
            return new Colour(c1.r * c2.r, c1.g * c2.g, c1.b * c2.b);
        }

        public static Colour operator *(float c, Colour c2)
        {
            return new Colour(c * c2.r, c * c2.g, c * c2.b);
        }

        public static Colour operator *(Colour c1, float c)
        {
            return new Colour(c * c1.r, c * c1.g, c * c1.b);
        }

        public static Colour operator /(Colour c1, float c)
        {
            return new Colour(c1.r / c, c1.g / c, c1.b / c);
        }

        public static Colour operator -(Colour c1)
        {
            return new Colour(-c1.r, -c1.g, -c1.b);
        }

        public static Colour operator +(Colour c1)
        {
            return new Colour(c1.r, c1.g, c1.b);
        }

        public static Colour NewColor(int r, int g, int b) => new Colour((float)r / 65535, (float)g / 65535, (float)b / 65535);

        public static Colour HexColor(int x)
        {
            var red = ((x >> 16) & 0xff) / 255.0f;
            var green = ((x >> 8) & 0xff) / 255.0f;
            var blue = (x & 0xff) / 255.0f;
            Colour color = new Colour(red, green, blue);
            return color.Pow(2.2f);
        }

        public Colour Pow(float b) => new Colour(MathF.Pow(r, b), MathF.Pow(g, b), MathF.Pow(this.b, b));

        public int getIntFromColor(float red, float green, float blue)
        {
            if (float.IsNaN(red))
                red = 0.0f;
            if (float.IsNaN(green))
                green = 0.0f;
            if (float.IsNaN(blue))
                blue = 0.0f;

            var r = (int)(256 * Math.Clamp(red, 0.0, 0.999));
            var g = (int)(256 * Math.Clamp(green, 0.0, 0.999));
            var b = (int)(256 * Math.Clamp(blue, 0.0, 0.999));
            return 255 << 24 | r << 16 | g << 8 | b;
        }

        public static Colour Kelvin(float K)
        {
            float red, green, blue;
            float a, b, c, x;
            // red
            if (K >= 6600)
            {
                a = 351.97690566805693F;
                b = 0.114206453784165F;
                c = -40.25366309332127F;
                x = K / 100 - 55;

                red = a + b * x + c * MathF.Log(x);
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
                green = a + b * x + c * MathF.Log(x);
            }
            else if (K >= 1000)
            {
                a = -155.25485562709179F;
                b = -0.44596950469579133F;
                c = 104.49216199393888F;
                x = K / 100 - 2;
                green = a + b * x + c * MathF.Log(x);
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

                blue = a + b * x + c * MathF.Log(x);

            }
            else
            {
                blue = 0;
            }
            red = MathF.Min(1, red / 255.0f);
            green = MathF.Min(1, green / 255.0f);
            blue = MathF.Min(1, blue / 255.0f);
            return new Colour(red, green, blue);
        }

        public Colour Mix(Colour b, float pct)
        {
            Colour a = MulScalar(1 - pct);
            b = b.MulScalar(pct);
            return a.Add(b);
        }

        public Colour MulScalar(float b) => new Colour(r * b, g * b, this.b * b);

        public Colour Add(Colour b) => new Colour(r + b.r, g + b.g, this.b + b.b);

        public Colour Sub(Colour b) => new Colour(r - b.r, g - b.g, this.b - b.b);

        public Colour Mul(Colour b) => new Colour(r * b.r, g * b.g, this.b * b.b);

        public Colour Div(Colour b) => new Colour(r / b.r, g / b.g, this.b / b.b);

        public Colour DivScalar(float b) => new Colour(r / b, g / b, this.b / b);

        public Colour Min(Colour b) => new Colour(MathF.Min(r, b.r), MathF.Min(g, b.g), MathF.Min(this.b, b.b));

        public Colour Max(Colour b) => new Colour(MathF.Max(r, b.r), MathF.Max(g, b.g), MathF.Max(this.b, b.b));

        public float MinComponent() => MathF.Min(MathF.Min(r, g), b);

        public float MaxComponent() => MathF.Max(MathF.Max(r, g), b);
    }
}
