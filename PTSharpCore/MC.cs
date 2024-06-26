using System;
using System.Collections.Generic;
using System.Linq;

namespace PTSharpCore
{
    class MC 
    {   
        internal static Mesh NewSDFMesh(SDF sdf, Box box, double step)
        {
            var min = box.Min;
            var size = box.Size();
            var nx = (int)Math.Ceiling(size.X / step);
            var ny = (int)Math.Ceiling(size.Y / step);
            var nz = (int)Math.Ceiling(size.Z / step);
            var sx = size.X / nx;
            var sy = size.Y / ny;
            var sz = size.Z / nz;
            List<Triangle> triangles = new();
            for (int x = 0; x < nx - 1; x++)
            {
                for(int y = 0; y < ny - 1; y++)
                {
                    for(int z = 0; z < nz - 1; z++)
                    {
                        (var x0, var y0, var z0) = (x * sx + min.X, y * sy + min.Y, z * sz + min.Z);
                        (var x1, var y1, var z1) = (x0 + sx, y0 + sy, z0 + sz);

                        var p = new Vector[8] {
                                new Vector( x0, y0, z0),
                                new Vector( x1, y0, z0),
                                new Vector( x1, y1, z0),
                                new Vector( x0, y1, z0),
                                new Vector( x0, y0, z1),
                                new Vector( x1, y0, z1),
                                new Vector( x1, y1, z1),
                                new Vector( x0, y1, z1)
                        };

                        double[] v = new double[8];
        
                        for(int i = 0; i < 8; i++)
                        {
                            v[i] = sdf.Evaluate(p[i]);
                        }

                        if (mcPolygonize(p, v, 0) == null)
                        {
                            continue;
                        } else
                        {
                            triangles.AddRange(mcPolygonize(p, v, 0));
                        }
                        
                        /*switch (mcPolygonize(p, v, 0))
                        {
                            case null:
                                continue;
                            default:
                                triangles.AddRange(mcPolygonize(p, v, 0));
                                break;
                        }*/
                    }
                }
            }
            return Mesh.NewMesh(triangles.ToArray());
        }
        
        static Triangle[] mcPolygonize(Vector[] p, double[] v, double x)
        {
            int index = 0;
            for (int i = 0; i < 8; i++)
            {
                if (v[i] < x)
                {
                    index |= 1 << Convert.ToUInt16(i);
                }
            }
            if (edgetable[index] == 0)
            {
                return null;
            }
            Vector[] points = new Vector[12];
            for (int i = 0; i < 12; i++)
            {
                int bit = 1 << Convert.ToUInt16(i);
                if ((edgetable[index] & bit) != 0)
                {
                    int a = pairTable[i][0];
                    int b = pairTable[i][1];
                    points[i] = mcInterpolate(p[a], p[b], v[a], v[b], x);
                }
            }
            
            var table = triangleTable[index];
            var count = table.Length / 3;
            Triangle[] result = new Triangle[count];
            
            for (int i = 0; i < count; i++)
            {
                Triangle triangle = new()
                {
                    V3 = points[table[i * 3 + 0]],
                    V2 = points[table[i * 3 + 1]],
                    V1 = points[table[i * 3 + 2]]
                };
                triangle.FixNormals();
                result[i] = triangle;
            }
            return result;
        }
        
        static Vector mcInterpolate(Vector p1, Vector p2, double v1, double v2, double x)
        {
            if (Math.Abs(x - v1) < Util.EPS)
                return p1;
            
            if (Math.Abs(x - v2) < Util.EPS)
                return p2;
            
            if (Math.Abs(v1 - v2) < Util.EPS)
                return p1;
            
            var t = (x - v1) / (v2 - v1);
            
            return new Vector(p1.X + t * (p2.X - p1.X), p1.Y + t * (p2.Y - p1.Y), p1.Z + t * (p2.Z - p1.Z));
        }

        static readonly int[][] pairTable =  {
            new int[] {0, 1}, new int[] {1, 2}, new int[] {2, 3}, new int[] {3, 0}, 
            new int[] {4, 5}, new int[] {5, 6}, new int[] {6, 7}, new int[] {7, 4},
            new int[] {0, 4}, new int[] {1, 5}, new int[] {2, 6}, new int[] {3, 7}
        };

        static readonly int[] edgetable =
        {
            0x0000, 0x0109, 0x0203, 0x030a, 0x0406, 0x050f, 0x0605, 0x070c,
            0x080c, 0x0905, 0x0a0f, 0x0b06, 0x0c0a, 0x0d03, 0x0e09, 0x0f00,
            0x0190, 0x0099, 0x0393, 0x029a, 0x0596, 0x049f, 0x0795, 0x069c,
            0x099c, 0x0895, 0x0b9f, 0x0a96, 0x0d9a, 0x0c93, 0x0f99, 0x0e90,
            0x0230, 0x0339, 0x0033, 0x013a, 0x0636, 0x073f, 0x0435, 0x053c,
            0x0a3c, 0x0b35, 0x083f, 0x0936, 0x0e3a, 0x0f33, 0x0c39, 0x0d30,
            0x03a0, 0x02a9, 0x01a3, 0x00aa, 0x07a6, 0x06af, 0x05a5, 0x04ac,
            0x0bac, 0x0aa5, 0x09af, 0x08a6, 0x0faa, 0x0ea3, 0x0da9, 0x0ca0,
            0x0460, 0x0569, 0x0663, 0x076a, 0x0066, 0x016f, 0x0265, 0x036c,
            0x0c6c, 0x0d65, 0x0e6f, 0x0f66, 0x086a, 0x0963, 0x0a69, 0x0b60,
            0x05f0, 0x04f9, 0x07f3, 0x06fa, 0x01f6, 0x00ff, 0x03f5, 0x02fc,
            0x0dfc, 0x0cf5, 0x0fff, 0x0ef6, 0x09fa, 0x08f3, 0x0bf9, 0x0af0,
            0x0650, 0x0759, 0x0453, 0x055a, 0x0256, 0x035f, 0x0055, 0x015c,
            0x0e5c, 0x0f55, 0x0c5f, 0x0d56, 0x0a5a, 0x0b53, 0x0859, 0x0950,
            0x07c0, 0x06c9, 0x05c3, 0x04ca, 0x03c6, 0x02cf, 0x01c5, 0x00cc,
            0x0fcc, 0x0ec5, 0x0dcf, 0x0cc6, 0x0bca, 0x0ac3, 0x09c9, 0x08c0,
            0x08c0, 0x09c9, 0x0ac3, 0x0bca, 0x0cc6, 0x0dcf, 0x0ec5, 0x0fcc,
            0x00cc, 0x01c5, 0x02cf, 0x03c6, 0x04ca, 0x05c3, 0x06c9, 0x07c0,
            0x0950, 0x0859, 0x0b53, 0x0a5a, 0x0d56, 0x0c5f, 0x0f55, 0x0e5c,
            0x015c, 0x0055, 0x035f, 0x0256, 0x055a, 0x0453, 0x0759, 0x0650,
            0x0af0, 0x0bf9, 0x08f3, 0x09fa, 0x0ef6, 0x0fff, 0x0cf5, 0x0dfc,
            0x02fc, 0x03f5, 0x00ff, 0x01f6, 0x06fa, 0x07f3, 0x04f9, 0x05f0,
            0x0b60, 0x0a69, 0x0963, 0x086a, 0x0f66, 0x0e6f, 0x0d65, 0x0c6c,
            0x036c, 0x0265, 0x016f, 0x0066, 0x076a, 0x0663, 0x0569, 0x0460,
            0x0ca0, 0x0da9, 0x0ea3, 0x0faa, 0x08a6, 0x09af, 0x0aa5, 0x0bac,
            0x04ac, 0x05a5, 0x06af, 0x07a6, 0x00aa, 0x01a3, 0x02a9, 0x03a0,
            0x0d30, 0x0c39, 0x0f33, 0x0e3a, 0x0936, 0x083f, 0x0b35, 0x0a3c,
            0x053c, 0x0435, 0x073f, 0x0636, 0x013a, 0x0033, 0x0339, 0x0230,
            0x0e90, 0x0f99, 0x0c93, 0x0d9a, 0x0a96, 0x0b9f, 0x0895, 0x099c,
            0x069c, 0x0795, 0x049f, 0x0596, 0x029a, 0x0393, 0x0099, 0x0190,
            0x0f00, 0x0e09, 0x0d03, 0x0c0a, 0x0b06, 0x0a0f, 0x0905, 0x080c,
            0x070c, 0x0605, 0x050f, 0x0406, 0x030a, 0x0203, 0x0109, 0x0000
        };

        static readonly int[][] triangleTable = new int[][] 
        {   
            new int[] {},
            new int[] {0, 8, 3},
            new int[] {0, 1, 9},
            new int[] {1, 8, 3, 9, 8, 1},
            new int[] {1, 2, 10},
            new int[] {0, 8, 3, 1, 2, 10},
            new int[] {9, 2, 10, 0, 2, 9},
            new int[] {2, 8, 3, 2, 10, 8, 10, 9, 8},
            new int[] {3, 11, 2},
            new int[] {0, 11, 2, 8, 11, 0},
            new int[] {1, 9, 0, 2, 3, 11},
            new int[] {1, 11, 2, 1, 9, 11, 9, 8, 11},
            new int[] {3, 10, 1, 11, 10, 3},
            new int[] {0, 10, 1, 0, 8, 10, 8, 11, 10},
            new int[] {3, 9, 0, 3, 11, 9, 11, 10, 9},
            new int[] {9, 8, 10, 10, 8, 11},
            new int[] {4, 7, 8},
            new int[] {4, 3, 0, 7, 3, 4},
            new int[] {0, 1, 9, 8, 4, 7},
            new int[] {4, 1, 9, 4, 7, 1, 7, 3, 1},
            new int[] {1, 2, 10, 8, 4, 7},
            new int[] {3, 4, 7, 3, 0, 4, 1, 2, 10},
            new int[] {9, 2, 10, 9, 0, 2, 8, 4, 7},
            new int[] {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4},
            new int[] {8, 4, 7, 3, 11, 2},
            new int[] {11, 4, 7, 11, 2, 4, 2, 0, 4},
            new int[] {9, 0, 1, 8, 4, 7, 2, 3, 11},
            new int[] {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1},
            new int[] {3, 10, 1, 3, 11, 10, 7, 8, 4},
            new int[] {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4},
            new int[] {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3},
            new int[] {4, 7, 11, 4, 11, 9, 9, 11, 10},
            new int[] {9, 5, 4},
            new int[] {9, 5, 4, 0, 8, 3},
            new int[] {0, 5, 4, 1, 5, 0},
            new int[] {8, 5, 4, 8, 3, 5, 3, 1, 5},
            new int[] {1, 2, 10, 9, 5, 4},
            new int[] {3, 0, 8, 1, 2, 10, 4, 9, 5},
            new int[] {5, 2, 10, 5, 4, 2, 4, 0, 2},
            new int[] {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8},
            new int[] {9, 5, 4, 2, 3, 11},
            new int[] {0, 11, 2, 0, 8, 11, 4, 9, 5},
            new int[] {0, 5, 4, 0, 1, 5, 2, 3, 11},
            new int[] {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5},
            new int[] {10, 3, 11, 10, 1, 3, 9, 5, 4},
            new int[] {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10},
            new int[] {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3},
            new int[] {5, 4, 8, 5, 8, 10, 10, 8, 11},
            new int[] {9, 7, 8, 5, 7, 9},
            new int[] {9, 3, 0, 9, 5, 3, 5, 7, 3},
            new int[] {0, 7, 8, 0, 1, 7, 1, 5, 7},
            new int[] {1, 5, 3, 3, 5, 7},
            new int[] {9, 7, 8, 9, 5, 7, 10, 1, 2},
            new int[] {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3},
            new int[] {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2},
            new int[] {2, 10, 5, 2, 5, 3, 3, 5, 7},
            new int[] {7, 9, 5, 7, 8, 9, 3, 11, 2},
            new int[] {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11},
            new int[] {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7},
            new int[] {11, 2, 1, 11, 1, 7, 7, 1, 5},
            new int[] {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11},
            new int[] {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0},
            new int[] {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0},
            new int[] {11, 10, 5, 7, 11, 5},
            new int[] {10, 6, 5},
            new int[] {0, 8, 3, 5, 10, 6},
            new int[] {9, 0, 1, 5, 10, 6},
            new int[] {1, 8, 3, 1, 9, 8, 5, 10, 6},
            new int[] {1, 6, 5, 2, 6, 1},
            new int[] {1, 6, 5, 1, 2, 6, 3, 0, 8},
            new int[] {9, 6, 5, 9, 0, 6, 0, 2, 6},
            new int[] {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8},
            new int[] {2, 3, 11, 10, 6, 5},
            new int[] {11, 0, 8, 11, 2, 0, 10, 6, 5},
            new int[] {0, 1, 9, 2, 3, 11, 5, 10, 6},
            new int[] {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11},
            new int[] {6, 3, 11, 6, 5, 3, 5, 1, 3},
            new int[] {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6},
            new int[] {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9},
            new int[] {6, 5, 9, 6, 9, 11, 11, 9, 8},
            new int[] {5, 10, 6, 4, 7, 8},
            new int[] {4, 3, 0, 4, 7, 3, 6, 5, 10},
            new int[] {1, 9, 0, 5, 10, 6, 8, 4, 7},
            new int[] {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4},
            new int[] {6, 1, 2, 6, 5, 1, 4, 7, 8},
            new int[] {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7},
            new int[] {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6},
            new int[] {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9},
            new int[] {3, 11, 2, 7, 8, 4, 10, 6, 5},
            new int[] {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11},
            new int[] {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6},
            new int[] {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6},
            new int[] {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6},
            new int[] {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11},
            new int[] {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7},
            new int[] {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9},
            new int[] {10, 4, 9, 6, 4, 10},
            new int[] {4, 10, 6, 4, 9, 10, 0, 8, 3},
            new int[] {10, 0, 1, 10, 6, 0, 6, 4, 0},
            new int[] {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10},
            new int[] {1, 4, 9, 1, 2, 4, 2, 6, 4},
            new int[] {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4},
            new int[] {0, 2, 4, 4, 2, 6},
            new int[] {8, 3, 2, 8, 2, 4, 4, 2, 6},
            new int[] {10, 4, 9, 10, 6, 4, 11, 2, 3},
            new int[] {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6},
            new int[] {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10},
            new int[] {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1},
            new int[] {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3},
            new int[] {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1},
            new int[] {3, 11, 6, 3, 6, 0, 0, 6, 4},
            new int[] {6, 4, 8, 11, 6, 8},
            new int[] {7, 10, 6, 7, 8, 10, 8, 9, 10},
            new int[] {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10},
            new int[] {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0},
            new int[] {10, 6, 7, 10, 7, 1, 1, 7, 3},
            new int[] {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7},
            new int[] {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9},
            new int[] {7, 8, 0, 7, 0, 6, 6, 0, 2},
            new int[] {7, 3, 2, 6, 7, 2},
            new int[] {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7},
            new int[] {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7},
            new int[] {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11},
            new int[] {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1},
            new int[] {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6},
            new int[] {0, 9, 1, 11, 6, 7},
            new int[] {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0},
            new int[] {7, 11, 6},
            new int[] {7, 6, 11},
            new int[] {3, 0, 8, 11, 7, 6},
            new int[] {0, 1, 9, 11, 7, 6},
            new int[] {8, 1, 9, 8, 3, 1, 11, 7, 6},
            new int[] {10, 1, 2, 6, 11, 7},
            new int[] {1, 2, 10, 3, 0, 8, 6, 11, 7},
            new int[] {2, 9, 0, 2, 10, 9, 6, 11, 7},
            new int[] {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8},
            new int[] {7, 2, 3, 6, 2, 7},
            new int[] {7, 0, 8, 7, 6, 0, 6, 2, 0},
            new int[] {2, 7, 6, 2, 3, 7, 0, 1, 9},
            new int[] {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6},
            new int[] {10, 7, 6, 10, 1, 7, 1, 3, 7},
            new int[] {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8},
            new int[] {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7},
            new int[] {7, 6, 10, 7, 10, 8, 8, 10, 9},
            new int[] {6, 8, 4, 11, 8, 6},
            new int[] {3, 6, 11, 3, 0, 6, 0, 4, 6},
            new int[] {8, 6, 11, 8, 4, 6, 9, 0, 1},
            new int[] {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6},
            new int[] {6, 8, 4, 6, 11, 8, 2, 10, 1},
            new int[] {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6},
            new int[] {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9},
            new int[] {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3},
            new int[] {8, 2, 3, 8, 4, 2, 4, 6, 2},
            new int[] {0, 4, 2, 4, 6, 2},
            new int[] {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8},
            new int[] {1, 9, 4, 1, 4, 2, 2, 4, 6},
            new int[] {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1},
            new int[] {10, 1, 0, 10, 0, 6, 6, 0, 4},
            new int[] {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3},
            new int[] {10, 9, 4, 6, 10, 4},
            new int[] {4, 9, 5, 7, 6, 11},
            new int[] {0, 8, 3, 4, 9, 5, 11, 7, 6},
            new int[] {5, 0, 1, 5, 4, 0, 7, 6, 11},
            new int[] {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5},
            new int[] {9, 5, 4, 10, 1, 2, 7, 6, 11},
            new int[] {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5},
            new int[] {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2},
            new int[] {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6},
            new int[] {7, 2, 3, 7, 6, 2, 5, 4, 9},
            new int[] {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7},
            new int[] {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0},
            new int[] {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8},
            new int[] {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7},
            new int[] {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4},
            new int[] {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10},
            new int[] {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10},
            new int[] {6, 9, 5, 6, 11, 9, 11, 8, 9},
            new int[] {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5},
            new int[] {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11},
            new int[] {6, 11, 3, 6, 3, 5, 5, 3, 1},
            new int[] {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6},
            new int[] {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10},
            new int[] {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5},
            new int[] {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3},
            new int[] {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2},
            new int[] {9, 5, 6, 9, 6, 0, 0, 6, 2},
            new int[] {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8},
            new int[] {1, 5, 6, 2, 1, 6},
            new int[] {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6},
            new int[] {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0},
            new int[] {0, 3, 8, 5, 6, 10},
            new int[] {10, 5, 6},
            new int[] {11, 5, 10, 7, 5, 11},
            new int[] {11, 5, 10, 11, 7, 5, 8, 3, 0},
            new int[] {5, 11, 7, 5, 10, 11, 1, 9, 0},
            new int[] {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1},
            new int[] {11, 1, 2, 11, 7, 1, 7, 5, 1},
            new int[] {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11},
            new int[] {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7},
            new int[] {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2},
            new int[] {2, 5, 10, 2, 3, 5, 3, 7, 5},
            new int[] {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5},
            new int[] {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2},
            new int[] {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2},
            new int[] {1, 3, 5, 3, 7, 5},
            new int[] {0, 8, 7, 0, 7, 1, 1, 7, 5},
            new int[] {9, 0, 3, 9, 3, 5, 5, 3, 7},
            new int[] {9, 8, 7, 5, 9, 7},
            new int[] {5, 8, 4, 5, 10, 8, 10, 11, 8},
            new int[] {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0},
            new int[] {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5},
            new int[] {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4},
            new int[] {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8},
            new int[] {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11},
            new int[] {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5},
            new int[] {9, 4, 5, 2, 11, 3},
            new int[] {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4},
            new int[] {5, 10, 2, 5, 2, 4, 4, 2, 0},
            new int[] {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9},
            new int[] {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2},
            new int[] {8, 4, 5, 8, 5, 3, 3, 5, 1},
            new int[] {0, 4, 5, 1, 0, 5},
            new int[] {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5},
            new int[] {9, 4, 5},
            new int[] {4, 11, 7, 4, 9, 11, 9, 10, 11},
            new int[] {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11},
            new int[] {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11},
            new int[] {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4},
            new int[] {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2},
            new int[] {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3},
            new int[] {11, 7, 4, 11, 4, 2, 2, 4, 0},
            new int[] {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4},
            new int[] {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9},
            new int[] {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7},
            new int[] {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10},
            new int[] {1, 10, 2, 8, 7, 4},
            new int[] {4, 9, 1, 4, 1, 7, 7, 1, 3},
            new int[] {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1},
            new int[] {4, 0, 3, 7, 4, 3},
            new int[] {4, 8, 7},
            new int[] {9, 10, 8, 10, 11, 8},
            new int[] {3, 0, 9, 3, 9, 11, 11, 9, 10},
            new int[] {0, 1, 10, 0, 10, 8, 8, 10, 11},
            new int[] {3, 1, 10, 11, 3, 10},
            new int[] {1, 2, 11, 1, 11, 9, 9, 11, 8},
            new int[] {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9},
            new int[] {0, 2, 11, 8, 0, 11},
            new int[] {3, 2, 11},
            new int[] {2, 3, 8, 2, 8, 10, 10, 8, 9},
            new int[] {9, 10, 2, 0, 9, 2},
            new int[] {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8},
            new int[] {1, 10, 2},
            new int[] {1, 3, 8, 9, 1, 8},
            new int[] {0, 9, 1},
            new int[] {0, 3, 8},
            new int[] {}
        };
    }
}
