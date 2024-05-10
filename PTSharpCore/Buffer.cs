using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PTSharpCore
{
    public enum Channel
    {
        ColorChannel,
        VarianceChannel,
        StandardDeviationChannel,
        SamplesChannel,
        AlbedoChannel,
        NormalChannel
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
        public ConcurrentDictionary<(int, int), Pixel> Pixels;

        public Buffer() { }

        public Buffer(int width, int height)
        {
            W = width;
            H = height;
            Pixels = new ConcurrentDictionary<(int, int), Pixel>();

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    Pixels[(x, y)] = new Pixel(0, new Colour(0, 0, 0), new Colour(0, 0, 0));
                }
            }
        }

        public Buffer(int width, int height, ConcurrentDictionary<(int, int), Pixel> pbuffer)
        {
            W = width;
            H = height;
            Pixels = pbuffer;
        }

        public Buffer Copy()
        {
            return new Buffer(W, H, Pixels);
        }

        public void AddSample(int x, int y, Colour sample)
        {
            Pixels[(x, y)].AddSample(sample);
        }

        public Vector GetNormalAt(int x, int y)
        {
            // Sample surrounding pixels or normals
            Pixel topLeft = GetPixelOrDefault(Pixels, (x - 1, y - 1));
            Pixel top = GetPixelOrDefault(Pixels, (x, y - 1));
            Pixel topRight = GetPixelOrDefault(Pixels, (x + 1, y - 1));
            Pixel left = GetPixelOrDefault(Pixels, (x - 1, y));
            Pixel right = GetPixelOrDefault(Pixels, (x + 1, y));
            Pixel bottomLeft = GetPixelOrDefault(Pixels, (x - 1, y + 1));
            Pixel bottom = GetPixelOrDefault(Pixels, (x, y + 1));
            Pixel bottomRight = GetPixelOrDefault(Pixels, (x + 1, y + 1));

            // Compute finite differences to estimate the surface normal
            double nx = (topLeft.M.r + 2 * left.M.r + bottomLeft.M.r - topRight.M.r - 2 * right.M.r - bottomRight.M.r) / 8.0;
            double ny = (topLeft.M.g + 2 * top.M.g + topRight.M.g - bottomLeft.M.g - 2 * bottom.M.g - bottomRight.M.g) / 8.0;
            double nz = 1.0; // Assuming the surface is approximately flat in the z-direction

            // Normalize the normal vector
            double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            nx /= length;
            ny /= length;
            nz /= length;

            // Return the computed normal vector
            return new Vector(nx, ny, nz);
        }

        public int Samples(int x, int y) => Pixels[(x, y)].Samples;

        public Colour Color(int x, int y) => Pixels[(x, y)].Color();

        public Colour Variance(int x, int y) => Pixels[(x, y)].Variance();

        public Colour StandardDeviation(int x, int y) => Pixels[(x, y)].StandardDeviation();

        public SKBitmap Image(Channel channel)
        {
            SKBitmap bmp = new SKBitmap(W, H);

            double maxSamples = 0;

            if (channel == Channel.SamplesChannel)
            {
                foreach (var p in Pixels)
                {
                    maxSamples = Math.Max(maxSamples, p.Value.Samples);
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
                            pixelColor = Pixels[(x, y)].Color().Pow(1.0 / 2.2);
                            byte r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            byte g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            byte b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                        case Channel.VarianceChannel:
                            pixelColor = Pixels[(x, y)].Variance();
                            r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                        case Channel.StandardDeviationChannel:
                            pixelColor = Pixels[(x, y)].StandardDeviation();
                            r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                        case Channel.SamplesChannel:
                            double p = (double)(Pixels[(x, y)].Samples / maxSamples);
                            pixelColor = new Colour(p, p, p);
                            r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                        case Channel.AlbedoChannel:
                            pixelColor = CalculateAlbedo(Pixels[(x, y)]);
                            r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                        case Channel.NormalChannel:
                            pixelColor = CalculateNormal(Pixels[(x, y)], x, y);
                            r = (byte)Math.Clamp(pixelColor.r * 255, 0, 255);
                            g = (byte)Math.Clamp(pixelColor.g * 255, 0, 255);
                            b = (byte)Math.Clamp(pixelColor.b * 255, 0, 255);
                            bmp.SetPixel(x, y, new SKColor(r, g, b, 255));
                            break;
                    }
                }
            }
            return bmp;
        }

        // Function to map depth value to color in the gradient
        public Colour MapDepthToColor(double depth, double minDepth, double maxDepth, Colour minColor, Colour maxColor)
        {
            // Normalize depth value to range [0, 1]
            double t = (depth - minDepth) / (maxDepth - minDepth);

            // Clamp t to ensure it stays within [0, 1]
            t = Math.Clamp(t, 0.0, 1.0);

            // Interpolate between black and white based on t
            double r = 1.0 - t;
            double g = 1.0 - t;
            double b = 1.0 - t;

            // Return the interpolated color
            return new Colour(r, g, b);
        }

        public Colour CalculateNormal(Pixel pixel, int x, int y)
        {
            // Get the normal vector at the pixel's position
            Vector normal = GetNormalAt(x, y);

            // Map the normal vector to the range [0,1] for each component
            double r = (normal.X + 1.0) * 0.5;
            double g = (normal.Y + 1.0) * 0.5;
            double b = normal.Z; // Z component is not mapped to [0,1], as it represents the height difference

            return new Colour(r, g, b);
        }

        private Pixel GetPixelOrDefault(ConcurrentDictionary<(int, int), Pixel> pixels, (int, int) coordinates)
        {
            return pixels.TryGetValue(coordinates, out Pixel pixel) ? pixel : new Pixel(); // Default Pixel if not found
        }

        public Colour CalculateAlbedo(Pixel pixel, Colour maxColor)
        {
            // Get the color of the pixel
            Colour color = pixel.Color();

            if (double.IsNaN(color.r) || double.IsNaN(color.g) || double.IsNaN(color.b))
            {
                // Handle NaN values in pixel color
                return new Colour(0, 0, 0);
            }

            // Check if maxColor is zero to avoid division by zero
            if (maxColor.r == 0 || maxColor.g == 0 || maxColor.b == 0)
            {
                // Handle division by zero
                return new Colour(0, 0, 0);
            }

            // Normalize the color by dividing each channel by the corresponding maximum color value
            double r = color.r / maxColor.r;
            double g = color.g / maxColor.g;
            double b = color.b / maxColor.b;

            // Ensure that the normalized values are within the valid range [0, 1]
            r = Math.Clamp(r, 0.0, 1.0);
            g = Math.Clamp(g, 0.0, 1.0);
            b = Math.Clamp(b, 0.0, 1.0);

            // Return the normalized color as the albedo
            return new Colour(r, g, b);
        }

        public Colour CalculateAlbedo(Pixel pixel)
        {
            // Get the color of the pixel
            Colour color = pixel.Color();

            // Calculate the maximum color value across all channels
            double maxColor = Math.Max(Math.Max(color.r, color.g), color.b);

            // Return the albedo of the pixel
            return CalculateAlbedo(pixel, new Colour(maxColor, maxColor, maxColor));
        }
    }
}
