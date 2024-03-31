using ILGPU.Algorithms.Random;
using Microsoft.VisualBasic;
using System;
using System.Numerics;
using System.Threading;
using static ILGPU.IR.Analyses.Uniforms;

namespace PTSharpCore
{
    public interface Sampler
    {
        Colour Sample(Scene scene, Ray ray, Random rand);
    }
    class DefaultSampler : Sampler
    {
        public int FirstHitSamples;
        public int MaxBounces;
        public bool DirectLighting;
        public bool SoftShadows;
        public LightMode LightMode;
        public SpecularMode SpecularMode;

        public DefaultSampler(int firstHitSamples, int maxBounces, bool directLighting, bool softShadows, LightMode lightMode, SpecularMode specularMode)
        {
            FirstHitSamples = firstHitSamples;
            MaxBounces = maxBounces;
            DirectLighting = directLighting;
            SoftShadows = softShadows;
            LightMode = lightMode;
            SpecularMode = specularMode;
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
            return SampleInternal(scene, ray, true, FirstHitSamples, 0, rand);
        }
        
        public void SetSpecularMode(SpecularMode s)
        {
            SpecularMode = s;
        }

        public void SetLightMode(LightMode l)
        {
            LightMode = l;
        }

        Colour SampleInternal(Scene scene, Ray ray, bool emission, int samples, int depth, Random rand, bool russianRoulette = false, double minReflectance = 0.05)
        {
            if (depth > MaxBounces)
            {
                return Colour.Black;
            }

            var hit = scene.Intersect(ray);

            if (!hit.Ok)
            {
                return SampleEnvironment(scene, ray);
            }

            var info = hit.Info(ray);
            var material = info.material;
            var result = new Colour(0, 0, 0);

            if (material.Emittance > 0 && emission)
            {
                result = result.Add(material.Color.MulScalar(material.Emittance * samples));
            }

            var n = (int)Math.Sqrt(samples);
            BounceType ma, mb;

            if (SpecularMode.Equals(SpecularMode.SpecularModeAll) || depth == 0 && SpecularMode.Equals(SpecularMode.SpecularModeFirst))
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
                        (var newRay, var reflected, var p) = ray.Bounce(info, ((double)u + rand.NextDouble()) / (double)n, ((float)v + rand.NextDouble()) / (double)n, mode, rand);

                        if (mode.Equals(BounceType.BounceTypeAny))
                            p = 1;

                        if (p > 0 && reflected)
                        {
                            var indirect = SampleInternal(scene, newRay, reflected, 1, depth + 1, rand, russianRoulette, minReflectance);
                            var tinted = indirect.Mix(material.Color.Mul(indirect), material.Tint);
                            result = result.Add(tinted.MulScalar(p));
                        }

                        if (p > 0 && !reflected)
                        {
                            var indirect = SampleInternal(scene, newRay, reflected, 1, depth + 1, rand, russianRoulette, minReflectance);
                            var direct = SampleLights(scene, newRay, rand, depth);
                            result = result.Add(material.Color.Mul(direct.Add(indirect)).MulScalar(p));
                        }
                    }
                }
            }

            if (russianRoulette && depth >= 2)
            {
                var probability = Math.Max(result.r, Math.Max(result.g, result.b));
                if (rand.NextDouble() > probability)
                    return result.DivScalar(probability);
                return result.DivScalar(probability * (1 - minReflectance));
            }

            return result.DivScalar(n * n);
        }


        /*
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
            var result = new Colour(0, 0, 0);

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

            if (SpecularMode.Equals(SpecularMode.SpecularModeAll) || depth == 0 && SpecularMode.Equals(SpecularMode.SpecularModeFirst))
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
                        (var newRay, var reflected, var p) = ray.Bounce(info, ((double)u + Random.Shared.NextDouble()) / (double)n, ((float)v + Random.Shared.NextDouble()) / (double)n, mode, Random.Shared);

                        if (mode.Equals(BounceType.BounceTypeAny))
                        {
                            p = 1;
                        }

                        if (p > 0 && reflected)
                        {
                            // specular
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1, Random.Shared, russianRoulette, minReflectance);
                            var tinted = indirect.Mix(material.Color.Mul(indirect), material.Tint);
                            result = result.Add(tinted.MulScalar(p));
                        }

                        if (p > 0 && !reflected)
                        {
                            // diffuse
                            var indirect = sample(scene, newRay, reflected, 1, depth + 1, Random.Shared, russianRoulette, minReflectance);
                            var direct = Colour.Black;

                            if (DirectLighting)
                            {
                                direct = sampleLights(scene, info.Ray, rand);
                            }
                            else
                            {
                                // If DirectLighting is disabled, still consider direct contribution from all light types
                                foreach (var light in scene.Lights)
                                {
                                    switch (light.Type)
                                    {
                                        case LightMode.LightModePoint:
                                            direct = direct.Add(samplePointLight(scene, info.Ray, (PointLight)light));
                                            break;
                                        case LightMode.LightModeDirectional:
                                            direct = direct.Add(sampleDirectionalLight(scene, info.Ray, (DirectionalLight)light));
                                            break;
                                        case LightMode.LightModeSpot:
                                            direct = direct.Add(sampleSpotLight(scene, info.Ray, (SpotLight)light));
                                            break;
                                        case LightMode.LightModeArea:
                                            direct = direct.Add(sampleAreaLight(scene, info.Ray, (AreaLight)light));
                                            break;
                                            // Add cases for other light types as needed
                                    }
                                }
                            }

                            result = result.Add(material.Color.Mul(direct.Add(indirect)).MulScalar(p));
                        }
                    }
                }
            }

            if (russianRoulette && depth >= 2)
            {
                // Russian Roulette termination
                var probability = Math.Max(result.r, Math.Max(result.g, result.b));
                if (Random.Shared.NextDouble() > probability)
                {
                    return result.DivScalar(probability);
                }
                return result.DivScalar(probability * (1 - minReflectance));
            }

            return result.DivScalar(n * n);
        }*/

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

        Colour SampleEnvironment(Scene scene, Ray ray)
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

        Colour SampleLights(Scene scene, Ray n, Random rand, int depth)
        {
            var nMaterialLights = scene.MaterialLights.Length;
            var nLights = scene.Lights.Length;

            if (nMaterialLights == 0 && nLights == 0)
                return Colour.Black;

            var result = new Colour();

            foreach (var light in scene.MaterialLights)
                result = result.Add(SampleLight(scene, n, light, rand, depth));

            foreach (var light in scene.Lights)
            {
                switch (light.Type)
                {
                    case LightMode.LightModePoint:
                        var directionToLight = ((PointLight)light).Position.Sub(n.Origin);
                        var rayToLight = new Ray(n.Origin, directionToLight.Normalize());
                        result = result.Add(SamplePointLight(scene, rayToLight, (PointLight)light, depth));
                        break;
                    case LightMode.LightModeDirectional:
                        result = result.Add(SampleDirectionalLight(scene, n, (DirectionalLight)light, depth));
                        break;
                    case LightMode.LightModeSpot:
                        result = result.Add(SampleSpotLight(scene, n, (SpotLight)light, depth));
                        break;
                    case LightMode.LightModeArea:
                        result = result.Add(SampleAreaLight(scene, n, (AreaLight)light, depth));
                        break;
                }
            }

            return result.DivScalar(nMaterialLights + nLights);
        }        
        private Colour SampleAreaLight(Scene scene, Ray n, AreaLight areaLight, int depth)
        {
            throw new NotImplementedException();
        }

        private Colour SampleSpotLight(Scene scene, Ray n, SpotLight spotLight, int depth)
        {
            // Calculate direction to the light source and its distance squared
            var directionToLight = spotLight.Position.Sub(n.Origin);
            var distanceSquared = directionToLight.LengthSquared();

            // Normalize the direction vector
            var direction = directionToLight.Normalize();

            // Calculate attenuation based on the inverse square law
            var attenuation = 1.0 / distanceSquared;

            // Create a ray towards the light source
            var rayToLight = new Ray(n.Origin, direction);

            // Check if the spotlight is occluded by any objects
            var occlusionHit = scene.Intersect(rayToLight);
            if (occlusionHit.Ok && occlusionHit.T * occlusionHit.T < distanceSquared)
            {
                // The spotlight is occluded, return black color
                return Colour.Black;
            }

            // Normalize the spotlight direction vector
            var normalizedSpotlightDirection = spotLight.Direction.Normalize();

            // Calculate the cosine of the angle between the light direction and the spotlight direction
            var cosAngle = direction.Dot(normalizedSpotlightDirection);

            // Check if the light direction is within the cone angle
            if (cosAngle >= Math.Cos(spotLight.Angle / 2.0))
            {
                // The intersection point is inside the spotlight cone, proceed with intensity calculation
            }
            else
            {
                // The intersection point is outside the spotlight cone, return black color
                return Colour.Black;
            }

            // Calculate light intensity based on the inverse square law and cosine of the angle
            var intensity = spotLight.Intensity * attenuation * cosAngle;

            // Intersect with the original ray to get hit information
            var hit = scene.Intersect(n);
            if (!hit.Ok || hit.HitInfo == null)
            {
                // If no intersection or HitInfo is null, return black color
                return Colour.Black;
            }

            // Calculate diffuse term using the dot product between light direction and surface normal
            var diffuse = direction.Dot(hit.HitInfo.Normal);
            if (diffuse <= 0)
            {
                // If the diffuse term is non-positive, return black color
                return Colour.Black;
            }

            // Calculate material at the intersection point on the surface
            var intersectionMaterial = Material.MaterialAt(hit.Shape, hit.HitInfo.Position);

            // Compute color contribution
            var m = intersectionMaterial.Emittance * diffuse * intensity;

            return intersectionMaterial.Color.MulScalar(m);
        }
        
        bool IsOccluded(Scene scene, Ray ray, double maxDistance)
        {
            var occlusionHit = scene.Intersect(ray);
            return occlusionHit.Ok && occlusionHit.T * occlusionHit.T < maxDistance;
        }

        private Colour SamplePointLight(Scene scene, Ray n, PointLight light, int depth)
        {
            if (depth > MaxBounces)
                return Colour.Black;

            var directionToLight = light.Position.Sub(n.Origin);
            var distance = directionToLight.Length();
            directionToLight = directionToLight.Normalize();

            var shadow = SoftShadows ? SampleShadow(scene, new Ray(n.Origin, directionToLight), distance) : 1;

            var attenuation = Math.Max(0, Vector.Dot(n.Direction, directionToLight)) / (distance * distance);
            return light.Color.MulScalar(attenuation * shadow);
        }

        double SampleShadow(Scene scene, Ray ray, double distance)
        {
            var hits = scene.IntersectAll(ray);
            double shadow = 1;
            foreach (var hit in hits)
            {
                if (hit.T < distance)
                    shadow *= hit.Shape.MaterialAt(hit.HitInfo.Position).Transparent ? 1 : 0;
            }
            return shadow;
        }

        Colour SampleDirectionalLight(Scene scene, Ray n, DirectionalLight light, int depth)
        {
            if (depth > MaxBounces)
                return Colour.Black;

            var shadow = SoftShadows ? SampleShadow(scene, new Ray(n.Origin, light.Direction.Negate()), float.PositiveInfinity) : 1;

            var attenuation = Math.Max(0, Vector.Dot(n.Direction, light.Direction.Negate()));
            return light.Color.MulScalar(attenuation * shadow);
        }


        //Colour samplePointLight(Scene scene, Ray n, PointLight light)
        //{
        //    var directionToLight = light.Position.Sub(n.Origin);
        //    var distanceSquared = directionToLight.LengthSquared();
        //    var direction = directionToLight.Normalize();
        //    var attenuation = 1.0 / distanceSquared;

        //    var rayToLight = new Ray(n.Origin, direction);

        //    // Check if the point light is occluded by any objects
        //    var occlusionHit = scene.Intersect(rayToLight);
        //    if (occlusionHit.Ok && occlusionHit.T * occlusionHit.T < distanceSquared)
        //    {
        //        // The point light is occluded, return black color
        //        return Colour.Black;
        //    }

        //    // Calculate light intensity based on the inverse square law
        //    var intensity = light.Intensity * attenuation;

        //    // Intersect with the original ray to get hit information
        //    var hit = scene.Intersect(n);

        //    // Check if HitInfo is null or intersection failed
        //    if (!hit.Ok || hit.HitInfo == null)
        //    {
        //        // If intersection failed, return light contribution based on intensity
        //        return light.Color.MulScalar(intensity);
        //    }

        //    // Calculate diffuse term using the dot product between light direction and surface normal
        //    var diffuse = direction.Dot(hit.HitInfo.Normal);
        //    if (diffuse <= 0)
        //        return Colour.Black;

        //    // Calculate material at the intersection point on the surface
        //    var intersectionMaterial = Material.MaterialAt(hit.Shape, hit.HitInfo.Position);

        //    if (intersectionMaterial.Transparent)
        //    {
        //        // Compute the refracted ray direction based on Snell's law
        //        var refractedDirection = direction.Refract(hit.HitInfo.Normal, 1.0, intersectionMaterial.Index);
        //        var refractedRay = new Ray(hit.HitInfo.Position, refractedDirection);

        //        // Recursively calculate color contribution using refracted ray
        //        var transparency = samplePointLight(scene, refractedRay, light);

        //        // Apply transparency and return the result
        //        return transparency;
        //    }

        //    // Compute color contribution with diffuse term
        //    var m = intersectionMaterial.Emittance * diffuse * intensity;
        //    return intersectionMaterial.Color.MulScalar(m);
        //}


        Colour SampleLight(Scene scene, Ray n, IShape light, Random rand, int depth)
        {

            Vector center;
            double radius;

            switch (light)
            {
                case Sphere sphere:
                    radius = sphere.Radius;
                    center = sphere.Center;
                    break;
                case Cylinder cylinder:
                    // For cylinders, consider the radius and top/bottom positions
                    radius = cylinder.Radius;
                    center = new Vector(0, 0, (cylinder.Z0 + cylinder.Z1) / 2);
                    break;
                default:
                    var box = light.BoundingBox();
                    radius = box.OuterRadius();
                    center = box.Center();
                    break;
            }

            var point = center;

            /*
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
                        point = center.Add(u.MulScalar(x * radius)).Add(v.MulScalar(y * radius));
                        break;
                    }
                }
            }*/

            if (SoftShadows)
            {
                var l = center.Sub(n.Origin).Normalize();
                var u = l.Cross(Vector.RandomUnitVector(rand)).Normalize();
                var v = l.Cross(u);

                var validPoint = false;
                point = Vector.Zero;

                while (!validPoint)
                {
                    var x = rand.NextDouble() * 2 - 1;
                    var y = rand.NextDouble() * 2 - 1;
                    var r2 = x * x + y * y;

                    if (r2 <= 1)
                    {
                        point = center.Add(u.MulScalar(x * radius)).Add(v.MulScalar(y * radius));
                        validPoint = true;
                    }
                }
            }

            var rayDirection = point.Sub(n.Origin).Normalize();
            var diffuse = rayDirection.Dot(n.Direction);

            if (diffuse <= 0)
                return Colour.Black;

            var ray = new Ray(n.Origin, rayDirection);
            var hit = scene.Intersect(ray);

            if (!hit.Ok || hit.Shape != light)
                return Colour.Black;

            // Calculate coverage for cylinder
            double coverage;
            if (light is Cylinder)
            {
                // Adjust coverage calculation for cylinders
                // You may need to refine this calculation based on the specific geometry of your cylinder
                coverage = 1.0; // Placeholder value, adjust as needed
            }
            else
            {
                // Calculate coverage for other light sources
                var hyp = center.Sub(n.Origin).Length();
                var theta = Math.Asin(radius / hyp);
                var adj = radius / Math.Tan(theta);
                var d = Math.Cos(theta) * adj;
                var r = Math.Sin(theta) * adj;
                coverage = (r * r) / (d * d);

                if (hyp < radius)
                    coverage = 1;

                coverage = Math.Min(coverage, 1);
            }

            // Compute color contribution
            var material = Material.MaterialAt(light, point);
            var m = material.Emittance * diffuse * coverage;

            return material.Color.MulScalar(m);
        }
    }
}
