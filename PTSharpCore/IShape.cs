using System;

namespace PTSharpCore
{
    public interface IShape
    {
        void Compile();
        Box BoundingBox();
        Hit Intersect(Ray ray);
        Vector UVector(Vector uv);
        Vector NormalAt(Vector normal);
        Material MaterialAt(Vector v);
    }
}
