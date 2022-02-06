using System;
using System.Collections;
using System.Collections.Generic;
using AssemblyLogging;
using MapManipulation;
using UnityEngine;
using UnityEngine.Assertions;

namespace AssemblyMaps.Map_Generator
{
    public class GraphMapGenerator : MapGenerator
    {
        [SerializeField] private int minRoomWidth = 15;
        [SerializeField] private int maxRoomWidth = 20;
        [SerializeField] private int minRoomHeight = 15;
        [SerializeField] private int maxRoomHeight = 20;
        [SerializeField] private int minGridSeparation = 5;
        [SerializeField] private int maxGridSeparation = 15;
        [SerializeField] private int minCorridorThickness = 3;
        [SerializeField] private int maxCorridorThickness = 3;

        private readonly List<Area> areas = new List<Area>();

        // TODO Add probabilities for corridor spawn, room sizes, ...
        private void Start()
        {
            SetReady(true);
            Assert.IsTrue(maxCorridorThickness > 0);
        }

        public override char[,] GenerateMap()
        {
            var roomsDictionary = new Dictionary<int, int>();
            areas.Clear();
            SaveMapSize();

            var realWidth = width;
            var realHeight = height;
            
            width += borderSize * 2;
            height += borderSize * 2;
            
            map = new char[width,height];
            
            MapEdit.FillMap(map, wallChar);
            InitializePseudoRandomGenerator();

            var maxNumberOfColumns = realWidth / (maxRoomWidth + maxGridSeparation);
            var maxNumberOfRows = realHeight / (maxRoomHeight + maxGridSeparation);
            // Initialize arrays containing widths and heights of every room
            var roomsWidth = new int[maxNumberOfRows][];
            var roomsHeight = new int[maxNumberOfRows][];

            for (var row = 0; row < maxNumberOfRows; row++)
            {
                roomsWidth[row] = new int[maxNumberOfColumns];
                roomsHeight[row] = new int[maxNumberOfColumns];
                for (var column = 0; column < maxNumberOfColumns; column++)
                {
                    if (pseudoRandomGen.NextDouble() < 0.5)
                    {
                        roomsWidth[row][column] = pseudoRandomGen.Next(minRoomWidth, maxRoomWidth);
                        roomsHeight[row][column] = pseudoRandomGen.Next(minRoomHeight, maxRoomHeight);
                        // placedRoomWidth = true;
                    } else
                    {
                        roomsWidth[row][column] = minCorridorThickness;
                        roomsHeight[row][column] = minCorridorThickness;
                    }
                }
            }

            // Initialize array containing separation height between cells rows
            var gridVerticalSeparations = new int[maxNumberOfRows - 1];
            for (var row = 0; row < maxNumberOfRows - 1; row++)
                gridVerticalSeparations[row] = pseudoRandomGen.Next(minGridSeparation, maxGridSeparation);
            // Initialize array containing separation width between cells columns
            var gridHorizontalSeparations = new int[maxNumberOfColumns - 1];
            for (var column = 0; column < maxNumberOfColumns - 1; column++)
                gridHorizontalSeparations[column] = pseudoRandomGen.Next(minGridSeparation, maxGridSeparation);

            // Calculate max height of each row
            var maxRowHeight = new int[maxNumberOfRows];
            for (var row = 0; row < maxNumberOfRows; row++)
            {
                for (var column = 0; column < maxNumberOfColumns; column++)
                    maxRowHeight[row] = Mathf.Max(maxRowHeight[row], roomsHeight[row][column]);
            }

            // Calculate max width of each column
            var maxColumnWidth = new int[maxNumberOfColumns];
            for (var row = 0; row < maxNumberOfRows; row++)
            {
                for (var column = 0; column < maxNumberOfColumns; column++)
                    maxColumnWidth[column] = Mathf.Max(maxColumnWidth[column], roomsWidth[row][column]);
            }

            // Initialize array containing the width of the corridor connecting cell (i,j) with (i+1,j).
            // A value of 0 indicates that there is no corridor
            var verticalCorridorsWidths = new int[maxNumberOfRows - 1][];
            for (var row = 0; row < maxNumberOfRows - 1; row++)
            {
                verticalCorridorsWidths[row] = new int[maxNumberOfColumns];
                for (var column = 0; column < maxNumberOfColumns; column++)
                {
                    if (pseudoRandomGen.NextDouble() < 0.5)
                    {
                        verticalCorridorsWidths[row][column] = Mathf.Min(
                            pseudoRandomGen.Next(minCorridorThickness, maxCorridorThickness),
                            maxColumnWidth[column]
                        );
                    } else
                    {
                        verticalCorridorsWidths[row][column] = 0;
                    }
                }
            }

            // Initialize array containing the height of the corridor connecting cell (i,j) with (i,j+1).
            // A value of 0 indicates that there is no corridor
            var horizontalCorridorsHeights = new int[maxNumberOfRows][];
            for (var row = 0; row < maxNumberOfRows; row++)
            {
                horizontalCorridorsHeights[row] = new int[maxNumberOfColumns - 1];
                for (var column = 0; column < maxNumberOfColumns - 1; column++)
                {
                    if (pseudoRandomGen.NextDouble() < 0.5)
                    {
                        horizontalCorridorsHeights[row][column] = Mathf.Min(
                            pseudoRandomGen.Next(minCorridorThickness, maxCorridorThickness),
                            maxColumnWidth[column]
                        );
                    } else
                    {
                        horizontalCorridorsHeights[row][column] = 0;
                    }
                }
            }

            // Calculate starting row index in the char[,] map of each row
            var rowStartingIndexes = new int[maxNumberOfRows];
            rowStartingIndexes[0] = borderSize;
            for (var row = 1; row < maxNumberOfRows; row++)
                rowStartingIndexes[row] = rowStartingIndexes[row - 1] + maxRowHeight[row - 1] +
                    gridVerticalSeparations[row - 1];

            // Calculate starting column index in the char[,] map of each column
            var columnStartingIndex = new int[maxNumberOfColumns];
            columnStartingIndex[0] = borderSize;
            for (var column = 1; column < maxNumberOfColumns; column++)
                columnStartingIndex[column] = columnStartingIndex[column - 1] + maxColumnWidth[column - 1] +
                    gridHorizontalSeparations[column - 1];

            // Graph visit initialization, every room has not been visited
            var roomConnectedComponentNum = new int[maxNumberOfRows][];
            for (var row = 0; row < maxNumberOfRows; row++)
                roomConnectedComponentNum[row] = new int[maxNumberOfColumns];

            // Graph visit, for each room not yet visited (roomConnectedComponentNum == 0), do a bfs/dfs visit
            var currentConnectedNumber = 1;
            var bestConnectedComponent = 0;
            var bestConnectedComponentSize = 0;

            for (var row = 0; row < maxNumberOfRows; row++)
            {
                for (var column = 0; column < maxNumberOfColumns; column++)
                {
                    if (roomConnectedComponentNum[row][column] == 0)
                    {
                        if (roomsHeight[row][column] == 0 || roomsWidth[row][column] == 0) continue;
                        var connectedRooms = VisitCell(currentConnectedNumber, row, column, roomConnectedComponentNum,
                            horizontalCorridorsHeights, verticalCorridorsWidths);
                        if (connectedRooms > bestConnectedComponentSize)
                        {
                            bestConnectedComponent = currentConnectedNumber;
                            bestConnectedComponentSize = connectedRooms;
                        }

                        currentConnectedNumber++;
                    }
                }
            }

            // Fill the map with the rooms and corridors belonging to the connected component
            for (var row = 0; row < maxNumberOfRows; row++)
            {
                for (var column = 0; column < maxNumberOfColumns; column++)
                {
                    if (roomConnectedComponentNum[row][column] == bestConnectedComponent)
                    {
                        roomsDictionary.Add(row * maxNumberOfColumns + column, areas.Count);
                        areas.Add(CreateRoom(rowStartingIndexes[row], columnStartingIndex[column],
                            maxColumnWidth[column],
                            maxRowHeight[row], roomsWidth[row][column], roomsHeight[row][column]));
                    }
                }
            }

            // Fill vertical corridors
            for (var row = 0; row < maxNumberOfRows - 1; row++)
            {
                for (var column = 0; column < maxNumberOfColumns; column++)
                {
                    if (verticalCorridorsWidths[row][column] > 0 &&
                        roomConnectedComponentNum[row][column] == bestConnectedComponent)
                    {
                        areas.Add(CreateVerticalCorridor(verticalCorridorsWidths[row][column],
                            areas[roomsDictionary[row * maxNumberOfColumns + column]],
                            areas[roomsDictionary[(row + 1) * maxNumberOfColumns + column]],
                            columnStartingIndex[column],
                            maxColumnWidth[column]));
                    }
                }
            }

            // Fill horizontal corridors
            for (var row = 0; row < maxNumberOfRows; row++)
            {
                for (var column = 0; column < maxNumberOfColumns - 1; column++)
                {
                    if (horizontalCorridorsHeights[row][column] > 0 &&
                        roomConnectedComponentNum[row][column] == bestConnectedComponent)
                    {
                        areas.Add(CreateHorizontalCorridor(horizontalCorridorsHeights[row][column],
                            rowStartingIndexes[row],
                            areas[roomsDictionary[row * maxNumberOfColumns + column]],
                            areas[roomsDictionary[row * maxNumberOfColumns + column + 1]],
                            maxRowHeight[row]));
                    }
                }
            }

            FillMap(areas);
            // ProcessMap();
            PopulateMap();
            
            var textMap = GetMapAsText();
            SaveMapTextGameEvent.Instance.Raise(textMap);
            if (createTextFile) SaveMapAsText(textMap);
            return map;
        }

        private void FillMap(List<Area> areas)
        {
            // TODO Areas are flipped vertically. Understand why
            foreach (var area in areas)
            {
                for (var row = area.topRow; row < area.bottomRow; row++)
                {
                    for (var col = area.leftColumn; col < area.rightColumn; col++)
                    {
                        map[height - row - 1, col] = 'r';
                    }
                }
            }
        }

        private static Area CreateVerticalCorridor(
            int corridorThickness,
            Area topRoom,
            Area bottomRoom,
            int currentColumnStartingIndex,
            int currentColumnMaxWidth
        )
        {
            var corridorStartingRow = topRoom.bottomRow;
            var corridorEndingRow = bottomRoom.topRow;

            int corridorStartingColumn;
            var corridorWidth = corridorThickness;
            if (corridorWidth <= currentColumnMaxWidth / 2)
                corridorStartingColumn = currentColumnStartingIndex + (currentColumnMaxWidth / 2) - corridorWidth;
            else
                corridorStartingColumn = currentColumnStartingIndex;

            var corridorEndingColumn = corridorStartingColumn + corridorWidth;

            return new Area(corridorStartingColumn, corridorStartingRow, corridorEndingColumn, corridorEndingRow, true);
        }

        private Area CreateHorizontalCorridor(
            int corridorThickness,
            int currentRowStartingIndex,
            Area leftRoom,
            Area rightRoom,
            int currentRowMaxHeight
        )
        {
            var corridorStartingColumn = leftRoom.rightColumn;
            var corridorEndingColumn = rightRoom.leftColumn;

            int corridorStartingRow;
            var corridorHeight = corridorThickness;
            if (corridorHeight <= currentRowMaxHeight / 2)
                corridorStartingRow = currentRowStartingIndex + (currentRowMaxHeight / 2) - corridorHeight;
            else
                corridorStartingRow = currentRowStartingIndex;

            var corridorEndingRow = corridorStartingRow + corridorHeight;

            return new Area(corridorStartingColumn, corridorStartingRow, corridorEndingColumn, corridorEndingRow, true);
        }

        private Area CreateRoom(
            int rowStartingIndex,
            int columnStartingIndex,
            int columnWidth,
            int rowHeight,
            int roomWidth,
            int roomHeight
        )
        {
            int selectedStartingColumn;
            int selectedStartingRow;
            if (roomWidth <= columnWidth / 2)
            {
                var possibleStartingColumns = roomWidth;
                selectedStartingColumn =
                    columnStartingIndex + (columnWidth / 2) - roomWidth +
                    pseudoRandomGen.Next(0, possibleStartingColumns);
            } else
            {
                var possibleStartingColumns = columnWidth - roomWidth;
                selectedStartingColumn = columnStartingIndex + pseudoRandomGen.Next(0, possibleStartingColumns);
            }

            if (roomHeight <= rowHeight / 2)
            {
                var possibleStartingRows = roomHeight;
                selectedStartingRow =
                    rowStartingIndex + (rowHeight / 2) - roomHeight +
                    pseudoRandomGen.Next(0, possibleStartingRows);
            } else
            {
                var possibleStartingRows = rowHeight - roomHeight;
                selectedStartingRow = rowStartingIndex + pseudoRandomGen.Next(0, possibleStartingRows);
            }

            var endingRow = selectedStartingRow + roomHeight;
            var endingCol = selectedStartingColumn + roomWidth;

            return new Area(selectedStartingColumn, selectedStartingRow, endingCol, endingRow);
        }

        // Returns the number of rooms found while exploring
        private int VisitCell(
            int connectedNumber,
            int row,
            int column,
            int[][] roomConnectedComponentNum,
            int[][] hasHorizontalCorridor,
            int[][] hasVerticalCorridor
        )
        {
            if (roomConnectedComponentNum[row][column] != 0) return 0;
            roomConnectedComponentNum[row][column] = connectedNumber;
            var maxRows = roomConnectedComponentNum.Length;
            var maxColumns = roomConnectedComponentNum[0].Length;

            var connectedComponents = 1; // I'm connected to myself
            if (row > 0 && hasVerticalCorridor[row - 1][column] > 0)
                connectedComponents += VisitCell(connectedNumber, row - 1, column, roomConnectedComponentNum,
                    hasHorizontalCorridor, hasVerticalCorridor);
            if (row < maxRows - 1 && hasVerticalCorridor[row][column] > 0)
                connectedComponents += VisitCell(connectedNumber, row + 1, column, roomConnectedComponentNum,
                    hasHorizontalCorridor, hasVerticalCorridor);

            if (column > 0 && hasHorizontalCorridor[row][column - 1] > 0)
                connectedComponents += VisitCell(connectedNumber, row, column - 1, roomConnectedComponentNum,
                    hasHorizontalCorridor, hasVerticalCorridor);
            if (column < maxColumns - 1 && hasHorizontalCorridor[row][column] > 0)
                connectedComponents += VisitCell(connectedNumber, row, column + 1, roomConnectedComponentNum,
                    hasHorizontalCorridor, hasVerticalCorridor);
            return connectedComponents;
        }

        public override string ConvertMapToAB(bool exportObjects = true)
        {
            throw new NotImplementedException();
        }

        public override Area[] ConvertMapToAreas()
        {
            return areas.ToArray();
        }
    }
}