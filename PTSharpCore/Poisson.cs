using System;
using System.Collections.Generic;
using System.Linq;

namespace PTSharpCore
{
    class Poisson
    {
        float r, size;
        Dictionary<V, V> cells;
        
        Poisson(float r, float size, Dictionary<V, V> hmap)
        {
            this.r = r;
            this.size = size;
            cells = hmap;
        }

        Poisson newPoissonGrid(float r)
        {
            float gridsize = r / MathF.Sqrt(2);
            return new Poisson(r, gridsize, new Dictionary<V, V>());
        }
        
        V normalize(V v)
        {
            var i = MathF.Floor(v.v.X / size);
            var j = MathF.Floor(v.v.Y / size);
            return new V(i, j, 0);
        }

        bool insert(V v)
        {
            V n = normalize(v);

            for (float i = n.v.X - 2; i < n.v.X + 3; i++)
            {
                for (float j = n.v.Y - 2; j < n.v.Y + 3; j++)
                {
                    if(cells.ContainsKey(new V(i, j, 0)))
                    {
                        V m = cells[new V(i, j, 0)];

                        if(MathF.Sqrt(MathF.Pow(m.v.X - v.v.X, 2) + MathF.Pow(m.v.Y - v.v.Y, 2)) < r)
                        {
                            return false;
                        }
                    }
                }
            }
            cells[n] = v;
            return true;
        }
        
        V[] PoissonDisc(float x1, float y1, float x2, float y2, float r, int n)
        {
            V[] result;
            var x = x1 + (x2 - x1) / 2;
            var y = y1 + (y2 - y1) / 2;
            var v = new V(x, y, 0);
            var active = new V[] { v };
            var grid = newPoissonGrid(r);
            grid.insert(v);
            result = new V[]{v};
                        
            while (active.Length != 0)
            {
                // Need non-negative random integers
                // must be a non-negative pseudo-random number in [0,n).
                int index = Random.Shared.Next(active.Length);
                V point = active.ElementAt(index);
                bool ok = false;

                for (int i = 0; i < n; i++)
                {
                    float a = Random.Shared.NextSingle() * 2 * MathF.PI;
                    float d = Random.Shared.NextSingle() * r + r;
                    x = point.v.X + MathF.Cos(a) * d;
                    y = point.v.Y + MathF.Sin(a) * d;
                    if (x < x1 || y < y1 || x > x2 || y > y2)
                    {
                        continue;
                    }
                    v = new V(x, y, 0);
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
