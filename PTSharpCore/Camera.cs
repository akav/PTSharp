using MathNet.Numerics.Random;
using MathNet.Numerics.RootFinding;
using Silk.NET.Input;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]

    public struct Camera
    {
        public Vector p, u, v, w;
        public double m;
        public double focalDistance;
        public double apertureRadius;
        public double fovy;
        public double theta;
        public static Vector Position;
        public Camera() { }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public void SetFocus(Vector focalPoint, double apertureRadius)
        {
            focalDistance = focalPoint.Sub(p).Length();
            this.apertureRadius = apertureRadius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Vector GetPixelPosition(int x, int y, int Width, int Height)
        {
            Vector pixelPosition = new Vector((x + 0.5f) / Width - 0.5f, (y + 0.5f) / Height - 0.5f, 0);
            return pixelPosition;
        }

        public Vector GetCameraPosition(int x, int y, int Width, int Height)
        {
            Vector pixelPosition = GetPixelPosition(x, y, Width, Height);
            Vector cameraPosition = pixelPosition.X * u + pixelPosition.Y * v + m * w;
            return cameraPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Ray GenerateRay(int x, int y, int Width, int Height)
        {
            // Calculate the pixel position in screen space
            Vector pixelPosition = new Vector((x + 0.5f) / Width - 0.5f, (y + 0.5f) / Height - 0.5f, 0);

            // Transform the pixel position to camera space
            Vector cameraPosition = pixelPosition.X * u + pixelPosition.Y * v + m * w;

            // Calculate the direction of the ray in camera space
            Vector rayDirection = (cameraPosition - p).Normalize();

            // Create and return a new ray
            return new Ray(p, rayDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Ray GenerateRandomRay(int x, int y, int width, int height)
        {
            // Generate a random point on the image plane
            double u = (2.0 * x / (double)width) - 1.0;
            double v = (2.0 * y / (double)height) - 1.0;

            // Generate a random point on the lens
            double r = apertureRadius * Math.Sqrt(Random.Shared.NextDouble());
            double theta = 2.0 * Math.PI * Random.Shared.NextDouble();
            Vector pointOnLens = p + r * (Math.Cos(theta) * u + Math.Sin(theta) * v);

            // Calculate the direction of the ray from the camera to the point on the lens
            Vector direction = (pointOnLens - p).Normalize();

            return new Ray(pointOnLens, direction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public Ray CastRay(int x, int y, int w, int h, double u, double v, Random rand)
        {
            double aspect = w / (double)h;
            var px = ((x + u - 0.5) / (w - 1.0)) * 2 - 1;
            var py = ((y + v - 0.5) / (h - 1.0)) * 2 - 1;
            
            Vector d = new Vector().Add(this.u.MulScalar(-px * aspect)).Add(this.v.MulScalar(-py)).Add(this.w.MulScalar(m)).Normalize();

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
