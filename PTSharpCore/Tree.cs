using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace PTSharpCore
{
    public class Tree
    {

        internal Box Box;
        internal Node Root;

        public Tree() { }

        Tree(Box box, Node root)
        {
            Box = box;
            Root = root;
        }

        internal static Tree NewTree(IShape[] shapes)
        {
            Console.Out.WriteLine("Building k-d tree: " + shapes.Length);
            var box = Box.BoxForShapes(shapes);
            var node = Node.NewNode(shapes);
            node.Split(0);
            return new Tree(box, node);
        }

        internal Hit Intersect(Ray r)
        {
            double tmin;
            double tmax;

            (tmin, tmax) = Box.Intersect(r);

            if (tmax < tmin || tmax <= 0)
            {
                return Hit.NoHit;
            }
            return Root.Intersect(r, tmin, tmax);
        }

        public class Node
        {
            Axis Axis;
            double Point;
            IShape[] Shapes;
            Node Left;
            Node Right;

            public double tsplit;
            public bool leftFirst;

            internal Node(Axis axis, double point, IShape[] shapes, Node left, Node right)
            {
                Axis = axis;
                Point = point;
                Shapes = shapes;
                Left = left;
                Right = right;
            }

            internal static Node NewNode(IShape[] shapes)
            {
                return new Node(Axis.AxisNone, 0, shapes, null, null);
            }

            internal Hit Intersect(Ray r, double tmin, double tmax)
            {
                double tsplit;
                bool leftFirst;

                switch (Axis)
                {
                    case Axis.AxisNone:
                        return IntersectShapes(r);
                    case Axis.AxisX:
                        tsplit = (Point - r.Origin.X) / r.Direction.X;
                        leftFirst = (r.Origin.X < Point) || (r.Origin.X == Point && r.Direction.X <= 0);
                        break;
                    case Axis.AxisY:
                        tsplit = (Point - r.Origin.Y) / r.Direction.Y;
                        leftFirst = (r.Origin.Y < Point) || (r.Origin.Y == Point && r.Direction.Y <= 0);
                        break;
                    case Axis.AxisZ:
                        tsplit = (Point - r.Origin.Z) / r.Direction.Z;
                        leftFirst = (r.Origin.Z < Point) || (r.Origin.Z == Point && r.Direction.Z <= 0);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid Axis");
                }

                Node first = leftFirst ? Left : Right;
                Node second = leftFirst ? Right : Left;

                if (tsplit > tmax || tsplit <= 0)
                {
                    return first.Intersect(r, tmin, tmax);
                }
                else if (tsplit < tmin)
                {
                    return second.Intersect(r, tmin, tmax);
                }
                else
                {
                    Hit h1 = first.Intersect(r, tmin, tsplit);
                    if (h1.T <= tsplit)
                    {
                        return h1;
                    }
                    Hit h2 = second.Intersect(r, tsplit, Math.Min(tmax, h1.T));
                    return h1.T <= h2.T ? h1 : h2;
                }
            }        

            internal Hit IntersectShapes(Ray r)
            {
                Hit hit = Hit.NoHit;

                foreach (var shape in Shapes)
                {
                    Hit h = shape.Intersect(r);
                    if (h.T < hit.T)
                    {
                        hit = h;
                    }
                }
                return hit;
            }

            public static double Median(ConcurrentBag<double> list)
            {
                int middle = list.Count / 2;

                if (list.Count == 0)
                {
                    return 0;
                }
                else if (list.Count % 2 == 1)
                {
                    return list.ElementAt(middle);
                }
                else
                {
                    var a = list.ElementAt(list.Count / 2 - 1);
                    var b = list.ElementAt(list.Count / 2);
                    return (a + b) / 2;
                }
            }

            public int PartitionScore(Axis axis, double point)
            {
                (int left, int right) = (0, 0);
                foreach (var box in from shape in Shapes
                                    let box = shape.BoundingBox()
                                    select box)
                {
                    (bool l, bool r) = box.Partition(axis, point);
                    if (l)
                    {
                        Interlocked.Increment(ref left);
                    }

                    if (r)
                    {
                        Interlocked.Increment(ref right);
                    }
                }

                if (left >= right)
                {
                    return left;
                }
                else
                {
                    return right;
                }
            }

            (IShape[], IShape[]) Partition(int size, Axis axis, double point)
            {
                ConcurrentBag<IShape> left = new();
                ConcurrentBag<IShape> right = new();

                foreach (var shape in Shapes)
                {
                    var box = shape.BoundingBox();
                    (bool l, bool r) = box.Partition(axis, point);
                    if (l is true)
                    {
                        // Append left 
                        left.Add(shape);
                    }
                    if (r is true)
                    {
                        // Append right 
                        right.Add(shape);
                    }
                }

                return (left.ToArray(), right.ToArray());
            }

            public void Split(int depth)
            {
                if (Shapes.Length < 8)
                {
                    return;
                }

                ConcurrentBag<double> xs = new();
                ConcurrentBag<double> ys = new();
                ConcurrentBag<double> zs = new();

                foreach (var shape in Shapes)
                {
                    Box box = shape.BoundingBox();
                    xs.Add(box.Min.X);
                    xs.Add(box.Max.X);
                    ys.Add(box.Min.Y);
                    ys.Add(box.Max.Y);
                    zs.Add(box.Min.Z);
                    zs.Add(box.Max.Z);
                }

                xs.AsParallel().AsOrdered();
                ys.AsParallel().AsOrdered();
                zs.AsParallel().AsOrdered();

                (double mx, double my, double mz) = (Median(xs), Median(ys), Median(zs));

                var best = (int)(Shapes.Length * 0.85);
                var bestAxis = Axis.AxisNone;
                var bestPoint = 0.0;

                var sx = PartitionScore(Axis.AxisX, mx);
                if (sx < best)
                {
                    best = sx;
                    bestAxis = Axis.AxisX;
                    bestPoint = mx;
                }

                var sy = PartitionScore(Axis.AxisY, my);
                if (sy < best)
                {
                    best = sy;
                    bestAxis = Axis.AxisY;
                    bestPoint = my;
                }

                var sz = PartitionScore(Axis.AxisZ, mz);
                if (sz < best)
                {
                    best = sz;
                    bestAxis = Axis.AxisZ;
                    bestPoint = mz;
                }

                if (bestAxis == Axis.AxisNone)
                {
                    return;
                }

                (var l, var r) = Partition(best, bestAxis, bestPoint);
                Axis = bestAxis;
                Point = bestPoint;

                (Left, Right) = (NewNode(l), NewNode(r));
                Left.Split(depth + 1);
                Right.Split(depth + 1);
                Shapes = null; // only needed at leaf nodes
            }
        }
    }
}
