using System;
using System.Collections.Generic;
using System.Linq;

namespace Graph
{
    // Represents a undirected graph. At most one edge between two nodes is permitted.
// TODO Move to another assembly so that I can put addEdge method internal
    public class Graph
    {
        private readonly Dictionary<long, Edge> edges =
            new Dictionary<long, Edge>();

        private readonly Dictionary<int, Node> nodes = new Dictionary<int, Node>();

        /// <summary>
        ///     Adds a node with given id to the graph, if one isn't already present
        /// </summary>
        /// <param name="id">The unique id of the node</param>
        /// <param name="attributes">The list of attributes of the node</param>
        /// <returns>True if the node was added successfully</returns>
        public bool AddNode(int id, params Tuple<string, object>[] attributes)
        {
            if (nodes.ContainsKey(id)) return false;
            var attribDictionary = attributes.ToDictionary(key => key.Item1, value => value.Item2);
            nodes.Add(id, new Node(id, attribDictionary));
            return true;
        }

        /// <summary>
        ///     Adds an edge between the given nodeAID and nodeB, if they both exist
        /// </summary>
        /// <param name="nodeAID">The unique id of first node to link</param>
        /// <param name="nodeBID">The unique id of the second node to link</param>
        /// <param name="weight">The weight of the edge</param>
        /// <param name="attributes">The list of attributes of the edge to add</param>
        /// <returns>True if the edge was added successfully</returns>
        public bool AddEdge(int nodeAID, int nodeBID, float weight = 1f, params Tuple<string, object>[] attributes)
        {
            if (!nodes.ContainsKey(nodeAID) || !nodes.ContainsKey(nodeBID)) return false;
            var id = EdgeUtils.GetEdgeKey(nodeAID, nodeBID);
            if (edges.ContainsKey(id)) return false;
            var attribList = attributes.ToList();
            var newEdge = new Edge(nodeAID, nodeBID, weight, attribList);
            edges.Add(id, newEdge);
            nodes[nodeAID].AddEdge(newEdge);
            nodes[nodeBID].AddEdge(newEdge);
            return true;
        }

        public bool HasNode(int id)
        {
            return nodes.ContainsKey(id);
        }

        public Node GetNode(int id)
        {
            return nodes[id];
        }

        public Node[] GetNodes()
        {
            return nodes.Select(it => it.Value).ToArray();
        }

        public int[] GetNodesIDs()
        {
            return nodes.Select(it => it.Key).ToArray();
        }

        public bool HasEdge(int node1ID, int node2ID)
        {
            return edges.ContainsKey(EdgeUtils.GetEdgeKey(node1ID, node2ID));
        }

        public Edge GetEdge(int id1, int id2)
        {
            return edges[EdgeUtils.GetEdgeKey(id1, id2)];
        }

        public Edge[] GetEdgesFromNode(int nodeID)
        {
            return !nodes.ContainsKey(nodeID) ? Array.Empty<Edge>() : nodes[nodeID].edges;
        }

        public readonly struct Node
        {
            public readonly int id;
            private readonly List<Edge> connectedEdges;
            private readonly Dictionary<string, object> properties;

            public Node(int id, Dictionary<string, object> properties)
            {
                this.id = id;
                connectedEdges = new List<Edge>();
                this.properties = properties;
            }

            public void AddEdge(Edge edge)
            {
                connectedEdges.Add(edge);
            }


            public object this[string key]
            {
                get => properties.TryGetValue(key, out var rtn) ? rtn : null;
                set => properties[key] = value;
            }

            public Edge[] edges => connectedEdges.ToArray();
        }

        public readonly struct Edge
        {
            public Edge(int node1, int node2, float weight, List<Tuple<string, object>> properties)
            {
                this.node1 = node1;
                this.node2 = node2;
                this.weight = weight;
                this.properties = properties;
            }

            public readonly int node1;
            public readonly int node2;
            public readonly float weight;
            public readonly List<Tuple<string, object>> properties;

            public float EdgeID => EdgeUtils.GetEdgeKey(node1, node2);
        }
    }

    internal static class EdgeUtils
    {
        public static long GetEdgeKey(int a, int b)
        {
            return a > b ? ((long) a << 32) + b : ((long) b << 32) + a;
        }
    }
}