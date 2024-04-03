using MathNet.Numerics.Random;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silk.NET.Vulkan;

namespace PTSharpCore
{
    class Renderer
    {
        Scene Scene;
        Camera Camera;
        Sampler Sampler;
        public Buffer PBuffer;
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

        Renderer() { }

        public static Renderer NewRenderer(Scene scene, Camera camera, Sampler sampler, int w, int h, bool multithreaded)
        {
            Renderer r = new Renderer();
            r.Scene = scene;
            r.Camera = camera;
            r.Sampler = sampler;
            r.PBuffer = new Buffer(w, h);
            r.SamplesPerPixel = 2;
            r.AdaptiveSamples = 0;
            r.StratifiedSampling = false;
            r.AdaptiveThreshold = 1;
            r.AdaptiveExponent = 1;
            r.FireflySamples = 0;
            r.FireflyThreshold = 1;

            if (multithreaded)
                r.NumCPU = Environment.ProcessorCount;
            else
                r.NumCPU = 1;
            return r;
        }

        void writeImage(String path, Buffer buf, Channel channel)
        {
            System.Drawing.Bitmap finalrender = buf.Image(channel);
            finalrender.Save(path);
            Console.WriteLine("Wrote image to location:" + path);
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
                            for (int p = 0; p < spp; p++)
                            {
                                Colour c = new Colour(0, 0, 0);
                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                buf.AddSample(x, y, c);
                                // Set the pixel color
                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }

                        }
                        // Adaptive Sampling
                        if (AdaptiveSamples > 0)
                        {
                            double v = buf.StandardDeviation(x, y).MaxComponent();
                            v = Util.Clamp(v / AdaptiveThreshold, 0, 1);
                            v = Math.Pow(v, AdaptiveExponent);
                            int samples = (int)(v * AdaptiveSamples);
                            for (int d = 0; d < samples; d++)
                            {

                                var fu = rand.NextDouble();
                                var fv = rand.NextDouble();
                                Ray ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                                Colour sample = sampler.Sample(scene, ray, rand);
                                buf.AddSample(x, y, sample);
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

            // Random Number Generator from on Math.Numerics
            var rand = Random.Shared;

            // Frame resolution
            int totalPixels = h * w;
            double invWidth = 1.0f / w;
            double invHeight = 1.0f / h;

            // Create a cancellation token for Parallel.For loop control
            CancellationTokenSource cts = new CancellationTokenSource();

            // ParallelOptions for Parallel.For
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;

            Console.WriteLine("{0} x {1}, {2} spp, {3} core(s)", w, h, spp, po.MaxDegreeOfParallelism);

            if (StratifiedSampling)
            {
                Parallel.For(0, w * h, po, (i, loopState) =>
                {
                    int y = i / w, x = i % w;
                    for (int u = 0; u < sppRoot; u++)
                    {
                        for (int v = 0; v < sppRoot; v++)
                        {
                            var fu = (u + 0.5) / sppRoot;
                            var fv = (v + 0.5) / sppRoot;
                            var ray = camera.CastRay(x, y, w, h, fu, fv, rand);
                            var sample = sampler.Sample(scene, ray, rand);
                            buf.AddSample(x, y, sample);
                        }
                    }
                });
            }
            else
            {                
                int tile_size = 256;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var renderingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);
                var colorSettingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);

                // Define the granularity of work distribution (e.g., sub-tiles)
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
                                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);

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
                                            Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                            Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                            Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
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

            // Main rendering loop
            if (AdaptiveSamples > 0)
            {
                // Define global variables for accumulating variance and sample counts
                ConcurrentDictionary<int, double> pixelVariances = new ConcurrentDictionary<int, double>(); // Stores variance for each pixel
                ConcurrentDictionary<int, int> pixelSampleCounts = new ConcurrentDictionary<int, int>(); // Stores sample count for each pixel

                Parallel.For(0, w * h, po, (i, loopState) => {
                    int y = i / w, x = i % w;

                    // Calculate variance
                    double variance = buf.StandardDeviation(x, y).MaxComponent();
                    variance = Util.Clamp(variance / AdaptiveThreshold, 0, 1);
                    variance = Math.Pow(variance, AdaptiveExponent);

                    // Determine number of samples based on accumulated variance
                    int samples = (int)(variance * AdaptiveSamples);

                    // Update accumulated variance and sample count
                    int index = y * w + x;
                    if (!pixelVariances.ContainsKey(index))
                    {
                        pixelVariances[index] = variance;
                        pixelSampleCounts[index] = samples;
                    }
                    else
                    {
                        pixelVariances[index] = (pixelVariances[index] + variance) / 2; // Update variance with running average
                        pixelSampleCounts[index] += samples; // Accumulate sample count
                    }

                    // Perform sampling
                    for (int s = 0; s < samples; s++)
                    {
                        var fu = Random.Shared.NextDouble();
                        var fv = Random.Shared.NextDouble();
                        Ray ray = camera.CastRay(x, y, w, h, fu, fv, Random.Shared);
                        Colour sample = sampler.Sample(scene, ray, Random.Shared);
                        buf.AddSample(x, y, sample);
                    }
                });
            }


            if (FireflySamples > 0)
            {
                // Concurrent dictionary to track skipped pixels
                ConcurrentDictionary<(int, int), bool> skippedPixels = new ConcurrentDictionary<(int, int), bool>();

                Parallel.For(0, w * h, po, (i, loopState) =>
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
                                    // If it's a firefly, mark the pixel and break the loop
                                    skippedPixels.TryAdd((x, y), true);
                                    break;
                                }

                                buf.AddSample(x, y, sample);
                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
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
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
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
            // Define your criteria for identifying fireflies
            // Here's an example of a more sophisticated criterion:
            // Fireflies are pixels with high brightness and high deviation from the local neighborhood

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


        public System.Drawing.Bitmap IterativeRender(String pathTemplate, int iter)
        {
            iterations = iter;
            System.Drawing.Bitmap finalrender = null;

            for (int i = 1; i < iterations; i++)
            {
                Console.WriteLine("Iterations " + i + " of " + iterations);
                if (NumCPU.Equals(1))
                {
                    Render();
                }
                else
                {
                    RenderParallel();
                }
                this.pathTemplate = pathTemplate;
                finalrender = PBuffer.Image(Channel.ColorChannel);
                finalrender.Save(pathTemplate);
            }
            return PBuffer.Image(Channel.ColorChannel);
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
