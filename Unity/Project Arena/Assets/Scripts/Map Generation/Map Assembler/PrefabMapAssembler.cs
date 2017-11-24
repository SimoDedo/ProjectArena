using System;
using System.Collections.Generic;
using UnityEngine;

public class PrefabMapAssembler : MapAssebler {

    // Ceil height.
    [SerializeField] private float ceilHeight = 0;
    // Floor height.
    [SerializeField] private float floorHeight = 0;
    // Rotation correction angle.
    [SerializeField] private int rotationCorrection = 0;
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

    private MeshCollider floorCollider;
    private MeshCollider ceilCollider;

    void Start() {
        GameObject childObject;

        childObject = new GameObject("Floor - Collider");
        childObject.transform.parent = transform;
        childObject.transform.localPosition = Vector3.zero;
        floorCollider = childObject.AddComponent<MeshCollider>();

        childObject = new GameObject("Ceil - Collider");
        childObject.transform.parent = transform;
        childObject.transform.localPosition = Vector3.zero;
        ceilCollider = childObject.AddComponent<MeshCollider>();

        SetReady(true);
    }

    public override void AssembleMap(char[,] m, char wChar, char rChar, float squareSize, float prefabHeight, bool generateMeshes) {
        wallChar = wChar;
        roomChar = rChar;
        width = m.GetLength(0);
        height = m.GetLength(1);
        map = m;

        // Process all the tiles.
        ProcessTiles();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x, y] != wallChar) {
                    string currentMask = GetNeighbourhoodMask(x, y);
                    foreach (ProcessedTilePrefab p in processedTilePrefabs) {
                        if (p.mask == currentMask)
                            AddPrefab(p.prefab, x, y, squareSize, p.rotation, prefabHeight);
                    }
                }
            }
        }

        if (generateMeshes) {
            floorCollider.sharedMesh = CreateFlatMesh(width, height, squareSize, prefabHeight + floorHeight, false);
            ceilCollider.sharedMesh = CreateFlatMesh(width, height, squareSize, prefabHeight + ceilHeight, true);
        }
    }

    public override void AssembleMap(char[,] m, char wChar, char rChar, float squareSize, float floorHeight) {
        AssembleMap(m, wChar, rChar, squareSize, floorHeight, true);
    }

    // Adds a prefab to the map.
    private void AddPrefab(GameObject gameObject, int x, int y, float squareSize, float rotation, float prefabHeight) {
        GameObject childObject = (GameObject)Instantiate(gameObject);
        childObject.name = gameObject.name;
        childObject.transform.parent = transform;
        childObject.transform.position = new Vector3(squareSize * (x - width / 2), prefabHeight, squareSize * (y - height / 2));
        childObject.transform.eulerAngles = new Vector3(0, rotation, 0);
    }

    // For each tile, converts its binary mask into a char array and creates three rotated copies.
    private void ProcessTiles() {
        processedTilePrefabs = new List<ProcessedTilePrefab>();

        // For each tile create three rotated copies.
        foreach (TilePrefab t in tilePrefabs) {
            string convertedMask = ConvertMask(t.binaryMask);
            processedTilePrefabs.Add(new ProcessedTilePrefab(convertedMask, t.prefab, rotationCorrection));
            // Debug.Log("Added mask " + convertedMask + ".");
            if (t.binaryMask != "0000" && t.binaryMask != "1111") {
                for (int i = 1; i < 4; i++) {
                    convertedMask = CircularShiftMask(convertedMask);
                    processedTilePrefabs.Add(new ProcessedTilePrefab(convertedMask, t.prefab, 90 * i + rotationCorrection));
                    // Debug.Log("Added mask " + convertedMask + ".");
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

    // Creates a flat mesh.
    private Mesh CreateFlatMesh(int sizeX, int sizeY, float squareSize, float height, bool inverted) {
        Mesh flatMesh = new Mesh();

        Vector3[] floorVertices = new Vector3[4];

        floorVertices[0] = new Vector3(-sizeX / 2 * squareSize, height, -sizeY / 2 * squareSize);
        floorVertices[1] = new Vector3(-sizeX / 2 * squareSize, height, sizeY / 2 * squareSize);
        floorVertices[2] = new Vector3(sizeX / 2 * squareSize, height, -sizeY / 2 * squareSize);
        floorVertices[3] = new Vector3(sizeX / 2 * squareSize, height, sizeY / 2 * squareSize);

        int[] floorTriangles;

        if (inverted)
            floorTriangles = new int[] { 3, 1, 2, 2, 1, 0 };
        else
            floorTriangles = new int[] { 0, 1, 2, 2, 1, 3 };

        flatMesh.vertices = floorVertices;
        flatMesh.triangles = floorTriangles;
        flatMesh.RecalculateNormals();

        return flatMesh;
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