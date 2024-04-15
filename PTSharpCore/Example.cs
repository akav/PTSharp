using glTFLoader.Schema;
using Silk.NET.SDL;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static ILGPU.IR.MethodCollections;

namespace PTSharpCore
{
    class Example
    {
        public static List<Triangle> Block(Vector p, Material material, List<int> tiles)
        {
            const int N = 16;
            const double A = 1.0 / 2048;
            const double B = 1.0 / N - A;            

            List<Vector> VP = new List<Vector>
            {
                new Vector(-0.5, -0.5, 0.5),
                new Vector(0.5, -0.5, 0.5),
                new Vector(-0.5, 0.5, 0.5),
                new Vector(0.5, 0.5, 0.5),
                new Vector(-0.5, 0.5, -0.5),
                new Vector(0.5, 0.5, -0.5),
                new Vector(-0.5, -0.5, -0.5),
                new Vector(0.5, -0.5, -0.5)
            };

            List<Vector> VT = new List<Vector>
            {
                new Vector(A, A, 0),
                new Vector(B, A, 0),
                new Vector(A, B, 0),
                new Vector(B, B, 0)
            };

            List<Tuple<Vector[], Vector[]>> Triangles = new List<Tuple<Vector[], Vector[]>>
            {
                Tuple.Create(new Vector[] { VP[0], VP[1], VP[2] }, new Vector[] { VT[0], VT[1], VT[2] }),
                Tuple.Create(new Vector[] { VP[2], VP[1], VP[3] }, new Vector[] { VT[2], VT[1], VT[3] }),
                Tuple.Create(new Vector[] { VP[2], VP[3], VP[4] }, new Vector[] { VT[0], VT[1], VT[2] }),
                Tuple.Create(new Vector[] { VP[4], VP[3], VP[5] }, new Vector[] { VT[2], VT[1], VT[3] }),
                Tuple.Create(new Vector[] { VP[4], VP[5], VP[6] }, new Vector[] { VT[3], VT[2], VT[1] }),
                Tuple.Create(new Vector[] { VP[6], VP[5], VP[7] }, new Vector[] { VT[1], VT[2], VT[0] }),
                Tuple.Create(new Vector[] { VP[6], VP[7], VP[0] }, new Vector[] { VT[0], VT[1], VT[2] }),
                Tuple.Create(new Vector[] { VP[0], VP[7], VP[1] }, new Vector[] { VT[2], VT[1], VT[3] }),
                Tuple.Create(new Vector[] { VP[1], VP[7], VP[3] }, new Vector[] { VT[0], VT[1], VT[2] }),
                Tuple.Create(new Vector[] { VP[3], VP[7], VP[5] }, new Vector[] { VT[2], VT[1], VT[3] }),
                Tuple.Create(new Vector[] { VP[6], VP[0], VP[4] }, new Vector[] { VT[0], VT[1], VT[2] }),
                Tuple.Create(new Vector[] { VP[4], VP[0], VP[2] }, new Vector[] { VT[2], VT[1], VT[3] })
            };

            List<Triangle> result = new List<Triangle>();
            
            for (int i = 0; i < Triangles.Count; i++)
            {
                var t = Triangles[i];
                var tile = tiles[i / 2];
                var m = new Vector((float)(tile % N) / N, (float)(tile / N) / N, 0);
                var v1 = t.Item1[0] + p;
                var v2 = t.Item1[1] + p;
                var v3 = t.Item1[2] + p;
                var t1 = t.Item2[0] + m;
                var t2 = t.Item2[1] + m;
                var t3 = t.Item2[2] + m;
                result.Add(Triangle.NewTriangle(v1, v2, v3, t1, t2, t3, material));
            }
            return result;
        }

        public static void craft(int width, int height)
        {
            // Note: Texture used for craft example is available in the examples folder
            // https://github.com/fogleman/pt/blob/master/examples/texture.png
            Scene scene = new Scene();
            scene.Color = Colour.White;
            ITexture texture;

            List<int> Dirt = new List<int> { 0, 0, 0, 0, 0, 0 };
            List<int> Grass = new List<int> { 16, 32, 16, 0, 16, 16 };

            try
            {
                texture = ColorTexture.LoadTexture("textures/texture.png");
            }
            catch (Exception e)
            {
                throw new Exception("Error loading texture. Check textures folder exists.", e);
            }
            
            Material material = Material.GlossyMaterial(Colour.HexColor(0xFCFAE1), 1.1, Util.Radians(20));
            material.Texture = texture;
            
            List<Triangle> triangles = new List<Triangle>();
            
            for (int x = -10; x <= 10; x++)
            {
                for (int z = -10; z <= 10; z++)
                {
                    int h = Random.Shared.Next(4);
                    for (int y = 0; y <= h; y++)
                    {
                        Vector p = new Vector(x, y, z);
                        List<int> tiles = y == h ? Grass : Dirt;
                        triangles.AddRange(Block(p, material, tiles));
                    }
                }
            }

            Mesh mesh = Mesh.NewMesh(triangles.ToArray());
            scene.Add(mesh);
            Camera camera = Camera.LookAt(new Vector(-13, 11, -7), new Vector(0, 0, 0), new Vector(0, 1, 0), 45);
            Sampler sampler = DefaultSampler.NewSampler(4, 4);
            Renderer renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("craft.png", 1000);
        }

        static double[] Normalize(double[] values, double a, double b)
        {
            var result = new double[values.Length];
            var lo = values.Min();
            var hi = values.Max();

            for (int i = 0; i < values.Length; i++)
            {
                var p = (values[i] - lo) / (hi - lo);
                result[i] = a + p * (b - a);
            }

            return result;
        }

        static double[] LowPass(double[] values, double alpha)
        {
            var result = new double[values.Length];
            double y = 0;

            for (int i = 0; i < values.Length; i++)
            {
                y -= alpha * (y - values[i]);
                result[i] = y;
            }

            return result;
        }

        static double[] LowPassNoise(int n, double alpha, int iterations)
        {
            var result = new double[n];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Random.Shared.NextDouble() * 2 - 1;
            }
            for (int i = 0; i < iterations; i++)
            {
                result = LowPass(result, alpha);
            }
            result = Normalize(result, -1, 1);
            return result;
        }

        static void Frame(string path, double t, int width, int height)
        {
            var materials = new Material[]
            {
                Material.GlossyMaterial(Material.GenerateRandomColor(), 1.3, Util.Radians(20)),
                Material.GlossyMaterial(Material.GenerateRandomColor(), 1.3, Util.Radians(20)),
                Material.GlossyMaterial(Material.GenerateRandomColor(), 1.3, Util.Radians(20)),
            };

            var eye = new Vector(4, 2, 8);
            var center = Vector.Zero;
            var up = new Vector(0, 0, 1);
            var scene = new Scene();

            for (int a = 0; a < 80; a++)
            {
                var material = materials[Random.Shared.Next(materials.Length)];
                var n = 400;
                var xs = LowPassNoise(n, 0.25, 4);
                var ys = LowPassNoise(n, 0.25, 4);
                var zs = LowPassNoise(n, 0.25, 4);
                var position = Vector.Zero;
                var positions = new Vector[n];

                for (int i = 0; i < n; i++)
                {
                    positions[i] = position;
                    var v = new Vector((float)xs[i], (float)ys[i], (float)zs[i]) / new Vector(xs[i], ys[i], zs[i]).Length();
                    position += v * 0.1f;
                }

                for (int i = 0; i < n - 1; i++)
                {
                    var aVec = positions[i];
                    var bVec = positions[i + 1];
                    var p = aVec + (bVec - aVec) * (float)t;
                    var sphere = Sphere.NewSphere(p, 0.1f, material);
                    scene.Add(sphere);
                }
            }

            scene.Add(Sphere.NewSphere(new Vector(4, 4, 20), 2, Material.LightMaterial(Colour.HexColor(0xFFFFFF), 30)));
            var fovy = 40.0f;
            var camera = Camera.LookAt(eye, center, up, fovy);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SpecularMode = SpecularMode.SpecularModeFirst;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender(path, 100);
        }

        public static void beads(int width, int height)
        {
            for (int i = 0; i < 30; i++)
            {
                var t = (double)i / 30;
                var path = "beads.png";
                Console.WriteLine(path);
                Frame(path, t, width, height);
            }
        }


        static Vector offset(double stdev)
        {
            var a = Random.Shared.NextDouble() * 2 * Math.PI;
            var r = Random.Shared.NextDouble() * stdev;
            var x = Math.Cos(a) * r;
            var y = Math.Sin(a) * r;
            return new Vector(x, 0, y);
        }

        public static bool intersects(Scene scene, IShape shape)
        {
            var box = shape.BoundingBox();
            foreach(var other in scene.Shapes)
            {
                if (box.Intersects(other.BoundingBox()))
                {
                    return true;
                }
            }
            return false;
        }

        public static void go(int width, int height)
        {
            (double,double)[] blackPositions = {
                ( 7, 3), (14, 17), ( 14, 4), (18, 4), ( 0, 7), ( 5, 8), (11, 5), (10, 7), 
                (7, 6), ( 6, 10), (12, 6), ( 3, 2), (5, 11), ( 7, 5), ( 14, 15), ( 12, 11), 
                ( 8, 12), ( 4, 15), ( 2, 11), ( 9, 9), ( 10, 3), ( 6, 17), ( 7, 2), ( 14, 5), 
                ( 13, 3), ( 13, 16), ( 3, 6), ( 1, 10), ( 4, 1), ( 10, 9), ( 5, 17), ( 12, 7), 
                ( 3, 5), ( 2, 7), ( 5, 10), ( 10, 10), ( 5, 7), ( 7, 4), ( 12, 4), ( 8, 13), ( 9, 8), 
                ( 15, 17), ( 3, 10), ( 4, 13), ( 2, 13), ( 8, 16), ( 12, 3), ( 17, 5), ( 13, 2), 
                ( 15, 3), ( 2, 3), (6, 5), (11, 7), ( 16, 5), (11, 8), (14, 7), (15, 6), 
                ( 1, 7), ( 5, 9), (10, 11), ( 6, 6), (4, 18), ( 7, 14), ( 17, 3), ( 4, 9), 
                 (10, 12), ( 6, 3), (16, 7), (14, 14), (16, 18), (3, 13), (1, 13), (2, 10), 
                 (7, 9), (13, 1), (12, 15), (4, 3), (5, 2), (10, 2)
            };

            (double, double)[] whitePositions = {
                (16, 6), (16, 9), (13, 4), (1, 6), (0, 10), (3, 7), 
                (1, 11), (8, 5), (6, 7), (5, 5), (15, 11), (13, 7), 
                (18, 9), (2, 6), (7, 10), (15, 14), (13, 10), (17, 18), 
                (7, 15), (5, 14), (3, 18), (15, 16), (14, 8), (12, 8), 
                (7, 13), (1, 15), (8, 9), (6, 14), (12, 2), (17, 6), 
                (18, 5), (17, 11), (9, 7), (6, 4), (5, 4), (6, 11), 
                (11, 9), (13, 6), (18, 6), (0, 8), (8, 3), (4, 6), 
                (9, 2), (4, 17), (14, 12), (13, 9), (18, 11), (3, 15), 
                (4, 8), (2, 8), (12, 9), (16, 17), (8, 10), (9, 11), (17, 7), 
                (16, 11), (14, 10), (3, 9), (1, 9), (8, 7), (2, 14), (9, 6), (5, 3), 
                (14, 16), (5, 16), (16, 8), (13, 5), (8, 4), (4, 7), (5, 6), (11, 2), (12, 5), 
                (15, 8), (2, 9), (9, 15), (8, 1), (4, 4), (16, 15), (12, 10), (13, 11), (2, 16), 
                (4, 14), (5, 15), (10, 1), (6, 8), (6, 12), (17, 9), (8, 8)
            };


            var scene = new Scene();
            scene.Color = Colour.White;
            var black = Material.GlossyMaterial(Colour.HexColor(0x111111), 1.5, Util.Radians(45));
            var white = Material.GlossyMaterial(Colour.HexColor(0xFFFFFF), 1.6, Util.Radians(20));

            foreach(var p in blackPositions)
            {
                for (; ; )
                {
                    var m = new Matrix().Scale(new Vector(0.48, 0.2, 0.48)).Translate(new Vector(p.Item1 - 9.5, 0, p.Item2 - 9.5));
                    m = m.Translate(offset(0.02));
                    var shape = TransformedShape.NewTransformedShape(Sphere.NewSphere(new Vector(), 1, black), m);

                    if(intersects(scene, shape))
                    {
                        continue;
                    }
                    scene.Add(shape);
                    break;
                }
            }

        	foreach(var p in whitePositions)
            {
                while (true)
                {
                    var m = new Matrix().Scale(new Vector(0.48, 0.2, 0.48)).Translate(new Vector(p.Item1 - 9.5, 0, p.Item2 - 9.5));
                    m = m.Translate(offset(0.02));

                    var shape = TransformedShape.NewTransformedShape(Sphere.NewSphere(new Vector(), 1, white), m);
			    
                    if(intersects(scene, shape))
                    {
                        continue;
                    }
                    scene.Add(shape);

                    break;
                    
                }
            }
	
            for(int i = 0; i< 19; i++)
            {
                var x = (double)i - 9.5;
                var m = 0.015;
                scene.Add(Cube.NewCube(new Vector(x - m, -1, -9.5), new Vector(x + m, -0.195, 8.5), black));
                scene.Add(Cube.NewCube(new Vector(-9.5, -1, x - m), new Vector(8.5, -0.195, x + m), black));
	        }

            var material = Material.GlossyMaterial(Colour.HexColor(0xEFECCA), 1.2, Util.Radians(30));
            //material.Texture = ColorTexture.GetTexture("examples/wood.jpg");
            scene.Add(Cube.NewCube(new Vector(-12, -12, -12), new Vector(12, -0.2, 12), material));
            //scene.Texture = ColorTexture.GetTexture("examples/courtyard_ccby/courtyard_8k.png");
            var camera = Camera.LookAt(new Vector(-0.5, 5, 5), new Vector(-0.5, 0, 0.5), new Vector(0, 1, 0), 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 2560 / 2, 1440 / 2, true);
            renderer.IterativeRender("gogo.png", 1000);
        }


        public static void example1(int Width, int Height)
        {
            Scene scene = new Scene();
            scene.Add(Sphere.NewSphere(new Vector(1.5, 1.25, 0), 1.25, Material.SpecularMaterial(Colour.HexColor(0x004358), 1.3)));
            scene.Add(Sphere.NewSphere(new Vector(-1, 1, 2), 1, Material.SpecularMaterial(Colour.HexColor(0xFFE11A), 1.3)));
            scene.Add(Sphere.NewSphere(new Vector(-2.5, 0.75, 0), 0.75, Material.SpecularMaterial(Colour.HexColor(0xFD7400), 1.3)));
            scene.Add(Sphere.NewSphere(new Vector(-0.75, 0.5, -1), 0.5, Material.ClearMaterial(1.5, 0)));
            scene.Add(Cube.NewCube(new Vector(-10, -1, -10), new Vector(10, 0, 10), Material.GlossyMaterial(Colour.White, 1.1, Util.Radians(10))));
            scene.Add(Sphere.NewSphere(new Vector(-1.5, 4, 0), 0.5, Material.LightMaterial(Colour.White, 30)));
            var camera = Camera.LookAt(new Vector(0, 2, -5), new Vector(0, 0.25, 3), new Vector(0, 1, 0), 45);
            camera.SetFocus(new Vector(-0.75, 1, -1), 0.1);
            DefaultSampler sampler = DefaultSampler.NewSampler(8, 10);
            sampler.SpecularMode = SpecularMode.SpecularModeFirst;
            Renderer renderer = Renderer.NewRenderer(scene, camera, sampler, Width, Height, true);
            renderer.AdaptiveSamples = 32;
            renderer.FireflySamples = 256;
            renderer.IterativeRender("example1.png", 500);
        }

        public static void example2(int Width, int Height)
        {
            var scene = new Scene();
            var material = Material.GlossyMaterial(Colour.HexColor(0xEFC94C), 3, Util.Radians(30));
            var whiteMat = Material.GlossyMaterial(Colour.White, 3, Util.Radians(30));

            for (int x = 0; x < 40; x++) 
            {
                for(int z = 0; z < 40; z++)
                {
                    var center = new Vector((double)x - 19.5, 0, (double)z - 19.5);
                    scene.Add(Sphere.NewSphere(center, 0.4, material));
        
                }
            }
            scene.Add(Cube.NewCube(new Vector(-100, -1, -100), new Vector(100, 0, 100), whiteMat));
            scene.Add(Sphere.NewSphere(new Vector(-1, 4, -1), 1, Material.LightMaterial(Colour.White, 20)));
            var camera = Camera.LookAt(new Vector(0, 4, -8), new Vector(0, 0, -2), new Vector(0, 1, 0), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, Width, Height, true);
            renderer.IterativeRender("example2.png", 1000);
        }

        public static void example3(int width, int height)
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(Colour.HexColor(0xFCFAE1));
            scene.Add(Cube.NewCube(new Vector(-1000, -1, -1000), new Vector(1000, 0, 1000), material));

            for(int x = -20; x <= 20; x++)
            {
                for(int z = -20; z <= 20; z++)
                {
                    if ((x + z)% 2 == 0)
                    {
                        continue;
        
                    }   
                    var s = 0.1;
                    var min = new Vector((double)x - s, 0, (double)z - s);
                    var max = new Vector((double)x + s, 2, (double)z + s);
                    scene.Add(Cube.NewCube(min, max, material));
                }
            }
            scene.Add(Cube.NewCube(new Vector(-5, 10, -5), new Vector(5, 11, 5), Material.LightMaterial(Colour.White, 5)));
            var camera = Camera.LookAt(new Vector(20, 10, 0), new Vector(8, 0, 0), new Vector(0, 1, 0), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("example3.png", 1000);
        }

        public static void colorwave(int width, int height)
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(Colour.HexColor(0xFCFAE1));
            scene.Add(Cube.NewCube(new Vector(-1000, -1, -1000), new Vector(1000, 0, 1000), material));

            Parallel.For(-200, 201, x =>
            {
                double posX = x / 10.0;
                for (double z = -20; z <= 20; z += 0.1)
                {
                    if ((Math.Floor(posX) + Math.Floor(z)) % 2 == 0)
                    {
                        continue;
                    }

                    var s = 0.05; // Decrease the size of the cube for denser pattern
                    var min = new Vector(posX - s, 0, z - s); // Set y-coordinate to 0
                    var max = new Vector(posX + s, Random.Shared.NextDouble() * 4, z + s); // Increase amplitude to 4

                    // Calculate wave color based on position with increased amplitude
                    var waveColor = Colour.FromRGB(
                        (Math.Sin(posX * 0.2) + 1) * 0.5,  // Increase amplitude of sine wave
                        (Math.Sin(z * 0.2) + 1) * 0.5,      // Increase amplitude of sine wave
                        0.5);                                // Blue component (constant)

                    // Create material with wave color
                    var materialWithColor = Material.DiffuseMaterial(waveColor);

                    lock (scene)
                    {
                        scene.Add(Cube.NewCube(min, max, materialWithColor));
                    }
                }
            });

            scene.Add(Cube.NewCube(new Vector(-5, 10, -5), new Vector(5, 11, 5), Material.LightMaterial(Colour.White, 5)));

            var camera = Camera.LookAt(new Vector(20, 10, 0), new Vector(8, 0, 0), new Vector(0, 1, 0), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("wave_example3.png", 1000);
        }

        public static void simplesphere(int width, int height)
        {
            var scene = new Scene();
            // create a material
            var material = Material.DiffuseMaterial(Colour.White);

            // add the floor (a plane)
            var plane = Plane.NewPlane(new Vector(0, 0, 0), new Vector(0, 0, 1), material);
            scene.Add(plane);

            // add the ball (a sphere)
            var sphere = Sphere.NewSphere(new Vector(0, 0, 1), 1.0F, material);
            scene.Add(sphere);

            // add a spherical light source
            var light = Sphere.NewSphere(new Vector(0, 0, 5.0F), 1.0F, Material.LightMaterial(Colour.White, 8));
            scene.Add(light);

            // position the camera
            var camera = Camera.LookAt(new Vector(3, 3, 3), new Vector(0, 0, 0.5F), new Vector(0, 0, 1), 50);

            // render the scene with progressive refinement
            var sampler = DefaultSampler.NewSampler(16, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.AdaptiveSamples = 128;
            renderer.FireflySamples = 32;
            renderer.IterativeRender("simplesphere.png", 100);
        }

        public static void simplecylinder(int width, int height)
        {
            var scene = new Scene();
            // create a material
            var material = Material.DiffuseMaterial(Colour.White);

            // add the floor (a plane)
            var plane = Plane.NewPlane(new Vector(0, 0, 0), new Vector(0, 0, 1), material);
            scene.Add(plane);

            // Calculate the translation matrix to place the bottom of the cylinder at the center
            double cylinderHeight = 2.0; // Height of the cylinder
            var translation = new Matrix().Translate(new Vector(0, 0, -cylinderHeight / 2.0));

            // add the cylinder with the adjusted translation
            var cylinder = TransformedShape.NewTransformedShape(Cylinder.NewCylinder(1, 0, cylinderHeight, material), translation);
            scene.Add(cylinder);

            // add a spherical light source
            var light = Sphere.NewSphere(new Vector(0, 0, 5), 1, Material.LightMaterial(Colour.White, 8));
            scene.Add(light);

            // position the camera
            var camera = Camera.LookAt(new Vector(3, 3, 3), new Vector(0, 0, 0.5), new Vector(0, 0, 1), 50);

            // render the scene with progressive refinement
            var sampler = DefaultSampler.NewSampler(16, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.AdaptiveSamples = 128;
            renderer.FireflySamples = 32;
            renderer.IterativeRender("simplecylinder.png", 100);
        }

        public static void shrender(int l, int m)
        {
            Scene scene = new Scene();
            var eye = new Vector(1, 1, 1);
            var center = new Vector(0, 0, 0);
            var up = new Vector(0, 0, 1);
            var light = Material.LightMaterial(Colour.White, 150);
            scene.Add(Sphere.NewSphere(new Vector(0, 0, 5), 0.5, light));
            scene.Add(Sphere.NewSphere(new Vector(5, 0, 2), 0.5, light));
            scene.Add(Sphere.NewSphere(new Vector(0, 5, 2), 0.5, light));
            var pm = Material.GlossyMaterial(Colour.HexColor(0x105B63), 1.3, Util.Radians(30));
            var nm = Material.GlossyMaterial(Colour.HexColor(0xBD4932), 1.3, Util.Radians(30));
            var sh = SphericalHarmonic.NewSphericalHarmonic(l, m, pm, nm);
            scene.Add(sh);
            var camera = Camera.LookAt(eye, center, up, 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SetLightMode(LightMode.LightModeAll);
            sampler.SetSpecularMode(SpecularMode.SpecularModeFirst);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 1600 / 2, 1600 / 2, false);
            string timestamp = DateTime.Now.ToString("yyyyMMddhhmmss");
            string filename = String.Format("sh{0}.png", timestamp);
            renderer.IterativeRender(filename, 10);
        }

        public void sh()
        {
            for (int l = 0; l <= 4; l++)
            {
                for (int m = -l; m <= l; m++)
                {
                    shrender(l, m);
                }
            }
        }

        public static void dragon(int width, int height)
        {
           var scene = new Scene();
           var material = Material.GlossyMaterial(Colour.HexColor(0xB7CA79), 1.5F, Util.Radians(20));
           var mesh = OBJ.Load("models/dragon.obj", material);
           mesh.FitInside(new Box(new Vector(-1, 0, -1), new Vector(1, 2, 1)), new Vector(0.5, 0, 0.5));
           scene.Add(mesh);
           var floor = Material.GlossyMaterial(Colour.HexColor(0xD8CAA8), 1.2F, Util.Radians(5));
           scene.Add(Cube.NewCube(new Vector(-50, -50, -50), new Vector(50, 0, 50), floor));
           var light = Material.LightMaterial(Colour.White, 75);
           scene.Add(Sphere.NewSphere(new Vector(-1, 10, 4), 1, light));
           var mouth = Material.LightMaterial(Colour.HexColor(0xFFFAD5), 500);
           scene.Add(Sphere.NewSphere(new Vector(-0.05F, 1, -0.5F), 0.03F, mouth));
           var camera = Camera.LookAt(new Vector(-3, 2, -1), new Vector(0, 0.6F, -0.1F), new Vector(0, 1, 0), 35);
           camera.SetFocus(new Vector(0, 1, -0.5F), 0.03F);
           var sampler = DefaultSampler.NewSampler(4, 4);
           var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
           renderer.IterativeRender("dragon.png", 100);
        }

        public static void cube(int width, int height)
        {
            Scene scene = new Scene();
            List<IShape> meshes = new List<IShape>
            {
                Util.CreateCubeMesh(Material.GlossyMaterial(Colour.HexColor(0x3B596A), 1.5, Util.Radians(20))),
                Util.CreateCubeMesh(Material.GlossyMaterial(Colour.HexColor(0x427676), 1.5, Util.Radians(20))),
                Util.CreateCubeMesh(Material.GlossyMaterial(Colour.HexColor(0x3F9A82), 1.5, Util.Radians(20))),
                Util.CreateCubeMesh(Material.GlossyMaterial(Colour.HexColor(0xA1CD73), 1.5, Util.Radians(20))),
                Util.CreateCubeMesh(Material.GlossyMaterial(Colour.HexColor(0xECDB60), 1.5, Util.Radians(20)))
            };

            for (int x = -8; x <= 8; x++)
            {
                for (int z = -12; z <= 12; z++)
                {
                    var fx = (double)x;
                    var fy = Random.Shared.NextDouble() * 2;
                    var fz = (double)z;
                    scene.Add(TransformedShape.NewTransformedShape(meshes[Random.Shared.Next(meshes.Count)], new Matrix().Translate(new Vector(fx, fy, fz))));
                    scene.Add(TransformedShape.NewTransformedShape(meshes[Random.Shared.Next(meshes.Count)], new Matrix().Translate(new Vector(fx, fy - 1, fz))));
                }
            }

            scene.Add(Sphere.NewSphere(new Vector(8, 10, 0), 3, Material.LightMaterial(Colour.White, 30)));
            Camera camera = Camera.LookAt(new Vector(-10, 10, 0), new Vector(-2, 0, 0), new Vector(0, 1, 0), 45);
            Sampler sampler = DefaultSampler.NewSampler(4, 4);
            Renderer renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("cube.png", 1000);
        }
      
        public static void runway(int width, int height_)
        {
            const int radius = 2;
            const int height = 3;
            const int emission = 3;
            Scene scene = new Scene();
            var white = Material.DiffuseMaterial(Colour.White);
            var floor = Cube.NewCube(new Vector(-250, -1500, -1), new Vector(250, 6200, 0), white);
            scene.Add(floor);
            var light = Material.LightMaterial(Colour.Kelvin(2700), emission);
            for (int y = 0; y <= 6000; y += 40)
            {
                scene.Add(Sphere.NewSphere(new Vector(-100, (double)y, height), radius, light));
                scene.Add(Sphere.NewSphere(new Vector(0, (double)y, height), radius, light));
                scene.Add(Sphere.NewSphere(new Vector(100, (double)y, height), radius, light));

            }
            for (int y = -40; y >= -750; y -= 20)
            {
                scene.Add(Sphere.NewSphere(new Vector(-10, (double)y, height), radius, light));
                scene.Add(Sphere.NewSphere(new Vector(0, (double)y, height), radius, light));
                scene.Add(Sphere.NewSphere(new Vector(10, (double)y, height), radius, light));
            }
            var green = Material.LightMaterial(Colour.HexColor(0x0BDB46), emission);
            var red = Material.LightMaterial(Colour.HexColor(0xDC4522), emission);

            for (int x = -160; x <= 160; x += 10)
            {
                scene.Add(Sphere.NewSphere(new Vector((double)x, -20, height), radius, green));
                scene.Add(Sphere.NewSphere(new Vector((double)x, 6100, height), radius, red));
            }
            scene.Add(Sphere.NewSphere(new Vector(-160, 250, height), radius, red));
            scene.Add(Sphere.NewSphere(new Vector(-180, 250, height), radius, red));
            scene.Add(Sphere.NewSphere(new Vector(-200, 250, height), radius, light));
            scene.Add(Sphere.NewSphere(new Vector(-220, 250, height), radius, light));
            for (int i = 0; i < 5; i++)
            {
                var y = (double)((i + 1) * -120);

                for (int j = 1; j <= 4; j++)
                {
                    var x = (double)(j + 4) * 7.5F;
                    scene.Add(Sphere.NewSphere(new Vector(x, y, height), radius, red));
                    scene.Add(Sphere.NewSphere(new Vector(-x, y, height), radius, red));
                    scene.Add(Sphere.NewSphere(new Vector(x, -y, height), radius, light));
                    scene.Add(Sphere.NewSphere(new Vector(-x, -y, height), radius, light));
                }
            }
            var camera = Camera.LookAt(new Vector(0, -1500, 200), new Vector(0, -100, 0), new Vector(0, 0, 1), 20);
            camera.SetFocus(new Vector(0, 20000, 0), 1);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height_, true);
            renderer.FireflySamples = 32;
            renderer.IterativeRender("runway.png", 1000);
        }

        public static void bunny(int width, int height)
        {
            var scene = new Scene();
            var material = Material.GlossyMaterial(Colour.HexColor(0xF2EBC7), 1.5F, Util.Radians(0));
            var mesh = OBJ.Load("models/bunny.obj", material);
            mesh.SmoothNormals();
            mesh.FitInside(new Box(new Vector(-1, 0, -1), new Vector(1, 2, 1)), new Vector(0.5F, 0, 0.5F));
            scene.Add(mesh);
            var floor = Material.GlossyMaterial(Colour.HexColor(0x33332D), 1.2F, Util.Radians(20));
            scene.Add(Cube.NewCube(new Vector(-10000, -10000, -10000), new Vector(10000, 0, 10000), floor));
            scene.Add(Sphere.NewSphere(new Vector(0, 5, 0), 1, Material.LightMaterial(Colour.White, 10)));
            scene.Add(Sphere.NewSphere(new Vector(4, 5, 4), 1, Material.LightMaterial(Colour.White, 10)));
            var camera = Camera.LookAt(new Vector(-1, 2, 3), new Vector(0, 0.75F, 0), new Vector(0, 1, 0), 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SetSpecularMode(SpecularMode.SpecularModeFirst);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 32;
            renderer.IterativeRender("bunny.png", 1000);
        }

        public static void ellipsoid(int width, int height)
        {
            var scene = new Scene();
            var wall = Material.GlossyMaterial(Colour.HexColor(0xFCFAE1), 1.333F, Util.Radians(30));
            scene.Add(Sphere.NewSphere(new Vector(10, 10, 10), 2, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Cube.NewCube(new Vector(-100, -100, -100), new Vector(-12, 100, 100), wall));
            scene.Add(Cube.NewCube(new Vector(-100, -100, -100), new Vector(100, -1, 100), wall));
            var material = Material.GlossyMaterial(Colour.HexColor(0x167F39), 1.333F, Util.Radians(30));
            var sphere = Sphere.NewSphere(new Vector(), 1, material);
            for (int i = 0; i < 180; i += 30)
            {
                var m = Matrix.Identity;
                m = m.Scale(new Vector(0.3F, 1, 5)).Mul(m);
                m = m.Rotate(new Vector(0, 1, 0), Util.Radians((double)i)).Mul(m);
                var shape = TransformedShape.NewTransformedShape(sphere, m);
                scene.Add(shape);
            }
            var camera = Camera.LookAt(new Vector(8, 8, 0), new Vector(1, 0, 0), new Vector(0, 1, 0), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("ellipsoid.png", 1000);
        }

        public static void refraction(int width, int height)
        {
            var scene = new Scene();
            var glass = Material.ClearMaterial(1.5, 0);
            // add a sphere primitive
            scene.Add(Sphere.NewSphere(new Vector(-1.5, 0, 0.5), 1, glass));
            // add a mesh sphere
            var mesh = STL.Load("models/sphere.stl", glass);
            mesh.SmoothNormals();
            mesh.Transform(new Matrix().Translate(new Vector(1.5, 0, 0.5)));
            scene.Add(mesh);
            // add the floor
            scene.Add(Plane.NewPlane(new Vector(0, 0, -1), new Vector(0, 0, 1), Material.DiffuseMaterial(Colour.White)));
            // add the light
            scene.Add(Sphere.NewSphere(new Vector(0, 0, 5), 1, Material.LightMaterial(Colour.White, 15)));
            var camera = Camera.LookAt(new Vector(0, -5, 5), new Vector(0, 0, 0), new Vector(0, 0, 1), 50);
            var sampler = DefaultSampler.NewSampler(16, 8);
            sampler.SetSpecularMode(SpecularMode.SpecularModeAll);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("refraction.png", 100);
        }

        public static void qbert(int Width, int Height)
        {
            var scene = new Scene();
            var floor = Material.GlossyMaterial(Colour.HexColor(0xFCFFF5), 1.2, Util.Radians(30));
            var cube = Material.GlossyMaterial(Colour.HexColor(0xFF8C00), 1.3, Util.Radians(20));
            var ball = Material.GlossyMaterial(Colour.HexColor(0xD90000), 1.4, Util.Radians(10));
            int n = 7;
            var fn = (double)n;
            var rand = Random.Shared;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n - z; x++)
                {
                    for (int y = 0; y < n - z - x; y++)
                    {
                        (var fx, var fy, var fz) = ((double)x, (double)y, (double)z);
                        scene.Add(Cube.NewCube(new Vector(fx, fy, fz), new Vector(fx + 1, fy + 1, fz + 1), cube));

                        if (x + y == n - z - 1)
                        {
                            if (rand.NextDouble() > 0.75)
                            {
                                scene.Add(Sphere.NewSphere(new Vector(fx + 0.5F, fy + 0.5F, fz + 1.5F), 0.35F, ball));
                            }
                        }
                    }
                }
            }
            scene.Add(Cube.NewCube(new Vector(-1000, -1000, -1), new Vector(1000, 1000, 0), floor));
            scene.Add(Sphere.NewSphere(new Vector(fn, fn / 3, fn * 2), 1, Material.LightMaterial(Colour.White, 100)));
            var camera = Camera.LookAt(new Vector(fn * 2, fn * 2, fn * 2), new Vector(0, 0, fn / 4), new Vector(0, 0, 1), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, Width, Height, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("qbert.png", 1000);
        }

        public static void love(int width, int height)
        {
            var scene = new Scene();
            var material = Material.GlossyMaterial(Colour.HexColor(0xF2F2F2), 1.5, Util.Radians(20));
            scene.Add(Cube.NewCube(new Vector(-100, -1, -100), new Vector(100, 0, 100), material));
            var heart = Material.GlossyMaterial(Colour.HexColor(0xF60A20), 1.5, Util.Radians(20));
            var mesh = STL.Load("models/love.stl", heart);
            mesh.FitInside(new Box(new Vector(-0.5F, 0, -0.5), new Vector(0.5, 1, 0.5)), new Vector(0.5, 0, 0.5));
            scene.Add(mesh);
            scene.Add(Sphere.NewSphere(new Vector(-2, 10, 2), 1, Material.LightMaterial(Colour.White, 30)));
            scene.Add(Sphere.NewSphere(new Vector(0, 10, 2), 1, Material.LightMaterial(Colour.White, 30)));
            scene.Add(Sphere.NewSphere(new Vector(2, 10, 2), 1, Material.LightMaterial(Colour.White, 30)));
            var camera = Camera.LookAt(new Vector(0, 1.5F, 2), new Vector(0, 0.5F, 0), new Vector(0, 1, 0), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("love.png", 1000); 
        }

        public static void materialspheres(int width, int height)
        {
            var scene = new Scene();
            var r = 0.4F;
            Material material;
            material = Material.DiffuseMaterial(Colour.HexColor(0x334D5C));
            scene.Add(Sphere.NewSphere(new Vector(-2, r, 0), r, material));
            material = Material.SpecularMaterial(Colour.HexColor(0x334D5C), 2);
            scene.Add(Sphere.NewSphere(new Vector(-1, r, 0), r, material));
            material = Material.GlossyMaterial(Colour.HexColor(0x334D5C), 2, Util.Radians(50));
            scene.Add(Sphere.NewSphere(new Vector(0, r, 0), r, material));
            material = Material.TransparentMaterial(Colour.HexColor(0x334D5C), 2, Util.Radians(20), 1);
            scene.Add(Sphere.NewSphere(new Vector(1, r, 0), r, material));
            material = Material.ClearMaterial(2, 0);
            scene.Add(Sphere.NewSphere(new Vector(2, r, 0), r, material));
            material = Material.MetallicMaterial(Colour.HexColor(0xFFFFFF), 0, 1);
            scene.Add(Sphere.NewSphere(new Vector(0, 1.5F, -4), 1.5F, material));
            scene.Add(Cube.NewCube(new Vector(-1000, -1, -1000), new Vector(1000, 0, 1000), Material.GlossyMaterial(Colour.HexColor(0xFFFFFF), 1.4F, Util.Radians(20))));
            scene.Add(Sphere.NewSphere(new Vector(0, 5, 0), 1, Material.LightMaterial(Colour.White, 25)));
            var camera = Camera.LookAt(new Vector(0, 3, 6), new Vector(0, 1, 0), new Vector(0, 1, 0), 30);
            var sampler = DefaultSampler.NewSampler(16, 16);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("materialspheres.png", 100);
        }

        public static void toybrick(int width, int height)
        {
            const double H = 1.46875F;
            var scene = new Scene();
            scene.Color = Colour.White;

            var whiteBrick = Util.CreateBrick(0xF2F3F2);
            var brightRedBrick = Util.CreateBrick(0xC4281B);
            var brightBlueBrick = Util.CreateBrick(0x0D69AB);
            var brightYellowBrick = Util.CreateBrick(0xF5CD2F);
            var blackBrick = Util.CreateBrick(0x1B2A34);
            var darkGreenBrick = Util.CreateBrick(0x287F46);

            var meshes = new Mesh[] { whiteBrick, brightRedBrick, brightBlueBrick, brightYellowBrick, blackBrick, darkGreenBrick };

            // Generate bricks and add them to the list
            var brickList = new List<IShape>();
            for (int i = 0; i < 6; i++)
            {
                var h = new Random().Next(5) + 1;
                for (int j = 0; j < h; j++)
                {
                    brickList.Add(meshes[i]);
                }
            }

            var random = new Random(); // Instantiate outside the loop

            for (int x = -30; x <= 50; x += 2)
            {
                for (int y = -50; y <= 20; y += 4)
                {
                    var h = random.Next(5) + 1;
                    for (int i = 0; i < h; i++)
                    {
                        var dy = 0;

                        if (((x / 2 + i) % 2) == 0)
                        {
                            dy = 2;
                        }
                        var z = i * H;
                        var mnum = random.Next(brickList.Count); // Randomly select from the brick list
                        var mesh = brickList[mnum]; // Selecting a mesh from the list
                        var m = new Matrix().Translate(new Vector((double)x, (double)(y + dy), (double)z));
                        scene.Add(TransformedShape.NewTransformedShape(mesh, m));
                    }
                }
            }

            var camera = Camera.LookAt(new Vector(-23, 13, 20), new Vector(0, 0, 0), new Vector(0, 0, 1), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 64;
            renderer.IterativeRender("toybrick.png", 1000);
        }

        internal static void cylinder2(int width, int height)
        {
            Scene scene = new Scene();
            var materials = new Material[]
            {
                Material.GlossyMaterial(Colour.HexColor(0x730046), 1.6F, Util.Radians(45)),
                Material.GlossyMaterial(Colour.HexColor(0xBFBB11), 1.6F, Util.Radians(45)),
                Material.GlossyMaterial(Colour.HexColor(0xFFC200), 1.6F, Util.Radians(45)),
                Material.GlossyMaterial(Colour.HexColor(0xE88801), 1.6F, Util.Radians(45)),
                Material.GlossyMaterial(Colour.HexColor(0xC93C00), 1.6F, Util.Radians(45)),
            };

            for (int x = -6; x <= 3; x++)
            {
                var material = materials[(x + 6) % materials.Length];
                for (int y = -5; y <= 4; y++)
                {
                    var fx = (double)x / 2;
                    var fy = (double)y;
                    var fz = (double)x / 2;

                    var cylinder = Cylinder.NewTransformedCylinder(new Vector(fx, fy, fz), new Vector(fx + 1, fy, fz), 1, material);
                    scene.Add(cylinder);
                }
            }
            scene.Add(Sphere.NewSphere(new Vector(1, 0, 10), 3, Material.LightMaterial(Colour.White, 20)));
            var camera = Camera.LookAt(new Vector(-5, 0, 5), new Vector(1, 0, 0), new Vector(0, 0, 1), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("cylinder.png", 1000);
        }

        public static void cylinder(int width, int height)
        {
            Scene scene = new Scene();
            var meshes = new Mesh[]
            {
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0x730046), 1.6F, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xBFBB11), 1.6F, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xFFC200), 1.6F, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xE88801), 1.6F, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xC93C00), 1.6F, Util.Radians(45))),
            };

            for (int x = -6; x <= 3; x++)
            {
                var mesh = meshes[(x + 6) % meshes.Length];
                for (int y = -5; y <= 4; y++)
                {
                    var fx = (double)x / 2;
                    var fy = (double)y;
                    var fz = (double)x / 2;

                    scene.Add(TransformedShape.NewTransformedShape(mesh, new Matrix().Translate(new Vector(fx, fy, fz))));
                }
            }
            scene.Add(Sphere.NewSphere(new Vector(1, 0, 10), 3, Material.LightMaterial(Colour.White, 20)));
            var camera = Camera.LookAt(new Vector(-5, 0, 5), new Vector(1, 0, 0), new Vector(0, 0, 1), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("cylinder.png", 1000);
        }

        public static void teapot(int width, int height)
        {
            var scene = new Scene();
            scene.Add(Sphere.NewSphere(new Vector(-2, 5, -3), 0.5F, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Sphere.NewSphere(new Vector(5, 5, -3), 0.5F, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Cube.NewCube(new Vector(-30, -1, -30), new Vector(30, 0, 30), Material.SpecularMaterial(Colour.HexColor(0xFCFAE1), 2)));
            var mesh = OBJ.Load("models/teapot.obj", Material.SpecularMaterial(Colour.HexColor(0xB9121B), 2));
            mesh.SmoothNormals();
            scene.Add(mesh);
            var camera = Camera.LookAt(new Vector(2, 5, -6), new Vector(0.5F, 1, 0), new Vector(0, 1, 0), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SpecularMode = SpecularMode.SpecularModeFirst;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 64;
            renderer.IterativeRender("teapot.png", 1000); 
        }

        public static void hits(int width, int height)
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(new Colour(0.95F, 0.95F, 1));
            var light = Material.LightMaterial(Colour.White, 300);
            scene.Add(Sphere.NewSphere(new Vector(-0.75F, -0.75F, 5), 0.25F, light));
            scene.Add(Cube.NewCube(new Vector(-1000, -1000, -1000), new Vector(1000, 1000, 0), material));
            var mesh = STL.Load("models/hits.stl", material);
            mesh.SmoothNormalsThreshold(Util.Radians(10));
            mesh.FitInside(new Box(new Vector(-1, -1, 0), new Vector(1, 1, 2)), new Vector(0.5F, 0.5F, 0));
            scene.Add(mesh);
            var camera = Camera.LookAt(new Vector(1.6F, -3, 2), new Vector(-0.25F, 0.5F, 0.5F), new Vector(0, 0, 1), 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("hits.png", 1000);
        }

        public static void suzanne(int width, int height)
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(Colour.HexColor(0x334D5C));
            scene.Add(Sphere.NewSphere(new Vector(0.5, 1, 3), 1, Material.LightMaterial(Colour.White, 4)));
            scene.Add(Sphere.NewSphere(new Vector(1.5, 1, 3), 1, Material.LightMaterial(Colour.White, 4)));
            scene.Add(Cube.NewCube(new Vector(-5, -5, -2), new Vector(5, 5, -1), material));
            var mesh = OBJ.Load("models/suzanne.obj", Material.SpecularMaterial(Colour.HexColor(0xEFC94C), 1.3));
            scene.Add(mesh);
            var camera = Camera.LookAt(new Vector(1, -0.45F, 4), new Vector(1, -0.6F, 0.4F), new Vector(0, 1, 0), 40);
            var sampler = DefaultSampler.NewSampler(16, 8);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("suzanne.png", 1000);
        }

        public static void sdf(int width, int height)
        {
            var scene = new Scene();
            var light = Material.LightMaterial(Colour.White, 180);
            double d = 4.0F;
            scene.Add(Sphere.NewSphere(new Vector(-1, -1, 0.5F).Normalize().MulScalar(d), 0.25F, light));
            scene.Add(Sphere.NewSphere(new Vector(0, -1, 0.25F).Normalize().MulScalar(d), 0.25F, light));
            scene.Add(Sphere.NewSphere(new Vector(-1, 1, 0).Normalize().MulScalar(d), 0.25F, light));
            var material = Material.GlossyMaterial(Colour.HexColor(0x468966), 1.2F, Util.Radians(20));
            var sphere = SphereSDF.NewSphereSDF(0.65F);
            var cube = CubeSDF.NewCubeSDF(new Vector(1, 1, 1));
            var roundedCube = IntersectionSDF.NewIntersectionSDF(new List<SDF> { sphere, cube });
            var a = CylinderSDF.NewCylinderSDF(0.25F, 1.1F);
            var b = TransformSDF.NewTransformSDF(a, new Matrix().Rotate(new Vector(1, 0, 0), Util.Radians(90)));
            var c = TransformSDF.NewTransformSDF(a, new Matrix().Rotate(new Vector(0, 0, 1), Util.Radians(90)));
            var difference = DifferenceSDF.NewDifferenceSDF(new List<SDF> { roundedCube, a, b, c });
            var sdf = TransformSDF.NewTransformSDF(difference, new Matrix().Rotate(new Vector(0, 0, 1), Util.Radians(30)));
            scene.Add(SDFShape.NewSDFShape(sdf, material));
            var floor = Material.GlossyMaterial(Colour.HexColor(0xFFF0A5), 1.2F, Util.Radians(20));
            scene.Add(Plane.NewPlane(new Vector(0, 0, -0.5F), new Vector(0, 0, 1), floor));
            var camera = Camera.LookAt(new Vector(-3, 0, 1), new Vector(0, 0, 0), new Vector(0, 0, 1), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.LightMode = LightMode.LightModeAll;
            sampler.SpecularMode = SpecularMode.SpecularModeAll;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("sdf.png", 1000);
        }

        public void volume()
        {
            string[] imglist = Directory.GetFiles(Directory.GetCurrentDirectory()+"\\images", "*.*", SearchOption.AllDirectories);
            List<SKBitmap> bmplist = new List<SKBitmap>();
            
            foreach (string file in imglist)
            {
                Console.WriteLine(file);
                using (FileStream stream = File.OpenRead(file))
                {
                    SKBitmap skBitmap = SkiaSharp.SKBitmap.Decode(stream);
                    bmplist.Add(skBitmap);
                }
            }

            Scene scene = new Scene();
            scene.Color = Colour.White;
            
            Colour[] colors = new Colour[]
            {
                // HexColor(0xFFF8E3),
                Colour.HexColor(0x004358),
                Colour.HexColor(0x1F8A70),
                Colour.HexColor(0xBEDB39),
                Colour.HexColor(0xFFE11A),
                Colour.HexColor(0xFD7400),
            };

            const double start = 0.2F;
            const double size = 0.01F;
            const double step = 0.1F;
            List<Volume.VolumeWindow> windows = new List<Volume.VolumeWindow>();
            for (int i = 0; i < colors.Length; i++)
            {
                var lo = start + step * (double)i;
                var hi = lo + size;
                var material = Material.GlossyMaterial(colors[i], 1.3F, Util.Radians(0));
                var w = new Volume.VolumeWindow(lo, hi, material);
                windows.Add(w);
            }
            var box = new Box(new Vector(-1, -1, -0.2F), new Vector(1, 1, 1));
            var volume = Volume.NewVolume(box, bmplist.ToArray(), 3.4F / 0.9765625F, windows.ToArray());
            scene.Add(volume);
            var camera = Camera.LookAt(new Vector(0, -3, -3), new Vector(0, 0, 0), new Vector(0, 0, -1), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 512, 512, false);
            renderer.IterativeRender("volume.png", 1000);
        }

        public void veachscene()
        {
            Scene scene = new Scene();
            Material material;
            Mesh mesh;
            material = Material.DiffuseMaterial(Colour.White);
            mesh = OBJ.Load("veach_scene/backdrop.obj", material);
            scene.Add(mesh);
            material = Material.MetallicMaterial(Colour.White, Util.Radians(20), 0);
            mesh = OBJ.Load("veach_scene/bar0.obj", material);
            scene.Add(mesh);
            material = Material.MetallicMaterial(Colour.White, Util.Radians(15), 0);
            mesh = OBJ.Load("veach_scene/bar1.obj", material);
            scene.Add(mesh);
            material = Material.MetallicMaterial(Colour.White, Util.Radians(10), 0);
            mesh = OBJ.Load("veach_scene/bar2.obj", material);
            scene.Add(mesh);
            material = Material.MetallicMaterial(Colour.White, Util.Radians(5), 0);
            mesh = OBJ.Load("veach_scene/bar3.obj", material);
            scene.Add(mesh);
            material = Material.MetallicMaterial(Colour.White, Util.Radians(0), 0);
            mesh = OBJ.Load("veach_scene/bar4.obj", material);
            scene.Add(Sphere.NewSphere(new Vector(3.7F, 4.281F, 0), 1.8F / 2, Material.LightMaterial(Colour.White, 3)));
            scene.Add(Sphere.NewSphere(new Vector(1.25F, 4.281F, 0), 0.6F / 2, Material.LightMaterial(Colour.White, 9)));
            scene.Add(Sphere.NewSphere(new Vector(-1.25F, 4.281F, 0), 0.2F / 2, Material.LightMaterial(Colour.White, 27)));
            scene.Add(Sphere.NewSphere(new Vector(-3.75F, 4.281F, 0), 0.066F / 2, Material.LightMaterial(Colour.White, 81.803F)));
            scene.Add(Sphere.NewSphere(new Vector(0, 10, 4), 1, Material.LightMaterial(Colour.White, 50)));
            var camera = Camera.LookAt(new Vector(0, 5, 12), new Vector(0, 1, 0), new Vector(0, 1, 0), 50);
            var sampler = DefaultSampler.NewSampler(4, 8);
            sampler.SpecularMode = SpecularMode.SpecularModeAll;
            sampler.LightMode = LightMode.LightModeAll;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("veachscene.png", 1000);
        }
        
        public static void maze(int width, int height)
        {
            var rand = Random.Shared;
            var scene = new Scene();
            var floor = Material.GlossyMaterial(Colour.HexColor(0x7E827A), 1.1F, Util.Radians(30));
            var material = Material.GlossyMaterial(Colour.HexColor(0xE3CDA4), 1.1F, Util.Radians(30));
            scene.Add(Cube.NewCube(new Vector(-10000, -10000, -10000), new Vector(10000, 10000, 0), floor));
            var n = 24;

            for (int x = -n; x <= n; x++) 
            {
                for (int y = -n; y <= n; y++)
                {

                    if (Random.Shared.NextDouble() > 0.8) 
                    {
                        var min = new Vector((double)x - 0.5F, (double)y - 0.5F, 0);
                        var max = new Vector((double)x + 0.5F, (double)y + 0.5F, 1);
                        var cube = Cube.NewCube(min, max, material);
                        scene.Add(cube);        
                    }
                }
            }
            scene.Add(Sphere.NewSphere(new Vector(0, 0, 2.25F), 0.25F, Material.LightMaterial(Colour.White, 500)));
            var camera = Camera.LookAt(new Vector(1, 0, 30), new Vector(0, 0, 0), new Vector(0, 0, 1), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("maze.png", 1000);
        }

        internal static void gopher(int width, int height)
        {
            var scene = new Scene();
            var gopher = Material.GlossyMaterial(Colour.Black, 1.2, Util.Radians(30));
            var wall = Material.GlossyMaterial(Colour.HexColor(0xFCFAE1), 1.5, Util.Radians(10));
            var light = Material.LightMaterial(Colour.White, 80);

            scene.Add(Cube.NewCube(new Vector(-10, -1, -10), new Vector(-2, 10, 10), wall));
            scene.Add(Cube.NewCube(new Vector(-10, -1, -10), new Vector(10, 0, 10), wall));
            scene.Add(Sphere.NewSphere(new Vector(4, 10, 1), 1, light));

            var mesh = OBJ.Load("models/gopher.obj", gopher);
            scene.Add(mesh);
            mesh.Transform(new Matrix().Rotate(new Vector(0,1,0), Util.Radians(-10)));
            mesh.SmoothNormals();
            mesh.FitInside(new Box(new Vector(-1, 0, -1), new Vector(1, 2, 1)), new Vector(0.5, 0, 0.5));
            scene.Add(mesh);
            
            var camera = Camera.LookAt(new Vector(4, 1, 0), new Vector(0, 0.9, 0), new Vector(0, 1, 0), 40);
            var sampler = DefaultSampler.NewSampler(16, 16);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.IterativeRender("gopher.png", 1000);
        }    

        public static void veach(int width, int height)
        {
            // Note: Resources for this example available in the examples folder
            // https://github.com/fogleman/pt/blob/master/examples/
            var scene = new Scene();
            Material material;
            Mesh mesh;

            material = Material.DiffuseMaterial(Colour.White);
            mesh = OBJ.Load("veach_scene/backdrop.obj", material);
            scene.Add(mesh);

            material = Material.MetallicMaterial(Colour.White, Util.Radians(20), 0);
            mesh = OBJ.Load("veach_scene/bar0.obj", material);
            scene.Add(mesh);

            material = Material.MetallicMaterial(Colour.White, Util.Radians(15), 0);
            mesh = OBJ.Load("veach_scene/bar1.obj", material);
            scene.Add(mesh);

            material = Material.MetallicMaterial(Colour.White, Util.Radians(10), 0);
            mesh = OBJ.Load("veach_scene/bar2.obj", material);
            scene.Add(mesh);

            material = Material.MetallicMaterial(Colour.White, Util.Radians(5), 0);
            mesh = OBJ.Load("veach_scene/bar3.obj", material);
            scene.Add(mesh);

            material = Material.MetallicMaterial(Colour.White, Util.Radians(0), 0);
            mesh = OBJ.Load("veach_scene/bar4.obj", material);
            scene.Add(mesh);

            scene.Add(Sphere.NewSphere(new Vector(3.75, 4.281, 0), 1.8 / 2, Material.LightMaterial(Colour.White, 3)));
            scene.Add(Sphere.NewSphere(new Vector(1.25, 4.281, 0), 0.6 / 2, Material.LightMaterial(Colour.White, 9)));
            scene.Add(Sphere.NewSphere(new Vector(-1.25, 4.281, 0), 0.2 / 2, Material.LightMaterial(Colour.White, 27)));
            scene.Add(Sphere.NewSphere(new Vector(-3.75, 4.281, 0), 0.066 / 2, Material.LightMaterial(Colour.White, 81.803)));
            scene.Add(Sphere.NewSphere(new Vector(0, 10, 4), 1, Material.LightMaterial(Colour.White, 50)));

            var camera = Camera.LookAt(new Vector(0, 5, 12), new Vector(0, 1, 0), new Vector(0, 1, 0), 50);
            var sampler = DefaultSampler.NewSampler(4, 8);
            sampler.SpecularMode = SpecularMode.SpecularModeAll;
            sampler.LightMode = LightMode.LightModeAll;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("veach.png", 100);
        }
    }
}