namespace PTSharpCore
{
    internal interface IShape
    {
        void Compile();
        Box BoundingBox();
        Hit Intersect(Ray ray);
        V UV(V uv);
        V NormalAt(V normal);
        Material MaterialAt(V v);
    }
}
