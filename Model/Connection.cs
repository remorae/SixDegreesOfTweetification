using System;
using System.Collections.Generic;
using System.Linq;

namespace SixDegrees.Model
{
    public class HashtagConnectionInfo
    {
        public int Distance { get; }
        public ISet<string> Connections { get; }

        public HashtagConnectionInfo(int distance)
        {
            Distance = distance;
            Connections = new HashSet<string>();
        }
    }

    public class UserConnectionInfo
    {
        public int Distance { get; }
        public ISet<UserResult> Connections { get; }

        public UserConnectionInfo(int distance)
        {
            Distance = distance;
            Connections = new HashSet<UserResult>();
        }
    }

    public class ConnectionInfo<T>
    {
        public class Node
        {
            private const float DegreeSearchDecayFactor = -2;

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

            public Node(T value, int distance)
            {
                Value = value;
                Distance = distance;
            }

            public T Value { get; }
            public int Distance { get; set; }
            private int previousMaxDepth = -1;
            private double storedHeuristic = -1;
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
        
        public IDictionary<Node, int> Connections { get; }

        public ConnectionInfo()
        {
            Connections = new Dictionary<Node, int>();
        }

        internal static List<Node> ShortestPath(IDictionary<Node, ConnectionInfo<T>> vertices, Node start, Node end)
        {
            var previous = new Dictionary<Node, Node>(new Node.EqualityComparer());
            var distances = new Dictionary<Node, int>(new Node.EqualityComparer());
            var nodes = new List<Node>();

            List<Node> path = null;

            foreach (var vertex in vertices)
            {
                distances[vertex.Key] = (vertex.Key.Value.Equals(start.Value) ? 0 : int.MaxValue);
                if (vertex.Value.Connections.Count > 0 || vertex.Key.Value.Equals(start.Value) || vertex.Key.Value.Equals(end.Value))
                    nodes.Add(vertex.Key);
            }

            while (nodes.Count > 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);
                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest.Value.Equals(end.Value))
                {
                    path = new List<Node>();
                    while (previous.ContainsKey(smallest) && !smallest.Value.Equals(start.Value))
                    {
                        path.Add(new Node(smallest.Value, distances[smallest]));
                        smallest = previous[smallest];
                    }
                    if (path.Count > 0)
                        path.Add(new Node(start.Value, 0));
                    path.Reverse();
                    break;
                }

                if (distances[smallest] == int.MaxValue)
                    break;
                
                foreach (var neighbor in vertices[smallest].Connections)
                {
                    var distanceFromShortestPath = distances[smallest] + neighbor.Value;
                    if (!distances.ContainsKey(neighbor.Key))
                        distances[neighbor.Key] = int.MaxValue;
                    if (distanceFromShortestPath < distances[neighbor.Key])
                    {
                        distances[neighbor.Key] = distanceFromShortestPath;
                        previous[neighbor.Key] = smallest;
                    }
                    else if (!previous.ContainsKey(neighbor.Key))
                        previous[neighbor.Key] = smallest;
                }
            }

            return path;
        }
    }
}