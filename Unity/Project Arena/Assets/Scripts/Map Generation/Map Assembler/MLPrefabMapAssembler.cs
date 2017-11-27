using System;
using System.Collections.Generic;
using UnityEngine;

public class MLPrefabMapAssembler : PrefabMapAssembler {

    [SerializeField] private GameObject floorPrefab;

    // Maps.
    private List<char[,]> maps;
    // Char that denotes a void tile.
    private char voidChar;
    // Height of each level.
    private float levelHeight;
    // Comulative mask.
    private char[,] comulativeMask;

    void Start() {
        SetReady(true);
    }

    public override void AssembleMap(List<char[,]> ms, char wChar, char rChar, char vChar, float squareSize, float h) {
        wallChar = wChar;
        roomChar = rChar;
        voidChar = vChar;
        levelHeight = h;
        width = ms[0].GetLength(0);
        height = ms[0].GetLength(1);
        maps = ms;

        InitializeComulativeMask();

        // Process all the tiles.
        ProcessTiles();

        for (int i = 0; i < maps.Count; i++) {
            UpdateComulativeMask(i);
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (maps[i][x, y] != wallChar && maps[i][x, y] != voidChar) {
                        string currentMask = GetComulativeNeighbourhoodMask(x, y);
                        foreach (ProcessedTilePrefab p in processedTilePrefabs) {
                            if (p.mask == currentMask) {
                                // AddPrefab(p.prefab, x, y, squareSize, p.rotation, levelHeight * i);
                                AddWallRecursevely(i, x, y, squareSize, p);
                                break;
                            }
                        }
                        AddPrefab(floorPrefab, x, y, squareSize, 0, levelHeight * i);
                    }
                }
            }
        }
    }

    private void InitializeComulativeMask() {
        comulativeMask = new char[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                comulativeMask[x, y] = wallChar;
    }

    private void UpdateComulativeMask(int currentLevel) {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (maps[currentLevel][x, y] != wallChar)
                    comulativeMask[x, y] = roomChar;
    }

    public override void AssembleMap(char[,] m, char wChar, char rChar, float squareSize, float prefabHeight) { }

    // Adds a wall recursively.
    private void AddWallRecursevely(int currentLevel, int x, int y, float squareSize, ProcessedTilePrefab p) {
        if (currentLevel != maps.Count - 1) {
            for (int i = currentLevel + 1; i < maps.Count; i++) {
                if (maps[i][x, y] == wallChar || maps[i][x, y] == voidChar)
                    AddPrefab(p.prefab, x, y, squareSize, p.rotation, levelHeight * i);
                else
                    break;
            }
        }
        AddPrefab(p.prefab, x, y, squareSize, p.rotation, levelHeight * currentLevel);
    }

    // Gets the coumlative neighbours of a cell as a mask.
    private string GetComulativeNeighbourhoodMask(int gridX, int gridY) {
        char[] mask = new char[4];
        mask[0] = comulativeMask[gridX, gridY + 1];
        mask[1] = comulativeMask[gridX + 1, gridY];
        mask[2] = comulativeMask[gridX, gridY - 1];
        mask[3] = comulativeMask[gridX - 1, gridY];
        return new string(mask);
    }

    // Gets the neighbours of a cell as a mask.
    private string GetNeighbourhoodMask(int level, int gridX, int gridY) {
        char[] mask = new char[4];
        mask[0] = GetTileChar(level, gridX, gridY + 1);
        mask[1] = GetTileChar(level, gridX + 1, gridY);
        mask[2] = GetTileChar(level, gridX, gridY - 1);
        mask[3] = GetTileChar(level, gridX - 1, gridY);
        return new string(mask);
    }

    // Returns the char of a tile.
    private char GetTileChar(int level, int x, int y) {
        if (IsInMapRange(x, y))
            return maps[level][x, y] == wallChar ? wallChar : roomChar;
        else
            return wallChar;
    }

}
