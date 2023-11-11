using MathNet.Numerics.Random;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public static class EnumerableExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            foreach (var item in source)
            {
                await action(item);
            }
        }
    }

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
            r.PBuffer = Buffer.NewBuffer(w, h);
            r.SamplesPerPixel = 1;
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
            (int w, int h) = (buf.Width, buf.Height);
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
                                var offset = (y * w + x) * 4;
                                Span<byte> pixelSpan = Program.bitmap.AsSpan(offset, 4); // Convert array to Span
                                pixelSpan[0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                pixelSpan[1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                pixelSpan[2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
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
            (int w, int h) = (buf.Width, buf.Height);
            int spp = SamplesPerPixel;
            int sppRoot = (int)(Math.Sqrt(SamplesPerPixel));
            scene.Compile();
            scene.rays = 0;

            // Stop watch timer 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Random Number Generator from on Math.Numerics
            var rand = new SystemRandomSource(sw.Elapsed.Milliseconds, true);

            // Frame resolution
            int totalPixels = h * w;
            double invWidth = 1.0f / w;
            double invHeight = 1.0f / h;

            //int ncpu = 1;//Environment.ProcessorCount;
            //ThreadPool.SetMaxThreads(ncpu, ncpu);
            //var ch = new BlockingCollection<int>();

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


                // Create a thread-safe buffer object to store the sample results
                //PixelBuffer buf = new PixelBuffer(w, h);

                // 16 sec
                
                // Process the pixels in parallel, ensuring order of processing
                /*Parallel.For(0, w * h, (i) =>
                {
                    int x = i % w;
                    int y = i / w;

                    Colour c = new Colour(0, 0, 0);

                    for (int p = 0; p < spp; p++)
                    {
                        // Render the pixel at the current coordinates, using the current resolution
                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                    }

                    // Average the color over the number of samples
                    c /= spp;

                    // Add the sample result to the buffer object in a thread-safe manner
                    buf.AddSample(x, y, c);

                    // Update the bitmap with the pixel color
                    var offset = (y * w + x) * 4; // BGR
                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                });*/


                // 20 seconds
                // Shuffle the pixels
                /*var pixels = new List<(int x, int y)>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixels.Add((x, y));
                    }
                }

                var pixelSpan = pixels.ToArray(); // Convert List to Span

                for (int i = pixelSpan.Length - 1; i > 0; i--)
                {
                    int j = rand.Next(0, i + 1);
                    (pixelSpan[j], pixelSpan[i]) = (pixelSpan[i], pixelSpan[j]);
                }

                // Process the pixels in parallel
                var processedPixels = new ConcurrentDictionary<(int x, int y), bool>();
                var lockObject = new object();

                Parallel.ForEach(pixelSpan, po, (pixel) =>
                {
                    int x = pixel.x;
                    int y = pixel.y;

                    Colour c = new Colour(0, 0, 0);

                    for (int p = 0; p < spp; p++)
                    {
                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                    }

                    c /= spp;

                    lock (lockObject)
                    {
                        buf.AddSample(x, y, c);
                    }

                    var offset = (y * w + x) * 4;
                    Span<byte> pixelSpan = Program.bitmap.AsSpan(offset, 4); // Convert array to Span
                    pixelSpan[0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    pixelSpan[1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    pixelSpan[2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                });*/

                /*
                // Shuffle the pixels
                List<(int x, int y)> pixels = new List<(int x, int y)>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixels.Add((x, y));
                    }
                }
                
                for (int i = pixels.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(0, i + 1);
                    (int x1, int y1) temp = pixels[i];
                    pixels[i] = pixels[j];
                    pixels[j] = temp;
                }

                // Process the pixels in parallel
                ConcurrentDictionary<(int x, int y), bool> processedPixels = new ConcurrentDictionary<(int x, int y), bool>();
                // Create a lock object
                object lockObject = new object();

                // Process the pixels in parallel
                Parallel.ForEach(pixels, po, (pixel) =>
                {
                    int x = pixel.x;
                    int y = pixel.y;

                    Colour c = new Colour(0, 0, 0);

                    for (int p = 0; p < spp; p++)
                    {
                        // Render the pixel at the current coordinates, using the current resolution
                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                    }

                    // Average the color over the number of samples
                    c /= spp;

                    // Lock the buffer object to ensure thread safety
                    lock (lockObject)
                    {
                        buf.AddSample(x, y, c);
                    }

                    // Update the bitmap with the pixel color
                    var offset = (y * w + x) * 4; // BGR
                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                });*/

                // 13 seconds -
                // Process the pixels in parallel
                /*ConcurrentBag<(int x, int y, Colour c)> renderedPixels = new ConcurrentBag<(int x, int y, Colour c)>();

                Parallel.For(0, h, y =>
                {
                    for (int x = 0; x < w; x++)
                    {
                        Colour c = new Colour(0, 0, 0);

                        for (int p = 0; p < spp; p++)
                        {
                            // Render the pixel at the current coordinates, using the current resolution
                            c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                        }

                        // Average the color over the number of samples
                        c /= spp;

                        renderedPixels.Add((x, y, c));
                    }
                });

                // Update the bitmap with the rendered pixels
                foreach (var pixel in renderedPixels)
                {
                    buf.AddSample(pixel.x, pixel.y, pixel.c);
                    var offset = (pixel.y * w + pixel.x) * 4; // BGR
                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(pixel.x, pixel.y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(pixel.x, pixel.y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(pixel.x, pixel.y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                }*/


                // 13 sec
                // Generate a list of pixel coordinates
                /*List<(int, int)> pixel_coords = new List<(int, int)>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixel_coords.Add((x, y));
                    }
                }

                // Shuffle the pixel coordinates
                
                for (int i = pixel_coords.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(i + 1);
                    (int, int) temp = pixel_coords[i];
                    pixel_coords[i] = pixel_coords[j];
                    pixel_coords[j] = temp;
                }

                ConcurrentDictionary<(int, int), Colour> processedPixels = new ConcurrentDictionary<(int, int), Colour>();

                Parallel.ForEach(pixel_coords, (pixel) =>
                {
                    int x = pixel.Item1;
                    int y = pixel.Item2;

                    Colour c = new Colour(0, 0, 0);

                    for (int p = 0; p < spp; p++)
                    {
                        // Render the pixel at the current coordinates, using the current resolution
                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                    }

                    // Average the color over the number of samples
                    c /= spp;

                    // Add the color to the dictionary
                    processedPixels[(x, y)] = c;
                });

                // Copy the processed pixels to the buffer
                foreach (var kvp in processedPixels)
                {
                    buf.AddSample(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
                    var offset = (kvp.Key.Item2 * w + kvp.Key.Item1) * 4; // BGR
                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(kvp.Value.Pow(1.0 / 2.2).r, 0.0, 0.999));
                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(kvp.Value.Pow(1.0 / 2.2).g, 0.0, 0.999));
                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(kvp.Value.Pow(1.0 / 2.2).b, 0.0, 0.999));
                }*/




                
                // Set the number of samples per pixel
                //int spp = 16;
                /*
                // Set the maximum number of iterations
                int max_iterations = 2;

                // Create a buffer to hold the accumulated samples
                //var buf = new Buffer(w, h, spp);

                // Render the image progressively
                for (int i = 0; i < max_iterations; i++)
                {
                    // Render each pixel in parallel using PLINQ
                    ParallelEnumerable.Range(0, h).ForAll(y =>
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // Generate a random offset for this sample
                            var rand = new Random((x + y * w) * (i + 1));

                            // Sample the color at this pixel
                            Colour c = new Colour(0, 0, 0);

                            for (int p = 0; p < spp; p++)
                            {
                                // Render the pixel using the current resolution
                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                            }

                            // Average the color over the number of samples
                            c /= spp;

                            // Add the sample to the buffer
                            buf.AddSample(x, y, c);

                            // Update the bitmap with the pixel color
                            var offset = (y * w + x) * 4; // BGR
                            Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                            Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                            Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                        }
                    });

                    // Show the current image
                    //Program.ShowImage();
                }*/



                // 16 seconds
                int tile_size = 128;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                for (int tile_y = 0; tile_y < num_tiles_y; tile_y++)
                {
                    for (int tile_x = 0; tile_x < num_tiles_x; tile_x++)
                    {
                        int x_start = tile_x * tile_size;
                        int y_start = tile_y * tile_size;
                        int x_end = Math.Min(x_start + tile_size, w);
                        int y_end = Math.Min(y_start + tile_size, h);

                        Parallel.For(y_start, y_end, y =>
                        {
                            for (int x = x_start; x < x_end; x++)
                            {
                                Colour c = new Colour(0, 0, 0);

                                for (int p = 0; p < spp; p++)
                                {
                                    // Render the tile at the current coordinates, using the current resolution
                                    c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                                }

                                // Average the color over the number of samples
                                c /= spp;
                                buf.AddSample(x, y, c);

                                // Update the bitmap with the pixel color
                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        });
                    }
                }



                // 15 seconds
                /*int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                // Create a list of tile indices
                List<int> tile_indices = new List<int>();
                for (int tile_index = 0; tile_index < num_tiles_x * num_tiles_y; tile_index++)
                {
                    tile_indices.Add(tile_index);
                }

                // Create a partitioner that partitions the list of tile indices into chunks
                var partitioner = Partitioner.Create(tile_indices, true);

                // Process the tiles using Parallel.ForEach with work stealing
                Parallel.ForEach(partitioner, (tile_index) =>
                {
                    int tile_x = tile_index % num_tiles_x;
                    int tile_y = tile_index / num_tiles_x;
                    int x_start = tile_x * tile_size;
                    int y_start = tile_y * tile_size;
                    int x_end = Math.Min(x_start + tile_size, w);
                    int y_end = Math.Min(y_start + tile_size, h);

                    // Render the tile
                    for (int y = y_start; y < y_end; y++)
                    {
                        for (int x = x_start; x < x_end; x++)
                        {
                            // Render the tile at the current coordinates, using the current resolution
                            Colour c = new Colour(0, 0, 0);
                            c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);

                            // Average the color over the number of samples
                            c /= spp;
                            buf.AddSample(x, y, c);

                            // Update the bitmap with the pixel color
                            var offset = (y * w + x) * 4; // BGR
                            Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                            Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                            Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                        }
                    }
                });*/

                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                for (spp = 1; spp <= 10; spp *= 2)
                {
                    List<Task> tasks = new List<Task>();

                    for (int tile_index = 0; tile_index < num_tiles_x * num_tiles_y; tile_index++)
                    {
                        int tile_x = tile_index % num_tiles_x;
                        int tile_y = tile_index / num_tiles_x;
                        int x_start = tile_x * tile_size;
                        int y_start = tile_y * tile_size;
                        int x_end = Math.Min(x_start + tile_size, w);
                        int y_end = Math.Min(y_start + tile_size, h);

                        Task task = Task.Run(() =>
                        {
                            for (int y = y_start; y < y_end; y++)
                            {
                                for (int x = x_start; x < x_end; x++)
                                {
                                    // Render the tile at the current coordinates, using the current resolution and sample count
                                    Colour c = new Colour(0, 0, 0);
                                    for (int i = 0; i < spp; i++)
                                    {
                                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                    }

                                    // Average the color over the number of samples
                                    c /= spp;
                                    buf.AddSample(x, y, c);

                                    // Update the bitmap with the new pixel value
                                    var offset = (y * w + x) * 4; // BGR
                                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                }
                            }
                        });

                        tasks.Add(task);
                    }

                    Task.WaitAll(tasks.ToArray());

                    // Display the image after each pass
                    //DisplayImage(Program.bitmap);
                }*/


                //  13 seconds
                /*int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                Parallel.ForEach(Partitioner.Create(0, num_tiles_x * num_tiles_y), tile_range =>
                {
                    for (int tile_index = tile_range.Item1; tile_index < tile_range.Item2; tile_index++)
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
                }); */

                // 15 seconds
                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                List<Task> tasks = new List<Task>();

                for (int tile_index = 0; tile_index < num_tiles_x * num_tiles_y; tile_index++)
                {
                    int tile_x = tile_index % num_tiles_x;
                    int tile_y = tile_index / num_tiles_x;
                    int x_start = tile_x * tile_size;
                    int y_start = tile_y * tile_size;
                    int x_end = Math.Min(x_start + tile_size, w);
                    int y_end = Math.Min(y_start + tile_size, h);

                    Task task = Task.Run(() =>
                    {
                        var pixels = buf.Pixels; // Store a local reference to the pixels array for this thread

                        for (int y = y_start; y < y_end; y++)
                        {
                            for (int x = x_start; x < x_end; x++)
                            {
                                // Render the tile at the current coordinates, using the current resolution
                                Colour c = new Colour(0, 0, 0);
                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);

                                // Average the color over the number of samples
                                c /= spp;
                                buf.AddSample(x, y, c);

                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }
                    });

                    tasks.Add(task);
                }

                Task.WaitAll(tasks.ToArray());*/

                /* 15.9seconds reverse tile order
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var workload = new ConcurrentBag<int>(Enumerable.Range(0, num_tiles_x * num_tiles_y));
                var loadBalancer = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

                Parallel.ForEach(workload, new ParallelOptions { TaskScheduler = loadBalancer }, (tile_index, state) =>
                {
                    for (int p = 0; p < spp; p++)
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
                                c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                buf.AddSample(x, y, c);

                                var offset = (y * w + x) * 4; // BGR
                                Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                            }
                        }
                    }
                });*/

                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;

                // Create a concurrent bag to hold the ranges of tiles to be processed
                ConcurrentBag<(int, int)> tileRanges = new ConcurrentBag<(int, int)>();
                for (int i = 0; i < num_tiles_x * num_tiles_y; i++)
                {
                    tileRanges.Add((i, i + 1));
                }

                // Spawn worker threads
                Thread[] threads = new Thread[Environment.ProcessorCount];
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        Random rand = new Random();

                        while (tileRanges.TryTake(out (int, int) range))
                        {
                            for (int p = 0; p < spp; p++)
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
                                            c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                            buf.AddSample(x, y, c);

                                            var offset = (y * w + x) * 4; // BGR
                                            Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                            Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                            Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                        }
                                    }

                                    // Check if there are more tiles than processors and, if so, yield the thread to the scheduler
                                    if (tileRanges.Count > Environment.ProcessorCount * 2)
                                    {
                                        if (tile_index % Environment.ProcessorCount == 0)
                                        {
                                            Thread.Yield();
                                        }
                                    }
                                }
                            }
                        }
                    });
                    threads[i].Start();
                }*/

                
                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = 24 }, range =>
                {
                    Random rand = Random.Shared;

                    for (int p = 0; p < spp; p++)
                    {
                        for (int tile_index = range.Item1; tile_index < range.Item2; tile_index++)
                        {
                            if (cts.IsCancellationRequested)
                            {
                                return;
                            }

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
                                    c += sampler.Sample(scene, camera.CastRay(x, y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                                    buf.AddSample(x, y, c);

                                    var offset = (y * w + x) * 4; // BGR
                                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));
                                }
                            }
                        }
                    }
                }); */



                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                    Parallel.ForEach(partitioner, po, (range, state) =>
                    {
                        for (int p = 0; p < spp; p++)
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
                                        c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
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
                        }
                    });*/

                
                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                int stride = w * 4;

                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                Parallel.ForEach(partitioner, po, (range, state) =>
                {
                    for (int p = 0; p < spp; p++)
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
                                int offset = y * stride + x_start * 4;
                                for (int x = x_start; x < x_end; x++)
                                {
                                    // Render the tile at the current coordinates, using the current resolution
                                    Colour c = new Colour(0, 0, 0);
                                    c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                    buf.AddSample(x, y, c);

                                    var color = buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2);
                                    Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(color.r, 0.0, 0.999));
                                    Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(color.g, 0.0, 0.999));
                                    Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(color.b, 0.0, 0.999));

                                    offset += 4;
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
                    }
                });*/


                /*
                int tile_size = 32;
                int num_tiles_x = (w + tile_size - 1) / tile_size;
                int num_tiles_y = (h + tile_size - 1) / tile_size;
                var partitioner = Partitioner.Create(0, num_tiles_x * num_tiles_y);

                // Option 1: Use ParallelOptions to set MaxDegreeOfParallelism
                Parallel.ForEach(partitioner, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (range, state) =>
                {
                    // Option 2: Use unsafe code for faster bitmap manipulation
                    unsafe
                    {
                        fixed (byte* ptr = Program.bitmap)
                        {
                            for (int p = 0; p < spp; p++)
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
                                            c += sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                                            buf.AddSample(x, y, c);

                                            var offset = (y * w + x) * 4; // BGR

                                            // Option 3: Use pointer arithmetic for faster bitmap manipulation
                                            byte* pixel = ptr + offset;
                                            Colour color = buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2);
                                            *pixel = (byte)(256 * Math.Clamp(color.r, 0.0, 0.999));
                                            *(pixel + 1) = (byte)(256 * Math.Clamp(color.g, 0.0, 0.999));
                                            *(pixel + 2) = (byte)(256 * Math.Clamp(color.b, 0.0, 0.999));
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
                            }
                        }
                    }
                });*/



                
                /*
                // Generate a list of pixel coordinates
                List<(int, int)> pixel_coords = new List<(int, int)>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixel_coords.Add((x, y));
                    }
                }

                // Shuffle the pixel coordinates
                for (int i = pixel_coords.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(i + 1);
                    (int, int) temp = pixel_coords[i];
                    pixel_coords[i] = pixel_coords[j];
                    pixel_coords[j] = temp;
                }

                // Create a work-stealing scheduler with the number of worker threads equal to the number of processor cores
                int concurrencyLevel = Environment.ProcessorCount;
                var scheduler = new WorkStealingScheduler(concurrencyLevel);

                // Create an array of tasks to render the pixels in parallel
                Task[] tasks = new Task[pixel_coords.Count];
                for (int i = 0; i < pixel_coords.Count; i++)
                {
                    (int x, int y) coords = pixel_coords[i];

                    tasks[i] = Task.Factory.StartNew(() =>
                    {
                        Colour c = new Colour(0, 0, 0);

                        for (int p = 0; p < spp; p++)
                        {
                            // Render the pixel at the current coordinates, using the current resolution
                            c += sampler.Sample(scene, camera.CastRay(coords.x, coords.y, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                        }

                        // Average the color over the number of samples
                        c /= spp;
                        buf.AddSample(coords.x, coords.y, c);

                        // Update the bitmap with the pixel color
                        var offset = (coords.y * w + coords.x) * 4; // BGR
                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.GetPixel(coords.x, coords.y).Pow(1.0 / 2.2).r, 0.0, 0.999));
                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.GetPixel(coords.x, coords.y).Pow(1.0 / 2.2).g, 0.0, 0.999));
                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.GetPixel(coords.x, coords.y).Pow(1.0 / 2.2).b, 0.0, 0.999));
                    });
                }

                // Wait for all the tasks to complete
                Task.WaitAll(tasks);

                // Dispose the work-stealing scheduler
                scheduler.Dispose();*/

                /*
                // Generate a list of pixel coordinates
                List<(int, int)> pixel_coords = new List<(int, int)>();
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        pixel_coords.Add((x, y));
                    }
                }

                // Shuffle the pixel coordinates
                for (int i = pixel_coords.Count - 1; i > 0; i--)
                {
                    int j = rand.Next(i + 1);
                    (int, int) temp = pixel_coords[i];
                    pixel_coords[i] = pixel_coords[j];
                    pixel_coords[j] = temp;
                }

                // Render the pixels in parallel
                await Task.Run(() =>
                {
                    Parallel.ForEach(pixel_coords, (coord) =>
                    {
                        Colour c = new Colour(0, 0, 0);

                        for (int p = 0; p < spp; p++)
                        {
                            // Render the pixel at the current coordinates, using the current resolution
                            c += sampler.Sample(scene, camera.CastRay(coord.Item1, coord.Item2, w, h, Random.Shared.NextDouble(), Random.Shared.NextDouble(), rand), rand);
                        }

                        // Average the color over the number of samples
                        c /= spp;
                        buf.AddSample(coord.Item1, coord.Item2, c);

                        // Update the bitmap with the pixel color
                        var offset = (coord.Item2 * w + coord.Item1) * 4; // BGR
                        Program.bitmap[offset + 0] = (byte)(256 * Math.Clamp(buf.GetPixelColor(coord.Item1, coord.Item2).Pow(1.0 / 2.2).r, 0.0, 0.999));
                        Program.bitmap[offset + 1] = (byte)(256 * Math.Clamp(buf.GetPixelColor(coord.Item1, coord.Item2).Pow(1.0 / 2.2).g, 0.0, 0.999));
                        Program.bitmap[offset + 2] = (byte)(256 * Math.Clamp(buf.GetPixelColor(coord.Item1, coord.Item2).Pow(1.0 / 2.2).b, 0.0, 0.999));
                    });
                });*/
                     
                /*
                Parallel.ForEach(Partitioner.Create(0, w * h), range =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        if (cts.Token.IsCancellationRequested)
                        {
                            return;
                        }

                        int y = i / w, x = i % w;
                        var color = sampler.Sample(scene, camera.CastRay(x, y, w, h, rand.NextDouble(), rand.NextDouble(), rand), rand);
                        buf.AddSample(x, y, color);
                        var offset = (y * w + x) * 4;
                        Span<byte> pixelSpan = Program.bitmap.AsSpan(offset, 4); // Convert array to Span
                        pixelSpan[0] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).r, 0.0, 0.999));
                        pixelSpan[1] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).g, 0.0, 0.999));
                        pixelSpan[2] = (byte)(256 * Math.Clamp(buf.Pixels[(x, y)].Color().Pow(1.0 / 2.2).b, 0.0, 0.999));

                    }
                });*/
            }
            if (AdaptiveSamples > 0)
            {
                Parallel.For(0, w * h, po, (i, loopState) =>
                {
                    int y = i / w, x = i % w;
                    double v = buf.StandardDeviation(x, y).MaxComponent();
                    v = Util.Clamp(v / AdaptiveThreshold, 0, 1);
                    v = Math.Pow(v, AdaptiveExponent);
                    int samples = (int)(v * AdaptiveSamples);
                    
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
