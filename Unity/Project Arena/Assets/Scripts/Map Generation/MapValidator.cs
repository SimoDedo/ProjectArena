using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class MapValidator : MonoBehaviour {

    [SerializeField] private int maxSize = 200;

    // Validates a map loaded from a text file as a char matrix.
    public int ValidateMap(string path) {
        if (path == null) {
            Debug.Log(path + " is null.");
            return 2;
        } else if (!File.Exists(path)) {
            Debug.Log(path + " doesn't exist.");
            return 2;
        } else {
            try {
                string[] lines = File.ReadAllLines(path);
                int xLenght = lines[0].Length;
                int yLenght = lines.GetLength(0);

                if (xLenght > maxSize || yLenght > maxSize)
                    return 4;

                int spawnPointCount = 0;

                for (int x = 0; x < xLenght; x++) {
                    for (int y = 0; y < yLenght; y++) {
                        if ((x == 0 || y == 0 || x == xLenght - 1 || y == yLenght - 1) && lines[y][x] != 'w')
                            return 3;
                        if (lines[y][x] == 's')
                            spawnPointCount++;
                    }
                }

                if (spawnPointCount == 0)
                    return 3;
                else
                    return 0;
            } catch (Exception) {
                return 3;
            }
        }
    }

    // Validates a multi-level map loaded from a text file as a char matrix.
    public int ValidateMLMap(string path) {
        if (path == null) {
            Debug.Log(path + " is null.");
            return 2;
        } else if (!File.Exists(path)) {
            Debug.Log(path + " doesn't exist.");
            return 2;
        } else {
            try {
                int mapsCount = 1;

                string[] lines = File.ReadAllLines(path);

                int xLenght = lines[0].Length;
                int yLenght = 0;

                foreach (string s in lines) {
                    if (s.Length == 0) {
                        mapsCount++;
                    } else if (mapsCount == 1)
                        yLenght++;
                }

                if (xLenght > maxSize || yLenght > maxSize)
                    return 4;
                if (mapsCount < 2)
                    return 6;

                for (int i = 0; i < mapsCount; i++) {
                    int spawnPointCount = 0;

                    for (int x = 0; x < xLenght; x++) {
                        for (int y = 0; y < yLenght; y++) {
                            if ((x == 0 || y == 0 || x == xLenght - 1 || y == yLenght - 1) && lines[y + i * (yLenght + 1)][x] != 'w')
                                return 6;
                            if (lines[y + i * (yLenght + 1)][x] == 's')
                                spawnPointCount++;
                        }
                    }

                    if (spawnPointCount == 0)
                        return 6;
                };

                return 0;
            } catch (Exception) {
                return 6;
            }
        }
    }

    // Validates a loaded from a string as a genome.
    public int ValidateGeneticMap(string genome) {
        Regex rgx = new Regex(@"(\<\d+,\d+,[1-9]\d*\>)+(\|(\<\d+,\d+,-?[1-9]\d*\>)*)?$");

        if (rgx.IsMatch(genome))
            return 0;
        else
            return 5;
    }

    // Validates a multi-level map loaded from a string as a genome.
    public int ValidateGeneticMLMap(string genome) {
        Regex rgx = new Regex(@"(\<\d+,\d+,[1-9]\d*\>)+(\|(\<\d+,\d+,-?[1-9]\d*\>)+)?(\|\|(((\<\d+,\d+,[1-9]\d*\>)+(\|(\<\d+,\d+,-?[1-9]\d*\>)+)?)|(\<0.\d+,0.\d+,0.\d+,0.\d+,0.\d+\>)))+$");
    
        if (rgx.IsMatch(genome))
            return 0;
        else
            return 5;
    }

}