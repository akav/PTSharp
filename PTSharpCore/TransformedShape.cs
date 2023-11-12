using System;
using TinyEmbree;

namespace PTSharpCore
{
    class TransformedShape : IShape
    {
        public IShape Shape;
        private Matrix Matrix;
        private Matrix Inverse;
        
        TransformedShape() { }

        internal TransformedShape(IShape s, Matrix m)
        {
            Shape = s;
            Matrix = m;
            Inverse = m.Inverse();
        }

        void IShape.Compile()
        {
            Shape.Compile();
        }

        internal static IShape NewTransformedShape(IShape s, Matrix m)
        {
            return new TransformedShape(s, m);
        }

        Box IShape.BoundingBox()
        {
            return Matrix.MulBox(Shape.BoundingBox());
        }

        Hit IShape.Intersect(Ray ray)
        {
            Ray shapeRay = Inverse.MulRay(ray);
            Hit hit = Shape.Intersect(shapeRay);
            if (!hit.Ok)
            {
                return hit;
            }

            IShape shape = hit.Shape;
            Vector shapePosition = shapeRay.Position(hit.T);
            Vector shapeNormal = shape.NormalAt(shapePosition);
            Vector position = Matrix.MulPosition(shapePosition);
            Vector normal = Inverse.Transpose().MulDirection(shapeNormal);
            Material material = Material.MaterialAt(shape, shapePosition);
            bool inside = false;

            if (Vector.Dot(shapeNormal, shapeRay.Direction) > 0)
            {
                normal = normal.Negate();
                inside = true;
            }

            Ray transformedRay = new Ray(position, normal);
            HitInfo info = new HitInfo(shape, position, normal, transformedRay, material, inside);
            hit.T = (position - ray.Origin).Length();
            hit.HitInfo = info;

            return hit;
        }

        Vector IShape.UV(Vector uv)
        {
            return Shape.UV(uv);
        }

        Vector IShape.NormalAt(Vector normal)
        {
            return Shape.NormalAt(normal);
        }

        Material IShape.MaterialAt(Vector v)
        {
            return Shape.MaterialAt(v);
        }
    }
}