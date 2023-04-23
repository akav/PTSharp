using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Buffers.Text;
using System.Numerics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PTSharpCore
{
    class OBJ
    {   /*
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
        } */
        
        static Dictionary<string, Material> matList = new Dictionary<string, Material>();
        
        public static Mesh Load(string filePath, Material material)
        {
            var vertices = new List<Vector>();
            var texCoords = new List<Vector>();
            var normals = new List<Vector>();
            var faces = new List<List<Tuple<int, int, int>>>();

            var vertexRegex = new Regex("^v\\s+([-+]?\\d*\\.?\\d+)\\s+([-+]?\\d*\\.?\\d+)\\s+([-+]?\\d*\\.?\\d+)$");
            var texCoordRegex = new Regex("^vt\\s+([-+]?\\d*\\.?\\d+)\\s+([-+]?\\d*\\.?\\d+)(\\s+([-+]?\\d*\\.?\\d+))?$");
            var normalRegex = new Regex("^vn\\s+([-+]?\\d*\\.?\\d+)\\s+([-+]?\\d*\\.?\\d+)\\s+([-+]?\\d*\\.?\\d+)$");
            var faceRegex = new Regex("^f\\s+(.*)$");

            using (var reader = new StreamReader(filePath))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();

                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    {
                        continue;
                    }

                    var match = vertexRegex.Match(line);
                    if (match.Success)
                    {
                        var x = float.Parse(match.Groups[1].Value);
                        var y = float.Parse(match.Groups[2].Value);
                        var z = float.Parse(match.Groups[3].Value);
                        vertices.Add(new Vector(x, y, z));
                        continue;
                    }

                    match = texCoordRegex.Match(line);
                    if (match.Success)
                    {
                        var u = float.Parse(match.Groups[1].Value);
                        var v = float.Parse(match.Groups[2].Value);
                        var w = match.Groups.Count >= 4 && !string.IsNullOrEmpty(match.Groups[4].Value) ? float.Parse(match.Groups[4].Value) : 0f;
                        texCoords.Add(new Vector(u, v, w));
                        continue;
                    }

                    match = normalRegex.Match(line);
                    if (match.Success)
                    {
                        var x = float.Parse(match.Groups[1].Value);
                        var y = float.Parse(match.Groups[2].Value);
                        var z = float.Parse(match.Groups[3].Value);
                        normals.Add(new Vector(x, y, z));
                        continue;
                    }

                    match = faceRegex.Match(line);
                    if (match.Success)
                    {
                        var face = new List<Tuple<int, int, int>>();
                        var vertexInfo = match.Groups[1].Value.Split(' ');

                        foreach (var info in vertexInfo)
                        {
                            var indices = info.Split('/');

                            var vertexIndex = int.Parse(indices[0]) - 1;
                            var texCoordIndex = indices.Length > 1 && !string.IsNullOrEmpty(indices[1]) ? int.Parse(indices[1]) - 1 : -1;
                            var normalIndex = indices.Length > 2 && !string.IsNullOrEmpty(indices[2]) ? int.Parse(indices[2]) - 1 : -1;

                            face.Add(new Tuple<int, int, int>(vertexIndex, texCoordIndex, normalIndex));
                        }

                        faces.Add(face);
                    }
                }
            }

            var triangles = new Triangle[faces.Count];

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                var v1 = vertices[face[0].Item1];
                var v2 = vertices[face[1].Item1];
                var v3 = vertices[face[2].Item1];
                var t1 = face[0].Item2 >= 0 ? texCoords[face[0].Item2] : new Vector();
                var t2 = face[1].Item2 >= 0 ? texCoords[face[1].Item2] : new Vector();
                var t3 = face[2].Item2 >= 0 ? texCoords[face[2].Item2] : new Vector();
                var n1 = face[0].Item3 >= 0 ? normals[face[0].Item3] : (v2.Sub(v1)).Cross(v3.Sub(v1)).Normalize();
                var n2 = face[1].Item3 >= 0 ? normals[face[1].Item3] : (v3.Sub(v2)).Cross(v1.Sub(v2)).Normalize();
                var n3 = face[2].Item3 >= 0 ? normals[face[2].Item3] : (v1.Sub(v3)).Cross(v2.Sub(v3)).Normalize();

                Triangle t = Triangle.NewTriangle(v1, v2, v3, n1, n2, n3, t1, t2, t3, material);
                t.FixNormals();
                triangles[i] = t;
            }

            return Mesh.NewMesh(triangles);
        }

        public static void LoadMTL(string path, Material parent)
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
