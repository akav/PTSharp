using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    // Referenced from merwaan/pbrt
    // https://github.com/merwaaan/pbrt/blob/master/pbrt/core/Filter.cs
    public abstract class Filter
    {
        public Vector2<float> Radius, InvRadius;

        protected Filter(Vector2<float> radius)
        {
            Radius = radius;
            InvRadius = new Vector2<float>(1.0f / radius.X, 1.0f / radius.Y);
        }

        public abstract float Evaluate(Point2<float> point);

        public override string ToString()
        {
            return $"{GetType().Name} (radius {Radius})";
        }
    }
}
