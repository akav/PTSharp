using System;
using System.Collections.Generic;
using System.Linq;

namespace PTSharpCore
{
    class Poisson
    {
        double r, size;
        Dictionary<IVector<double>, IVector<double>> cells;
        
        Poisson(double r, double size, Dictionary<IVector<double>, IVector<double>> hmap)
        {
            this.r = r;
            this.size = size;
            cells = hmap;
        }

        Poisson newPoissonGrid(double r)
        {
            double gridsize = r / Math.Sqrt(2);
            return new Poisson(r, gridsize, new Dictionary<IVector<double>, IVector<double>>());
        }

        IVector<double> normalize(IVector<double> v)
        {
            var i = Math.Floor(v.dv[0] / size);
            var j = Math.Floor(v.dv[1] / size);
            return new IVector<double>(new double[] { i, j, 0, 0 });
        }

        bool insert(IVector<double> v)
        {
            IVector<double> n = normalize(v);

            for (double i = n.dv[0] - 2; i < n.dv[0] + 3; i++)
            {
                for (double j = n.dv[1] - 2; j < n.dv[1] + 3; j++)
                {
                    if(cells.ContainsKey(new IVector<double>(new double[] { i, j, 0, 0 })))
                    {
                        IVector<double> m = cells[new IVector<double>(new double[] { i, j, 0, 0 })];

                        if(Math.Sqrt(Math.Pow(m.dv[0] - v.dv[0], 2) + Math.Pow(m.dv[1] - v.dv[1], 2)) < r)
                        {
                            return false;
                        }
                    }
                }
            }
            cells[n] = v;
            return true;
        }

        IVector<double>[] PoissonDisc(double x1, double y1, double x2, double y2, double r, int n)
        {
            IVector<double>[] result;
            var x = x1 + (x2 - x1) / 2;
            var y = y1 + (y2 - y1) / 2;
            var v = new IVector<double>(new double[] { x, y, 0, 0 });
            var active = new IVector<double>[] { v };
            var grid = newPoissonGrid(r);
            grid.insert(v);
            result = new IVector<double>[]{v};
                        
            while (active.Length != 0)
            {
                // Need non-negative random integers
                // must be a non-negative pseudo-random number in [0,n).
                int index = Random.Shared.Next(active.Length);
                IVector<double> point = active.ElementAt(index);
                bool ok = false;

                for (int i = 0; i < n; i++)
                {
                    double a = Random.Shared.NextDouble() * 2 * Math.PI;
                    double d = Random.Shared.NextDouble() * r + r;
                    x = point.dv[0] + Math.Cos(a) * d;
                    y = point.dv[1] + Math.Sin(a) * d;
                    if (x < x1 || y < y1 || x > x2 || y > y2)
                    {
                        continue;
                    }
                    v = new IVector<double>(new double[] { x, y, 0, 0 });
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
