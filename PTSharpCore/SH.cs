using System;

namespace PTSharpCore
{
    internal delegate double func(Vector d);

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
            sh.mesh = MC.NewSDFMesh(sh, sh.BoundingBox(), 0.01F);
            return sh;
        }

        void IShape.Compile()
        {
            mesh.Compile();
        }

        Box IShape.BoundingBox()
        {
            double r = 1;
            return new Box(new Vector(-r, -r, -r), new Vector(r, r, r));
        }

        Box SDF.BoundingBox()
        {
            double r = 1;
            return new Box(new Vector(-r, -r, -r), new Vector(r, r, r));
        }

        internal Box BoundingBox()
        {
            const int r = 1;
            return new Box(new Vector(-r, -r, -r), new Vector(r, r, r));
        }

        Hit IShape.Intersect(Ray r)
        {
            var hit = mesh.Intersect(r);
            if (!hit.Ok)
            {
                return Hit.NoHit;
            }
            return new Hit(this, hit.T, null);
        }

        Vector IShape.UVector(Vector p)
        {
            return new Vector();
        }

        Material IShape.MaterialAt(Vector p)
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

        Vector IShape.NormalAt(Vector p)
        {
            const double e = 0.0001;
            (var x, var y, var z) = (p.X, p.Y, p.Z);

            var n = new Vector(
                Evaluate(new Vector(x - e, y, z)) - Evaluate(new Vector(x + e, y, z)),
                Evaluate(new Vector(x, y - e, z)) - Evaluate(new Vector(x, y + e, z)),
                Evaluate(new Vector(x, y, z - e)) - Evaluate(new Vector(x, y, z + e)));

            return n.Normalize();
        }

        double EvaluateHarmonic(Vector p)
        {
            return harmonicFunction(p.Normalize());
        }

        double Evaluate(Vector p)
        {
            return p.Length() - Math.Abs(harmonicFunction(p.Normalize()));
        }

        double SDF.Evaluate(Vector p)
        {
            return p.Length() - Math.Abs(harmonicFunction(p.Normalize()));
        }

        static double sh00(Vector d)
        {
            return 0.282095F;
        }

        static double sh1n1(Vector d)
        {
            return -0.488603F * d.Y;
        }

        static double sh10(Vector d)
        {
            return 0.488603F * d.Z;
        }

        static double sh1p1(Vector d)
        {
            return -0.488603F * d.X;
        }

        static double sh2n2(Vector d)
        {
            // 0.5 * sqrt(15/pi) * x * y
            return 1.092548F * d.X * d.Y;
        }

        static double sh2n1(Vector d)
        {
            // -0.5 * sqrt(15/pi) * y * z
            return -1.092548F * d.Y * d.Z;
        }

        static double sh20(Vector d)
        {
            // 0.25 * sqrt(5/pi) * (-x^2-y^2+2z^2)
            return 0.315392F * (-d.X * d.X - d.Y * d.Y + 2.0F * d.Z * d.Z);
        }

        static double sh2p1(Vector d)
        {
            // -0.5 * sqrt(15/pi) * x * z
            return -1.092548F * d.X * d.Z;
        }

        static double sh2p2(Vector d)
        {
            // 0.25 * sqrt(15/pi) * (x^2 - y^2)
            return 0.546274F * (d.X * d.X - d.Y * d.Y);
        }

        static double sh3n3(Vector d)
        {
            // -0.25 * sqrt(35/(2pi)) * y * (3x^2 - y^2)
            return -0.590044F * d.Y * (3.0F * d.X * d.X - d.Y * d.Y);
        }

        static double sh3n2(Vector d)
        {
            // 0.5 * sqrt(105/pi) * x * y * z
            return 2.890611F * d.X * d.Y * d.Z;
        }

        static double sh3n1(Vector d)
        {
            // -0.25 * sqrt(21/(2pi)) * y * (4z^2-x^2-y^2)
            return -0.457046F * d.Y * (4.0F * d.Z * d.Z - d.X * d.X - d.Y * d.Y);
        }

        static double sh30(Vector d)
        {
            // 0.25 * sqrt(7/pi) * z * (2z^2 - 3x^2 - 3y^2)
            return 0.373176F * d.Z * (2.0F * d.Z * d.Z - 3.0F * d.X * d.X - 3.0F * d.Y * d.Y);
        }

        static double sh3p1(Vector d)
        {
            // -0.25 * sqrt(21/(2pi)) * x * (4z^2-x^2-y^2)
            return -0.457046F * d.X * (4.0F * d.Z * d.Z - d.X * d.X - d.Y * d.Y);
        }

        static double sh3p2(Vector d)
        {
            // 0.25 * sqrt(105/pi) * z * (x^2 - y^2)
            return 1.445306F * d.Z * (d.X * d.X - d.Y * d.Y);
        }

        static double sh3p3(Vector d)
        {
            // -0.25 * sqrt(35/(2pi)) * x * (x^2-3y^2)
            return -0.590044F * d.X * (d.X * d.X - 3.0F * d.Y * d.Y);
        }
        static double sh4n4(Vector d)
        {
            // 0.75 * sqrt(35/pi) * x * y * (x^2-y^2)
            return 2.503343F * d.X * d.Y * (d.X * d.X - d.Y * d.Y);
        }

        static double sh4n3(Vector d)
        {
            // -0.75 * sqrt(35/(2pi)) * y * z * (3x^2-y^2)
            return -1.770131F * d.Y * d.Z * (3.0F * d.X * d.X - d.Y * d.Y);
        }

        static double sh4n2(Vector d)
        {
            // 0.75 * sqrt(5/pi) * x * y * (7z^2-1)
            return 0.946175F * d.X * d.Y * (7.0F * d.Z * d.Z - 1.0F);
        }

        static double sh4n1(Vector d)
        {
            // -0.75 * sqrt(5/(2pi)) * y * z * (7z^2-3)
            return -0.669047F * d.Y * d.Z * (7.0F * d.Z * d.Z - 3.0F);
        }

        static double sh40(Vector d)
        {
            // 3/16 * sqrt(1/pi) * (35z^4-30z^2+3)
            double z2 = d.Z * d.Z;
            return 0.105786F * (35.0F * z2 * z2 - 30.0F * z2 + 3.0F);
        }

        static double sh4p1(Vector d)
        {
            // -0.75 * sqrt(5/(2pi)) * x * z * (7z^2-3)
            return -0.669047F * d.X * d.Z * (7.0F * d.Z * d.Z - 3.0F);
        }

        static double sh4p2(Vector d)
        {
            // 3/8 * sqrt(5/pi) * (x^2 - y^2) * (7z^2 - 1)
            return 0.473087F * (d.X * d.X - d.Y * d.Y) * (7.0F * d.Z * d.Z - 1.0F);
        }

        static double sh4p3(Vector d)
        {
            // -0.75 * sqrt(35/(2pi)) * x * z * (x^2 - 3y^2)
            return -1.770131F * d.X * d.Z * (d.X * d.X - 3.0F * d.Y * d.Y);
        }

        static double sh4p4(Vector d)
        {
            // 3/16*sqrt(35/pi) * (x^2 * (x^2 - 3y^2) - y^2 * (3x^2 - y^2))
            double x2 = d.X * d.X;
            double y2 = d.Y * d.Y;
            return 0.625836F * (x2 * (x2 - 3.0F * y2) - y2 * (3.0F * x2 - y2));
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

        public Colour ComputeContribution(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Colour ComputeDirectLighting(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Colour ComputeIndirectLighting(Vector position, Vector normal, Material material, Scene scene)
        {
            throw new NotImplementedException();
        }

        public Vector DirectionFrom(Vector position)
        {
            throw new NotImplementedException();
        }

        public Vector SamplePoint(Random rand)
        {
            throw new NotImplementedException();
        }
    }
}