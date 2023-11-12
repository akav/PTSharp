using System;
using System.Collections.Generic;
using System.Linq;

namespace PTSharpCore
{
    class PoissonGrid
    {
        double r, size;
        Dictionary<Vector, Vector> cells;

        PoissonGrid(double r, double size, Dictionary<Vector, Vector> hmap)
        {
            this.r = r;
            this.size = size;
            cells = hmap;
        }

        PoissonGrid NewPoissonGrid(double r)
        {
            this.r = r;
            double gridsize = r / Math.Sqrt(2);
            return new PoissonGrid(r, gridsize, new Dictionary<Vector, Vector>());
        }

        Vector Normalize(Vector v)
        {
            var i = Math.Floor(v.X / size);
            var j = Math.Floor(v.Y / size);
            return new Vector(i, j, 0);
        }

        bool Insert(Vector v)
        {
            Vector n = Normalize(v);

            for (double i = n.X - 2; i < n.X + 3; i++)
            {
                for (double j = n.Y - 2; j < n.Y + 3; j++)
                {
                    if (cells.ContainsKey(new Vector(i, j, 0)))
                    {
                        Vector m = cells[new Vector(i, j, 0)];

                        if (Math.Sqrt(Math.Pow(m.X - v.X, 2) + Math.Pow(m.Y - v.Y, 2)) < r)
                        {
                            return false;
                        }
                    }
                }
            }
            cells[n] = v;
            return true;
        }

        List<Vector> PoissonDisc(double x1, double y1, double x2, double y2, double r, int n, Random rand)
        {
            List<Vector> result = new List<Vector>();
            double x = x1 + (x2 - x1) / 2;
            double y = y1 + (y2 - y1) / 2;
            Vector v = new Vector(x, y, 0);
            List<Vector> active = new List<Vector> { v };
            PoissonGrid grid = NewPoissonGrid(r);
            grid.Insert(v);
            result.Add(v);
            while (active.Count > 0)
            {
                int index = rand.Next(active.Count);
                Vector point = active[index];
                bool ok = false;
                for (int i = 0; i < n; i++)
                {
                    double a = rand.NextDouble() * 2 * Math.PI;
                    double d = rand.NextDouble() * r + r;
                    x = point.X + Math.Cos(a) * d;
                    y = point.Y + Math.Sin(a) * d;
                    if (x < x1 || y < y1 || x > x2 || y > y2) continue;
                    v = new Vector(x, y, 0);
                    if (!grid.Insert(v)) continue;
                    result.Add(v);
                    active.Add(v);
                    ok = true;
                    break;
                }
                if (!ok)
                {
                    active.RemoveAt(index);
                }
            }
            return result;
        }
    }
}
