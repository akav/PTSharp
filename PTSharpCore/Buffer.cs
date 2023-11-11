using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace PTSharpCore
{
    public enum Channel
    {
        ColorChannel, VarianceChannel, StandardDeviationChannel, SamplesChannel
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Pixel
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
            ++Samples;
            if (Samples == 1)
            {
                M = sample;
                return;
            }
            var m = M;
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

    public class Sample
    {
        private Colour color;
        private int numSamples;

        public Sample()
        {
            color = Colour.Black;
            numSamples = 0;
        }

        public void AddSample(Colour sample)
        {
            color = color.Add(sample);
            numSamples++;
        }

        public Colour Color()
        {
            if (numSamples == 0)
            {
                return Colour.Black;
            }

            return color.DivScalar(numSamples);
        }
    }

    public class TileBuffer
    {
        private readonly int width;
        private readonly int height;
        public static int tileWidth;
        public static int tileHeight;
        private readonly int numTilesX;
        private readonly int numTilesY;
        private readonly Dictionary<(int, int), Buffer> buffers;

        public TileBuffer(int width, int height, int tW, int tH)
        {
            this.width = width;
            this.height = height;
            tileWidth = tW;
            tileHeight = tH;
            this.numTilesX = (width + tileWidth - 1) / tileWidth;
            this.numTilesY = (height + tileHeight - 1) / tileHeight;
            this.buffers = new Dictionary<(int, int), Buffer>();
            for (int i = 0; i < numTilesX; i++)
            {
                for (int j = 0; j < numTilesY; j++)
                {
                    int tileX = i * tileWidth;
                    int tileY = j * tileHeight;
                    int tileWidthActual = Math.Min(tileWidth, width - tileX);
                    int tileHeightActual = Math.Min(tileHeight, height - tileY);
                    buffers[(tileX, tileY)] = Buffer.NewBuffer(tileWidthActual, tileHeightActual);
                }
            }
        }

        public static int GetTileWidth()
        {
            return tileWidth;
        }

        public static int GetTileHeight()
        {
            return tileHeight;
        }

        public void AddSample(int x, int y, Colour color)
        {
            int tileX = x / tileWidth;
            int tileY = y / tileHeight;
            int tileIndexX = x % tileWidth;
            int tileIndexY = y % tileHeight;
            buffers[(tileX * tileWidth, tileY * tileHeight)].AddSample(tileIndexX, tileIndexY, color);
        }        
    }

    class Buffer
    {
        public int Width, Height;
        public ConcurrentDictionary<(int, int), Pixel> Pixels;

        public Buffer() { }

        public static Buffer NewBuffer(int width, int height)
        {
            ConcurrentDictionary<(int, int), Pixel> Pixels = new ConcurrentDictionary<(int, int), Pixel>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Pixels.TryAdd((x,y),new Pixel(0, new Colour(0, 0, 0), new Colour(0, 0, 0)));
                }
            }

            return new Buffer(width, height, Pixels);
        }

        public Buffer(int width, int height, ConcurrentDictionary<(int, int), Pixel> pbuffer)
        {
            Width = width;
            Height = height;
            Pixels = pbuffer;
        }

        public Colour GetPixel(int tileIndexX, int tileIndexY)
        {
            // Determine the range of pixels within the specified tile.
            int startX = tileIndexX * TileBuffer.GetTileWidth();
            int startY = tileIndexY * TileBuffer.GetTileHeight();
            int endX = Math.Min(startX + TileBuffer.GetTileWidth(), Width);
            int endY = Math.Min(startY + TileBuffer.GetTileHeight(), Height);

            // Accumulate the samples and compute the average color for the pixels within the tile.
            int totalSamples = 0;
            Colour totalColor = Colour.Black;
            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    Pixel pixel;
                    if (Pixels.TryGetValue((x, y), out pixel))
                    {
                        totalColor = totalColor.Add(pixel.M.MulScalar(pixel.Samples));
                        totalSamples += pixel.Samples;
                    }
                }
            }
            if (totalSamples > 0)
            {
                return totalColor.DivScalar(totalSamples);
            }
            return Colour.Black;
        }

        public Buffer Copy()
        {
            return new Buffer(Width, Height, new ConcurrentDictionary<(int, int), Pixel>(Pixels));
        }

        public void AddSample(int x, int y, Colour sample)
        {
            Pixel pixel;
            if (Pixels.TryGetValue((x, y), out pixel))
            {
                pixel.AddSample(sample);
                Pixels[(x, y)] = pixel;
            }
        }

        public int Samples(int x, int y)
        {
            Pixel pixel;
            if (Pixels.TryGetValue((x, y), out pixel))
            {
                return pixel.Samples;
            }
            return 0;
        }

        public Colour Color(int x, int y)
        {
            Pixel pixel;
            if (Pixels.TryGetValue((x, y), out pixel))
            {
                return pixel.Color();
            }
            return Colour.Black;
        }

        public Colour Variance(int x, int y)
        {
            Pixel pixel;
            if (Pixels.TryGetValue((x, y), out pixel))
            {
                return pixel.Variance();
            }
            return Colour.Black;
        }

        public Colour StandardDeviation(int x, int y) => Pixels[(x, y)].StandardDeviation();

        public Bitmap Image(Channel channel)
        {
            Bitmap bmp = new Bitmap(Width, Height);

            double maxSamples = 0;

            if (channel == Channel.SamplesChannel)
            {
                foreach (var p in Pixels)
                {
                    maxSamples = Math.Max(maxSamples, p.Value.Samples);
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Colour pixelColor = new Colour();
                    switch (channel)
                    {
                        case Channel.ColorChannel:
                            pixelColor = Pixels[(x, y)].Color().Pow(1.0 / 2.2);
                            break;
                        case Channel.VarianceChannel:
                            pixelColor = Pixels[(x, y)].Variance();
                            break;
                        case Channel.StandardDeviationChannel:
                            pixelColor = Pixels[(x, y)].StandardDeviation();
                            break;
                        case Channel.SamplesChannel:
                            double p = (double)(Pixels[(x, y)].Samples / maxSamples);
                            pixelColor = new Colour(p, p, p);
                            break;
                    }
                    lock (bmp)
                    {
                        bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(Colour.getIntFromColor(pixelColor.r, pixelColor.g, pixelColor.b)));
                    }                   
                }
            }
            return bmp;
        }
    }
}
