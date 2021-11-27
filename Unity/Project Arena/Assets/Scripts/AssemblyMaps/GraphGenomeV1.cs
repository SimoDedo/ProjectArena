using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyGraph;

namespace AssemblyMaps
{
    public class GraphGenomeV1
    {
        public readonly int numRows; // DO NOT CHANGE during evolution
        public readonly int numColumns; // DO NOT CHANGE during evolution

        public readonly int rowMaxHeight; // DO NOT CHANGE during evolution
        public readonly int columnMaxWidth; // DO NOT CHANGE during evolution

        public readonly int corridorThickness; // DO NOT CHANGE during evolution
        public readonly int rowSeparationHeight; // DO NOT CHANGE during evolution
        public readonly int columnSeparationWidth; // DO NOT CHANGE during evolution

        // Real Genome 
        public readonly AreaSize[,] roomSizes;
        public readonly bool[,] verticalConnections;
        public readonly bool[,] horizontalConnections;

        public GraphGenomeV1(
            int numRows,
            int numColumns,
            int rowMaxHeight,
            int columnMaxWidth,
            int corridorThickness,
            int rowSeparationHeight,
            int columnSeparationWidth,
            AreaSize[,] roomSizes,
            bool[,] verticalConnections,
            bool[,] horizontalConnections
        )
        {
            this.numRows = numRows;
            this.numColumns = numColumns;
            this.rowMaxHeight = rowMaxHeight;
            this.columnMaxWidth = columnMaxWidth;
            this.corridorThickness = corridorThickness;
            this.roomSizes = roomSizes;
            this.verticalConnections = verticalConnections;
            this.horizontalConnections = horizontalConnections;
            this.rowSeparationHeight = rowSeparationHeight;
            this.columnSeparationWidth = columnSeparationWidth;
        }
    }

    public static class GenomeGenerator
    {
        public static GraphGenomeV1 Generate(
            int numRows,
            int numColumns,
            int rowMaxHeight,
            int columnMaxWidth,
            int corridorThickness,
            int rowSeparations,
            int colSeparations,
            Random random
        )
        {
            var roomSizes = new AreaSize[numRows, numColumns];
            var verticalConnections = new bool[numRows - 1, numColumns];
            var horizontalConnections = new bool[numRows, numColumns - 1];
            // Step 1: generate room sizes
            for (var r = 0; r < numRows; r++)
            {
                for (var c = 0; c < numColumns; c++)
                {
                    if (random.NextDouble() < 0.3)
                    {
                        // Empty room
                        roomSizes[r, c] = new AreaSize(0, 0);
                    } else
                    {
                        var width = random.Next(corridorThickness, columnMaxWidth + 1);
                        var height = random.Next(corridorThickness, rowMaxHeight + 1);
                        roomSizes[r, c] = new AreaSize(width, height);
                    }
                }
            }

            // Step 2: generate vertical corridors
            for (var r = 0; r < numRows - 1; r++)
            {
                for (var c = 0; c < numColumns; c++)
                {
                    if (random.NextDouble() > 0.3)
                    {
                        verticalConnections[r, c] = true;
                    }
                }
            }

            // Step 3: generate horizontal corridors
            for (var r = 0; r < numRows; r++)
            {
                for (var c = 0; c < numColumns - 1; c++)
                {
                    if (random.NextDouble() > 0.3)
                    {
                        horizontalConnections[r, c] = true;
                    }
                }
            }

            return new GraphGenomeV1(numRows, numColumns, rowMaxHeight, columnMaxWidth, corridorThickness,
                rowSeparations, colSeparations,
                roomSizes,
                verticalConnections, horizontalConnections);
        }
    }

    public class AreaSize
    {
        public readonly int width;
        public readonly int height;

        public AreaSize(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public bool IsInvalid => width == 0 ^ height == 0;
        public bool IsZeroSized => width == 0 && height == 0;
    }

    public interface IGenomeTranslator
    {
        public void TranslateGenome(GraphGenomeV1 genome, out char[,] map, out Area[] areas);
    }

    public class GenomeTranslatorV1 : IGenomeTranslator
    {
        private readonly Random random;

        public GenomeTranslatorV1(Random random)
        {
            this.random = random;
        }

        public void TranslateGenome(GraphGenomeV1 genome, out char[,] map, out Area[] areas)
        {
            // TODO fix steps number

            // Step 1: Consistency checks
            // FIXME: probably the first two consistency checks are not useful
            // Check that no row has only 0 sized rooms
            for (var r = 0; r < genome.numRows; r++)
            {
                var okRow = false;
                for (var c = 0; c < genome.numColumns; c++)
                {
                    okRow |= genome.roomSizes[r, c].height > 0;
                }

                if (!okRow) throw new ApplicationException("Invalid genome: row has no room with height > 0");
            }

            // Check that no room has height greater than max
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (genome.roomSizes[r, c].height > genome.rowMaxHeight)
                        throw new ApplicationException(
                            "Invalid genome: room height is greater than the max row height"
                        );
                }
            }

            // Check that no column has only 0 sized rooms
            for (var c = 0; c < genome.numColumns; c++)
            {
                var okColumn = false;
                for (var r = 0; r < genome.numRows; r++)
                {
                    okColumn |= genome.roomSizes[r, c].width > 0;
                }

                if (!okColumn) throw new ApplicationException("Invalid genome: column has no room with width > 0");
            }

            // Check that no room has width greater than max
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (genome.roomSizes[r, c].width > genome.columnMaxWidth)
                        throw new ApplicationException(
                            "Invalid genome: room width is greater than the max column width"
                        );
                }
            }

            // Check that, if a room has 0 width/height, then also the height/width is zero
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (genome.roomSizes[r, c].IsInvalid)
                        throw new ApplicationException("Invalid genome: room has invalid dimensions!");
                }
            }

            // TODO other checks?

            // Step 1: find connected components
            var roomCCs = new int[genome.numRows, genome.numColumns];
            var ccCurrentNum = 1;
            var biggestCC = 0;
            var biggestCCSize = 0;
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (roomCCs[r, c] == 0)
                    {
                        var ccSize = GenomeGraphVisit(ccCurrentNum, r, c, genome, roomCCs);
                        if (ccSize > biggestCCSize)
                        {
                            biggestCC = ccCurrentNum;
                            biggestCCSize = ccSize;
                        }

                        ccCurrentNum++;
                    }
                }
            }

            // Step 1: find real height of every row, calculated as the max height of the rooms in that row which are
            // connected to the biggest subgraph
            var rowHeights = new int[genome.numRows];
            for (var r = 0; r < genome.numRows; r++)
            {
                var maxHeightRow = 0;
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (roomCCs[r, c] == biggestCC && maxHeightRow < genome.roomSizes[r, c].height)
                        maxHeightRow = genome.roomSizes[r, c].height;
                }

                rowHeights[r] = maxHeightRow;
            }

            // Step 2: find real width of every column, calculated as the max width of the rooms in that column which
            // are connected to the biggest subgraph
            var columnWidths = new int[genome.numColumns];
            for (var c = 0; c < genome.numColumns; c++)
            {
                var maxWidthColumns = 0;
                for (var r = 0; r < genome.numRows; r++)
                {
                    if (roomCCs[r, c] == biggestCC && maxWidthColumns < genome.roomSizes[r, c].width)
                        maxWidthColumns = genome.roomSizes[r, c].width;
                }

                columnWidths[c] = maxWidthColumns;
            }

            // Step 3: start placing rooms
            var rowStartingPos = new int[genome.numRows];
            for (var r = 1; r < genome.numRows; r++)
                rowStartingPos[r] = rowStartingPos[r - 1] + rowHeights[r - 1] + genome.rowSeparationHeight;

            var columnStartingPos = new int[genome.numColumns];
            for (var c = 1; c < genome.numColumns; c++)
                columnStartingPos[c] = columnStartingPos[c - 1] + columnWidths[c - 1] + genome.columnSeparationWidth;


            var placedRoomsMapping = new Dictionary<int, Area>();
            var areasList = new List<Area>();
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (roomCCs[r, c] != biggestCC) continue;
                    var area = CreateRoom(r, c, rowStartingPos[r], columnStartingPos[c], rowHeights[r], columnWidths[c],
                        genome, random);
                    areasList.Add(area);
                    placedRoomsMapping.Add(GetRoomIndex(r, c, genome), area);
                }
            }

            // Place vertical corridors
            for (var r = 0; r < genome.numRows - 1; r++)
            {
                for (var c = 0; c < genome.numColumns; c++)
                {
                    if (genome.verticalConnections[r, c] && roomCCs[r, c] == biggestCC &&
                        roomCCs[r + 1, c] == biggestCC)
                    {
                        var topRow = placedRoomsMapping[GetRoomIndex(r, c, genome)].bottomRow;
                        var bottomRow = placedRoomsMapping[GetRoomIndex(r + 1, c, genome)].topRow;
                        areasList.Add(CreateVerticalCorridor(genome.corridorThickness, topRow, bottomRow,
                            columnStartingPos[c],
                            columnWidths[c]));
                    }
                }
            }

            // Place horizontal corridors
            for (var r = 0; r < genome.numRows; r++)
            {
                for (var c = 0; c < genome.numColumns - 1; c++)
                {
                    if (genome.horizontalConnections[r, c] && roomCCs[r, c] == biggestCC &&
                        roomCCs[r, c + 1] == biggestCC)
                    {
                        var leftColumn = placedRoomsMapping[GetRoomIndex(r, c, genome)].rightColumn;
                        var rightColumn = placedRoomsMapping[GetRoomIndex(r, c + 1, genome)].leftColumn;
                        areasList.Add(CreateHorizontalCorridor(genome.corridorThickness, rowStartingPos[r],
                            rowHeights[r],
                            leftColumn, rightColumn));
                    }
                }
            }

            areas = areasList.ToArray();
            map = MapUtils.TranslateAreaMap(areas);
        }

        private static Area CreateHorizontalCorridor(
            int corridorThickness,
            int currentRowStartingIndex,
            int currentRowMaxHeight,
            int corridorStartingColumn,
            int corridorEndingColumn
        )
        {
            int corridorStartingRow;
            var corridorHeight = corridorThickness;
            if (corridorHeight <= currentRowMaxHeight / 2)
                corridorStartingRow = currentRowStartingIndex + (currentRowMaxHeight / 2) - corridorHeight;
            else
                corridorStartingRow = currentRowStartingIndex;

            var corridorEndingRow = corridorStartingRow + corridorHeight;

            return new Area(corridorStartingColumn, corridorStartingRow, corridorEndingColumn, corridorEndingRow, true);
        }

        private static int GetRoomIndex(int row, int column, GraphGenomeV1 genome)
        {
            return row * genome.numColumns + column;
        }

        private static Area CreateRoom(
            int row,
            int col,
            int rowStartingPos,
            int columnStartingPos,
            int rowHeight,
            int columnWidth,
            GraphGenomeV1 genome,
            Random pseudoRandomGen
        )
        {
            int selectedStartingColumn;
            int selectedStartingRow;

            var roomWidth = genome.roomSizes[row, col].width;
            var roomHeight = genome.roomSizes[row, col].height;
            if (roomWidth <= columnWidth / 2)
            {
                var possibleStartingColumns = roomWidth;
                selectedStartingColumn =
                    columnStartingPos + (columnWidth / 2) - roomWidth +
                    pseudoRandomGen.Next(0, possibleStartingColumns);
            } else
            {
                var possibleStartingColumns = columnWidth - roomWidth;
                selectedStartingColumn = columnStartingPos + pseudoRandomGen.Next(0, possibleStartingColumns);
            }

            if (roomHeight <= rowHeight / 2)
            {
                var possibleStartingRows = roomHeight;
                selectedStartingRow =
                    rowStartingPos + (rowHeight / 2) - roomHeight +
                    pseudoRandomGen.Next(0, possibleStartingRows);
            } else
            {
                var possibleStartingRows = rowHeight - roomHeight;
                selectedStartingRow = rowStartingPos + pseudoRandomGen.Next(0, possibleStartingRows);
            }

            var endingRow = selectedStartingRow + roomHeight;
            var endingCol = selectedStartingColumn + roomWidth;

            return new Area(selectedStartingColumn, selectedStartingRow, endingCol, endingRow);
        }

        private static Area CreateVerticalCorridor(
            int corridorThickness,
            int corridorStartingRow,
            int corridorEndingRow,
            int currentColumnStartingIndex,
            int currentColumnMaxWidth
        )
        {
            int corridorStartingColumn;
            var corridorWidth = corridorThickness;
            if (corridorWidth <= currentColumnMaxWidth / 2)
                corridorStartingColumn = currentColumnStartingIndex + (currentColumnMaxWidth / 2) - corridorWidth;
            else
                corridorStartingColumn = currentColumnStartingIndex;

            var corridorEndingColumn = corridorStartingColumn + corridorWidth;

            return new Area(corridorStartingColumn, corridorStartingRow, corridorEndingColumn, corridorEndingRow, true);
        }

        private int GenomeGraphVisit(int ccNumber, int row, int column, GraphGenomeV1 genome, int[,] ccRooms)
        {
            if (ccRooms[row, column] != 0) return 0;
            if (genome.roomSizes[row, column].IsZeroSized) return 0;
            ccRooms[row, column] = ccNumber;

            var connectedComponents = 1; // I'm connected to myself
            if (row > 0 && genome.verticalConnections[row - 1, column])
                connectedComponents += GenomeGraphVisit(ccNumber, row - 1, column, genome, ccRooms);
            if (row < genome.numRows - 1 && genome.verticalConnections[row, column])
                connectedComponents += GenomeGraphVisit(ccNumber, row + 1, column, genome, ccRooms);

            if (column > 0 && genome.horizontalConnections[row, column - 1])
                connectedComponents += GenomeGraphVisit(ccNumber, row, column - 1, genome, ccRooms);
            if (column < genome.numColumns - 1 && genome.horizontalConnections[row, column])
                connectedComponents += GenomeGraphVisit(ccNumber, row, column + 1, genome, ccRooms);
            return connectedComponents;
        }
    }

    internal static class IENumerableExtension
    {
        public static IEnumerable<int> CumulativeSum(this IEnumerable<int> sequence)
        {
            using var iterator = sequence.GetEnumerator();
            if (!iterator.MoveNext())
            {
                yield break;
            }

            yield return 0;
            var previous = iterator.Current;
            var sum = 0;
            while (iterator.MoveNext())
            {
                sum += previous;
                yield return sum;
                previous = iterator.Current;
            }
        }

        public static IEnumerable<int> SkipLast(this IEnumerable<int> source)
        {
            using var iterator = source.GetEnumerator();
            if (!iterator.MoveNext())
            {
                yield break;
            }

            var previous = iterator.Current;
            while (iterator.MoveNext())
            {
                yield return previous;
                previous = iterator.Current;
            }
        }
    }
}