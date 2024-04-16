using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Matrix
    {
        public double M11, M12, M13, M14;
        public double M21, M22, M23, M24;
        public double M31, M32, M33, M34;
        public double M41, M42, M43, M44;

        public Matrix(double x00, double x01, double x02, double x03,
                      double x10, double x11, double x12, double x13,
                      double x20, double x21, double x22, double x23,
                      double x30, double x31, double x32, double x33)
        {
            this.M11 = x00; this.M12 = x01; this.M13 = x02; this.M14 = x03;
            this.M21 = x10; this.M22 = x11; this.M23 = x12; this.M24 = x13;
            this.M31 = x20; this.M32 = x21; this.M33 = x22; this.M34 = x23;
            this.M41 = x30; this.M42 = x31; this.M43 = x32; this.M44 = x33;
        }

        public Matrix() {}

        internal static Matrix Identity = new Matrix(1, 0, 0, 0,
                                                     0, 1, 0, 0,
                                                     0, 0, 1, 0,
                                                     0, 0, 0, 1);       

        internal Matrix Translate(Vector v) => new Matrix(1, 0, 0, v.X,
            0, 1, 0, v.Y,
            0, 0, 1, v.Z,
            0, 0, 0, 1);       

        internal Matrix Scale(Vector v) => new Matrix(v.X, 0, 0, 0,
            0, v.Y, 0, 0,
            0, 0, v.Z, 0,
            0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Rotate(Vector v, double a)
        {
            v = v.Normalize();
            var s = Math.Sin(a);
            var c = Math.Cos(a);
            var m = 1 - c;
            return new Matrix(m * v.X * v.X + c, m * v.X * v.Y + v.Z * s, m * v.Z * v.X - v.Y * s, 0,
                              m * v.X * v.Y - v.Z * s, m * v.Y * v.Y + c, m * v.Y * v.Z + v.X * s, 0,
                              m * v.Z * v.X + v.Y * s, m * v.Y * v.Z - v.X * s, m * v.Z * v.Z + c, 0,
                              0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Frustum(double l, double r, double b, double t, double n, double f)
        {
            double t1 = 2 * n;
            double t2 = r - l;
            double t3 = t - b;
            double t4 = f - n;
            return new Matrix(t1 / t2, 0, (r + l) / t2, 0,
                              0, t1 / t3, (t + b) / t3, 0,
                              0, 0, (-f - n) / t4, (-t1 * f) / t4,
                              0, 0, -1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Orthographic(double l, double r, double b, double t, double n, double f)
        {
            return new Matrix(2 / (r - l), 0, 0, -(r + l) / (r - l),
                              0, 2 / (t - b), 0, -(t + b) / (t - b),
                              0, 0, -2 / (f - n), -(f + n) / (f - n),
                              0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Perspective(double fovy, double aspect, double near, double far)
        {
            double ymax = near * Math.Tan(fovy * Math.PI / 360);
            double xmax = ymax * aspect;
            return Frustum(-xmax, xmax, -ymax, ymax, near, far);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix LookAtMatrix(Vector eye, Vector center, Vector up)
        {
            up = up.Normalize();
            var f = center.Sub(eye).Normalize();
            var s = f.Cross(up).Normalize();
            var u = s.Cross(f);

            var m = new Matrix(s.X, u.X, f.X, 0,
                               s.Y, u.Y, f.Y, 0,
                               s.Z, u.Z, f.Z, 0,
                               0, 0, 0, 1);

            return m.Transpose().Inverse().Translate(m, eye);
        }


        internal Matrix Translate(Matrix m, Vector v) => new Matrix().Translate(v).Mul(m);
                
        public Matrix Scale(Matrix m, Vector v) => Scale(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Rotate(Matrix m, Vector v, double a) => Rotate(v, a).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Mul(Matrix b)
        {
            Matrix m = new Matrix();
            m.M11 = M11 * b.M11 + M12 * b.M21 + M13 * b.M31 + M14 * b.M41;
            m.M21 = M21 * b.M11 + M22 * b.M21 + M23 * b.M31 + M24 * b.M41;
            m.M31 = M31 * b.M11 + M32 * b.M21 + M33 * b.M31 + M34 * b.M41;
            m.M41 = M41 * b.M11 + M42 * b.M21 + M43 * b.M31 + M44 * b.M41;
            m.M12 = M11 * b.M12 + M12 * b.M22 + M13 * b.M32 + M14 * b.M42;
            m.M22 = M21 * b.M12 + M22 * b.M22 + M23 * b.M32 + M24 * b.M42;
            m.M32 = M31 * b.M12 + M32 * b.M22 + M33 * b.M32 + M34 * b.M42;
            m.M42 = M41 * b.M12 + M42 * b.M22 + M43 * b.M32 + M44 * b.M42;
            m.M13 = M11 * b.M13 + M12 * b.M23 + M13 * b.M33 + M14 * b.M43;
            m.M23 = M21 * b.M13 + M22 * b.M23 + M23 * b.M33 + M24 * b.M43;
            m.M33 = M31 * b.M13 + M32 * b.M23 + M33 * b.M33 + M34 * b.M43;
            m.M43 = M41 * b.M13 + M42 * b.M23 + M43 * b.M33 + M44 * b.M43;
            m.M14 = M11 * b.M14 + M12 * b.M24 + M13 * b.M34 + M14 * b.M44;
            m.M24 = M21 * b.M14 + M22 * b.M24 + M23 * b.M34 + M24 * b.M44;
            m.M34 = M31 * b.M14 + M32 * b.M24 + M33 * b.M34 + M34 * b.M44;
            m.M44 = M41 * b.M14 + M42 * b.M24 + M43 * b.M34 + M44 * b.M44;
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulPosition(Vector b)
        {
            var x = M11 * b.X + M12 * b.Y + M13 * b.Z + M14;
            var y = M21 * b.X + M22 * b.Y + M23 * b.Z + M24;
            var z = M31 * b.X + M32 * b.Y + M33 * b.Z + M34;

            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulDirection(Vector b)
        {
            var x = M11 * b.X + M12 * b.Y + M13 * b.Z;
            var y = M21 * b.X + M22 * b.Y + M23 * b.Z;
            var z = M31 * b.X + M32 * b.Y + M33 * b.Z;
            return new Vector(x, y, z).Normalize();
        }


        public Ray MulRay(Ray b) => new Ray(MulPosition(b.Origin), MulDirection(b.Direction));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box MulBox(Box box)
        {
            var r = new Vector(M11, M21, M31);
            var u = new Vector(M12, M22, M32);
            var b = new Vector(M13, M23, M33);
            var t = new Vector(M14, M24, M34);
        
            (var xa, var xb) = ((r.MulScalar(box.Min.X)), (r.MulScalar(box.Max.X)));
            (var ya, var yb) = ((u.MulScalar(box.Min.Y)), (u.MulScalar(box.Max.Y)));
            (var za, var zb) = ((b.MulScalar(box.Min.Z)), (b.MulScalar(box.Max.Z)));
            (xa, xb) = (xa.Min(xb), xa.Max(xb));
            (ya, yb) = (ya.Min(yb), ya.Max(yb));
            (za, zb) = (za.Min(zb), za.Max(zb));
            var min = xa.Add(ya).Add(za).Add(t);
            var max = xb.Add(yb).Add(zb).Add(t);
            return new Box(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Transpose() => new Matrix(M11, M21, M31, M41, M12, M22, M32, M42, M13, M23, M33, M43, M14, M24, M34, M44);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {
            return (M11 * M22 * M33 * M44 - M11 * M22 * M34 * M43 +
                    M11 * M23 * M34 * M42 - M11 * M23 * M32 * M44 +
                    M11 * M24 * M32 * M43 - M11 * M24 * M33 * M42 -
                    M12 * M23 * M34 * M41 + M12 * M23 * M31 * M44 -
                    M12 * M24 * M31 * M43 + M12 * M24 * M33 * M41 -
                    M12 * M21 * M33 * M44 + M12 * M21 * M34 * M43 +
                    M13 * M24 * M31 * M42 - M13 * M24 * M32 * M41 +
                    M13 * M21 * M32 * M44 - M13 * M21 * M34 * M42 +
                    M13 * M22 * M34 * M41 - M13 * M22 * M31 * M44 -
                    M14 * M21 * M32 * M43 + M14 * M21 * M33 * M42 -
                    M14 * M22 * M33 * M41 + M14 * M22 * M31 * M43 -
                    M14 * M23 * M31 * M42 + M14 * M23 * M32 * M41);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Inverse()
        {
            Matrix m = new Matrix();
            double d = Determinant();
            m.M11 = (M23 * M34 * M42 - M24 * M33 * M42 + M24 * M32 * M43 - M22 * M34 * M43 - M23 * M32 * M44 + M22 * M33 * M44) / d;
            m.M12 = (M14 * M33 * M42 - M13 * M34 * M42 - M14 * M32 * M43 + M12 * M34 * M43 + M13 * M32 * M44 - M12 * M33 * M44) / d;
            m.M13 = (M13 * M24 * M42 - M14 * M23 * M42 + M14 * M22 * M43 - M12 * M24 * M43 - M13 * M22 * M44 + M12 * M23 * M44) / d;
            m.M14 = (M14 * M23 * M32 - M13 * M24 * M32 - M14 * M22 * M33 + M12 * M24 * M33 + M13 * M22 * M34 - M12 * M23 * M34) / d;
            m.M21 = (M24 * M33 * M41 - M23 * M34 * M41 - M24 * M31 * M43 + M21 * M34 * M43 + M23 * M31 * M44 - M21 * M33 * M44) / d;
            m.M22 = (M13 * M34 * M41 - M14 * M33 * M41 + M14 * M31 * M43 - M11 * M34 * M43 - M13 * M31 * M44 + M11 * M33 * M44) / d;
            m.M23 = (M14 * M23 * M41 - M13 * M24 * M41 - M14 * M21 * M43 + M11 * M24 * M43 + M13 * M21 * M44 - M11 * M23 * M44) / d;
            m.M24 = (M13 * M24 * M31 - M14 * M23 * M31 + M14 * M21 * M33 - M11 * M24 * M33 - M13 * M21 * M34 + M11 * M23 * M34) / d;
            m.M31 = (M22 * M34 * M41 - M24 * M32 * M41 + M24 * M31 * M42 - M21 * M34 * M42 - M22 * M31 * M44 + M21 * M32 * M44) / d;
            m.M32 = (M14 * M32 * M41 - M12 * M34 * M41 - M14 * M31 * M42 + M11 * M34 * M42 + M12 * M31 * M44 - M11 * M32 * M44) / d;
            m.M33 = (M12 * M24 * M41 - M14 * M22 * M41 + M14 * M21 * M42 - M11 * M24 * M42 - M12 * M21 * M44 + M11 * M22 * M44) / d;
            m.M34 = (M14 * M22 * M31 - M12 * M24 * M31 - M14 * M21 * M32 + M11 * M24 * M32 + M12 * M21 * M34 - M11 * M22 * M34) / d;
            m.M41 = (M23 * M32 * M41 - M22 * M33 * M41 - M23 * M31 * M42 + M21 * M33 * M42 + M22 * M31 * M43 - M21 * M32 * M43) / d;
            m.M42 = (M12 * M33 * M41 - M13 * M32 * M41 + M13 * M31 * M42 - M11 * M33 * M42 - M12 * M31 * M43 + M11 * M32 * M43) / d;
            m.M43 = (M13 * M22 * M41 - M12 * M23 * M41 - M13 * M21 * M42 + M11 * M23 * M42 + M12 * M21 * M43 - M11 * M22 * M43) / d;
            m.M44 = (M12 * M23 * M31 - M13 * M22 * M31 + M13 * M21 * M32 - M11 * M23 * M32 - M12 * M21 * M33 + M11 * M22 * M33) / d;
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector Transform(Vector position, Matrix matrix)
        {
            // Multiply the position by the transformation matrix to compute the transformed position
            var x = position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41;
            var y = position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42;
            var z = position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43;
            var w = position.X * matrix.M14 + position.Y * matrix.M24 + position.Z * matrix.M34 + matrix.M44;

            // Return the transformed position, dividing by the fourth component if necessary
            return new Vector(x / w, y / w, z / w);
        }
    }
};
