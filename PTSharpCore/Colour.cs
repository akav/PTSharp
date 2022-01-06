using System;

namespace PTSharpCore
{
    public class Colour
    {
        internal double r;
        internal double g;
        internal double b;

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

        public static Colour Black = new Colour(0, 0, 0);
        public static Colour White = new Colour(1, 1, 1);

        public Colour() { }

        public static Colour NewColor(int r, int g, int b) => new Colour((double)r / 65535, (double)g / 65535, (double)b / 65535);

        public static Colour HexColor(int x)
        {
            var red = Convert.ToDouble((x >> 16) & 0xff) / 255;
            var green = Convert.ToDouble((x >> 8) & 0xff) / 255;
            var blue = Convert.ToDouble(x & 0xff) / 255;
            Colour color = new Colour(red, green, blue);
            return color.Pow(2.2);
        }

        public Colour Pow(double b) => new Colour(Math.Pow(r, b), Math.Pow(g, b), Math.Pow(this.b, b));

        public int getIntFromColor(double red, double green, double blue)
        {
            if (double.IsNaN(red))
                red = 0.0D;
            
            if (double.IsNaN(green))
                green = 0.0D;
            
            if (double.IsNaN(blue))
                blue = 0.0D;

            var r = (int)(256 * Math.Clamp(red, 0.0, 0.999)); 
            var g = (int)(256 * Math.Clamp(green, 0.0, 0.999));
            var b = (int)(256 * Math.Clamp(blue, 0.0, 0.999));
            
            return 255 << 24 | r << 16 | g << 8 | b;
        }

        public int getIntFromColor64(double red, double green, double blue)
        {
            var r = (byte)Convert.ToUInt16(Math.Max(0, Math.Min(65535, red * 65535)));
            var g = (byte)Convert.ToUInt16(Math.Max(0, Math.Min(65535, green * 65535)));
            var b = (byte)Convert.ToUInt16(Math.Max(0, Math.Min(65535, blue * 65535)));
            return 65535 << 24 | r << 16 | g << 8 | b;
        }

        public static Colour Kelvin(double K)
        {
            double red, green, blue;
            double a, b, c, x;
            // red
            if (K >= 6600)
            {
                a = 351.97690566805693;
                b = 0.114206453784165;
                c = -40.25366309332127;
                x = K / 100 - 55;

                red = a + b * x + c * Math.Log(x);
            }
            else
            {
                red = 255;
            }
            if (K >= 6600)
            {
                a = 325.4494125711974;
                b = 0.07943456536662342;
                c = -28.0852963507957;
                x = K / 100 - 50;
                green = a + b * x + c * Math.Log(x);
            }
            else if (K >= 1000)
            {
                a = -155.25485562709179;
                b = -0.44596950469579133;
                c = 104.49216199393888;
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
                a = -254.76935184120902;
                b = 0.8274096064007395;
                c = 115.67994401066147;
                x = K / 100 - 10;

                blue = a + b * x + c * Math.Log(x);

            }
            else
            {
                blue = 0;
            }
            red = Math.Min(1, red / 255);
            green = Math.Min(1, green / 255);
            blue = Math.Min(1, blue / 255);
            return new Colour(red, green, blue);
        }

        public Colour Mix(Colour b, double pct)
        {
            Colour a = MulScalar(1 - pct);
            b = b.MulScalar(pct);
            return a.Add(b);
        }

        public Colour MulScalar(double b) => new Colour(r * b, g * b, this.b * b);

        public Colour Add(Colour b) => new Colour(r + b.r, g + b.g, this.b + b.b);

        public Colour Sub(Colour b) => new Colour(r - b.r, g - b.g, this.b - b.b);

        public Colour Mul(Colour b) => new Colour(r * b.r, g * b.g, this.b * b.b);

        public Colour Div(Colour b) => new Colour(r / b.r, g / b.g, this.b / b.b);

        public Colour DivScalar(double b) => new Colour(r / b, g / b, this.b / b);

        public Colour Min(Colour b) => new Colour(Math.Min(r, b.r), Math.Min(g, b.g), Math.Min(this.b, b.b));

        public Colour Max(Colour b) => new Colour(Math.Max(r, b.r), Math.Max(g, b.g), Math.Max(this.b, b.b));

        public double MinComponent() => Math.Min(Math.Min(r, g), b);

        public double MaxComponent() => Math.Max(Math.Max(r, g), b);
    }
}
