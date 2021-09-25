namespace PTSharpCore
{
    internal interface IShape
    {
        void Compile();
        Box BoundingBox();
        Hit Intersect(Ray ray);
        Vector UV(Vector uv);
        Vector NormalAt(Vector normal);
        Material MaterialAt(Vector v);
    }
}
