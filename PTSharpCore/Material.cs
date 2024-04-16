using MathNet.Numerics.Distributions;
using System;
using System.Runtime.InteropServices;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Auto, Pack = 1)]
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

        public static Material GenerateRandomMaterial()
        {
            Colour color = GenerateRandomColor();
            ITexture texture = null; 
            ITexture normalTexture = null; 
            ITexture bumpTexture = null; 
            ITexture glossTexture = null;
            double bumpMultiplier = Random.Shared.NextDouble();
            double emittance = Random.Shared.NextDouble();
            double index = Random.Shared.NextDouble();
            double gloss = Random.Shared.NextDouble();
            double tint = Random.Shared.NextDouble();         
            double reflectivity = -1;
            bool transparent = false;        
            return new Material(color, texture, normalTexture, bumpTexture, glossTexture, bumpMultiplier, emittance, index, gloss, tint, reflectivity, transparent);
        }

        public static Colour GenerateRandomColor()
        {
            var r = Random.Shared.NextDouble();
            var g = Random.Shared.NextDouble();
            var b = Random.Shared.NextDouble();
            return new Colour(r, g, b);
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
    }
};

