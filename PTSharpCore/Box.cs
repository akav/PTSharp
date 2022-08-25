using PTSharpCore;
using System;

namespace PTSharpCore
{
    class Box
    {
        internal V Min;
        internal V Max;
        internal bool left;
        internal bool right;

        internal Box() { }

        public Box(V min, V max)
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

        public V Anchor(V anchor) => Min.Add(Size().Mul(anchor));

        public V Center() => Anchor(new V(0.5, 0.5, 0.5));

        public double OuterRadius() => Min.Sub(Center()).Length();

        public double InnerRadius() => Center().Sub(Min).MaxComponent();

        public V Size() => Max.Sub(Min);

        public Box Extend(Box b) => new Box(Min.Min(b.Min), Max.Max(b.Max));

        public bool Contains(V b) => Min.v.X <= b.v.X && Max.v.X >= b.v.X &&
                                     Min.v.Y <= b.v.Y && Max.v.Y >= b.v.Y &&
                                     Min.v.Z <= b.v.Z && Max.v.Z >= b.v.Z;

        public bool Intersects(Box b) => !(Min.v.X > b.Max.v.X || Max.v.X < b.Min.v.X || Min.v.Y > b.Max.v.Y ||
        Max.v.Y < b.Min.v.Y || Min.v.Z > b.Max.v.Z || Max.v.Z < b.Min.v.Z);

        public (double, double) Intersect(Ray r)
        {
            var x1 = (Min.v.X - r.Origin.v.X) / r.Direction.v.X;
            var y1 = (Min.v.Y - r.Origin.v.Y) / r.Direction.v.Y;
            var z1 = (Min.v.Z - r.Origin.v.Z) / r.Direction.v.Z;
            var x2 = (Max.v.X - r.Origin.v.X) / r.Direction.v.X;
            var y2 = (Max.v.Y - r.Origin.v.Y) / r.Direction.v.Y;
            var z2 = (Max.v.Z - r.Origin.v.Z) / r.Direction.v.Z;

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
            var t1 = Math.Max(Math.Max(x1, y1), z1);
            var t2 = Math.Min(Math.Min(x2, y2), z2);
            return (t1, t2);
        }

        public (bool, bool) Partition(Axis axis, double point)
        {
            switch (axis)
            {
                case Axis.AxisX:
                    left = Min.v.X <= point;
                    right = Max.v.X >= point;
                    break;
                case Axis.AxisY:
                    left = Min.v.Y <= point;
                    right = Max.v.Y >= point;
                    break;
                case Axis.AxisZ:
                    left = Min.v.Z <= point;
                    right = Max.v.Z >= point;
                    break;
            }
            return (left, right);
        }
    }
}
