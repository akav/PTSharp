using glTFLoader.Schema;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
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

        //public IShape[] Shapes;
        public ConcurrentDictionary<IShape, (Matrix Transform, Material Material)> Shapes;
        public List<IShape> Lights;
        //public IShape[] Lights;

        public Scene() 
        {
            Shapes = new();
            //Shapes = Array.Empty<IShape>(); 
            //Lights = Array.Empty<IShape>(); 
            Lights = new();
            Color = new Colour();
            BackgroundColor = new Colour(0.1, 0.1, 0.1);
        }

        public void AddInstance(IShape geometry, Matrix transform, Material material)
        {
            // Create a new instance of the geometry with its transformation matrix and material
            var instanceGeometry = new TransformedShape(geometry, transform, transform.Inverse());

            // Add the geometry instance to the lookup table
            Shapes.TryAdd(geometry, (transform, material));

            // Check if the material is a light source and add it to the Lights list if needed
            if (material.Emittance > 0)
            {
                Lights.Add(geometry);
            }

            Compile();
        }

        internal void Add(IShape p)
        {
            // Add the geometry instance to the lookup table
            Shapes.TryAdd(p, (Matrix.Identity, p.MaterialAt(new())));
            
            if (p.MaterialAt(new Vector()).Emittance > 0)
            {
                Lights.Add(p);
            }
        }

        public void Compile()
        {
            // Create a list to hold all shapes, including instances
            var allShapes = new List<IShape>();

            // Add all shapes from the Shapes dictionary
            foreach (var kvp in Shapes)
            {
                allShapes.Add(kvp.Key);
            }

            // Add all lights from the Lights list
            allShapes.AddRange(Lights);

            // Parallel compilation of all shapes
            Parallel.ForEach(allShapes, shape =>
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
                        tree = Tree.NewTree(allShapes.ToArray());
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
