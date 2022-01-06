using MathNet.Numerics.Random;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace PTSharpCore
{
    class Renderer
    {
        Scene Scene;
        Camera Camera;
        Sampler Sampler;
        Buffer PBuffer;
        int SamplesPerPixel;
        public bool StratifiedSampling;
        public int AdaptiveSamples;
        float AdaptiveThreshold;
        float AdaptiveExponent;
        public int FireflySamples;
        float FireflyThreshold;
        int NumCPU;
        int iterations;
        String pathTemplate;

        Renderer() {}
        
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
            int sppRoot = (int)(MathF.Sqrt(SamplesPerPixel));
            scene.Compile();
            scene.rays = 0;
            Random rand = new Random();
            float fu, fv;

            // Stop watch timer 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            float invWidth = 1.0f / w;
            float invHeight = 1.0f / h;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {

                    if (StratifiedSampling)
                    {
                        for (int u = 0; u < sppRoot; u++)
                        {
                            for (int v = 0; v < sppRoot; v++)
                            {
                                fu = (u + 0.5F) / sppRoot;
                                fv = (v + 0.5F) / sppRoot;
                                Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                                Colour sample = sampler.Sample(scene, ray);
                                buf.AddSample(x, y, sample);
                            }
                        }
                        
                    }
                    else
                    {
                        // Random subsampling
                        for (int p = 0; p < spp; p++)
                        {
                            fu = (x+Random.Shared.NextSingle())*invWidth;
                            fv = (y+Random.Shared.NextSingle())*invHeight;
                            Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                            Colour sample = sampler.Sample(scene, ray);
                            sample += sample;
                            buf.AddSample(x, y, sample);
                        }
                        
                    }
                    // Adaptive Sampling
                    if (AdaptiveSamples > 0)
                    {
                        float v = buf.StandardDeviation(x, y).MaxComponent();
                        v = Util.Clamp(v / AdaptiveThreshold, 0, 1);
                        v = MathF.Pow(v, AdaptiveExponent);
                        int samples = (int)(v * AdaptiveSamples);
                        for (int d = 0; d < samples; d++)
                        {

                            fu = Random.Shared.NextSingle();
                            fv = Random.Shared.NextSingle();
                            Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                            Colour sample = sampler.Sample(scene, ray);
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
                                fu = (x + Random.Shared.NextSingle()) * invWidth;
                                fv = (y + Random.Shared.NextSingle()) * invHeight;
                                Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                                sample += sampler.Sample(scene, ray);
                                PBuffer.AddSample(x, y, sample);
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
            int sppRoot = (int)(MathF.Sqrt(SamplesPerPixel));
            scene.Compile();
            scene.rays = 0;

            // Stop watch timer 
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

            // Random Number Generator from on Math.Numerics
            //System.Random rand = new SystemRandomSource(sw.Elapsed.Milliseconds, true);

            // Frame resolution
            int totalPixels = h * w;
            float invWidth = 1.0f / w;
            float invHeight = 1.0f / h;

            // Create a cancellation token for Parallel.For loop control
            CancellationTokenSource cts = new CancellationTokenSource();

            // ParallelOptions for Parallel.For
            ParallelOptions po = new ParallelOptions();
            po.CancellationToken = cts.Token;

            // Set number of cores/threads
            po.MaxDegreeOfParallelism = Environment.ProcessorCount;

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
                              var fu = (u + 0.5F) / sppRoot;
                              var fv = (v + 0.5F) / sppRoot;
                              Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                              Colour sample = sampler.Sample(scene, ray);
                              buf.AddSample(x, y, sample);
                          }
                      }
                  });
                //Console.WriteLine("time elapsed:" + sw.Elapsed);
            }
            else
            {
                //Random subsampling
                _ = Parallel.ForEach(Partitioner.Create(0, totalPixels), po, (range) =>
                  {
                      for (int i = range.Item1; i < range.Item2; i++)
                      {
                          for (int s = 0; s < spp; s++)
                          {
                              int y = i / w, x = i % w;
                              var fu = (x + Random.Shared.NextSingle()) * invWidth;
                              var fv = (y + Random.Shared.NextSingle()) * invHeight;
                              var ray = camera.CastRay(x, y, w, h, fu, fv);
                              var sample = sampler.Sample(scene, ray);
                              buf.AddSample(x, y, sample);
                          }
                      }
                  });
                //Console.WriteLine("time elapsed:" + sw.Elapsed);
            }

            if (AdaptiveSamples > 0)
            {
                _ = Parallel.For(0, w * h, po, (i, loopState) =>
                  {
                      int y = i / w, x = i % w;
                      float v = buf.StandardDeviation(x, y).MaxComponent();
                      v = Util.Clamp(v / AdaptiveThreshold, 0, 1);
                      v = MathF.Pow(v, AdaptiveExponent);
                      int samples = (int)(v * AdaptiveSamples);
                      for (int s = 0; s < samples; s++)
                      {

                          var fu = Random.Shared.NextSingle();
                          var fv = Random.Shared.NextSingle();
                          Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                          Colour sample = sampler.Sample(scene, ray);
                          buf.AddSample(x, y, sample);
                      }
                  });
            }

            if (FireflySamples > 0)
            {
                _ = Parallel.For(0, w * h, po, (i, loopState) =>
                  {
                      int y = i / w, x = i % w;
                      if (PBuffer.StandardDeviation(x, y).MaxComponent() > FireflyThreshold)
                      {

                          Parallel.For(0, FireflySamples, po, (e, loop) =>
                          {
                              Colour sample = new Colour(0, 0, 0);
                              var fu = (x + Random.Shared.NextSingle()) * invWidth;
                              var fv = (y + Random.Shared.NextSingle()) * invHeight;
                              Ray ray = camera.CastRay(x, y, w, h, fu, fv);
                              sample = sampler.Sample(scene, ray);
                              buf.AddSample(x, y, sample);
                          });
                      }
                  });
            }
            //sw.Stop();
        }

        public System.Drawing.Bitmap IterativeRender(String pathTemplate, int iterations)
        {
            this.iterations = iterations;
            System.Drawing.Bitmap finalrender = null;
            for (int iter = 1; iter < this.iterations; iter++)
            {
                Console.WriteLine("[Iteration:" + iter + " of " + iterations + "]");
                if (NumCPU.Equals(1))
                {
                    Render();
                } else
                {
                    RenderParallel();
                }
                this.pathTemplate = pathTemplate;
                finalrender = PBuffer.Image(Channel.ColorChannel);
                Console.WriteLine("Writing file...");
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
