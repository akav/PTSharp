using System;
using System.Collections.Generic;
using System.Threading;

namespace PTSharpCore
{
    public class Scene
    {
        public Colour Color;
        public ITexture Texture = null;
        public double TextureAngle = 0;
        public Tree tree;
        public int rays = 0;
        public Colour BackgroundColor;

        public IShape[] Shapes;
        public IShape[] Lights;
        public IShape[] PhysicalLights;

        public Scene() 
        {
            Shapes = Array.Empty<IShape>(); 
            Lights = Array.Empty<IShape>(); 
            Color = new Colour();
            BackgroundColor = new Colour(0.1, 0.1, 0.1);
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
            tree ??= Tree.NewTree(Shapes);
        }

        int RayCount()
        {
            return rays;
        }
        
        internal Hit Intersect(Ray r)
        {
            Interlocked.Increment(ref rays);
            return tree.Intersect(r);
        }
    }
}
