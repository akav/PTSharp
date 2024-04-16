using System;

namespace PTSharpCore
{
    public struct Cylinder : IShape
    {
        public double Radius;
        public double Z0, Z1;
        public Material CylinderMaterial;

        public Cylinder(double radius, double z0, double z1, Material material)
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
            double r = Radius;
            Vector o = ray.Origin;
            Vector d = ray.Direction;

            // Calculate intersection with top and bottom planes
            double tTop = (Z1 - o.Z) / d.Z;
            double tBottom = (Z0 - o.Z) / d.Z;

            // Calculate intersection with lateral surface
            double a = d.X * d.X + d.Y * d.Y;
            double b = 2 * (o.X * d.X + o.Y * d.Y);
            double c = o.X * o.X + o.Y * o.Y - r * r;
            double discriminant = b * b - 4 * a * c;

            // Check if ray intersects with top surface
            if (tTop > Util.EPS && tTop > 0)
            {
                Vector intersectionTop = o + d * tTop;
                double distanceToCenterTop = Math.Sqrt(intersectionTop.X * intersectionTop.X + intersectionTop.Y * intersectionTop.Y);
                if (distanceToCenterTop <= r)
                {
                    return new Hit(this, tTop, null);
                }
            }

            // Check if ray intersects with bottom surface
            if (tBottom > Util.EPS && tBottom > 0)
            {
                Vector intersectionBottom = o + d * tBottom;
                double distanceToCenterBottom = Math.Sqrt(intersectionBottom.X * intersectionBottom.X + intersectionBottom.Y * intersectionBottom.Y);
                if (distanceToCenterBottom <= r)
                {
                    return new Hit(this, tBottom, null);
                }
            }

            // Check if ray intersects with lateral surface
            if (discriminant >= 0)
            {
                double sqrtDiscriminant = Math.Sqrt(discriminant);
                double t1 = (-b + sqrtDiscriminant) / (2 * a);
                double t2 = (-b - sqrtDiscriminant) / (2 * a);

                double tLateral = double.NaN;
                if (t1 > Util.EPS && t1 > 0)
                {
                    tLateral = t1;
                }
                else if (t2 > Util.EPS && t2 > 0)
                {
                    tLateral = t2;
                }

                if (!double.IsNaN(tLateral))
                {
                    Vector intersectionLateral = o + d * tLateral;
                    double z = intersectionLateral.Z;
                    if (z >= Z0 && z <= Z1)
                    {
                        return new Hit(this, tLateral, null);
                    }
                }
            }

            return Hit.NoHit;

        }


        Vector IShape.UVector(Vector p)
        {
            // Calculate the tangent vector based on the x and y coordinates of the given point
            return new Vector(-p.Y, p.X, 0).Normalize();
        }

        Material IShape.MaterialAt(Vector p) => CylinderMaterial;

        Vector IShape.NormalAt(Vector p)
        {
            double epsilon = 0.0001; // A small value to handle floating point imprecision

            // Check if p is on the lateral surface
            if (Math.Abs(p.Z - Z0) > epsilon && Math.Abs(p.Z - Z1) > epsilon)
            {
                // Point is on the lateral surface
                // Calculate the center of the lateral surface
                Vector center = new Vector(0, 0, (Z0 + Z1) / 2);

                // Calculate the vector from the center to the given point
                Vector toPoint = p - center;

                // Normalize the vector to get the normal vector
                Vector normal = toPoint.Normalize();

                // Check if the normal is pointing inward, if so, invert it
                if (normal.Dot(p - new Vector(0, 0, Z0)) < 0)
                {
                    normal = -normal;
                }

                return normal;
            }
            else // Point is on the top or bottom surface
            {
                if (Math.Abs(p.Z - Z0) < epsilon) // p is on the bottom surface
                {
                    return new Vector(0, 0, -1); // Normal points downward for the bottom surface
                }
                else if (Math.Abs(p.Z - Z1) < epsilon) // p is on the top surface
                {
                    return new Vector(0, 0, 1); // Normal points upward for the top surface
                }
                else
                {
                    // Default case (should not reach here)
                    return new Vector(0, 0, 0);
                }
            }
        }

        void IShape.Compile() { }        
    }
}
