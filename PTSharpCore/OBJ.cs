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
    {           
        static Dictionary<string, Material> matList = new Dictionary<string, Material>();

        public static Mesh LoadOBJ(string path, Material parent)
        {
            Console.WriteLine($"Loading OBJ: {path}");
            List<Vector> vs = new List<Vector> { new Vector() }; // 1-based indexing
            List<Vector> vts = new List<Vector> { new Vector() }; // 1-based indexing
            List<Vector> vns = new List<Vector> { new Vector() }; // 1-based indexing
            List<Triangle> triangles = new List<Triangle>();
            Dictionary<string, Material> materials = new Dictionary<string, Material>();
            Material material = parent;

            using (var file = new StreamReader(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length == 0) continue;

                    string keyword = fields[0];
                    string[] args = fields.Skip(1).ToArray();

                    switch (keyword)
                    {
                        case "mtllib":
                            string p = RelativePath(path, args[0]);
                            LoadMTL(p, parent, materials);
                            break;
                        case "usemtl":
                            if (materials.TryGetValue(args[0], out Material m))
                            {
                                material = m;
                            }
                            break;
                        case "v":
                            var f = ParseFloats(args);
                            var v = new Vector(f[0], f[1], f[2]);
                            vs.Add(v);
                            break;
                        case "vt":
                            f = ParseFloats(args);
                            v = new Vector(f[0], f[1], 0);
                            vts.Add(v);
                            break;
                        case "vn":
                            f = ParseFloats(args);
                            v = new Vector(f[0], f[1], f[2]);
                            vns.Add(v);
                            break;
                        case "f":
                            int[] fvs = args.Select(arg => ParseIndex(arg.Split('/')[0], vs.Count)).ToArray();
                            int[] fvts = args.Select(arg => ParseIndex(arg.Split('/')[1], vts.Count)).ToArray();
                            int[] fvns = args.Select(arg => ParseIndex(arg.Split('/')[2], vns.Count)).ToArray();

                            for (int i = 1; i < fvs.Length - 1; i++)
                            {
                                Triangle t = new Triangle();
                                t.Material = material;
                                t.V1 = vs[fvs[0]];
                                t.V2 = vs[fvs[i]];
                                t.V3 = vs[fvs[i + 1]];
                                t.T1 = vts[fvts[0]];
                                t.T2 = vts[fvts[i]];
                                t.T3 = vts[fvts[i + 1]];
                                t.N1 = vns[fvns[0]];
                                t.N2 = vns[fvns[i]];
                                t.N3 = vns[fvns[i + 1]];
                                // t.FixNormals(); // Implement if needed
                                triangles.Add(t);
                            }
                            break;
                    }
                }
            }

            return Mesh.NewMesh(triangles.ToArray()); // Assuming a constructor in Mesh class that takes a list of triangles
        }

        public static Mesh RegexLoad(string filePath, Material material)
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
                triangles[i] = t;
            }

            return Mesh.NewMesh(triangles);
        }

        public static void LoadMTL(string path, Material parent, Dictionary<string, Material> materials)
        {
            Console.WriteLine($"Loading MTL: {path}");
            Material material = parent; // Assuming a Copy method in Material

            using (var file = new StreamReader(path))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    string[] fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length == 0) continue;

                    string keyword = fields[0];
                    string[] args = fields.Skip(1).ToArray();

                    switch (keyword)
                    {
                        case "newmtl":
                            material = parent;
                            materials[args[0]] = material;
                            break;
                        case "Ke":
                            var c = ParseFloats(args);
                            double max = Math.Max(Math.Max(c[0], c[1]), c[2]);
                            if (max > 0)
                            {
                                material.Color = new Colour(c[0] / max, c[1] / max, c[2] / max);
                                material.Emittance = max;
                            }
                            break;
                        case "Kd":
                            c = ParseFloats(args);
                            material.Color = new Colour(c[0], c[1], c[2]);
                            break;
                        case "map_Kd":
                            var kd_filepath = RelativePath(path, args[0]);
                            material.Texture = ColorTexture.GetTexture(kd_filepath); // Implement GetTexture
                            break;
                        case "map_bump":
                            var bump_filepath = RelativePath(path, args[0]);
                            material.NormalTexture = ColorTexture.GetTexture(bump_filepath).Pow(1 / 2.2); // Implement GetTexture and Pow
                            break;
                    }
                }
            }
        }

        private static int ParseIndex(string value, int length)
        {
            int parsed = int.Parse(value, CultureInfo.InvariantCulture);
            int n = parsed < 0 ? parsed + length : parsed;
            return n;
        }

        private static float[] ParseFloats(string[] args)
        {
            return args.Select(arg => float.Parse(arg, CultureInfo.InvariantCulture)).ToArray();
        }

        private static string RelativePath(string basePath, string relativePath)
        {
            var directory = Path.GetDirectoryName(basePath);
            return Path.Combine(directory, relativePath);
        }
    }
}
