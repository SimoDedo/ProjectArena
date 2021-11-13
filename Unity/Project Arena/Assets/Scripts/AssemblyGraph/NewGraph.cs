using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using QuikGraph.Algorithms.ShortestPath;

namespace AssemblyGraph
{
    public readonly struct NodeProperties
    {
        private readonly Dictionary<string, object> properties;

        public NodeProperties(Dictionary<string, object> properties)
        {
            this.properties = properties;
        }

        public object this[string key]
        {
            get => properties.TryGetValue(key, out var rtn) ? rtn : null;
            set => properties[key] = value;
        }
    }

    public class NewGraph
    {
        private readonly Dictionary<int, NodeProperties> nodeProperties = new Dictionary<int, NodeProperties>();

        private readonly UndirectedGraph<int, TaggedUndirectedEdge<int, float>> graph =
            new UndirectedGraph<int, TaggedUndirectedEdge<int, float>>();

        /// <summary>
        /// Adds a node with given id to the graph, if one isn't already present
        /// </summary>
        /// <param name="id">The unique id of the node</param>
        /// <param name="attributes">The list of attributes of the node</param>
        /// <returns>True if the node was added successfully</returns>
        public bool AddNode(int id, params Tuple<string, object>[] attributes)
        {
            if (graph.ContainsVertex(id)) return false;
            var attribDictionary = attributes.ToDictionary(key => key.Item1, value => value.Item2);
            graph.AddVertex(id);
            nodeProperties.Add(id, new NodeProperties(attribDictionary));
            return true;
        }

        /// <summary>
        /// Adds an edge between the given nodeAID and nodeB, if they both exist
        /// </summary>
        /// <param name="nodeAID">The unique id of first node to link</param>
        /// <param name="nodeBID">The unique id of the second node to link</param>
        /// <param name="weight">The weight of the edge</param>
        /// <returns>True if the edge was added successfully</returns>
        public bool AddEdge(int nodeAID, int nodeBID, float weight = 1f)
        {
            return graph.AddEdge(new TaggedUndirectedEdge<int, float>(nodeAID, nodeBID, weight));
        }

        public bool HasNode(int id)
        {
            return graph.ContainsVertex(id);
        }

        public int[] GetNodeIDs()
        {
            return graph.Vertices.ToArray();
        }

        public bool HasEdge(int node1ID, int node2ID)
        {
            return graph.ContainsEdge(node1ID, node2ID);
        }

        // public TaggedUndirectedEdge<int,float>[] GetEdgesFromNode(int nodeID)
        // {
        //     return graph.AdjacentEdges(nodeID).ToArray();
        // }

        public int[] GetAdjacentNodes(int nodeID)
        {
            return graph.AdjacentVertices(nodeID).ToArray();
        }

        private static double GetEdgeWeightFunc(TaggedUndirectedEdge<int,float> edge)
        {
            return edge.Tag;
        }

        public float CalculateShortestPathLenght(int source, int target)
        {
            var a = new UndirectedDijkstraShortestPathAlgorithm<int, TaggedUndirectedEdge<int, float>>(graph, GetEdgeWeightFunc);
            a.Compute(source);
            return (float) a.GetDistance(target);
        }
    }
}