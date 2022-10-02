using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;

namespace PTSharpCore
{
    public enum Channel
    {
        ColorChannel, VarianceChannel, StandardDeviationChannel, SamplesChannel
    }

    public class Pixel
    {
        public int Samples;
        public Colour M;
        public Colour V;

        public Pixel() { }

        public Pixel(int Samples, Colour M, Colour V)
        {
            this.Samples = Samples;
            this.M = M;
            this.V = V;
        }

        public void AddSample(Colour sample)
        {
            Interlocked.Increment(ref Samples);
            if (Samples.Equals(1))
            {
                M = sample;
                return;
            }
            Colour m = M;
            M = M.Add(sample.Sub(M).DivScalar(Samples));
            V = V.Add(sample.Sub(m).Mul(sample.Sub(M)));
        }

        public Colour Color() => M;

        public Colour Variance()
        {
            if (Samples < 2)
            {
                return Colour.Black;
            }
            return V.DivScalar((double)(Samples - 1));
        }

        public Colour StandardDeviation() => Variance().Pow(0.5f);
    }

    class Buffer
    {
        public int W, H;
        //ConcurrentDictionary<(int,int), Pixel> Pixels;
        Pixel[] Pixels;

        public Buffer() { }

        public Buffer(int width, int height)
        {
            W = width;
            H = height;
            //Pixels = new ConcurrentDictionary<(int,int),Pixel>();
            Pixels = new Pixel[W * H]; 

            /*for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Pixels[(x, y)] = new Pixel(0, new Colour(0,0,0), new Colour(0,0,0));
                }
            }*/
            for (int i = 0; i < W * H; i++)
            {
                Pixels[i] = new Pixel(0, new Colour(0,0,0), new Colour(0,0,0));
            }
        }
        /*public Buffer(int width, int height, ConcurrentDictionary<(int, int), Pixel> pbuffer)
        {
            W = width;
            H = height;
            Pixels = pbuffer;
        }*/
        public Buffer(int width, int height, Pixel[] Pixels)
        {
            W = width;
            H = height;
            this.Pixels = Pixels;
        }

        public Buffer Copy()
        {
            return new Buffer(W, H, Pixels);
        }

        public void AddSample(int x, int y, Colour sample)
        {
            this.Pixels[y * W + x].AddSample(sample);
            //Pixels[(x,y)].AddSample(sample); 
            //Pixel v;
            //Pixels.TryGetValue((x, y), out v);//.AddSample(sample);
            //v.AddSample(sample);
        }

        public int Samples(int x, int y) => Pixels[y * W + x].Samples;

        public Colour Color(int x, int y) => Pixels[y * W + x].Color();

        public Colour Variance(int x, int y) => Pixels[y * W + x].Variance();

        public Colour StandardDeviation(int x, int y) => Pixels[y * W + x].StandardDeviation();

        public Bitmap Image(Channel channel)
        {
            Bitmap bmp = new Bitmap(W, H);
            
            double maxSamples=0;
            
            if (channel == Channel.SamplesChannel)
            {  
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        maxSamples = Math.Max(maxSamples, Pixels[y * W + x].Samples);
                    }
                }
            }

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Colour pixelColor = new Colour();
                    switch (channel)
                    {
                        case Channel.ColorChannel:
                            pixelColor = Pixels[y * W + x].Color().Pow(1 / 2.2f);
                            break;
                        case Channel.VarianceChannel:
                            pixelColor = Pixels[y * W + x].Variance();
                            break;
                        case Channel.StandardDeviationChannel:
                            pixelColor = Pixels[y * W + x].StandardDeviation();
                            break;
                        case Channel.SamplesChannel:
                            double p = (double)(Pixels[y * W + x].Samples / maxSamples);
                            pixelColor = new Colour(p, p, p);
                            break;
                    }
                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(Colour.getIntFromColor(pixelColor.r, pixelColor.g, pixelColor.b)));
                }
            }
            return bmp;
        }
    }
}
