using System;
using System.Text;
using UnityEngine;

namespace AssemblyGraph
{
    public class TestMapAnalyzer : MonoBehaviour
    {
        private void Start()
        {
            string[] stringMap1 = {
                "rrrrwrr", 
                "rrrrrrr", 
                "rrrrwrr", 
                "rrrrwww", 
                "wwwwwww", 
                
            };

            var areaMap1 = new Area[]
            {
                new Area(0, 0, 4, 4, false),
                new Area(5, 0, 6, 2, true),
                new Area(2, 1, 7, 3, false),
            };
            
            var map1 = MapUtils.TranslateStringMap(stringMap1);

            MapAnalyzer.GenerateRoomsCorridorsObjectsGraph(areaMap1, map1, new[] {'w'});

            // MapUtils.printCharMap(map);
            // var graph = MapAnalyzer.GenerateTileGraph(map, 'w');
        }
    }

    public static class MapUtils
    {
        public static void printCharMap(char[,] map)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < map.GetLength(0); i++)
            {
                for (var j = 0; j < map.GetLength(1); j++)
                    builder.Append(map[i,j]);
                builder.Append("\n");
            }
            Debug.Log(builder.ToString());
        }

        public static char[,] TranslateStringMap(string[] stringMap)
        {
            var rtn = new char[stringMap.Length,stringMap[0].Length];
            for (var row = 0; row < stringMap.Length; row++)
            {
                for (var col = 0; col < stringMap[0].Length; col++)
                {
                    rtn[row, col] = stringMap[row][col];
                }
            }

            return rtn;
        }
    }
}