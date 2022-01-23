using System;
using System.Collections.Generic;
using System.Drawing;

namespace PTSharpCore
{
    class Util
    {
        public static float INF = float.PositiveInfinity;
        public static float EPS = float.Epsilon;
        
        public static float Radians(float degrees) => degrees * MathF.PI / 180;
        
        public static float Degrees(float radians) => radians * 180 / MathF.PI;
        
        public static V Cone(V direction, float theta, float u, float v)
        {
            if (theta < Util.EPS)
            {
                return direction;
            }
            theta = theta * (1 - (2 * MathF.Acos(u) / MathF.PI));
            var m1 = MathF.Sin(theta);
            var m2 = MathF.Cos(theta);
            var a = v * 2 * MathF.PI;
            var q = V.RandomUnitVector();
            var s = direction.Cross(q);
            var t = direction.Cross(s);
            var d = new V();
            d = d.Add(s.MulScalar(m1 * MathF.Cos(a)));
            d = d.Add(t.MulScalar(m1 * MathF.Sin(a)));
            d = d.Add(direction.MulScalar(m2));
            d = d.Normalize();
            return d;
        }
        
        public static Mesh CreateMesh(Material material)
        {
            var mesh = STL.Load("models/cylinder.stl", material);
            mesh.FitInside(new Box(new V(-0.1F, -0.1F, 0), new V(1.1F, 1.1F, 100)), new V(0.5F, 0.5F, 0));
            mesh.SmoothNormalsThreshold(Radians(10));
            return mesh;
        }

        public static Mesh CreateCubeMesh(Material material)
        {
            var mesh = STL.LoadSTLB("models/cube.stl", material);
            mesh.FitInside(new Box(new V(0, 0, 0), new V(1, 1, 1)), new V(0.5F, 0.5F, 0.5F));
            return mesh;
        }

        public static Mesh CreateBrick(int color)
        {
            var material = Material.GlossyMaterial(Colour.HexColor(color), 1.3F, Radians(20));
            var mesh = STL.Load("models/toybrick.stl", material);
	        mesh.SmoothNormalsThreshold(Radians(20));
            mesh.FitInside(new Box(new V(), new V(2, 4, 10)), new V ( 0, 0, 0 ));
	        return mesh;
        }
        public static Bitmap LoadImage(String path)
        {
            try
            {
                Bitmap image1 = new Bitmap(path); 
                return image1;
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("There was an error opening the bitmap." +
                    "Please check the path.");
                return null;
            }
        }
        
        void SavePNG(String path, Bitmap bitmap)
        {
            try
            {
                if(bitmap != null)
                bitmap.Save(path);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine("There was an error writing image to file..." +
                    "Please check the path.");
            }
        }
        
        internal static float Median(float[] items)
        {
            var n = items.Length;
            if (n == 0)
            {

                return 0;
            } else if (n%2 == 1)
            {
                return items[items.Length / 2];
            } else { 
                var a = items[items.Length / 2 - 1];
                var b = items[items.Length / 2];
               return (a + b) / 2;
            }
        }

        internal static (int, float) Modf(float input)
        {
            int dec = Convert.ToInt32(MathF.Truncate(input));
            var frac = input - MathF.Truncate(input);
            return (dec, frac);
        }

        internal static float Fract(float x)
        {
            float ret = x - MathF.Truncate(x);
            return x;
        }
        
        internal static float Clamp(float x, float lo, float hi)
        {
            if (x < lo)
                return lo;
            if (x > hi)
                return hi;
            return x;
        }
        
        internal static int ClampInt(int x, int lo, int hi)
        {
            if (x < lo)
                return lo;
            if (x > hi)
                return hi;
            return x;
        }

        internal static String NumberString(float x)
        {
            return x.ToString();
        }
        
        float[] ParseFloats(String[] items)
        {
            List<float> result = new List<float>(items.Length);
            foreach(String item in items)
            {
                float f = float.Parse(item);
                result.Add(f);
            }
            return result.ToArray();
        }
        
        int[] ParseInts(String[] items)
        {
            List<int> result = new List<int>(items.Length);
            foreach (String item in items)
            {
                int f = int.Parse(item);
                result.Add(f);
            }
            return result.ToArray();
        }
    }
}
