﻿using System;
using System.Collections.Generic;
using System.IO;
using Logging;
using UnityEngine;

namespace Maps.MapGenerator
{
    /// <summary>
    ///     DivisiveMapGenerator is an implementation of MapGenerator that generates the map by recursevly
    ///     splitting the map. A part of the resulting rooms are kept and connection are added.
    /// </summary>
    public class DivisiveMapGenerator : MapGenerator
    {
        // Probability of a room being divided again.
        [Header("Divisive generation")] [SerializeField] [Range(0, 100)]
        private int roomDivideProbability;

        // Probability of a room not being erased.
        [SerializeField] [Range(0, 100)] private int mapRoomPercentage;

        // Percentual lower bound of where a room can be divided.
        [SerializeField] [Range(10, 90)] private int divideLowerBound;

        // Percentual upper bound of where a room can be divided.
        [SerializeField] [Range(10, 90)] private int divideUpperBound;

        // Minimum room dimension.
        [SerializeField] private int minimumRoomDimension = 5;

        // Border size.
        [SerializeField] private int minimumDepth = 5;

        // Passage width.
        [SerializeField] private int passageWidth = 5;

        // Passage width.
        [SerializeField] private int maxRandomPassages = 5;
        private List<ABRoom> ABCorridors;

        private List<ABRoom> ABRooms;
        private List<ABTile> ABTiles;

        private int minimumDividableRoomDimension;
        private int minimumFilledTiles;
        private List<Room> placedRooms;

        private List<Room> rooms;

        private void Start()
        {
            minimumDividableRoomDimension = minimumRoomDimension * 2;
            minimumFilledTiles = (int) (width * height * mapRoomPercentage / 100f);

            SetReady(true);
        }

        public override char[,] GenerateMap()
        {
            map = GetVoidMap();

            InitializePseudoRandomGenerator();

            MapEdit.FillMap(map, wallChar);

            rooms = new List<Room>();
            placedRooms = new List<Room>();

            if (createTextFile || prepareABExport)
            {
                ABRooms = new List<ABRoom>();
                ABCorridors = new List<ABRoom>();
                ABTiles = new List<ABTile>();
            }

            if (width > height)
                DivideRoom(0, 0, width, height, true, 0);
            else
                DivideRoom(0, 0, width, height, false, 0);

            ProcessRooms();

            ProcessMap();

            map = MapEdit.AddBorders(map, borderSize, wallChar);
            width = map.GetLength(0);
            height = map.GetLength(1);

            if (createTextFile || prepareABExport)
            {
                PopulateABRooms();
                PopulateABObjects();
                AddBorderToAB(borderSize);
            }

            var textMap = GetMapAsText();
            SaveMapTextGameEvent.Instance.Raise(textMap);

            if (createTextFile)
            {
                SaveMapAsText(textMap);
                SaveMapAsAB();
            }

            return map;
        }

        // Processes the map.
        private void ProcessMap()
        {
            // If there are at least two rooms.
            if (placedRooms.Count > 1)
            {
                placedRooms.Sort();
                placedRooms[0].isMainRoom = true;
                placedRooms[0].isAccessibleFromMainRoom = true;

                ConnectClosestRooms(placedRooms);

                AddRandomConnections();

                PopulateMap(map);
            }
            else
            {
                ManageError(Error.HARD_ERROR, "Error while creating the map. Please try again.");
            }
        }

        // Divides a room in subrooms or stops there.
        private void DivideRoom(int originX, int originY, int roomWidth, int roomHeigth,
            bool horizontal, int depth)
        {
            if (pseudoRandomGen.Next(0, 100) < roomDivideProbability &&
                roomWidth > minimumDividableRoomDimension &&
                roomHeigth > minimumDividableRoomDimension || depth < minimumDepth)
            {
                // Divide the room.
                if (horizontal)
                {
                    var division = GetDivisionPoint(roomHeigth);
                    // Debug.Log("Dividing vertically in " + division + " a " + roomWidth + "x" + 
                    // roomHeigth + "room in [" + originX + ", " + originY + "].");
                    DivideRoom(originX, originY, roomWidth, division, false, depth + 1);
                    DivideRoom(originX, originY + division + 1, roomWidth, roomHeigth - division - 1,
                        false, depth + 1);
                }
                else
                {
                    var division = GetDivisionPoint(roomWidth);
                    // Debug.Log("Dividing horizontally in " + division + " a " + roomWidth + "x" + 
                    // roomHeigth + "room in [" + originX + ", " + originY + "].");
                    DivideRoom(originX, originY, division, roomHeigth, true, depth + 1);
                    DivideRoom(originX + division + 1, originY, roomWidth - division - 1, roomHeigth,
                        true, depth + 1);
                }
            }
            else
            {
                AddRoom(originX, originY, roomWidth, roomHeigth);
            }
        }

        // Makes some of the possible rooms into actual rooms.
        private void ProcessRooms()
        {
            var currentFill = 0;

            while (currentFill < minimumFilledTiles && rooms.Count > 0)
            {
                var current = pseudoRandomGen.Next(0, rooms.Count);
                // Debug.Log("Selected room " + current + " out of " + rooms.Count + " of size " + 
                // rooms[current].roomSize  + ".");
                PlaceRoom(rooms[current].originX, rooms[current].originY, rooms[current].width,
                    rooms[current].height);
                currentFill += rooms[current].roomSize;
                placedRooms.Add(rooms[current]);
                rooms.RemoveAt(current);
            }
        }

        // Returns the point where the room must be divided.
        private int GetDivisionPoint(int roomWidth)
        {
            return roomWidth * pseudoRandomGen.Next(divideLowerBound, divideUpperBound) / 100;
        }

        // Connects each room which the closest one.
        private void ConnectClosestRooms(List<Room> allRooms,
            bool forceAccessibilityFromMainRoom = false)
        {
            // Accessible rooms.
            var roomsA = new List<Room>();
            // Not accessible rooms.
            var roomsB = new List<Room>();

            if (forceAccessibilityFromMainRoom)
            {
                foreach (var room in allRooms)
                    if (room.isAccessibleFromMainRoom)
                        roomsB.Add(room);
                    else
                        roomsA.Add(room);
            }
            else
            {
                roomsA = allRooms;
                roomsB = allRooms;
            }

            var bestDistance = 0;
            var bestRoomA = new Room();
            var bestRoomB = new Room();
            var possibleConnectionFound = false;

            foreach (var roomA in roomsA)
            {
                if (!forceAccessibilityFromMainRoom)
                {
                    possibleConnectionFound = false;
                    if (roomA.connectedRooms.Count > 0) continue;
                }

                foreach (var roomB in roomsB)
                {
                    var distanceBetweenRooms = (int) (Mathf.Pow(roomA.centerX - roomB.centerX, 2) +
                                                      Mathf.Pow(roomA.centerY - roomB.centerY, 2));

                    if (!(roomA == roomB || roomA.IsConnected(roomB)) &&
                        (distanceBetweenRooms < bestDistance || !possibleConnectionFound))
                    {
                        bestDistance = distanceBetweenRooms;
                        possibleConnectionFound = true;
                        bestRoomA = roomA;
                        bestRoomB = roomB;
                    }
                }

                if (possibleConnectionFound && !forceAccessibilityFromMainRoom) CreatePassage(bestRoomA, bestRoomB);
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB);
                ConnectClosestRooms(allRooms, true);
            }

            if (!forceAccessibilityFromMainRoom) ConnectClosestRooms(allRooms, true);
        }

        // Creates a passage between two rooms.
        private void CreatePassage(Room roomA, Room roomB)
        {
            if (roomB.centerX > roomA.centerX)
            {
                CreatePassage(roomB, roomA);
            }
            else
            {
                Room.ConnectRooms(roomA, roomB);

                int extremeX;
                int extremeY;
                int connectionX;
                int connectionY;

                if (pseudoRandomGen.Next(100) < 50)
                {
                    connectionX = roomA.centerX;
                    connectionY = roomB.centerY;
                }
                else
                {
                    connectionX = roomB.centerX;
                    connectionY = roomA.centerY;
                }

                if (roomA.centerX != connectionX)
                    extremeX = roomA.centerX;
                else
                    extremeX = roomB.centerX;

                if (roomA.centerY != connectionY)
                    extremeY = roomA.centerY;
                else
                    extremeY = roomB.centerY;

                // Create the horizontal section.
                if (extremeX > connectionX)
                {
                    for (var x = connectionX - Mathf.FloorToInt(passageWidth / 2f);
                        x <= extremeX;
                        x++)
                    for (var y = connectionY - passageWidth / 2;
                        y <= connectionY + passageWidth / 2;
                        y++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                    if ((createTextFile || prepareABExport) && extremeX != connectionX)
                        ABCorridors.Add(new ABRoom(connectionX - Mathf.FloorToInt(passageWidth / 2f),
                            connectionY - Mathf.FloorToInt(passageWidth / 2f),
                            extremeX - connectionX + passageWidth));
                }
                else
                {
                    for (var x = extremeX; x <= connectionX + Mathf.FloorToInt(passageWidth / 2f); x++)
                    for (var y = connectionY - passageWidth / 2;
                        y <= connectionY + passageWidth / 2;
                        y++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                    if ((createTextFile || prepareABExport) && extremeX != connectionX)
                        ABCorridors.Add(new ABRoom(extremeX - Mathf.FloorToInt(passageWidth / 2f),
                            connectionY - Mathf.FloorToInt(passageWidth / 2f),
                            connectionX - extremeX + passageWidth));
                }

                // Create the vertical section.
                if (extremeY > connectionY)
                {
                    for (var y = connectionY - Mathf.FloorToInt(passageWidth / 2f);
                        y <= extremeY;
                        y++)
                    for (var x = connectionX - passageWidth / 2;
                        x <= connectionX + passageWidth / 2;
                        x++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                    if ((createTextFile || prepareABExport) && extremeY != connectionY)
                        ABCorridors.Add(new ABRoom(connectionX - Mathf.FloorToInt(passageWidth / 2f),
                            connectionY - Mathf.FloorToInt(passageWidth / 2f),
                            connectionY - extremeY - passageWidth));
                }
                else
                {
                    for (var y = extremeY;
                        y <= connectionY + Mathf.FloorToInt(passageWidth / 2f);
                        y++)
                    for (var x = connectionX - passageWidth / 2;
                        x <= connectionX + passageWidth / 2;
                        x++)
                        if (MapInfo.IsInMapRange(x, y, width, height))
                            map[x, y] = roomChar;
                    if ((createTextFile || prepareABExport) && extremeY != connectionY)
                        ABCorridors.Add(new ABRoom(connectionX - Mathf.FloorToInt(passageWidth / 2f),
                            extremeY - Mathf.FloorToInt(passageWidth / 2f),
                            extremeY - connectionY - passageWidth));
                }
            }
        }

        // Adds random connections between the rooms.
        private void AddRandomConnections()
        {
            for (var i = 0; i < maxRandomPassages; i++)
            {
                var roomA = placedRooms[pseudoRandomGen.Next(0, placedRooms.Count)];
                var roomB = placedRooms[pseudoRandomGen.Next(0, placedRooms.Count)];
                if (!roomA.connectedRooms.Contains(roomB))
                    CreatePassage(roomA, roomB);
            }
        }

        // Adds a room to the list.
        private void AddRoom(int originX, int originY, int roomWidth, int roomHeigth)
        {
            // If the room makes sense.
            if (MapInfo.IsInMapRange(originX, originY, width, height) &&
                MapInfo.IsInMapRange(originX + roomWidth, originY + roomHeigth, width, height) &&
                roomWidth > minimumRoomDimension && roomHeigth > minimumRoomDimension)
                // Add it to the room list.
                rooms.Add(new Room(originX, originY, roomWidth, roomHeigth));
        }

        // Places a room.
        private void PlaceRoom(int originX, int originY, int roomWidth, int roomHeigth)
        {
            for (var x = originX; x < roomWidth + originX; x++)
            for (var y = originY; y < roomHeigth + originY; y++)
                map[x, y] = roomChar;
        }

        // Populates the AB rooms starting from the placed rooms.
        private void PopulateABRooms()
        {
            foreach (var r in placedRooms)
                if (r.width > r.height)
                {
                    var count = r.width / r.height;
                    for (var i = 0; i < count; i++)
                        ABRooms.Add(new ABRoom(r.originX + i * r.height, r.originY, r.height));
                    if (r.width % r.height != 0)
                        ABRooms.Add(new ABRoom(r.originX + r.width - r.height, r.originY, r.height));
                }
                else
                {
                    var count = r.height / r.width;
                    for (var i = 0; i < count; i++)
                        ABRooms.Add(new ABRoom(r.originX, r.originY + i * r.width, r.width));
                    if (r.height % r.width != 0)
                        ABRooms.Add(new ABRoom(r.originX, r.originY + r.height - r.width, r.width));
                }
        }

        // Populates the AB objects scanning the map.
        private void PopulateABObjects()
        {
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                if (map[x, y] != wallChar && map[x, y] != roomChar)
                    ABTiles.Add(new ABTile(x, y, map[x, y]));
        }

        // Adds the border to the AB notation.
        private void AddBorderToAB(int border)
        {
            foreach (var a in ABRooms)
            {
                a.originX += border;
                a.originY += border;
            }

            foreach (var c in ABCorridors)
            {
                c.originX += border;
                c.originY += border;
            }
        }

        // Saves the map using AB notation.
        private void SaveMapAsAB()
        {
            if (textFilePath == null && !Directory.Exists(textFilePath))
                ManageError(Error.SOFT_ERROR, "Error while retrieving the folder, please insert a " +
                                              "valid path.");
            else
                try
                {
                    File.WriteAllText(textFilePath + "/" + seed + ".ab.txt",
                        ConvertMapToAB());
                }
                catch (Exception)
                {
                    ManageError(Error.SOFT_ERROR, "Error while saving the map, please insert a valid " +
                                                  "path and check its permissions.");
                }
        }

        public override string ConvertMapToAB(bool exportObjects = true)
        {
            var genome = "";

            // Add the rooms to the genome.
            foreach (var r in ABRooms) genome += "<" + r.originX + ',' + r.originY + ',' + r.dimension + ">";
            // Add the corridors to the genome.
            if (ABCorridors.Count > 0)
            {
                genome += "|";
                foreach (var r in ABCorridors) genome += "<" + r.originX + ',' + r.originY + ',' + r.dimension + ">";
            }

            if (exportObjects) // Add the tiles to the genome.
                if (ABTiles.Count > 0)
                {
                    genome += "|";
                    foreach (var t in ABTiles) genome += "<" + t.x + ',' + t.y + ',' + t.value + ">";
                }

            return genome;
        }


        // Stores all information about a room.
        private class Room : IComparable<Room>
        {
            public readonly int centerX;
            public readonly int centerY;

            public readonly List<Room> connectedRooms;
            public readonly int height;
            public readonly int originX;
            public readonly int originY;
            public readonly int roomSize;
            public readonly int width;
            public bool isAccessibleFromMainRoom;
            public bool isMainRoom;

            public Room()
            {
            }

            public Room(int x, int y, int w, int h)
            {
                originX = x;
                originY = y;
                centerX = x + w / 2;
                centerY = y + h / 2;
                width = w;
                height = h;
                roomSize = w * h;

                connectedRooms = new List<Room>();
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