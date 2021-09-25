namespace PTSharpCore
{
    interface ITexture
    {
        Colour Sample(double u, double v);
        Vector NormalSample(double u, double v);
        Vector BumpSample(double u, double v);
        ITexture Pow(double a);
        ITexture MulScalar(double a);
    }
}

