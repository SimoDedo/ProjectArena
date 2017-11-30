using System;
using System.Collections.Generic;
using UnityEngine;

public class ABMLMapManager : MLMapManager {

    [Header("AB generation")] [SerializeField] private MapGenerator diggerGeneratorScript;

    // Does at least one genome need the digger generator?
    private bool usingDiggerGenerator = false;

    public override void ManageMap(bool assembleMap) {
        if (loadMapFromFile) {
            // Load the map.
            LoadMapFromText();
        } else {
            // Extract the genomes.
            List<Genome> genomes = ExtractGenomes(seed);
            // Generate the map.
            foreach (Genome g in genomes) {
                mapGeneratorScript.ResetMapSize();
                if (GetParameterManager() != null) {
                    if (g.useDefaultGenerator)
                        maps.Add(mapGeneratorScript.GenerateMap(g.genome, false, null));
                    else
                        maps.Add(diggerGeneratorScript.GenerateMap(g.genome, false, null));
                } else
                    maps.Add(mapGeneratorScript.GenerateMap());
                mapGeneratorScript.ResetMapSize();
            }
            // Resize all the maps.
            ResizeAllMaps();
            // Add the stairs.
            if (!usingDiggerGenerator) {
                stairsGeneratorScript.GenerateStairs(maps, mapGeneratorScript);
            } else { }
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

    // Resizes all the maps.
    private void ResizeAllMaps() {
        int maxWidth = 0;
        int maxHeight = 0;

        foreach (char[,] m in maps) {
            if (m.GetLength(0) > maxWidth)
                maxWidth = m.GetLength(0);
            if (m.GetLength(1) > maxHeight)
                maxHeight = m.GetLength(1);
        }

        for (int i = 0; i < maps.Count; i++) {
            if (maps[i].GetLength(0) < maxWidth || maps[i].GetLength(1) < maxHeight)
                maps[i] = ResizeMap(maps[i], maxWidth, maxHeight);
        }
    }

    // Resizes a map.
    private char[,] ResizeMap(char[,] map, int maxWidth, int maxHeight) {
        char[,] resizedMap = new char[maxWidth, maxHeight];

        char wallChar = mapGeneratorScript.GetWallChar();

        for (int x = 0; x < maxWidth; x++) {
            for (int y = 0; y < maxHeight; y++) {
                if (x < map.GetLength(0) && y < map.GetLength(1))
                    resizedMap[x, y] = map[x, y];
                else
                    resizedMap[x, y] = wallChar;
            }
        }

        return resizedMap;
    }

    // Extracts the genomes from the seed.
    private List<Genome> ExtractGenomes(string seed) {
        List<Genome> genomes = new List<Genome>();

        string processedString = "";
        int genesCount = 1;

        for (int i = 0; i < seed.Length; i++) {
            if (i < seed.Length - 1 && seed[i] == '|' && seed[i + 1] == '|') {
                genomes.Add(CreateNewGenome(processedString, genesCount));
                processedString = "";
                genesCount = 1;
                i++;
            } else if (i == seed.Length - 1) {
                genomes.Add(CreateNewGenome(processedString + seed[i], genesCount));
            } else if (seed[i] == ',') {
                processedString += seed[i];
                genesCount++;
            } else if (seed[i] == '<') {
                processedString += seed[i];
                genesCount = 1;
            } else {
                processedString += seed[i];
            }
        }

        return genomes;
    }

    private Genome CreateNewGenome(string s, int g) {
        return new Genome {
            genome = s,
            useDefaultGenerator = (g == 5) ? false : true
        };
    }

    private struct Genome {
        public string genome;
        public bool useDefaultGenerator;
    }

}
