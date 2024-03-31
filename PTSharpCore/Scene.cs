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
        public IShape[] MaterialLights;
        public ILight[] Lights;

        public Scene() 
        {
            Shapes = Array.Empty<IShape>();
            MaterialLights = Array.Empty<IShape>();
            Lights = Array.Empty<ILight>(); 
            Color = new Colour();
            BackgroundColor = new Colour(0.1, 0.1, 0.1);
        }

        internal void Add(IShape shape)
        {
            Array.Resize(ref Shapes, Shapes.GetLength(0) + 1);
            Shapes[Shapes.GetLength(0) - 1] = shape;
            if (shape.MaterialAt(new Vector()).Emittance > 0)
            {
                Array.Resize(ref MaterialLights, MaterialLights.GetLength(0) + 1);
                MaterialLights[MaterialLights.GetLength(0) - 1] = shape;
            }            
        }

        internal void Add(ILight light)
        {
            Array.Resize(ref Lights, Lights.GetLength(0) + 1);
            Lights[Lights.GetLength(0) - 1] = light;
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

        internal Hit[] IntersectAll(Ray ray)
        {
            var hits = new List<Hit>();
            foreach (var light in MaterialLights)
            {
                var test = light.Intersect(ray);
                if (test != null)
                    hits.Add(test);
            }
            foreach (var light in Lights)
            {
                var test = light.Intersect(ray);
                if (test != null)
                    hits.Add(test);
            }
            hits.Sort((a, b) => a.T.CompareTo(b.T));
            return hits.ToArray();
        }
    }
}
