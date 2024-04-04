using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PTSharpCore
{
    public class Box
    {
        public readonly Vector Min;
        public readonly Vector Max;

        public Box() { }
        public Box(Vector min, Vector max)
        {
            Min = min;
            Max = max;
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
            !(Min.X > b.Max.X || Max.X < b.Min.X ||
              Min.Y > b.Max.Y || Max.Y < b.Min.Y ||
              Min.Z > b.Max.Z || Max.Z < b.Min.Z);

        public (double, double) Intersect(Ray r)
        {
            double tMinX = (Min.X - r.Origin.X) / r.Direction.X;
            double tMaxX = (Max.X - r.Origin.X) / r.Direction.X;
            if (tMinX > tMaxX)
                (tMinX, tMaxX) = (tMaxX, tMinX);

            double tMinY = (Min.Y - r.Origin.Y) / r.Direction.Y;
            double tMaxY = (Max.Y - r.Origin.Y) / r.Direction.Y;
            if (tMinY > tMaxY)
                (tMinY, tMaxY) = (tMaxY, tMinY);

            double tMinZ = (Min.Z - r.Origin.Z) / r.Direction.Z;
            double tMaxZ = (Max.Z - r.Origin.Z) / r.Direction.Z;
            if (tMinZ > tMaxZ)
                (tMinZ, tMaxZ) = (tMaxZ, tMinZ);

            double tMin = Math.Max(Math.Max(tMinX, tMinY), tMinZ);
            double tMax = Math.Min(Math.Min(tMaxX, tMaxY), tMaxZ);

            return (tMin, tMax);
        }

        public (bool, bool) Partition(Axis axis, double point)
        {
            switch (axis)
            {
                case Axis.AxisX:
                    return (Min.X <= point, Max.X >= point);
                case Axis.AxisY:
                    return (Min.Y <= point, Max.Y >= point);
                case Axis.AxisZ:
                    return (Min.Z <= point, Max.Z >= point);
                default:
                    return (false, false); // Handle invalid axis
            }
        }

        public static Box BoxForShapes(IShape[] shapes)
        {
            if (shapes.Length == 0)
            {
                return new Box();
            }

            var boxes = shapes.Select(shape => shape.BoundingBox()).ToArray();
            var box = boxes[0];
            foreach (var b in boxes.Skip(1))
            {
                box = box.Extend(b);
            }
            return box;
        }
    }
}
