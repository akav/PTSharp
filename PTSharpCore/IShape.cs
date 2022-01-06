namespace PTSharpCore
{
    internal interface IShape
    {
        void Compile();
        Box BoundingBox();
        Hit Intersect(Ray ray);
        IVector<double> UV(IVector<double> uv);
        IVector<double> NormalAt(IVector<double> normal);
        Material MaterialAt(IVector<double> v);
    }
}
