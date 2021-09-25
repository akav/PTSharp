using NativeInteropEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PTSharpCore
{

    class STL
    {
        // Binary STL reader is based on the article by Frank Niemeyer 
        // http://frankniemeyer.blogspot.gr/2014/05/binary-stl-io-using-nativeinteropstream.html
        // Requires NativeInterop from Nuget
        // https://www.nuget.org/packages/NativeInterop/

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct STLVector
        {
            public float X;
            public float Y;
            public float Z;

            public STLVector(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct STLTriangle
        {
            // 4 * 3 * 4 byte + 2 byte = 50 byte
            public  STLVector Normal;
            public  STLVector A;
            public  STLVector B;
            public  STLVector C;
            public  ushort AttributeByteCount;

            public STLTriangle(
                STLVector normalVec,
                STLVector vertex1,
                STLVector vertex2,
                STLVector vertex3,
                ushort attr = 0)
            {
                Normal = normalVec;
                A = vertex1;
                B = vertex2;
                C = vertex3;
                AttributeByteCount = attr;
            }
        }

        STLTriangle[] mesh = new STLTriangle[] {

            new STLTriangle(new STLVector(0, 0, 0),
                    new STLVector(0, 0, 0),
                    new STLVector(0, 1, 0),
                    new STLVector(1, 0, 0)),
            new STLTriangle(new STLVector(0, 0, 0),
                    new STLVector(0, 0, 0),
                    new STLVector(0, 0, 1),
                    new STLVector(0, 1, 0)),
            new STLTriangle(new STLVector(0, 0, 0),
                    new STLVector(0, 0, 0),
                    new STLVector(0, 0, 1),
                    new STLVector(1, 0, 0)),
            new STLTriangle(new STLVector(0, 0, 0),
                    new STLVector(0, 1, 0),
                    new STLVector(0, 0, 1),
                    new STLVector(1, 0, 0)),
        };
    
        public static Mesh Load(String filePath, Material material)
        {
          
            byte[] buffer = new byte[80];
            FileInfo fi = new FileInfo(filePath);
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

                    //Console.WriteLine("Code = " + code);
                    if (code.Equals("00") || code.Equals("10181") || code.Equals("8689") || code.Equals("19593"))
                    {
                        return LoadSTLB(filePath, material);
                    } else {
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
            string line = null;
            int counter = 0;

            // Creating storage structures for storing facets, vertex and normals
            List<Vector> facetnormal = new List<Vector>();
            List<Vector> vertexes = new List<Vector>();
            List<Triangle> triangles = new List<Triangle>();
            Vector[] varray;
            Match match = null;

            const string regex = @"\s*(facet normal|vertex)\s+(?<X>[^\s]+)\s+(?<Y>[^\s]+)\s+(?<Z>[^\s]+)";
            const NumberStyles numberStyle = (NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
            StreamReader file = new StreamReader(filename);

            // Reading text filled STL file   
            try
            {
                // Checking to see if the file header has proper structure and that the file does contain something
                if ((line = file.ReadLine()) != null && line.Contains("solid"))
                {
                    counter++;
                    //While there are lines to be read in the file
                    while ((line = file.ReadLine()) != null && !line.Contains("endsolid"))
                    {
                        counter++;
                        if (line.Contains("normal"))
                        {
                            match = Regex.Match(line, regex, RegexOptions.IgnoreCase);
                            //Reading facet
                            //Console.WriteLine("Read facet on line " + counter);
                            double.TryParse(match.Groups["X"].Value, numberStyle, CultureInfo.InvariantCulture, out double x);
                            double.TryParse(match.Groups["Y"].Value, numberStyle, CultureInfo.InvariantCulture, out double y);
                            double.TryParse(match.Groups["Z"].Value, numberStyle, CultureInfo.InvariantCulture, out double z);

                            Vector f = new Vector(x, y, z);
                            //Console.WriteLine("Added facet (x,y,z)"+ " "+x+" "+y+" "+z);
                            facetnormal.Add(f);
                        }

                        line = file.ReadLine();
                        counter++;

                        // Checking if we are in the outer loop line
                        if (line.Contains("outer loop"))
                        {
                            //Console.WriteLine("Outer loop");
                            line = file.ReadLine();
                            counter++;
                        }

                        if (line.Contains("vertex"))
                        {
                            match = Regex.Match(line, regex, RegexOptions.IgnoreCase);
                            //Console.WriteLine("Read vertex on line " + counter);
                            double.TryParse(match.Groups["X"].Value, numberStyle, CultureInfo.InvariantCulture, out double x);
                            double.TryParse(match.Groups["Y"].Value, numberStyle, CultureInfo.InvariantCulture, out double y);
                            double.TryParse(match.Groups["Z"].Value, numberStyle, CultureInfo.InvariantCulture, out double z);

                            Vector v = new Vector(x, y, z);
                            //Console.WriteLine("Added vertex 1 (x,y,z)" + " " + x + " " + y + " " + z);
                            vertexes.Add(v);
                        }

                        line = file.ReadLine();
                        counter++;

                        if (line.Contains("vertex"))
                        {
                            match = Regex.Match(line, regex, RegexOptions.IgnoreCase);
                            //Console.WriteLine("Read vertex on line " + counter);
                            double.TryParse(match.Groups["X"].Value, numberStyle, CultureInfo.InvariantCulture, out double x);
                            double.TryParse(match.Groups["Y"].Value, numberStyle, CultureInfo.InvariantCulture, out double y);
                            double.TryParse(match.Groups["Z"].Value, numberStyle, CultureInfo.InvariantCulture, out double z);

                            Vector v = new Vector(x, y, z);
                            //Console.WriteLine("Added vertex 2 (x,y,z)" + " " + x + " " + y + " " + z);
                            vertexes.Add(v);
                            line = file.ReadLine();
                            counter++;
                        }

                        if (line.Contains("vertex"))
                        {
                            match = Regex.Match(line, regex, RegexOptions.IgnoreCase);
                            //Console.WriteLine("Read vertex on line " + counter);
                            double.TryParse(match.Groups["X"].Value, numberStyle, CultureInfo.InvariantCulture, out double x);
                            double.TryParse(match.Groups["Y"].Value, numberStyle, CultureInfo.InvariantCulture, out double y);
                            double.TryParse(match.Groups["Z"].Value, numberStyle, CultureInfo.InvariantCulture, out double z);

                            Vector v = new Vector(x, y, z);
                            //Console.WriteLine("Added vertex 3 (x,y,z)" + " " + x + " " + y + " " + z);
                            vertexes.Add(v);
                            line = file.ReadLine();
                            counter++;
                        }
                        
                        if (line.Contains("endloop"))
                        {
                            //Console.WriteLine("End loop");
                            line = file.ReadLine();
                            counter++;
                        }
                        
                        if (line.Contains("endfacet"))
                        {
                            //Console.WriteLine("End facet");
                            line = file.ReadLine();
                            counter++;

                            if (line.Contains("endsolid"))
                            {
                                varray = vertexes.ToArray();
                                for (int i = 0; i < varray.Length; i += 3)
                                {
                                    Triangle t = new Triangle(varray[i + 0], varray[i + 1], varray[i + 2], material);
                                    t.FixNormals();
                                    triangles.Add(t);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
                return null;
            }
            file.Close();
            return Mesh.NewMesh(triangles.ToArray());
        }

       public static Mesh LoadSTLB(String filename, Material material)
        {
            string header;
            STLTriangle[] mesh;

            using (var br = new BinaryReader(File.OpenRead(filename), Encoding.ASCII))
            {
                header = Encoding.ASCII.GetString(br.ReadBytes(80));
                var triCount = br.ReadUInt32();
                mesh = br.BaseStream.ReadUnmanagedStructRange<STLTriangle>((int)triCount);
            }

            List<Triangle> tlist = new List<Triangle>();

            foreach (STLTriangle m in mesh)
            {
                Triangle t = new Triangle(new Vector(m.A.X, m.A.Y, m.A.Z), new Vector(m.B.X, m.B.Y, m.B.Z), new Vector(m.C.X, m.C.Y, m.C.Z), material);
                t.FixNormals();
                tlist.Add(t);
            }
            return Mesh.NewMesh(tlist.ToArray());
        }
    }
}
