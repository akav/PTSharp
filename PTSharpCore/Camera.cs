using System;

namespace PTSharpCore
{
    class Camera
    {
        IVector<double> p, u, v, w;
        double m;
        double focalDistance;
        double apertureRadius;

        public Camera() { }

        public static Camera LookAt(IVector<double> eye, IVector<double> center, IVector<double> up, double fovy)
        {
            Camera c = new Camera();
            c.p = eye;
            c.w = center.Sub(eye).Normalize();
            c.u = up.Cross(c.w).Normalize();
            c.v = c.w.Cross(c.u).Normalize();
            c.m = 1 / Math.Tan(fovy * Math.PI / 360);
            return c;
        }
        
        public void SetFocus(IVector<double> focalPoint, double apertureRadius)
        {
            focalDistance = focalPoint.Sub(p).Length();
            this.apertureRadius = apertureRadius;
        }
        
        public Ray CastRay(int x, int y, int w, int h, double u, double v, Random rand)
        {
            double aspect = (double)w / (double)h;
            var px = (((double)x + u - 0.5D) / ((double)w - 1D)) * 2D - 1D;
            var py = (((double)y + v - 0.5D) / ((double)h - 1D)) * 2D - 1D;
            IVector<double> d = new IVector<double>();
            d = d.Add(this.u.MulScalar(-px * aspect));
            d = d.Add(this.v.MulScalar(-py));
            d = d.Add(this.w.MulScalar(m));
            d = d.Normalize();
            var p = this.p;
            if (apertureRadius > 0)
            {
                var focalPoint = this.p.Add(d.MulScalar(focalDistance));
                var angle = Random.Shared.NextDouble() * 2 * Math.PI;
                var radius = Random.Shared.NextDouble() * apertureRadius;
                p = p.Add(this.u.MulScalar(Math.Cos(angle) * radius));
                p = p.Add(this.v.MulScalar(Math.Sin(angle) * radius));
                d = focalPoint.Sub(p).Normalize();
            }
            return new Ray(p, d);
        }
    }
}
