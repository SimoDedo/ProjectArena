using System;
using System.IO;

public class SLMapManager : MapManager {

    private char[,] map;

    protected override void InitializeAll() {
        mapAssemblerScript = mapAssembler.GetComponent<MapAssebler>();
        mapGeneratorScript = mapGenerator.GetComponent<MapGenerator>();
        objectDisplacerScript = objectDisplacer.GetComponent<ObjectDisplacer>();
    }

    public override void ManageMap(bool assembleMap) {
        if (loadMapFromFile) {
            // Load the map.
            LoadMapFromText();
        } else {
            // Generate the map.
            if (GetParameterManager() != null)
                map = mapGeneratorScript.GenerateMap(seed, export, exportPath);
            else
                map = mapGeneratorScript.GenerateMap();
        }

        if (assembleMap) {
            // Assemble the map.
            mapAssemblerScript.AssembleMap(map, mapGeneratorScript.GetWallChar(), mapGeneratorScript.GetRoomChar(), squareSize, heigth);
            // Displace the objects.
            objectDisplacerScript.DisplaceObjects(map, squareSize, heigth);
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
                string[] lines = File.ReadAllLines(@textFilePath);

                map = new char[lines[0].Length, lines.GetLength(0)];

                for (int x = 0; x < map.GetLength(0); x++) {
                    for (int y = 0; y < map.GetLength(1); y++) {
                        map[x, y] = lines[y][x];
                    }
                }
            } catch (Exception) {
                GetParameterManager().ErrorBackToMenu(-1);
            }
        }
    }

}