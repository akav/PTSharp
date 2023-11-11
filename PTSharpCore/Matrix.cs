using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PTSharpCore
{
    public class Matrix
    {
        public double M11, M12, M13, M14;
        public double M21, M22, M23, M24;
        public double M31, M32, M33, M34;
        public double M41, M42, M43, M44;

        public Vector256<double> row1, row2, row3, row4;

        public Matrix(double M11, double M12, double M13, double M14,
                  double M21, double M22, double M23, double M24,
                  double M31, double M32, double M33, double M34,
                  double M41, double M42, double M43, double M44)
        {
            row1 = Vector256.Create(M11, M12, M13, M14);
            row2 = Vector256.Create(M21, M22, M23, M24);
            row3 = Vector256.Create(M31, M32, M33, M34);
            row4 = Vector256.Create(M41, M42, M43, M44);
        }

        public Matrix() { }

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

        internal Matrix Orthographic(double l, double r, double b, double t, double n, double f)
        {
            return new Matrix(2 / (r - l), 0, 0, -(r + l) / (r - l),
                              0, 2 / (t - b), 0, -(t + b) / (t - b),
                              0, 0, -2 / (f - n), -(f + n) / (f - n),
                              0, 0, 0, 1);
        }

        internal Matrix Perspective(double fovy, double aspect, double near, double far)
        {
            double ymax = near * Math.Tan(fovy * Math.PI / 360);
            double xmax = ymax * aspect;
            return Frustum(-xmax, xmax, -ymax, ymax, near, far);
        }

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
        public static Vector256<double> MultiplyAndSum(Vector256<double> row,
                                                    Vector256<double> col1, Vector256<double> col2,
                                                    Vector256<double> col3, Vector256<double> col4)
        {
            // Extracting columns from the matrix
            Vector256<double> col1_vals = Vector256.Create(col1.GetElement(0), col2.GetElement(0), col3.GetElement(0), col4.GetElement(0));
            Vector256<double> col2_vals = Vector256.Create(col1.GetElement(1), col2.GetElement(1), col3.GetElement(1), col4.GetElement(1));
            Vector256<double> col3_vals = Vector256.Create(col1.GetElement(2), col2.GetElement(2), col3.GetElement(2), col4.GetElement(2));
            Vector256<double> col4_vals = Vector256.Create(col1.GetElement(3), col2.GetElement(3), col3.GetElement(3), col4.GetElement(3));

            // Multiplying and summing
            Vector256<double> mul1 = Avx.Multiply(row, col1_vals);
            Vector256<double> mul2 = Avx.Multiply(row, col2_vals);
            Vector256<double> mul3 = Avx.Multiply(row, col3_vals);
            Vector256<double> mul4 = Avx.Multiply(row, col4_vals);

            // Summing up the products
            Vector256<double> sum1 = Avx.Add(mul1, mul2);
            Vector256<double> sum2 = Avx.Add(mul3, mul4);
            Vector256<double> result = Avx.Add(sum1, sum2);

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Matrix Mul(Matrix b)
        {
            var result = new Matrix(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            // Multiply row of 'this' matrix with column of 'b' matrix and sum up
            result.row1 = MultiplyAndSum(row1, b.row1, b.row2, b.row3, b.row4);
            result.row2 = MultiplyAndSum(row2, b.row1, b.row2, b.row3, b.row4);
            result.row3 = MultiplyAndSum(row3, b.row1, b.row2, b.row3, b.row4);
            result.row4 = MultiplyAndSum(row4, b.row1, b.row2, b.row3, b.row4);

            return result;            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector MulPosition(Vector b)
        {
            var vec = Vector256.Create(b.X, b.Y, b.Z, b.W);

            var res1 = Avx.Multiply(row1, vec);
            var res2 = Avx.Multiply(row2, vec);
            var res3 = Avx.Multiply(row3, vec);
            var res4 = Avx.Multiply(row4, vec);
            var sum1 = Avx.HorizontalAdd(res1, res2);
            var sum2 = Avx.HorizontalAdd(res3, res4);
            var sum = Avx.HorizontalAdd(sum1, sum2);

            // Assuming Vector structure has a constructor that takes four doubles
            return new Vector(sum.GetElement(0), sum.GetElement(1), sum.GetElement(2), sum.GetElement(3));
        }

        public Vector MulDirection(Vector b)
        {
            var x = M11 * b.X + M12 * b.Y + M13 * b.Z;
            var y = M21 * b.X + M22 * b.Y + M23 * b.Z;
            var z = M31 * b.X + M32 * b.Y + M33 * b.Z;
            return new Vector(x, y, z).Normalize();
        }

        public Ray MulRay(Ray b) => new Ray(MulPosition(b.Origin), MulDirection(b.Direction));

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
        public Matrix Transpose() => new Matrix(M11, M21, M31, M41, M12, M22, M32, M42, M13, M23, M33, M43, M14, M24, M34, M44);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Determinant()
        {
            double a = row1.GetElement(0);
            double b = row1.GetElement(1);
            double c = row1.GetElement(2);
            double d = row1.GetElement(3);

            double e = row2.GetElement(0);
            double f = row2.GetElement(1);
            double g = row2.GetElement(2);
            double h = row2.GetElement(3);

            double i = row3.GetElement(0);
            double j = row3.GetElement(1);
            double k = row3.GetElement(2);
            double l = row3.GetElement(3);

            double m = row4.GetElement(0);
            double n = row4.GetElement(1);
            double o = row4.GetElement(2);
            double p = row4.GetElement(3);

            // Calculating the determinant
            double det = a * (f * (k * p - l * o) - j * (g * p - h * o) + n * (g * l - h * k))
                       - b * (e * (k * p - l * o) - i * (g * p - h * o) + m * (g * l - h * k))
                       + c * (e * (j * p - l * n) - i * (f * p - h * n) + m * (f * l - h * j))
                       - d * (e * (j * o - k * n) - i * (f * o - g * n) + m * (f * k - g * j));

            return det;
        }
        public Matrix Inverse()
        {
            double det = this.Determinant();

            // Extract matrix elements from Vector256<double>
            double M11 = row1.GetElement(0), M12 = row1.GetElement(1), M13 = row1.GetElement(2), M14 = row1.GetElement(3);
            double M21 = row2.GetElement(0), M22 = row2.GetElement(1), M23 = row2.GetElement(2), M24 = row2.GetElement(3);
            double M31 = row3.GetElement(0), M32 = row3.GetElement(1), M33 = row3.GetElement(2), M34 = row3.GetElement(3);
            double M41 = row4.GetElement(0), M42 = row4.GetElement(1), M43 = row4.GetElement(2), M44 = row4.GetElement(3);

            // Calculate inverse using your existing formula
            Matrix m = new Matrix();
            m.row1 = Vector256.Create(
                (M23 * M34 * M42 - M24 * M33 * M42 + M24 * M32 * M43 - M22 * M34 * M43 - M23 * M32 * M44 + M22 * M33 * M44) / det,
                (M14 * M33 * M42 - M13 * M34 * M42 - M14 * M32 * M43 + M12 * M34 * M43 + M13 * M32 * M44 - M12 * M33 * M44) / det,
                (M13 * M24 * M42 - M14 * M23 * M42 + M14 * M22 * M43 - M12 * M24 * M43 - M13 * M22 * M44 + M12 * M23 * M44) / det,
                (M14 * M23 * M32 - M13 * M24 * M32 - M14 * M22 * M33 + M12 * M24 * M33 + M13 * M22 * M34 - M12 * M23 * M34) / det
            );

            m.row2 = Vector256.Create(
            (M24 * M33 * M41 - M23 * M34 * M41 - M24 * M31 * M43 + M21 * M34 * M43 + M23 * M31 * M44 - M21 * M33 * M44) / det,
            (M13 * M34 * M41 - M14 * M33 * M41 + M14 * M31 * M43 - M11 * M34 * M43 - M13 * M31 * M44 + M11 * M33 * M44) / det,
            (M14 * M23 * M41 - M13 * M24 * M41 - M14 * M21 * M43 + M11 * M24 * M43 + M13 * M21 * M44 - M11 * M23 * M44) / det,
            (M13 * M24 * M31 - M14 * M23 * M31 + M14 * M21 * M33 - M11 * M24 * M33 - M13 * M21 * M34 + M11 * M23 * M34) / det
        );

            m.row3 = Vector256.Create(
                (M22 * M34 * M41 - M24 * M32 * M41 + M24 * M31 * M42 - M21 * M34 * M42 - M22 * M31 * M44 + M21 * M32 * M44) / det,
                (M14 * M32 * M41 - M12 * M34 * M41 - M14 * M31 * M42 + M11 * M34 * M42 + M12 * M31 * M44 - M11 * M32 * M44) / det,
                (M12 * M24 * M41 - M14 * M22 * M41 + M14 * M21 * M42 - M11 * M24 * M42 - M12 * M21 * M44 + M11 * M22 * M44) / det,
                (M14 * M22 * M31 - M12 * M24 * M31 - M14 * M21 * M32 + M11 * M24 * M32 + M12 * M21 * M34 - M11 * M22 * M34) / det
            );

            m.row4 = Vector256.Create(
                (M23 * M32 * M41 - M22 * M33 * M41 - M23 * M31 * M42 + M21 * M33 * M42 + M22 * M31 * M43 - M21 * M32 * M43) / det,
                (M12 * M33 * M41 - M13 * M32 * M41 + M13 * M31 * M42 - M11 * M33 * M42 - M12 * M31 * M43 + M11 * M32 * M43) / det,
                (M13 * M22 * M41 - M12 * M23 * M41 - M13 * M21 * M42 + M11 * M23 * M42 + M12 * M21 * M43 - M11 * M22 * M43) / det,
                (M12 * M23 * M31 - M13 * M22 * M31 + M13 * M21 * M32 - M11 * M23 * M32 - M12 * M21 * M33 + M11 * M22 * M33) / det
            );

            return m;            
        }        
    }
};
