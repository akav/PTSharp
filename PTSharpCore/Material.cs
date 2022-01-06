using System;

namespace PTSharpCore
{
    struct Material
    {
        public Colour Color;
        public ITexture Texture;
        public ITexture NormalTexture;
        public ITexture BumpTexture;
        public ITexture GlossTexture;
        public float BumpMultiplier;
        public float Emittance;
        public float Index;
        public float Gloss;
        public float Tint;
        public float Reflectivity;
        public bool Transparent;

        public Material(Colour color, ITexture texture, ITexture normaltexture, ITexture bumptexture, ITexture glosstexture, float b, float e, float i, float g, float tint, float r, Boolean t)
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

        public static Material SpecularMaterial(Colour color, float index)
        {
            return new Material(color, null, null, null, null, 1, 0, index, 0, 0, -1, false);
        }

        public static Material GlossyMaterial(Colour color, float index, float gloss)
        {
            return new Material(color, null, null, null, null, 1, 0, index, gloss, 0, -1, false);
        }

        public static Material ClearMaterial(float index, float gloss)
        {
            return new Material(new Colour(0, 0, 0), null, null, null, null, 1, 0, index, gloss, 0, -1, true);
        }

        public static Material TransparentMaterial(Colour color, float index, float gloss, float tint)
        {
            return new Material(color, null, null, null, null, 1, 0, index, gloss, tint, -1, true);
        }

        public static Material MetallicMaterial(Colour color, float gloss, float tint)
        {
            return new Material(color, null, null, null, null, 1, 0, 1, gloss, tint, 1, false);
        }

        public static Material LightMaterial(Colour color, float emittance)
        {
            return new Material(color, null, null, null, null, 1, emittance, 1, 0, 0, -1, false);
        }

        internal static Material MaterialAt(IShape shape, V point)
        {
            var material = shape.MaterialAt(point);
            var uv = shape.UV(point);
            if (material.Texture != null)
            {
                material.Color = material.Texture.Sample(uv.v.X, uv.v.Y);
            }
            if (material.GlossTexture != null)
            {
                var c = material.GlossTexture.Sample(uv.v.X, uv.v.Y);
                material.Gloss = (c.r + c.g + c.b) / 3;
             }
            return material;
        }
    };
}
