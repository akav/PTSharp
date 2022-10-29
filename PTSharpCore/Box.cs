using PTSharpCore;
using System;
using System.Threading;

namespace PTSharpCore
{
    class Box
    {
        internal Vector Min;
        internal Vector Max;
        internal static ThreadLocal<bool> left = new();
        internal static ThreadLocal<bool> right = new();

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

        public bool Contains(Vector b) => Min.x <= b.x && Max.x >= b.x &&
                                     Min.y <= b.y && Max.y >= b.y &&
                                     Min.z <= b.z && Max.z >= b.z;

        public bool Intersects(Box b) => !(Min.x > b.Max.x || Max.x < b.Min.x || Min.y > b.Max.y ||
        Max.y < b.Min.y || Min.z > b.Max.z || Max.z < b.Min.z);

        public (double, double) Intersect(Ray r)
        {
            var x1 = (Min.x - r.Origin.x) / r.Direction.x;
            var y1 = (Min.y - r.Origin.y) / r.Direction.y;
            var z1 = (Min.z - r.Origin.z) / r.Direction.z;
            var x2 = (Max.x - r.Origin.x) / r.Direction.x;
            var y2 = (Max.y - r.Origin.y) / r.Direction.y;
            var z2 = (Max.z - r.Origin.z) / r.Direction.z;

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
                    left.Value = Min.x <= point;
                    right.Value = Max.x >= point;
                    break;
                case Axis.AxisY:
                    left.Value = Min.y <= point;
                    right.Value = Max.y >= point;
                    break;
                case Axis.AxisZ:
                    left.Value = Min.z <= point;
                    right.Value = Max.z >= point;
                    break;
            }
            return (left.Value, right.Value);
        }
    }
}
