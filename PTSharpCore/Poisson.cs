using System;
using System.Collections.Generic;
using System.Linq;

namespace PTSharpCore
{
    class Poisson
    {
        double r, size;
        Dictionary<Vector, Vector> cells;
        
        Poisson(double r, double size, Dictionary<Vector, Vector> hmap)
        {
            this.r = r;
            this.size = size;
            cells = hmap;
        }

        Poisson newPoissonGrid(double r)
        {
            double gridsize = r / Math.Sqrt(2);
            return new Poisson(r, gridsize, new Dictionary<Vector, Vector>());
        }
        
        Vector normalize(Vector v)
        {
            var i = Math.Floor(v.x / size);
            var j = Math.Floor(v.y / size);
            return new Vector(i, j, 0);
        }

        bool insert(Vector v)
        {
            Vector n = normalize(v);

            for (double i = n.x - 2; i < n.x + 3; i++)
            {
                for (double j = n.y - 2; j < n.y + 3; j++)
                {
                    if(cells.ContainsKey(new Vector(i, j, 0)))
                    {
                        Vector m = cells[new Vector(i, j, 0)];

                        if(Math.Sqrt(Math.Pow(m.x-v.x, 2) + Math.Pow(m.y-v.y, 2)) < r)
                        {
                            return false;
                        }
                    }
                }
            }
            cells[n] = v;
            return true;
        }
        
        Vector[] PoissonDisc(double x1, double y1, double x2, double y2, double r, int n)
        {
            Vector[] result;
            var x = x1 + (x2 - x1) / 2;
            var y = y1 + (y2 - y1) / 2;
            var v = new Vector(x, y, 0);
            var active = new Vector[] { v };
            var grid = newPoissonGrid(r);
            grid.insert(v);
            result = new Vector[]{v};
                        
            while (active.Length != 0)
            {
                // Need non-negative random integers
                // must be a non-negative pseudo-random number in [0,n).
                int index = Random.Shared.Next(active.Length);
                Vector point = active.ElementAt(index);
                bool ok = false;

                for (int i = 0; i < n; i++)
                {
                    double a = Random.Shared.NextDouble() * 2 * Math.PI;
                    double d = Random.Shared.NextDouble() * r + r;
                    x = point.x + Math.Cos(a) * d;
                    y = point.y + Math.Sin(a) * d;
                    if (x < x1 || y < y1 || x > x2 || y > y2)
                    {
                        continue;
                    }
                    v = new Vector(x, y, 0);
                    if (!grid.insert(v))
                    {
                        continue;
                    }
                    Array.Resize(ref result, result.GetLength(0) + 1);
                    result[result.GetLength(0) - 1] = v;

                    Array.Resize(ref active, active.GetLength(0) + 1);
                    active[active.GetLength(0) - 1] = v;

                    ok = true;
                    break;
                }
                
                if (!ok)
                {
                    Array.Resize(ref active, active.GetLength(0) + 1);
                    active[active.GetLength(0) - 1] = active[index+1];
                }
            }
            return result;
        }
    }
}
