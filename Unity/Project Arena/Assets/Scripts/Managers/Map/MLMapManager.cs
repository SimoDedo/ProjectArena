using System;
using System.Collections.Generic;
using System.IO;

public class MLMapManager : MapManager {

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
            if (GetParameterManager() != null)
                maps.Add(mapGeneratorScript.GenerateMap(seed, export, exportPath));
            else
                maps.Add(mapGeneratorScript.GenerateMap());
        }

        if (assembleMap) {
            for (int i = 0; i < maps.Count; i++) {
                // Assemble the map.
                mapAssemblerScript.AssembleMap(maps[i], mapGeneratorScript.GetWallChar(), mapGeneratorScript.GetRoomChar(), squareSize, heigth * (i + 1));
                // Displace the objects.
                objectDisplacerScript.DisplaceObjects(maps[i], squareSize, heigth * (i + 1));
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

}