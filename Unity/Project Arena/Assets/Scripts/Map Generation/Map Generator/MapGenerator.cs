using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class MapGenerator : CoreComponent {

    // Do I have to generate my seed?
    [Header("Seed")] [SerializeField] protected bool useRandomSeed = true;
    // Seed used to generate the map.
    [SerializeField] protected string seed = null;

    // Map width.
    [Header("Generation")] [SerializeField] protected int width = 100;
    // Map height.
    [SerializeField] protected int height = 100;
    // Wall height.
    [SerializeField] protected float wallHeight = 5f;
    // Square size.
    [SerializeField] protected float squareSize = 1f;
    // Minimum distance of an object w.r.t another object.
    [SerializeField] protected int objectToObjectDistance = 5;
    // Minimum distance of an object w.r.t a wall.
    [SerializeField,] protected int objectToWallDistance = 2;
    // Border size.
    [SerializeField] protected int borderSize = 5;

    // Char that denotes a room;
    [Header("Representation")] [SerializeField] protected char roomChar = 'r';
    // Char that denotes a wall;
    [SerializeField] protected char wallChar = 'w';
    // Custom objects that will be added to the map.
    [SerializeField] protected MapObject[] mapObjects;

    // Do I have to create a .txt output?
    [Header("Export")] [SerializeField] protected bool createTextFile = false;
    // Path where to save the text map.
    [SerializeField] protected string textFilePath = null;

    // Map, defined as a grid of chars.
    protected char[,] map;
    // Hash of the seed.
    protected int hash;
    // Pseudo random generator.
    protected System.Random pseudoRandomGen;

    protected int originalWidth = 0;
    protected int originalHeight = 0;

    // Initializes the pseudo random generator.
    protected void InitializePseudoRandomGenerator() {
        if (useRandomSeed)
            seed = GetDateString();

        hash = seed.GetHashCode();
        pseudoRandomGen = new System.Random(hash);
    }

    // Sets the parmaters, generates a map and returns it.
    public char[,] GenerateMap(string s, bool ctf, string e) {
        useRandomSeed = false;
        seed = s;

        createTextFile = ctf;
        textFilePath = e;

        return GenerateMap();
    }

    // Generates the map and returns it.
    public abstract char[,] GenerateMap();

    // Adds custom objects to the map. I erode the map once, then I place the objects that don't have placement restriction 
    // (I erode once so that they want compenetrate che walls). Then I erode as many time as needed and I place the other
    // objects.
    protected void PopulateMap() {
        if (mapObjects.Length > 0) {
            char[,] restrictedMap = map.Clone() as char[,];

            if (objectToWallDistance > 0)
                ErodeMap(restrictedMap);
            List<Coord> roomTiles = GetFreeTiles(restrictedMap);

            bool mustRestrictFurther = false;

            // Place the unrestricted objects.
            foreach (MapObject o in mapObjects) {
                if (o.placeAnywere) {
                    for (int i = 0; i < o.numObjPerMap; i++) {
                        if (roomTiles.Count > 0) {
                            int selected = pseudoRandomGen.Next(0, roomTiles.Count);
                            map[roomTiles[selected].tileX, roomTiles[selected].tileY] = o.objectChar;
                            // Make unavailable the cells around for the restricted objects.
                            DrawCircle(roomTiles[selected].tileX, roomTiles[selected].tileY, 1, restrictedMap, wallChar);
                            roomTiles.RemoveAt(selected);
                        } else {
                            ManageError(Error.SOFT_ERROR, "Error while populating the map, no more free tiles are availabe.");
                            return;
                        }
                    }
                } else if (mustRestrictFurther == false)
                    mustRestrictFurther = true;
            }

            // Restrict again if there are object that need a further restriction.
            if (objectToWallDistance > 1 && mustRestrictFurther) {
                for (int i = 1; i < objectToWallDistance; i++) {
                    ErodeMap(restrictedMap);
                }
                roomTiles = GetFreeTiles(restrictedMap);
            } else {
                roomTiles = GetFreeTiles(restrictedMap);
            }

            // Place the restricted objects.
            foreach (MapObject o in mapObjects) {
                if (!o.placeAnywere) {
                    for (int i = 0; i < o.numObjPerMap; i++) {
                        if (roomTiles.Count > 0) {
                            int selected = pseudoRandomGen.Next(0, roomTiles.Count);
                            map[roomTiles[selected].tileX, roomTiles[selected].tileY] = o.objectChar;
                            // Remove the tiles are around the choosen one from the vailable ones.
                            DrawCircle(roomTiles[selected].tileX, roomTiles[selected].tileY, objectToObjectDistance, restrictedMap, wallChar);
                            roomTiles = GetFreeTiles(restrictedMap);
                        } else {
                            ManageError(Error.SOFT_ERROR, "Error while populating the map, no more free tiles are availabe.");
                            return;
                        }
                    }
                }
            }
        }
    }

    // Erodes the map as many times as specified.
    protected void ErodeMap(char[,] toBeErodedMap) {
        char[,] originalMap = CloneMap(toBeErodedMap);

        for (int x = 0; x < originalMap.GetLength(0); x++) {
            for (int y = 0; y < originalMap.GetLength(1); y++) {
                if (GetSurroundingWallCount(x, y, originalMap) > 0) {
                    toBeErodedMap[x, y] = wallChar;
                }
            }
        }
    }

    // Gets the number of walls surrounding a cell.
    protected int GetSurroundingWallCount(int gridX, int gridY, char[,] gridMap) {
        int wallCount = 0;

        // Loop on 3x3 grid centered on [gridX, gridY].
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {
                if (IsInMapRange(neighbourX, neighbourY)) {
                    if (neighbourX != gridX || neighbourY != gridY)
                        wallCount += GetMapTileAsNumber(neighbourX, neighbourY, gridMap);
                } else
                    wallCount++;
            }
        }

        return wallCount;
    }

    // Clones a map.
    protected char[,] CloneMap(char[,] toBeClonedMap) {
        char[,] clonedMap = new char[toBeClonedMap.GetLength(0), toBeClonedMap.GetLength(1)];

        for (int x = 0; x < toBeClonedMap.GetLength(0); x++) {
            for (int y = 0; y < toBeClonedMap.GetLength(1); y++) {
                clonedMap[x, y] = toBeClonedMap[x, y];
            }
        }

        return clonedMap;
    }

    // Returns a list of the free tiles.
    protected List<Coord> GetFreeTiles(char[,] m) {
        List<Coord> roomTiles = new List<Coord>();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (m[x, y] == roomChar)
                    roomTiles.Add(new Coord(x, y));
            }
        }

        return roomTiles;
    }

    // Draws a circe of a given radius around a point.
    protected void DrawCircle(int centerX, int centerY, int r, char[,] m, char t) {
        for (int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) {
                if (x * x + y * y <= r * r) {
                    int drawX = centerX + x;
                    int drawY = centerY + y;

                    if (IsInMapRange(drawX, drawY))
                        m[drawX, drawY] = t;
                }
            }
        }
    }

    // Converts coordinates to world position.
    protected Vector3 CoordToWorldPoint(Coord tile) {
        return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
    }

    // Tells if the "general" (full/room) type of two tiles is the same.
    protected bool IsSameGeneralType(char tyleType, char t) {
        if (tyleType == wallChar)
            return t == wallChar;
        else
            return t != wallChar;
    }

    // Tells if a tile is in the map.
    protected bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Return 1 if the tile is a wall, 0 otherwise.
    protected int GetMapTileAsNumber(int x, int y, char[,] gridMap) {
        if (gridMap[x, y] == wallChar)
            return 1;
        else
            return 0;
    }

    // Saves the map in a text file.
    protected void SaveMapAsText() {
        if (textFilePath == null && !Directory.Exists(textFilePath)) {
            Debug.LogError("Error while retrieving the folder, please insert a valid path.");
        } else {
            try {
                string textMap = "";

                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        textMap = textMap + map[x, y];
                    }
                    if (x < width - 1)
                        textMap = textMap + "\n";
                }

                System.IO.File.WriteAllText(@textFilePath + "/" + seed.ToString() + "_map.txt", textMap);
            } catch (Exception) {
                Debug.LogError("Error while saving the map, please insert a valid path and check its permissions.");
            }
        }
    }

    // Gets the current date as string.
    protected string GetDateString() {
        return System.DateTime.Now.ToString();
    }

    // Returns the maximum map size.
    protected int GetMapSize() {
        if (width > height)
            return width;
        else
            return height;
    }

    // Adds borders to the map.
    protected void AddBorders() {
        char[,] borderedMap = new char[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++) {
            for (int y = 0; y < borderedMap.GetLength(1); y++) {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                else
                    borderedMap[x, y] = wallChar;
            }
        }

        map = borderedMap;

        width = borderedMap.GetLength(0);
        height = borderedMap.GetLength(1);
    }

    // Fills the map with wall cells.
    protected void FillMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                map[x, y] = wallChar;
            }
        }
    }

    // Saves the original size of the map.
    public void SaveMapSize() {
        originalWidth = width;
        originalHeight = height;
    }

    // Resets the size of the map.
    public void ResetMapSize() {
        width = originalWidth;
        height = originalHeight;
    }

    public bool GetRandomBoolean() {
        return (pseudoRandomGen.Next(100) < 50) ? true : false;
    }

    public int GetRandomInteger() {
        return pseudoRandomGen.Next();
    }

    public int GetRandomInteger(int min, int max) {
        return pseudoRandomGen.Next(min, max);
    }

    public char GetWallChar() {
        return wallChar;
    }

    public char GetRoomChar() {
        return roomChar;
    }

    public float GetSquareSize() {
        return squareSize;
    }

    public float GetWallHeight() {
        return wallHeight;
    }

    // Coordinates of a tile.
    protected struct Coord {
        public int tileX;
        public int tileY;

        public Coord(int x, int y) {
            tileX = x;
            tileY = y;
        }
    }

    // Informations about an object. 
    [Serializable]
    protected struct MapObject {
        // Character which defines the object.
        public char objectChar;
        // Number of objects to be put in the map.
        public int numObjPerMap;
        // The object must respect placement restrictions?
        public bool placeAnywere;
    }

}