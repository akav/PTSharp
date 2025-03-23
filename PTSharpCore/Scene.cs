using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        internal void AddRange(IEnumerable<IShape> shapes)
        {
            foreach (var shape in shapes)
            {
                Add(shape);
            }
        }

        public void Compile()
        {
            // Parallel compilation of shapes
            Parallel.ForEach(Shapes, shape =>
            {
                shape.Compile();
            });

            // Check if the tree has already been instantiated
            if (tree == null)
            {
                // Using a lock to ensure thread safety when instantiating the tree
                lock (this)
                {
                    if (tree == null) // Double-checking to prevent race conditions
                    {
                        tree = Tree.NewTree(Shapes);
                    }
                }
            }
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
