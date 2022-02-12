using UnityEngine;
using Random = System.Random;

namespace Maps
{
    public static class GenomeGenerator
    {
        public static GraphGenomeV1 Generate(
            int numRows,
            int numColumns,
            int roomMaxSize,
            float minRatio,
            float maxRatio,
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
            for (var c = 0; c < numColumns; c++)
                if (random.NextDouble() < 0.3)
                {
                    // Empty room
                    roomSizes[r, c] = new AreaSize(0, 0);
                }
                else
                {
                    var width = random.Next(corridorThickness, roomMaxSize + 1);
                    var minHeight = (int) Mathf.Max(corridorThickness, Mathf.Ceil(width / maxRatio));
                    var maxHeight = (int) Mathf.Min(roomMaxSize, Mathf.Floor(width / minRatio)) + 1;
                    var height = random.Next(minHeight, maxHeight);
                    roomSizes[r, c] = new AreaSize(width, height);
                }

            // Step 2: generate vertical corridors
            for (var r = 0; r < numRows - 1; r++)
            for (var c = 0; c < numColumns; c++)
                if (random.NextDouble() > 0.3)
                    verticalConnections[r, c] = true;

            // Step 3: generate horizontal corridors
            for (var r = 0; r < numRows; r++)
            for (var c = 0; c < numColumns - 1; c++)
                if (random.NextDouble() > 0.3)
                    horizontalConnections[r, c] = true;

            return new GraphGenomeV1(numRows, numColumns, roomMaxSize, minRatio, maxRatio,
                corridorThickness,
                rowSeparations, colSeparations,
                roomSizes,
                verticalConnections, horizontalConnections);
        }
    }
}