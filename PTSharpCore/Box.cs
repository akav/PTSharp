using PTSharpCore;
using System;

namespace PTSharpCore
{
    class Box
    {
        public IVector<double> Min;
        public IVector<double> Max;
        internal bool left;
        internal bool right;

        internal Box() { }

        public Box(IVector<double> min, IVector<double> max)
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

        public IVector<double> Anchor(IVector<double> anchor) => Min.Add(Size().Mul(anchor));

        public IVector<double> Center() => Anchor(new IVector<double>(new double[] { 0.5D, 0.5D, 0.5D, 0.0}));

        public double OuterRadius() => Min.Sub(Center()).Length();

        public double InnerRadius() => Center().Sub(Min).MaxComponent();

        public IVector<double> Size() => Max.Sub(Min);

        public Box Extend(Box b) => new Box(Min.Min(b.Min), Max.Max(b.Max));

        public bool Contains(IVector<double> b) => Min.dv[0] <= b.dv[0] && Max.dv[0] >= b.dv[0] &&
                                                   Min.dv[1] <= b.dv[1] && Max.dv[1] >= b.dv[1] &&
                                                   Min.dv[2] <= b.dv[2] && Max.dv[2] >= b.dv[2];


        public bool Intersects(Box b)
        {
            return !(Min.dv[0] > b.Max.dv[0] || 
                    Max.dv[0] < b.Min.dv[0] || 
                    Min.dv[1] > b.Max.dv[1] ||
                    Max.dv[1] < b.Min.dv[1] || 
                    Min.dv[2] > b.Max.dv[2] || 
                    Max.dv[2] < b.Min.dv[2]);
        }
        

        public (double, double) Intersect(Ray r)
        {
            var x1 = (Min.dv[0] - r.Origin.dv[0]) / r.Direction.dv[0];
            var y1 = (Min.dv[1] - r.Origin.dv[1]) / r.Direction.dv[1];
            var z1 = (Min.dv[2] - r.Origin.dv[2]) / r.Direction.dv[2];
            var x2 = (Max.dv[0] - r.Origin.dv[0]) / r.Direction.dv[0];
            var y2 = (Max.dv[1] - r.Origin.dv[1]) / r.Direction.dv[1];
            var z2 = (Max.dv[2] - r.Origin.dv[2]) / r.Direction.dv[2];

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
                    left = Min.dv[0] <= point;
                    right = Max.dv[0] >= point;
                    break;
                case Axis.AxisY:
                    left = Min.dv[1] <= point;
                    right = Max.dv[1] >= point;
                    break;
                case Axis.AxisZ:
                    left = Min.dv[2] <= point;
                    right = Max.dv[2] >= point;
                    break;
            }
            return (left, right);
        }
    }
}
