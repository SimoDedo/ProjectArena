using System;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;
using UnityEngine;

namespace AssemblyGraph
{
    public static class MapAnalyzer
    {
        private static readonly float SQRT2 = Mathf.Sqrt(2);
        private static readonly string ROW = "row";
        private static readonly string COLUMN = "column";
        private static readonly string OBJECT = "char";
        private static readonly string ORIGINX = "originX";
        private static readonly string ORIGINY = "originY";
        private static readonly string ENDX = "endX";
        private static readonly string ENDY = "endY";
        private static readonly string VISIBILITY = "visibility";
        private static readonly string ISCORRIDOR = "isCorridor";

        public static Graph GenerateTileGraph(char[,] map, char wallChar)
        {
            var tileGraph = GenerateTileGraphNodesFromMap(map, wallChar, out var rows, out var columns);
            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    if (map[row, column] == wallChar) continue;
                    if (row + 1 < rows && map[row + 1, column] != wallChar)
                    {
                        tileGraph.AddEdge(
                            GetTileIndexFromCoordinates(row, column, rows, columns),
                            GetTileIndexFromCoordinates(row + 1, column, rows, columns)
                        );
                    }

                    if (column + 1 < columns && map[row, column + 1] != wallChar)
                    {
                        tileGraph.AddEdge(
                            GetTileIndexFromCoordinates(row, column, rows, columns),
                            GetTileIndexFromCoordinates(row, column + 1, rows, columns)
                        );
                    }

                    if (row + 1 < rows && column + 1 < columns && map[row, column + 1] != wallChar &&
                        map[row + 1, column] != wallChar && map[row + 1, column + 1] != wallChar)
                    {
                        tileGraph.AddEdge(
                            GetTileIndexFromCoordinates(row, column, rows, columns),
                            GetTileIndexFromCoordinates(row + 1, column + 1, rows, columns),
                            SQRT2
                        );
                    }

                    if (row - 1 > 0 && column + 1 < columns && map[row, column + 1] != wallChar &&
                        map[row - 1, column] != wallChar && map[row - 1, column + 1] != wallChar)
                    {
                        tileGraph.AddEdge(
                            GetTileIndexFromCoordinates(row, column, rows, columns),
                            GetTileIndexFromCoordinates(row - 1, column + 1, rows, columns),
                            SQRT2
                        );
                    }
                }
            }

            return tileGraph;
        }

        private static Graph GenerateTileGraphNodesFromMap(char[,] map, char wallChar, out int rows, out int columns)
        {
            rows = map.GetLength(0);
            columns = map.GetLength(1);
            var tileGraph = new Graph();
            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    if (map[row, column] == wallChar) continue;
                    var idx = GetTileIndexFromCoordinates(row, column, rows, columns);
                    tileGraph.AddNode(idx,
                        new Tuple<string, object>(ROW, row),
                        new Tuple<string, object>(COLUMN, column),
                        new Tuple<string, object>(OBJECT, map[row, column])
                    );
                }
            }

            return tileGraph;
        }

        private static Graph GenerateVisibilityGraph(char[,] map, char wallChar)
        {
            var visibilityGraph = GenerateTileGraphNodesFromMap(map, wallChar, out var rows, out var columns);
            var visibilities = new int[rows, columns];
            var maxVisibility = float.MinValue;
            var minVisibility = float.MaxValue;
            for (var row1 = 0; row1 < rows; row1++)
            {
                for (var col1 = 0; col1 < columns; col1++)
                {
                    if (map[row1, col1] == wallChar) continue;
                    var nodeAID = GetTileIndexFromCoordinates(row1, col1, rows, columns);
                    // Now, we need to check, for each tile, if it is visible from our position
                    for (var row2 = 0; row2 < rows; row2++)
                    {
                        for (var col2 = 0; col2 < columns; col2++)
                        {
                            if (map[row2, col2] == wallChar) continue;
                            if (row1 == row2 && col1 == col2) continue;
                            // The two cells are not walls. We need to check every cell in between
                            if (isTileVisible(row1, col1, row2, col2, map, wallChar))
                            {
                                var nodeBID = GetTileIndexFromCoordinates(row2, col2, rows, columns);
                                visibilityGraph.AddEdge(nodeAID, nodeBID, EulerDistance(row1, row2, col1, col2));
                            }
                        }
                    }

                    var visibility = visibilityGraph.GetEdgesFromNode(nodeAID).Length;
                    visibilities[row1, col1] = visibility;
                    if (visibility > maxVisibility)
                        maxVisibility = visibility;
                    else if (visibility < minVisibility)
                        minVisibility = visibility;
                }
            }

            var visibilityInterval = maxVisibility - minVisibility;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    if (map[row, col] == wallChar) continue;
                    var visibility = (visibilities[row, col] - minVisibility) / visibilityInterval;
                    var node =
                        visibilityGraph.GetNode(GetTileIndexFromCoordinates(row, col, rows, columns));
                    node[VISIBILITY] = visibility;
                }
            }

            return visibilityGraph;
        }

        public static Graph GenerateRoomsCorridorsGraph(Area[] areas)
        {
            var roomsCorridorsGraph = new Graph();
            // ID of each area is simply it's index in the for loop
            for (var index = 0; index < areas.Length; index++)
            {
                var current = areas[index];
                roomsCorridorsGraph.AddNode(index,
                    new Tuple<string, object>(ORIGINX, current.topLeftX),
                    new Tuple<string, object>(ORIGINY, current.topLeftY),
                    new Tuple<string, object>(ENDX, current.bottomRightX),
                    new Tuple<string, object>(ENDY, current.bottomRightY),
                    new Tuple<string, object>(ISCORRIDOR, current.isCorridor)
                );
            }

            for (var index1 = 0; index1 < areas.Length; index1++)
            {
                var area1 = areas[index1];
                var center1X = (area1.topLeftX + area1.bottomRightX) / 2f;
                var center1Y = (area1.topLeftY + area1.bottomRightY) / 2f;
                for (var index2 = index1 + 1; index2 < areas.Length; index2++)
                {
                    var area2 = areas[index2];
                    var overlapX = Math.Min(area1.bottomRightX, area2.bottomRightX) -
                                   Math.Max(area1.topLeftX, area2.topLeftX);
                    var overlapY = Math.Min(area1.bottomRightY, area2.bottomRightY) -
                                   Math.Max(area1.topLeftY, area2.topLeftY);

                    if (overlapX >= 0 && overlapY >= 0)
                    {
                        if (overlapX == 0 && overlapY == 0) continue;
                        var center2X = (area2.topLeftX + area2.bottomRightX) / 2f;
                        var center2Y = (area2.topLeftY + area2.bottomRightY) / 2f;
                        var distance = EulerDistance(center1X, center2X, center1Y, center2Y);
                        roomsCorridorsGraph.AddEdge(index1, index2, distance);
                    }
                }
            }

            return roomsCorridorsGraph;
        }

        public static Graph GenerateRoomsCorridorsObjectsGraph(Area[] areas, char[,] map, char[] excludedChars)
        {
            var graph = GenerateRoomsCorridorsGraph(areas);

            var rows = map.GetLength(0);
            var columns = map.GetLength(1);

            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    if (!excludedChars.Contains(map[row, col]))
                    {
                        var objectNodeId = GetTileIndexFromCoordinates(row, col, rows, columns);
                        graph.AddNode(GetTileIndexFromCoordinates(row, col, rows, columns),
                            new Tuple<string, object>(OBJECT, map[row, col]),
                            new Tuple<string, object>(ROW, row),
                            new Tuple<string, object>(COLUMN, col)
                        );

                        // Find all the areas which contain this item
                        for (var index = 0; index < areas.Length; index++)
                        {
                            var area = areas[index];
                            // Is object in room
                            if (col >= area.topLeftX && 
                                col < area.bottomRightX &&
                                row >= area.topLeftY &&
                                row < area.bottomRightY)
                            {
                                var centerX = (area.topLeftX + area.bottomRightX) / 2f;
                                var centerY = (area.topLeftY + area.bottomRightY) / 2f;
                                var distance = EulerDistance(centerX, col, centerY, row);
                                graph.AddEdge(index, objectNodeId, distance);
                            }
                        }
                    }
                }
            }

            return graph;
        }

        // Dijkstra, assume edge weights are positive
        public static float CalculateShortestPathLength(Graph input, int nodeA, int nodeB)
        {
            var visitedNodes = new HashSet<int>();
            var nodeQueue = new SimplePriorityQueue<int, float>();
            nodeQueue.Enqueue(nodeA, 0);
            while (nodeQueue.Count != 0)
            {
                var currentNode = nodeQueue.First;
                var pathLength = nodeQueue.GetPriority(currentNode);
                nodeQueue.Dequeue();
                if (currentNode == nodeB) // Reached destination!
                    return pathLength;
                visitedNodes.Add(currentNode);
                var outgoingEdges = input.GetEdgesFromNode(currentNode);
                foreach (var e in outgoingEdges)
                {
                    var nextNode = e.node1 == currentNode ? e.node2 : e.node1;
                    var totalLength = pathLength + e.weight;
                    nodeQueue.Enqueue(nextNode, totalLength);
                }
            }

            // If we arrived here, nodeB couldn't be reached from nodeA.
            return float.PositiveInfinity;
        }

        private static int GetTileIndexFromCoordinates(int row, int column, int numRows, int numColumns)
        {
            return row * numColumns + column;
        }

        // private static int GetRoomIndexFromIndex(int roomNum, int rooms)
        // {
        //     return row * numColumns + column;
        // }

        // Tells if a tile is visible from another tile.
        private static bool isTileVisible(int y1, int x1, int y2, int x2, char[,] map, char wallChar)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;

            if (dx == 0)
            {
                var min = Mathf.Min(y1, y2);
                var max = Mathf.Max(y1, y2);
                for (var i = min; i <= max; i++)
                {
                    if (map[i, x1] == wallChar)
                        return false;
                }

                return true;
            }

            if (dy == 0)
            {
                var min = Mathf.Min(x1, x2);
                var max = Mathf.Max(x1, x2);
                for (var i = min; i <= max; i++)
                {
                    if (map[y1, i] == wallChar)
                        return false;
                }

                return true;
            }

            if (Mathf.Abs(dy) > Mathf.Abs(dx))
            {
                // If we travel more vertically than horizontally
                var min = Mathf.Min(y1, y2);
                var max = Mathf.Max(y1, y2);
                var m = (float) dx / dy;
                var q = (float) (y2 * x1 - y1 * x2) / dy;
                for (var i = min; i <= max; i++)
                {
                    var loc = q + m * i;
                    var floor = (int) Mathf.Floor(loc);
                    var ceil = (int) Mathf.Ceil(loc);
                    if (map[i, floor] == wallChar && map[i, ceil] == wallChar) return false;
                }
            }
            else
            {
                // If we travel more horizontally then vertically
                var min = Mathf.Min(x1, x2);
                var max = Mathf.Max(x1, x2);
                var m = (float) dy / dx;
                var q = (float) (x2 * y1 - x1 * y2) / dx;
                for (var i = min; i <= max; i++)
                {
                    var loc = q + m * i;
                    var floor = (int) Mathf.Floor(loc);
                    var ceil = (int) Mathf.Ceil(loc);
                    if (map[floor, i] == wallChar && map[ceil, i] == wallChar) return false;
                }
            }

            return true;
        }

        private static float EulerDistance(float x1, float x2, float y1, float y2)
        {
            return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
        }
    }
}

public class Area
{
    public readonly int topLeftX;
    public readonly int topLeftY;
    public readonly int bottomRightX;
    public readonly int bottomRightY;
    public readonly bool isCorridor;

    public Area(int topLeftX, int topLeftY, int bottomRightX, int bottomRightY, bool isCorridor)
    {
        this.topLeftX = topLeftX;
        this.topLeftY = topLeftY;
        this.bottomRightX = bottomRightX;
        this.bottomRightY = bottomRightY;
        this.isCorridor = isCorridor;
    }
}