using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace PTSharpCore
{
    class Example
    {
        public static void shrender(int l, int m)
        {
            Scene scene = new Scene();
            var eye = new IVector<double>(new double[] { 1, 1, 1, 0 });
            var center = new IVector<double>(new double[] { 0, 0, 0, 0 });
            var up = new IVector<double>(new double[] { 0, 0, 1, 0 });
            var light = Material.LightMaterial(Colour.White, 150);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 0, 5, 0 }), 0.5, light));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 5, 0, 2, 0 }), 0.5, light));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 5, 2, 0 }), 0.5, light));
            var pm = Material.GlossyMaterial(Colour.HexColor(0x105B63), 1.3, Util.Radians(30));
            var nm = Material.GlossyMaterial(Colour.HexColor(0xBD4932), 1.3, Util.Radians(30));
            var sh = SphericalHarmonic.NewSphericalHarmonic(l, m, pm, nm);
            scene.Add(sh);
            var camera = Camera.LookAt(eye, center, up, 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SetLightMode(LightMode.LightModeAll);
            sampler.SetSpecularMode(SpecularMode.SpecularModeFirst);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 1600 / 2, 1600 / 2, false);

            renderer.IterativeRender("sh.png", 2);
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

        public void dragon()
        {
           var scene = new Scene();
           var material = Material.GlossyMaterial(Colour.HexColor(0xB7CA79), 1.5, Util.Radians(20));
           var mesh = OBJ.Load("models/dragon.obj", material);
           mesh.FitInside(new Box(new IVector<double>(new double[] { -1, 0, -1, 0 }), new IVector<double>(new double[] { 1, 2, 1, 0 })), new IVector<double>(new double[] { 0.5, 0, 0.5, 0 }));
           scene.Add(mesh);
           var floor = Material.GlossyMaterial(Colour.HexColor(0xD8CAA8), 1.2, Util.Radians(5));
           scene.Add(Cube.NewCube(new IVector<double>(new double[] { -50, -50, -50, 0 }), new IVector<double>(new double[] { 50, 0, 50, 0 }), floor));
           var light = Material.LightMaterial(Colour.White, 75);
           scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1, 10, 4, 0 }), 1, light));
           var mouth = Material.LightMaterial(Colour.HexColor(0xFFFAD5), 500);
           scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -0.05, 1, -0.5, 0 }), 0.03, mouth));
           var camera = Camera.LookAt(new IVector<double>(new double[] { -3, 2, -1, 0 }), new IVector<double>(new double[] { 0, 0.6, -0.1, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 35);
           camera.SetFocus(new IVector<double>(new double[] { 0, 1, -0.5, 0 }), 0.03);
           var sampler = DefaultSampler.NewSampler(8, 8);
           var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
           renderer.IterativeRender("dragon.png", 10);
        }
        
        public void cube()
        {
            var scene = new Scene();

            var meshes = new IShape[]
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
                    scene.Add(TransformedShape.NewTransformedShape(meshes[new Random().Next(meshes.Length)], new Matrix().Translate(new IVector<double>(new double[] { fx, fy, fz, 0 }))));
                    scene.Add(TransformedShape.NewTransformedShape(meshes[new Random().Next(meshes.Length)], new Matrix().Translate(new IVector<double>(new double[] { fx, fy - 1, fz, 0 }))));
                }
            }

            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 8, 10, 0, 0 }), 3, Material.LightMaterial(Colour.White, 30)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { -10, 10, 0, 0 }), new IVector<double>(new double[] { -2, 0, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, false);
            renderer.IterativeRender("cube.png", 100);
        }

        public void runway()
        {
            const int radius = 2;
            const int height = 3;
            const int emission = 3;
            Scene scene = new Scene();
            var white = Material.DiffuseMaterial(Colour.White);
            var floor = Cube.NewCube(new IVector<double>(new double[] { -250, -1500, -1, 0 }), new IVector<double>(new double[] { 250, 6200, 0, 0 }), white);
            scene.Add(floor);
            var light = Material.LightMaterial(Colour.Kelvin(2700), emission);
            for (int y = 0; y <= 6000; y += 40)
            {
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -100, (double)y, height, 0 }), radius, light));
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, (double)y, height, 0 }), radius, light));
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 100, (double)y, height, 0 }), radius, light));

            }
            for (int y = -40; y >= -750; y -= 20)
            {
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -10, (double)y, height, 0 }), radius, light));
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, (double)y, height, 0 }), radius, light));
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 10, (double)y, height, 0 }), radius, light));
            }
            var green = Material.LightMaterial(Colour.HexColor(0x0BDB46), emission);
            var red = Material.LightMaterial(Colour.HexColor(0xDC4522), emission);

            for (int x = -160; x <= 160; x += 10)
            {
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { (double)x, -20, height, 0 }), radius, green));
                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { (double)x, 6100, height, 0 }), radius, red));
            }
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -160, 250, height, 0 }), radius, red));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -180, 250, height, 0 }), radius, red));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -200, 250, height, 0 }), radius, light));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -220, 250, height, 0 }), radius, light));
            for (int i = 0; i < 5; i++)
            {
                var y = (double)((i + 1) * -120);

                for (int j = 1; j <= 4; j++)
                {
                    var x = (double)(j + 4) * 7.5;
                    scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { x, y, height, 0 }), radius, red));
                    scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -x, y, height, 0 }), radius, red));
                    scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { x, -y, height, 0 }), radius, light));
                    scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -x, -y, height, 0 }), radius, light));
                }
            }
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, -1500, 200, 0 }), new IVector<double>(new double[] { 0, -100, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 20);
            camera.SetFocus(new IVector<double>(new double[] { 0, 20000, 0, 0 }), 1);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 1920, 1080, true);
            renderer.IterativeRender("runway.png", 1000);
        }

        public void bunny()
        {
            var scene = new Scene();
            var material = Material.GlossyMaterial(Colour.HexColor(0xF2EBC7), 1.5, Util.Radians(0));
            var mesh = OBJ.Load("models/bunny.obj", material);
            mesh.SmoothNormals();
            mesh.FitInside(new Box(new IVector<double>(new double[] { -1, 0, -1, 0 }), new IVector<double>(new double[] { 1, 2, 1, 0 })), new IVector<double>(new double[] { 0.5, 0, 0.5, 0 }));
            scene.Add(mesh);
            var floor = Material.GlossyMaterial(Colour.HexColor(0x33332D), 1.2, Util.Radians(20));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -10000, -10000, -10000, 0 }), new IVector<double>(new double[] { 10000, 0, 10000, 0 }), floor));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 5, 0, 0 }), 1, Material.LightMaterial(Colour.White, 10)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 4, 5, 4, 0 }), 1, Material.LightMaterial(Colour.White, 10)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { -1, 2, 3, 0 }), new IVector<double>(new double[] { 0, 0.75, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SetSpecularMode(SpecularMode.SpecularModeFirst);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            //renderer.FireflySamples = 32;
            renderer.IterativeRender("bunny.png", 1000);
        }

        public void ellipsoid()
        {
            var scene = new Scene();
            var wall = Material.GlossyMaterial(Colour.HexColor(0xFCFAE1), 1.333, Util.Radians(30));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 10, 10, 10, 0 }), 2, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -100, -100, -100, 0 }), new IVector<double>(new double[] { -12, 100, 100, 0 }), wall));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -100, -100, -100, 0 }), new IVector<double>(new double[] { 100, -1, 100, 0 }), wall));
            var material = Material.GlossyMaterial(Colour.HexColor(0x167F39), 1.333, Util.Radians(30));
            var sphere = Sphere.NewSphere(new IVector<double>(), 1, material);
            for (int i = 0; i < 180; i += 30)
            {
                var m = Matrix.Identity;
                m = m.Scale(new IVector<double>(new double[] { 0.3, 1, 5, 0 })).Mul(m);
                m = m.Rotate(new IVector<double>(new double[] { 0, 1, 0, 0 }), Util.Radians((double)i)).Mul(m);
                var shape = TransformedShape.NewTransformedShape(sphere, m);
                scene.Add(shape);
            }
            var camera = Camera.LookAt(new IVector<double>(new double[] { 8, 8, 0, 0 }), new IVector<double>(new double[] { 1, 0, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, false);
            renderer.IterativeRender("ellipsoid.png", 1000);
        }

        public void refraction()
        {
            var scene = new Scene();
            var glass = Material.ClearMaterial(1.5, 0);
            // add a sphere primitive
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1.5, 0, 0.5, 0 }), 1, glass));
            // add a mesh sphere
            var mesh = STL.Load("models/sphere.stl", glass);
            mesh.SmoothNormals();
            mesh.Transform(new Matrix().Translate(new IVector<double>(new double[] { 1.5, 0, 0.5, 0 })));
            scene.Add(mesh);
            // add the floor
            scene.Add(Plane.NewPlane(new IVector<double>(new double[] { 0, 0, -1, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), Material.DiffuseMaterial(Colour.White)));
            // add the light
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 0, 5, 0 }), 1, Material.LightMaterial(Colour.White, 15)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, -5, 5, 0 }), new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 50);
            var sampler = DefaultSampler.NewSampler(16, 8);
            sampler.SetSpecularMode(SpecularMode.SpecularModeAll);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("refraction.png", 100);
        }

        public void qbert()
        {
            var scene = new Scene();
            var floor = Material.GlossyMaterial(Colour.HexColor(0xFCFFF5), 1.2d, Util.Radians(30));
            var cube = Material.GlossyMaterial(Colour.HexColor(0xFF8C00), 1.3d, Util.Radians(20));
            var ball = Material.GlossyMaterial(Colour.HexColor(0xD90000), 1.4d, Util.Radians(10));
            int n = 7;
            var fn = (double)n;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n - z; x++)
                {
                    for (int y = 0; y < n - z - x; y++)
                    {
                        (var fx, var fy, var fz) = ((double)x, (double)y, (double)z);
                        scene.Add(Cube.NewCube(new IVector<double>(new double[] { fx, fy, fz, 0 }), new IVector<double>(new double[] { fx + 1, fy + 1, fz + 1, 0 }), cube));
        
                        if(x + y == n - z - 1)
                        {
                            if (Random.Shared.NextDouble() > 0.75d) 
                            {
                                scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { fx + 0.5, fy + 0.5, fz + 1.5, 0 }), 0.35, ball));
                            }   
                        }
                    }
                }
            }
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -1000, -1000, -1, 0 }), new IVector<double>(new double[] { 1000, 1000, 0, 0 }), floor));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { fn, fn / 3, fn * 2, 0 }), 1, Material.LightMaterial(Colour.White, 100)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { fn * 2, fn * 2, fn * 2, 0 }), new IVector<double>(new double[] { 0, 0, fn / 4, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            //renderer.FireflySamples = 64;
            renderer.IterativeRender("qbert.png", 100); 
        }

        public void love()
        {
            var scene = new Scene();
            var material = Material.GlossyMaterial(Colour.HexColor(0xF2F2F2), 1.5, Util.Radians(20));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -100, -1, -100, 0 }), new IVector<double>(new double[] { 100, 0, 100, 0 }), material));
            var heart = Material.GlossyMaterial(Colour.HexColor(0xF60A20), 1.5, Util.Radians(20));
            var mesh = STL.Load("love.stl", heart);
            mesh.FitInside(new Box(new IVector<double>(new double[] { -0.5, 0, -0.5, 0 }), new IVector<double>(new double[] { 0.5, 1, 0.5, 0 })), new IVector<double>(new double[] { 0.5, 0, 0.5, 0 }));
            scene.Add(mesh);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -2, 10, 2, 0 }), 1, Material.LightMaterial(Colour.White, 30)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 10, 2, 0 }), 1, Material.LightMaterial(Colour.White, 30)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 2, 10, 2, 0 }), 1, Material.LightMaterial(Colour.White, 30)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, 1.5, 2, 0 }), new IVector<double>(new double[] { 0, 0.5, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("love.png", 1000); 
        }

        public void materialspheres()
        {
            var scene = new Scene();
            var r = 0.4;
            Material material;
            material = Material.DiffuseMaterial(Colour.HexColor(0x334D5C));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -2, r, 0, 0 }), r, material));
            material = Material.SpecularMaterial(Colour.HexColor(0x334D5C), 2);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1, r, 0, 0 }), r, material));
            material = Material.GlossyMaterial(Colour.HexColor(0x334D5C), 2, Util.Radians(50));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, r, 0, 0 }), r, material));
            material = Material.TransparentMaterial(Colour.HexColor(0x334D5C), 2, Util.Radians(20), 1);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 1, r, 0, 0 }), r, material));
            material = Material.ClearMaterial(2, 0);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 2, r, 0, 0 }), r, material));
            material = Material.MetallicMaterial(Colour.HexColor(0xFFFFFF), 0, 1);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 1.5, -4, 0 }), 1.5, material));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -1000, -1, -1000, 0 }), new IVector<double>(new double[] { 1000, 0, 1000, 0 }), Material.GlossyMaterial(Colour.HexColor(0xFFFFFF), 1.4, Util.Radians(20))));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 5, 0, 0 }), 1, Material.LightMaterial(Colour.White, 25)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, 3, 6, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 30);
            var sampler = DefaultSampler.NewSampler(16, 16);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, false);
            renderer.FireflySamples = 32;
            renderer.IterativeRender("materialspheres.png", 100);
        }

        public void toybrick()
        {
            const double H = 1.46875;
            var scene = new Scene();
            scene.Color = Colour.White;
            var meshes = new Mesh[]
            {
                Util.CreateBrick(0xF2F3F2), // white
                Util.CreateBrick(0xC4281B), // bright red
                Util.CreateBrick(0x0D69AB), // bright blue
                Util.CreateBrick(0xF5CD2F), // bright yellow
                Util.CreateBrick(0x1B2A34), // black
                Util.CreateBrick(0x287F46), // dark green
            };

            for(int x = -30; x <= 50; x+=2)
            {
                for (int y = -50; y <= 20; y+=4)
                {
                    var h = new Random().Next(5) + 1;
                    for( int i = 0; i < h; i++)
                    {
                        var dy = 0;

                        if (((x / 2 + i)% 2) == 0 )
                        {
                            dy = 2;
                        }
                        var z = i * H;
                        var mnum = new Random().Next(meshes.Length);
                        var mesh = meshes[mnum];
                        var m = new Matrix().Translate(new IVector<double>(new double[] { (double)x, (double)(y + dy), (double)z, 0 }));
                        scene.Add(TransformedShape.NewTransformedShape(mesh, m));
                    }
                }
            }
            var camera = Camera.LookAt(new IVector<double>(new double[] { -23, 13, 20, 0 }), new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("toybrick.png", 1000);
        }

        public void cylinder()
        {
            Scene scene = new Scene();
            var meshes = new Mesh[]
            {
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0x730046), 1.6, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xBFBB11), 1.6, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xFFC200), 1.6, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xE88801), 1.6, Util.Radians(45))),
                Util.CreateMesh(Material.GlossyMaterial(Colour.HexColor(0xC93C00), 1.6, Util.Radians(45))),
            };

            for (int x = -6; x <= 3; x++)
            {
                var mesh = meshes[(x + 6) % meshes.Length];
                for (int y = -5; y <= 4; y++)
                {
                    var fx = (double)x / 2;
                    var fy = (double)y;
                    var fz = (double)x / 2;

                    scene.Add(TransformedShape.NewTransformedShape(mesh, new Matrix().Translate(new IVector<double>(new double[] { fx, fy, fz, 0 }))));
                }
            }
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 1, 0, 10, 0 }), 3, Material.LightMaterial(Colour.White, 20)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { -5, 0, 5, 0 }), new IVector<double>(new double[] { 1, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, false);
            renderer.IterativeRender("cylinder.png", 1000);
        }

        public void sphere()
        {
            int width = 1920;
            int height = 1080;
            Scene scene = new Scene();
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 1.5, 1.25, 0, 0 }), 1.25, Material.SpecularMaterial(Colour.HexColor(0x004358), 1.3)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1, 1, 2, 0 }), 1, Material.SpecularMaterial(Colour.HexColor(0xFFE11A), 1.3)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -2.5, 0.75, 0, 0 }), 0.75, Material.SpecularMaterial(Colour.HexColor(0xFD7400), 1.3)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -0.75, 0.5, -1, 0 }), 0.5, Material.ClearMaterial(1.5, 0)));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -10, -1, -10, 0 }), new IVector<double>(new double[] { 10, 0, 10, 0 }), Material.GlossyMaterial(Colour.White, 1.1, Util.Radians(10))));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1.5, 4, 0, 0 }), 0.5, Material.LightMaterial(Colour.White, 30)));
            Camera camera = Camera.LookAt(new IVector<double>(new double[] { 0, 2, -5, 0 }), new IVector<double>(new double[] { 0, 0.25, 3, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 45);
            camera.SetFocus(new IVector<double>(new double[] { -0.75, 1, -1, 0 }), 0.1);
            DefaultSampler sampler = DefaultSampler.NewSampler(4, 8);
            sampler.SpecularMode = SpecularMode.SpecularModeFirst;
            Renderer renderer = Renderer.NewRenderer(scene, camera, sampler, width, height, false);
            //renderer.FireflySamples = 64;
            renderer.IterativeRender("sphere.png", 500);
        }

        public void teapot()
        {
            var scene = new Scene();
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -2, 5, -3, 0 }), 0.5, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 5, 5, -3, 0 }), 0.5, Material.LightMaterial(Colour.White, 50)));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -30, -1, -30, 0 }), new IVector<double>(new double[] { 30, 0, 30, 0 }), Material.SpecularMaterial(Colour.HexColor(0xFCFAE1), 2)));
            var mesh = OBJ.Load("teapot.obj", Material.SpecularMaterial(Colour.HexColor(0xB9121B), 2));
            mesh.SmoothNormals();
            scene.Add(mesh);
            var camera = Camera.LookAt(new IVector<double>(new double[] { 2, 5, -6, 0 }), new IVector<double>(new double[] { 0.5, 1, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 45);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.SpecularMode = SpecularMode.SpecularModeFirst;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 1920, 1080, false);
            renderer.FireflySamples = 64;
            renderer.IterativeRender("teapot.png", 1000); 
        }

        public void hits()
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(new Colour(0.95, 0.95, 1));
            var light = Material.LightMaterial(Colour.White, 300);
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -0.75, -0.75, 5, 0 }), 0.25, light));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -1000, -1000, -1000, 0 }), new IVector<double>(new double[] { 1000, 1000, 0, 0 }), material));
            var mesh = STL.Load("models/hits.stl", material);
            mesh.SmoothNormalsThreshold(Util.Radians(10));
            mesh.FitInside(new Box(new IVector<double>(new double[] { -1, -1, 0, 0 }), new IVector<double>(new double[] { 1, 1, 2, 0 })), new IVector<double>(new double[] { .5, 0.5, 0, 0 }));
            scene.Add(mesh);
            var camera = Camera.LookAt(new IVector<double>(new double[] { 1.6, -3, 2, 0 }), new IVector<double>(new double[] { -0.25, 0.5, 0.5, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 50);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("hits.png", 1000);
        }

        public void simplesphere()
        {
            var scene = new Scene();
            // create a material
            var material = Material.DiffuseMaterial(Colour.White);

            // add the floor (a plane)
            var plane = Plane.NewPlane(new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), material);
            scene.Add(plane);

            // add the ball (a sphere)
            var sphere = Sphere.NewSphere(new IVector<double>(new double[] { 0, 0, 1, 0 }), 1.0D, material);
            scene.Add(sphere);

            // add a spherical light source
            var light = Sphere.NewSphere(new IVector<double>(new double[] { 0, 0, 5, 0 }), 0.1, Material.LightMaterial(Colour.White, 8));
            scene.Add(light);

            // position the camera
            var camera = Camera.LookAt(new IVector<double>(new double[] { 3, 3, 3, 0 }), new IVector<double>(new double[] { 0, 0, 0.5, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 50);

            // render the scene with progressive refinement
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, false);
            renderer.AdaptiveSamples = 128;
            renderer.IterativeRender("simplesphere.png", 1000);
        }

        public void suzanne()
        {
            var scene = new Scene();
            var material = Material.DiffuseMaterial(Colour.HexColor(0x334D5C));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0.5, 1, 3, 0 }), 1, Material.LightMaterial(Colour.White, 4)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 1.5, 1, 3, 0 }), 1, Material.LightMaterial(Colour.White, 4)));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -5, -5, -2, 0 }), new IVector<double>(new double[] { 5, 5, -1, 0 }), material));
            var mesh = OBJ.Load("models/suzanne.obj", Material.SpecularMaterial(Colour.HexColor(0xEFC94C), 1.3));
            scene.Add(mesh);
            var camera = Camera.LookAt(new IVector<double>(new double[] { 1, -0.45, 4, 0 }), new IVector<double>(new double[] { 1, -0.6, 0.4, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 40);
            var sampler = DefaultSampler.NewSampler(16, 8);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("suzanne.png", 1000);
        }

        public void sdf()
        {
            var scene = new Scene();
            var light = Material.LightMaterial(Colour.White, 180);
            double d = 4.0;
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1, -1, 0.5, 0 }).Normalize().MulScalar(d), 0.25, light));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, -1, 0.25, 0 }).Normalize().MulScalar(d), 0.25, light));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1, 1, 0, 0 }).Normalize().MulScalar(d), 0.25, light));
            var material = Material.GlossyMaterial(Colour.HexColor(0x468966), 1.2, Util.Radians(20));
            var sphere = SphereSDF.NewSphereSDF(0.65);
            var cube = CubeSDF.NewCubeSDF(new IVector<double>(new double[] { 1, 1, 1, 0 }));
            var roundedCube = IntersectionSDF.NewIntersectionSDF(new List<SDF> { sphere, cube });
            var a = CylinderSDF.NewCylinderSDF(0.25, 1.1);
            var b = TransformSDF.NewTransformSDF(a, new Matrix().Rotate(new IVector<double>(new double[] { 1, 0, 0, 0 }), Util.Radians(90)));
            var c = TransformSDF.NewTransformSDF(a, new Matrix().Rotate(new IVector<double>(new double[] { 0, 0, 1, 0 }), Util.Radians(90)));
            var difference = DifferenceSDF.NewDifferenceSDF(new List<SDF> { roundedCube, a, b, c });
            var sdf = TransformSDF.NewTransformSDF(difference, new Matrix().Rotate(new IVector<double>(new double[] { 0, 0, 1, 0 }), Util.Radians(30)));
            scene.Add(SDFShape.NewSDFShape(sdf, material));
            var floor = Material.GlossyMaterial(Colour.HexColor(0xFFF0A5), 1.2, Util.Radians(20));
            scene.Add(Plane.NewPlane(new IVector<double>(new double[] { 0, 0, -0.5, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), floor));
            var camera = Camera.LookAt(new IVector<double>(new double[] { -3, 0, 1, 0 }), new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            sampler.LightMode = LightMode.LightModeAll;
            sampler.SpecularMode = SpecularMode.SpecularModeAll;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("sdf.png", 1000);
        }

        public void volume()
        {
            string[] imglist = Directory.GetFiles(Directory.GetCurrentDirectory()+"\\images", "*.*", SearchOption.AllDirectories);
            List<Bitmap> bmplist = new List<Bitmap>();
            foreach (string file in imglist)
            {
                Console.WriteLine(file);
                bmplist.Add(new Bitmap(file));
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
            const double start = 0.2;
            const double size = 0.01;
            const double step = 0.1;
            List<Volume.VolumeWindow> windows = new List<Volume.VolumeWindow>();
            for (int i = 0; i < colors.Length; i++)
            {
                var lo = start + step * (double)i;
                var hi = lo + size;
                var material = Material.GlossyMaterial(colors[i], 1.3, Util.Radians(0));
                var w = new Volume.VolumeWindow(lo, hi, material);
                windows.Add(w);
            }
            var box = new Box(new IVector<double>(new double[] { -1, -1, -0.2, 0 }), new IVector<double>(new double[] { 1, 1, 1, 0 }));
            var volume = Volume.NewVolume(box, bmplist.ToArray(), 3.4 / 0.9765625, windows.ToArray());
            scene.Add(volume);
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, -3, -3, 0 }), new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, -1, 0 }), 35);
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
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 3.75, 4.281, 0, 0 }), 1.8 / 2, Material.LightMaterial(Colour.White, 3)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 1.25, 4.281, 0, 0 }), 0.6 / 2, Material.LightMaterial(Colour.White, 9)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -1.25, 4.281, 0, 0 }), 0.2 / 2, Material.LightMaterial(Colour.White, 27)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { -3.75, 4.281, 0, 0 }), 0.066 / 2, Material.LightMaterial(Colour.White, 81.803)));
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 10, 4, 0 }), 1, Material.LightMaterial(Colour.White, 50)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { 0, 5, 12, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), new IVector<double>(new double[] { 0, 1, 0, 0 }), 50);
            var sampler = DefaultSampler.NewSampler(4, 8);
            sampler.SpecularMode = SpecularMode.SpecularModeAll;
            sampler.LightMode = LightMode.LightModeAll;
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 960, 540, true);
            renderer.IterativeRender("veachscene.png", 1000);
        }
        
        public void maze()
        {
            var scene = new Scene();
            var floor = Material.GlossyMaterial(Colour.HexColor(0x7E827A), 1.1, Util.Radians(30));
            var material = Material.GlossyMaterial(Colour.HexColor(0xE3CDA4), 1.1, Util.Radians(30));
            scene.Add(Cube.NewCube(new IVector<double>(new double[] { -10000, -10000, -10000, 0 }), new IVector<double>(new double[] { 10000, 10000, 0, 0 }), floor));
            var n = 24;

            for (int x = -n; x <= n; x++) 
            {
                for (int y = -n; y <= n; y++)
                {
                    if (Random.Shared.NextDouble() > 0.8D) 
                    {
                        var min = new IVector<double>(new double[] { (double)x - 0.5D, (double)y - 0.5D, 0, 0 });
                        var max = new IVector<double>(new double[] { (double)x + 0.5D, (double)y + 0.5D, 1, 0 });
                        var cube = Cube.NewCube(min, max, material);
                        scene.Add(cube);        
                    }
                }
            }
            scene.Add(Sphere.NewSphere(new IVector<double>(new double[] { 0, 0, 2.25D, 0 }), 0.25, Material.LightMaterial(Colour.White, 500)));
            var camera = Camera.LookAt(new IVector<double>(new double[] { 1, 0, 30D, 0 }), new IVector<double>(new double[] { 0, 0, 0, 0 }), new IVector<double>(new double[] { 0, 0, 1, 0 }), 35);
            var sampler = DefaultSampler.NewSampler(4, 4);
            var renderer = Renderer.NewRenderer(scene, camera, sampler, 3840, 2160, true);
            renderer.FireflySamples = 128;
            renderer.IterativeRender("maze.png", 1000);
        }
    }
}
