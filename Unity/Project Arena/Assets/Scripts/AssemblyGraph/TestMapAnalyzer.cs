using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Math.Metrics;
using Unity.Collections;
using UnityEngine;

namespace AssemblyGraph
{
    public class TestMapAnalyzer : MonoBehaviour
    {
        private void Start()
        {

            var areas = new[]
            {
                new Area(0, 0, 4, 5),
                new Area(6, 0, 8, 4),
                new Area(8, 2, 10, 5),
                new Area(1, 7, 6, 13),
                new Area(8, 12, 11, 14),
                new Area(8, 9, 9, 10),
                
                new Area(4, 2, 6, 3, true),
                new Area(2, 5, 3, 7, true),
                new Area(6, 9, 8, 10, true),
                new Area(8, 10, 9, 12, true),
                
            };

            var charMap = MapUtils.TranslateAreaMap(areas);

            MapAnalyzer.CalculateGraphProperties(areas, charMap);

            var finalMap = MapUtils.GetStringFromCharMap(charMap);
        }
    }

    
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

        public static char[,] TranslateStringMap(string[] stringMap)
        {
            var rtn = new char[stringMap.Length, stringMap[0].Length];
            for (var row = 0; row < stringMap.Length; row++)
            {
                for (var col = 0; col < stringMap[0].Length; col++)
                {
                    rtn[row, col] = stringMap[row][col];
                }
            }

            return rtn;
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
                        if (area.isCorridor)
                            rtn[row - minCoords.y, col - minCoords.x] = 'R';
                        else
                            rtn[row - minCoords.y, col - minCoords.x] = 'r';
                    }
                }
            }

            return rtn;
        }

        // Broken since the previous thesis has a lot of mistakes and cannot even define a proper convention for AB maps
        // public static Area[] ConvertToAreas(string genome)
        // {
        //     var rtn = new List<Area>();
        //
        //     var reachedCorridors = false;
        //     var currentChar = 0;
        //     while (currentChar < genome.Length)
        //     {
        //         if (genome[currentChar] == '<')
        //         {
        //             if (reachedCorridors)
        //                 currentChar = ReadCorridor(genome, rtn, currentChar + 1);
        //             else
        //                 currentChar = ReadRoom(genome, rtn, currentChar + 1);
        //         }
        //
        //         if (genome[currentChar] == '|')
        //         {
        //             currentChar++;
        //             if (!reachedCorridors)
        //                 reachedCorridors = true;
        //             else
        //                 return rtn.ToArray();
        //         }
        //     }
        // }

        private static int ReadCorridor(string genome, List<Area> areas, int currentChar)
        {
            var newChar = ReadGenomeAngularBrackets(genome, currentChar, out var readData);
            
            if (readData[2] > 0)
                areas.Add(new Area(readData[0], readData[1], readData[0] + readData[2], readData[1] + 3, true));
            else
                areas.Add(new Area(readData[0], readData[1], readData[0] + 3, readData[1] - readData[2], true));
            return newChar;
        }
        
        private static int ReadRoom(string genome, List<Area> areas, int currentChar)
        {
            var newChar = ReadGenomeAngularBrackets(genome, currentChar, out var readData);

            areas.Add(new Area(readData[0], readData[1], readData[0] + readData[2], readData[1] + readData[2], false));
            return currentChar + 1;
        }

        private static int ReadGenomeAngularBrackets(string genome, int currentChar, out int[] readData)
        {
            readData = new int[3];
            var currentIndex = 0;
            var currentNumber = 1;
            while (genome[currentChar] != '>')
            {
                if (genome[currentChar] == '-')
                    currentNumber = -1;
                if (genome[currentChar] == ',')
                {
                    readData[currentIndex++] = currentNumber;
                    currentNumber = 0;
                }
                else
                {
                    currentNumber = currentNumber * 10 + (genome[currentChar] - '0');
                }
                currentChar++;
            }
            
            return currentChar;
        }
    }
}