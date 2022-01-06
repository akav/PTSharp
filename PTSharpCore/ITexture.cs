namespace PTSharpCore
{
    interface ITexture
    {
        Colour Sample(float u, float v);
        V NormalSample(float u, float v);
        V BumpSample(float u, float v);
        ITexture Pow(float a);
        ITexture MulScalar(float a);
    }
}

