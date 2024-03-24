using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTSharpCore
{
    public class Tree
    {
        public Box Box { get; }
        public Node Root { get; }

        public Tree(Box box, Node root)
        {
            Box = box;
            Root = root;
        }

        public static Tree NewTree(IShape[] shapes)
        {
            Console.Out.WriteLine("Building k-d tree: " + shapes.Length);
            var box = Box.BoxForShapes(shapes);
            var node = Node.NewNode(shapes.ToList());
            node.Split(0);
            return new Tree(box, node);
        }

        public Hit Intersect(Ray r)
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
            public Axis Axis { get; private set; }
            public double Point { get; private set; }
            public List<IShape> Shapes { get; private set; }
            public Node Left { get; private set; }
            public Node Right { get; private set; }

            public double TSplit { get; private set; }
            public bool LeftFirst { get; private set; }

            private Node(Axis axis, double point, List<IShape> shapes, Node left, Node right)
            {
                Axis = axis;
                Point = point;
                Shapes = shapes;
                Left = left;
                Right = right;
            }

            public static Node NewNode(List<IShape> shapes)
            {
                return new Node(Axis.AxisNone, 0, shapes, null, null);
            }

            public Hit Intersect(Ray r, double tmin, double tmax)
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

            private Hit IntersectShapes(Ray r)
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

            
            public static double Median(IEnumerable<double> list)
            {
                var sortedList = list.OrderBy(x => x).ToList();
                int middle = sortedList.Count / 2;

                if (sortedList.Count == 0)
                {
                    return 0;
                }
                else if (sortedList.Count % 2 == 1)
                {
                    return sortedList[middle];
                }
                else
                {
                    var a = sortedList[middle - 1];
                    var b = sortedList[middle];
                    return (a + b) / 2;
                }
            }

            public int PartitionScore(Axis axis, double point)
            {
                int left = 0;
                int right = 0;

                foreach (var shape in Shapes)
                {
                    var box = shape.BoundingBox();
                    (bool l, bool r) = box.Partition(axis, point);
                    if (l)
                    {
                        left++;
                    }

                    if (r)
                    {
                        right++;
                    }
                }

                return Math.Max(left, right);
            }

            (IShape[], IShape[]) Partition(int size, Axis axis, double point)
            {
                var left = new ConcurrentBag<IShape>();
                var right = new ConcurrentBag<IShape>();

                // Use PLINQ to parallelize the loop
                Shapes.AsParallel().ForAll(shape =>
                {
                    var box = shape.BoundingBox();
                    (bool l, bool r) = box.Partition(axis, point);
                    if (l)
                    {
                        left.Add(shape);
                    }
                    if (r)
                    {
                        right.Add(shape);
                    }
                });

                return (left.ToArray(), right.ToArray());
            }

            public void Split(int depth)
            {
                if (Shapes.Count < 8)
                {
                    return;
                }

                List<double> xs = new();
                List<double> ys = new();
                List<double> zs = new();

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

                xs.Sort();
                ys.Sort();
                zs.Sort();

                double mx = Node.Median(xs);
                double my = Node.Median(ys);
                double mz = Node.Median(zs);

                var best = (int)(Shapes.Count * 0.85);
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

                Left = new Node(Axis.AxisNone, 0, l.ToList(), null, null);
                Right = new Node(Axis.AxisNone, 0, r.ToList(), null, null);
                Left.Split(depth + 1);
                Right.Split(depth + 1);
                Shapes = null; // only needed at leaf nodes
            }
        }
    }
}
