using System;
using System.Collections.Generic;
using UnityEngine;

public class DivisiveMapGenerator : MapGenerator {

    // Probability of a room being divided again.
    [Header("Divisive generation")] [SerializeField, Range(0, 45)] private int roomDivideProbability;
    // Probability of a room not being erased.
    [SerializeField, Range(0, 45)] private int roomSurviveProbability;
    // Percentual lower bound of where a room can be divided.
    [SerializeField, Range(10, 90)] private int divideLowerBound;
    // Percentual upper bound of where a room can be divided.
    [SerializeField, Range(10, 90)] private int divideUpperBound;
    // Passage width.
    [SerializeField] private int passageWidth = 5;
    // Border size.
    [SerializeField] private int borderSize = 5;

    private List<Room> roomList;

    private void Start() {
        SetReady(true);
    }

    public override char[,] GenerateMap() {
        map = new char[width, height];

        InitializePseudoRandomGenerator();

        FillMap();

        roomList = new List<Room>();

        if (width > height)
            DivideRoom(0, 0, width, height, true);
        else
            DivideRoom(0, 0, width, height, false);

        ProcessMap();

        if (createTextFile)
            SaveMapAsText();

        return map;
    }

    // Processes the map.
    private void ProcessMap() {
        PopulateMap();
    }

    // Computes the maximum room depth as the number of times the smallest room, sized as the passage,
    // can be contained in the smallest map dimension.
    private int ComputeMaximumDepth() {
        return width < height ? width / passageWidth : height / passageWidth;
    }

    // Divides a room in subrooms or stops there.
    private void DivideRoom(int originX, int originY, int roomWidth, int roomHeigth, bool horizontal) {
        int random = pseudoRandomGen.Next(0, 100);

        if (random > (100 - roomSurviveProbability)) {
            // Make the room white and add it to the rooms return.
            AddRoom(originX, originY, roomWidth, roomHeigth);
        } else if (random < roomDivideProbability && roomWidth > passageWidth && roomHeigth > passageWidth) {
            // Divivde the room.
            if (horizontal) {
                int division = GetDivisionPoint(roomHeigth);
                DivideRoom(originX, originY, roomWidth, division, false);
                DivideRoom(originX, originY + division + 1, roomWidth, roomHeigth - division - 1, false);
            } else {
                int division = GetDivisionPoint(roomWidth);
                DivideRoom(originX, originY, division, roomHeigth, true);
                DivideRoom(originX + division + 1, originY, roomWidth - division - 1, roomHeigth, true);
            }
        }
    }

    // Returns the point where the room must be divided.
    private int GetDivisionPoint(int roomWidth) {
        return (int)roomWidth * pseudoRandomGen.Next(divideLowerBound, divideUpperBound) / 100;
    }

    // Adds a room in the map.
    private void AddRoom(int originX, int originY, int roomWidth, int roomHeigth) {
        // Restrict the room by one tile.
        originX++;
        originY++;
        roomWidth -= 2;
        roomHeigth -= 2;

        Debug.Log("Creating a " + roomWidth + "x" + roomHeigth + "room.");

        // If the restricted room still makes sense.
        if (IsInMapRange(originX, originY) && IsInMapRange(originX + roomWidth, originY + roomHeigth) && roomWidth > 0 && roomHeigth > 0) {
            // Draw the room in the map.
            for (int x = originX + 1; x < roomWidth; x++) {
                for (int y = originY + 1; y < roomHeigth; y++) {
                    map[x, y] = roomChar;
                }
            }

            // Add it to the room list.
            roomList.Add(new Room(originX, originY, roomWidth, roomHeigth));
        }
    }

    // Fills the map with wall cells.
    private void FillMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < width; y++) {
                map[x, y] = wallChar;
            }
        }
    }

    // Stores all information about a room.
    private class Room {
        public int originX;
        public int originY;
        public int width;
        public int height;

        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room() {
        }

        public Room(int x, int y, int w, int h) {
            originX = x;
            originY = y;
            width = w;
            height = h;
        }
    }

}