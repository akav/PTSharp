using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;

namespace PTSharpCore
{
    class STL
    {
        // Binary STL reader is based on the article by Frank Niemeyer 
        // http://frankniemeyer.blogspot.gr/2014/05/binary-stl-io-using-nativeinteropstream.html
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct STLTriangle
        {
            // 4 * 3 * 4 byte + 2 byte = 50 byte
            public Vector Normal;
            public Vector A;
            public Vector B;
            public Vector C;
            public ushort AttributeByteCount;

            public STLTriangle(
                Vector normalVec,
                Vector vertex1,
                Vector vertex2,
                Vector vertex3,
                ushort attr = 0)
            {
                Normal = normalVec;
                A = vertex1;
                B = vertex2;
                C = vertex3;
                AttributeByteCount = attr;
            }
        }

        public static Mesh Load(string filePath, Material material)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (IsBinaryStl(stream))
                {
                    return ReadBinaryStl(filePath, material);
                }
                else
                {
                    return ReadTextStl(filePath, material);
                }
            }
        }

        static bool IsBinaryStl(FileStream stream)
        {
            const int HeaderSize = 80;
            if (stream.Length < HeaderSize + 4) return false;

            byte[] header = new byte[HeaderSize];
            stream.ReadExactly(header, 0, HeaderSize);
            stream.Seek(0, SeekOrigin.Begin);

            // Check if the file starts with "solid", a marker for ASCII STL
            string headerString = Encoding.ASCII.GetString(header);
            if (headerString.TrimStart().StartsWith("solid", StringComparison.OrdinalIgnoreCase))
            {
                // Confirm if it’s ASCII by scanning for newline characters
                byte[] content = new byte[256];
                stream.ReadExactly(content);
                stream.Seek(0, SeekOrigin.Begin);
                string contentString = Encoding.ASCII.GetString(content);
                return !contentString.Contains("facet");
            }

            return true;
        }

        public static Mesh ReadTextStl(string filename, Material material)
        {
            const string regex = @"\s*(facet normal|vertex)\s+(?<X>[^\s]+)\s+(?<Y>[^\s]+)\s+(?<Z>[^\s]+)";
            const NumberStyles numberStyle = NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

            List<Vector> facetNormals = new List<Vector>();
            List<Vector> vertices = new List<Vector>();
            List<Triangle> triangles = new List<Triangle>();

            try
            {
                using (StreamReader file = new StreamReader(filename))
                {
                    // Check the header for "solid" to ensure it's an STL file
                    string line = file.ReadLine();
                    if (line == null || !line.Contains("solid"))
                    {
                        throw new InvalidDataException("Invalid STL file: Missing 'solid' header.");
                    }

                    while ((line = file.ReadLine()) != null)
                    {
                        line = line.Trim();

                        // Parse facet normal
                        if (line.StartsWith("facet normal", StringComparison.OrdinalIgnoreCase))
                        {
                            facetNormals.Add(ParseVector(line, regex, numberStyle));
                        }
                        // Parse vertices
                        else if (line.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                        {
                            vertices.Add(ParseVector(line, regex, numberStyle));
                        }
                        // Handle endfacet
                        else if (line.StartsWith("endfacet", StringComparison.OrdinalIgnoreCase))
                        {
                            if (vertices.Count >= 3)
                            {
                                // Add a triangle for every three vertices
                                var triangle = new Triangle(vertices[0], vertices[1], vertices[2], material);
                                triangle.FixNormals();
                                triangles.Add(triangle);
                                vertices.Clear(); // Clear vertices for the next facet
                            }
                        }
                        // Handle endsolid
                        else if (line.StartsWith("endsolid", StringComparison.OrdinalIgnoreCase))
                        {
                            break; // Exit the loop
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read STL file: {ex.Message}");
                return new Mesh(); // Return an empty mesh on failure
            }

            return Mesh.NewMesh(triangles.ToArray());
        }

        // Helper method to parse a vector from a line using a regex
        private static Vector ParseVector(string line, string regex, NumberStyles numberStyle)
        {
            Match match = Regex.Match(line, regex, RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidDataException($"Invalid line format: {line}");
            }

            double x = double.Parse(match.Groups["X"].Value, numberStyle, CultureInfo.InvariantCulture);
            double y = double.Parse(match.Groups["Y"].Value, numberStyle, CultureInfo.InvariantCulture);
            double z = double.Parse(match.Groups["Z"].Value, numberStyle, CultureInfo.InvariantCulture);

            return new Vector(x, y, z);
        }


        public static Mesh ReadBinaryStl(String filePath, Material material)
        {
            List<Triangle> tList = new List<Triangle>();

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    const int HeaderSize = 80;
                    const int FacetSize = 50; // Normal (3 floats) + 3 vertices (9 floats) + 2 bytes
                    

                    stream.Seek(HeaderSize, SeekOrigin.Begin);

                    // Read the number of facets
                    byte[] facetCountBytes = new byte[4];
                    stream.ReadExactly(facetCountBytes, 0, 4);
                    int facetCount = BitConverter.ToInt32(facetCountBytes, 0);

                    for (int i = 0; i < facetCount; i++)
                    {
                        byte[] facetBytes = new byte[FacetSize];
                        stream.ReadExactly(facetBytes, 0, FacetSize);

                        Vector a = new Vector(
                            BitConverter.ToSingle(facetBytes, 12),
                            BitConverter.ToSingle(facetBytes, 16),
                            BitConverter.ToSingle(facetBytes, 20)
                        );

                        Vector b = new Vector(
                            BitConverter.ToSingle(facetBytes, 24),
                            BitConverter.ToSingle(facetBytes, 28),
                            BitConverter.ToSingle(facetBytes, 32)
                        );

                        Vector c = new Vector(
                            BitConverter.ToSingle(facetBytes, 36),
                            BitConverter.ToSingle(facetBytes, 40),
                            BitConverter.ToSingle(facetBytes, 44)
                        );

                        Vector normal = new Vector(
                            BitConverter.ToSingle(facetBytes, 0),
                            BitConverter.ToSingle(facetBytes, 4),
                            BitConverter.ToSingle(facetBytes, 8)
                        );

                        ushort attributeByteCount = BitConverter.ToUInt16(facetBytes, 48);

                        Triangle t = new Triangle(a, b, c, material);
                        t.FixNormals();

                        tList.Add(t);

                    }                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading binary STL file: " + ex.Message);
            }

            return Mesh.NewMesh(tList.ToArray());
        }
    }
}