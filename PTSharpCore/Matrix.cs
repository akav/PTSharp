using System;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace PTSharpCore
{
    struct Matrix
    {
        Matrix4x4 m;

        public Matrix(float m11, float m12, float m13, float m14,
                      float m21, float m22, float m23, float m24,
                      float m31, float m32, float m33, float m34,
                      float m41, float m42, float m43, float m44)
        {
            m = new Matrix4x4(m11, m12, m13, m14, 
                              m21, m22, m23, m24, 
                              m31, m32, m33, m34, 
                              m41, m42, m43, m44);
        }

        public Matrix(Matrix4x4 inputM)
        {
            m = inputM;
        }

        internal static Matrix Identity = new Matrix(Matrix4x4.Identity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(V v)
        {
            return new Matrix(Matrix4x4.CreateTranslation(new Vector3(v.v.X, v.v.Y, v.v.Z)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Scale(V v) => new Matrix(v.v.X, 0, 0, 0, 0, v.v.Y, 0, 0, 0, 0, v.v.Z, 0, 0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Rotate(V v, float a)
        {
            v = v.Normalize();
            var s = MathF.Sin(a);
            var c = MathF.Cos(a);
            var m = 1 - c;
            return new Matrix(m * v.v.X * v.v.X + c, m * v.v.X * v.v.Y + v.v.Z * s, m * v.v.Z * v.v.X - v.v.Y * s, 0,
                              m * v.v.X * v.v.Y - v.v.Z * s, m * v.v.Y * v.v.Y + c, m * v.v.Y * v.v.Z + v.v.X * s, 0,
                              m * v.v.Z * v.v.X + v.v.Y * s, m * v.v.Y * v.v.Z - v.v.X * s, m * v.v.Z * v.v.Z + c, 0,
                              0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Frustum(float l, float r, float b, float t, float n, float f)
        {
            float t1 = 2 * n;
            float t2 = r - l;
            float t3 = t - b;
            float t4 = f - n;
            return new Matrix(t1 / t2, 0, (r + l) / t2, 0,
                              0, t1 / t3, (t + b) / t3, 0,
                              0, 0, (-f - n) / t4, (-t1 * f) / t4,
                              0, 0, -1, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Orthographic(float l, float r, float b, float t, float n, float f)
        {
            return new Matrix(2 / (r - l), 0, 0, -(r + l) / (r - l),
                              0, 2 / (t - b), 0, -(t + b) / (t - b),
                              0, 0, -2 / (f - n), -(f + n) / (f - n),
                              0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Perspective(float fovy, float aspect, float near, float far)
        {
            float ymax = near * MathF.Tan(fovy * MathF.PI / 360);
            float xmax = ymax * aspect;
            return Frustum(-xmax, xmax, -ymax, ymax, near, far);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix LookAtMatrix(V eye, V center, V up)
        {
            Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(eye.v.X, eye.v.Y, eye.v.Z), new Vector3(center.v.X, center.v.Y, center.v.Z), new Vector3(up.v.X, up.v.Y, up.v.Z));
            return new Matrix(view);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(Matrix m, V v) => new Matrix().Translate(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Scale(Matrix m, V v) => Scale(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Rotate(Matrix m, V v, float a) => Rotate(v, a).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Mul(Matrix b)
        {
            return new Matrix(Matrix4x4.Multiply(m, b.m));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MulPosition(V b)
        {
            var x = m.M11 * b.v.X + m.M12 * b.v.Y + m.M13 * b.v.Z + m.M14;
            var y = m.M21 * b.v.X + m.M22 * b.v.Y + m.M23 * b.v.Z + m.M24;
            var z = m.M31 * b.v.X + m.M32 * b.v.Y + m.M33 * b.v.Z + m.M34;
            return new V(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public V MulDirection(V b)
        {
            var x = m.M11 * b.v.X + m.M12 * b.v.Y + m.M13 * b.v.Z;
            var y = m.M21 * b.v.X + m.M22 * b.v.Y + m.M23 * b.v.Z;
            var z = m.M31 * b.v.X + m.M32 * b.v.Y + m.M33 * b.v.Z;
            return new V(x, y, z).Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ray MulRay(Ray b) => new Ray(MulPosition(b.Origin), MulDirection(b.Direction));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box MulBox(Box box)
        {
            var r = new V(m.M11, m.M21, m.M31);
            var u = new V(m.M12, m.M22, m.M32);
            var b = new V(m.M13, m.M23, m.M33);
            var t = new V(m.M14, m.M24, m.M34);
            var xa = r.MulScalar(box.Min.v.X);
            var xb = r.MulScalar(box.Max.v.X);
            var ya = u.MulScalar(box.Min.v.Y);
            var yb = u.MulScalar(box.Max.v.Y);
            var za = b.MulScalar(box.Min.v.Z);
            var zb = b.MulScalar(box.Max.v.Z);
            (xa, xb) = (xa.Min(xb), xa.Max(xb));
            (ya, yb) = (ya.Min(yb), ya.Max(yb));
            (za, zb) = (za.Min(zb), za.Max(zb));
            var min = xa.Add(ya).Add(za).Add(t);
            var max = xb.Add(yb).Add(zb).Add(t);
            return new Box(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Transpose() 
        {
            return new Matrix(Matrix4x4.Transpose(m));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Inverse()
        {
            Matrix4x4.Invert(m, out Matrix4x4 invm);
            return new Matrix(invm);
        }
    }
};
