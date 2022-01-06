namespace PTSharpCore
{
    interface ITexture
    {
        Colour Sample(double u, double v);
        IVector<double> NormalSample(double u, double v);
        IVector<double> BumpSample(double u, double v);
        ITexture Pow(double a);
        ITexture MulScalar(double a);
    }
}

