using System;
using System.Collections.Generic;
using System.Drawing;

namespace PTSharpCore
{
    class Util
    {
        public static double INF = 1e9;
        public static double EPS = 1e-9;

        public static double Radians(double degrees) => degrees * Math.PI / 180;
        
        public static double Degrees(double radians) => radians * 180 / Math.PI;
        
        public static Vector Cone(Vector direction, double theta, double u, double v, Random rand)
        {
            if (theta < Util.EPS)
            {
                return direction;
            }
            theta = theta * (1 - (2 * Math.Acos(u) / Math.PI));
            var m1 = Math.Sin(theta);
            var m2 = Math.Cos(theta);
            var a = v * 2 * Math.PI;
            var q = Vector.RandomUnitVector(rand);
            var s = direction.Cross(q);
            var t = direction.Cross(s);
            var d = new Vector();
            d = d.Add(s.MulScalar(m1 * Math.Cos(a)));
            d = d.Add(t.MulScalar(m1 * Math.Sin(a)));
            d = d.Add(direction.MulScalar(m2));
            d = d.Normalize();
            return d;
        }
        
        public static Mesh CreateMesh(Material material)
        {
            var mesh = STL.Load("models/cylinder.stl", material);
            mesh.FitInside(new Box(new Vector(-0.1F, -0.1F, 0), new Vector(1.1F, 1.1F, 100)), new Vector(0.5F, 0.5F, 0));
            mesh.SmoothNormalsThreshold(Radians(10));
            return mesh;
        }

        public static Mesh CreateCubeMesh(Material material)
        {
            var mesh = STL.LoadSTLB("models/cube.stl", material);
            mesh.FitInside(new Box(new Vector(0, 0, 0), new Vector(1, 1, 1)), new Vector(0.5F, 0.5F, 0.5F));
            return mesh;
        }

        public static Mesh CreateBrick(int color)
        {
            var material = Material.GlossyMaterial(Colour.HexColor(color), 1.3F, Radians(20));
            var mesh = STL.Load("models/toybrick.stl", material);
	        mesh.SmoothNormalsThreshold(Radians(20));
            mesh.FitInside(new Box(new Vector(), new Vector(2, 4, 10)), new Vector( 0, 0, 0 ));
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
        
        internal static double Median(double[] items)
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

        internal static (int, double) Modf(double input)
        {
            int dec = Convert.ToInt32(Math.Truncate(input));
            var frac = input - Math.Truncate(input);
            return (dec, frac);
        }

        internal static double Fract(double x)
        {
            double ret = x - Math.Truncate(x);
            return x;
        }
        
        internal static double Clamp(double x, double lo, double hi)
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

        internal static String NumberString(double x)
        {
            return x.ToString();
        }
        
        double[] ParseFloats(String[] items)
        {
            List<double> result = new List<double>(items.Length);
            foreach(String item in items)
            {
                double f = double.Parse(item);
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
