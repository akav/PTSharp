using System;
using System.Collections.Generic;
using System.Threading;

namespace PTSharpCore
{
    public class Scene
    {
        public Colour Color;
        internal ITexture Texture = null;
        internal double TextureAngle = 0;
        private Tree tree;
        internal int rays = 0;

        internal IShape[] Shapes; 
        internal IShape[] Lights;

        public Scene() 
        {
            Shapes = Array.Empty<IShape>(); // new IShape[0];
            Lights = Array.Empty<IShape>(); //new IShape[0];
            Color = new Colour();                                            
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
            //rays++;
            return tree.Intersect(r);
        }
    }
}
