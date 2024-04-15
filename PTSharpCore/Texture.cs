using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PTSharpCore
{

    public class Texture : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public unsafe Texture(GL gl, string path)
        {
            //Loading an image using imagesharp.
            using (Image<Rgba32> img = SixLabors.ImageSharp.Image.Load<Rgba32>(path))
            {
                //We need to flip our image as image sharps coordinates has origin (0, 0) in the top-left corner,
                //whereas openGL has origin in the bottom-left corner.
                img.Mutate(x => x.Flip(FlipMode.Vertical));

                // Converting the image data to a byte array
                byte[] imageData = new byte[img.Width * img.Height * 4];
                int index = 0;
                for (int y = 0; y < img.Height; y++)
                {
                    for (int x = 0; x < img.Width; x++)
                    {
                        Rgba32 pixel = img[x, y];
                        imageData[index++] = pixel.R;
                        imageData[index++] = pixel.G;
                        imageData[index++] = pixel.B;
                        imageData[index++] = pixel.A;
                    }
                }

                fixed (void* data = imageData)
                {
                    //Loading the actual image.
                    Load(gl, data, (uint)img.Width, (uint)img.Height);
                }
            }
        }

        public unsafe Texture(GL gl, Span<byte> data, uint width, uint height)
        {
            //We want the ability to create a texture using data generated from code aswell.
            fixed (void* d = &data[0])
            {
                Load(gl, d, width, height);
            }
        }

        private unsafe void Load(GL gl, void* data, uint width, uint height)
        {
            //Saving the gl instance.
            _gl = gl;

            //Generating the opengl handle;
            _handle = _gl.GenTexture();
            Bind();

            //Setting the data of a texture.
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            //Setting some texture perameters so the texture behaves as expected.
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

            //Generating mipmaps.
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            //When we bind a texture we can choose which textureslot we can bind it to.
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void Dispose()
        {
            //In order to dispose we need to delete the opengl handle for the texure.
            _gl.DeleteTexture(_handle);
        }
    }

    class ColorTexture : ITexture
    {
        public int Width;
        public int Height;
        public Colour[] Data;
        
        internal static IDictionary<string, ITexture> textures = new Dictionary<string, ITexture>();

        ColorTexture()
        {
            Width = 0;
            Height = 0;
            Data = Array.Empty<Colour>();//new Colour[] { };
        }
        
        ColorTexture(int width, int height, Colour[] data)
        {
            Width = width;
            Height = height;
            Data = data;
        }

        internal static ITexture GetTexture(string path)
        {
            if (textures.ContainsKey(path))
            {
                Console.WriteLine("Texture: " + path + " ... OK");
                return textures[path];
            }
            else
            {
                Console.WriteLine("Adding texture to list...");
                ITexture img = LoadTexture(path);
                textures.Add(path, img);
                return img;
            }
        }

        internal static ITexture LoadTexture(String path)
        {
            Console.WriteLine("IMG: "+path);
            SKBitmap image = Util.LoadImage(path);
            
            if (image == null)
            {
                Console.WriteLine("IMG load: FAIL");
            }
            else
            {
                Console.WriteLine("IMG load: OK ");
            }
            return NewTexture(image);
        }

        static ITexture NewTexture(SKBitmap image)
        {
            int xMax = image.Width;
            int yMax = image.Height;

            Colour[] imgdata = new Colour[xMax * yMax];

            for (int y = 0; y < yMax; y++)
            {
                for (int x = 0; x < xMax; x++)
                {
                    int index = y * xMax + x;
                    SKColor pixelcolor = image.GetPixel(x, y);
                    imgdata[index] = new Colour((double)(pixelcolor.Red) / 255, (double)(pixelcolor.Green) / 255, (double)(pixelcolor.Blue) / 255).Pow(2.2F);
                }
            }

            return new ColorTexture(xMax, yMax, imgdata);
        }
        
        ITexture ITexture.Pow(double a)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = Data[i].Pow(a);
            }
            return this;
        }

        ITexture ITexture.MulScalar(double a)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = Data[i].MulScalar(a);
            }
            return this;
        }

        Colour BilinearSample(double u, double v)
        {
            if(u == 1)
            {
                u -= Util.EPS;
            }
            if(v == 1)
            {
                v -= Util.EPS;
            }
            var w = (double)Width -1;
            var h = (double)Height - 1;
            (var X, var x) = Util.Modf(u * w);
            (var Y, var y) = Util.Modf(v * h);
            var x0 = (int)X;
            var y0 = (int)Y;
            var x1 = x0 + 1;
            var y1 = y0 + 1;
            var c00 = Data[y0 * Width + x0];
            var c01 = Data[y1 * Width + x0];
            var c10 = Data[y0 * Width + x1];
            var c11 = Data[y1 * Width + x1];
            var c = Colour.Black;
            c = c.Add(c00.MulScalar((1 - x) * (1 - y)));
            c = c.Add(c10.MulScalar(x * (1 - y)));
            c = c.Add(c01.MulScalar((1 - x) * y));
            c = c.Add(c11.MulScalar(x * y));
            return c;
        }

        static double Fract(double x)
        {
            x = Util.Modf(x).Item2;
            return x;
        }

        Colour ITexture.Sample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            return BilinearSample(u, 1 - v);
        }

        Vector ITexture.NormalSample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            var c = BilinearSample(u, 1 - v);
            return new Vector(c.r * 2 - 1, c.g * 2 - 1, c.b * 2 - 1).Normalize();
        }

        Vector ITexture.BumpSample(double u, double v)
        {
            u = Fract(Fract(u) + 1);
            v = Fract(Fract(v) + 1);
            v = 1 - v;
            int x = (int)(u * Width);
            int y = (int)(v * Height);
            (var x1, var x2) = (Util.ClampInt(x - 1, 0, Width - 1), Util.ClampInt(x + 1, 0, Width - 1));
            (var y1, var y2) = (Util.ClampInt(y - 1, 0, Height - 1), Util.ClampInt(y + 1, 0, Height - 1));
            Colour cx = Data[y * Width + x1].Sub(Data[y * Width + x2]);
            Colour cy = Data[y1 * Width + x].Sub(Data[y2 * Width + x]);
            return new Vector(cx.r, cy.r, 0);
        }
    }
}
