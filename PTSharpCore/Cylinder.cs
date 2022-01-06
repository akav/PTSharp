using System;

namespace PTSharpCore
{
    public class Cylinder : IShape
    {
        float Radius;
        float Z0, Z1;
        Material CylinderMaterial;

        Cylinder(float radius, float z0, float z1, Material material)
        {
            Radius = radius;
            Z0 = z0;
            Z1 = z1;
            CylinderMaterial = material;
        }

        internal static Cylinder NewCylinder(float radius, float z0, float z1, Material material) => new Cylinder(radius, z0, z1, material);

        internal static IShape NewTransformedCylinder(V v0, V v1, float radius, Material material)
        {
            var up = new V(0, 0, 1);
            var d = v1.Sub(v0);
            var z = d.Length();
            var a = MathF.Acos(d.Normalize().Dot(up));
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
            float r = Radius;
            return new Box(new V(-r, -r, Z0), new V(r, r, Z1));
        }

        Hit IShape.Intersect(Ray ray)
        {
            var r = Radius;
            var o = ray.Origin;
            var d = ray.Direction;
            var a = (d.v.X * d.v.X) + (d.v.Y * d.v.Y);
            var b = (2 * o.v.X * d.v.X) + (2 * o.v.Y * d.v.Y);
            var c = (o.v.X * o.v.X) + (o.v.Y * o.v.Y) - (r * r);
            var q = (b * b) - (4 * a * c);
            if (q < Util.EPS)
            {
                return Hit.NoHit;
            }
            var s = MathF.Sqrt(q);
            var t0 = (-b + s) / (2 * a);
            var t1 = (-b - s) / (2 * a);
            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }
            var z0 = o.v.Z + t0 * d.v.Z;
            var z1 = o.v.Z + t1 * d.v.Z;
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

        V IShape.UV(V p) => new V();

        Material IShape.MaterialAt(V p) => CylinderMaterial;

        V IShape.NormalAt(V p)
        {
            p.v.Z = 0.0F;
            return p.Normalize();
        }

        void IShape.Compile() { }
    }
}
