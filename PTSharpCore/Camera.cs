using System;

namespace PTSharpCore
{
    class Camera
    {
        public Vector p, u, v, w;
        public double m;
        public double focalDistance;
        public double apertureRadius;
        public double fovy;
        public double theta;


        public Camera() { }

        public static Camera LookAt(Vector eye, Vector center, Vector up, double fovy)
        {
            Camera c = new Camera();
            c.fovy = fovy;
            c.theta = fovy * Math.PI / 180;
            c.p = eye;
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

        public Ray CastRay(int x, int y, int w, int h, double u, double v)
        {
            double aspect = (double)w / (double)h;
            var px = (((double)x + u - 0.5f) / ((double)w - 1)) * 2 - 1;
            var py = (((double)y + v - 0.5f) / ((double)h - 1)) * 2 - 1;
            Vector d = new Vector();
            d = d.Add(this.u.MulScalar(-px * aspect));
            d = d.Add(this.v.MulScalar(-py));
            d = d.Add(this.w.MulScalar(m));
            d = d.Normalize();
            var p = this.p;
            if (apertureRadius > 0.0F)
            {
                var focalPoint = this.p.Add(d.MulScalar(focalDistance));
                var angle = Random.Shared.NextDouble() * 2.0f * Math.PI;
                var radius = Random.Shared.NextDouble() * apertureRadius;
                p = p.Add(this.u.MulScalar(Math.Cos(angle) * radius));
                p = p.Add(this.v.MulScalar(Math.Sin(angle) * radius));
                d = focalPoint.Sub(p).Normalize();
            }
            return new Ray(p, d);
        }
    }
}
