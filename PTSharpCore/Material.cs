using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using System;
using System.Numerics;

namespace PTSharpCore
{       
    public struct Material
    {
        // Color of the material
        public Colour Color { get; set; }

        // Texture of the material
        public ITexture Texture { get; set; }

        // Normal map of the material
        public ITexture NormalTexture { get; set; }

        // Bump map of the material
        public ITexture BumpTexture { get; set; }

        // Gloss map of the material
        public ITexture GlossTexture { get; set; }

        // Multiplier for the bump map
        public double BumpMultiplier { get; set; }

        // Emittance of the material
        public double Emittance { get; set; }

        // Index of refraction of the material
        public double Index { get; set; }

        // Gloss of the material
        public double Gloss { get; set; }

        // Tint of the material
        public double Tint { get; set; }

        // Reflectivity of the material
        public double Reflectivity { get; set; }

        // Flag indicating if the material is transparent
        public bool Transparent { get; set; }

        public IDistribution Distribution { get; set; }

        public Material() { }

        public Material(Colour color, ITexture texture, ITexture normaltexture, ITexture bumptexture, ITexture glosstexture, double b, double e, double i, double g, double tint, double r, Boolean t)
        {
            Color = color;
            Texture = texture;
            NormalTexture = normaltexture;
            BumpTexture = bumptexture;
            GlossTexture = glosstexture;
            BumpMultiplier = b;
            Emittance = e;
            Index = i;
            Gloss = g;
            Tint = tint;
            Reflectivity = r;
            Transparent = t;
        }

        public static Material DiffuseMaterial(Colour color)
        {
            return new Material(color, null, null, null, null, 1, 0, 1, 0, 0, -1, false);
        }

        public static Material SpecularMaterial(Colour color, double index)
        {
            return new Material(color, null, null, null, null, 1, 0, index, 0, 0, -1, false);
        }

        public static Material GlossyMaterial(Colour color, double index, double gloss)
        {
            return new Material(color, null, null, null, null, 1, 0, index, gloss, 0, -1, false);
        }

        public static Material ClearMaterial(double index, double gloss)
        {
            return new Material(new Colour(0, 0, 0), null, null, null, null, 1, 0, index, gloss, 0, -1, true);
        }

        public static Material TransparentMaterial(Colour color, double index, double gloss, double tint)
        {
            return new Material(color, null, null, null, null, 1, 0, index, gloss, tint, -1, true);
        }

        public static Material MetallicMaterial(Colour color, double gloss, double tint)
        {
            return new Material(color, null, null, null, null, 1, 0, 1, gloss, tint, 1, false);
        }

        public static Material LightMaterial(Colour color, double emittance)
        {
            return new Material(color, null, null, null, null, 1, emittance, 1, 0, 0, -1, false);
        }

        internal static Material MaterialAt(IShape shape, Vector point)
        {
            var material = shape.MaterialAt(point);
            var uv = shape.UVector(point);
            if (material.Texture != null)
            {
                material.Color = material.Texture.Sample(uv.X, uv.Y);
            }
            if (material.GlossTexture != null)
            {
                var c = material.GlossTexture.Sample(uv.X, uv.Y);
                material.Gloss = (c.r + c.g + c.b) / 3;
            }
            return material;
        }
        
        // Generates a random direction in the hemisphere centered around a given normal
        private Vector RandomDirection(Vector normal)
        {
            // Generate a random point on the unit sphere
            var u = Random.Shared.NextDouble();
            var v = Random.Shared.NextDouble();
            var theta = 2 * Math.PI * u;
            var phi = Math.Acos(2 * v - 1);
            var x = Math.Sin(phi) * Math.Cos(theta);
            var y = Math.Sin(phi) * Math.Sin(theta);
            var z = Math.Cos(phi);

            // Transform the point to the hemisphere centered around the normal
            Vector point = new Vector(x, y, z);
            return Transform(point, FromAxisAngle(normal, 0));

        }
                
        public static Matrix4x4 FromAxisAngle(Vector axis, double angle)
        {
            // Normalize the axis
            axis = axis.Normalize();

            // Calculate the sine and cosine of the angle
            float cos = MathF.Cos((float)angle);
            float sin = MathF.Sin((float)angle);

            // Calculate the elements of the matrix
            double m11 = cos + axis.X * axis.X * (1 - cos);
            double m12 = axis.X * axis.Y * (1 - cos) - axis.Z * sin;
            double m13 = axis.X * axis.Z * (1 - cos) + axis.Y * sin;
            double m14 = 0;
            double m21 = axis.Y * axis.X * (1 - cos) + axis.Z * sin;
            double m22 = cos + axis.Y * axis.Y * (1 - cos);
            double m23 = axis.Y * axis.Z * (1 - cos) - axis.X * sin;
            double m24 = 0;
            double m31 = axis.Z * axis.X * (1 - cos) - axis.Y * sin;
            double m32 = axis.Z * axis.Y * (1 - cos) + axis.X * sin;
            double m33 = cos + axis.Z * axis.Z * (1 - cos);
            double m34 = 0;
            double m41 = 0;
            double m42 = 0;
            double m43 = 0;
            double m44 = 1;

            // Return the matrix
            return new Matrix4x4((float)m11, (float)m12, (float)m13, (float)m14, (float)m21, (float)m22, (float)m23, (float)m24, (float)m31, (float)m32, (float)m33, (float)m34, (float)m41, (float)m42, (float)m43, (float)m44);
        }

        Vector Transform(Vector v, Matrix4x4 m)
        {
            return new Vector(
              (v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + m.M41),
              (v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + m.M42),
              (v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + m.M43)
            );
        }
        public Vector UVector(Vector position)
        {
            // Calculate the polar angle of the position vector
            double phi = Math.Atan2(position.Z, position.X);

            // Calculate the azimuthal angle of the position vector
            double theta = Math.Asin(position.Y);

            // Map the polar and azimuthal angles to the range [0, 1]
            double u = 1 - (phi + Math.PI) / (2 * Math.PI);
            double v = (theta + Math.PI / 2) / Math.PI;

            // Return the UV coordinate
            return new Vector(u, v, 0);
        }

        public Vector NormalAt(Vector position)
        {
            // If the normal vector is stored in a normal map, use the UVector method to map the position
            // to a UV coordinate, and then sample the normal map using the Sample method of the ITexture interface
            if (NormalTexture != null)
            {
                Vector uv = UVector(position);
                return NormalTexture.Sample(uv.X, uv.Y).ToVector();
            }

            // If the normal vector is being calculated procedurally, use the position value to calculate the normal vector
            // This could involve using a noise function or some other algorithm to generate the normal vector
            return CalculateNormal(position);
        }

        Vector CalculateNormal(Vector position)
        {
            // Get the UV coordinates of the position
            Vector uv = this.UVector(position);

            // Sample the normal texture to get the normal vector at the position
            Vector normal = NormalTexture.Sample(uv.X, uv.Y).ToVector();

            // If there is a bump texture, use it to perturb the normal vector
            if (this.BumpTexture != null)
            {
                // Sample the bump texture to get the bump value at the position
                double bump = BumpTexture.Sample(uv.X, uv.Y).r * BumpMultiplier;

                // Calculate the perturbation vector using the bump value and the position
                Vector perturb = position * bump;

                // Perturb the normal vector
                normal = (normal + perturb).Normalize();
            }

            return normal;
        }       
    }
};

