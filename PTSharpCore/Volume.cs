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
            public float Lo, Hi;
            internal Material VolumeWindowMaterial;

            internal VolumeWindow(float l, float h, Material mat)
            {
                Lo = l;
                Hi = h;
                VolumeWindowMaterial = mat;
            }
        }

        int W, H, D;
        float ZScale;
        float[] Data;
        VolumeWindow[] Windows;
        Box Box;

        public Volume() { }

        Volume(int W, int H, int D, float ZScale, float[] Data, VolumeWindow[] Windows, Box Box)
        {
            this.W = W;
            this.H = H;
            this.D = D;
            this.ZScale = ZScale;
            this.Data = Data;
            this.Windows = Windows;
            this.Box = Box;
        }
        
        public float Get(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= W || y >= H || z >= D) {
                return 0;
            }
            return Data[x + y * W + z * W * H];
        }
        
        internal static Volume NewVolume(Box box, Bitmap[] images, float sliceSpacing, VolumeWindow[] windows)
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = images[0].GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.Left, (int)boundsF.Top, (int)boundsF.Width, (int)boundsF.Height);
            int w = (int)boundsF.Height;
            int h = (int)boundsF.Width;
            int d = images.Length;
            float zs = (sliceSpacing * (float)d) / (float)w;
            float[] data = new float[w * h * d];
            int zval = 0;

            foreach (var image in images)
            { 
                for(int y = 0; y < h; y++)
                {
                    for(int x = 0; x < w; x++)
                    {
                        var r = image.GetPixel(x, y).R;
                        float f = (float)r / 65535;

                        data[x + y * w + zval * w * h] = f;
                    }
                }
                zval++;
            }
            return new Volume(w, h, d, zs, data, windows, box);
        }
        
        public float Sample(float x, float y, float z)
        {
            z /= ZScale;
            x = ((x + 1) / 2) * (float)W;
            y = ((z + 1) / 2) * (float)H;
            z = ((z + 2) / 2) * (float)D;
            var x0 = (int)MathF.Floor(x);
            var y0 = (int)MathF.Floor(y);
            var z0 = (int)MathF.Floor(z);
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
            x -= (float)x0;
            y -= (float)y0;
            z -= (float)z0;
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
        
        internal int Sign(V a)
        {
            float s = Sample(a.v.X, a.v.Y, a.v.Z);
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

        V IShape.UV(V p)
        {
            return new V();
        }

        V IShape.NormalAt(V p)
        {
            float eps = 0.001F;
            V n = new V(Sample(p.v.X - eps, p.v.Y, p.v.Z) - Sample(p.v.X + eps, p.v.Y, p.v.Z),
                                  Sample(p.v.X, p.v.Y - eps, p.v.Z) - Sample(p.v.X, p.v.Y + eps, p.v.Z),
                                  Sample(p.v.X, p.v.Y, p.v.Z - eps) - Sample(p.v.X, p.v.Y, p.v.Z + eps));
            return n.Normalize();
        }

        Material IShape.MaterialAt(V p)
        {
            float be = 1e9F;
            Material bm = new Material();
            float s = Sample(p.v.X, p.v.Y, p.v.Z);

            foreach(var window in Windows)
            {
                if (s >= window.Lo && s <= window.Hi)
                {
                    return window.VolumeWindowMaterial;
                }
                float e = MathF.Min(MathF.Abs(s - window.Lo), MathF.Abs(s - window.Hi));
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
            (float tmin, float tmax) = Box.Intersect(ray);
            float step = 1.0F / 512F;
            float start = Math.Max(step, tmin);
            int sign = -1;
            for (float t = start; t <= tmax; t += step)
            {
                V p = ray.Position(t);
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
    }
}
