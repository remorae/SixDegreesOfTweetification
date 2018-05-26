using System;
using System.Collections.Generic;

namespace SixDegrees.Model
{
    /// <summary>
    /// A connection for finding entities connected to a single starting point.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleConnection<T>
    {
        public int DistanceFromStart { get; }
        public ISet<T> Connections { get; }

        public SingleConnection(int distance)
        {
            DistanceFromStart = distance;
            Connections = new HashSet<T>();
        }
    }

    /// <summary>
    /// A connection between two entities used for finding paths between them.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Connection<T>
    {
        /// <summary>
        /// An endpoint of a two-way connection.
        /// </summary>
        public class Node
        {
            /// <summary>
            /// Compares node values by their respective Equals method rather than by reference.
            /// </summary>
            public class EqualityComparer : EqualityComparer<Node>
            {
                public override bool Equals(Node x, Node y)
                {
                    return x.Value.Equals(y.Value);
                }

                public override int GetHashCode(Node obj)
                {
                    return obj.Value.GetHashCode();
                }
            }

            private const float DegreeSearchDecayFactor = -2;

            private int previousMaxDepth = -1;
            private double storedHeuristic = -1;

            public Node(T value, int distance)
            {
                Value = value;
                Distance = distance;
            }

            public T Value { get; }
            public int Distance { get; set; }

            public double Heuristic(int maxDepth)
            {
                if (maxDepth != previousMaxDepth)
                {
                    previousMaxDepth = maxDepth;
                    double sum = 0;
                    for (int i = 1; i <= maxDepth; ++i)
                        sum += Math.Pow(Math.E, DegreeSearchDecayFactor * i);
                    storedHeuristic = Math.Pow(Math.E, DegreeSearchDecayFactor * Distance) / sum;
                }
                return storedHeuristic;
            }
        }

        public Connection()
        {
            Connections = new Dictionary<Node, int>();
        }

        public IDictionary<Node, int> Connections { get; }
    }
}