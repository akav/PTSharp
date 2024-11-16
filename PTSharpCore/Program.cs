using System;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Window = Silk.NET.Windowing.Window;
using System.Threading.Tasks;

namespace PTSharpCore
{
    class Program : IDisposable
    {
        private static ImGuiController _controller;
        private static IInputContext _inputContext;
        private static IWindow _window;
        private static GL _gl;
        private static BufferObject<float> _vbo;
        private static BufferObject<uint> _ebo;
        private static VertexArrayObject<float, uint> _vao;
        private static Texture _texture;
        private static Shader _shader;

        public static int Width = 1920;
        public static int Height = 1080;
        public static byte[] Bitmap = new byte[Width * Height * 4];
        public static int Id;
        public static int WindowId;
        public static int ThreadCount = Environment.ProcessorCount;
        public const int TileSize = 32;
        public static Filter Filter = new TriangleFilter(new Vector2<float>(1.5f, 1.5f));
        public static Bounds2<float> CropWindow = new Bounds2<float>(Point2<float>.Zero, Point2<float>.One);

        private static readonly float[] Vertices =
        {
             //X      Y     Z     U     V
             -1.0f, -1.0f, 0.0f, 0.0f, 1.0f,
              1.0f, -1.0f, 0.0f, 1.0f, 1.0f,
              1.0f,  1.0f, 0.0f, 1.0f, 0.0f,
             -1.0f,  1.0f, 0.0f, 0.0f, 0.0f
        };

        private static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        static void Main(string[] args)
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height);
            options.Title = "PTSharp Viewport";
            _window = Window.Create(options);
            Id = WindowId++;
            var x = (Width * Id) % Width;
            var y = (Width * Id) / Height * 100;
            var location = new System.Drawing.Point(x, y);
            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Closing += OnClose;
            _window.Run();
        }

        private static void KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            if (key == Key.Escape)
            {
                _window.Close();
            }
        }

        private unsafe static void OnLoad()
        {
            try
            {
                _inputContext = _window.CreateInput();
                foreach (var keyboard in _inputContext.Keyboards)
                {
                    keyboard.KeyDown += KeyDown;
                }

                _gl = GL.GetApi(_window);
                _ebo = new BufferObject<uint>(_gl, Indices, BufferTargetARB.ElementArrayBuffer);
                _vbo = new BufferObject<float>(_gl, Vertices, BufferTargetARB.ArrayBuffer);
                _vao = new VertexArrayObject<float, uint>(_gl, _vbo, _ebo);
                _vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
                _vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
                _shader = new Shader(_gl, "shader.vert", "shader.frag");
                _controller = new ImGuiController(
                        _gl = _window.CreateOpenGL(),
                        _window,
                        _inputContext
                    );

                // Start rendering
                Task.Factory.StartNew(() => Example.example3(Width, Height));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during OnLoad: {ex.Message}");
            }
        }

        private static void OnClose()
        {
            DisposeResources();
        }

        private static unsafe void OnRender(double obj)
        {
            try
            {
                _controller.Update((float)2);
                _gl.Clear((uint)ClearBufferMask.ColorBufferBit);
                _vao.Bind();
                _shader.Use();

                // Loading a texture.
                _texture = new Texture(_gl, Bitmap, (uint)Width, (uint)Height);

                // Bind a texture and set the uTexture0 to use texture0.
                _texture.Bind(TextureUnit.Texture0);
                _shader.SetUniform("uTexture0", 0);
                _gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
                _texture.Dispose();

                // Make sure ImGui renders too!
                _controller.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during OnRender: {ex.Message}");
            }
        }

        private static void DisposeResources()
        {
            _controller?.Dispose();
            _inputContext?.Dispose();
            _vbo?.Dispose();
            _ebo?.Dispose();
            _vao?.Dispose();
            _shader?.Dispose();
            _texture?.Dispose();
        }

        public void Dispose()
        {
            DisposeResources();
        }
    }
}
