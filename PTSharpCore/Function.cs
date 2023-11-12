using glTFLoader.Schema;
using System;
using TinyEmbree;

namespace PTSharpCore
{
    public delegate double Func(double x, double y);
    class Function : IShape
    {
        public Func _Function;
        public Box Box;
        public Material Material;
        public Colour Color { get; set; }

        Function() { }

        Function(Func Function, Box Box, Material Material)
        {
            this._Function = Function;
            this.Box = Box;
            this.Material = Material;
        }

        static IShape NewFunction(Func function, Box box, Material material)
        {
            return new Function(function, box, material);
        }

        public void Compile()
        {
            // No action required
        }

        public Box BoundingBox()
        {
            return Box;
        }

        public Hit Intersect(Ray ray)
        {
            double step = 1.0 / 32;
            bool sign = Contains(ray.Position(step));
            for (double t = step; t < 12; t += step)
            {
                Vector v = ray.Position(t);
                if (Contains(v) != sign && Box.Contains(v))
                {
                    return new Hit(this, t - step, null); // Assuming Hit is a struct or class
                }
            }
            return Hit.NoHit;
        }

        bool Contains(Vector v)
        {
            return v.Z < _Function(v.X, v.Y);
        }

        public Vector UV(Vector p)
        {
            double u = (p.X - Box.Min.X) / (Box.Max.X - Box.Min.X);
            double v = (p.Y - Box.Min.Y) / (Box.Max.Y - Box.Min.Y);
            return new Vector(u, v, 0);
        }

        public Vector NormalAt(Vector p)
        {
            const double eps = 1e-3;
            Vector v = new Vector(
                _Function(p.X - eps, p.Y) - _Function(p.X + eps, p.Y),
                _Function(p.X, p.Y - eps) - _Function(p.X, p.Y + eps),
                2 * eps
            );
            return v.Normalize();
        }

        public Material MaterialAt(Vector v)
        {
            return Material;
        }
    }
}
