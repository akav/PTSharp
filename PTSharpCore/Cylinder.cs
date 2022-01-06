using System;

namespace PTSharpCore
{
    public class Cylinder : IShape
    {
        double Radius;
        double Z0, Z1;
        Material CylinderMaterial;

        Cylinder(double radius, double z0, double z1, Material material)
        {
            Radius = radius;
            Z0 = z0;
            Z1 = z1;
            CylinderMaterial = material;
        }

        internal static Cylinder NewCylinder(double radius, double z0, double z1, Material material) => new Cylinder(radius, z0, z1, material);

        internal static IShape NewTransformedCylinder(IVector<double> v0, IVector<double> v1, double radius, Material material)
        {
            var up = new IVector<double>(new double[] { 0, 0, 1, 0 });
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
            return new Box(new IVector<double>(new double[] { -r, -r, Z0, 0 } ), new IVector<double>(new double[] { r, r, Z1, 0 }));
        }

        Hit IShape.Intersect(Ray ray)
        {
            var r = Radius;
            var o = ray.Origin;
            var d = ray.Direction;
            var a = (d.dv[0] * d.dv[0]) + (d.dv[1] * d.dv[1]);
            var b = (2 * o.dv[0] * d.dv[0]) + (2 * o.dv[1] * d.dv[1]);
            var c = (o.dv[0] * o.dv[0]) + (o.dv[1] * o.dv[1]) - (r * r);
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
            var z0 = o.dv[2] + t0 * d.dv[2];
            var z1 = o.dv[2] + t1 * d.dv[2];
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

        IVector<double> IShape.UV(IVector<double> p) => new IVector<double>();

        Material IShape.MaterialAt(IVector<double> p) => CylinderMaterial;

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            //p.dv[2] = 0;
            //return p.Normalize();

            IVector<double> np = new IVector<double>(new double[] { p.dv[0], p.dv[1], 0, 0 });
            return np.Normalize();
        }

        void IShape.Compile() { }
    }
}
