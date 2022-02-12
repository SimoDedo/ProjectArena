using System;
using System.Collections.Generic;
using System.IO;
using Graph;
using Maps;
using UnityEngine;

namespace Managers.Map
{
    /// <summary>
    ///     MLMapManager is an implementation of MapManager used to manage multi-level maps.
    /// </summary>
    public class MLMapManager : MapManager
    {
        [Header("ML generation")] [SerializeField]
        protected int levelsCount;

        [SerializeField] protected StairsGenerator stairsGeneratorScript;

        protected List<char[,]> maps = new List<char[,]>();

        public override void ManageMap(bool assembleMap)
        {
            if (loadMapFromFile)
            {
                // Load the map.
                LoadMapFromText();
            }
            else
            {
                var partialGenomes = new List<string>();

                // Generate the map.
                mapGeneratorScript.SaveMapSize();
                for (var i = 0; i < levelsCount; i++)
                {
                    if (ParameterManager.HasInstance())
                        maps.Add(mapGeneratorScript.GenerateMap(seed + i, export));
                    else
                        maps.Add(mapGeneratorScript.GenerateMap());
                    if (export) partialGenomes.Add(mapGeneratorScript.ConvertMapToAB(false));
                    mapGeneratorScript.ResetMapSize();
                }

                // Add the stairs.
                stairsGeneratorScript.GenerateStairs(maps, mapGeneratorScript);
                // Save the map.
                if (export)
                {
                    AddTilesToGenomes(partialGenomes);
                    SaveMLMapAsAB(partialGenomes);
                    SaveMLMapAsText();
                }
            }

            if (assembleMap)
            {
                // Assemble the map.
                mapAssemblerScript.AssembleMap(maps, mapGeneratorScript.GetWallChar(),
                    mapGeneratorScript.GetRoomChar(), stairsGeneratorScript.GetVoidChar());
                // Displace the objects.
                for (var i = 0; i < maps.Count; i++)
                    objectDisplacerScript.DisplaceObjects(maps[i], mapAssemblerScript.GetSquareSize(),
                        mapAssemblerScript.GetWallHeight() * i);
            }
        }

        // Loads the map from a text file.
        protected override void LoadMapFromText()
        {
            if (seed == null)
            {
                if (textFilePath == null)
                    ManageError(Error.HARD_ERROR, -1);
                else if (!File.Exists(textFilePath))
                    ManageError(Error.HARD_ERROR, -1);
                else
                    try
                    {
                        ConvertToMatrices(File.ReadAllLines(textFilePath));
                    }
                    catch (Exception)
                    {
                        ManageError(Error.HARD_ERROR, -1);
                    }
            }
            else
            {
                ConvertToMatrices(seed.Split(new[] {"\n", "\r\n"},
                    StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public override char[,] GetMap()
        {
            throw new NotImplementedException();
        }

        public override Area[] GetAreas()
        {
            return new Area[0];
        }

        // Converts the map from a list of lines to a list of matrices.
        private void ConvertToMatrices(string[] lines)
        {
            var mapsCount = 1;
            var width = 0;

            foreach (var s in lines)
                if (s.Length == 0)
                    mapsCount++;
                else if (mapsCount == 1)
                    width++;

            for (var i = 0; i < mapsCount; i++)
            {
                maps.Add(new char[width, lines[0].Length]);

                for (var x = 0; x < maps[i].GetLength(0); x++)
                for (var y = 0; y < maps[i].GetLength(1); y++)
                    maps[i][x, y] = lines[x + i * (width + 1)][y];
            }
        }

        // Saves the map in a text file.
        protected void SaveMLMapAsText()
        {
            if (exportPath == null && !Directory.Exists(exportPath))
            {
                ManageError(Error.SOFT_ERROR, "Error while retrieving the folder, please insert a " +
                                              "valid path.");
            }
            else
            {
                var width = maps[0].GetLength(0);
                var height = maps[0].GetLength(1);

                try
                {
                    var textMap = "";

                    for (var i = 0; i < maps.Count; i++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            for (var y = 0; y < height; y++) textMap = textMap + maps[i][x, y];
                            if (x < width - 1) textMap = textMap + "\n";
                        }

                        if (i != maps.Count - 1) textMap = textMap + "\n\n";
                    }

                    File.WriteAllText(exportPath + "/" + seed + ".map.txt",
                        textMap);
                }
                catch (Exception)
                {
                    ManageError(Error.SOFT_ERROR, "Error while saving the map, please insert a " +
                                                  "valid path and check its permissions.");
                }
            }
        }

        // Saves the map using AB notation.
        private void SaveMLMapAsAB(List<string> genomes)
        {
            if (textFilePath == null && !Directory.Exists(textFilePath))
                ManageError(Error.SOFT_ERROR, "Error while retrieving the folder, please insert a " +
                                              "valid path.");
            else
                try
                {
                    var genome = genomes[0];

                    for (var i = 1; i < genomes.Count; i++) genome += "||" + genomes[i];

                    File.WriteAllText(exportPath + "/" + seed + ".ab.txt",
                        genome);
                }
                catch (Exception)
                {
                    ManageError(Error.SOFT_ERROR, "Error while saving the map, please insert a valid " +
                                                  "path and check its permissions.");
                }
        }

        // Adds tiles to the already computed genomes.
        private void AddTilesToGenomes(List<string> genomes)
        {
            var wallChar = mapGeneratorScript.GetWallChar();
            var roomChar = mapGeneratorScript.GetRoomChar();

            for (var i = 0; i < genomes.Count; i++)
            {
                genomes[i] += "|";
                for (var x = 0; x < maps[i].GetLength(0); x++)
                for (var y = 0; y < maps[i].GetLength(1); y++)
                    if (maps[i][x, y] != wallChar && maps[i][x, y] != roomChar)
                        genomes[i] += "<" + x + "," + y + "," + maps[i][x, y] + ">";
            }
        }
    }
}