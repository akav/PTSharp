using STLDotNet6.Formats.StereoLithography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;


namespace PTSharpCore
{

    class STL
    {
        public static Mesh Load(String filePath, Material material)
        {

            byte[] buffer = new byte[80];
            FileInfo fi = new(filePath);
            BinaryReader reader;
            long size;

            if (File.Exists(filePath))
            {
                Console.WriteLine("Loading STL:" + filePath);
                size = fi.Length;
                bool isReadOnly = fi.IsReadOnly;

                using (reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
                {
                    buffer = reader.ReadBytes(80);
                    reader.ReadBytes(4);
                    int filelength = (int)reader.BaseStream.Length;
                    string code = reader.ReadByte().ToString() + reader.ReadByte().ToString();
                    reader.BaseStream.Close();

                    Console.WriteLine("Code = " + code);
                    if (code.Equals("00") || code.Equals("10181") || code.Equals("8689") || code.Equals("19593"))
                    {
                        return LoadSTLB(filePath, material);
                    }
                    else
                    {
                        return LoadSTLA(filePath, material);
                    }
                }
            }
            else
            {
                Console.WriteLine("Specified file could not be opened...");
                return null;
            }
        }

        public static Mesh LoadSTLA(String filename, Material material)
        {
            List<Triangle> triangles = new List<Triangle>();

            using (var reader = new StreamReader(filename))
            {
                string line;
                Vector normal = new();
                Vector[] vertices = new Vector[3];
                int vertexCount = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    var tokens = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens[0] == "facet" && tokens[1] == "normal")
                    {
                        normal = new Vector(
                            double.Parse(tokens[2], CultureInfo.InvariantCulture),
                            double.Parse(tokens[3], CultureInfo.InvariantCulture),
                            double.Parse(tokens[4], CultureInfo.InvariantCulture));
                    }
                    else if (tokens[0] == "vertex")
                    {
                        vertices[vertexCount] = new Vector(
                            double.Parse(tokens[1], CultureInfo.InvariantCulture),
                            double.Parse(tokens[2], CultureInfo.InvariantCulture),
                            double.Parse(tokens[3], CultureInfo.InvariantCulture));
                        vertexCount++;

                        if (vertexCount == 3)
                        {
                            // Create triangle and add to list
                            Triangle triangle = new Triangle
                            {
                                V1 = vertices[0],
                                V2 = vertices[1],
                                V3 = vertices[2],
                                N1 = normal,
                                N2 = normal,
                                N3 = normal,
                                Material = material
                            };
                            triangle.FixNormals();
                            triangles.Add(triangle);
                            vertexCount = 0;
                        }
                    }
                }
            }

            return Mesh.NewMesh(triangles.ToArray());
        }

        public static Mesh LoadSTLB(String filename, Material material)
        {
            List<Triangle> tlist = new List<Triangle>();

            try
            {
                STLDocument stlBinary;

                using (Stream stream = File.Open(filename, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        {
                            stlBinary = STLDocument.Read(reader);
                        }
                    }
                }

                foreach (var facet in stlBinary.Facets)
                {
                    Triangle t = new Triangle(new Vector(facet.Vertices[0].X, facet.Vertices[0].Y, facet.Vertices[0].Z),
                                              new Vector(facet.Vertices[1].X, facet.Vertices[1].Y, facet.Vertices[1].Z),
                                              new Vector(facet.Vertices[2].X, facet.Vertices[2].Y, facet.Vertices[2].Z), material);
                    t.FixNormals();
                    tlist.Add(t);
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }

            return Mesh.NewMesh(tlist.ToArray());
        }
    }
}
