using System;

namespace PTSharpCore
{
    internal delegate double func(IVector<double> d);

    class SphericalHarmonic : IShape, SDF
    {
        Material PositiveMaterial;
        Material NegativeMaterial;
        func harmonicFunction;
        Mesh mesh;

        internal static IShape NewSphericalHarmonic(int l, int m, Material pm, Material nm)
        {
            var sh = new SphericalHarmonic();
            sh.PositiveMaterial = pm;
            sh.NegativeMaterial = nm;
            sh.harmonicFunction = shFunc(l, m);
            sh.mesh = MC.NewSDFMesh(sh, sh.BoundingBox(), 0.01);
            return sh;
        }

        void IShape.Compile()
        {
            mesh.Compile();
        }

        Box IShape.BoundingBox()
        {
            double r = 1;
            return new Box(new IVector<double>(new double[] { -r, -r, -r, 0 }), new IVector<double>(new double[] { r, r, r, 0 }));
        }

        Box SDF.BoundingBox()
        {
            double r = 1;
            return new Box(new IVector<double>(new double[] { -r, -r, -r, 0 }), new IVector<double>(new double[] { r, r, r, 0 }));
        }

        internal Box BoundingBox()
        {
            const int r = 1;
            return new Box(new IVector<double>(new double[] { -r, -r, -r, 0 }), new IVector<double>(new double[] { r, r, r, 0 }));
        }

        Hit IShape.Intersect(Ray r)
        {
            var hit = mesh.Intersect(r);
            if (!hit.Ok())
            {
                return Hit.NoHit;
            }
            return new Hit(this, hit.T, null);
        }

        IVector<double> IShape.UV(IVector<double> p)
        {
            return new IVector<double>();
        }

        Material IShape.MaterialAt(IVector<double> p)
        {
            var h = EvaluateHarmonic(p);
            if (h < 0)
            {
                return NegativeMaterial;
            }
            else
            {
                return PositiveMaterial;
            }
        }

        IVector<double> IShape.NormalAt(IVector<double> p)
        {
            const double e = 0.0001;
            (var x, var y, var z) = (p.dv[0], p.dv[1], p.dv[2]);

            var n = new IVector<double>(new double[] {
                Evaluate(new IVector<double>(new double[] { x - e, y, z, 0 })) - Evaluate(new IVector<double>(new double[] { x + e, y, z, 0 })),
                Evaluate(new IVector<double>(new double[] { x, y - e, z, 0 })) - Evaluate(new IVector<double>(new double[] { x, y + e, z, 0 })),
                Evaluate(new IVector<double>(new double[] { x, y, z - e, 0 })) - Evaluate(new IVector<double>(new double[] { x, y, z + e, 0 })), 0 });

            return n.Normalize();
        }

        double EvaluateHarmonic(IVector<double> p)
        {
            return harmonicFunction(p.Normalize());
        }

        double Evaluate(IVector<double> p)
        {
            return p.Length() - Math.Abs(harmonicFunction(p.Normalize()));
        }

        double SDF.Evaluate(IVector<double> p)
        {
            return p.Length() - Math.Abs(harmonicFunction(p.Normalize()));
        }

        static double sh00(IVector<double> d)
        {
            return 0.282095;
        }

        static double sh1n1(IVector<double> d)
        {
            return -0.488603 * d.dv[1];
        }

        static double sh10(IVector<double> d)
        {
            return 0.488603 * d.dv[2];
        }

        static double sh1p1(IVector<double> d)
        {
            return -0.488603 * d.dv[0];
        }

        static double sh2n2(IVector<double> d)
        {
            // 0.5 * sqrt(15/pi) * x * y
            return 1.092548 * d.dv[0] * d.dv[1];
        }

        static double sh2n1(IVector<double> d)
        {
            // -0.5 * sqrt(15/pi) * y * z
            return -1.092548 * d.dv[1] * d.dv[2];
        }

        static double sh20(IVector<double> d)
        {
            // 0.25 * sqrt(5/pi) * (-x^2-y^2+2z^2)
            return 0.315392 * (-d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1] + 2.0 * d.dv[2] * d.dv[2]);
        }

        static double sh2p1(IVector<double> d)
        {
            // -0.5 * sqrt(15/pi) * x * z
            return -1.092548 * d.dv[0] * d.dv[2];
        }

        static double sh2p2(IVector<double> d)
        {
            // 0.25 * sqrt(15/pi) * (x^2 - y^2)
            return 0.546274 * (d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh3n3(IVector<double> d)
        {
            // -0.25 * sqrt(35/(2pi)) * y * (3x^2 - y^2)
            return -0.590044 * d.dv[1] * (3.0 * d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh3n2(IVector<double> d)
        {
            // 0.5 * sqrt(105/pi) * x * y * z
            return 2.890611 * d.dv[0] * d.dv[1] * d.dv[2];
        }

        static double sh3n1(IVector<double> d)
        {
            // -0.25 * sqrt(21/(2pi)) * y * (4z^2-x^2-y^2)
            return -0.457046 * d.dv[1] * (4.0 * d.dv[2] * d.dv[2] - d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh30(IVector<double> d)
        {
            // 0.25 * sqrt(7/pi) * z * (2z^2 - 3x^2 - 3y^2)
            return 0.373176 * d.dv[2] * (2.0 * d.dv[2] * d.dv[2] - 3.0 * d.dv[0] * d.dv[0] - 3.0 * d.dv[1] * d.dv[1]);
        }

        static double sh3p1(IVector<double> d)
        {
            // -0.25 * sqrt(21/(2pi)) * x * (4z^2-x^2-y^2)
            return -0.457046 * d.dv[0] * (4.0 * d.dv[2] * d.dv[2] - d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh3p2(IVector<double> d)
        {
            // 0.25 * sqrt(105/pi) * z * (x^2 - y^2)
            return 1.445306 * d.dv[2] * (d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh3p3(IVector<double> d)
        {
            // -0.25 * sqrt(35/(2pi)) * x * (x^2-3y^2)
            return -0.590044 * d.dv[0] * (d.dv[0] * d.dv[0] - 3.0 * d.dv[1] * d.dv[1]);
        }
        static double sh4n4(IVector<double> d)
        {
            // 0.75 * sqrt(35/pi) * x * y * (x^2-y^2)
            return 2.503343 * d.dv[0] * d.dv[1] * (d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh4n3(IVector<double> d)
        {
            // -0.75 * sqrt(35/(2pi)) * y * z * (3x^2-y^2)
            return -1.770131 * d.dv[1] * d.dv[2] * (3.0 * d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]);
        }

        static double sh4n2(IVector<double> d)
        {
            // 0.75 * sqrt(5/pi) * x * y * (7z^2-1)
            return 0.946175 * d.dv[0] * d.dv[1] * (7.0 * d.dv[2] * d.dv[2] - 1.0);
        }

        static double sh4n1(IVector<double> d)
        {
            // -0.75 * sqrt(5/(2pi)) * y * z * (7z^2-3)
            return -0.669047 * d.dv[1] * d.dv[2] * (7.0 * d.dv[2] * d.dv[2] - 3.0);
        }

        static double sh40(IVector<double> d)
        {
            // 3/16 * sqrt(1/pi) * (35z^4-30z^2+3)
            double z2 = d.dv[2] * d.dv[2];
            return 0.105786 * (35.0 * z2 * z2 - 30.0 * z2 + 3.0);
        }

        static double sh4p1(IVector<double> d)
        {
            // -0.75 * sqrt(5/(2pi)) * x * z * (7z^2-3)
            return -0.669047 * d.dv[0] * d.dv[2] * (7.0 * d.dv[2] * d.dv[2] - 3.0);
        }

        static double sh4p2(IVector<double> d)
        {
            // 3/8 * sqrt(5/pi) * (x^2 - y^2) * (7z^2 - 1)
            return 0.473087 * (d.dv[0] * d.dv[0] - d.dv[1] * d.dv[1]) * (7.0 * d.dv[2] * d.dv[2] - 1.0);
        }

        static double sh4p3(IVector<double> d)
        {
            // -0.75 * sqrt(35/(2pi)) * x * z * (x^2 - 3y^2)
            return -1.770131 * d.dv[0] * d.dv[2] * (d.dv[0] * d.dv[0] - 3.0 * d.dv[1] * d.dv[1]);
        }

        static double sh4p4(IVector<double> d)
        {
            // 3/16*sqrt(35/pi) * (x^2 * (x^2 - 3y^2) - y^2 * (3x^2 - y^2))
            double x2 = d.dv[0] * d.dv[0];
            double y2 = d.dv[1] * d.dv[1];
            return 0.625836 * (x2 * (x2 - 3.0 * y2) - y2 * (3.0 * x2 - y2));
        }

        static func shFunc(int l, int m)
        {
            func f = null;

            if (l == 0 && m == 0)
            {
                f = sh00;
            }
            else if (l == 1 && m == -1)
            {
                f = sh1n1;
            }
            else if (l == 1 && m == 0)
            {
                f = sh10;
            }
            else if (l == 1 && m == 1)
            {
                f = sh1p1;
            }
            else if (l == 2 && m == -2)
            {
                f = sh2n2;
            }
            else if (l == 2 && m == -1)
            {
                f = sh2n1;
            }
            else if (l == 2 && m == 0)
            {
                f = sh20;
            }
            else if (l == 2 && m == 1)
            {
                f = sh2p1;
            }
            else if (l == 2 && m == 2)
            {
                f = sh2p2;
            }
            else if (l == 3 && m == -3)
            {
                f = sh3n3;
            }
            else if (l == 3 && m == -2)
            {
                f = sh3n2;
            }
            else if (l == 3 && m == -1)
            {
                f = sh3n1;
            }
            else if (l == 3 && m == 0)
            {
                f = sh30;
            }
            else if (l == 3 && m == 1)
            {
                f = sh3p1;
            }
            else if (l == 3 && m == 2)
            {
                f = sh3p2;
            }
            else if (l == 3 && m == 3)
            {
                f = sh3p3;
            }
            else if (l == 4 && m == -4)
            {
                f = sh4n4;
            }
            else if (l == 4 && m == -3)
            {
                f = sh4n3;
            }
            else if (l == 4 && m == -2)
            {
                f = sh4n2;
            }
            else if (l == 4 && m == -1)
            {
                f = sh4n1;
            }
            else if (l == 4 && m == 0)
            {
                f = sh40;
            }
            else if (l == 4 && m == 1)
            {
                f = sh4p1;
            }
            else if (l == 4 && m == 2)
            {
                f = sh4p2;
            }
            else if (l == 4 && m == 3)
            {
                f = sh4p3;
            }
            else if (l == 4 && m == 4)
            {
                f = sh4p4;
            }
            else
            {
                Console.WriteLine("unsupported spherical harmonic");
            }
            return f;
        }
    }
}