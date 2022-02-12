using System;
using System.Collections.Generic;
using Logging;
using UnityEngine;

namespace Maps.MapGenerator
{
    /// <summary>
    ///     ABMapGenerator is an implementation of MapGenerator that generates the map by decoding its AB notation.
    /// </summary>
    public class ABMapGenerator : MapGenerator
    {
        [SerializeField] private int passageWidth = 3;
        private List<ABRoom> arenas;
        private List<ABRoom> corridors;

        private ABRoom mainRoom;
        private List<ABTile> tiles;

        private void Start()
        {
            SetReady(true);
        }

        public override char[,] GenerateMap()
        {
            InitializePseudoRandomGenerator();

            width = 0;
            height = 0;

            ParseGenome();

            InitializeMap();

            ProcessMap();

            map = MapEdit.AddBorders(map, borderSize, wallChar);
            width += borderSize * 2;
            height += borderSize * 2;

            var textMap = GetMapAsText();
            SaveMapTextGameEvent.Instance.Raise(textMap);
            if (createTextFile)
            {
                seed = seed.GetHashCode().ToString();
                SaveMapAsText(textMap);
            }

            return map;
        }

        // Decodes the genome populating the lists of arenas, corridors and tiles.
        private void ParseGenome()
        {
            arenas = new List<ABRoom>();
            corridors = new List<ABRoom>();
            tiles = new List<ABTile>();

            var currentValue = "";
            var currentChar = 0;

            // Parse the arenas.
            while (currentChar < seed.Length && seed[currentChar] == '<')
            {
                var arena = new ABRoom();
                currentChar++;

                // Get the x coordinate of the origin.
                while (char.IsNumber(seed[currentChar]))
                {
                    currentValue += seed[currentChar];
                    currentChar++;
                }

                arena.originX = int.Parse(currentValue);

                currentValue = "";
                currentChar++;

                // Get the y coordinate of the origin.
                while (char.IsNumber(seed[currentChar]))
                {
                    currentValue += seed[currentChar];
                    currentChar++;
                }

                arena.originY = int.Parse(currentValue);

                currentValue = "";
                currentChar++;

                // Get the size of the arena.
                while (char.IsNumber(seed[currentChar]))
                {
                    currentValue += seed[currentChar];
                    currentChar++;
                }

                arena.dimension = int.Parse(currentValue);

                // Add the arena to the list.
                UpdateMapSize(arena.originX, arena.originY, arena.dimension, true);
                arenas.Add(arena);

                currentValue = "";
                currentChar++;
            }

            var rollbackCurrentChar = currentChar;

            // Parse the corridors.
            if (currentChar < seed.Length && seed[currentChar] == '|')
            {
                currentChar++;

                while (currentChar < seed.Length && seed[currentChar] == '<')
                {
                    var corridor = new ABRoom();
                    currentChar++;

                    // Get the x coordinate of the origin.
                    while (char.IsNumber(seed[currentChar]))
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    corridor.originX = int.Parse(currentValue);

                    currentValue = "";
                    currentChar++;

                    // Get the y coordinate of the origin.
                    while (char.IsNumber(seed[currentChar]))
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    corridor.originY = int.Parse(currentValue);

                    currentValue = "";
                    currentChar++;

                    // Stop parsing the corridors if what I have is a tile.
                    if (!(seed[currentChar] == '-' || char.IsNumber(seed[currentChar])))
                    {
                        currentChar = rollbackCurrentChar;
                        break;
                    }

                    // Get the size of the corridor.
                    if (seed[currentChar] == '-')
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    while (char.IsNumber(seed[currentChar]))
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    corridor.dimension = int.Parse(currentValue);

                    // Add the arena to the list.
                    UpdateMapSize(corridor.originX, corridor.originY, corridor.dimension, false);
                    corridors.Add(corridor);

                    currentValue = "";
                    currentChar++;
                }
            }

            // Parse the tiles.
            if (currentChar < seed.Length && seed[currentChar] == '|')
            {
                currentChar++;

                while (currentChar < seed.Length && seed[currentChar] == '<')
                {
                    var tile = new ABTile();
                    currentChar++;

                    // Get the x coordinate of the origin.
                    while (char.IsNumber(seed[currentChar]))
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    tile.x = int.Parse(currentValue);

                    currentValue = "";
                    currentChar++;

                    // Get the y coordinate of the origin.
                    while (char.IsNumber(seed[currentChar]))
                    {
                        currentValue += seed[currentChar];
                        currentChar++;
                    }

                    tile.y = int.Parse(currentValue);

                    currentValue = "";
                    currentChar++;

                    // Get the value of the tile.
                    tile.value = seed[currentChar];

                    // Add the arena to the list.
                    tiles.Add(tile);

                    currentValue = "";
                    currentChar += 2;
                }
            }

            mainRoom = arenas[0];
        }

        // Updates the map size.
        private void UpdateMapSize(int originX, int originY, int dimension, bool isArena)
        {
            if (isArena)
            {
                if (originX + dimension > width) width = originX + dimension;
                if (originY + dimension > height) height = originY + dimension;
            }
            else
            {
                if (dimension > 0)
                {
                    if (originX + dimension > width) width = originX + dimension;
                    if (originY + passageWidth > height) height = originY + passageWidth;
                }
                else
                {
                    if (originX + passageWidth > width) width = originX + passageWidth;
                    if (originY + dimension > height) height = originY + dimension;
                }
            }
        }

        // Initializes the map adding arenas and corridors.
        private void InitializeMap()
        {
            map = GetVoidMap();

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                map[x, y] = wallChar;

            foreach (var a in arenas)
                for (var x = a.originX; x < a.originX + a.dimension; x++)
                for (var y = a.originY; y < a.originY + a.dimension; y++)
                    if (MapInfo.IsInMapRange(x, y, width, height))
                        map[x, y] = roomChar;

            foreach (var c in corridors)
                if (c.dimension > 0)
                {
                    for (var x = c.originX; x < c.originX + c.dimension; x++)
                    for (var y = c.originY; y < c.originY + passageWidth; y++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                }
                else
                {
                    for (var x = c.originX; x < c.originX + passageWidth; x++)
                    for (var y = c.originY; y < c.originY - c.dimension; y++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                }
        }

        // Removes rooms that are not reachable from the main one and adds objects.
        private void ProcessMap()
        {
            // Get the reachability mask.
            var reachabilityMask = ComputeReachabilityMask();

            // Remove rooms not connected to the main one.
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (!reachabilityMask[x, y])
                    map[x, y] = wallChar;

            // Add objects;
            if (tiles.Count > 0)
            {
                foreach (var t in tiles)
                    if (MapInfo.IsInMapRange(t.x, t.y, width, height))
                        map[t.x, t.y] = t.value;
            }
            else
            {
                PopulateMap();
            }
        }

        // Computes a mask of the tiles reachable by the main arena and scales the number of objects to 
        // be displaced.
        private bool[,] ComputeReachabilityMask()
        {
            var reachabilityMask = new bool[width, height];
            var floorCount = 0;

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                reachabilityMask[x, y] = false;

            var queue = new Queue<Coord>();
            queue.Enqueue(new Coord(mainRoom.originX, mainRoom.originY));

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();

                for (var x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                for (var y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    if (MapInfo.IsInMapRange(x, y, width, height) && (y == tile.tileY ||
                                                                      x == tile.tileX))
                        if (reachabilityMask[x, y] == false && map[x, y] == roomChar)
                        {
                            reachabilityMask[x, y] = true;
                            queue.Enqueue(new Coord(x, y));
                            floorCount++;
                        }
            }

            ScaleObjectsPopulation(floorCount);

            return reachabilityMask;
        }

        // Scales the number of instance of each object depending on the size of the map w.r.t. the 
        // original one.
        private void ScaleObjectsPopulation(int floorCount)
        {
            var scaleFactor = floorCount / (originalHeight * originalWidth / 3f);

            for (var i = 0; i < mapObjects.Length; i++)
                mapObjects[i].numObjPerMap = (int) Math.Ceiling(scaleFactor *
                                                                mapObjects[i].numObjPerMap);
        }

        public override string ConvertMapToAB(bool exportObjects = true)
        {
            throw new NotImplementedException();
        }
    }
}