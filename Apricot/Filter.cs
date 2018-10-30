using System;
using System.Collections.Generic;
using System.Linq;

namespace Apricot
{
    public class Filter
    {
        private Random random = null;
        private List<double[]> inputVectorList = null;
        private List<double[]> outputVectorList = null;
        private int maxIterations = 10000;
        private int iterations = 0;
        private int width = 10;
        private int height = 10;
        private Dictionary<String, int> labelDictionary = null;
        private Node<double> rootNode = null;
        private Dictionary<Node<double>, string> nodeDictionary = null;

        public int MaxIterations
        {
            get
            {
                return this.maxIterations;
            }
            set
            {
                this.maxIterations = value;
            }
        }

        public int Width
        {
            get
            {
                return this.width;
            }
            set
            {
                this.width = value;
            }
        }

        public int Height
        {
            get
            {
                return this.height;
            }
            set
            {
                this.height = value;
            }
        }

        public Filter(int seed)
        {
            this.random = new Random(seed);
            this.inputVectorList = new List<double[]>();
            this.outputVectorList = new List<double[]>();
            this.labelDictionary = new Dictionary<string, int>();
            this.nodeDictionary = new Dictionary<Node<double>, string>();
        }

        public void Add(string label, double[] vector)
        {
            this.inputVectorList.Add(vector);
            this.labelDictionary.Add(label, this.inputVectorList.IndexOf(vector));
        }

        public void Remove(string label)
        {
            this.inputVectorList.RemoveAt(this.labelDictionary[label]);
            this.labelDictionary.Remove(label);
        }

        public void Reset()
        {
            int dimension = this.inputVectorList.Count > 0 ? this.inputVectorList[0].Length : 0;

            this.iterations = 0;
            this.outputVectorList.Clear();

            for (int i = 0; i < this.width * this.height; i++)
            {
                double[] vector = new double[dimension];

                for (int j = 0; j < dimension; j++)
                {
                    vector[j] = this.random.NextDouble();
                }

                this.outputVectorList.Add(vector);
            }
        }

        public void Train(int iterations)
        {
            /// Self-Organizing Map (SOM).
            /// T. Kohonen, Self-Organizing Maps, Berlin, Germany, 1995, Springer-Verlag.
            int t = 0;

            while (iterations > t++)
            {
                int index = this.random.Next(this.inputVectorList.Count);
                Nullable<int> winner = FindBestMatchingUnit(this.outputVectorList, this.inputVectorList[index]); // Use Winner-take-all model.

                if (winner.HasValue)
                {
                    int winnerN = winner.Value % this.width;
                    int winnerM = winner.Value / this.width;
                    int dimension = this.inputVectorList[index].Length;

                    for (int i = 0; i < this.outputVectorList.Count; i++)
                    {
                        double h = Neighborhood(i % this.width, i / this.width, winnerN, winnerM, this.width, this.height, this.iterations + t, this.maxIterations);

                        for (int j = 0; j < dimension; j++)
                        {
                            /// mi(t + 1) = mi(t) + hci(t)[x(t) - mi(t)]
                            this.outputVectorList[i][j] = this.outputVectorList[i][j] + h * (this.inputVectorList[index][j] - this.outputVectorList[i][j]);
                        }
                    }
                }
            }

            this.iterations += iterations;
        }

        public double GetDistance(string label1, string label2)
        {
            double[] label1Vector = this.inputVectorList[this.labelDictionary[label1]];
            double[] label2Vector = this.inputVectorList[this.labelDictionary[label2]];
            Nullable<int> index1 = FindBestMatchingUnit(this.outputVectorList, label1Vector);
            Nullable<int> index2 = FindBestMatchingUnit(this.outputVectorList, label2Vector);

            if (index1.HasValue && index2.HasValue)
            {
                int x1 = index1.Value % this.width;
                int y1 = index1.Value / this.width;
                int x2 = index2.Value % this.width;
                int y2 = index2.Value / this.width;

                return Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
            }

            return Double.NaN;
        }

        private Nullable<int> FindBestMatchingUnit(List<double[]> vectorList, double[] vector)
        {
            double lowestDistance = Double.MaxValue;
            Nullable<int> winner = null;

            for (int i = 0; i < vectorList.Count; i++)
            {
                double distance = EuclideanDistance(vectorList[i], vector);

                if (lowestDistance > distance)
                {
                    lowestDistance = distance;
                    winner = new Nullable<int>(i);
                }
            }

            return winner;
        }

        private double Neighborhood(int x1, int y1, int x2, int y2, int max_x, int max_y, int t, int t_max)
        {
            /// Neighborhood function:
            /// hci = alpha(t) * exp(-||rc - ri||^2 / 2siama^2(t))
            double r = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            double r_max = Math.Max(max_x, max_y) * (1 - (double)t / t_max);
            double alpha = 0.1 * Math.Exp(-(double)t / t_max);
            double sigma = r_max * Math.Exp(-(double)t / t_max);

            return alpha * Math.Exp(-(r * r) / (2 * (sigma * sigma)));
        }

        private double EuclideanDistance(double[] p, double[] q)
        {
            double distance = 0;

            if (p.Length != q.Length)
            {
                return Double.NaN;
            }

            for (int i = 0; i < p.Length; i++)
            {
                distance += (p[i] - q[i]) * (p[i] - q[i]);
            }

            return Math.Sqrt(distance);
        }

        public void Build()
        {
            List<Node<double>> nodeList = new List<Node<double>>();

            this.nodeDictionary.Clear();

            foreach (string label in this.labelDictionary.Keys)
            {
                Nullable<int> i = FindBestMatchingUnit(this.outputVectorList, this.inputVectorList[this.labelDictionary[label]]);

                if (i.HasValue)
                {
                    double[] vector = new double[2];
                    Node<double> node = new Node<double>();

                    vector[0] = i.Value % this.width;
                    vector[1] = i.Value / this.width;

                    node.Vector = vector;

                    nodeList.Add(node);
                    this.nodeDictionary.Add(node, label);
                }
            }

            // Build the K-d tree.
            this.rootNode = KdTree(nodeList, 0);
        }

        public IEnumerable<string> Query(string label, int width, int height)
        {
            int index;

            if (this.labelDictionary.TryGetValue(label, out index))
            {
                return Query(this.inputVectorList[index], width, height);
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> Query(double[] vector, int width, int height)
        {
            Nullable<int> i = FindBestMatchingUnit(this.outputVectorList, vector);

            if (i.HasValue)
            {
                return Query(i.Value % this.width - width / 2, i.Value / this.width - height / 2, width, height);
            }

            return Enumerable.Empty<string>();
        }

        public IEnumerable<string> Query(int x, int y, int width, int height)
        {
            if (this.rootNode != null)
            {
                double[] location = new double[2];
                double[] size = new double[2];

                location[0] = x;
                location[1] = y;
                size[0] = width;
                size[1] = height;

                foreach (Node<double> node in Query(this.rootNode, location, size, 0))
                {
                    string label;

                    if (this.nodeDictionary.TryGetValue(node, out label))
                    {
                        yield return label;
                    }
                }
            }
        }

        private IEnumerable<Node<double>> Query(Node<double> node, double[] location, double[] size, int depth)
        {
            /// Orthogonal range search in a K-d tree.
            bool withinRange = true;
            int k = node.Vector.Length;
            int axis = depth % k;

            for (int i = 0; i < node.Vector.Length; i++)
            {
                if (node.Vector[i] < location[i] || node.Vector[i] > location[i] + size[i])
                {
                    withinRange = false;

                    break;
                }
            }

            if (withinRange)
            {
                yield return node;
            }

            if (node.Vector[axis] >= location[axis] && node.LeftChild != null)
            {
                foreach (Node<double> n in Query(node.LeftChild, location, size, depth + 1))
                {
                    yield return n;
                }
            }

            if (node.Vector[axis] <= location[axis] + size[axis] && node.RightChild != null)
            {
                foreach (Node<double> n in Query(node.RightChild, location, size, depth + 1))
                {
                    yield return n;
                }
            }
        }

        private Node<double> KdTree(List<Node<double>> nodeList, int depth)
        {
            /// K-d tree (k-dimensional tree).
            if (nodeList.Count == 0)
            {
                return null;
            }

            int k = nodeList[0].Vector.Length;
            int axis = depth % k;

            nodeList.Sort(delegate (Node<double> n1, Node<double> n2)
            {
                if (n1.Vector[axis] > n2.Vector[axis])
                {
                    return 1;
                }
                else if (n1.Vector[axis] < n2.Vector[axis])
                {
                    return -1;
                }

                return 0;
            });

            int median = nodeList.Count / 2;
            List<Node<double>> leftNodeList = new List<Node<double>>();
            List<Node<double>> rightNodeList = new List<Node<double>>();

            for (int i = 0; i < median; i++)
            {
                leftNodeList.Add(nodeList[i]);
            }

            for (int i = median + 1; i < nodeList.Count; i++)
            {
                rightNodeList.Add(nodeList[i]);
            }

            Node<double> node = nodeList[median];

            node.LeftChild = KdTree(leftNodeList, depth + 1);
            node.RightChild = KdTree(rightNodeList, depth + 1);

            if (node.LeftChild != null)
            {
                node.LeftChild.Parent = node;
            }

            if (node.RightChild != null)
            {
                node.RightChild.Parent = node;
            }

            return node;
        }

        private class Node<T>
        {
            Node<T> parentNode = null;
            Node<T> leftChildNode = null;
            Node<T> rightChildNode = null;
            T[] vector = null;

            public Node<T> Parent
            {
                get
                {
                    return this.parentNode;
                }
                set
                {
                    this.parentNode = value;
                }
            }

            public Node<T> LeftChild
            {
                get
                {
                    return this.leftChildNode;
                }
                set
                {
                    this.leftChildNode = value;
                }
            }

            public Node<T> RightChild
            {
                get
                {
                    return this.rightChildNode;
                }
                set
                {
                    this.rightChildNode = value;
                }
            }

            public T[] Vector
            {
                get
                {
                    return this.vector;
                }
                set
                {
                    this.vector = value;
                }
            }
        }
    }
}
