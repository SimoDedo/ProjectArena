using System;
using System.Collections.Generic;
using UnityEngine;

public class DivisiveMapGenerator : MapGenerator {

    // Probability of a room being divided again.
    [Header("Divisive generation")] [SerializeField, Range(0, 100)] private int roomDivideProbability;
    // Probability of a room not being erased.
    [SerializeField, Range(0, 100)] private int mapRoomPercentage;
    // Percentual lower bound of where a room can be divided.
    [SerializeField, Range(10, 90)] private int divideLowerBound;
    // Percentual upper bound of where a room can be divided.
    [SerializeField, Range(10, 90)] private int divideUpperBound;
    // Minimum room dimension.
    [SerializeField] private int minimumRoomDimension = 5;
    // Border size.
    [SerializeField] private int minimumDepth = 5;
    // Passage width.
    [SerializeField] private int passageWidth = 5;
    // Passage width.
    [SerializeField] private int maxRandomPassages = 5;

    private List<Room> rooms;
    private List<Room> placedRooms;
    private List<Room> placedCorridors;

    private int minimumDividableRoomDimension;
    private int minimumFilledTiles;

    private void Start() {
        minimumDividableRoomDimension = minimumRoomDimension * 2;
        minimumFilledTiles = width * height * mapRoomPercentage / 100;

        SetReady(true);
    }

    public override char[,] GenerateMap() {
        map = new char[width, height];

        InitializePseudoRandomGenerator();

        FillMap();

        rooms = new List<Room>();
        placedRooms = new List<Room>();
        placedCorridors = new List<Room>();

        if (width > height)
            DivideRoom(0, 0, width, height, true, 0);
        else
            DivideRoom(0, 0, width, height, false, 0);

        ProcessRooms();

        ProcessMap();

        AddBorders();

        if (flipMap)
            FlipMap();

        if (createTextFile) {
            SaveMapAsText();
            // SameMapAsAB();
        }

        return map;
    }

    // Processes the map.
    private void ProcessMap() {
        // If there are at least two rooms.
        if (placedRooms.Count > 1) {
            placedRooms.Sort();
            placedRooms[0].isMainRoom = true;
            placedRooms[0].isAccessibleFromMainRoom = true;

            ConnectClosestRooms(placedRooms);
        }

        AddRandomConnections();

        PopulateMap();
    }

    // Divides a room in subrooms or stops there.
    private void DivideRoom(int originX, int originY, int roomWidth, int roomHeigth, bool horizontal, int depth) {
        if ((pseudoRandomGen.Next(0, 100) < roomDivideProbability && roomWidth > minimumDividableRoomDimension && roomHeigth > minimumDividableRoomDimension) || depth < minimumDepth) {
            // Divivde the room.
            if (horizontal) {
                int division = GetDivisionPoint(roomHeigth);
                // Debug.Log("Dividing vertically in " + division + " a " + roomWidth + "x" + roomHeigth + "room in [" + originX + ", " + originY + "].");
                DivideRoom(originX, originY, roomWidth, division, false, depth + 1);
                DivideRoom(originX, originY + division + 1, roomWidth, roomHeigth - division - 1, false, depth + 1);
            } else {
                int division = GetDivisionPoint(roomWidth);
                // Debug.Log("Dividing horizontally in " + division + " a " + roomWidth + "x" + roomHeigth + "room in [" + originX + ", " + originY + "].");
                DivideRoom(originX, originY, division, roomHeigth, true, depth + 1);
                DivideRoom(originX + division + 1, originY, roomWidth - division - 1, roomHeigth, true, depth + 1);
            }
        } else {
            AddRoom(originX, originY, roomWidth, roomHeigth);
        }
    }

    private void ProcessRooms() {
        int currentFill = 0;

        while (currentFill < minimumFilledTiles && rooms.Count > 1) {
            int current = pseudoRandomGen.Next(0, rooms.Count);
            // Debug.Log("Selected room " + current + " out of " + rooms.Count + ".");
            PlaceRoom(rooms[current].originX, rooms[current].originY, rooms[current].width, rooms[current].height);
            currentFill += rooms[current].roomSize;
            placedRooms.Add(rooms[current]);
            rooms.RemoveAt(current);
        }
    }

    // Returns the point where the room must be divided.
    private int GetDivisionPoint(int roomWidth) {
        return (int)roomWidth * pseudoRandomGen.Next(divideLowerBound, divideUpperBound) / 100;
    }

    // Connects each room which the closest one.
    private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {
        // Accessible rooms.
        List<Room> roomsA = new List<Room>();
        // Not accessible rooms.
        List<Room> roomsB = new List<Room>();

        if (forceAccessibilityFromMainRoom) {
            foreach (Room room in allRooms) {
                if (room.isAccessibleFromMainRoom)
                    roomsB.Add(room);
                else
                    roomsA.Add(room);
            }
        } else {
            roomsA = allRooms;
            roomsB = allRooms;
        }

        int bestDistance = 0;
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomsA) {
            if (!forceAccessibilityFromMainRoom) {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                    continue;
            }

            foreach (Room roomB in roomsB) {
                int distanceBetweenRooms = (int)(Mathf.Pow(roomA.centerX - roomB.centerX, 2) + Mathf.Pow(roomA.centerY - roomB.centerY, 2));

                if (!(roomA == roomB || roomA.IsConnected(roomB)) && (distanceBetweenRooms < bestDistance || !possibleConnectionFound)) {
                    bestDistance = distanceBetweenRooms;
                    possibleConnectionFound = true;
                    bestRoomA = roomA;
                    bestRoomB = roomB;
                }
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {
                CreatePassage(bestRoomA, bestRoomB);
            }
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
            CreatePassage(bestRoomA, bestRoomB);
            ConnectClosestRooms(allRooms, true);
        }
        if (!forceAccessibilityFromMainRoom) {
            ConnectClosestRooms(allRooms, true);
        }
    }

    // Creates a passage between two rooms.
    private void CreatePassage(Room roomA, Room roomB) {
        Room.ConnectRooms(roomA, roomB);

        int extremeX;
        int extremeY;
        int connectionX;
        int connectionY;

        if (pseudoRandomGen.Next(100) < 50) {
            connectionX = roomA.centerX;
            connectionY = roomB.centerY;
        } else {
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

        if (extremeX > connectionX) {
            for (int x = connectionX; x <= extremeX; x++) {
                for (int y = connectionY - passageWidth / 2; y <= connectionY + passageWidth / 2; y++) {
                    if (IsInMapRange(x, y))
                        map[x, y] = roomChar;
                }
            }
        } else {
            for (int x = extremeX; x <= connectionX; x++) {
                for (int y = connectionY - passageWidth / 2; y <= connectionY + passageWidth / 2; y++) {
                    if (IsInMapRange(x, y))
                        map[x, y] = roomChar;
                }
            }
        }

        if (extremeY > connectionY) {
            for (int y = connectionY; y <= extremeY; y++) {
                for (int x = connectionX - passageWidth / 2; x <= connectionX + passageWidth / 2; x++) {
                    if (IsInMapRange(x, y))
                        map[x, y] = roomChar;
                }
            }
        } else {
            for (int y = extremeY; y <= connectionY; y++) {
                for (int x = connectionX - passageWidth / 2; x <= connectionX + passageWidth / 2; x++) {
                    if (IsInMapRange(x, y))
                        map[x, y] = roomChar;
                }
            }
        }
    }

    // Adds random connections between the rooms.
    private void AddRandomConnections() {
        for (int i = 0; i < maxRandomPassages; i++) {
            Room roomA = placedRooms[pseudoRandomGen.Next(0, placedRooms.Count)];
            Room roomB = placedRooms[pseudoRandomGen.Next(0, placedRooms.Count)];
            if (!roomA.connectedRooms.Contains(roomB))
                CreatePassage(roomA, roomB);
        }
    }

    // Adds a room to the list.
    private void AddRoom(int originX, int originY, int roomWidth, int roomHeigth) {
        // If the room makes sense.
        if (IsInMapRange(originX, originY) && IsInMapRange(originX + roomWidth, originY + roomHeigth) && roomWidth > passageWidth && roomHeigth > passageWidth) {
            // Add it to the room list.
            rooms.Add(new Room(originX, originY, roomWidth, roomHeigth));
        } else {
            // Debug.Log("The room is too small or placed outside the map, removing it.");
        }
    }

    // Places a room.
    private void PlaceRoom(int originX, int originY, int roomWidth, int roomHeigth) {
        for (int x = originX; x < roomWidth + originX; x++) {
            for (int y = originY; y < roomHeigth + originY; y++) {
                map[x, y] = roomChar;
            }
        }
    }

    // Stores all information about a room.
    private class Room : IComparable<Room> {
        public int originX;
        public int originY;
        public int centerX;
        public int centerY;
        public int width;
        public int height;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public List<Room> connectedRooms;

        public Room() {
        }

        public Room(int x, int y, int w, int h) {
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
        public int CompareTo(Room otherRoom) {
            return otherRoom.roomSize.CompareTo(roomSize);
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
    }

}