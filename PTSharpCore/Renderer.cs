using MathNet.Numerics.Random;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.OpenCL;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime.Cuda;
using ILGPU.Algorithms;
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
            r.SamplesPerPixel = 1;
            r.StratifiedSampling = false;
            r.AdaptiveSamples = 0;
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

        public static void MyRandomKernel(Index1D index, RNGView<XorShift64Star> rng, ArrayView1D<double, Stride1D.Dense> view)
        {
            view[index] = rng.NextDouble();
        }

        //public void RenderParallel(Accelerator a, Device dev)
        public void RenderParallel()
        {
            Scene scene = Scene;
            Camera camera = Camera;
            Sampler sampler = Sampler;

            //ThreadLocal<Sampler> sampler = new ThreadLocal<Sampler>(() =>
            //{
            //    return Sampler;
            //});

            Buffer buf = PBuffer;

            //ThreadLocal<Buffer> buf = new ThreadLocal<Buffer>(() =>
            //{
            //   return PBuffer;
            //});

            (int w, int h) = (buf.W, buf.H);
            int spp = SamplesPerPixel;
            int sppRoot = (int)(Math.Sqrt(SamplesPerPixel));
            scene.Compile();
            scene.rays = 0;

            // Stop watch timer 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Random Number Generator from on Math.Numerics
            var rand = Random.Shared; //new SystemRandomSource(sw.Elapsed.Milliseconds, true);

            // Frame resolution
            int totalPixels = h * w;
            double invWidth = 1.0f / w;
            double invHeight = 1.0f / h;

            // Create a cancellation token for Parallel.For loop control
            CancellationTokenSource cts = new CancellationTokenSource();

            // ParallelOptions for Parallel.For
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;

            // Set number of cores/threads
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;

            var numbers = Enumerable.Range(0, w * h).ToList();

            // Experiment:
            // Use ILGPU to generate an array of random numbers
            //using var rng = RNG.Create<XorShift64Star>(a, rand);
            //var rngView = rng.GetView(a.WarpSize);
            //using var bufferfu = a.Allocate1D<double>(w * h);
            //var kernelfu = a.LoadAutoGroupedStreamKernel<Index1D, RNGView<XorShift64Star>, ArrayView1D<double, Stride1D.Dense>>(MyRandomKernel);
            //kernelfu((int)bufferfu.Length, rngView, bufferfu.View);

            //ThreadLocal<double[]> fuRandomValues = new ThreadLocal<double[]>(() =>
            //{
            //    return bufferfu.GetAsArray1D();
            //});

            Console.WriteLine("{0} x {1}, {2} spp, {3} core(s)", w, h, spp, po.MaxDegreeOfParallelism);

            if (StratifiedSampling)
            {
                _ = Parallel.For(0, w * h, po, (i, loopState) =>
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
                //Random subsampling
                ConcurrentDictionary<(int, int), Ray> rayBuffer = new ConcurrentDictionary<(int, int), Ray>();
                ThreadLocal<Colour> colourLocal = new();

                for (int j = 0; j < spp; j++)
                {
                    Parallel.For(0, w * h, po, (index) =>
                    {
                        var x = index % w;
                        var y = index / w;
                        var fu = Random.Shared.NextDouble();
                        var fv = Random.Shared.NextDouble();
                        var c = sampler.Sample(scene, camera.CastRay(x, y, w, h, fu, fv, Random.Shared), Random.Shared);
                        buf.AddSample(x, y, c);
                        //rayBuffer.TryAdd((x, y), camera.CastRay(x, y, w, h, fu, fv, Random.Shared));
                        //buf.AddSample(x, y, sampler.Sample(scene, rayBuffer[(x, y)], Random.Shared));
                    });
                }
            }

            if (AdaptiveSamples > 0)
            {
                _ = Parallel.For(0, w * h, po, (i, loopState) =>
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

            //using Context context = Context.Create(builder => builder.Default().EnableAlgorithms());
            //Device dev = context.GetPreferredDevice(preferCPU: false);
            //using var accelerator = dev.CreateAccelerator(context);
            //Console.WriteLine($"Performing operations on {accelerator}\n");

            for (int i = 1; i < iterations; i++)
            {
                Console.WriteLine("Iterations " + i + " of " + iterations);
                if (NumCPU.Equals(1))
                {
                    Render();
                }
                else
                {
                    //RenderParallel(accelerator, dev);
                    RenderParallel();
                }
                this.pathTemplate = pathTemplate;
                finalrender = PBuffer.Image(Channel.ColorChannel);
                finalrender.Save(pathTemplate);
            }

            //accelerator.Dispose();
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
