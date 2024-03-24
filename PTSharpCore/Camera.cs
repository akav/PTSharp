using ILGPU.Algorithms.Random;
using System;

namespace PTSharpCore
{
    public class Camera
    {
        public Vector p, u, v, w;
        public double m;
        public double focalDistance;
        public double apertureRadius;
        public double fovy;
        public double theta;
        public static Vector Position;
        public Camera() { }

        public static Camera LookAt(Vector eye, Vector center, Vector up, double fovy)
        {
            Camera c = new Camera();
            c.fovy = fovy;
            c.theta = fovy * Math.PI / 180;
            c.p = eye;
            Position = c.p;
            c.w = center.Sub(eye).Normalize();
            c.u = up.Cross(c.w).Normalize();
            c.v = c.w.Cross(c.u).Normalize();
            c.m = 1 / Math.Tan(fovy * Math.PI / 360);
            return c;
        }

        public void SetFocus(Vector focalPoint, double apertureRadius)
        {
            focalDistance = focalPoint.Sub(p).Length();
            this.apertureRadius = apertureRadius;
        }

        /*
        public Ray CastRay(int x, int y, int w, int h, double u, double v, Random rand)
        {
            double aspect = (double)w / h;
            double px = ((x + u) / (w - 1)) * 2 - 1;
            double py = ((y + v) / (h - 1)) * 2 - 1;

            Vector d = new Vector(
                -this.u.X * px * aspect - this.v.X * py + this.w.X * m,
                -this.u.Y * px * aspect - this.v.Y * py + this.w.Y * m,
                -this.u.Z * px * aspect - this.v.Z * py + this.w.Z * m
            ).Normalize();

            Vector p = this.p;

            if (apertureRadius > 0)
            {
                Vector focalPoint = p + d * this.focalDistance;
                double angle = rand.NextDouble() * 2 * Math.PI;
                double radius = Math.Sqrt(rand.NextDouble()) * this.apertureRadius; // Using sqrt to ensure even distribution
                double cosAngle = Math.Cos(angle);
                double sinAngle = Math.Sin(angle);

                // Perturb the origin point towards a random point within the aperture
                p += this.u * (cosAngle * radius) + this.v * (sinAngle * radius);
                d = (focalPoint - p).Normalize();
            }

            return new Ray(p, d);
        }*/


        
        public Ray CastRay(int x, int y, int w, int h, double u, double v, Random rand)
        {
            double aspect = (double)w / h;
            double px = ((x + u - 0.5) / (w - 1)) * 2 - 1;
            double py = ((y + v - 0.5) / (h - 1)) * 2 - 1;

            Vector d = new Vector();
            d = d.Add(this.u.MulScalar(-px * aspect));
            d = d.Add(this.v.MulScalar(-py));
            d = d.Add(this.w.MulScalar(m));
            d = d.Normalize();

            Vector p = this.p;

            if (apertureRadius > 0)
            {
                Vector focalPoint = p.Add(d.MulScalar(this.focalDistance));
                double angle = rand.NextDouble() * 2 * Math.PI;
                double radius = rand.NextDouble() * this.apertureRadius;

                p = p.Add(this.u.MulScalar(Math.Cos(angle) * radius));
                p = p.Add(this.v.MulScalar(Math.Sin(angle) * radius));
                d = focalPoint.Sub(p).Normalize();
            }

            return new Ray(p, d);
        }
    }
}
