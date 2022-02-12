using System;
using System.Collections.Generic;
using Logging;
using UnityEngine;

namespace Maps.MapGenerator
{
    /// <summary>
    ///     CellularMapGenerator is an implementation of MapGenerator that generates the map by using a
    ///     cellular automata.
    /// </summary>
    public class CellularMapGenerator : MapGenerator
    {
        [Header("Cellular generation")]
        // How much the map will be randomly filled at the beginning.
        [SerializeField]
        [Range(0, 100)]
        private int ramdomFillPercent = 50;

        // Number of smoothing iterations to be done.
        [SerializeField] [Range(0, 3)] private int smoothingIterations = 3;

        // You must have more than this number of neighbour to became wall.
        [SerializeField] [Range(0, 9)] private int neighbourTileLimitHigh = 4;

        // You must have less than this number of neighbour to became room.
        [SerializeField] [Range(0, 9)] private int neighbourTileLimitLow = 4;

        // Minimum size of a wall region.
        [SerializeField] private int wallThresholdSize = 50;

        // Minimum size of a room region.
        [SerializeField] private int roomThresholdSize = 50;

        // Passage width.
        [SerializeField] private int passageWidth = 5;

        private void Start()
        {
            SetReady(true);
        }

        public override char[,] GenerateMap()
        {
            map = GetVoidMap();

            InitializePseudoRandomGenerator();

            RandomFillMap();

            for (var i = 0; i < smoothingIterations; i++) SmoothMap();

            ProcessMap();

            map = MapEdit.AddBorders(map, borderSize, wallChar);
            width = map.GetLength(0);
            height = map.GetLength(1);

            var textMap = GetMapAsText();
            SaveMapTextGameEvent.Instance.Raise(textMap);
            if (createTextFile)
                SaveMapAsText(textMap);
            return map;
        }

        // Processes the map.
        private void ProcessMap()
        {
            var wallRegions = GetRegions(wallChar);

            foreach (var wallRegion in wallRegions)
                if (wallRegion.Count < wallThresholdSize)
                    foreach (var tile in wallRegion)
                        map[tile.tileX, tile.tileY] = roomChar;

            var roomRegions = GetRegions(roomChar);
            var survivingRooms = new List<Room>();

            foreach (var roomRegion in roomRegions)
                if (roomRegion.Count < roomThresholdSize)
                    foreach (var tile in roomRegion)
                        map[tile.tileX, tile.tileY] = wallChar;
                else
                    survivingRooms.Add(new Room(roomRegion, map, wallChar));

            // If there are at least two rooms.
            if (survivingRooms.Count > 0)
            {
                survivingRooms.Sort();
                survivingRooms[0].isMainRoom = true;
                survivingRooms[0].isAccessibleFromMainRoom = true;

                ConnectClosestRooms(survivingRooms);
            }

            PopulateMap();
        }

        // Connects each room which the closest one.
        private void ConnectClosestRooms(List<Room> allRooms,
            bool forceAccessibilityFromMainRoom = false)
        {
            // Accessible rooms.
            var roomListA = new List<Room>();
            // Not accessible rooms.
            var roomListB = new List<Room>();

            if (forceAccessibilityFromMainRoom)
            {
                foreach (var room in allRooms)
                    if (room.isAccessibleFromMainRoom)
                        roomListB.Add(room);
                    else
                        roomListA.Add(room);
            }
            else
            {
                roomListA = allRooms;
                roomListB = allRooms;
            }

            var bestDistance = 0;
            var bestTileA = new Coord();
            var bestTileB = new Coord();
            var bestRoomA = new Room();
            var bestRoomB = new Room();
            var possibleConnectionFound = false;

            foreach (var roomA in roomListA)
            {
                if (!forceAccessibilityFromMainRoom)
                {
                    possibleConnectionFound = false;
                    if (roomA.connectedRooms.Count > 0) continue;
                }

                foreach (var roomB in roomListB)
                {
                    if (roomA == roomB || roomA.IsConnected(roomB)) continue;

                    for (var tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                    for (var tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        var tileA = roomA.edgeTiles[tileIndexA];
                        var tileB = roomB.edgeTiles[tileIndexB];
                        var distanceBetweenRooms = (int) (Mathf.Pow(tileA.tileX - tileB.tileX, 2) +
                                                          Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }

                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(allRooms, true);
            }

            if (!forceAccessibilityFromMainRoom) ConnectClosestRooms(allRooms, true);
        }

        // Creates a passage between two rooms.
        private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
        {
            Room.ConnectRooms(roomA, roomB);

            var line = GetLine(tileA, tileB);

            foreach (var c in line) MapEdit.DrawCircle(c.tileX, c.tileY, passageWidth, map, roomChar);
        }

        // Returns a list of coordinates for each point in the line.
        private List<Coord> GetLine(Coord from, Coord to)
        {
            var line = new List<Coord>();

            var x = from.tileX;
            var y = from.tileY;

            var dx = to.tileX - from.tileX;
            var dy = to.tileY - from.tileY;

            var inverted = false;
            var step = Math.Sign(dx);
            var gradientStep = Math.Sign(dy);

            var longest = Mathf.Abs(dx);
            var shortest = Mathf.Abs(dy);

            if (longest < shortest)
            {
                inverted = true;
                longest = Mathf.Abs(dy);
                shortest = Mathf.Abs(dx);

                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }

            var gradientAccumulation = longest / 2;
            for (var i = 0; i < longest; i++)
            {
                line.Add(new Coord(x, y));

                if (inverted)
                    y += step;
                else
                    x += step;

                gradientAccumulation += shortest;
                if (gradientAccumulation >= longest)
                {
                    if (inverted)
                        x += gradientStep;
                    else
                        y += gradientStep;
                    gradientAccumulation -= longest;
                }
            }

            return line;
        }

        // Given a certain "general" (full/room) tile type it returns all the regions of that type.
        private List<List<Coord>> GetRegions(char tileType)
        {
            var regions = new List<List<Coord>>();
            var mapFlags = new int[width, height];

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (mapFlags[x, y] == 0 && MapInfo.IsSameGeneralType(tileType, map[x, y],
                    wallChar))
                {
                    var newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (var tile in newRegion) mapFlags[tile.tileX, tile.tileY] = 1;
                }

            return regions;
        }

        // Return the tiles of the region the parameter coordinates belong too using the flood-fill 
        // algorithm.
        private List<Coord> GetRegionTiles(int startX, int startY)
        {
            var tiles = new List<Coord>();
            var mapFlags = new int[width, height];
            var tileType = map[startX, startY];

            var queue = new Queue<Coord>();
            queue.Enqueue(new Coord(startX, startY));
            mapFlags[startX, startY] = 1;

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                tiles.Add(tile);

                for (var x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                for (var y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    if (MapInfo.IsInMapRange(x, y, width, height) && (y == tile.tileY ||
                                                                      x == tile.tileX))
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
            }

            return tiles;
        }

        // Randomly fills the map based on a seed.
        private void RandomFillMap()
        {
            // Loop on each tile and assign a value;
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    map[x, y] = wallChar;
                else
                    map[x, y] = pseudoRandomGen.Next(0, 100) < ramdomFillPercent ? wallChar : roomChar;
        }

        // Smooths the map.
        private void SmoothMap()
        {
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var neighbourWallTiles = MapInfo.GetSurroundingWallCount(x, y, map, wallChar);

                if (neighbourWallTiles > neighbourTileLimitHigh)
                    map[x, y] = wallChar;
                else if (neighbourWallTiles < neighbourTileLimitLow) map[x, y] = roomChar;
            }
        }

        public override string ConvertMapToAB(bool exportObjects = true)
        {
            throw new NotImplementedException();
        }

        // Stores all information about a room.
        private class Room : IComparable<Room>
        {
            public readonly List<Room> connectedRooms;
            public readonly List<Coord> edgeTiles;
            public bool isAccessibleFromMainRoom;
            public bool isMainRoom;
            public readonly int roomSize;
            public readonly List<Coord> tiles;

            public Room()
            {
            }

            public Room(List<Coord> roomTiles, char[,] map, char wallChar)
            {
                tiles = roomTiles;
                roomSize = tiles.Count;
                connectedRooms = new List<Room>();
                edgeTiles = new List<Coord>();

                // For each tile of the room I get the neighbours that are walls obtaining the edge of 
                // the room.
                foreach (var tile in tiles)
                    for (var x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                    for (var y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                        if (x == tile.tileX || y == tile.tileY)
                            if (map[x, y] == wallChar)
                                edgeTiles.Add(tile);
            }

            // Implementation of the interface method to have automatic ordering. 
            public int CompareTo(Room otherRoom)
            {
                return otherRoom.roomSize.CompareTo(roomSize);
            }

            public void SetAccessibleFromMainRoom()
            {
                if (!isAccessibleFromMainRoom)
                {
                    isAccessibleFromMainRoom = true;
                    foreach (var connectedRooms in connectedRooms) connectedRooms.SetAccessibleFromMainRoom();
                }
            }

            public static void ConnectRooms(Room roomA, Room roomB)
            {
                if (roomA.isAccessibleFromMainRoom)
                    roomB.SetAccessibleFromMainRoom();
                else if (roomB.isAccessibleFromMainRoom) roomA.SetAccessibleFromMainRoom();
                roomA.connectedRooms.Add(roomB);
                roomB.connectedRooms.Add(roomA);
            }

            public bool IsConnected(Room otherRoom)
            {
                return connectedRooms.Contains(otherRoom);
            }
        }
    }
}