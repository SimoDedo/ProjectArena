using System.Text;
using UnityEngine;

namespace Graph
{
    public static class MapUtils
    {
        public static string GetStringFromCharMap(char[,] map)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < map.GetLength(0); i++)
            {
                for (var j = 0; j < map.GetLength(1); j++)
                    builder.Append(map[i, j]);
                builder.Append("\n");
            }

            return builder.ToString();
        }

        public static char[,] TranslateAreaMap(Area[] areas)
        {
            var minCoords = new Vector2Int();
            var maxCoords = new Vector2Int();

            foreach (var area in areas)
            {
                if (area.leftColumn < minCoords.x)
                    minCoords.x = area.leftColumn;
                if (area.topRow < minCoords.y)
                    minCoords.y = area.topRow;
                if (area.rightColumn > maxCoords.x)
                    maxCoords.x = area.rightColumn;
                if (area.bottomRow > maxCoords.y)
                    maxCoords.y = area.bottomRow;
            }

            var rtn = new char[maxCoords.y - minCoords.y, maxCoords.x - minCoords.x];
            var rows = rtn.GetLength(0);
            var columns = rtn.GetLength(1);
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                    rtn[row, col] = 'w';
            }

            foreach (var area in areas)
            {
                for (var row = area.topRow; row < area.bottomRow; row++)
                {
                    for (var col = area.leftColumn; col < area.rightColumn; col++)
                    {
                        // if (area.isCorridor)
                        //     rtn[row - minCoords.y, col - minCoords.x] = 'R';
                        // else
                            rtn[row - minCoords.y, col - minCoords.x] = 'r';
                    }
                }
            }

            return rtn;
        }
    }
}