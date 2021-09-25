using PTSharpCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            
            (double tmin, double tmax) = Box.Intersect(r);
            
            if (tmax < tmin || tmax <= 0) {
                return Hit.NoHit;
            }
            return Root.Intersect(r, tmin, tmax);
        }

        public class Node {
            Axis Axis;
            double Point;
            IShape[] Shapes;
            Node Left;
            Node Right;

            public double tsplit;
            public bool leftFirst;

            internal Node(Axis axis, double point, IShape[] shapes, Node left, Node right) {
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
                switch (Axis)
                {
                    case Axis.AxisNone:
                        return IntersectShapes(r);
                    case Axis.AxisX:
                        tsplit = (Point - r.Origin.x) / r.Direction.x;
                        leftFirst = (r.Origin.x < Point) || (r.Origin.x == Point && r.Direction.x <= 0);
                        break;
                    case Axis.AxisY:
                        tsplit = (Point - r.Origin.y) / r.Direction.y;
                        leftFirst = (r.Origin.y < Point) || (r.Origin.y == Point && r.Direction.y <= 0);
                        break;
                    case Axis.AxisZ:
                        tsplit = (Point - r.Origin.z) / r.Direction.z;
                        leftFirst = (r.Origin.z < Point) || (r.Origin.z == Point && r.Direction.z <= 0);
                        break;
                }

                Node first, second; 

                if(leftFirst)
                {
                    first = Left;
                    second = Right;
                }
                else
                {
                    first = Right;
                    second = Left;
                }

                if (tsplit > tmax || tsplit <= 0)
                {
                    return first.Intersect(r, tmin, tmax);
                }
                else if(tsplit < tmin) {
                    return second.Intersect(r, tmin, tmax);
                }
                else
                {
                    var h1 = first.Intersect(r, tmin, tsplit);
          
                    if(h1.T <= tsplit)
                    {
                        return h1;
                    }

                    var h2 = second.Intersect(r, tsplit, Math.Min(tmax, h1.T));
          
                    if(h1.T <= h2.T)
                    {
                        return h1;

                    }
                    else
                    {
                        return h2;
                    }
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

            public double Median(List<Double> list)
            {
                int middle = list.Count() / 2;

                if (list.Count == 0)
                {
                    return 0;
                }
                else if (list.Count() % 2 == 1)
                {
                    return list.ElementAt(middle);
                }
                else
                {
                    var a = list.ElementAt(list.Count() / 2 - 1);
                    var b = list.ElementAt(list.Count() / 2);
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
                        left++;
                    }

                    if (r)
                    {
                        right++;
                    }
                }

                if (left >= right) {
                    return left;
                }
                else {
                    return right;
                }
            }

            (IShape[], IShape[]) Partition(int size, Axis axis, double point)
            {
                List<IShape> left = new List<IShape>();
                List<IShape> right = new List<IShape>();

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

                List<double> xs = new List<double>();
                List<double> ys = new List<double>();
                List<double> zs = new List<double>();

                foreach (var shape in Shapes) {
                    Box box = shape.BoundingBox();
                    xs.Add(box.Min.x); 
                    xs.Add(box.Max.x); 
                    ys.Add(box.Min.y); 
                    ys.Add(box.Max.y); 
                    zs.Add(box.Min.z); 
                    zs.Add(box.Max.z); 
                }

                xs.Sort();
                ys.Sort();
                zs.Sort();

                (double mx, double my, double mz) = (Median(xs), Median(ys), Median(zs));
                var best = (int)(Shapes.Length * 0.85);
                var bestAxis = Axis.AxisNone;
                var bestPoint = 0.0;

                var sx = PartitionScore(Axis.AxisX, mx);
                if (sx < best) {
                    best = sx;
                    bestAxis = Axis.AxisX;
                    bestPoint = mx;
                }

                var sy = PartitionScore(Axis.AxisY, my);
                if (sy < best) {
                    best = sy;
                    bestAxis = Axis.AxisY;
                    bestPoint = my;
                }

                var sz = PartitionScore(Axis.AxisZ, mz);
                if (sz < best) {
                    best = sz;
                    bestAxis = Axis.AxisZ;
                    bestPoint = mz;
                }

                if (bestAxis == Axis.AxisNone) {
                    return;
                }

                (var l, var r) = Partition(best, bestAxis, bestPoint);
                Axis = bestAxis;
                Point = bestPoint;
                Left = NewNode(l);
                Right = NewNode(r);
                Left.Split(depth + 1);
                Right.Split(depth + 1);
                Shapes = null; // only needed at leaf nodes
            }
        }
    }
}
