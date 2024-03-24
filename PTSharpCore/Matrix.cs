using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Matrix
    {
        private double[,] data;

        public Matrix(double x00, double x01, double x02, double x03,
            double x10, double x11, double x12, double x13,
            double x20, double x21, double x22, double x23,
            double x30, double x31, double x32, double x33)
        {
            data = new double[4, 4];
            data[0, 0] = x00; data[0, 1] = x01; data[0, 2] = x02; data[0, 3] = x03;
            data[1, 0] = x10; data[1, 1] = x11; data[1, 2] = x12; data[1, 3] = x13;
            data[2, 0] = x20; data[2, 1] = x21; data[2, 2] = x22; data[2, 3] = x23;
            data[3, 0] = x30; data[3, 1] = x31; data[3, 2] = x32; data[3, 3] = x33;
        }

        public Matrix()
        {
            data = new double[4, 4];
        }

        public Matrix(double[,] m) : this()
        {
            this.m = m;
        }

        internal static Matrix Identity = new Matrix(1, 0, 0, 0,
                                                        0, 1, 0, 0,
                                                        0, 0, 1, 0,
                                                        0, 0, 0, 1);
        private double[,] m;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(Vector v) => new Matrix(1, 0, 0, v.X,
                                                            0, 1, 0, v.Y,
                                                            0, 0, 1, v.Z,
                                                            0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            return m.Transpose().Inverse().Translate(eye);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Matrix Translate(Matrix m, Vector v) => new Matrix().Translate(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Scale(Matrix m, Vector v) => Scale(v).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Rotate(Matrix m, Vector v, double a) => Rotate(v, a).Mul(m);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Mul(Matrix b)
        {
            var m = new Matrix();
            for (var i = 0; i < 4; i++)
            {
                var x = data[i, 0];
                var y = data[i, 1];
                var z = data[i, 2];
                var w = data[i, 3];
                m.data[i, 0] = x * b.data[0, 0] + y * b.data[1, 0] + z * b.data[2, 0] + w * b.data[3, 0];
                m.data[i, 1] = x * b.data[0, 1] + y * b.data[1, 1] + z * b.data[2, 1] + w * b.data[3, 1];
                m.data[i, 2] = x * b.data[0, 2] + y * b.data[1, 2] + z * b.data[2, 2] + w * b.data[3, 2];
                m.data[i, 3] = x * b.data[0, 3] + y * b.data[1, 3] + z * b.data[2, 3] + w * b.data[3, 3];
            }
            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulPosition(Vector b)
        {
            var x = data[0, 0] * b.X + data[0, 1] * b.Y + data[0, 2] * b.Z + data[0, 3];
            var y = data[1, 0] * b.X + data[1, 1] * b.Y + data[1, 2] * b.Z + data[1, 3];
            var z = data[2, 0] * b.X + data[2, 1] * b.Y + data[2, 2] * b.Z + data[2, 3];
            return new Vector(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulDirection(Vector b)
        {
            var x = data[0, 0] * b.X + data[0, 1] * b.Y + data[0, 2] * b.Z;
            var y = data[1, 0] * b.X + data[1, 1] * b.Y + data[1, 2] * b.Z;
            var z = data[2, 0] * b.X + data[2, 1] * b.Y + data[2, 2] * b.Z;
            return new Vector(x, y, z).Normalize();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ray MulRay(ref Ray b) => new Ray(MulPosition(b.Origin), MulDirection(b.Direction));


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Box MulBox(Box box)
        {

            var r = new Vector(data[0, 0], data[1, 0], data[2, 0]);
            var u = new Vector(data[0, 1], data[1, 1], data[2, 1]);
            var b = new Vector(data[0, 2], data[1, 2], data[2, 2]);
            var t = new Vector(data[0, 3], data[1, 3], data[2, 3]);

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
        public Matrix Transpose()
        {
            var m = new Matrix();
            double[,] localData = this.data; // Create a local copy of the data

            Parallel.For(0, 4, i =>
            {
                m.data[0, i] = localData[i, 0];
                m.data[1, i] = localData[i, 1];
                m.data[2, i] = localData[i, 2];
                m.data[3, i] = localData[i, 3];
            });

            return m;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {

            var a = data[0, 0] * data[1, 1] - data[0, 1] * data[1, 0];
            var b = data[0, 0] * data[1, 2] - data[0, 2] * data[1, 0];
            var c = data[0, 0] * data[1, 3] - data[0, 3] * data[1, 0];
            var d = data[0, 1] * data[1, 2] - data[0, 2] * data[1, 1];
            var e = data[0, 1] * data[1, 3] - data[0, 3] * data[1, 1];
            var f = data[0, 2] * data[1, 3] - data[0, 3] * data[1, 2];
            var g = data[2, 0] * data[3, 1] - data[2, 1] * data[3, 0];
            var h = data[2, 0] * data[3, 2] - data[2, 2] * data[3, 0];
            var i = data[2, 0] * data[3, 3] - data[2, 3] * data[3, 0];
            var j = data[2, 1] * data[3, 2] - data[2, 2] * data[3, 1];
            var k = data[2, 1] * data[3, 3] - data[2, 3] * data[3, 1];
            var l = data[2, 2] * data[3, 3] - data[2, 3] * data[3, 2];
            return a * l - b * k + c * j + d * i - e * h + f * g;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static double SubmatrixDeterminant(double[,] matrixData, int excludeRow, int excludeCol)
        {
            double[,] submatrix = new double[3, 3];
            int subi = 0;

            for (int i = 0; i < 4; i++)
            {
                if (i == excludeRow) continue;

                int subj = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (j == excludeCol) continue;

                    submatrix[subi, subj] = matrixData[i, j];
                    subj++;
                }
                subi++;
            }

            // Calculate and return the determinant of the 3x3 submatrix
            return submatrix[0, 0] * (submatrix[1, 1] * submatrix[2, 2] - submatrix[1, 2] * submatrix[2, 1]) -
                    submatrix[0, 1] * (submatrix[1, 0] * submatrix[2, 2] - submatrix[1, 2] * submatrix[2, 0]) +
                    submatrix[0, 2] * (submatrix[1, 0] * submatrix[2, 1] - submatrix[1, 1] * submatrix[2, 0]);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Inverse()
        {

            Matrix m = new Matrix();
            double d = Determinant();

            m.data[0, 0] = (data[1, 1] * data[2, 2] * data[3, 3] + data[1, 2] * data[2, 3] * data[3, 1] + data[1, 3] * data[2, 1] * data[3, 2] - data[1, 1] * data[2, 3] * data[3, 2] - data[1, 2] * data[2, 1] * data[3, 3] - data[1, 3] * data[2, 2] * data[3, 1]) / d;
            m.data[0, 1] = (data[0, 1] * data[2, 3] * data[3, 2] + data[0, 2] * data[2, 1] * data[3, 3] + data[0, 3] * data[2, 2] * data[3, 1] - data[0, 1] * data[2, 2] * data[3, 3] - data[0, 2] * data[2, 3] * data[3, 1] - data[0, 3] * data[2, 1] * data[3, 2]) / d;
            m.data[0, 2] = (data[0, 1] * data[1, 2] * data[3, 3] + data[0, 2] * data[1, 3] * data[3, 1] + data[0, 3] * data[1, 1] * data[3, 2] - data[0, 1] * data[1, 3] * data[3, 2] - data[0, 2] * data[1, 1] * data[3, 3] - data[0, 3] * data[1, 2] * data[3, 1]) / d;
            m.data[0, 3] = (data[0, 1] * data[1, 3] * data[2, 2] + data[0, 2] * data[1, 1] * data[2, 3] + data[0, 3] * data[1, 2] * data[2, 1] - data[0, 1] * data[1, 2] * data[2, 3] - data[0, 2] * data[1, 3] * data[2, 1] - data[0, 3] * data[1, 1] * data[2, 2]) / d;
            m.data[1, 0] = (data[1, 0] * data[2, 3] * data[3, 2] + data[1, 2] * data[2, 0] * data[3, 3] + data[1, 3] * data[2, 2] * data[3, 0] - data[1, 0] * data[2, 2] * data[3, 3] - data[1, 2] * data[2, 3] * data[3, 0] - data[1, 3] * data[2, 0] * data[3, 2]) / d;
            m.data[1, 1] = (data[0, 0] * data[2, 2] * data[3, 3] + data[0, 2] * data[2, 3] * data[3, 0] + data[0, 3] * data[2, 0] * data[3, 2] - data[0, 0] * data[2, 3] * data[3, 2] - data[0, 2] * data[2, 0] * data[3, 3] - data[0, 3] * data[2, 2] * data[3, 0]) / d;
            m.data[1, 2] = (data[0, 0] * data[1, 3] * data[3, 2] + data[0, 2] * data[1, 0] * data[3, 3] + data[0, 3] * data[1, 2] * data[3, 0] - data[0, 0] * data[1, 2] * data[3, 3] - data[0, 2] * data[1, 3] * data[3, 0] - data[0, 3] * data[1, 0] * data[3, 2]) / d;
            m.data[1, 3] = (data[0, 0] * data[1, 2] * data[2, 3] + data[0, 2] * data[1, 3] * data[2, 0] + data[0, 3] * data[1, 0] * data[2, 2] - data[0, 0] * data[1, 3] * data[2, 2] - data[0, 2] * data[1, 0] * data[2, 3] - data[0, 3] * data[1, 2] * data[2, 0]) / d;
            m.data[2, 0] = (data[1, 0] * data[2, 1] * data[3, 3] + data[1, 1] * data[2, 3] * data[3, 0] + data[1, 3] * data[2, 0] * data[3, 1] - data[1, 0] * data[2, 3] * data[3, 1] - data[1, 1] * data[2, 0] * data[3, 3] - data[1, 3] * data[2, 1] * data[3, 0]) / d;
            m.data[2, 1] = (data[0, 0] * data[2, 3] * data[3, 1] + data[0, 1] * data[2, 0] * data[3, 3] + data[0, 3] * data[2, 1] * data[3, 0] - data[0, 0] * data[2, 1] * data[3, 3] - data[0, 1] * data[2, 3] * data[3, 0] - data[0, 3] * data[2, 0] * data[3, 1]) / d;
            m.data[2, 2] = (data[0, 0] * data[1, 1] * data[3, 3] + data[0, 1] * data[1, 3] * data[3, 0] + data[0, 3] * data[1, 0] * data[3, 1] - data[0, 0] * data[1, 3] * data[3, 1] - data[0, 1] * data[1, 0] * data[3, 3] - data[0, 3] * data[1, 1] * data[3, 0]) / d;
            m.data[2, 3] = (data[0, 0] * data[1, 3] * data[2, 1] + data[0, 1] * data[1, 0] * data[2, 3] + data[0, 3] * data[1, 1] * data[2, 0] - data[0, 0] * data[1, 1] * data[2, 3] - data[0, 1] * data[1, 3] * data[2, 0] - data[0, 3] * data[1, 0] * data[2, 1]) / d;
            m.data[3, 0] = (data[1, 0] * data[2, 2] * data[3, 1] + data[1, 1] * data[2, 0] * data[3, 2] + data[1, 2] * data[2, 1] * data[3, 0] - data[1, 0] * data[2, 1] * data[3, 2] - data[1, 1] * data[2, 2] * data[3, 0] - data[1, 2] * data[2, 0] * data[3, 1]) / d;
            m.data[3, 1] = (data[0, 0] * data[2, 1] * data[3, 2] + data[0, 1] * data[2, 2] * data[3, 0] + data[0, 2] * data[2, 0] * data[3, 1] - data[0, 0] * data[2, 2] * data[3, 1] - data[0, 1] * data[2, 0] * data[3, 2] - data[0, 2] * data[2, 1] * data[3, 0]) / d;
            m.data[3, 2] = (data[0, 0] * data[1, 2] * data[3, 1] + data[0, 1] * data[1, 0] * data[3, 2] + data[0, 2] * data[1, 1] * data[3, 0] - data[0, 0] * data[1, 1] * data[3, 2] - data[0, 1] * data[1, 2] * data[3, 0] - data[0, 2] * data[1, 0] * data[3, 1]) / d;
            m.data[3, 3] = (data[0, 0] * data[1, 1] * data[2, 2] + data[0, 1] * data[1, 2] * data[2, 0] + data[0, 2] * data[1, 0] * data[2, 1] - data[0, 0] * data[1, 2] * data[2, 1] - data[0, 1] * data[1, 0] * data[2, 2] - data[0, 2] * data[1, 1] * data[2, 0]) / d;
            return m;
        }
    }
}
