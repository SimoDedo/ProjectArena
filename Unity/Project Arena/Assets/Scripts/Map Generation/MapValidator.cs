using System;
using System.IO;
using UnityEngine;

public class MapValidator : MonoBehaviour {

    // Validates the imported map.
    public int ValidateMap(string path) {
        if (path == null) {
            Debug.Log(path + " is null.");
            return 1;
        } else if (!File.Exists(path)) {
            Debug.Log(path + " doesn't exist.");
            return 1;
        } else {
            try {
                string[] lines = File.ReadAllLines(path);
                int xLenght = lines[0].Length;
                int yLenght = lines.GetLength(0);

                int spawnPointCount = 0;

                for (int x = 0; x < xLenght; x++) {
                    for (int y = 0; y < yLenght; y++) {
                        if ((x == 0 || y == 0 || x == xLenght - 1 || y == yLenght - 1) && lines[y][x] != 'w')
                            return 2;
                        if (lines[y][x] == 's')
                            spawnPointCount++;
                    }
                }

                if (spawnPointCount == 0)
                    return 2;
                else
                    return 0;
            } catch (Exception) {
                return 2;
            }
        }
    }

}