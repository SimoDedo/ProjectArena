using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MLMapManager : MapManager {

    [Header("ML generation")] [SerializeField] protected int levelsCount;
    [SerializeField] protected StairsGenerator stairsGeneratorScript;

    protected List<char[,]> maps;

    protected override void InitializeAll() {
        mapAssemblerScript = mapAssembler.GetComponent<MapAssebler>();
        mapGeneratorScript = mapGenerator.GetComponent<MapGenerator>();
        objectDisplacerScript = objectDisplacer.GetComponent<ObjectDisplacer>();

        maps = new List<char[,]>();
    }

    public override void ManageMap(bool assembleMap) {
        if (loadMapFromFile) {
            // Load the map.
            LoadMapFromText();
        } else {
            // Generate the map.
            mapGeneratorScript.SaveMapSize();
            for (int i = 0; i < levelsCount; i++) {
                if (GetParameterManager() != null)
                    maps.Add(mapGeneratorScript.GenerateMap(seed + i, false, null));
                else
                    maps.Add(mapGeneratorScript.GenerateMap());
                mapGeneratorScript.ResetMapSize();
            }
            // Add the stairs.
            stairsGeneratorScript.GenerateStairs(maps, mapGeneratorScript);
            // Save the map.
            if (export)
                SaveMLMapAsText();
        }


        if (assembleMap) {
            // Assemble the map.
            mapAssemblerScript.AssembleMap(maps, mapGeneratorScript.GetWallChar(), mapGeneratorScript.GetRoomChar(), stairsGeneratorScript.GetVoidChar(), mapGeneratorScript.GetSquareSize(), mapGeneratorScript.GetWallHeight());
            // Displace the objects.
            for (int i = 0; i < maps.Count; i++)
                objectDisplacerScript.DisplaceObjects(maps[i], mapGeneratorScript.GetSquareSize(), mapGeneratorScript.GetWallHeight() * i);
        }
    }

    // Loads the map from a text file.
    protected override void LoadMapFromText() {
        if (textFilePath == null) {
            ManageError(Error.HARD_ERROR, -1);
        } else if (!File.Exists(textFilePath)) {
            ManageError(Error.HARD_ERROR, -1);
        } else {
            try {
                int mapsCount = 1;
                int width = 0;

                string[] lines = File.ReadAllLines(@textFilePath);

                foreach (string s in lines) {
                    if (s.Length == 0) {
                        mapsCount++;
                    } else if (mapsCount == 1)
                        width++;
                }

                for (int i = 0; i < mapsCount; i++) {
                    maps.Add(new char[width, lines[0].Length]);

                    for (int x = 0; x < maps[i].GetLength(0); x++) {
                        for (int y = 0; y < maps[i].GetLength(1); y++) {
                            maps[i][x, y] = lines[x + i * (width + 1)][y];
                        }
                    }
                }
            } catch (Exception) {
                ManageError(Error.HARD_ERROR, -1);
            }
        }
    }

    // Saves the map in a text file.
    protected void SaveMLMapAsText() {
        if (exportPath == null && !Directory.Exists(exportPath)) {
            ManageError(Error.SOFT_ERROR, "Error while retrieving the folder, please insert a valid path.");
        } else {
            int width = maps[0].GetLength(0);
            int height = maps[0].GetLength(1);

            try {
                string textMap = "";

                for (int i = 0; i < maps.Count; i++) {
                    for (int x = 0; x < width; x++) {
                        for (int y = 0; y < height; y++) {
                            textMap = textMap + maps[i][x, y];
                        }
                        if (x < width - 1)
                            textMap = textMap + "\n";
                    }
                    if (i != maps.Count - 1)
                        textMap = textMap + "\n\n";
                }

                System.IO.File.WriteAllText(exportPath + "/" + seed.ToString() + "_map.txt", textMap);
            } catch (Exception) {
                ManageError(Error.SOFT_ERROR, "Error while saving the map, please insert a valid path and check its permissions.");
            }

        }
    }

}