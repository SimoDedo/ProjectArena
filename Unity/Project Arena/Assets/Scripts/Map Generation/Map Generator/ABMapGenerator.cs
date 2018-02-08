using MapManipulation;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CellularMapGenerator is an implementation of MapGenerator that generates the map by using a
/// cellular automata.
/// </summary>
public class ABMapGenerator : MapGenerator {

    [SerializeField] private int passageWidth = 3;

    private Room mainRoom;
    private List<Room> arenas;
    private List<Room> corridors;

    private void Start() {
        originalWidth = width;
        originalHeight = height;

        SetReady(true);
    }

    public override char[,] GenerateMap() {
        InitializePseudoRandomGenerator();

        width = 0;
        height = 0;

        ParseGenome();

        InitializeMap();

        ProcessMap();

        MapSize newSize = MapEdit.AddBorders(map, borderSize, wallChar);
        width = newSize.width;
        height = newSize.height;

        if (createTextFile) {
            seed = seed.GetHashCode().ToString();
            SaveMapAsText();
        }

        return map;
    }

    // Decodes the genome populating the lists of arenas and corridors.
    private void ParseGenome() {
        arenas = new List<Room>();
        corridors = new List<Room>();

        string currentValue = "";
        int currentChar = 0;

        while (currentChar < seed.Length && seed[currentChar] == '<') {
            Room arena = new Room();
            currentChar++;

            // Get the x coordinate of the origin.
            while (Char.IsNumber(seed[currentChar])) {
                currentValue += seed[currentChar];
                currentChar++;
            }
            arena.originX = Int32.Parse(currentValue);

            currentValue = "";
            currentChar++;

            // Get the y coordinate of the origin.
            while (Char.IsNumber(seed[currentChar])) {
                currentValue += seed[currentChar];
                currentChar++;
            }
            arena.originY = Int32.Parse(currentValue);

            currentValue = "";
            currentChar++;

            // Get the size of the arena.
            while (Char.IsNumber(seed[currentChar])) {
                currentValue += seed[currentChar];
                currentChar++;
            }
            arena.dimension = Int32.Parse(currentValue);

            // Add the arena to the list.
            UpdateMapSize(arena.originX, arena.originY, arena.dimension, true);
            arenas.Add(arena);

            currentValue = "";
            currentChar++;
        }

        if (currentChar < seed.Length && seed[currentChar] == '|') {
            currentChar++;

            while (currentChar < seed.Length && seed[currentChar] == '<') {
                Room corridor = new Room();
                currentChar++;

                // Get the x coordinate of the origin.
                while (Char.IsNumber(seed[currentChar])) {
                    currentValue += seed[currentChar];
                    currentChar++;
                }
                corridor.originX = Int32.Parse(currentValue);

                currentValue = "";
                currentChar++;

                // Get the y coordinate of the origin.
                while (Char.IsNumber(seed[currentChar])) {
                    currentValue += seed[currentChar];
                    currentChar++;
                }
                corridor.originY = Int32.Parse(currentValue);

                currentValue = "";
                currentChar++;

                // Get the size of the corridor.
                if (seed[currentChar] == '-') {
                    currentValue += seed[currentChar];
                    currentChar++;
                }
                while (Char.IsNumber(seed[currentChar])) {
                    currentValue += seed[currentChar];
                    currentChar++;
                }
                corridor.dimension = Int32.Parse(currentValue);

                // Add the arena to the list.
                UpdateMapSize(corridor.originX, corridor.originY, corridor.dimension, false);
                corridors.Add(corridor);

                currentValue = "";
                currentChar++;
            }
        }

        mainRoom = arenas[0];
    }

    // Updates the map size.
    private void UpdateMapSize(int originX, int originY, int dimension, bool isArena) {
        if (isArena) {
            if (originX + dimension > width) {
                width = originX + dimension;
            }
            if (originY + dimension > height) {
                height = originY + dimension;
            }
        } else {
            if (dimension > 0) {
                if (originX + dimension > width) {
                    width = originX + dimension;
                }
                if (originY + passageWidth > height) {
                    height = originY + passageWidth;
                }
            } else {
                if (originX + passageWidth > width) {
                    width = originX + passageWidth;
                }
                if (originY + dimension > height) {
                    height = originY + dimension;
                }
            }
        }
    }

    // Initializes the map adding arenas and corridors.
    private void InitializeMap() {
        map = new char[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                map[x, y] = wallChar;
            }
        }

        foreach (Room a in arenas) {
            for (int x = a.originX; x < a.originX + a.dimension; x++) {
                for (int y = a.originY; y < a.originY + a.dimension; y++) {
                    if (MapInfo.IsInMapRange(x, y, width, height)) {
                        map[x, y] = roomChar;
                    }
                }
            }
        }

        foreach (Room c in corridors) {
            if (c.dimension > 0) {
                for (int x = c.originX; x < c.originX + c.dimension; x++) {
                    for (int y = c.originY; y < c.originY + passageWidth; y++) {
                        if (MapInfo.IsInMapRange(x, y, width, height)) {
                            map[x, y] = roomChar;
                        }
                    }
                }
            } else {
                for (int x = c.originX; x < c.originX + passageWidth; x++) {
                    for (int y = c.originY; y < c.originY - c.dimension; y++) {
                        if (MapInfo.IsInMapRange(x, y, width, height)) {
                            map[x, y] = roomChar;
                        }
                    }
                }
            }
        }
    }

    // Removes rooms that are not reachable from the main one and adds objects.
    private void ProcessMap() {
        // Get the reachability mask.
        bool[,] reachabilityMask = ComputeReachabilityMask();

        // Remove rooms not connected to the main one.
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (!reachabilityMask[x, y]) {
                    map[x, y] = wallChar;
                }
            }
        }

        // Add objects;
        PopulateMap();
    }

    // Computes a mask of the tiles reachable by the main arena and scales the number of objects to 
    // be displaced..
    private bool[,] ComputeReachabilityMask() {
        bool[,] reachabilityMask = new bool[width, height];
        int floorCount = 0;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                reachabilityMask[x, y] = false;
            }
        }

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(mainRoom.originX, mainRoom.originY));

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (MapInfo.IsInMapRange(x, y, width, height) && (y == tile.tileY ||
                        x == tile.tileX)) {
                        if (reachabilityMask[x, y] == false && map[x, y] == roomChar) {
                            reachabilityMask[x, y] = true;
                            queue.Enqueue(new Coord(x, y));
                            floorCount++;
                        }
                    }
                }
            }
        }

        ScaleObjectsPopulation(floorCount);

        return reachabilityMask;
    }

    // Scales the number of instance of each object depending on the size of the map w.r.t. the 
    // original one.
    private void ScaleObjectsPopulation(int floorCount) {
        float scaleFactor = floorCount / (originalHeight * originalWidth / 3f);

        for (int i = 0; i < mapObjects.Length; i++) {
            mapObjects[i].numObjPerMap = (int)Math.Ceiling(scaleFactor *
                mapObjects[i].numObjPerMap);
        }
    }

    // Stores all information about a room.
    private class Room {
        public int originX;
        public int originY;
        public int dimension;

        public Room() {
        }
    }

}