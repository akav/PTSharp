using System;
using System.Collections.Generic;

namespace PTSharpCore
{
    public class Scene
    {
        internal Colour Color = new Colour();
        internal ITexture Texture = null;
        internal double TextureAngle = 0;
        private Tree tree;
        internal int rays = 0;

        internal IShape[] Shapes; 
        internal IShape[] Lights;

        public Scene() 
        {
            Shapes = new IShape[0];
            Lights = new IShape[0];        
        }

        internal void Add(IShape p)
        {
            Array.Resize(ref Shapes, Shapes.GetLength(0) + 1);
            Shapes[Shapes.GetLength(0) - 1] = p;
            if (p.MaterialAt(new Vector()).Emittance > 0)
            {
                Array.Resize(ref Lights, Lights.GetLength(0) + 1);
                Lights[Lights.GetLength(0) - 1] = p;
            }
        }

        public void Compile()
        {
            foreach(IShape shape in Shapes)
            {
                shape.Compile();
            }
            if (tree is null)
            {
                tree = Tree.NewTree(Shapes);
            }
        }

        int RayCount()
        {
            return rays;
        }
        
        internal Hit Intersect(Ray r)
        {
            rays++;
            return tree.Intersect(r);
        }
    }
}
