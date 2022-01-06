using System;
using System.Runtime.CompilerServices;


namespace PTSharpCore
{
    struct Matrix
    {
        public double x00, x01, x02, x03;
        public double x10, x11, x12, x13;
        public double x20, x21, x22, x23;
        public double x30, x31, x32, x33;

        public Matrix(double x00_, double x01_, double x02_, double x03_,
                      double x10_, double x11_, double x12_, double x13_,
                      double x20_, double x21_, double x22_, double x23_,
                      double x30_, double x31_, double x32_, double x33_)
        {
            x00 = x00_; x01 = x01_; x02 = x02_; x03 = x03_;
            x10 = x10_; x11 = x11_; x12 = x12_; x13 = x13_;
            x20 = x20_; x21 = x21_; x22 = x22_; x23 = x23_;
            x30 = x30_; x31 = x31_; x32 = x32_; x33 = x33_;
        }

        internal static Matrix Identity = new Matrix(1, 0, 0, 0,
                                                     0, 1, 0, 0,
                                                     0, 0, 1, 0,
                                                     0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(IVector<double> v) => new Matrix(1, 0, 0, v.dv[0],
                                                                   0, 1, 0, v.dv[1],
                                                                   0, 0, 1, v.dv[2],
                                                                   0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Scale(IVector<double> v) => new Matrix(v.dv[0], 0, 0, 0,
                                                               0, v.dv[1], 0, 0,
                                                               0, 0, v.dv[2], 0,
                                                               0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Rotate(IVector<double> v, double a)
        {
            v = v.Normalize();
            var s = Math.Sin(a);
            var c = Math.Cos(a);
            var m = 1 - c;
            return new Matrix(m * v.dv[0] * v.dv[0] + c, m * v.dv[0] * v.dv[1] + v.dv[2] * s, m * v.dv[2] * v.dv[0] - v.dv[1] * s, 0,
                              m * v.dv[0] * v.dv[1] - v.dv[2] * s, m * v.dv[1] * v.dv[1] + c, m * v.dv[1] * v.dv[2] + v.dv[0] * s, 0,
                              m * v.dv[2] * v.dv[0] + v.dv[1] * s, m * v.dv[1] * v.dv[2] - v.dv[0] * s, m * v.dv[2] * v.dv[2] + c, 0,
                              0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Frustum(double l, double r, double b, double t, double n, double f)
        {
            double t1 = 2.0D * n;
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
        internal Matrix LookAtMatrix(IVector<double> eye, IVector<double> center, IVector<double> up)
        {
            up = up.Normalize();
            var f = center.Sub(eye).Normalize();
            var s = f.Cross(up).Normalize();
            var u = s.Cross(f);

            var m = new Matrix(s.dv[0], u.dv[0], f.dv[0], 0,
                               s.dv[1], u.dv[1], f.dv[1], 0,
                               s.dv[2], u.dv[2], f.dv[2], 0,
                               0, 0, 0, 1);

            return m.Transpose().Inverse().Translate(m, eye);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(Matrix m, IVector<double> v) => new Matrix().Translate(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Scale(Matrix m, IVector<double> v) => Scale(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Rotate(Matrix m, IVector<double> v, double a) => Rotate(v, a).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Mul(Matrix b)
        {
            Matrix m = new Matrix();
            m.x00 = x00 * b.x00 + x01 * b.x10 + x02 * b.x20 + x03 * b.x30;
            m.x10 = x10 * b.x00 + x11 * b.x10 + x12 * b.x20 + x13 * b.x30;
            m.x20 = x20 * b.x00 + x21 * b.x10 + x22 * b.x20 + x23 * b.x30;
            m.x30 = x30 * b.x00 + x31 * b.x10 + x32 * b.x20 + x33 * b.x30;
            m.x01 = x00 * b.x01 + x01 * b.x11 + x02 * b.x21 + x03 * b.x31;
            m.x11 = x10 * b.x01 + x11 * b.x11 + x12 * b.x21 + x13 * b.x31;
            m.x21 = x20 * b.x01 + x21 * b.x11 + x22 * b.x21 + x23 * b.x31;
            m.x31 = x30 * b.x01 + x31 * b.x11 + x32 * b.x21 + x33 * b.x31;
            m.x02 = x00 * b.x02 + x01 * b.x12 + x02 * b.x22 + x03 * b.x32;
            m.x12 = x10 * b.x02 + x11 * b.x12 + x12 * b.x22 + x13 * b.x32;
            m.x22 = x20 * b.x02 + x21 * b.x12 + x22 * b.x22 + x23 * b.x32;
            m.x32 = x30 * b.x02 + x31 * b.x12 + x32 * b.x22 + x33 * b.x32;
            m.x03 = x00 * b.x03 + x01 * b.x13 + x02 * b.x23 + x03 * b.x33;
            m.x13 = x10 * b.x03 + x11 * b.x13 + x12 * b.x23 + x13 * b.x33;
            m.x23 = x20 * b.x03 + x21 * b.x13 + x22 * b.x23 + x23 * b.x33;
            m.x33 = x30 * b.x03 + x31 * b.x13 + x32 * b.x23 + x33 * b.x33;
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> MulPosition(IVector<double> b)
        {
            var x = x00 * b.dv[0] + x01 * b.dv[1] + x02 * b.dv[2] + x03;
            var y = x10 * b.dv[0] + x11 * b.dv[1] + x12 * b.dv[2] + x13;
            var z = x20 * b.dv[0] + x21 * b.dv[1] + x22 * b.dv[2] + x23;

            return new IVector<double>(new double[] { x, y, z, 0 });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IVector<double> MulDirection(IVector<double> b)
        {
            var x = x00 * b.dv[0] + x01 * b.dv[1] + x02 * b.dv[2];
            var y = x10 * b.dv[0] + x11 * b.dv[1] + x12 * b.dv[2];
            var z = x20 * b.dv[0] + x21 * b.dv[1] + x22 * b.dv[2];
            return new IVector<double>(new double[] { x, y, z, 0 }).Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ray MulRay(Ray b) => new Ray(MulPosition(b.Origin), MulDirection(b.Direction));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box MulBox(Box box)
        {
            var r = new IVector<double>(new double[] { x00, x10, x20, 0 });
            var u = new IVector<double>(new double[] { x01, x11, x21, 0 });
            var b = new IVector<double>(new double[] { x02, x12, x22, 0 });
            var t = new IVector<double>(new double[] { x03, x13, x23, 0 });
            var xa = r.MulScalar(box.Min.dv[0]);
            var xb = r.MulScalar(box.Max.dv[0]);
            var ya = u.MulScalar(box.Min.dv[1]);
            var yb = u.MulScalar(box.Max.dv[1]);
            var za = b.MulScalar(box.Min.dv[2]);
            var zb = b.MulScalar(box.Max.dv[2]);
            (xa, xb) = (xa.Min(xb), xa.Max(xb));
            (ya, yb) = (ya.Min(yb), ya.Max(yb));
            (za, zb) = (za.Min(zb), za.Max(zb));
            var min = xa.Add(ya).Add(za).Add(t);
            var max = xb.Add(yb).Add(zb).Add(t);
            return new Box(min, max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Transpose() => new Matrix(x00, x10, x20, x30, x01, x11, x21, x31, x02, x12, x22, x32, x03, x13, x23, x33);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {
            return (x00 * x11 * x22 * x33 - x00 * x11 * x23 * x32 +
                    x00 * x12 * x23 * x31 - x00 * x12 * x21 * x33 +
                    x00 * x13 * x21 * x32 - x00 * x13 * x22 * x31 -
                    x01 * x12 * x23 * x30 + x01 * x12 * x20 * x33 -
                    x01 * x13 * x20 * x32 + x01 * x13 * x22 * x30 -
                    x01 * x10 * x22 * x33 + x01 * x10 * x23 * x32 +
                    x02 * x13 * x20 * x31 - x02 * x13 * x21 * x30 +
                    x02 * x10 * x21 * x33 - x02 * x10 * x23 * x31 +
                    x02 * x11 * x23 * x30 - x02 * x11 * x20 * x33 -
                    x03 * x10 * x21 * x32 + x03 * x10 * x22 * x31 -
                    x03 * x11 * x22 * x30 + x03 * x11 * x20 * x32 -
                    x03 * x12 * x20 * x31 + x03 * x12 * x21 * x30);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Inverse()
        {
            Matrix m = new Matrix();
            double d = Determinant();
            m.x00 = (x12 * x23 * x31 - x13 * x22 * x31 + x13 * x21 * x32 - x11 * x23 * x32 - x12 * x21 * x33 + x11 * x22 * x33) / d;
            m.x01 = (x03 * x22 * x31 - x02 * x23 * x31 - x03 * x21 * x32 + x01 * x23 * x32 + x02 * x21 * x33 - x01 * x22 * x33) / d;
            m.x02 = (x02 * x13 * x31 - x03 * x12 * x31 + x03 * x11 * x32 - x01 * x13 * x32 - x02 * x11 * x33 + x01 * x12 * x33) / d;
            m.x03 = (x03 * x12 * x21 - x02 * x13 * x21 - x03 * x11 * x22 + x01 * x13 * x22 + x02 * x11 * x23 - x01 * x12 * x23) / d;
            m.x10 = (x13 * x22 * x30 - x12 * x23 * x30 - x13 * x20 * x32 + x10 * x23 * x32 + x12 * x20 * x33 - x10 * x22 * x33) / d;
            m.x11 = (x02 * x23 * x30 - x03 * x22 * x30 + x03 * x20 * x32 - x00 * x23 * x32 - x02 * x20 * x33 + x00 * x22 * x33) / d;
            m.x12 = (x03 * x12 * x30 - x02 * x13 * x30 - x03 * x10 * x32 + x00 * x13 * x32 + x02 * x10 * x33 - x00 * x12 * x33) / d;
            m.x13 = (x02 * x13 * x20 - x03 * x12 * x20 + x03 * x10 * x22 - x00 * x13 * x22 - x02 * x10 * x23 + x00 * x12 * x23) / d;
            m.x20 = (x11 * x23 * x30 - x13 * x21 * x30 + x13 * x20 * x31 - x10 * x23 * x31 - x11 * x20 * x33 + x10 * x21 * x33) / d;
            m.x21 = (x03 * x21 * x30 - x01 * x23 * x30 - x03 * x20 * x31 + x00 * x23 * x31 + x01 * x20 * x33 - x00 * x21 * x33) / d;
            m.x22 = (x01 * x13 * x30 - x03 * x11 * x30 + x03 * x10 * x31 - x00 * x13 * x31 - x01 * x10 * x33 + x00 * x11 * x33) / d;
            m.x23 = (x03 * x11 * x20 - x01 * x13 * x20 - x03 * x10 * x21 + x00 * x13 * x21 + x01 * x10 * x23 - x00 * x11 * x23) / d;
            m.x30 = (x12 * x21 * x30 - x11 * x22 * x30 - x12 * x20 * x31 + x10 * x22 * x31 + x11 * x20 * x32 - x10 * x21 * x32) / d;
            m.x31 = (x01 * x22 * x30 - x02 * x21 * x30 + x02 * x20 * x31 - x00 * x22 * x31 - x01 * x20 * x32 + x00 * x21 * x32) / d;
            m.x32 = (x02 * x11 * x30 - x01 * x12 * x30 - x02 * x10 * x31 + x00 * x12 * x31 + x01 * x10 * x32 - x00 * x11 * x32) / d;
            m.x33 = (x01 * x12 * x20 - x02 * x11 * x20 + x02 * x10 * x21 - x00 * x12 * x21 - x01 * x10 * x22 + x00 * x11 * x22) / d;
            return m;
        }
    }
};
