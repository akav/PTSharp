using System;

namespace PTSharpCore
{
    class Camera
    {
        public V p, u, v, w;
        public float m;
        public float focalDistance;
        public float apertureRadius;
        public float fovy;
        public float theta;


        public Camera() { }

        public static Camera LookAt(V eye, V center, V up, float fovy)
        {
            Camera c = new Camera();
            c.fovy = fovy;
            c.theta = fovy * MathF.PI / 180;
            c.p = eye;
            c.w = center.Sub(eye).Normalize();
            c.u = up.Cross(c.w).Normalize();
            c.v = c.w.Cross(c.u).Normalize();
            c.m = 1 / MathF.Tan(fovy * MathF.PI / 360);
            return c;
        }
        
        public void SetFocus(V focalPoint, float apertureRadius)
        {
            focalDistance = focalPoint.Sub(p).Length();
            this.apertureRadius = apertureRadius;
        }

        public Ray CastRay(int x, int y, int w, int h, float u, float v)
        {
            float aspect = (float)w / (float)h;
            var px = (((float)x + u - 0.5f) / ((float)w - 1)) * 2 - 1;
            var py = (((float)y + v - 0.5f) / ((float)h - 1)) * 2 - 1;
            V d = new V();
            d = d.Add(this.u.MulScalar(-px * aspect));
            d = d.Add(this.v.MulScalar(-py));
            d = d.Add(this.w.MulScalar(m));
            d = d.Normalize();
            var p = this.p;
            if (apertureRadius > 0.0F)
            {
                var focalPoint = this.p.Add(d.MulScalar(focalDistance));
                var angle = Random.Shared.NextSingle() * 2.0f * MathF.PI;
                var radius = Random.Shared.NextSingle() * apertureRadius;
                p = p.Add(this.u.MulScalar(MathF.Cos(angle) * radius));
                p = p.Add(this.v.MulScalar(MathF.Sin(angle) * radius));
                d = focalPoint.Sub(p).Normalize();
            }
            return new Ray(p, d);
        }
    }
}
