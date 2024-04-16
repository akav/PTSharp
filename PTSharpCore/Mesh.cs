using System;
using System.Collections.Generic;

namespace PTSharpCore
{
    public struct Mesh : IShape
    {
        public Triangle[] Triangles;
        Box box;
        Tree tree;
        public Colour Color { get; set; }
        public Vector Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Mesh() { }
        
        public Mesh(Triangle[] triangles_, Box box_, Tree tree_)
        {
            Triangles = triangles_;
            box = box_;
            tree = tree_;
        }
        
        public static Mesh NewMesh(Triangle[] triangles)
        {
            return new Mesh(triangles, null, null);
        }
        
        void dirty()
        {
            box = null;
            tree = null;
        }
        
        Mesh Copy()
        {
            Triangle[] triangle = new Triangle[Triangles.Length];

            for (int i = 0; i < Triangles.Length; i++)
            {
                triangle[i] = Triangles[i];
            }
            return NewMesh(triangle);
        }

        void IShape.Compile()
        {
            if (tree is null)
            {
                var shapes = new IShape[Triangles.Length];

                for (int i=0; i<Triangles.Length; i++)
                {
                    shapes[i] = Triangles[i];
                }
                tree = Tree.NewTree(shapes);
            }
        }

        internal void Compile()
        {
            if (tree is null)
            {
                List<IShape> shapes = new List<IShape>();

                foreach(var triangle in Triangles)
                {
                    shapes.Add(triangle);
                }

                tree = Tree.NewTree(shapes.ToArray());
            }
        }

        void Add(Mesh b)
        {
            Triangle[] all = new Triangle[Triangles.Length + b.Triangles.Length];
            Array.Copy(Triangles, all, Triangles.Length);
            Array.Copy(b.Triangles, 0, all, Triangles.Length, b.Triangles.Length);
            Triangles = all;
            dirty();
        }

        internal Hit Intersect(Ray r)
        {
            return tree.Intersect(r);
        }
        
        Box IShape.BoundingBox()
        {
            if (box is null)
            {
                Vector min = Triangles[0].V1;
                Vector max = Triangles[0].V1;

                foreach (var t in Triangles)
                {
                    min = min.Min(t.V1).Min(t.V2).Min(t.V3);
                    max = max.Max(t.V1).Max(t.V2).Max(t.V3);
                }
                box = new Box(min, max);
            }
            return box;
        }
        
        internal Box BoundingBox()
        {
            if (box is null)
            {
                var min = Triangles[0].V1;
                var max = Triangles[0].V1;

                foreach (Triangle t in Triangles)
                {
                    min = min.Min(t.V1).Min(t.V2).Min(t.V3);
                    max = max.Max(t.V1).Max(t.V2).Max(t.V3);
                }
                box = new Box(min, max);
            }
            return box;
        }

        Hit IShape.Intersect(Ray r)
        {
            return tree.Intersect(r);
        }

        Vector IShape.UVector(Vector p)
        {
            return new Vector();
        }

        Material IShape.MaterialAt(Vector p)
        {
            return new Material();
        }

        Vector IShape.NormalAt(Vector p)
        {
            return new Vector();
        }

        Vector smoothNormalsThreshold(Vector normal, Vector[] normals, double threshold)
        {
            Vector result = new Vector();
            foreach (Vector x in normals)
            {
                if (x.Dot(normal) >= threshold)
                {
                    result = result.Add(x);
                }
            }
            return result.Normalize();
        }

        internal void SmoothNormalsThreshold(double radians)
        {
            double threshold = Math.Cos(radians);
            
            List<Vector> NL1 = new List<Vector>();
            List<Vector> NL2 = new List<Vector>();
            List<Vector> NL3 = new List<Vector>();

            Dictionary<Vector, Vector[]> lookup = new Dictionary<Vector, Vector[]>();
            
            foreach (Triangle t in Triangles)
            {
                NL1.Add(t.N1);
                NL2.Add(t.N2);
                NL3.Add(t.N3);

                lookup[t.V1] = NL1.ToArray();
                lookup[t.V2] = NL2.ToArray();
                lookup[t.V3] = NL3.ToArray();
            }

            // Create a copy of Triangles array to update normals
            Triangle[] trianglesCopy = new Triangle[Triangles.Length];
            Triangles.CopyTo(trianglesCopy, 0);

            for (int i = 0; i < trianglesCopy.Length; i++)
            {
                trianglesCopy[i].N1 = smoothNormalsThreshold(trianglesCopy[i].N1, lookup[trianglesCopy[i].V1], threshold);
                trianglesCopy[i].N2 = smoothNormalsThreshold(trianglesCopy[i].N2, lookup[trianglesCopy[i].V2], threshold);
                trianglesCopy[i].N3 = smoothNormalsThreshold(trianglesCopy[i].N3, lookup[trianglesCopy[i].V3], threshold);
            }

            // Replace Triangles array with the updated copy
            Triangles = trianglesCopy;
        }

        public void SmoothNormals()
        {
            Dictionary<Vector, Vector> lookup = new Dictionary<Vector, Vector>();

            foreach (var t in Triangles)
            {
                lookup[t.V1] = new Vector();
                lookup[t.V2] = new Vector();
                lookup[t.V3] = new Vector();
            }

            foreach (var t in Triangles)
            {
                lookup[t.V1] = lookup[t.V1].Add(t.N1);
                lookup[t.V2] = lookup[t.V2].Add(t.N2);
                lookup[t.V3] = lookup[t.V3].Add(t.N3);
            }

            Dictionary<Vector, Vector> lookup2 = new Dictionary<Vector, Vector>();

            foreach (KeyValuePair<Vector, Vector> p in lookup)
            {
                lookup2[p.Key] = lookup[p.Key].Normalize();
            }

            // Create a copy of Triangles array to update normals
            Triangle[] trianglesCopy = new Triangle[Triangles.Length];
            Triangles.CopyTo(trianglesCopy, 0);

            for (int i = 0; i < trianglesCopy.Length; i++)
            {
                trianglesCopy[i].N1 = lookup2[trianglesCopy[i].V1];
                trianglesCopy[i].N2 = lookup2[trianglesCopy[i].V2];
                trianglesCopy[i].N3 = lookup2[trianglesCopy[i].V3];
            }

            // Replace Triangles array with the updated copy
            Triangles = trianglesCopy;
        }

        void UnitCube()
        {
            FitInside(new Box(new Vector(0, 0, 0), new Vector(1, 1, 1)), new Vector(0, 0, 0));
            MoveTo(new Vector(0, 0, 0), new Vector(0.5, 0.5, 0.5));
        }

        public void MoveTo(Vector position, Vector anchor)
        {
            Matrix matrix = new Matrix().Translate(position.Sub(BoundingBox().Anchor(anchor)));
            Transform(matrix);
        }

        internal void FitInside(Box box, Vector anchor)
        {
            var scale = box.Size().Div(BoundingBox().Size()).MinComponent();
            var extra = box.Size().Sub(BoundingBox().Size().MulScalar(scale));
            var matrix = Matrix.Identity;
            matrix = matrix.Translate(BoundingBox().Min.Negate()).Mul(matrix);
            matrix = matrix.Scale(new Vector(scale, scale, scale)).Mul(matrix);
            matrix = matrix.Translate(box.Min.Add(extra.Mul(anchor))).Mul(matrix);
            Transform(matrix);
        }

        internal void Transform(Matrix matrix)
        {
            // Create a copy of Triangles array to update vertices and normals
            Triangle[] trianglesCopy = new Triangle[Triangles.Length];
            Triangles.CopyTo(trianglesCopy, 0);

            for (int i = 0; i < trianglesCopy.Length; i++)
            {
                trianglesCopy[i].V1 = matrix.MulPosition(trianglesCopy[i].V1);
                trianglesCopy[i].V2 = matrix.MulPosition(trianglesCopy[i].V2);
                trianglesCopy[i].V3 = matrix.MulPosition(trianglesCopy[i].V3);
                trianglesCopy[i].N1 = matrix.MulDirection(trianglesCopy[i].N1);
                trianglesCopy[i].N2 = matrix.MulDirection(trianglesCopy[i].N2);
                trianglesCopy[i].N3 = matrix.MulDirection(trianglesCopy[i].N3);
            }

            // Replace Triangles array with the updated copy
            Triangles = trianglesCopy;

            dirty();
        }

        void SetMaterial(Material material)
        {
            // Create a copy of Triangles array to update material
            Triangle[] trianglesCopy = new Triangle[Triangles.Length];
            Triangles.CopyTo(trianglesCopy, 0);

            for (int i = 0; i < trianglesCopy.Length; i++)
            {
                trianglesCopy[i].Material = material;
            }

            // Replace Triangles array with the updated copy
            Triangles = trianglesCopy;
        }

        public Vector SamplePoint(Random rand)
        {
            throw new NotImplementedException();
        }
    }
}
