using ILGPU.Algorithms.Random;
using System;
using System.Numerics;
using System.Threading;

namespace PTSharpCore
{
    public interface Sampler
    {
        Colour Sample(Scene scene, Ray ray, Random rand);
    }
    class DefaultSampler : Sampler
    {

        int FirstHitSamples;
        int MaxBounces;
        bool DirectLighting;
        bool SoftShadows;
        public LightMode LightMode;
        public SpecularMode SpecularMode;
        private object lockObj = new();

        DefaultSampler(int FH, int MB, bool DL, bool SS, LightMode LM, SpecularMode SM)
        {
            FirstHitSamples = FH;
            MaxBounces = MB;
            DirectLighting = DL;
            SoftShadows = SS;
            LightMode = LM;
            SpecularMode = SM;
        }

        public static DefaultSampler NewSampler(int firstHitSamples, int maxBounces)
        {
            return new DefaultSampler(firstHitSamples, maxBounces, true, true, LightMode.LightModeRandom, SpecularMode.SpecularModeNaive);
        }

        DefaultSampler NewDirectSampler()
        {
            return new DefaultSampler(1, 0, true, false, LightMode.LightModeAll, SpecularMode.SpecularModeAll);
        }

        public Colour Sample(Scene scene, Ray ray, Random rand)
        {
            return sample(scene, ray, true, FirstHitSamples, 0, rand);
        }

        public void SetSpecularMode(SpecularMode s)
        {
            SpecularMode = s;
        }

        public void SetLightMode(LightMode l)
        {
            LightMode = l;
        }

        Colour sample(Scene scene, Ray ray, bool emission, int samples, int depth, Random rand, bool russianRoulette = false, double minReflectance = 0.05)
        {
            if (depth > MaxBounces)
            {
                return Colour.Black;
            }

            var hit = scene.Intersect(ray);

            if (!hit.Ok)
            {
                return sampleEnvironment(scene, ray);
            }

            var info = hit.Info(ray);
            var material = info.material;
            var result = Colour.Black;

            if (material.Emittance > 0)
            {
                if (DirectLighting && !emission)
                {
                    return Colour.Black;
                }
                result = result.Add(material.Color.MulScalar(material.Emittance * samples));
            }

            var n = (int)Math.Sqrt(samples);
            BounceType ma, mb;

            if (SpecularMode == SpecularMode.SpecularModeAll || (depth == 0 && SpecularMode == SpecularMode.SpecularModeFirst))
            {
                ma = BounceType.BounceTypeDiffuse;
                mb = BounceType.BounceTypeSpecular;
            }
            else
            {
                ma = BounceType.BounceTypeAny;
                mb = BounceType.BounceTypeAny;
            }

            for (int u = 0; u < n; u++)
            {
                for (int v = 0; v < n; v++)
                {
                    for (BounceType mode = ma; mode <= mb; mode++)
                    {
                        double fu = (u + rand.NextDouble()) / n;
                        double fv = (v + rand.NextDouble()) / n;
                        var (newRay, reflected, p) = ray.Bounce(info, fu, fv, mode, rand);
                        if (mode == BounceType.BounceTypeAny)
                        {
                            p = 1;
                        }
                        if (p > 0 && reflected)
                        {
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1, rand);
                            var tinted = indirect.Mix(material.Color.Mul(indirect), material.Tint);
                            result = result.Add(tinted.MulScalar(p));
                        }
                        if (p > 0 && !reflected)
                        {
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1, rand);
                            var direct = Colour.Black;
                            if (DirectLighting)
                            {
                                direct = sampleLights(scene, info.Ray, rand);
                            }
                            result = result.Add(material.Color.Mul(direct.Add(indirect)).MulScalar(p));
                        }
                    }
                }
            }
            return result.DivScalar(n * n);
        }

        public static Vector RandomUnitVectorOnUnitSphere(Random rand)
        {
            //ref: http://mathworld.wolfram.com/SpherePointPicking.html
            var x0 = rand.NextDouble() * 2 - 1;
            var x1 = rand.NextDouble() * 2 - 1;
            var x2 = rand.NextDouble() * 2 - 1;
            var x3 = rand.NextDouble() * 2 - 1;
            var divider = x0 * x0 + x1 * x1 + x2 * x2 + x3 * x3;
            var pX = 2 * (x1 * x3 + x0 * x2) / divider;
            var pY = 2 * (x2 * x3 - x0 * x1) / divider;
            var pZ = x0 * x0 + x3 * x3 - x1 * x1 - x2 * x2 / divider;
            return new Vector(pX, pY, pZ);
        }

        public static double AngleBetween(Vector a, Vector b)
        {
            return Math.Acos(a.Dot(b) / (a.Length() * b.Length()));
        }

        public static Vector RotateUnitVector(Vector p, Vector a, Vector b)
        {
            a = a.Normalize();
            b = b.Normalize();
            var axis = a.Cross(b).Normalize();
            var angle = AngleBetween(a, b);
            var quaternion = Quaternion.CreateFromAxisAngle(new Vector3((float)axis.X, (float)axis.Y, (float)axis.Z), (float)angle);
            var v = Vector3.Transform(new Vector3((float)p.X, (float)p.Y, (float)p.Z), quaternion);
            return new Vector(v.X, v.Y, v.Z);
        }

        Colour sampleEnvironment(Scene scene, Ray ray)
        {
            if (scene.Texture != null)
            {
                var d = ray.Direction;
                var u = Math.Atan2(d.Z, d.X) + scene.TextureAngle;
                var v = Math.Atan2(d.Y, new Vector(d.X, 0, d.Z).Length());
                u = (u + Math.PI) / (2 * Math.PI);
                v = (v + Math.PI / 2) / Math.PI;
                return scene.Texture.Sample(u, v);
            }
            return scene.Color;
        }
                
        Colour sampleLights(Scene scene, Ray n, Random rand)
        {
            var nLights = scene.Lights.Length;

            if (nLights == 0)
            {
                return Colour.Black;
            }

            if (LightMode == LightMode.LightModeAll)
            {
                Colour result = new Colour();
                foreach (var light in scene.Lights)
                {
                    result = result.Add(sampleLight(scene, n, light, rand));
                }
                return result.DivScalar(nLights);
            }
            else
            {
                // pick a random light
                var lightIndex = Random.Shared.Next(nLights);
                return sampleLight(scene, n, scene.Lights[lightIndex], rand).MulScalar((double)nLights);
            }
        }
        
        Colour sampleLight(Scene scene, Ray n, IShape light, Random rand)
        {
            Vector center;
            double radius;

            switch (light)
            {
                case Sphere sphere:
                    radius = sphere.Radius;
                    center = sphere.Center;
                    break;
                default:
                    Box box = light.BoundingBox();
                    radius = box.OuterRadius();
                    center = box.Center();
                    break;
            }

            var point = center;

            if (SoftShadows)
            {
                while (true)
                {
                    var x = Random.Shared.NextDouble() * 2 - 1;
                    var y = Random.Shared.NextDouble() * 2 - 1;

                    if (x * x + y * y <= 1)
                    {
                        var l = center.Sub(n.Origin).Normalize();
                        var u = l.Cross(Vector.RandomUnitVector(rand)).Normalize();
                        var v = l.Cross(u);
                        point = new Vector();
                        point = point.Add(u.MulScalar(x * radius));
                        point = point.Add(v.MulScalar(y * radius));
                        point = point.Add(center);
                        break;
                    }
                }
            }

            // construct ray toward light point
            Ray ray = new Ray(n.Origin, point.Sub(n.Origin).Normalize());

            // get cosine term
            var diffuse = ray.Direction.Dot(n.Direction);

            if (diffuse <= 0)
            {
                return Colour.Black;
            }

            // check for light visibility
            Hit hit = scene.Intersect(ray);

            if (!hit.Ok || hit.Shape != light)
            {
                return Colour.Black;
            }

            // compute solid angle (hemisphere coverage)
            var hyp = center.Sub(n.Origin).Length();
            var opp = radius;
            var theta = Math.Asin(opp / hyp);
            var adj = opp / Math.Tan(theta);
            var d = Math.Cos(theta) * adj;
            var r = Math.Sin(theta) * adj;
            var coverage = (r * r) / (d * d);

            if (hyp < opp)
            {
                coverage = 1;
            }

            coverage = Math.Min(coverage, 1);

            // get material properties from light
            Material material = Material.MaterialAt(light, point);

            // combine factors
            var m = material.Emittance * diffuse * coverage;

            return material.Color.MulScalar(m);
        }
    }
}
