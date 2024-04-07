using System;

namespace PTSharpCore
{
    // Referenced from https://github.com/merwaaan/pbrt/blob/master/pbrt/filters/TriangleFilter.cs
    public class TriangleFilter : Filter
    {
        public TriangleFilter(Vector2<float> radius)
            : base(radius)
        {
        }

        public TriangleFilter(float radius)
            : base(new Vector2<float>(radius, radius))
        {
        }

        public override float Evaluate(Point2<float> point)
        {
            return
                Math.Max(0.0f, Radius.X - Math.Abs(point.X)) *
                Math.Max(0.0f, Radius.Y - Math.Abs(point.Y));
        }
    }
}
