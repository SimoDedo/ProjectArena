﻿using System;
using System.IO;
using Maps;
using Maps.MapGenerator;

namespace Managers.Map
{
    /// <summary>
    ///     SLMapManager is an implementation of MapManager used to manage single-level maps.
    /// </summary>
    public class SLMapManager : MapManager
    {
        private char[,] map;

        public override void ManageMap(bool assembleMap)
        {
            if (loadMapFromFile)
            {
                // Load the map.
                LoadMapFromText();
                // Flip the map if needed.
                if (flip) map = MapEdit.FlipMap(map);
            }
            else
            {
                // Generate the map.
                if (ParameterManager.HasInstance())
                    map = mapGeneratorScript.GenerateMap(seed, export, exportPath);
                else
                    map = mapGeneratorScript.GenerateMap();
            }

            if (assembleMap)
            {
                // Assemble the map.
                mapAssemblerScript.AssembleMap(map, mapGeneratorScript.GetWallChar(),
                    mapGeneratorScript.GetRoomChar());
                // Displace the objects.
                objectDisplacerScript.DisplaceObjects(map, mapAssemblerScript.GetMapScale(),
                    0);
            }
        }

        // Loads the map from a text file.
        protected override void LoadMapFromText()
        {
            if (seed == null)
            {
                if (textFilePath == null)
                    ErrorManager.ErrorBackToMenu(-1);
                else if (!File.Exists(textFilePath))
                    ErrorManager.ErrorBackToMenu(-1);
                else
                    try
                    {
                        ConvertToMatrix(File.ReadAllLines(textFilePath));
                    }
                    catch (Exception)
                    {
                        ErrorManager.ErrorBackToMenu(-1);
                    }
            }
            else
            {
                ConvertToMatrix(seed.Split(new[] {"\n", "\r\n"},
                    StringSplitOptions.RemoveEmptyEntries));
            }
        }


        public override char[,] GetMap()
        {
            return (char[,]) map.Clone();
        }

        public override Area[] GetAreas()
        {
            return mapGeneratorScript.ConvertMapToAreas();
        }

        // Converts the map from a list of lines to a matrix.
        private void ConvertToMatrix(string[] lines)
        {
            map = new char[lines.GetLength(0), lines[0].Length];

            for (var x = 0; x < map.GetLength(0); x++)
            for (var y = 0; y < map.GetLength(1); y++)
                map[x, y] = lines[x][y];
        }
    }
}