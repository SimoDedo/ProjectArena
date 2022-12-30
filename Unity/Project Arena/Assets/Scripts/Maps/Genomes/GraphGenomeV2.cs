using System;
using System.Collections.Generic;
using System.Linq;
using Graph;
using UnityEngine;

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
        public float mapScale;
        public Room[,] rooms;
        public bool[,] verticalCorridors;
        public bool[,] horizontalCorridors;

        public GraphGenomeV2()
        {
        }

        public GraphGenomeV2(int cellsWidth, int cellsHeight, int squareSize, float mapScale, Room[,] rooms, bool[,] verticalCorridors,
            bool[,] horizontalCorridors)
        {
            this.cellsHeight = cellsHeight;
            this.cellsWidth = cellsWidth;
            this.squareSize = squareSize;
            this.mapScale = mapScale;
            this.rooms = rooms;
            this.verticalCorridors = verticalCorridors;
            this.horizontalCorridors = horizontalCorridors;
        }

        public bool IsGenomeValid()
        {
            if (rooms == null || verticalCorridors == null || horizontalCorridors == null)
            {
                // Missing genome data
                return false;
            }

            if (rooms.Length == 0)
            {
                // Genome must have at least one room
                return false;
            }

            if (cellsWidth <= 1 || cellsHeight <= 1) // TODO min size?
            {
                // Grid size invalid
                return false;
            }

            // if (mapScale < 1 || mapScale > 3)
            // {
            //     return false;
            // }

            var numRows = rooms.GetLength(0);
            var numColumns = rooms.GetLength(1);

            if (verticalCorridors.GetLength(0) != numRows - 1 || verticalCorridors.GetLength(1) != numColumns)
            {
                // Invalid number of vertical corridors
                return false;
            }

            if (horizontalCorridors.GetLength(0) != numRows || horizontalCorridors.GetLength(1) != numColumns - 1)
            {
                // Invalid number of horizontal corridors
                return false;
            }


            foreach (var room in rooms)
            {
                if (room.leftColumn < 0 || room.bottomRow < 0 ||
                    room.rightColumn > cellsWidth || room.topRow > cellsHeight)
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

            var visitedRooms = FindClosestConnectedComponent();

            // Start placing the Areas for Rooms
            for (var r = 0; r < numRows; r++)
            {
                for (var c = 0; c < numColumns; c++)
                {
                    var room = rooms[r, c];
                    if (!room.isReal) continue;
                    if (!visitedRooms[r, c]) continue;
                    var area = new Area(
                        c * cellsWidth + room.leftColumn,
                        r * cellsHeight + room.bottomRow,
                        c * cellsWidth + room.rightColumn,
                        r * cellsHeight + room.topRow
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
                    if (!visitedRooms[r, c]) continue;
                    PlaceVerticalConnectionsIfNeeded(areas, r, c);
                }
            }

            for (var c = 1; c < numColumns; c++)
            {
                for (var r = 0; r < numRows; r++)
                {
                    if (!visitedRooms[r, c]) continue;
                    PlaceHorizontalConnectionsIfNeeded(areas, r, c);
                }
            }

            if (areas.Any(it => it.bottomRow == it.topRow || it.leftColumn == it.rightColumn))
            {
                Debug.LogError("There is an invalid area!");
            }

            return areas.Select(it => ScaleArea(it, squareSize)).ToArray();
        }

        private bool[,] FindClosestConnectedComponent()
        {
            var rows = rooms.GetLength(0);
            var columns = rooms.GetLength(1);

            var visitedRooms = new bool[rows, columns];

            var closestRow = 0;
            var closestColumn = 0;
            var closestDistance = int.MaxValue;

            var centerRow = (rows - 1) / 2f;
            var centerCol = (columns - 1) / 2f;

            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    if (!rooms[r, c].isReal) continue;
                    
                    var roomScore = Math.Abs(centerRow - r) + Math.Abs(centerCol - c);
                    
                    if (roomScore > closestDistance) continue;
                    
                    closestColumn = c;
                    closestRow = r;
                    closestDistance = (int) roomScore;
                }
            }

            VisitTree2(closestRow, closestColumn, visitedRooms);

            return visitedRooms;
        }
        
        private void VisitTree2(int r, int c, bool[,] visited)
        {
            if (visited[r, c])
            {
                // cell already visited.
                return;
            }

            if (!rooms[r, c].isReal) return;
            visited[r, c] = true;

            if (r > 0 && verticalCorridors[r - 1, c])
            {
                VisitTree2(r - 1, c, visited);
            }

            if (r < rooms.GetLength(0) - 1 && verticalCorridors[r, c])
            {
                VisitTree2( r + 1, c, visited);
            }

            if (c > 0 && horizontalCorridors[r, c - 1])
            {
                VisitTree2( r, c - 1, visited);
            }

            if (c < rooms.GetLength(1) - 1 && horizontalCorridors[r, c])
            {
                VisitTree2( r, c + 1, visited);
            }
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

            if (!verticalCorridors[r - 1, c])
            {
                // Rooms are not connected, nothing to do.
                return;
            }

            var spacedVertically = bottomRoom.topRow != cellsHeight || topRoom.bottomRow != 0;
            var intersectHorizontally =
                bottomRoom.leftColumn < topRoom.rightColumn && topRoom.leftColumn < bottomRoom.rightColumn;

            if (!spacedVertically && intersectHorizontally)
            {
                // Rooms are touching, nothing to do
                return;
            }

            var topY = r * cellsHeight + topRoom.bottomRow;
            var bottomY = (r - 1) * cellsHeight + bottomRoom.topRow;

            var maxOfMinX = Mathf.Max(bottomRoom.leftColumn, topRoom.leftColumn);
            var minOfMaxX = Mathf.Min(bottomRoom.rightColumn, topRoom.rightColumn);

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
            var middleXTopRoom = c * cellsWidth + (topRoom.leftColumn + topRoom.rightColumn) / 2;
            var middleXBottomRoom = c * cellsWidth + (bottomRoom.leftColumn + bottomRoom.rightColumn) / 2;

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

            if (!horizontalCorridors[r, c - 1])
            {
                // Rooms are not connected, nothing to do.
                return;
            }

            var spacedHorizontally = leftRoom.rightColumn != cellsWidth || rightRoom.leftColumn != 0;
            var intersectVertically =
                leftRoom.bottomRow < rightRoom.topRow && rightRoom.bottomRow < leftRoom.topRow;

            if (!spacedHorizontally && intersectVertically)
            {
                // Rooms are touching, nothing to do
                return;
            }

            var rightX = c * cellsWidth + rightRoom.leftColumn;
            var leftX = (c - 1) * cellsWidth + leftRoom.rightColumn;

            var maxOfMinY = Mathf.Max(leftRoom.bottomRow, rightRoom.bottomRow);
            var minOfMaxY = Mathf.Min(leftRoom.topRow, rightRoom.topRow);

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
            var middleYRightRoom = r * cellsHeight + (rightRoom.bottomRow + rightRoom.topRow) / 2;
            var middleYLeftRoom = r * cellsHeight + (leftRoom.bottomRow + leftRoom.topRow) / 2;

            var corridorX = (leftX + rightX) / 2;
            // Vertical corridor from bottom room
            if (leftX != corridorX)
            {
                areas.Add(new Area(
                    leftX,
                    middleYLeftRoom,
                    corridorX,
                    middleYLeftRoom + 1,
                    true));
            }

            if (corridorX != rightX)
            {
                // Vertical corridor from top room
                areas.Add(new Area(
                    corridorX,
                    middleYRightRoom,
                    rightX,
                    middleYRightRoom + 1,
                    true));
            }

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

            var spacedVertically = bottomRoom.topRow != cellsHeight || topRoom.bottomRow != 0;
            if (spacedVertically)
            {
                // There is space for a corridor or to keep the rooms separated.
                return;
            }

            var intersectHorizontally = // TODO Check
                bottomRoom.leftColumn < topRoom.rightColumn && topRoom.leftColumn < bottomRoom.rightColumn;

            if (verticalCorridors[r - 1, c] && intersectHorizontally)
            {
                // The rooms touch each other, so there is no need to make space for a corridor.
                return;
            }

            if (!verticalCorridors[r - 1, c] && !intersectHorizontally)
            {
                // The rooms are separated and do not need a corridor.
                return;
            }


            // We need space, either to separate the two rooms or to fit a corridor in the middle.
            var oldRoom = bottomRoom;
            rooms[r - 1, c] = new Room(
                oldRoom.leftColumn,
                oldRoom.rightColumn,
                Mathf.Max(0, oldRoom.bottomRow - 1),
                oldRoom.topRow - 1
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

            var spacedHorizontally = leftRoom.rightColumn != cellsWidth || rightRoom.leftColumn != 0;
            if (spacedHorizontally)
            {
                // There is space for a corridor or to keep the rooms separated.
                return;
            }

            var intersectVertically = // TODO Check
                leftRoom.bottomRow < rightRoom.topRow && rightRoom.bottomRow < leftRoom.topRow;

            if (horizontalCorridors[r, c - 1] && intersectVertically)
            {
                // The rooms touch each other, so there is no need to make space for a corridor.
                return;
            }

            if (!horizontalCorridors[r, c - 1] && !intersectVertically)
            {
                // The rooms are separated and do not need a corridor.
                return;
            }


            // We need space, either to separate the two rooms or to fit a corridor in the middle.
            var oldRoom = leftRoom;
            rooms[r, c - 1] = new Room(
                Mathf.Max(0, oldRoom.leftColumn - 1),
                oldRoom.rightColumn - 1,
                oldRoom.bottomRow,
                oldRoom.topRow
            );
        }

        public static GraphGenomeV2 Default = new GraphGenomeV2(
            10, 10, 3, 1, new[,]
            {
                {
                    new Room(0, 10, 0, 10),
                    new Room(7, 9, 2, 4)
                },
                {
                    new Room(0, 10, 0, 10),
                    new Room(3, 5, 6, 9)
                }
            }, new[,] {{false, true}},
            new[,] {{true}, {true}}
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
        public int leftColumn;
        public int rightColumn;
        public int bottomRow;
        public int topRow;

        public Room()
        {
        }


        public Room(int leftColumn, int rightColumn, int bottomRow, int topRow)
        {
            isReal = true;
            this.leftColumn = leftColumn;
            this.rightColumn = rightColumn;
            this.bottomRow = bottomRow;
            this.topRow = topRow;
        }
    }
}