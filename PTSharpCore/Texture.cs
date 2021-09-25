using System;
using System.Collections.Generic;
using System.Drawing;

namespace PTSharpCore
{
    class ColorTexture : ITexture
    {
        public int Width;
        public int Height;
        public Colour[] Data;
        
        internal static IDictionary<string, ITexture> textures = new Dictionary<string, ITexture>();

        ColorTexture()
        {
            Width = 0;
            Height = 0;
            Data = new Colour[] { };
        }
        
        ColorTexture(int width, int height, Colour[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        internal static ITexture GetTexture(string path)
        {
            if (textures.ContainsKey(path))
            {
                Console.WriteLine("Texture: " + path + " ... OK");
                return textures[path];
            }
            else
            {
                Console.WriteLine("Adding texture to list...");
                ITexture img = new ColorTexture().LoadTexture(path);
                textures.Add(path, img);
                return img;
            }
        }
        internal ITexture LoadTexture(String path)
        {
            Console.WriteLine("IMG: "+path);
            Bitmap image = Util.LoadImage(path);
            if (image == null)
            {
                Console.WriteLine("IMG load: FAIL");
            }
            else
            {
                Console.WriteLine("IMG load: OK ");
            }
            return NewTexture(image);
        }
                
        ITexture NewTexture(Bitmap image)
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = image.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.Left, (int)boundsF.Top, (int)boundsF.Width, (int)boundsF.Height);

            int yMax = (int)boundsF.Height;
            int xMax = (int)boundsF.Width;

            Colour[] imgdata = new Colour[xMax * yMax];
            
            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    int index = y * xMax + x;
                    System.Drawing.Color pixelcolor = image.GetPixel(x, y);
                    imgdata[index] = new Colour((double)(pixelcolor.R)  / 255, (double)(pixelcolor.G) / 255, (double)(pixelcolor.B) / 255).Pow(2.2); 
                }
            }
            return new ColorTexture(xMax, yMax, imgdata);
        }
        
        ITexture ITexture.Pow(double a)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = Data[i].Pow(a);
            }
            return this;
        }

        ITexture ITexture.MulScalar(double a)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = Data[i].MulScalar(a);
            }
            return this;
        }

        Colour bilinearSample(double u, double v)
        {
            if(u == 1)
            {
                u -= Util.EPS;
            }
            if(v == 1)
            {
                v -= Util.EPS;
            }
            var w = (double)Width -1;
            var h = (double)Height - 1;
            (var X, var x) = Util.Modf(u * w);
            (var Y, var y) = Util.Modf(v * h);
            var x0 = (int)X;
            var y0 = (int)Y;
            var x1 = x0 + 1;
            var y1 = y0 + 1;
            var c00 = Data[y0 * Width + x0];
            var c01 = Data[y1 * Width + x0];
            var c10 = Data[y0 * Width + x1];
            var c11 = Data[y1 * Width + x1];
            var c = Colour.Black;
            c = c.Add(c00.MulScalar((1 - x) * (1 - y)));
            c = c.Add(c10.MulScalar(x * (1 - y)));
            c = c.Add(c01.MulScalar((1 - x) * y));
            c = c.Add(c11.MulScalar(x * y));
            return c;
        }

        double Fract(double x)
        {
            x = Util.Modf(x).Item2;
            return x;
        }

        Colour ITexture.Sample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            return bilinearSample(u, 1 - v);
        }

        Vector ITexture.NormalSample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            var c = bilinearSample(u, 1 - v);
            return new Vector(c.r * 2 - 1, c.g * 2 - 1, c.b * 2 - 1).Normalize();
        }

        Vector ITexture.BumpSample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            v = 1 - v;
            int x = (int)(u * Width);
            int y = (int)(v * Height);
            (var x1, var x2) = (Util.ClampInt(x - 1, 0, Width - 1), Util.ClampInt(x + 1, 0, Width - 1));
            (var y1, var y2) = (Util.ClampInt(y - 1, 0, Height - 1), Util.ClampInt(y + 1, 0, Height - 1));
            Colour cx = Data[y * Width + x1].Sub(Data[y * Width + x2]);
            Colour cy = Data[y1 * Width + x].Sub(Data[y2 * Width + x]);
            return new Vector(cx.r, cy.r, 0);
        }
    }
}
