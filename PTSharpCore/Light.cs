using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public interface ILight
    {
        LightMode Type { get; }
        Vector Position { get; }
        Vector Direction { get; }
        Colour Color { get; }
        double Intensity { get; }
    }

    public struct PointLight : ILight
    {
        public LightMode Type => LightMode.LightModePoint;
        public Vector Position { get; }
        public Vector Direction => Vector.Zero;
        public Colour Color { get; }
        public double Intensity { get; }

        public PointLight(Vector position, Colour color, double intensity)
        {
            Position = position;
            Color = color;
            Intensity = intensity;
        }
    }

    public struct DirectionalLight : ILight
    {
        public LightMode Type => LightMode.LightModeDirectional;
        public Vector Position => Vector.Zero;
        public Vector Direction { get; }
        public Colour Color { get; }
        public double Intensity { get; }

        public DirectionalLight(Vector direction, Colour color, double intensity)
        {
            Direction = direction;
            Color = color;
            Intensity = intensity;
        }
    }

    public struct SpotLight : ILight
    {
        public LightMode Type => LightMode.LightModeSpot;
        public Vector Position { get; }
        public Vector Direction { get; }
        public Colour Color { get; }
        public double Intensity { get; }
        public double Angle { get; }

        public SpotLight(Vector position, Vector direction, Colour color, double intensity, double angle)
        {
            Position = position;
            Direction = direction;
            Color = color;
            Intensity = intensity;
            Angle = angle;
        }
    }

    public struct AreaLight : ILight
    {
        public LightMode Type => LightMode.LightModeArea;
        public Vector Position { get; }
        public Vector Direction { get; }
        public Colour Color { get; }
        public double Intensity { get; }
        public double Width { get; }
        public double Height { get; }

        public AreaLight(Vector position, Vector direction, Colour color, double intensity, double width, double height)
        {
            Position = position;
            Direction = direction;
            Color = color;
            Intensity = intensity;
            Width = width;
            Height = height;
        }
    }

}
