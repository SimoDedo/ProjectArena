using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour {

    // Do I have to generate my seed?
    [SerializeField] private bool useRandomSeed = true;
    // Seed used to generate the map.
    [SerializeField] private string seed = null;

    // Map width.
    [SerializeField] private int width = 100;
    // Map height.
    [SerializeField] private int height = 100;
    // Border size.
    [SerializeField] private int borderSize = 5;
    // Passage width.
    [SerializeField] private int passageWidth = 5;
    // Wall height.
    [SerializeField] private float wallHeight = 5f;
    // Wall height.
    [SerializeField] private float squareSize = 1f;
    // Minimum size of a wall region.
    [SerializeField] private int wallThresholdSize = 50;
    // Minimum size of a room region.
    [SerializeField] private int roomThresholdSize = 50;
    // How much the map will be randomly filled at the beginning.
    [SerializeField, Range(0, 100)] private int ramdomFillPercent = 50;
    // Number of smoothing iterations to be done.
    [SerializeField, Range(0, 3)] private int smoothingIterations = 3;
    // You must have more than this number of neighbour to became wall.
    [SerializeField, Range(0, 9)] private int neighbourTileLimitHigh = 4;
    // You must have less than this number of neighbour to became room.
    [SerializeField, Range(0, 9)] private int neighbourTileLimitLow = 4;
    // Do I have to use custom rules when the neighbour count falls outside the limits?
    /* [SerializeField] private bool useCustomRules = false; */
    // List of rules. Each rule is a string which decribes the neighbour tiles in the order 1a, 1b, 1c, 2a, 2c, 3a, 3b, 3c.
    // Rules are binary strings of length 8 and specified only for 0 cases.
    /* [SerializeField] private string[] binaryMasks; */

    // Char that denotes a room;
    [SerializeField] private char roomChar = 'v';
    // Char that denotes a wall;
    [SerializeField] private char wallChar = 'w';
    // Custom objects that will be added to the map.
    [SerializeField] private MapObject[] mapObjects;

    // Do I have to create a  mesh representation?
    [SerializeField] private bool createMesh = false;
    // Object containing the Map Builder script.
    [SerializeField] private GameObject mapBuilder;
    // Object containing the Object Displacer script.
    [SerializeField] private GameObject objectDisplacer;

    // Do I have to create a .txt output?
    [SerializeField] private bool createTextFile = false;
    // Path where to save the text map.
    [SerializeField] private string textFilePath = null;

    // Map, defined as a grid of chars.
    private char[,] map;
    // Hash of the seed.
    private int hash;
    // List of arrays containing character masks.
    /* private List<char[]> characterMasks; */

    void Start() {
        GenerateMap();
    }

    /* void Update() {
        if (Input.GetMouseButtonDown(0))
            GenerateMap();
    } */

    // Generates the map.
    private void GenerateMap() {
        map = new char[width, height];

        /* if (useCustomRules)
            TranslateMasks(); */

        RandomFillMap();

        for (int i = 0; i < smoothingIterations; i++)
            SmoothMap();

        ProcessMap();

        char[,] borderedMap = new char[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++) {
            for (int y = 0; y < borderedMap.GetLength(1); y++) {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    borderedMap[x, y] = map[x - borderSize, y - borderSize];
                else
                    borderedMap[x, y] = wallChar;
            }
        }

        if (createMesh) {
            if (mapBuilder != null) {
                IMapBuilderFromText mapBuilderFromTextScript = mapBuilder.GetComponent<IMapBuilderFromText>();
                mapBuilderFromTextScript.BuildMap(borderedMap, wallChar, squareSize, wallHeight);
            } else
                Debug.LogError("Error while trying to display the map, no Map Builder is attached to the script.");
            if (objectDisplacer != null) {
                ObjectDisplacer objectDisplacerScript = objectDisplacer.GetComponent<ObjectDisplacer>();
                objectDisplacerScript.DisplaceObjects(borderedMap, squareSize, wallHeight);
            } else
                Debug.LogError("Error while trying to displace the objects map, no Object Displacer is attached to the script.");
        }

        if (createTextFile)
            SaveMapAsText();
    }

    // Checks if the masks are valid and translates them from binary strings to arrays of characters.
    /* private void TranslateMasks() {
        characterMasks = new List<char[]>();

        for (int i = 0; i < binaryMasks.GetLength(0); i++) {
            if (binaryMasks[i].Length != 8) {
                Debug.LogError("Error while translating the mask " + i + ", masks must be binary strings of length 8. Further masks will be ignored.");
                useCustomRules = false;
                return;
            } else {
                characterMasks.Add(new char[8]);

                for (int j = 0; j < 8; j++) {
                    char currentChar = binaryMasks[i][j];

                    if (currentChar == '0') {
                        characterMasks[i][j] = roomChar;
                    } else if (currentChar == '1') {
                        characterMasks[i][j] = wallChar;
                    } else {
                        Debug.LogError("Error while translating the character " + currentChar + ", masks must be binary strings of length 8. Further masks will be ignored.");
                        useCustomRules = false;
                        return;
                    }
                }
            }
        }
    } */

    // Processes the map.
    private void ProcessMap() {
        List<List<Coord>> wallRegions = GetRegions(wallChar);

        foreach (List<Coord> wallRegion in wallRegions) {
            if (wallRegion.Count < wallThresholdSize) {
                foreach (Coord tile in wallRegion) {
                    map[tile.tileX, tile.tileY] = roomChar;
                }
            }
        }

        List<List<Coord>> roomRegions = GetRegions(roomChar);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions) {
            if (roomRegion.Count < roomThresholdSize) {
                foreach (Coord tile in roomRegion) {
                    map[tile.tileX, tile.tileY] = wallChar;
                }
            } else {
                survivingRooms.Add(new Room(roomRegion, map, wallChar));
            }
        }

        // If there are at least two rooms.
        if (survivingRooms.Count > 0) {
            survivingRooms.Sort();
            survivingRooms[0].isMainRoom = true;
            survivingRooms[0].isAccessibleFromMainRoom = true;

            ConnectClosestRooms(survivingRooms);
        }

        PopulateMap();
    }

    // Connects each room which the closest one.
    private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {
        // Accessible rooms.
        List<Room> roomListA = new List<Room>();
        // Not accessible rooms.
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom) {
            foreach (Room room in allRooms) {
                if (room.isAccessibleFromMainRoom)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
            }
        } else {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA) {
            if (!forceAccessibilityFromMainRoom) {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                    continue;
            }

            foreach (Room roomB in roomListB) {
                if (roomA == roomB || roomA.IsConnected(roomB))
                    continue;

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }
        if (!forceAccessibilityFromMainRoom) {
            ConnectClosestRooms(allRooms, true);
        }
    }

    // Creates a passage between two rooms.
    private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB) {
        Room.ConnectRooms(roomA, roomB);

        List<Coord> line = GetLine(tileA, tileB);

        foreach (Coord c in line)
            DrawCircle(c, passageWidth);
    }

    // Adds custom objects to the map.
    private void PopulateMap() {
        if (mapObjects.Length > 0) {
            List<Coord> roomTiles = GetFreeTiles();

            foreach (MapObject o in mapObjects) {
                for (int i = 0; i < o.numObjPerMap; i++) {
                    if (roomTiles.Count > 0) {
                        int selected = UnityEngine.Random.Range(0, roomTiles.Count);
                        map[roomTiles[selected].tileX, roomTiles[selected].tileY] = o.objectChar;
                        roomTiles.RemoveAt(selected);
                    } else {
                        Debug.LogError("Error while populating the map, no more free tiles are availabe.");
                        return;
                    }
                }
            }
        }
    }

    // Returns a list of the free tiles.
    private List<Coord> GetFreeTiles() {
        List<Coord> roomTiles = new List<Coord>();

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (map[x, y] == roomChar)
                    roomTiles.Add(new Coord(x, y));
            }
        }

        return roomTiles;
    }

    // Draws a circe of a given radius around a point.
    private void DrawCircle(Coord c, int r) {
        for (int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) {
                if (x * x + y * y <= r) {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;

                    if (IsInMapRange(drawX, drawY))
                        map[drawX, drawY] = roomChar;
                }
            }
        }
    }

    // Returns a list of coordinates for each point in the line.
    private List<Coord> GetLine(Coord from, Coord to) {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest) {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++) {
            line.Add(new Coord(x, y));

            if (inverted) {
                y += step;
            } else {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest) {
                if (inverted) {
                    x += gradientStep;
                } else {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    // Converts coordinates to world position.
    private Vector3 CoordToWorldPoint(Coord tile) {
        return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
    }

    // Given a certain "general" (full/room) tile type it returns all the regions of that type.
    private List<List<Coord>> GetRegions(char tileType) {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (mapFlags[x, y] == 0 && IsSameGeneralType(tileType, map[x, y])) {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion) {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    // Return the tiles of the region the parameter coordinates belong too using the flood-fill algorithm.
    private List<Coord> GetRegionTiles(int startX, int startY) {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        char tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX)) {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType) {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }

        return tiles;
    }

    // Tells if the "general" (full/room) type of two tiles is the same.
    private bool IsSameGeneralType(char tyleType, char t) {
        if (tyleType == wallChar)
            return t == wallChar;
        else
            return t != wallChar;
    }

    // Tells if a tile is in the map.
    private bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Randomly fills the map based on a seed.
    private void RandomFillMap() {
        if (useRandomSeed)
            seed = GetDateString();

        hash = seed.GetHashCode();

        System.Random pseudoRandomGen = new System.Random(hash);

        // Loop on each tile and assign a value;
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    map[x, y] = wallChar;
                else
                    map[x, y] = (pseudoRandomGen.Next(0, 100) < ramdomFillPercent) ? wallChar : roomChar;
            }
        }
    }

    // Smooths the map.
    private void SmoothMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                /* char[] neighbours = GetNeighbours(x, y);
                int neighbourWallTiles = GetNeighboursWallCount(neighbours); */
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > neighbourTileLimitHigh)
                    map[x, y] = wallChar;
                else if (neighbourWallTiles < neighbourTileLimitLow)
                    map[x, y] = roomChar;
                /* else if (useCustomRules)
                [x, y] = ApplyMasks(x, y, neighbours); */
            }
        }
    }

    // Gets the number of walls surrounding a cell.
    private int GetSurroundingWallCount(int gridX, int gridY) {
        int wallCount = 0;

        // Loop on 3x3 grid centered on [gridX, gridY].
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {
                if (IsInMapRange(neighbourX, neighbourY)) {
                    if (neighbourX != gridX || neighbourY != gridY)
                        wallCount += getMapTileAsNumber(neighbourX, neighbourY);
                } else
                    wallCount++;
            }
        }

        return wallCount;
    }

    // Returns a room tile if a match is found, a wall tile otherwise.
    /* private char ApplyMasks(int gridX, int gridY, char[] neighbours) {
        foreach (char[] mask in characterMasks) {
            int matchCount = 0;

            for (int i = 0; i < 8; i++) {
                if (mask[i] == neighbours[i])
                    matchCount++;
            }

            if (matchCount == 8)
                return roomChar;
        }

        return wallChar;
    } */

    // Gets the number of walls surrounding a cell.
    /* private int GetNeighboursWallCount(char[] neighbours) {
        int wallCount = 0;

        for (int i = 0; i < 8; i++) {
            if (neighbours[i] == wallChar)
                wallCount++;
        }

        return wallCount;
    } */

    // Gets the neighbours of a cell.
    /* private char[] GetNeighbours(int gridX, int gridY) {
        char[] neighbours = new char[8];
        int i = 0;

        // Loop on 3x3 grid centered on [gridX, gridY].
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++) {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++) {
                if (IsInMapRange(neighbourX, neighbourY)) {
                    if (neighbourX != gridX || neighbourY != gridY) {
                        if (map[neighbourX, neighbourY] == roomChar)
                            neighbours[i] = roomChar;
                        else
                            neighbours[i] = wallChar;
                        i++;
                    }
                } else {
                    neighbours[i] = wallChar;
                    i++;
                }
            }
        }

        return neighbours;
    } */

    // Coordinates of a tile.
    private struct Coord {
        public int tileX;
        public int tileY;

        public Coord(int x, int y) {
            tileX = x;
            tileY = y;
        }
    }

    // Return 1 if the tile is a wall, 0 otherwise.
    private int getMapTileAsNumber(int x, int y) {
        if (map[x, y] == wallChar)
            return 1;
        else
            return 0;
    }

    // Stores all information about a room.
    private class Room : IComparable<Room> {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room() {
        }

        public Room(List<Coord> roomTiles, char[,] map, char wallChar) {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();
            edgeTiles = new List<Coord>();

            // For each tile of the room I get the neighbours that are walls obtaining the edge of the room.
            foreach (Coord tile in tiles) {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                        if (x == tile.tileX || y == tile.tileY) {
                            if (map[x, y] == wallChar)
                                edgeTiles.Add(tile);
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom() {
            if (!isAccessibleFromMainRoom) {
                isAccessibleFromMainRoom = true;
                foreach (Room connectedRooms in connectedRooms) {
                    connectedRooms.SetAccessibleFromMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB) {
            if (roomA.isAccessibleFromMainRoom) {
                roomB.SetAccessibleFromMainRoom();
            } else if (roomB.isAccessibleFromMainRoom) {
                roomA.SetAccessibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom) {
            return connectedRooms.Contains(otherRoom);
        }

        // Implementation of the interface method to have automatic ordering. 
        public int CompareTo(Room otherRoom) {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }

    // Informations about an object. 
    [Serializable]
    private struct MapObject {
        // Character which defines the object.
        public char objectChar;
        // Number of objects to be put in the map.
        public int numObjPerMap;
    }

    // Saves the map in a text file.
    private void SaveMapAsText() {
        if (textFilePath == null && !Directory.Exists(textFilePath)) {
            Debug.LogError("Error while retrieving the folder, please insert a valid path.");
        } else {
            try {
                String textMap = "";

                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        textMap = textMap + map[x, y];
                    }
                    if (x < width - 1)
                        textMap = textMap + "\n";
                }

                System.IO.File.WriteAllText(@textFilePath + "/map_" + hash.ToString() + ".txt", textMap);
            } catch (Exception) {
                Debug.LogError("Error while retrieving the folder, please insert a valid path and check its permissions.");
            }
        }
    }

    // Gets the current date as string.
    private string GetDateString() {
        return System.DateTime.Now.ToString();
    }

    // Returns the maximum map size.
    public int GetMapSize() {
        if (width > height)
            return width;
        else
            return height;
    }

    // Draws the map.
    private void OnDrawGizmos() {
        if (map != null && !createMesh) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    Gizmos.color = (map[x, y] == wallChar) ? Color.black : Color.white;
                    Vector3 position = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);
                    Gizmos.DrawCube(position, Vector3.one);
                }
            }
        }

    }

}