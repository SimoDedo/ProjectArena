using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MLMapManager : MapManager {

    [Header("ML generation")] [SerializeField] private int levelsCount;
    [SerializeField] private float heightCorrection = 0;
    [SerializeField] private StairsGenerator stairsGeneratorScript;

    private List<char[,]> maps;

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
        }

        if (export)
            SaveMLMapAsText();

        if (assembleMap) {
            for (int i = 0; i < maps.Count; i++) {
                // Assemble the map.
                mapAssemblerScript.AssembleMap(maps[i], mapGeneratorScript.GetWallChar(), mapGeneratorScript.GetRoomChar(), mapGeneratorScript.GetSquareSize(), mapGeneratorScript.GetWallHeight() * i, false);
                // Displace the objects.
                objectDisplacerScript.DisplaceObjects(maps[i], mapGeneratorScript.GetSquareSize(), mapGeneratorScript.GetWallHeight() * i + heightCorrection);
            }
        }
    }

    // Loads the map from a text file.
    protected override void LoadMapFromText() {
        if (textFilePath == null) {
            GetParameterManager().ErrorBackToMenu(-1);
        } else if (!File.Exists(textFilePath)) {
            GetParameterManager().ErrorBackToMenu(-1);
        } else {
            try {
                int mapsCount = 0;
                int heigth = 0;

                string[] lines = File.ReadAllLines(@textFilePath);

                foreach (string s in lines)
                    if (s != "") {
                        heigth++;
                    } else {
                        mapsCount++;
                        break;
                    }

                for (int i = 0; i < mapsCount; i++) {
                    maps[i] = new char[lines[0].Length, heigth];

                    for (int x = 0; x < maps[i].GetLength(0); x++) {
                        for (int y = 0; y < maps[i].GetLength(1); y++) {
                            maps[0][x, y] = lines[y + i * (heigth + 1)][x];
                        }
                    }
                }
            } catch (Exception) {
                GetParameterManager().ErrorBackToMenu(-1);
            }
        }
    }

    // Saves the map in a text file.
    private void SaveMLMapAsText() {
        if (exportPath == null && !Directory.Exists(exportPath)) {
            Debug.LogError("Error while retrieving the folder, please insert a valid path.");
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
                Debug.LogError("Error while saving the map, please insert a valid path and check its permissions.");
            }

        }
    }

}