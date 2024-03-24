using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System;
using System.Threading.Tasks;
using Window = Silk.NET.Windowing.Window;

namespace PTSharpCore
{
    class Program
    {
        public static ImGuiController controller = null;
        public static IInputContext inputContext = null;
        private static IWindow window;
        private static GL Gl;
        private static BufferObject<float> Vbo;
        private static BufferObject<uint> Ebo;
        private static VertexArrayObject<float, uint> Vao;

        //Create a texture object.
        private static Texture Texture;
        private static Shader Shader;

        public static int Width = 1920;
        public static int Height = 1080;

        public static byte[] bitmap = new byte[Width * Height * 4];
        public static int id;
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
            window = Window.Create(options);
            id = WindowId++;
            var x = (Width * id) % Width;
            var y = (Width * id) / Height * 100;
            var Location = new System.Drawing.Point(x, y);
            window.Load += OnLoad;
            window.Render += OnRender;
            window.Closing += OnClose;
            window.Run();
        }

        private static void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                window.Close();
            }
        }

        private unsafe static void OnLoad()
        {
            IInputContext input = window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }

            Gl = GL.GetApi(window);
            Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
            Vbo = new BufferObject<float>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
            Vao = new VertexArrayObject<float, uint>(Gl, Vbo, Ebo);
            Vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
            Vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            Shader = new Shader(Gl, "shader.vert", "shader.frag");
            controller = new ImGuiController(
                    Gl = window.CreateOpenGL(), // load OpenGL
                    window, // pass in our window
                    inputContext = window.CreateInput() // create an input context
                );

            // Start rendering
            Task.Factory.StartNew(() => Example.Runway(Width, Height));
        }

        private static void OnClose()
        {
            // Dispose our controller first
            controller?.Dispose();

            // Dispose the input context
            inputContext?.Dispose();

            Vbo.Dispose();
            Ebo.Dispose();
            Vao.Dispose();
            Shader.Dispose();

            //Remember to dispose the texture.
            Texture.Dispose();
        }

        private static unsafe void OnRender(double obj)
        {
            controller.Update((float)2);
            Gl.Clear((uint)ClearBufferMask.ColorBufferBit);
            Vao.Bind();
            Shader.Use();

            //Loading a texture.
            Texture = new Texture(Gl, bitmap, (uint)Width, (uint)Height);

            //Bind a texture and and set the uTexture0 to use texture0.
            Texture.Bind(TextureUnit.Texture0);
            Shader.SetUniform("uTexture0", 0);
            Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            Texture.Dispose();

            // This is where you'll do all of your ImGUi rendering
            // Here, we're just showing the ImGui built-in demo window.
            // ImGuiNET.ImGui.ShowDemoWindow();

            // Make sure ImGui renders too!
            controller.Render();
        }
    }
}
