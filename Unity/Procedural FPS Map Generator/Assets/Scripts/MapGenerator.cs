using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class MapGenerator : MonoBehaviour {

	// Do I have to generate my seed?
	public bool useRandomSeed = true;
	// Seed used to generate the map.
	public string seed = null;

	// Map width.
	public int width = 100;
	// Map height.
	public int height = 100;
	// Border size.
	public int borderSize = 5;
	// Wall height.
	public int wallHeight = 5;
	// Passage width.
	public int passageWidth = 5;
	// Minimum size of a wall region.
	public int wallThresholdSize = 50;
	// Minimum size of a room region.
	public int roomThresholdSize = 50;
	// How much the map will be randomly filled at the beginning.
	[Range(0, 100)]
	public int ramdomFillPercent = 50;
	// Number of smoothing iterations to be done.
	[Range(0, 3)]
	public int smoothingIterations = 3;
	// You must have more than this number of neighbour to became wall.
	[Range(0, 9)]
	public int neighbourTileLimitHigh = 4;
	// You must have less than this number of neighbour to became room.
	[Range(0, 9)]
	public int neighbourTileLimitLow = 4;

	// Char that denotes a room;
	public char charRoom = 'v';
	// Char that denotes a wall;
	public char charWall = 'w';

	// Do I have to create a  mesh representation?
	public bool createMesh = false;
	// Object containing the Map Builder script.
	public GameObject mapBuilder;

	// Do I have to create a .txt output?
	public bool createTextFile = false;
	// Path where to save the text map.
	public string textFilePath = null;

	// Map, defined as a grid of chars.
	private char [,] map;
	// Hash of the seed.
	private int hash;

	void Start() {
		GenerateMap();
	}

	void Update() {
		if (Input.GetMouseButtonDown(0))
			GenerateMap();
	}

	// Generates the map.
	private void GenerateMap() {
		map = new char[width, height];
		
		RandomFillMap();

		for (int i = 0; i < smoothingIterations; i++)
			SmoothMap();

		ProcessMap();

		char [,] borderedMap = new char[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++) {
			for (int y = 0; y < borderedMap.GetLength(1); y++) {
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				else
					borderedMap[x, y] = charWall;
			}
		}

		if (createMesh) {
			if (mapBuilder != null) {
				MapBuilderFromText mapBuilderFromText = mapBuilder.GetComponent<MapBuilderFromText>();
				mapBuilderFromText.BuildMap(borderedMap, charWall, 1, wallHeight);
			} else
				Debug.Log("Please attach a Map Builder to the script.");
		}

		if (createTextFile)
			SaveMapAsText();
	}

	// Cleans the map.
	private void ProcessMap() {
		List<List<Coord>> wallRegions = GetRegions(charWall);

		foreach (List<Coord> wallRegion in wallRegions) {
			if (wallRegion.Count < wallThresholdSize) {
				foreach (Coord tile in wallRegion) {
					map[tile.tileX, tile.tileY] = charRoom;
				}
			}
		}

		List<List<Coord>> roomRegions = GetRegions(charRoom);
		List<Room> survivingRooms = new List<Room>();

		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map[tile.tileX, tile.tileY] = charWall;
				}
			} else {
				survivingRooms.Add(new Room(roomRegion, map, charWall));
			}
		}	

		// If there are at least two rooms.
		if (survivingRooms.Count > 0) {
			survivingRooms.Sort();
			survivingRooms[0].isMainRoom = true;
			survivingRooms[0].isAccessibleFromMainRoom = true;

			ConnectClosestRooms(survivingRooms);
		}
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

	// Draws a circe of a given radius around a point.
	private void DrawCircle (Coord c, int r) {
		for (int x = - r; x <= r; x++) {
			for (int y = - r; y <= r; y++) {
				if (x * x + y * y <= r) {
					int drawX = c.tileX + x;
					int drawY = c.tileY + y;
					
					if (IsInMapRange(drawX, drawY))
						map[drawX, drawY] = charRoom;
				}
			}
		}
	}


	// Returns a list of coordinates for each point in the line.
    private List<Coord> GetLine(Coord from, Coord to) {
        List<Coord> line = new List<Coord> ();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign (dx);
        int gradientStep = Math.Sign (dy);

        int longest = Mathf.Abs (dx);
        int shortest = Mathf.Abs (dy);

        if (longest < shortest) {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign (dy);
            gradientStep = Math.Sign (dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i =0; i < longest; i ++) {
            line.Add(new Coord(x,y));

            if (inverted) {
                y += step;
            }
            else {
                x += step;
            }

            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest) {
                if (inverted) {
                    x += gradientStep;
                }
                else {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

	// Converts coordinates to world position.
	private Vector3 CoordToWorldPoint(Coord tile) {
		return new Vector3(- width / 2 + .5f + tile.tileX, 2, - height / 2 + .5f + tile.tileY);
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
		List<Coord> tiles = new List<Coord> ();
        int[,] mapFlags = new int[width,height];
        char tileType = map [startX, startY];

        Queue<Coord> queue = new Queue<Coord> ();
        queue.Enqueue (new Coord (startX, startY));
        mapFlags [startX, startY] = 1;

        while (queue.Count > 0) {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

			for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
				for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if (IsInMapRange(x,y) && (y == tile.tileY || x == tile.tileX)) {
                        if (mapFlags[x,y] == 0 && map[x,y] == tileType) {
                            mapFlags[x,y] = 1;
                            queue.Enqueue(new Coord(x,y));
                        }
                    }
                }
            }
        }

        return tiles;
	}

	// Tells if the "general" (full/room) type of two tiles is the same.
	private bool IsSameGeneralType(char tyleType, char t) {
		if (tyleType == charWall)
			return t == charWall;
		 else
			return t != charWall;
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
					map[x, y] = charWall;
				else	
					map[x, y] = (pseudoRandomGen.Next(0, 100) < ramdomFillPercent) ? charWall : charRoom;
			}	
		}
	}

	// Smooths the map.
	private void SmoothMap() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int neighbourWallTiles = GetSurroundingWallCount(x, y);

				if (neighbourWallTiles > neighbourTileLimitHigh)
					map[x, y] = charWall;
				else if  (neighbourWallTiles < neighbourTileLimitLow)
					map[x, y] = charRoom;
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
				    wallCount ++;
			}
		}

		return wallCount;
	}

	// Coordinates of a tile.
	private struct Coord {
		public int tileX;
		public int tileY;

		public Coord (int x, int y) {
			tileX = x;
			tileY = y;
		}
	}

	// Return 1 if the tile is a wall, 0 otherwise.
	private int getMapTileAsNumber(int x, int y) {
		if (map[x, y] == charWall)
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

		public Room () {			
		}

		public Room (List<Coord> roomTiles, char[,] map, char charWall) {
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();
			edgeTiles = new List<Coord>();

			// For each tile of the room I get the neighbours that are walls obtaining the edge of the room.
			foreach (Coord tile in tiles) {
				for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
					for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
						if (x == tile.tileX || y == tile.tileY) {
							if (map[x, y] == charWall)
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

		// Saves the map in a text file.
	private void SaveMapAsText() {
		if (textFilePath == null && !Directory.Exists(textFilePath)) {
			Debug.Log("ERROR: error while retrieving the folder, please insert a valid path.");
		} else {
			try {
				String textMap = "";

				for (int x = 0; x < width; x++) {
					for (int y = 0; y < height; y++) {
						textMap = textMap + map[x,y];
					}
					if (x < width - 1)
						textMap = textMap + "\n";
				}

				System.IO.File.WriteAllText(@textFilePath + "/map_" + hash.ToString() + ".txt" , textMap);
			} catch (Exception) {
				Debug.Log("ERROR: error while retrieving the folder, please insert a valid path and check its permissions.");
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
	/*
	private void OnDrawGizmos() {
		if (map != null) {
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					Gizmos.color = (map[x, y] == charWall) ? Color.black : Color.white;
					Vector3 position = new Vector3(- width / 2 + x + 0.5f, 0, - height / 2 + y + 0.5f);
					Gizmos.DrawCube(position, Vector3.one);
				}
			}
		}

	}
	*/	

}