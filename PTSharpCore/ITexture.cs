namespace PTSharpCore
{
    interface ITexture
    {
        Colour Sample(double u, double v);
        V NormalSample(double u, double v);
        V BumpSample(double u, double v);
        ITexture Pow(double a);
        ITexture MulScalar(double a);
    }
}

