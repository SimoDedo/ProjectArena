using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using UnityEngine;

namespace Graph
{
    // FIXME Limitation: due to the usage of integer ids in the graph for the nodes, I cannot handle maps with have a width
// greater than MAX_MAP_WIDTH, otherwise I'll get clashes between room indexes and tile indexes
// ReSharper disable class InconsistentNaming
    public static class MapAnalyzer
    {
        private const int MAX_MAP_WIDTH = 10000;
        private const string RESOURCE = "resource";
        private const string ROW = "col";
        private const string COLUMN = "column";
        private const string OBJECT = "char";
        private const string LEFT_COLUMN = "leftColumn";
        private const string TOP_ROW = "topRow";
        private const string RIGHT_COLUMN = "rightColumn";
        private const string BOTTOM_ROW = "bottomRow";
        private const string VISIBILITY = "visibility";
        private const string IS_CORRIDOR = "isCorridor";
        private const string IS_DUMMY = "isDummy";
        private const char WALL_CHAR = 'w';
        private const char FLOOR_CHAR = 'r';
        private const float TOLERANCE = 0.001f;
        private static readonly float Sqrt2 = Mathf.Sqrt(2);

        public static DirectedGraph GenerateTileGraph(char[,] map)
        {
            var tileGraph = GenerateTileGraphNodesFromMap(map, out var rows, out var columns);
            for (var column = 0; column < columns; column++)
            for (var row = 0; row < rows; row++)
            {
                if (map[row, column] == WALL_CHAR) continue;
                if (row + 1 < rows && map[row + 1, column] != WALL_CHAR)
                    tileGraph.AddEdges(
                        GetTileIndexFromCoordinates(row, column, rows, columns),
                        GetTileIndexFromCoordinates(row + 1, column, rows, columns)
                    );

                if (column + 1 < columns && map[row, column + 1] != WALL_CHAR)
                    tileGraph.AddEdges(
                        GetTileIndexFromCoordinates(row, column, rows, columns),
                        GetTileIndexFromCoordinates(row, column + 1, rows, columns)
                    );

                if (row + 1 < rows && column + 1 < columns && map[row, column + 1] != WALL_CHAR &&
                    map[row + 1, column] != WALL_CHAR && map[row + 1, column + 1] != WALL_CHAR)
                    tileGraph.AddEdges(
                        GetTileIndexFromCoordinates(row, column, rows, columns),
                        GetTileIndexFromCoordinates(row + 1, column + 1, rows, columns),
                        Sqrt2
                    );

                if (row - 1 > 0 && column + 1 < columns && map[row, column + 1] != WALL_CHAR &&
                    map[row - 1, column] != WALL_CHAR && map[row - 1, column + 1] != WALL_CHAR)
                    tileGraph.AddEdges(
                        GetTileIndexFromCoordinates(row, column, rows, columns),
                        GetTileIndexFromCoordinates(row - 1, column + 1, rows, columns),
                        Sqrt2
                    );
            }

            return tileGraph;
        }

        private static DirectedGraph GenerateTileGraphNodesFromMap(char[,] map, out int rows, out int columns)
        {
            rows = map.GetLength(0);
            columns = map.GetLength(1);
            var tileGraph = new DirectedGraph();
            for (var column = 0; column < columns; column++)
            for (var row = 0; row < rows; row++)
            {
                if (map[row, column] == WALL_CHAR) continue;
                var idx = GetTileIndexFromCoordinates(row, column, rows, columns);
                tileGraph.AddNode(idx,
                    new Tuple<string, object>(ROW, row),
                    new Tuple<string, object>(COLUMN, column),
                    new Tuple<string, object>(OBJECT, map[row, column])
                );
            }

            return tileGraph;
        }

        private static DirectedGraph GenerateVisibilityGraph(char[,] map)
        {
            var visibilityGraph = GenerateTileGraphNodesFromMap(map, out var rows, out var columns);
            var visibilities = new int[rows, columns];
            var maxVisibility = float.MinValue;
            var minVisibility = float.MaxValue;
            for (var row1 = 0; row1 < rows; row1++)
            for (var col1 = 0; col1 < columns; col1++)
            {
                if (map[row1, col1] == WALL_CHAR) continue;
                var nodeAID = GetTileIndexFromCoordinates(row1, col1, rows, columns);
                // Now, we need to check, for each tile, if it is visible from our position
                for (var row2 = 0; row2 < rows; row2++)
                for (var col2 = 0; col2 < columns; col2++)
                {
                    if (map[row2, col2] == WALL_CHAR) continue;
                    if (row1 == row2 && col1 == col2) continue;
                    // The two cells are not walls. We need to check every cell in between
                    if (IsTileVisible(row1, col1, row2, col2, map))
                    {
                        var nodeBID = GetTileIndexFromCoordinates(row2, col2, rows, columns);
                        visibilityGraph.AddEdges(nodeAID, nodeBID, EuclideanDistance(row1, col1, row2, col2));
                    }
                }

                var visibility = visibilityGraph.GetNodeDegree(nodeAID);
                visibilities[row1, col1] = visibility;
                if (visibility > maxVisibility)
                    maxVisibility = visibility;
                else if (visibility < minVisibility) minVisibility = visibility;
            }

            var visibilityInterval = maxVisibility - minVisibility;
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                if (map[row, col] == WALL_CHAR) continue;
                var visibility = (visibilities[row, col] - minVisibility) / visibilityInterval;
                var node =
                    visibilityGraph.GetNodeProperties(GetTileIndexFromCoordinates(row, col, rows, columns));
                node[VISIBILITY] = visibility;
            }

            return visibilityGraph;
        }

        public static float[,] GenerateVisibilityMatrix(char[,] map)
        {
            var rows = map.GetLength(0);
            var columns = map.GetLength(1);
            var visibilityMatrix = new float[rows, columns];
            var maxVisibility = float.MinValue;
            var minVisibility = float.MaxValue;

            for (var row1 = 0; row1 < rows; row1++)
            for (var col1 = 0; col1 < columns; col1++)
            {
                if (map[row1, col1] == WALL_CHAR) continue;
                // Now, we need to check, for each tile, if it is visible from our position
                for (var row2 = 0; row2 < rows; row2++)
                for (var col2 = 0; col2 < columns; col2++)
                {
                    if (map[row2, col2] == WALL_CHAR) continue;
                    if (row1 == row2 && col1 == col2) continue;
                    // The two cells are not walls. We need to check every cell in between
                    if (IsTileVisible(row1, col1, row2, col2, map)) visibilityMatrix[row1, col1]++;
                }

                var visibility = visibilityMatrix[row1, col1];
                visibilityMatrix[row1, col1] = visibility;
                if (visibility > maxVisibility) maxVisibility = visibility;
                if (visibility < minVisibility) minVisibility = visibility;
            }

            var visibilityInterval = maxVisibility - minVisibility;
            if (visibilityInterval == 0) visibilityInterval = maxVisibility; // Everything will be normalized to 1

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
            {
                if (map[row, col] == WALL_CHAR) continue;
                visibilityMatrix[row, col] = (visibilityMatrix[row, col] - minVisibility) / visibilityInterval;
            }

            return visibilityMatrix;
        }

        public static DirectedGraph GenerateRoomsCorridorsGraph(Area[] areas)
        {
            var roomsCorridorsGraph = new DirectedGraph();
            // ID of each area is simply it's index in the for loop
            for (var index = 0; index < areas.Length; index++)
            {
                var current = areas[index];
                roomsCorridorsGraph.AddNode(GetRoomIndexFromIndex(index),
                    new Tuple<string, object>(LEFT_COLUMN, current.leftColumn),
                    new Tuple<string, object>(TOP_ROW, current.topRow),
                    new Tuple<string, object>(RIGHT_COLUMN, current.rightColumn),
                    new Tuple<string, object>(BOTTOM_ROW, current.bottomRow),
                    new Tuple<string, object>(IS_CORRIDOR, current.isCorridor),
                    new Tuple<string, object>(IS_DUMMY, current.isDummyRoom)
                );
            }

            for (var index1 = 0; index1 < areas.Length; index1++)
            {
                var area1 = areas[index1];
                var centerColumn1 = (area1.leftColumn + area1.rightColumn) / 2f;
                var centerRow1 = (area1.topRow + area1.bottomRow) / 2f;
                for (var index2 = index1 + 1; index2 < areas.Length; index2++)
                {
                    var area2 = areas[index2];
                    var overlapColumn = Math.Min(area1.rightColumn, area2.rightColumn) -
                                        Math.Max(area1.leftColumn, area2.leftColumn);
                    var overlapRow = Math.Min(area1.bottomRow, area2.bottomRow) -
                                     Math.Max(area1.topRow, area2.topRow);

                    if (overlapColumn >= 0 && overlapRow >= 0)
                    {
                        if (overlapColumn == 0 && overlapRow == 0) continue;
                        if (!(area1.isCorridor ^ area2.isCorridor))
                            // In the map, two corridors touch. Ignore this, or duplicated edges will be generated
                            continue;
                        var centerColumn2 = (area2.leftColumn + area2.rightColumn) / 2f;
                        var centerRow2 = (area2.topRow + area2.bottomRow) / 2f;
                        var distance = EuclideanDistance(centerColumn1, centerRow1, centerColumn2, centerRow2);
                        roomsCorridorsGraph.AddEdges(GetRoomIndexFromIndex(index1), GetRoomIndexFromIndex(index2),
                            distance);
                    }
                }
            }

            return roomsCorridorsGraph;
        }

        public static DirectedGraph GenerateRoomsGraph(Area[] areas)
        {
            var roomsCorridorsGraph = GenerateRoomsCorridorsGraph(areas);
            // Now, find all the areas which are corridors, find all the vertices connected to them, connect them and
            // remove the vertex

            var nodes = roomsCorridorsGraph.GetNodeIDs();
            var nodesToRemove = new List<int>();
            foreach (var node in nodes)
            {
                var isCorridor = roomsCorridorsGraph.GetNodeProperties(node)[IS_CORRIDOR];
                if (isCorridor != null && (bool) isCorridor)
                {
                    var outEdges = roomsCorridorsGraph.GetOutEdges(node).ToList();

                    for (var i = 0; i < outEdges.Count; i++)
                    {
                        var (nodeA, lengthA) = outEdges[i];
                        for (var j = i + 1; j < outEdges.Count; j++)
                        {
                            var (nodeB, lengthB) = outEdges[j];
                            roomsCorridorsGraph.AddEdges(nodeA, nodeB, lengthA + lengthB);
                        }
                    }

                    nodesToRemove.Add(node);
                }
            }

            var discardDummies = areas.Any(it => !it.isCorridor && !it.isDummyRoom);
            if (discardDummies)
                foreach (var node in nodes)
                {
                    var isDummy = roomsCorridorsGraph.GetNodeProperties(node)[IS_DUMMY];
                    if (isDummy != null && (bool) isDummy)
                    {
                        var outEdges = roomsCorridorsGraph.GetOutEdges(node).ToList();

                        for (var i = 0; i < outEdges.Count; i++)
                        {
                            var (nodeA, lengthA) = outEdges[i];
                            for (var j = i + 1; j < outEdges.Count; j++)
                            {
                                var (nodeB, lengthB) = outEdges[j];
                                roomsCorridorsGraph.AddEdges(nodeA, nodeB, lengthA + lengthB);
                            }
                        }

                        nodesToRemove.Add(node);
                    }
                }

            nodesToRemove.ForEach(it => roomsCorridorsGraph.RemoveNode(it));
            return roomsCorridorsGraph;
        }

        public static DirectedGraph GenerateRoomsCorridorsObjectsGraph(Area[] areas, char[,] map, char[] excludedChars)
        {
            if (map.GetLength(1) > MAX_MAP_WIDTH)
                throw new InvalidOperationException("Cannot handle maps with width > " + MAX_MAP_WIDTH);

            var graph = GenerateRoomsCorridorsGraph(areas);

            var rows = map.GetLength(0);
            var columns = map.GetLength(1);

            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
                if (!excludedChars.Contains(map[row, col]))
                {
                    var objectNodeId = GetTileIndexFromCoordinates(row, col, rows, columns);
                    graph.AddNode(GetTileIndexFromCoordinates(row, col, rows, columns),
                        new Tuple<string, object>(OBJECT, map[row, col]),
                        new Tuple<string, object>(ROW, row),
                        new Tuple<string, object>(COLUMN, col)
                    );

                    // Find all the areas which contain this item
                    for (var roomIndex = 0; roomIndex < areas.Length; roomIndex++)
                    {
                        var area = areas[roomIndex];
                        // Is object in room
                        if (col >= area.leftColumn &&
                            col < area.rightColumn &&
                            row >= area.topRow &&
                            row < area.bottomRow)
                        {
                            var centerColumn = (area.leftColumn + area.rightColumn) / 2f;
                            var centerRow = (area.topRow + area.bottomRow) / 2f;
                            var distance = EuclideanDistance(centerColumn, centerRow, col, row);
                            graph.AddEdges(GetRoomIndexFromIndex(roomIndex), objectNodeId, distance);
                        }
                    }
                }

            return graph;
        }

        public static MapProperties CalculateGraphProperties(Area[] areas)
        {
            var rtn = new MapProperties();
            var roomGraph = GenerateRoomsGraph(areas);
            // var roomGraph = GenerateRoomsCorridorsGraph(areas);
            var nodes = roomGraph.GetNodeIDs();

            // TODO should I include corridors in the analysis? 
            // Degree centrality min, max, avg
            var degreeMax = int.MinValue;
            var degreeMin = int.MaxValue;
            var avgDegree = 0f;
            foreach (var node in nodes)
            {
                var degree = roomGraph.GetNodeDegree(node);
                if (degree > degreeMax) degreeMax = degree;
                if (degree < degreeMin) degreeMin = degree;
                avgDegree += degree;
            }

            avgDegree /= nodes.Length;
            rtn.degreeMin = degreeMin;
            rtn.degreeMax = degreeMax;
            rtn.degreeAvg = avgDegree;

            // Closeness centrality min, max, avg; eccentricity min, max and avg and betweenness
            var betweenness = nodes.ToDictionary(node => node, node => 0f);
            var minCCentrality = float.MaxValue;
            var maxCCentrality = float.MinValue;
            var avgCCentrality = 0f;

            var radius = float.MaxValue;
            var centerSetSize = 0;
            var diameter = float.MinValue;
            var peripherySetSize = 0;
            var avgEccentricity = 0f;

            foreach (var node1 in nodes)
            {
                var totalPathLength = 0f;
                var eccentricityNode = float.MinValue;
                foreach (var node2 in nodes)
                    if (node1 != node2)
                    {
                        var pathLenght = roomGraph.CalculateShortestPathLenghtAndBetweeness(node1, node2, betweenness);
                        totalPathLength += pathLenght;
                        if (pathLenght > eccentricityNode) eccentricityNode = pathLenght;
                    }

                if (eccentricityNode < radius)
                {
                    radius = eccentricityNode;
                    centerSetSize = 1;
                }
                else if (Math.Abs(eccentricityNode - radius) < TOLERANCE)
                {
                    centerSetSize++;
                }

                if (eccentricityNode > diameter)
                {
                    diameter = eccentricityNode;
                    peripherySetSize = 1;
                }
                else if (Math.Abs(eccentricityNode - diameter) < TOLERANCE)
                {
                    peripherySetSize++;
                }

                avgEccentricity += eccentricityNode;

                if (totalPathLength < minCCentrality) minCCentrality = totalPathLength;
                if (totalPathLength > maxCCentrality) maxCCentrality = totalPathLength;
                avgCCentrality += totalPathLength;
            }

            avgCCentrality /= nodes.Length;
            avgEccentricity /= nodes.Length;

            rtn.cCentralityMin = minCCentrality;
            rtn.cCentralityMax = maxCCentrality;
            rtn.cCentralityAvg = avgCCentrality;

            rtn.bCentralityMin = betweenness.Min(it => it.Value);
            rtn.bCentralityMax = betweenness.Max(it => it.Value);
            rtn.bCentralityAvg = betweenness.Average(it => it.Value);

            rtn.radius = radius;
            rtn.centerSetSize = centerSetSize;
            rtn.diameter = diameter;
            rtn.peripherySetSize = peripherySetSize;
            rtn.avgEccentricity = avgEccentricity;


            // Density
            var edgesCompleteGraph = nodes.Length * (nodes.Length - 1) / 2f;
            var totalEdges = nodes.Aggregate(0f, (current, node) =>
                current + roomGraph.GetNodeDegree(node));
            var density = totalEdges / edgesCompleteGraph;

            rtn.density = density;

            return rtn;
        }

        // Dijkstra, assume edge weights are positive
        private static float CalculateShortestPathLength(DirectedGraph input, int nodeA, int nodeB)
        {
            return input.CalculateShortestPathLenght(nodeA, nodeB);
        }

        private static int GetTileIndexFromCoordinates(int row, int column, int numRows, int numColumns)
        {
            return row * numColumns + column;
        }

        private static int GetRoomIndexFromIndex(int roomNum)
        {
            return roomNum + MAX_MAP_WIDTH;
        }

        // Tells if a tile is visible from another tile.
        private static bool IsTileVisible(int y1, int x1, int y2, int x2, char[,] map)
        {
            var dx = x2 - x1;
            var dy = y2 - y1;

            if (dx == 0)
            {
                var min = Mathf.Min(y1, y2);
                var max = Mathf.Max(y1, y2);
                for (var i = min; i <= max; i++)
                    if (map[i, x1] == WALL_CHAR)
                        return false;

                return true;
            }

            if (dy == 0)
            {
                var min = Mathf.Min(x1, x2);
                var max = Mathf.Max(x1, x2);
                for (var i = min; i <= max; i++)
                    if (map[y1, i] == WALL_CHAR)
                        return false;

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
                    var floor = (int) Mathf.Max(0, Mathf.Floor(loc));
                    var ceil = (int) Mathf.Min(map.GetLength(1), Mathf.Ceil(loc));
                    if (map[i, floor] == WALL_CHAR && map[i, ceil] == WALL_CHAR) return false;
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
                    var floor = (int) Mathf.Max(0, Mathf.Floor(loc));
                    var ceil = (int) Mathf.Min(map.GetLength(0), Mathf.Ceil(loc));
                    if (map[floor, i] == WALL_CHAR && map[ceil, i] == WALL_CHAR) return false;
                }
            }

            return true;
        }

        private static float EuclideanDistance(float x1, float y1, float x2, float y2)
        {
            return Mathf.Sqrt(Mathf.Pow(x2 - x1, 2) + Mathf.Pow(y2 - y1, 2));
        }

        public static void AddEverything(Area[] areas, char[,] map)
        {
            ClearMap(map);
            var height = map.GetLength(0);
            var width = map.GetLength(1);
            var roomGraph = GenerateRoomsCorridorsGraph(areas);
            var diameter = ComputeDiameter(areas, roomGraph);
            var diagonal = Mathf.Sqrt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));
            var visibilityMatrix = GenerateVisibilityMatrix(map);

            var normalizedDegrees = ComputeNormalizedDegree(roomGraph);

            // Place spawn points

            var objectsPlacedPos = new List<Vector2Int>();
            const char spawnPointsChar = 's';
            const int numSpawnPointsToPlace = 5;
            var objectTypesConsidered = new List<char> {spawnPointsChar};

            var spawnPointsRoomWeights = new[] {1f, 0.25f, -2f};
            var spawnPointsTileWeights = new[] {1f, 0.5f, 0.5f};
            var spawnPointsVisibilityFit = visibilityMatrix.Convert(it => 1 - it);
            var spawnPointNormDegreeFitness = ComputeNormalizedDegreeFitness(normalizedDegrees, 0.1f, 0.3f);
            for (var i = 0; i < numSpawnPointsToPlace; i++)
            {
                var bestTile = GetBestTile(roomGraph, diameter, diagonal, spawnPointsChar, numSpawnPointsToPlace,
                    objectTypesConsidered,
                    objectsPlacedPos,
                    spawnPointNormDegreeFitness, spawnPointsVisibilityFit, spawnPointsRoomWeights,
                    spawnPointsTileWeights);

                TryAddResource(bestTile, spawnPointsChar, roomGraph, map, objectsPlacedPos);
            }

            Debug.Log("Finished placing spawn points");
            const char medkitsChar = 'h';
            const int numMedkitsToPlace = 2;
            var medkitsRoomWeights = new[] {1f, 0.25f, 0f};
            var medkitsTileWeights = new[] {1f, 0.25f, 0.5f};
            var medkitsVisibilityFit = visibilityMatrix.Convert(it => 1 - Mathf.Abs(0.5f - it));
            var medkitsNormDegreeFitness = ComputeNormalizedDegreeFitness(normalizedDegrees, 0.3f, 0.5f);
            objectTypesConsidered.Add(medkitsChar);
            for (var i = 0; i < numMedkitsToPlace; i++)
            {
                var bestTile = GetBestTile(roomGraph, diameter, diagonal, medkitsChar, numMedkitsToPlace,
                    objectTypesConsidered,
                    objectsPlacedPos,
                    medkitsNormDegreeFitness, medkitsVisibilityFit, medkitsRoomWeights, medkitsTileWeights);

                TryAddResource(bestTile, medkitsChar, roomGraph, map, objectsPlacedPos);
            }

            Debug.Log("Finished placing medkits");
            const char ammoCrateChar = 'a';
            const int numAmmoCratesToPlace = 2;
            var ammoCrateRoomWeights = new[] {1f, 0.25f, 0f};
            var ammoCrateTileWeights = new[] {1f, 0.25f, 0.5f};
            var ammoCrateNormDegreeFitness1 = ComputeNormalizedDegreeFitness(normalizedDegrees, 0.2f, 0.4f);
            var ammoCrateNormDegreeFitness2 = ComputeNormalizedDegreeFitness(normalizedDegrees, 0.8f, 0.9f);
            var ammoCratesVisibilityFit = visibilityMatrix;
            objectTypesConsidered.Add(ammoCrateChar);
            for (var i = 0; i < numAmmoCratesToPlace / 2; i++)
            {
                var bestTile = GetBestTile(roomGraph, diameter, diagonal, ammoCrateChar, numAmmoCratesToPlace,
                    objectTypesConsidered,
                    objectsPlacedPos,
                    ammoCrateNormDegreeFitness1, ammoCratesVisibilityFit, ammoCrateRoomWeights, ammoCrateTileWeights);

                TryAddResource(bestTile, ammoCrateChar, roomGraph, map, objectsPlacedPos);
            }

            Debug.Log("Finished placing ammo 1");

            for (var i = numAmmoCratesToPlace / 2; i < numAmmoCratesToPlace; i++)
            {
                var bestTile = GetBestTile(roomGraph, diameter, diagonal, ammoCrateChar, numAmmoCratesToPlace,
                    objectTypesConsidered,
                    objectsPlacedPos,
                    ammoCrateNormDegreeFitness2, ammoCratesVisibilityFit, ammoCrateRoomWeights, ammoCrateTileWeights);

                TryAddResource(bestTile, ammoCrateChar, roomGraph, map, objectsPlacedPos);
            }

            Debug.Log("Finished placing ammo 2");

            Debug.Log("Fine");
        }

        private static void ClearMap(char[,] map)
        {
            var rows = map.GetLength(0);
            var columns = map.GetLength(1);
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < columns; col++)
                if (map[row, col] != WALL_CHAR && map[row, col] != FLOOR_CHAR)
                    map[row, col] = FLOOR_CHAR;
        }

        private static void TryAddResource(
            Vector2Int bestTile,
            char spawnPointsChar,
            DirectedGraph roomGraph,
            char[,] map,
            List<Vector2Int> objectsPlacedPos
        )
        {
            if (bestTile.x == -1 || bestTile.y == -1) return; // Invalid tile, do not do anything
            var rows = map.GetLength(0);
            var columns = map.GetLength(1);

            var objectNodeID = GetTileIndexFromCoordinates(bestTile.y, bestTile.x, rows, columns);
            roomGraph.AddNode(objectNodeID,
                new Tuple<string, object>(ROW, bestTile.y),
                new Tuple<string, object>(COLUMN, bestTile.x),
                new Tuple<string, object>(RESOURCE, spawnPointsChar)
            );

            var nodesIDs = roomGraph.GetNodeIDs();
            foreach (var nodeID in nodesIDs)
            {
                var node = roomGraph.GetNodeProperties(nodeID);
                var temp = node[LEFT_COLUMN];
                if (temp == null) continue; // this is not a room
                var leftColumn = (int) temp;
                var topRow = (int) node[TOP_ROW];
                var rightColumn = (int) node[RIGHT_COLUMN];
                var bottomRow = (int) node[BOTTOM_ROW];

                if (bestTile.x >= leftColumn && bestTile.x < rightColumn && bestTile.y >= topRow &&
                    bestTile.y < bottomRow)
                {
                    var centerColumn = (leftColumn + rightColumn) / 2f;
                    var centerRow = (topRow + bottomRow) / 2f;
                    var distance = EuclideanDistance(centerColumn, centerRow, bestTile.x, bestTile.y);
                    roomGraph.AddEdges(objectNodeID, nodeID, distance);
                }
            }

            map[bestTile.y, bestTile.x] = spawnPointsChar;
            objectsPlacedPos.Add(bestTile);
        }

        private static Vector2Int GetBestTile(
            DirectedGraph roomGraph,
            float diameter,
            float diagonal,
            char spawnPointsChar,
            int numSpawnPoints,
            List<char> objectTypesConsidered,
            List<Vector2Int> objectsPlaced,
            Dictionary<int, float> normDegreeFitness,
            float[,] visibilityFit,
            float[] roomWeights,
            float[] tileWeights
        )
        {
            var roomsToConsider = normDegreeFitness.Keys;
            var bestScore = float.MinValue;
            var bestNodeID = -1;
            foreach (var roomID in roomsToConsider)
            {
                // Do not consider nodes which represent resources
                if (roomGraph.GetNodeProperties(roomID)[RESOURCE] != null) continue;
                var roomScore = RoomFit(roomGraph, diameter, roomID, normDegreeFitness[roomID], spawnPointsChar,
                    numSpawnPoints, objectTypesConsidered, roomWeights);
                if (roomScore > bestScore)
                {
                    bestScore = roomScore;
                    bestNodeID = roomID;
                }
            }

            if (bestNodeID == -1) Debug.Log("!?");
            var bestNodeProperties = roomGraph.GetNodeProperties(bestNodeID);

            var leftColumn = (int) bestNodeProperties[LEFT_COLUMN];
            var topRow = (int) bestNodeProperties[TOP_ROW];
            var rightColumn = (int) bestNodeProperties[RIGHT_COLUMN];
            var bottomRow = (int) bestNodeProperties[BOTTOM_ROW];

            var bestTileScore = float.MinValue;
            var bestTile = new Vector2Int(-1, -1);
            var currentTile = new Vector2Int();
            for (var col = leftColumn; col < rightColumn; col++)
            {
                currentTile.x = col;
                for (var row = topRow; row < bottomRow; row++)
                {
                    currentTile.y = row;
                    if (objectsPlaced.Contains(currentTile))
                        continue; // Avoid placing object where there is one already

                    var tileFit = TileFit(col, row, visibilityFit[row, col], leftColumn, topRow, rightColumn, bottomRow,
                        objectsPlaced,
                        diagonal, tileWeights);
                    if (tileFit > bestTileScore)
                    {
                        bestTileScore = tileFit;
                        bestTile.x = col;
                        bestTile.y = row;
                    }
                }
            }

            return bestTile;
        }

        private static float TileFit(
            int row,
            int col,
            float visibilityFit,
            int originX,
            int originY,
            int endX,
            int endY,
            List<Vector2Int> objectsPlaced,
            float diagonal,
            float[] tileWeights
        )
        {
            return tileWeights[0] * visibilityFit +
                   tileWeights[1] * WallDistance(originX, originY, endX, endY, row, col) +
                   tileWeights[2] * ObjectDistance(row, col, objectsPlaced, diagonal);
        }

        private static float ObjectDistance(int x, int y, List<Vector2Int> objectsPlaced, float diagonal)
        {
            if (objectsPlaced.Count == 0) return 0;
            var minDistance = objectsPlaced.Select(coord => EuclideanDistance(x, y, coord.x, coord.y))
                .Min();
            return minDistance / diagonal;
        }

        private static float WallDistance(int leftColumn, int topRow, int rightColumn, int bottomRow, int col, int row)
        {
            var distance = Mathf.Min(col - leftColumn, rightColumn - 1 - col) +
                           Mathf.Min(row - topRow, bottomRow - 1 - row) + 1f;
            var normalizingFactor = (rightColumn - leftColumn + bottomRow - topRow) / 2f;
            return distance / normalizingFactor;
        }

        private static float RoomFit(
            DirectedGraph roomGraph,
            float diameter,
            int roomID,
            float degreeFit,
            char spawnPointsChar,
            int numSpawnPoints,
            List<char> objectTypesConsidered,
            float[] roomWeights
        )
        {
            return roomWeights[0] * degreeFit + roomWeights[1] *
                ResourceDistance(roomGraph, diameter, roomID, objectTypesConsidered) + roomWeights[2] *
                ResourceRedundancy(roomGraph, roomID, spawnPointsChar, numSpawnPoints);
        }

        private static float ResourceRedundancy(
            DirectedGraph roomGraph,
            int roomID,
            char spawnPointsChar,
            int numSpawnPoints
        )
        {
            var adjacentNodes = roomGraph.GetAdjacentNodes(roomID);
            var redundancy = 0f;
            foreach (var otherNodeID in adjacentNodes)
            {
                var resource = roomGraph.GetNodeProperties(otherNodeID)[RESOURCE];
                if (resource != null && (char) resource == spawnPointsChar) redundancy++;
            }

            return redundancy / numSpawnPoints;
        }

        private static float ResourceDistance(
            DirectedGraph roomGraph,
            float diameter,
            int roomID,
            List<char> objectTypesConsidered
        )
        {
            var nodesIDs = roomGraph.GetNodeIDs();
            var minDistance =
                (from nodeID in nodesIDs
                    let resource = roomGraph.GetNodeProperties(nodeID)[RESOURCE]
                    where resource != null && objectTypesConsidered.Contains((char) resource)
                    select CalculateShortestPathLength(roomGraph, roomID, nodeID)).Prepend(diameter).Min();

            return minDistance / diameter;
        }

        private static Dictionary<int, float> ComputeNormalizedDegreeFitness(
            Dictionary<int, float> normDegrees,
            float min,
            float max
        )
        {
            var minFitness = float.MaxValue;
            var maxFitness = float.MinValue;
            foreach (var distance in normDegrees.Select(degree => IntervalDistance(min, max, degree.Value)))
            {
                if (distance < minFitness) minFitness = distance;
                if (distance > maxFitness) maxFitness = distance;
            }

            var variation = maxFitness - minFitness;
            if (variation == 0) // TODO BETTER NORMALIZATION IN THIS CASE
                return normDegrees.ToDictionary(degree => degree.Key,
                    degree => 1f);
            return normDegrees.ToDictionary(degree => degree.Key,
                degree => 1f - (degree.Value - minFitness) / variation);
        }

        private static float ComputeDiameter(Area[] areas, DirectedGraph areaGraph)
        {
            var diameter = float.MinValue;
            for (var i1 = 0; i1 < areas.Length; i1++)
            for (var i2 = i1 + 1; i2 < areas.Length; i2++)
            {
                var distance = CalculateShortestPathLength(areaGraph,
                    GetRoomIndexFromIndex(i1),
                    GetRoomIndexFromIndex(i2));
                if (distance > diameter) diameter = distance;
            }

            return diameter;
        }

        private static Dictionary<int, float> ComputeNormalizedDegree(
            DirectedGraph roomGraph,
            bool discardDeadEnds = false
        )
        {
            var ids = roomGraph.GetNodeIDs();
            var minDegree = float.MaxValue;
            var maxDegree = float.MinValue;
            var rtn = new Dictionary<int, float>();
            foreach (var id in ids)
            {
                var degree = roomGraph.GetNodeDegree(id);
                if (degree < minDegree) minDegree = degree;
                if (degree > maxDegree) maxDegree = degree;
            }

            var degreeVariation = maxDegree - minDegree;
            foreach (var id in ids)
                if (degreeVariation == 0)
                {
                    rtn.Add(id, 1f);
                }
                else
                {
                    var degree = roomGraph.GetNodeDegree(id);
                    if (!discardDeadEnds || degree != 1) rtn.Add(id, (degree - minDegree) / degreeVariation);
                }

            return rtn;
        }

        private static float IntervalDistance(float min, float max, float value)
        {
            if (value >= min && value <= max) return 0;
            return value < min ? Mathf.Abs(min - value) : Mathf.Abs(value - max);
        }

        // TODO Convert to struct
        [Serializable]
        public class MapProperties
        {
            public int degreeMax;
            public int degreeMin;
            public float degreeAvg;

            public float cCentralityMin;
            public float cCentralityMax;
            public float cCentralityAvg;

            public float bCentralityMin;
            public float bCentralityMax;
            public float bCentralityAvg;

            public float radius;
            public float centerSetSize;
            public float diameter;
            public float peripherySetSize;
            public float avgEccentricity;

            public float density;
        }
    }

    public class Area
    {
        public readonly int bottomRow;
        public readonly bool isCorridor;
        public readonly bool isDummyRoom;
        public readonly int leftColumn;
        public readonly int rightColumn;
        public readonly int topRow;

        public Area(
            int leftColumn,
            int topRow,
            int rightColumn,
            int bottomRow,
            bool isCorridor = false,
            bool isDummyRoom = false
        )
        {
            this.leftColumn = leftColumn;
            this.topRow = topRow;
            this.rightColumn = rightColumn;
            this.bottomRow = bottomRow;
            this.isCorridor = isCorridor;
            this.isDummyRoom = isDummyRoom;
        }
    }
}