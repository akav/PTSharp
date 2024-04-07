using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]

    struct TransformedShape : IShape
    {
        public IShape Shape;
        private Matrix Matrix;
        private Matrix Inverse;
        public Colour Color { get; set; }
        public Vector Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TransformedShape() { }

        public TransformedShape(IShape s, Matrix m, Matrix im)
        {
            Shape = s;
            Matrix = m;
            Inverse = im;
        }

        void IShape.Compile()
        {
            Shape.Compile();
        }

        internal static IShape NewTransformedShape(IShape s, Matrix m)
        {   
            return new TransformedShape(s, m, m.Inverse());
        }

        Box IShape.BoundingBox()
        {
            return Matrix.MulBox(Shape.BoundingBox());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        Hit IShape.Intersect(Ray r)
        {
            var shapeRay = Matrix.Inverse().MulRay(r);
            var hit = Shape.Intersect(shapeRay);

            if(!hit.Ok)
            {
                return hit;
            }

            var shape = hit.Shape;
            var shapePosition = shapeRay.Position(hit.T);
            var shapeNormal = shape.NormalAt(shapePosition);
            var position = Matrix.MulPosition(shapePosition);
            var normal = Matrix.Inverse().Transpose().MulDirection(shapeNormal);
            var material = Material.MaterialAt(shape, shapePosition);
            var inside = false;

            if(shapeNormal.Dot(shapeRay.Direction) > 0)
            {
                normal = normal.Negate();
                inside = true;
            }

            var ray = new Ray(position, normal);
            var info = new HitInfo(shape, position, normal, ray, material, inside);
            hit.T = position.Sub(r.Origin).Length();
            hit.HitInfo = info;
            return hit;
        }

        Vector IShape.UVector(Vector uv)
        {
            return Shape.UVector(uv);
        }

        Vector IShape.NormalAt(Vector normal)
        {
            return Shape.NormalAt(normal);
        }

        Material IShape.MaterialAt(Vector v)
        {
            return Shape.MaterialAt(v);
        }

        public Vector SamplePoint(Random rand)
        {
            throw new NotImplementedException();
        }
    }
}
