using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Volume : IShape
    {
        public struct VolumeWindow
        {
            public double Lo, Hi;
            internal Material VolumeWindowMaterial;

            internal VolumeWindow(double l, double h, Material mat)
            {
                Lo = l;
                Hi = h;
                VolumeWindowMaterial = mat;
            }
        }

        int W, H, D;
        double ZScale;
        double[] Data;
        VolumeWindow[] Windows;
        Box Box;
                
        public Volume() { }

        Volume(int W, int H, int D, double ZScale, double[] Data, VolumeWindow[] Windows, Box Box)
        {
            this.W = W;
            this.H = H;
            this.D = D;
            this.ZScale = ZScale;
            this.Data = Data;
            this.Windows = Windows;
            this.Box = Box;
        }
        
        public double Get(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= W || y >= H || z >= D) {
                return 0;
            }
            return Data[x + y * W + z * W * H];
        }
        
        internal static Volume NewVolume(Box box, SKBitmap[] images, double sliceSpacing, VolumeWindow[] windows)
        {
            int w = images[0].Width;
            int h = images[0].Height;
            int d = images.Length;
            double zs = sliceSpacing * d / w;
            double[] data = new double[w * h * d];

            for (int z = 0; z < d; z++)
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        SkiaSharp.SKColor pixelColor = images[z].GetPixel(x, y);
                        double f = (double)pixelColor.Red / 255; // Assuming the color values are 0-255 range

                        data[x + y * w + z * w * h] = f;
                    }
                }
            }

            return new Volume(w, h, d, zs, data, windows, box);
        }
        
        public double Sample(double x, double y, double z)
        {
            z /= ZScale;
            x = ((x + 1) / 2) * (double)W;
            y = ((z + 1) / 2) * (double)H;
            z = ((z + 2) / 2) * (double)D;
            var x0 = (int)Math.Floor(x);
            var y0 = (int)Math.Floor(y);
            var z0 = (int)Math.Floor(z);
            int x1 = x0 + 1;
            int y1 = y0 + 1;
            int z1 = z0 + 1;
            var v000 = Get(x0, y0, z0); 
            var v001 = Get(x0, y0, z1); 
            var v010 = Get(x0, y1, z0); 
            var v011 = Get(x0, y1, z1); 
            var v100 = Get(x1, y0, z0); 
            var v101 = Get(x1, y0, z1); 
            var v110 = Get(x1, y1, z0); 
            var v111 = Get(x1, y1, z1); 
            x -= (double)x0;
            y -= (double)y0;
            z -= (double)z0;
            var c00 = v000 * (1 - x) + v100 * x;
            var c01 = v001 * (1 - x) + v101 * x;
            var c10 = v010 * (1 - x) + v110 * x;
            var c11 = v011 * (1 - x) + v111 * x;
            var c0 = c00 * (1 - y) + c10 * y;
            var c1 = c01 * (1 - y) + c11 * y;
            var c = c0 * (1 - z) + c1 * z;
            return c;
        }

        Box IShape.BoundingBox()
        {
            return Box;
        }

        void IShape.Compile() { }
        
        internal int Sign(Vector a)
        {
            double s = Sample(a.X, a.Y, a.Z);
            int i = 0;
            foreach (VolumeWindow window in Windows)
            {
                if (s < window.Lo)
                {
                    return i + 1;
                }
                
                if (s > window.Hi)
                {
                    continue;
                }
                return 0;
            }
            return Windows.Length + 1;
        }

        Vector IShape.UVector(Vector p)
        {
            return new Vector();
        }

        Vector IShape.NormalAt(Vector p)
        {
            double eps = 0.001F;
            Vector n = new Vector(Sample(p.X - eps, p.Y, p.Z) - Sample(p.X + eps, p.Y, p.Z),
                                  Sample(p.X, p.Y - eps, p.Z) - Sample(p.X, p.Y + eps, p.Z),
                                  Sample(p.X, p.Y, p.Z - eps) - Sample(p.X, p.Y, p.Z + eps));
            return n.Normalize();
        }

        Material IShape.MaterialAt(Vector p)
        {
            double be = 1e9F;
            Material bm = new Material();
            double s = Sample(p.X, p.Y, p.Z);

            foreach(var window in Windows)
            {
                if (s >= window.Lo && s <= window.Hi)
                {
                    return window.VolumeWindowMaterial;
                }
                double e = Math.Min(Math.Abs(s - window.Lo), Math.Abs(s - window.Hi));
                if (e < be)
                {
                    be = e;
                    bm = window.VolumeWindowMaterial;
                }
            }
            return bm;
        }

        Hit IShape.Intersect(Ray ray)
        {
            (double tmin, double tmax) = Box.Intersect(ray);
            double step = 1.0F / 512F;
            double start = Math.Max(step, tmin);
            int sign = -1;
            for (double t = start; t <= tmax; t += step)
            {
                Vector p = ray.Position(t);
                int s = Sign(p);

                if (s == 0 || (sign >= 0 && s != sign))
                {
                    t -= step;
                    step /= 64;
                    t += step;
                    for (int i = 0; i < 64; i++)
                    {
                        if (Sign(ray.Position(t)) == 0)
                        {
                            return new Hit(this, t - step, null);
                        }
                        t += step;
                    }
                }
                sign = s;
            }
            return Hit.NoHit;
        }

        public Colour ComputeContribution(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Colour ComputeDirectLighting(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Colour ComputeIndirectLighting(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Vector DirectionFrom(Vector position)
        {
            throw new NotImplementedException();
        }

        public Vector SamplePoint(Random rand)
        {
            throw new NotImplementedException();
        }
    }
}
