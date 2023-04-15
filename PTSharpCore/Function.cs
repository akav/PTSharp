using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    interface Func : IShape
    {
        double func(double x, double y);
    }
    
    class Function : Func
    {
        Func Funct;
        Box Box;
        Material Material;
        public Colour Color { get; set; }
        
        Function() {}

        Function(Func Function, Box Box, Material Material)
        {
            this.Funct = Function;
            this.Box = Box;
            this.Material = Material;
        }
        
        static IShape NewFunction(Func function, Box box, Material material)
        {
            return new Function(function, box, material);
        }
        
        void IShape.Compile() { }

        Box GetBoundingBox()
        {
            return this.Box;
        }

        bool Contains(Vector v)
        {
            return v.Z < func(v.X, v.Y);
        }

        Hit IShape.Intersect(Ray ray)
        {
            double step = 1.0 / 32;
            bool sign = Contains(ray.Position(step));
            for (double t = step; t < 12; t += step)
            {
                Vector v = ray.Position(t);
                if (Contains(v) != sign && Box.Contains(v))
                {
                    return new Hit(this, t - step, null);
                }
            }
            return Hit.NoHit;
        }

        Vector IShape.UVector(Vector p)
        {
            double x1 = Box.Min.X;
            double x2 = Box.Max.X;
            double y1 = Box.Min.Y;
            double y2 = Box.Max.Y;
            double u = p.X - x1 / x2 - x1;
            double v = p.Y - y1 / y2 - y1;
            return new Vector(u, v, 0);
        }

        Material IShape.MaterialAt(Vector p)
        {
            return Material;
        }

        Vector IShape.NormalAt(Vector p)
        {
            double eps = 1e-3F;
            double x = func(p.X - eps, p.Y) - func(p.X + eps, p.Y);
            double y = func(p.X, p.Y - eps) - func(p.X, p.Y + eps);
            double z = 2 * eps;
            Vector v = new Vector(x, y, z);
            return v.Normalize();
        }

        public double func(double x, double y)
        {
            return Funct.func(x, y);
        }

        public Box BoundingBox()
        {
            return Funct.BoundingBox();
        }

        double Func.func(double x, double y)
        {
            throw new NotImplementedException();
        }

        Box IShape.BoundingBox()
        {
            throw new NotImplementedException();
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
    }
}
