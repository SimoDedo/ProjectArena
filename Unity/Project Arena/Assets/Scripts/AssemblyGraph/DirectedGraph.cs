using System;
using System.Collections.Generic;
using System.Linq;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.Observers;
using QuikGraph.Algorithms.RankedShortestPath;
using QuikGraph.Algorithms.ShortestPath;
using UnityEngine;

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

    public class DirectedGraph
    {
        private readonly Dictionary<int, NodeProperties> nodeProperties = new Dictionary<int, NodeProperties>();

        private readonly BidirectionalGraph<int, TaggedEdge<int, float>> graph =
            new BidirectionalGraph<int, TaggedEdge<int, float>>();

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
        /// Adds edge connecting nodeAID and nodeBID and vice versa, if they both exist
        /// </summary>
        /// <param name="nodeAID">The unique id of first node to link</param>
        /// <param name="nodeBID">The unique id of the second node to link</param>
        /// <param name="weight">The weight of the edge</param>
        /// <returns>True if the edge was added successfully</returns>
        public bool AddEdges(int nodeAID, int nodeBID, float weight = 1f)
        {
            return graph.AddEdge(new TaggedEdge<int, float>(nodeAID, nodeBID, weight))
                & graph.AddEdge(new TaggedEdge<int, float>(nodeBID, nodeAID, weight));
        }

        public bool HasNode(int id)
        {
            return graph.ContainsVertex(id);
        }

        public int[] GetNodeIDs()
        {
            return graph.Vertices.ToArray();
        }

        // public TaggedUndirectedEdge<int,float>[] GetEdgesFromNode(int nodeID)
        // {
        //     return graph.AdjacentEdges(nodeID).ToArray();
        // }

        public IEnumerable<int> GetAdjacentNodes(int nodeID)
        {
            return graph.OutEdges(nodeID).Select(it => it.Target);
        }

        public int GetNodeDegree(int nodeID)
        {
            return graph.OutDegree(nodeID);
        }

        public NodeProperties GetNodeProperties(int nodeID)
        {
            return nodeProperties[nodeID];
        }

        private static double GetEdgeWeightFunc(TaggedEdge<int, float> edge)
        {
            return edge.Tag;
        }

        public float CalculateShortestPathLenght(int source, int target)
        {
            var a = new DijkstraShortestPathAlgorithm<int, TaggedEdge<int, float>>(graph, GetEdgeWeightFunc);
            a.Compute(source);
            return a.TryGetDistance(target, out var rtn) ? (float) rtn : float.MaxValue;
        }

        public float CalculateShortestPathLenghtAndBetweeness(int source, int target, Dictionary<int, float> betweeness)
        {
            // TODO using ranked algorithms to find possible different shortest paths algorithms. By default
            //  the algorithms tries 3 different paths, and I filter out those that are not the shortest.
            //  If, in some map, there are more than 3 shortest paths, I do not consider them, so the
            //  betweeness centrality calculated will be wrong.
            var alg = new HoffmanPavleyRankedShortestPathAlgorithm<int, TaggedEdge<int, float>>(graph,
                GetEdgeWeightFunc);

            alg.Compute(source, target);

            var minDistance = float.MaxValue;
            var foundValue = false;
            foreach (var algPath in alg.ComputedShortestPaths)
            {
                var pathList = algPath.ToList();
                var sum = pathList.Sum(it => it.Tag);
                if (!foundValue)
                {
                    foundValue = true;
                    minDistance = sum;
                } else if (sum > minDistance)
                {
                    // TODO are the paths returned in lenght order?
                    break;
                } else
                {
                    Debug.Log("This map has more than one shortest path between nodes!");
                }

                // DO not consider final vertex in path
                pathList.RemoveAt(pathList.Count-1);
                pathList.ForEach(it=>
                {
                    betweeness[it.Target] = betweeness[it.Target] + 1f / pathList.Count;
                });

            }
            return minDistance;
        }
    }
}