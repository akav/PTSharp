using PTSharpCore;
using System;
using System.Threading;

namespace PTSharpCore
{
    public class Box
    {
        internal Vector Min;
        internal Vector Max;
        internal bool left = new();
        internal bool right = new();

        internal Box() { }

        public Box(Vector min, Vector max)
        {
            Min = min;
            Max = max;
        }

        internal static Box BoxForShapes(IShape[] shapes)
        {
            if (shapes.Length == 0)
            {
                return new Box();
            }
            var box = shapes[0].BoundingBox();
            foreach (var shape in shapes)
            {
                box = box.Extend(shape.BoundingBox());
            }
            return box;
        }

        internal static Box BoxForTriangles(Triangle[] shapes)
        {
            if (shapes.Length == 0)
            {
                return new Box();
            }
            Box box = shapes[0].BoundingBox();
            foreach (var shape in shapes)
            {
                box = box.Extend(shape.BoundingBox());
            }
            return box;
        }

        public Vector Anchor(Vector anchor) => Min.Add(Size().Mul(anchor));

        public Vector Center() => Anchor(new Vector(0.5, 0.5, 0.5));

        public double OuterRadius() => Min.Sub(Center()).Length();

        public double InnerRadius() => Center().Sub(Min).MaxComponent();

        public Vector Size() => Max.Sub(Min);

        public Box Extend(Box b) => new Box(Min.Min(b.Min), Max.Max(b.Max));

        public bool Contains(Vector b) => Min.X <= b.X && Max.X >= b.X &&
                                     Min.Y <= b.Y && Max.Y >= b.Y &&
                                     Min.Z <= b.Z && Max.Z >= b.Z;

        public bool Intersects(Box b) =>  
            ! (Min.X > b.Max.X 
            || Max.X < b.Min.X 
            || Min.Y > b.Max.Y 
            || Max.Y < b.Min.Y 
            || Min.Z > b.Max.Z 
            || Max.Z < b.Min.Z);

        public (double, double) Intersect(Ray r)
        {
            (var x1, var y1, var z1) = (((Min.X - r.Origin.X) / r.Direction.X), ((Min.Y - r.Origin.Y) / r.Direction.Y), 
                ((Min.Z - r.Origin.Z) / r.Direction.Z));

            (var x2, var y2, var z2) = (((Max.X - r.Origin.X) / r.Direction.X), 
                ((Max.Y - r.Origin.Y) / r.Direction.Y), ((Max.Z - r.Origin.Z) / r.Direction.Z));

            if (x1 > x2)
            {
                (x1, x2) = (x2, x1);
            }
            if (y1 > y2)
            {
                (y1, y2) = (y2, y1);
            }
            if (z1 > z2)
            {
                (z1, z2) = (z2, z1);
            }
            
            return (Math.Max(Math.Max(x1, y1), z1), Math.Min(Math.Min(x2, y2), z2));
        }

        public (bool, bool) Partition(Axis axis, double point)
        {
            switch (axis)
            {
                case Axis.AxisX:
                    left = Min.X <= point;
                    right = Max.X >= point;
                    break;
                case Axis.AxisY:
                    left = Min.Y <= point;
                    right = Max.Y >= point;
                    break;
                case Axis.AxisZ:
                    left = Min.Z <= point;
                    right = Max.Z >= point;
                    break;
            }
            return (left, right);
        }
    }
}
