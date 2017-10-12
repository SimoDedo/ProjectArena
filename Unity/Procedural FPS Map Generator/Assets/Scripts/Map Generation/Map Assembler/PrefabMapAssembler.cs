using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabMapAssembler : MapAssebler {

    // List of prefabs.
    [SerializeField] private List<TilePrefab> tilePrefabs;

    // List of processed prefabs.
    private List<ProcessedTilePrefab> processedTilePrefabs;
    // Char that denotes a wall tile.
    private char wallChar;
    // Char that denotes a room tile.
    private char roomChar;
    // Map width.
    private int width;
    // Map heigth.
    private int height;
    // Map.
    char[,] map;

    void Start() {
        SetReady(true);
    }

    public override void AssembleMap(char[,] m, char wChar, char rChar, float squareSize, float h) {
        wallChar = wChar;
        roomChar = rChar;
        width = m.GetLength(0);
        height = m.GetLength(1);
        map = m;

        // Process all the tiles.
        ProcessTiles();

        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(0); y++) {
                if (map[x, y] == rChar) {
                    string currentMask = GetNeighbourhoodMask(x, y);
                    foreach (ProcessedTilePrefab p in processedTilePrefabs) {
                        if (p.mask == currentMask)
                            AddPrefab(p.prefab, x, y, squareSize, h);
                    }
                }
            }
        }
    }

    // Adds a prefab to the map.
    private void AddPrefab(GameObject gameObject, int x, int y, float squareSize, float h) {
        GameObject childObject = (GameObject)Instantiate(gameObject);
        childObject.name = gameObject.name;
        childObject.transform.parent = transform;
        childObject.transform.localPosition = new Vector3(x * squareSize - width / 2, h, y * squareSize - height / 2);
    }

    // For each tile, converts its binary mask into a char array and creates three rotated copies.
    private void ProcessTiles() {
        processedTilePrefabs = new List<ProcessedTilePrefab>();

        // For each tile create three rotated copies.
        foreach (TilePrefab t in tilePrefabs) {
            string convertedMask = ConvertMask(t.binaryMask);
            processedTilePrefabs.Add(new ProcessedTilePrefab(convertedMask, t.prefab, 0));
            Debug.Log("Added mask " + convertedMask + ".");
            if (t.binaryMask != "0000" && t.binaryMask != "1111") {
                for (int i = 1; i < 4; i++) {
                    convertedMask = CircularShiftMask(convertedMask);
                    processedTilePrefabs.Add(new ProcessedTilePrefab(convertedMask, t.prefab, 90 * i));
                    Debug.Log("Added mask " + convertedMask + ".");
                }
            }
        }
    }

    // Converts the mask form a binary string to char array.
    private string ConvertMask(string binaryMask) {
        binaryMask = binaryMask.Replace('0', roomChar);
        binaryMask = binaryMask.Replace('1', wallChar);
        return binaryMask;
    }

    // Performs a circular shift of the mask.
    private string CircularShiftMask(string mask) {
        char[] shiftedMask = { mask[3], mask[0], mask[1], mask[2] };
        return new string(shiftedMask);
    }

    // Gets the neighbours of a cell as a mask.
    private string GetNeighbourhoodMask(int gridX, int gridY) {
        char[] mask = new char[4];
        mask[0] = GetTileChar(gridX, gridY + 1);
        mask[1] = GetTileChar(gridX + 1, gridY);
        mask[2] = GetTileChar(gridX, gridY - 1);
        mask[3] = GetTileChar(gridX - 1, gridY);
        return new string(mask);
    }

    // Returns the char of a tile.
    private char GetTileChar(int x, int y) {
        if (IsInMapRange(x, y))
            return map[x, y] == wallChar ? wallChar : roomChar;
        else
            return wallChar;
    }

    // Tells if a tile is in the map.
    private bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Custom prefab. 
    [Serializable]
    private struct TilePrefab {
        // Mask of the tile.
        public string binaryMask;
        // Prefab of the tile.
        public GameObject prefab;
    }

    // Custom processed prefab. 
    [Serializable]
    public class ProcessedTilePrefab {
        // Mask of the tile.
        public string mask;
        // Prefab of the tile.
        public GameObject prefab;
        // Rotation
        public int rotation;

        public ProcessedTilePrefab(string m, GameObject p, int r) {
            mask = m;
            prefab = p;
            rotation = r;
        }
    }

}