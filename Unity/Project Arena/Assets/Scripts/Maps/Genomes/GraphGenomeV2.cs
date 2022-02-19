using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Graph;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Maps.Genomes
{
    /// <summary>
    /// TODO
    /// Genome which treats the space as a grid.
    /// Each cell can contain at most one Room which can be connected to the adjacent rooms (if any) indirectly with a
    /// corridor (if they are separated) or directly (by being attached and not having a wall between them)
    /// </summary>
    [Serializable]
    public class GraphGenomeV2
    {
        public int cellsWidth;
        public int cellsHeight;
        public int squareSize;
        public Room[,] rooms;

        public GraphGenomeV2()
        {
        }

        public GraphGenomeV2(int cellsWidth, int cellsHeight, int squareSize, Room[,] rooms)
        {
            this.cellsHeight = cellsHeight;
            this.cellsWidth = cellsWidth;
            this.squareSize = squareSize;
            this.rooms = rooms;
        }

        public bool IsGenomeValid()
        {
            if (rooms.Length == 0)
            {
                // Empty genome is not valid
                return false;
            }

            if (cellsWidth <= 1 || cellsHeight <= 1) // TODO min size?
            {
                // Grid size invalid
                return false;
            }

            foreach (var room in rooms)
            {
                if (room.startingX < 0 || room.startingY < 0 ||
                    room.endingX > cellsWidth || room.endingY > cellsHeight)
                {
                    // Genome contains invalid room.
                    return false;
                }
            }

            return true;
        }

        public int GetHeight()
        {
            return rooms.GetLength(0) * cellsHeight * squareSize;
        }

        public int GetWidth()
        {
            return rooms.GetLength(1) * cellsWidth * squareSize;
        }

        public Area[] ConvertToAreas()
        {
            if (!IsGenomeValid())
            {
                throw new InvalidOperationException("The genome provided is not valid.");
            }

            var numRows = rooms.GetLength(0);
            var numColumns = rooms.GetLength(1);

            var areas = new List<Area>();

            // Resize rooms: If I have two adjacent rooms that should not see each other, I need to resize one in
            // order to prevent them from touching. This is needed because thin walls are not supported in the char map.
            // At the same time, if I have two adjacent rooms but they don't really touch each other, I need to separate
            // them just a little.
            for (var c = 0; c < numColumns; c++)
            {
                for (var r = numRows - 1; r > 0; r--)
                {
                    ShrinkVerticallyIfNeeded(r, c);
                }
            }

            for (var r = 0; r < numRows; r++)
            {
                for (var c = numColumns - 1; c > 0; c--)
                {
                    ShrinkHorizontallyIfNeeded(r, c);
                }
            }

            var cellTreeNumbers = new int[numRows, numColumns];
            var bestTreeNumber = VisitForest(cellTreeNumbers);

            // Start placing the Areas for Rooms
            for (var r = 0; r < numRows; r++)
            {
                for (var c = 0; c < numColumns; c++)
                {
                    var room = rooms[r, c];
                    if (!room.isReal) continue;
                    if (cellTreeNumbers[r, c] != bestTreeNumber) continue;
                    var area = new Area(
                        c * cellsWidth + room.startingX,
                        r * cellsHeight + room.startingY,
                        c * cellsWidth + room.endingX,
                        r * cellsHeight + room.endingY
                        // TODO Dummy room flag when?
                    );
                    areas.Add(area);
                }
            }


            // Place corridors
            for (var r = 1; r < numRows; r++)
            {
                for (var c = 0; c < numColumns; c++)
                {
                    if (cellTreeNumbers[r, c] != bestTreeNumber) continue;
                    PlaceVerticalConnectionsIfNeeded(areas, r, c);
                }
            }

            for (var c = 1; c < numColumns; c++)
            {
                for (var r = 0; r < numRows; r++)
                {
                    if (cellTreeNumbers[r, c] != bestTreeNumber) continue;
                    PlaceHorizontalConnectionsIfNeeded(areas, r, c);
                }
            }

            if (areas.Any(it => it.bottomRow == it.topRow || it.leftColumn == it.rightColumn))
            {
                Debug.LogError("There is an invalid area!");
            }

            return areas.Select(it => ScaleArea(it, squareSize)).ToArray();
        }

        private int VisitForest(int[,] cellTreeNumber)
        {
            var rows = rooms.GetLength(0);
            var columns = rooms.GetLength(1);

            var visitNumber = 1;
            var biggestTree = 0;
            var biggestTreeSize = 0;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    if (cellTreeNumber[r, c] == 0)
                    {
                        var size = VisitTree(cellTreeNumber, visitNumber, r, c);
                        if (size > biggestTreeSize)
                        {
                            biggestTree = visitNumber;
                            biggestTreeSize = size;
                        }

                        visitNumber++;
                    }
                }
            }

            return biggestTree;
        }

        private int VisitTree(int[,] cellTreeNumber, int visitNumber, int r, int c)
        {
            if (cellTreeNumber[r, c] != 0)
            {
                // cell already visited.
                return 0;
            }

            cellTreeNumber[r, c] = visitNumber;
            if (!rooms[r, c].isReal) return 0;

            var totalSize = 1;
            if (r > 0 && rooms[r - 1, c].isConnectedToTheTop)
            {
                totalSize += VisitTree(cellTreeNumber, visitNumber, r - 1, c);
            }

            if (r < rooms.GetLength(0) - 1 && rooms[r, c].isConnectedToTheTop)
            {
                totalSize += VisitTree(cellTreeNumber, visitNumber, r + 1, c);
            }

            if (c > 0 && rooms[r, c - 1].isConnectedToTheRight)
            {
                totalSize += VisitTree(cellTreeNumber, visitNumber, r, c - 1);
            }

            if (c < rooms.GetLength(1) - 1 && rooms[r, c].isConnectedToTheRight)
            {
                totalSize += VisitTree(cellTreeNumber, visitNumber, r, c + 1);
            }

            return totalSize;
        }

        private static Area ScaleArea(Area area, int scale)
        {
            return new Area(
                scale * area.leftColumn,
                scale * area.bottomRow,
                scale * area.rightColumn,
                scale * area.topRow,
                area.isCorridor,
                area.isDummyRoom);
        }

        private void PlaceVerticalConnectionsIfNeeded(List<Area> areas, int r, int c)
        {
            var topRoom = rooms[r, c];
            var bottomRoom = rooms[r - 1, c];

            if (!topRoom.isReal || !bottomRoom.isReal)
            {
                // Rooms are not real
                return;
            }

            if (!bottomRoom.isConnectedToTheTop)
            {
                // Rooms are not connected, nothing to do.
                return;
            }

            var spacedVertically = bottomRoom.endingY != cellsHeight || topRoom.startingY != 0;
            var intersectHorizontally =
                bottomRoom.startingX < topRoom.endingX && topRoom.startingX < bottomRoom.endingX;

            if (!spacedVertically && intersectHorizontally)
            {
                // Rooms are touching, nothing to do
                return;
            }

            var topY = r * cellsHeight + topRoom.startingY;
            var bottomY = (r - 1) * cellsHeight + bottomRoom.endingY;

            var maxOfMinX = Mathf.Max(bottomRoom.startingX, topRoom.startingX);
            var minOfMaxX = Mathf.Min(bottomRoom.endingX, topRoom.endingX);

            if (maxOfMinX < minOfMaxX)
            {
                // We can place a straight corridor.
                // var startX = c * cellsWidth + Random.Range(maxOfMinX, minOfMaxX);
                var startX = c * cellsWidth + (maxOfMinX + minOfMaxX) / 2;
                areas.Add(new Area(
                    startX,
                    bottomY,
                    startX + 1,
                    topY,
                    true));
                return;
            }

            // We cannot place a straight corridor. Place an twisted one.

            // Find X1 and X2 of the corridor.
            var middleXTopRoom = c * cellsWidth + (topRoom.startingX + topRoom.endingX) / 2;
            var middleXBottomRoom = c * cellsWidth + (bottomRoom.startingX + bottomRoom.endingX) / 2;

            // var corridorY = Random.Range(bottomY, topY);
            var corridorY = (bottomY + topY) / 2;

            if (bottomY != corridorY)
            {
                // Vertical corridor from bottom room
                areas.Add(new Area(middleXBottomRoom, bottomY, middleXBottomRoom + 1, corridorY, true));
            }

            if (topY != corridorY)
            {
                // Vertical corridor from top room
                areas.Add(new Area(middleXTopRoom, corridorY, middleXTopRoom + 1, topY, true));
            }

            var startingX = Mathf.Min(middleXBottomRoom, middleXTopRoom);
            var endingX = Mathf.Max(middleXBottomRoom, middleXTopRoom) + 1;
            // Horizontal corridor
            areas.Add(new Area(
                startingX,
                corridorY,
                endingX,
                corridorY + 1,
                true));
        }

        private void PlaceHorizontalConnectionsIfNeeded(List<Area> areas, int r, int c)
        {
            var leftRoom = rooms[r, c - 1];
            var rightRoom = rooms[r, c];

            if (!rightRoom.isReal || !leftRoom.isReal)
            {
                // Rooms are not real
                return;
            }

            if (!leftRoom.isConnectedToTheRight)
            {
                // Rooms are not connected, nothing to do.
                return;
            }

            var spacedHorizontally = leftRoom.endingX != cellsWidth || rightRoom.startingX != 0;
            var intersectVertically =
                leftRoom.startingY < rightRoom.endingY && rightRoom.startingY < leftRoom.endingY;

            if (!spacedHorizontally && intersectVertically)
            {
                // Rooms are touching, nothing to do
                return;
            }

            var rightX = c * cellsWidth + rightRoom.startingX;
            var leftX = (c - 1) * cellsWidth + leftRoom.endingX;

            var maxOfMinY = Mathf.Max(leftRoom.startingY, rightRoom.startingY);
            var minOfMaxY = Mathf.Min(leftRoom.endingY, rightRoom.endingY);

            if (maxOfMinY < minOfMaxY)
            {
                // We can place a straight corridor.
                var startY = r * cellsHeight + (maxOfMinY + minOfMaxY) / 2;
                areas.Add(new Area(
                    leftX,
                    startY,
                    rightX,
                    startY + 1,
                    true));
                return;
            }

            // We cannot place a straight corridor. Place an twisted one.
            // Find Y1 and Y2 of the corridor.
            var middleYRightRoom = r * cellsHeight + (rightRoom.startingY + rightRoom.endingY) / 2;
            var middleYLeftRoom = r * cellsHeight + (leftRoom.startingY + leftRoom.endingY) / 2;

            var corridorX = (leftX + rightX) / 2;
            // Vertical corridor from bottom room
            areas.Add(new Area(
                leftX,
                middleYLeftRoom,
                corridorX,
                middleYLeftRoom + 1,
                true));

            // Vertical corridor from top room
            areas.Add(new Area(
                corridorX,
                middleYRightRoom,
                rightX,
                middleYRightRoom + 1,
                true));

            var startingY = Mathf.Min(middleYLeftRoom, middleYRightRoom);
            var endingY = Mathf.Max(middleYLeftRoom, middleYRightRoom) + 1;
            // Horizontal corridor
            areas.Add(new Area(
                corridorX,
                startingY,
                corridorX + 1,
                endingY,
                true));
        }

        private void ShrinkVerticallyIfNeeded(int r, int c)
        {
            var bottomRoom = rooms[r - 1, c];
            var topRoom = rooms[r, c];
            if (!bottomRoom.isReal || !topRoom.isReal)
            {
                // A Room is not real
                return;
            }

            var spacedVertically = bottomRoom.endingY != cellsHeight || topRoom.startingY != 0;
            if (spacedVertically)
            {
                // There is space for a corridor or to keep the rooms separated.
                return;
            }

            var intersectHorizontally = // TODO Check
                bottomRoom.startingX < topRoom.endingX && topRoom.startingX < bottomRoom.endingX;

            if (bottomRoom.isConnectedToTheTop && intersectHorizontally)
            {
                // The rooms touch each other, so there is no need to make space for a corridor.
                return;
            }

            if (!bottomRoom.isConnectedToTheTop && !intersectHorizontally)
            {
                // The rooms are separated and do not need a corridor.
                return;
            }


            // We need space, either to separate the two rooms or to fit a corridor in the middle.
            var oldRoom = bottomRoom;
            rooms[r - 1, c] = new Room(
                oldRoom.startingX,
                oldRoom.endingX,
                Mathf.Max(0, oldRoom.startingY - 1),
                oldRoom.endingY - 1,
                oldRoom.isConnectedToTheRight,
                oldRoom.isConnectedToTheTop
            );
        }

        private void ShrinkHorizontallyIfNeeded(int r, int c)
        {
            var leftRoom = rooms[r, c - 1];
            var rightRoom = rooms[r, c];
            if (!leftRoom.isReal || !rightRoom.isReal)
            {
                // A Room is not real
                return;
            }

            var spacedHorizontally = leftRoom.endingX != cellsWidth || rightRoom.startingX != 0;
            if (spacedHorizontally)
            {
                // There is space for a corridor or to keep the rooms separated.
                return;
            }

            var intersectVertically = // TODO Check
                leftRoom.startingY < rightRoom.endingY && rightRoom.startingY < leftRoom.endingY;

            if (leftRoom.isConnectedToTheRight && intersectVertically)
            {
                // The rooms touch each other, so there is no need to make space for a corridor.
                return;
            }

            if (!leftRoom.isConnectedToTheRight && !intersectVertically)
            {
                // The rooms are separated and do not need a corridor.
                return;
            }


            // We need space, either to separate the two rooms or to fit a corridor in the middle.
            var oldRoom = leftRoom;
            rooms[r, c - 1] = new Room(
                Mathf.Max(0, oldRoom.startingX - 1),
                oldRoom.endingX - 1,
                oldRoom.startingY,
                oldRoom.endingY,
                oldRoom.isConnectedToTheRight,
                oldRoom.isConnectedToTheTop
            );
        }

        public static GraphGenomeV2 Default = new GraphGenomeV2(
            10, 10, 3, new[,]
            {
                {
                    new Room(0, 10, 0, 10, true, false),
                    new Room(7, 9, 2, 4, false, true),
                },
                {
                    new Room(0, 10, 0, 10, true, false),
                    new Room(3, 5, 6, 9, false, false),
                }
            }
        );
    }


    // FIXME I'd like this to be a readonly struct, but there are some problems with serialization in that way...
    /// <summary>
    /// Represents the position and dimension of a room inside a cell in the graph.
    /// (x,y) coordinates are to be interpreted as:
    /// - x: Left and right. Going to the right increases the coordinate;
    /// - y: Down and up. Going to the upwards increases the coordinate.
    /// </summary>
    // A room starts at the coordinate specified and ends just before the coordinate specified.
    // So, for example, a room with X start and end point of 1 and 3 would start at position 1, fill the squares 1 and 2
    // and end at the end of square 2 / start of square 3.
    [Serializable]
    public class Room
    {
        // If false, this instance is just used to fill the array but otherwise there is no room in that space.
        // In that situation, every variable has value 0.
        public bool isReal;
        public int startingX;
        public int endingX;
        public int startingY;
        public int endingY;
        public bool isConnectedToTheRight;
        public bool isConnectedToTheTop;

        public Room()
        {
            
        }

    // public Room(int startingX, int endingX, int startingY, int endingY, bool isConnectedToTheRight,
    //         bool isConnectedToTheTop) : this(true, startingX, endingX, startingY, endingY, isConnectedToTheRight,
    //         isConnectedToTheTop)
    //     {
    //     }

        public Room(int startingX, int endingX, int startingY, int endingY, bool isConnectedToTheRight,
            bool isConnectedToTheTop)
        {
            isReal = true;
            this.startingX = startingX;
            this.endingX = endingX;
            this.startingY = startingY;
            this.endingY = endingY;
            this.isConnectedToTheRight = isConnectedToTheRight;
            this.isConnectedToTheTop = isConnectedToTheTop;
        }
    }
}