using System.Collections.Generic;
using UnityEngine;

public class StairsGenerator : MonoBehaviour {

    [SerializeField] private int stairsPerLevel = 4;
    [SerializeField] private int stairLength = 4;
    [SerializeField] private char voidChar = '0';
    [SerializeField] private char stairCharUp = '1';
    [SerializeField] private char stairCharDown = '2';
    [SerializeField] private char stairCharLeft = '3';
    [SerializeField] private char stairCharRigth = '4';

    private MapGenerator mapGeneratorScript = null;
    private List<char[,]> maps = null;

    // Places stairs connecting adjacent levels of the map.
    public void GenerateStairs(List<char[,]> ms, MapGenerator mg) {
        mapGeneratorScript = mg;
        maps = ms;

        for (int i = 0; i < maps.Count; i++) {
            if (i > 0) {
                List<Stair> stairList = GetPossibleStairs(i - 1, i);
                // Place the stairs.
                for (int j = 0; j < stairsPerLevel; j++) {
                    // Debug.Log("Extracting a stair from " + stairList.Count / stairsPerLevel * j + " to " + (stairList.Count / stairsPerLevel * (j + 1) - 1) + " out of " + stairList.Count + " stairs.");
                    PlaceStair(stairList[mapGeneratorScript.GetRandomInteger(stairList.Count / stairsPerLevel * j, (stairList.Count / stairsPerLevel * (j + 1) - 1))], i - 1, i);
                }
            }
        }        
    }

    // Returns a list of the possible stairs that can be placed between the two levels passed as parameter.
    private List<Stair> GetPossibleStairs(int bottomMap, int topMap) {
        List<Stair> stairList = new List<Stair>();

        bool[,] bottomMapBool = GetBoolMap(maps[bottomMap]);
        bool[,] topMapBool = GetBoolMap(maps[topMap]);

        for (int x = 0; x < bottomMapBool.GetLength(0) - stairLength; x++) {
            for (int y = 0; y < bottomMapBool.GetLength(1) - stairLength; y++) {
                if (bottomMapBool[x, y] && topMapBool[x, y]) {
                    // Check if an horizontal stair can be placed.
                    for (int i = 0; i < stairLength; i++) {
                        if (!(bottomMapBool[x + i, y] && topMapBool[x + i, y]))
                            break;
                        else if (i == stairLength - 1) {
                            stairList.Add(InstantiatePossibleStair(x, x + i, y, y, mapGeneratorScript.GetRandomBoolean()));
                            for (int j = 0; j < stairLength; j++) {
                                bottomMapBool[x + i, y] = false;
                                topMapBool[x + i, y] = false;
                            }
                        }
                    }
                    // Check if a vertical stair can be placed.
                    for (int i = 0; i < stairLength; i++) {
                        if (!(bottomMapBool[x, y + i] && topMapBool[x, y + i]))
                            break;
                        else if (i == stairLength - 1) {
                            stairList.Add(InstantiatePossibleStair(x, x, y, y + i, mapGeneratorScript.GetRandomBoolean()));
                            for (int j = 0; j < stairLength; j++) {
                                bottomMapBool[x, y + i] = false;
                                topMapBool[x, y + i] = false;
                            }
                        }
                    }
                }
            }
        }

        return stairList;
    }

    // Returns a boolean version of the map where only room tiles are true.
    private bool[,] GetBoolMap(char[,] charMap) {
        bool[,] boolMap = new bool[charMap.GetLength(1), charMap.GetLength(1)];

        char roomChar = mapGeneratorScript.GetRoomChar();

        for (int x = 0; x < charMap.GetLength(0); x++)
            for (int y = 0; y < charMap.GetLength(1); y++)
                if (charMap[x, y] == roomChar)
                    boolMap[x, y] = true;
                else
                    boolMap[x, y] = false;

        return boolMap;
    }

    // Places a stair.
    private void PlaceStair(Stair stair, int bottomMap, int topMap) {
        if (stair.originY == stair.endY) {
            if (stair.originX > stair.endX)
                maps[bottomMap][stair.originX + 1, stair.originY] = stairCharRigth;
            else
                maps[bottomMap][stair.originX - 1, stair.originY] = stairCharLeft;
        } else {
            if (stair.originY > stair.endY)
                maps[bottomMap][stair.originX, stair.originY + 1] = stairCharDown;
            else
                maps[bottomMap][stair.originX, stair.originY - 1] = stairCharUp;
        }

        for (int j = 0; j < stairLength - 2; j++)
            if (stair.originY == stair.endY) {
                maps[topMap][stair.originX + 1 + j, stair.originY] = voidChar;
            } else {
                maps[topMap][stair.originX, stair.originY + 1 + j] = voidChar;
            }
    }

    // Instantiates a new possible stair.
    private Stair InstantiatePossibleStair(int originX, int endX, int originY, int endY, bool reverse) {
        if (reverse)
            return new Stair {
                originX = originX,
                originY = originY,
                endX = endX,
                endY = endY
            };
        else
            return new Stair {
                originX = endX,
                originY = endY,
                endX = originX,
                endY = originY
            };
    }

    private struct Stair {
        public int originX;
        public int endX;
        public int originY;
        public int endY;
    }

}