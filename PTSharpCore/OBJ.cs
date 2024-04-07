using System;
using System.Collections.Generic;
using System.IO;

namespace PTSharpCore
{
    class OBJ
    {
        static Dictionary<string, Material> matList = new Dictionary<string, Material>();

        internal static Mesh Load(string path, Material parent)
        {
            Console.WriteLine("Loading OBJ:" + path);
            List<Vector> vs = new List<Vector>();
            List<Vector> vts = new List<Vector>();
            List<Vector> vns = new List<Vector>();
            vns.Add(new Vector(0, 0, 0));
            List<int> vertexIndices = new List<int>();
            List<int> textureIndices = new List<int>();
            List<int> normalIndices = new List<int>();
            List<Triangle> triangles = new List<Triangle>();

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }

            var material = parent;

            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    List<string> words = new List<string>(streamReader.ReadLine().ToLower().Split(' '));

                    words.RemoveAll(s => s == string.Empty);

                    if (words.Count == 0)
                        continue;

                    string type = words[0];

                    words.RemoveAt(0);

                    switch (type)
                    {
                        // Mtl
                        case "mtllib":
                            var p = Directory.GetCurrentDirectory() + "\\" + words[0];
                            Console.WriteLine("Reading mtllib:" + p);
                            LoadMTL(p, parent);
                            break;
                        case "usemtl":
                            if (!matList.ContainsKey(words[0]))
                            {
                                Console.WriteLine("Mtl " + words[0] + " not contained in list...");

                            }
                            else
                            {
                                Console.WriteLine("Using mtl file..." + words[0]);
                                var m = matList[words[0]];
                                material = m;
                            }
                            break;
                        // vertex
                        case "v":
                            Vector v = new Vector(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
                            vs.Add(v);
                            //v.Index = vs.Count(); 
                            break;
                        case "vt":
                            Vector vt = new Vector(float.Parse(words[0]), float.Parse(words[1]), 0);
                            vts.Add(vt);
                            //vt.Index = vts.Count();
                            break;
                        case "vn":
                            Vector vn = new Vector(float.Parse(words[0]), float.Parse(words[1]), float.Parse(words[2]));
                            vns.Add(vn);
                            //vn.Index = vns.Count();
                            break;
                        // face
                        case "f":
                            var fvs = new int[words.Count];
                            var fvts = new int[words.Count];
                            var fvns = new int[words.Count];
                            string[] separatingChars = { "//", "/" };

                            int count = 0;
                            foreach (string arg in words)
                            {
                                if (arg.Length == 0)
                                    continue;

                                string[] vertex = arg.Split(separatingChars, System.StringSplitOptions.RemoveEmptyEntries);

                                if (vertex.Length > 0 && vertex[0].Length != 0)
                                    fvs[count] = int.Parse(vertex[0]) - 1;// -1;

                                if (vertex.Length > 1 && vertex[1].Length != 0)
                                    fvts[count] = int.Parse(vertex[1]) - 1;// -1;

                                if (vertex.Length > 2)
                                    fvns[count] = int.Parse(vertex[2]) - 1;// -1;

                                count++;
                            }

                            for (int i = 1; i < fvs.Length - 1; i++)
                            {
                                (var i1, var i2, var i3) = (0, i, i + 1);
                                var t = new Triangle();
                                t.Material = material;

                                if (vs.Count == 0)
                                {
                                    t.V1 = new Vector();
                                    t.V2 = new Vector();
                                    t.V3 = new Vector();
                                }
                                else
                                {
                                    t.V1 = vs[fvs[i1]];
                                    t.V2 = vs[fvs[i2]];
                                    t.V3 = vs[fvs[i3]];
                                }


                                if (vts.Count == 0)
                                {
                                    t.T1 = new Vector();
                                    t.T2 = new Vector();
                                    t.T3 = new Vector();
                                }
                                else
                                {
                                    t.T1 = vts[fvts[i1]];
                                    t.T2 = vts[fvts[i2]];
                                    t.T3 = vts[fvts[i3]];
                                }

                                if (vns.Count == 0)
                                {
                                    t.N1 = new Vector();
                                    t.N2 = new Vector();
                                    t.N3 = new Vector();
                                }
                                else
                                {
                                    t.N1 = vns[fvns[i1]];
                                    t.N2 = vns[fvns[i2]];
                                    t.N3 = vns[fvns[i3]];
                                }
                                t.FixNormals();
                                triangles.Add(t);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }
            return Mesh.NewMesh(triangles.ToArray());
        }

        public static void LoadMTL(string path, Material parent) //, Dictionary<string, Material> materials)
        {
            Console.WriteLine("Loading MTL:" + path);
            var parentCopy = parent;
            var material = parentCopy;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Unable to open \"" + path + "\", does not exist.");
            }

            using (StreamReader streamReader = new StreamReader(path))
            {
                while (!streamReader.EndOfStream)
                {
                    string[] words = streamReader.ReadLine().Split(' ');
                    switch (words[0])
                    {
                        case "newmtl":
                            parentCopy = parent;
                            material = parentCopy;
                            //materials[words[0].Split(' ').ToString()] = material;
                            matList[words[1]] = material;
                            break;
                        case "Ke":
                            var max = Math.Max(Math.Max(float.Parse(words[1]), float.Parse(words[2])), float.Parse(words[3]));
                            if (max > 0)
                            {
                                material.Color = new Colour(float.Parse(words[1]) / max, float.Parse(words[2]) / max, float.Parse(words[3]) / max);
                                material.Emittance = max;
                            }
                            break;
                        case "Kd":
                            material.Color = new Colour(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]));
                            break;
                        case "map_Kd":
                            Console.WriteLine("map_Kd: " + Directory.GetCurrentDirectory() + "\\" + words[1]);
                            var kdmap = Directory.GetCurrentDirectory() + "\\" + words[1];
                            material.Texture = ColorTexture.GetTexture(kdmap);
                            break;
                        case "map_bump":
                            Console.WriteLine("map_bump: " + Directory.GetCurrentDirectory() + "\\" + words[3]);
                            var bumpmap = Directory.GetCurrentDirectory() + "\\" + words[3];
                            material.NormalTexture = ColorTexture.GetTexture(bumpmap).Pow(1 / 2.2);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}