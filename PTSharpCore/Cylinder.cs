using System;

namespace PTSharpCore
{
    public class Cylinder : IShape
    {
        double Radius;
        double Z0, Z1;
        Material CylinderMaterial;
        public Colour Color { get; set; }
        public Vector Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        Cylinder(double radius, double z0, double z1, Material material)
        {
            Radius = radius;
            Z0 = z0;
            Z1 = z1;
            CylinderMaterial = material;
        }

        internal static Cylinder NewCylinder(double radius, double z0, double z1, Material material) => new Cylinder(radius, z0, z1, material);

        internal static IShape NewTransformedCylinder(Vector v0, Vector v1, double radius, Material material)
        {
            var up = new Vector(0, 0, 1);
            var d = v1.Sub(v0);
            var z = d.Length();
            var a = Math.Acos(d.Normalize().Dot(up));
            var m = new Matrix().Translate(v0);
            if (a != 0)
            {
                var u = d.Cross(up).Normalize();
                m = new Matrix().Rotate(u, a).Translate(v0);
            }
            var c = NewCylinder(radius, 0, z, material);
            return TransformedShape.NewTransformedShape(c, m);
        }

        Box IShape.BoundingBox()
        {
            double r = Radius;
            return new Box(new Vector(-r, -r, Z0), new Vector(r, r, Z1));
        }

        Hit IShape.Intersect(Ray ray)
        {
            var r = Radius;
            var o = ray.Origin;
            var d = ray.Direction;
            var a = (d.X * d.X) + (d.Y * d.Y);
            var b = (2 * o.X * d.X) + (2 * o.Y * d.Y);
            var c = (o.X * o.X) + (o.Y * o.Y) - (r * r);
            var q = (b * b) - (4 * a * c);
            if (q < Util.EPS)
            {
                return Hit.NoHit;
            }
            var s = Math.Sqrt(q);
            var t0 = (-b + s) / (2 * a);
            var t1 = (-b - s) / (2 * a);
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }
            var z0 = o.Z + t0 * d.Z;
            var z1 = o.Z + t1 * d.Z;
            if (t0 > Util.EPS && Z0 < z0 && z0 < Z1)
            {
                return new Hit(this, t0, null);
            }
            if (t1 > Util.EPS && Z0 < z1 && z1 < Z1)
            {
                return new Hit(this, t1, null);
            }
            return Hit.NoHit;
        }

        Vector IShape.UV(Vector p) => new();

        Material IShape.MaterialAt(Vector p) => CylinderMaterial;

        Vector IShape.NormalAt(Vector p)
        {
            p.Z = 0.0d;
            return p.Normalize();
        }

        void IShape.Compile() { }

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
