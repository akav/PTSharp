using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTSharpCore
{
    internal class Instance
    {
        public Mesh Mesh { get; private set; }
        public Matrix Transform { get; private set; }
        public Material Material { get; private set; }

        public Instance(Mesh mesh, Matrix transform, Material material)
        {
            Mesh = mesh;
            Transform = transform;
            Material = material;
        }
    }
}
