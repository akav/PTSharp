using MathNet.Numerics.Random;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

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
                /*
                int tile_size = 256;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var renderingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);
                var colorSettingScheduler = new WorkStealingScheduler(Environment.ProcessorCount);

                // Define the granularity of work distribution (e.g., sub-tiles)
                int sub_tile_size = 8;

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
                colorSettingScheduler.Dispose();*/

                int tile_size = 256;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var scheduler = new WorkStealingScheduler(Environment.ProcessorCount);

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

                                        // Perform Monte Carlo integration by sampling random offsets within the pixel
                                        for (int s = 0; s < spp; s++)
                                        {
                                            // Generate random offsets within the pixel
                                            double u = (x + rand.NextDouble()) / w;
                                            double v = (y + rand.NextDouble()) / h;

                                            // Cast ray into the scene using the random offsets
                                            Ray ray = camera.CastRay(x, y, w, h, u, v, rand);

                                            // Evaluate scene illumination at the intersection point
                                            c += sampler.Sample(scene, ray, rand);
                                        }

                                        // Average the color over the number of samples
                                        c /= spp;

                                        // Update pixel buffer with the averaged color
                                        buf.AddSample(x, y, c);

                                        // Update bitmap with the averaged color
                                        var offset = (y * w + x) * 4; // BGR
                                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                    }
                                }
                            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
                        }
                    }
                }

                // Dispose the scheduler after rendering completes
                scheduler.Dispose();



                /*int tile_size = 256;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var scheduler = new WorkStealingScheduler(Environment.ProcessorCount);

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
                                        // Generate random offsets within the pixel
                                        double u = (x + rand.NextDouble()) / w;
                                        double v = (y + rand.NextDouble()) / h;
                                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, u, v, rand), rand);

                                        // Average the color over the number of samples
                                        c /= spp;
                                        buf.AddSample(x, y, c);

                                        var offset = (y * w + x) * 4; // BGR
                                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                    }
                                }
                            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
                        }
                    }
                }

                // Dispose the scheduler after rendering completes
                scheduler.Dispose();*/

                /*
                int tile_size = 64;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var scheduler = new WorkStealingScheduler(Environment.ProcessorCount);

                for (int tile_index = 0; tile_index < num_tiles_x * num_tiles_y; tile_index++)
                {
                    int tile_x = tile_index % num_tiles_x;
                    int tile_y = tile_index / num_tiles_x;
                    int x_start = tile_x * tile_size;
                    int y_start = tile_y * tile_size;
                    int x_end = Math.Min(x_start + tile_size, w);
                    int y_end = Math.Min(y_start + tile_size, h);

                    Task.Factory.StartNew(() =>
                    {
                        // Calculate the number of sub-tiles in each dimension based on the available processors
                        int sub_tile_size_x = (x_end - x_start + Environment.ProcessorCount - 1) / Environment.ProcessorCount;
                        int sub_tile_size_y = (y_end - y_start + Environment.ProcessorCount - 1) / Environment.ProcessorCount;

                        for (int sub_tile_y = 0; sub_tile_y < Environment.ProcessorCount; sub_tile_y++)
                        {
                            for (int sub_tile_x = 0; sub_tile_x < Environment.ProcessorCount; sub_tile_x++)
                            {
                                int sub_x_start = x_start + sub_tile_x * sub_tile_size_x;
                                int sub_y_start = y_start + sub_tile_y * sub_tile_size_y;
                                int sub_x_end = Math.Min(sub_x_start + sub_tile_size_x, x_end);
                                int sub_y_end = Math.Min(sub_y_start + sub_tile_size_y, y_end);

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

                                        var offset = (y * w + x) * 4; // BGR
                                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                    }
                                }
                            }
                        }
                    }, CancellationToken.None, TaskCreationOptions.None, scheduler);
                }

                // Dispose the scheduler after rendering completes
                scheduler.Dispose();*/

                /*
                int tile_size = 64;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                Parallel.ForEach(partitioner, (range, state) =>
                {
                    for (int tile_index = range.Item1; tile_index < range.Item2; tile_index++)
                    {
                        int tile_x = tile_index % num_tiles_x;
                        int tile_y = tile_index / num_tiles_x;
                        int x_start = tile_x * tile_size;
                        int y_start = tile_y * tile_size;
                        int x_end = Math.Min(x_start + tile_size, w);
                        int y_end = Math.Min(y_start + tile_size, h);

                        // Calculate the number of sub-tiles in each dimension based on the available processors
                        int sub_tile_size_x = (x_end - x_start + Environment.ProcessorCount - 1) / Environment.ProcessorCount;
                        int sub_tile_size_y = (y_end - y_start + Environment.ProcessorCount - 1) / Environment.ProcessorCount;

                        for (int sub_tile_y = 0; sub_tile_y < Environment.ProcessorCount; sub_tile_y++)
                        {
                            for (int sub_tile_x = 0; sub_tile_x < Environment.ProcessorCount; sub_tile_x++)
                            {
                                int sub_x_start = x_start + sub_tile_x * sub_tile_size_x;
                                int sub_y_start = y_start + sub_tile_y * sub_tile_size_y;
                                int sub_x_end = Math.Min(sub_x_start + sub_tile_size_x, x_end);
                                int sub_y_end = Math.Min(sub_y_start + sub_tile_size_y, y_end);

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

                                        var offset = (y * w + x) * 4; // BGR
                                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                    }
                                }
                            }
                        }
                    }
                });*/

                /*
                int tile_size = 64;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                Parallel.ForEach(partitioner, (range, state) =>
                {
                    for (int tile_index = range.Item1; tile_index < range.Item2; tile_index++)
                    {
                        int tile_x = tile_index % num_tiles_x;
                        int tile_y = tile_index / num_tiles_x;
                        int x_start = tile_x * tile_size;
                        int y_start = tile_y * tile_size;
                        int x_end = Math.Min(x_start + tile_size, w);
                        int y_end = Math.Min(y_start + tile_size, h);

                        byte[] localBuffer = new byte[(x_end - x_start) * (y_end - y_start) * 4]; // BGR

                        for (int y = y_start; y < y_end; y++)
                        {
                            for (int x = x_start; x < x_end; x++)
                            {
                                Colour accumulatedColor = new Colour(0, 0, 0);

                                // Accumulate color values from multiple samples
                                for (int s = 0; s < spp; s++)
                                {
                                    // Jittered sampling within each pixel
                                    Colour c = sampler.Sample(scene, camera.CastRay(x, y, w, h, (x + rand.NextDouble()) / w, (y + rand.NextDouble()) / h, rand), rand);

                                    // Sum the color values
                                    accumulatedColor += c;
                                }

                                // Average the accumulated color over the number of samples
                                accumulatedColor /= spp;

                                var offset = ((y - y_start) * (x_end - x_start) + (x - x_start)) * 4; // BGR
                                localBuffer[offset + 0] = (byte)(256 * Math.Clamp(accumulatedColor.Pow(1.0 / 2.2).r, 0.0, 0.999));
                                localBuffer[offset + 1] = (byte)(256 * Math.Clamp(accumulatedColor.Pow(1.0 / 2.2).g, 0.0, 0.999));
                                localBuffer[offset + 2] = (byte)(256 * Math.Clamp(accumulatedColor.Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }

                        // Copy the local buffer to the corresponding region in the bitmap
                        for (int y = y_start; y < y_end; y++)
                        {
                            for (int x = x_start; x < x_end; x++)
                            {
                                var offset = (y * w + x) * 4; // BGR
                                var localOffset = ((y - y_start) * (x_end - x_start) + (x - x_start)) * 4; // BGR

                                Program.bitmap[offset + 0] = localBuffer[localOffset + 0];
                                Program.bitmap[offset + 1] = localBuffer[localOffset + 1];
                                Program.bitmap[offset + 2] = localBuffer[localOffset + 2];
                            }
                        }

                        // Here you can update the display with the current state of the bitmap
                    }
                });*/

                /*
                int tile_size = 64;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                Parallel.ForEach(partitioner, (range, state) =>
                {
                    for (int tile_index = range.Item1; tile_index < range.Item2; tile_index++)
                    {
                        int tile_x = tile_index % num_tiles_x;
                        int tile_y = tile_index / num_tiles_x;
                        int x_start = tile_x * tile_size;
                        int y_start = tile_y * tile_size;
                        int x_end = Math.Min(x_start + tile_size, w);
                        int y_end = Math.Min(y_start + tile_size, h);

                        for (int y = y_start; y < y_end; y++)
                        {
                            for (int x = x_start; x < x_end; x++)
                            {
                                // Render the tile at the current coordinates, using the current resolution
                                Colour c = new Colour(0, 0, 0);

                                // Jittered sampling within each pixel
                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, (x + rand.NextDouble()) / w, (y + rand.NextDouble()) / h, rand), rand);


                                // Average the color over the number of samples
                                c /= spp;
                                buf.AddSample(x, y, c);

                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }

                        // Check if there are more tiles than processors and, if so, yield the thread to the scheduler
                        if (num_tiles_x * num_tiles_y > Environment.ProcessorCount * 2)
                        {
                            if (tile_index % Environment.ProcessorCount == 0)
                            {
                                Thread.Yield();
                            }
                        }
                    }
                });*/

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
                _ = Parallel.For(0, w * h, po, (i, loopState) =>
                  {
                      int y = i / w, x = i % w;
                      if (buf.StandardDeviation(x, y).MaxComponent() > FireflyThreshold)
                      {
                          for (int j = 0; j < FireflySamples; j++)
                          {
                              buf.AddSample(x, y, sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared), Random.Shared));
                          }
                      }
                  });
            }
            Console.WriteLine("time elapsed:" + sw.Elapsed);
            sw.Stop();
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
