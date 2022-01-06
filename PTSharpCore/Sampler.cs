using System;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public interface Sampler
    {
        Colour Sample(Scene scene, Ray ray);
    }

    public enum LightMode
    {
        LightModeRandom, LightModeAll
    }

    public enum SpecularMode
    {
        SpecularModeNaive, SpecularModeFirst, SpecularModeAll
    }

    public enum BounceType
    {
        BounceTypeAny, BounceTypeDiffuse, BounceTypeSpecular
    }
    class DefaultSampler : Sampler
    {
        int FirstHitSamples;
        int MaxBounces;
        bool DirectLighting;
        bool SoftShadows;
        public LightMode LightMode;
        public SpecularMode SpecularMode;

        DefaultSampler(int FirstHitSamples, int MaxBounces, bool DirectLighting, bool SoftShadows, LightMode LM, SpecularMode SM)
        {
            this.FirstHitSamples = FirstHitSamples;
            this.MaxBounces = MaxBounces;
            this.DirectLighting = DirectLighting;
            this.SoftShadows = SoftShadows;
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

        public Colour Sample(Scene scene, Ray ray)
        {
            return sample(scene, ray, true, FirstHitSamples, 0);
        }

        public void SetSpecularMode(SpecularMode s)
        {
            SpecularMode = s;
        }

        public void SetLightMode(LightMode l)
        {
            LightMode = l;
        }

        Colour sample(Scene scene, Ray ray, bool emission, int samples, int depth)
        {
            if (depth > MaxBounces)
            {
                return Colour.Black;
            }

            var hit = scene.Intersect(ray);

            if (!hit.Ok())
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

            var n = (int)MathF.Sqrt(samples);
            BounceType ma, mb;

            if (SpecularMode == SpecularMode.SpecularModeAll || depth == 0 && SpecularMode == SpecularMode.SpecularModeFirst)
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

                        var fu = (u + Random.Shared.NextSingle()) / n;
                        var fv = (v + Random.Shared.NextSingle()) / n;
                        (var newRay, var reflected, var p) = ray.Bounce(info, fu, fv, mode);

                        if (mode == BounceType.BounceTypeAny)
                        {
                            p = 1;
                        }

                        if (p > 0 && reflected)
                        {
                            // specular
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1);
                            var tinted = indirect.Mix(material.Color.Mul(indirect), material.Tint);
                            result = result.Add(tinted.MulScalar(p));
                        }

                        if (p > 0 && !reflected)
                        {
                            // diffuse
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1);
                            var direct = Colour.Black;

                            if (DirectLighting)
                            {
                                direct = sampleLights(scene, info.Ray);
                            }
                            result = result.Add(material.Color.Mul(direct.Add(indirect)).MulScalar(p));
                        }

                    }
                }
            }
            return result.DivScalar(n * n);
        }

        Colour sampleEnvironment(Scene scene, Ray ray)
        {
            if (scene.Texture != null)
            {
                var d = ray.Direction;
                var u = MathF.Atan2(d.v.Z, d.v.X) + scene.TextureAngle;
                var v = MathF.Atan2(d.v.Y, new V(d.v.X, 0, d.v.Z).Length());
                u = (u + MathF.PI) / (2 * MathF.PI);
                v = (v + MathF.PI / 2) / MathF.PI;
                return scene.Texture.Sample(u, v);
            }
            return scene.Color;
        }

        Colour sampleLights(Scene scene, Ray n)
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
                    result = result.Add(sampleLight(scene, n, light));
                }
                return result;

            }
            else
            {
                // pick a random light
                var light = scene.Lights[Random.Shared.Next(nLights)];
                return sampleLight(scene, n, light).MulScalar((float)nLights);
            }
        }

        Colour sampleLight(Scene scene, Ray n, IShape light)
        {
            V center;
            float radius;

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
                for (; ; )
                {
                    var x = Random.Shared.NextSingle() * 2 - 1;
                    var y = Random.Shared.NextSingle() * 2 - 1;
                    if (x * x + y * y <= 1)
                    {
                        var l = center.Sub(n.Origin).Normalize();
                        var u = l.Cross(V.RandomUnitVector()).Normalize();
                        var v = l.Cross(u);
                        point = new V();
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
            if (!hit.Ok() || hit.Shape != light)
            {
                return Colour.Black;
            }
            // compute solid angle (hemisphere coverage)
            var hyp = center.Sub(n.Origin).Length();
            var opp = radius;
            var theta = MathF.Asin(opp / hyp);
            var adj = opp / MathF.Tan(theta);
            var d = MathF.Cos(theta) * adj;
            var r = MathF.Sin(theta) * adj;
            var coverage = (r * r) / (d * d);
            if (hyp < opp)
            {
                coverage = 1;
            }
            coverage = MathF.Min(coverage, 1);
            // get material properties from light
            Material material = Material.MaterialAt(light, point);
            // combine factors
            var m = material.Emittance * diffuse * coverage;
            return material.Color.MulScalar(m);
        }
    }
}
