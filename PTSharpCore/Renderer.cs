using System;
using SkiaSharp;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MathNet.Numerics.Random;
using static PTSharpCore.OIDN;
using System.Security.Cryptography;

namespace PTSharpCore
{
    class Renderer
    {
        Scene Scene;
        Camera Camera;
        Sampler Sampler;
        public static Buffer PBuffer;
        public int SamplesPerPixel;
        public bool StratifiedSampling;
        public int AdaptiveSamples;
        double AdaptiveThreshold;
        double AdaptiveExponent;
        public int FireflySamples;
        double FireflyThreshold;
        int NumCPU;
        public static int iterations;
        public string pathTemplate;
        public bool Denoise { get; internal set; }

        Renderer() { }

        public static Renderer NewRenderer(Scene scene, Camera camera, Sampler sampler, int w, int h, bool multithreaded)
        {
            Renderer r = new Renderer();
            r.Scene = scene;
            r.Camera = camera;
            r.Sampler = sampler;
            PBuffer = new Buffer(w, h);
            r.SamplesPerPixel = 2;
            r.AdaptiveSamples = 0;
            r.StratifiedSampling = false;
            r.AdaptiveThreshold = 1;
            r.AdaptiveExponent = 1;
            r.FireflySamples = 0;
            r.FireflyThreshold = 1;
            r.Denoise = false;

            if (multithreaded)
                r.NumCPU = Environment.ProcessorCount;
            else
                r.NumCPU = 1;
            return r;
        }

        void writeImage(String path, Buffer buf, Channel channel)
        {
            SKBitmap finalrender = buf.Image(channel);

            using (SKImage img = SKImage.FromBitmap(finalrender))
            {
                using (Stream stream = File.OpenWrite(path))
                {
                    try
                    {
                        img.Encode(SKEncodedImageFormat.Png, 100).SaveTo(stream);
                        Console.WriteLine("Wrote image to location: " + path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to write image to location: " + path);
                        Console.WriteLine("Error: " + ex.Message);
                    }
                }
            }
        }

        public void Render()
        {
            Scene scene = Scene;
            Camera camera = Camera;
            Sampler sampler = Sampler;
            Buffer buf = PBuffer;
            (int w, int h) = (buf.W, buf.H);
            int spp = SamplesPerPixel;
            int sppRoot = (int)(Math.Sqrt(SamplesPerPixel));
            scene.Compile();

            // Stop watch timer 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Random Number Generator from on Math.Numerics
            var rand = new SystemRandomSource(sw.Elapsed.Milliseconds, true);

            double invWidth = 1.0f / w;
            double invHeight = 1.0f / h;
            scene.rays = 0;

            for (int i = 0; i < 1; i++)
            {
                for (int y = i; y < h; y += 1)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (StratifiedSampling)
                        {
                            for (int u = 0; u < sppRoot; u++)
                            {
                                for (int v = 0; v < sppRoot; v++)
                                {
                                    var fu = (u + 0.5) / sppRoot;
                                    var fv = (v + 0.5) / sppRoot;
                                    Ray ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                                    Colour sample = sampler.Sample(scene, ray, rand);
                                    buf.AddSample(x, y, sample);
                                }
                            }

                        }
                        else
                        {
                            // Render the sub-tile at the current coordinates, using the current resolution
                            Colour c = new Colour(0, 0, 0);

                            for (int p = 0; p < spp; p++)
                            {
                                // Generate random offsets within the pixel
                                double xOffset = Random.Shared.NextDouble();
                                double yOffset = Random.Shared.NextDouble();

                                // Use jittered offsets to generate rays
                                double fu = (x + xOffset) / w;
                                double fv = (y + yOffset) / h;

                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, fu, fv, rand), rand);
                            }

                            // Average the color over the number of samples
                            c /= spp;
                            buf.AddSample(x, y, c);

                            // Set the pixel color
                            var offset = (y * w + x) * 4; // BGR
                            Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                            Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                            Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));

                        }
                        // Adaptive Sampling
                        if (AdaptiveSamples > 0)
                        {
                            var v = buf.StandardDeviation(x, y).MaxComponent();
                            v = Math.Clamp(v / AdaptiveThreshold, 0, 1);
                            v = Math.Pow(v, AdaptiveExponent);
                            int samples = AdaptiveSamples * (int)(v);

                            for (int d = 0; d < samples; d++)
                            {
                                Colour sample = Colour.Black;
                                var fu = rand.NextDouble();
                                var fv = rand.NextDouble();
                                Ray ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                                sample += sampler.Sample(scene, ray, rand);
                                buf.AddSample(x, y, sample);

                                // Set the pixel color
                                var offset = (y * w + x) * 4; // BGR
                                Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }

                        if (FireflySamples > 0)
                        {
                            if (PBuffer.StandardDeviation(x, y).MaxComponent() > FireflyThreshold)
                            {
                                for (int e = 0; e < FireflySamples; e++)
                                {
                                    Colour sample = new Colour(0, 0, 0);
                                    var fu = (x + rand.NextDouble()) * invWidth;
                                    var fv = (y + rand.NextDouble()) * invHeight;
                                    Ray ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                                    sample += sampler.Sample(scene, ray, rand);
                                    PBuffer.AddSample(x, y, sample);
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("time elapsed:" + sw.Elapsed);
            sw.Stop();
        }
        public void RenderParallel()
        {
            Scene scene = Scene;
            Camera camera = Camera;
            Sampler sampler = Sampler;
            Buffer buf = PBuffer;
            (int w, int h) = (buf.W, buf.H);
            int spp = SamplesPerPixel;
            int sppRoot = (int)(Math.Sqrt(SamplesPerPixel));
            scene.Compile();
            scene.rays = 0;

            // Stop watch timer 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var rand = Random.Shared;
            int totalPixels = h * w;
            double invWidth = 1.0f / w;
            double invHeight = 1.0f / h;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            ParallelOptions parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Console.WriteLine("{0} x {1}, {2} spp, {3} core(s)", w, h, spp, parallelOptions.MaxDegreeOfParallelism);

            if (StratifiedSampling)
            {
                Parallel.For(0, w * h, parallelOptions, index =>
                {
                    int y = index / w, x = index % w;
                    for (int u = 0; u < sppRoot; u++)
                    {
                        for (int v = 0; v < sppRoot; v++)
                        {
                            var fu = ((double)u + 0.5) / (double)sppRoot;
                            var fv = ((double)v + 0.5) / (double)sppRoot;
                            var ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                            var sample = sampler.Sample(scene, ray, rand);
                            buf.AddSample(x, y, sample);
                        }
                    }

                    // Set the pixel color
                    var offset = (y * w + x) * 4; // BGR
                    Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                });
            }
            else
            {
                int tile_size = 256;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var renderingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);
                var colorSettingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);
                int sub_tile_size = 32;

                for (int tile_index = 0; tile_index < num_tiles_x * num_tiles_y; tile_index++)
                {
                    int tile_x = tile_index % num_tiles_x;
                    int tile_y = tile_index / num_tiles_x;
                    int x_start = tile_x * tile_size;
                    int y_start = tile_y * tile_size;
                    int x_end = Math.Min(x_start + tile_size, w);
                    int y_end = Math.Min(y_start + tile_size, h);

                    // Divide each tile into smaller sub-tiles for finer-grained parallelism
                    for (int sub_tile_y = 0; sub_tile_y < tile_size; sub_tile_y += sub_tile_size)
                    {
                        for (int sub_tile_x = 0; sub_tile_x < tile_size; sub_tile_x += sub_tile_size)
                        {
                            int sub_x_start = x_start + sub_tile_x;
                            int sub_y_start = y_start + sub_tile_y;
                            int sub_x_end = Math.Min(sub_x_start + sub_tile_size, x_end);
                            int sub_y_end = Math.Min(sub_y_start + sub_tile_size, y_end);

                            // Schedule rendering of each sub-tile as a separate task
                            Task.Factory.StartNew(() =>
                            {
                                // Rendering logic for the sub-tile
                                for (int y = sub_y_start; y < sub_y_end; y++)
                                {
                                    for (int x = sub_x_start; x < sub_x_end; x++)
                                    {
                                        // Render the sub-tile at the current coordinates, using the current resolution
                                        Colour c = new Colour(0, 0, 0);

                                        for (int p = 0; p < spp; p++)
                                        {
                                            // Generate random offsets within the pixel
                                            double xOffset = Random.Shared.NextDouble();
                                            double yOffset = Random.Shared.NextDouble();

                                            // Use jittered offsets to generate rays
                                            double fu = (x + xOffset) / w;
                                            double fv = (y + yOffset) / h;

                                            c += sampler.Sample(scene, camera.CastRay(x, y, w, h, fu, fv, rand), rand);
                                        }

                                        // Average the color over the number of samples
                                        c /= spp;
                                        buf.AddSample(x, y, c);
                                    }
                                }
                            }, CancellationToken.None, TaskCreationOptions.None, renderingScheduler)
                            .ContinueWith(_ =>
                            {
                                // Setting color values in parallel to avoid contention
                                Task.Factory.StartNew(() =>
                                {
                                    for (int y = sub_y_start; y < sub_y_end; y++)
                                    {
                                        for (int x = sub_x_start; x < sub_x_end; x++)
                                        {
                                            // Set the pixel color
                                            var offset = (y * w + x) * 4; // BGR
                                            Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                            Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                            Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                        }
                                    }
                                }, CancellationToken.None, TaskCreationOptions.None, colorSettingScheduler);
                            });
                        }
                    }
                }

                // Dispose the schedulers after rendering completes
                renderingScheduler.Dispose();
                colorSettingScheduler.Dispose();
            }

            if (AdaptiveSamples > 0)
            {
                Console.WriteLine("Adaptive sampling set to {0} samples", AdaptiveSamples);

                // Define global variables for accumulating variance and sample counts
                ConcurrentDictionary<int, double> pixelVariances = new ConcurrentDictionary<int, double>(); // Stores variance for each pixel
                ConcurrentDictionary<int, int> pixelSampleCounts = new ConcurrentDictionary<int, int>(); // Stores sample count for each pixel

                // Replace the variance calculation with an optimal approach using parallel aggregation
                Parallel.For(0, w * h, parallelOptions, (i, loopState) =>
                {
                    int y = i / w, x = i % w;

                    // Calculate the sum of color components
                    double sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    for (int j = 0; j < AdaptiveSamples; j++)
                    {
                        var fu = Random.Shared.NextDouble();
                        var fv = Random.Shared.NextDouble();
                        Ray ray = camera.CastRay(x, y, w, h, fu, fv, Random.Shared);
                        Colour sample = sampler.Sample(scene, ray, Random.Shared);
                        buf.AddSample(x, y, sample);

                        // Accumulate the color components
                        sumR += sample.r;
                        sumG += sample.g;
                        sumB += sample.b;
                        count++;
                    }

                    // Calculate the average color
                    double avgR = sumR / count;
                    double avgG = sumG / count;
                    double avgB = sumB / count;

                    // Calculate the variance
                    double variance = 0;

                    for (int j = 0; j < AdaptiveSamples; j++)
                    {
                        var fu = Random.Shared.NextDouble();
                        var fv = Random.Shared.NextDouble();
                        Ray ray = camera.CastRay(x, y, w, h, fu, fv, Random.Shared);
                        Colour sample = sampler.Sample(scene, ray, Random.Shared);

                        // Calculate the squared difference from the average color
                        double diffR = sample.r - avgR;
                        double diffG = sample.g - avgG;
                        double diffB = sample.b - avgB;
                        variance += diffR * diffR + diffG * diffG + diffB * diffB;
                    }

                    // Set the pixel color
                    var offset = (y * w + x) * 4; // BGR
                    Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));

                    // Normalize the variance
                    variance /= count;

                    // Update accumulated variance and sample count
                    int index = y * w + x;
                    if (!pixelVariances.ContainsKey(index))
                    {
                        pixelVariances[index] = variance;
                        pixelSampleCounts[index] = count;
                    }
                    else
                    {
                        pixelVariances[index] = (pixelVariances[index] + variance) / 2; // Update variance with running average
                        pixelSampleCounts[index] += count; // Accumulate sample count
                    }
                });
            }

            if (FireflySamples > 0)
            {
                // Concurrent dictionary to track skipped pixels
                ConcurrentDictionary<(int, int), bool> skippedPixels = new ConcurrentDictionary<(int, int), bool>();

                Parallel.For(0, w * h, parallelOptions, (i, loopState) =>
                {
                    int y = i / w, x = i % w;
                    if (buf.StandardDeviation(x, y).MaxComponent() > FireflyThreshold)
                    {
                        if (!skippedPixels.ContainsKey((x, y))) // Check if the pixel was previously marked as a firefly
                        {
                            for (int j = 0; j < FireflySamples; j++)
                            {
                                var sample = sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);

                                // Check if the sample is a firefly
                                if (IsFirefly(sample, x, y, ref buf))
                                {
                                    // If firefly, mark the pixel and break the loop
                                    skippedPixels.TryAdd((x, y), true);
                                    break;
                                }

                                buf.AddSample(x, y, sample);
                                var offset = (y * w + x) * 4; // BGR
                                Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }
                        else
                        {
                            // Pixel was marked as a firefly in a previous run, recompute samples
                            for (int j = 0; j < FireflySamples; j++)
                            {
                                var sample = sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                                buf.AddSample(x, y, sample);
                                var offset = (y * w + x) * 4; // BGR
                                Program.Bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.Bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.Bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }

                            // Remove the pixel from skippedPixels dictionary as it's being recomputed
                            bool _;
                            skippedPixels.TryRemove((x, y), out _);
                        }
                    }
                });
            }

            Console.WriteLine("time elapsed:" + sw.Elapsed);
            sw.Stop();
        }

        public bool IsFirefly(Colour sample, int x, int y, ref Buffer buf)
        {
            double brightnessThreshold = 0.9; // Adjust as needed
            double brightness = sample.r * 0.2126 + sample.g * 0.7152 + sample.b * 0.0722; // Calculate brightness

            // Check if the brightness exceeds the threshold
            if (brightness > brightnessThreshold)
            {
                // Now, let's check the deviation from the local neighborhood
                double localDeviationThreshold = 0.2; // Adjust as needed
                double localDeviation = CalculateLocalDeviation(sample, x, y, ref buf); // Implement this method

                // If the deviation from the local neighborhood is high, consider it a firefly
                return localDeviation > localDeviationThreshold;
            }
            else
            {
                // If the brightness is not high enough, it's not a firefly
                return false;
            }
        }

        private double CalculateLocalDeviation(Colour sample, int x, int y, ref Buffer buf)
        {
            // Define the size of the local neighborhood (e.g., a 3x3 or 5x5 window)
            int neighborhoodSize = 3; // Adjust as needed

            // Calculate the boundaries of the neighborhood
            int startX = Math.Max(0, x - neighborhoodSize / 2);
            int startY = Math.Max(0, y - neighborhoodSize / 2);
            int endX = Math.Min(buf.W - 1, x + neighborhoodSize / 2);
            int endY = Math.Min(buf.H - 1, y + neighborhoodSize / 2);

            // Accumulate the color components of neighboring pixels
            double totalR = 0, totalG = 0, totalB = 0;
            int count = 0;

            for (int j = startY; j <= endY; j++)
            {
                for (int i = startX; i <= endX; i++)
                {
                    Colour neighborColor = buf.Color(i, j);
                    totalR += neighborColor.r;
                    totalG += neighborColor.g;
                    totalB += neighborColor.b;
                    count++;
                }
            }

            // Calculate the average color of the neighborhood
            double avgR = totalR / count;
            double avgG = totalG / count;
            double avgB = totalB / count;

            // Compute the deviation of the sample color from the average neighborhood color
            double deviationR = Math.Abs(sample.r - avgR);
            double deviationG = Math.Abs(sample.g - avgG);
            double deviationB = Math.Abs(sample.b - avgB);

            // Calculate the overall deviation (e.g., Euclidean distance)
            double overallDeviation = Math.Sqrt(deviationR * deviationR + deviationG * deviationG + deviationB * deviationB);

            return overallDeviation;
        }

        public static float[] SKImageToFloatArray(SKBitmap image)
        {
            using (var imagePixmap = image.PeekPixels())
            {
                var info = imagePixmap.Info;

                // Calculate the number of pixels
                int pixelCount = info.Width * info.Height;

                byte[] pixels = new byte[info.BytesSize];
                Marshal.Copy(imagePixmap.GetPixels(), pixels, 0, pixels.Length);

                // Convert byte values to float values in the range [0, 1]
                float[] normalizedPixels = new float[pixelCount * 3]; // Assuming RGB format
                for (int i = 0, j = 0; i < pixels.Length; i += 4, j += 3)
                {
                    normalizedPixels[j] = pixels[i] / 255.0f;       // Red
                    normalizedPixels[j + 1] = pixels[i + 1] / 255.0f; // Green
                    normalizedPixels[j + 2] = pixels[i + 2] / 255.0f; // Blue
                }

                return normalizedPixels;
            }
        }

        public static SKBitmap FloatArrayToSKBitmap(float[] data, int width, int height)
        {
            var imageInfo = new SKImageInfo(width, height);

            // Ensure that the float array has the correct size for RGBA format
            if (data.Length != width * height * 3) // Assuming RGB format with 3 components per pixel
            {
                throw new ArgumentException("Invalid float array size for the specified width and height.", nameof(data));
            }

            var bitmap = new SKBitmap(imageInfo);

            // Convert float array to byte array
            byte[] pixelData = new byte[width * height * 4]; // RGBA format
            for (int i = 0, j = 0; i < data.Length; i += 3, j += 4)
            {
                pixelData[j] = (byte)(data[i] * 255);           // Red
                pixelData[j + 1] = (byte)(data[i + 1] * 255);   // Green
                pixelData[j + 2] = (byte)(data[i + 2] * 255);   // Blue
                pixelData[j + 3] = 255;                         // Alpha
            }

            // Pin byte array in memory
            GCHandle handle = GCHandle.Alloc(pixelData, GCHandleType.Pinned);
            try
            {
                // Copy pixel data to SKBitmap
                IntPtr ptr = handle.AddrOfPinnedObject();
                using (var pixmap = new SKPixmap(imageInfo, ptr, (int)imageInfo.RowBytes))
                {
                    if (!bitmap.InstallPixels(pixmap))
                    {
                        throw new InvalidOperationException("Failed to install pixels from SKPixmap to SKBitmap.");
                    }
                }
            }
            finally
            {
                // Release pinned memory
                handle.Free();
            }

            return bitmap;
        }

        public float[] DenoiseRGB(float[] inputRGB, float[] albedoRGB, float[] normalRGB, int width, int height)
        {
            try
            {
                // Create a denoiser instance
                var denoiser = new Denoiser(OIDN.OIDNDeviceType.OIDN_DEVICE_TYPE_DEFAULT, Environment.ProcessorCount);

                // Calculate the size of the color buffer for float3 data
                int colorBufSize = width * height * 3 * sizeof(float);
                IntPtr colorBuf = oidnNewBuffer(denoiser.oidnDevice, colorBufSize);

                // Obtain the pointer to the beginning of the color buffer
                IntPtr colorDataPtr = oidnGetBufferData(colorBuf);

                // Create a separate buffer for the output image
                IntPtr outputBuf = oidnNewBuffer(denoiser.oidnDevice, colorBufSize);

                // Create an OIDN Ray Tracing (RT) filter for denoising
                IntPtr filter = oidnNewFilter(denoiser.oidnDevice, "RT");

                int pixelStride = 3 * sizeof(float); // Assuming RGB format

                try
                {
                    // Copy input data to the color buffer
                    Marshal.Copy(inputRGB, 0, colorDataPtr, inputRGB.Length);

                    // Set input and output images for denoising filter
                    oidnSetFilterImage(filter, "color", colorBuf, OIDNImageFormat.OIDN_FORMAT_FLOAT3,
                        width, height, 0, pixelStride, width * pixelStride);
                    oidnSetFilterImage(filter, "output", outputBuf, OIDNImageFormat.OIDN_FORMAT_FLOAT3,
                        width, height, 0, pixelStride, width * pixelStride);
                    oidnSetFilterBool(filter, "srgb", true);
                    oidnSetFilterFloat(filter, "inputScale", float.NaN);
                    oidnCommitFilter(filter);

                    // Execute denoising
                    oidnExecuteFilter(filter);

                    // Check for errors
                    if (oidnGetDeviceError(denoiser.oidnDevice, out var errorMessage) != OIDNError.OIDN_ERROR_NONE)
                        Console.WriteLine("Error: {0}", errorMessage);

                    // Determine the size of the denoised output buffer
                    int outputBufSize = width * height * 3;

                    // Create a float array to hold the denoised output data
                    float[] outputRGB = new float[outputBufSize];

                    // Obtain the pointer to the beginning of the color buffer
                    IntPtr outputDataPtr = oidnGetBufferData(outputBuf);

                    // Copy denoised data from the output buffer to the float array
                    Marshal.Copy(outputDataPtr, outputRGB, 0, outputBufSize);

                    return outputRGB;
                }
                finally
                {
                    // Release allocated memory for buffers
                    oidnReleaseBuffer(colorBuf);
                    oidnReleaseBuffer(outputBuf);

                    // Release OIDN filter device
                    oidnReleaseFilter(filter);

                    // Release OIDN device
                    oidnReleaseDevice(denoiser.oidnDevice);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during denoising: {ex.Message}");
                throw;
            }
        }

        public SKBitmap DenoiseImage(SKBitmap color, SKBitmap albedo, SKBitmap normal)
        {
            // Convert SKBitmaps to float arrays
            float[] colorData = SKImageToFloatArray(color);
            float[] albedoData = SKImageToFloatArray(albedo);
            float[] normalData = SKImageToFloatArray(normal);

            // Denoise RGB data
            float[] denoisedData = DenoiseRGB(colorData, albedoData, normalData, color.Width, color.Height);

            // Convert denoised float array back to an SKBitmap
            SKBitmap denoisedBitmap = FloatArrayToSKBitmap(denoisedData, color.Width, color.Height);

            return denoisedBitmap;
        }

        public SKBitmap IterativeRender(String pathTemplate, int iter)
        {
            iterations = iter;
            SKBitmap colourChannel = null;
            SKBitmap albedoChannel = null;
            SKBitmap normalChannel = null;

            for (int i = 1; i <= iterations; i++)
            {
                Console.WriteLine("Iteration " + i + " of " + iterations);
                if (NumCPU.Equals(1))
                {
                    Render();
                }
                else
                {
                    RenderParallel();
                }

                string currentPath = string.Format(pathTemplate, i); // Update the path for each iteration

                colourChannel = PBuffer.Image(Channel.ColorChannel);

                // Save the colour channel
                using (var colorstream = File.OpenWrite(currentPath))
                {
                    colourChannel.Encode(SKEncodedImageFormat.Png, 100).SaveTo(colorstream);
                }

                // Experimental: OIDN 
                if (Denoise)
                {
                    albedoChannel = PBuffer.Image(Channel.AlbedoChannel);

                    // Save the Albedo channel
                    using (var albedostream = File.OpenWrite("albedo_channel.png"))
                    {
                        albedoChannel.Encode(SKEncodedImageFormat.Png, 100).SaveTo(albedostream);
                        Console.WriteLine("Albedo channel saved");
                    }

                    normalChannel = PBuffer.Image(Channel.NormalChannel);

                    // Save the Normal Channel
                    using (var normalstream = File.OpenWrite("normal_channel.png"))
                    {
                        normalChannel.Encode(SKEncodedImageFormat.Png, 100).SaveTo(normalstream);
                        Console.WriteLine("Normal channel saved");
                    }

                    // Denoise the image using OIDN
                    colourChannel = DenoiseImage(colourChannel, albedoChannel, normalChannel);

                    // Save the denoised image
                    using (var denoisedstream = File.OpenWrite("denoised_output.png"))
                    {
                        colourChannel.Encode(SKEncodedImageFormat.Png, 100).SaveTo(denoisedstream);
                        Console.WriteLine("Denoised output saved");
                    }
                }
            }           

            return colourChannel;
        }

        internal void FrameRender(String path, int iterations)
        {
            for (int i = 1; i <= iterations; i++)
            {
                Console.WriteLine("Iterations " + i + " of " + iterations);
                Render();
            }
            Buffer buf = PBuffer.Copy();
            writeImage(path, buf, Channel.ColorChannel);
        }
    }
}
