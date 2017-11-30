using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiggerMapGenerator : MapGenerator {

    [Header("Divisive generation")] [SerializeField, Range(0, 100)] private int forwardProbability;
    [SerializeField, Range(0, 100)] private int leftProbability;
    [SerializeField, Range(0, 100)] private int rigthProbability;
    [SerializeField, Range(0, 100)] private int visitedProbability;
    [SerializeField, Range(0, 100)] private int stairProbability;
    [SerializeField, Range(0, 100)] private int roomPercentage;

    [Header("AB generation")] [SerializeField] private bool useABGeneration;

    private List<Coord> visitedTiles;

    private int currentX;
    private int currentY;
    private int direction;

    private void Start() {
        SetReady(true);
    }

    // Generates a map.
    public override char[,] GenerateMap() {
        map = new char[width, height];

        // Parse the genome if needed.
        if (useABGeneration)
            ParseGenome();

        ValidateProbabilities();

        InitializePseudoRandomGenerator();

        FillMap();

        DigMap();

        if (!useABGeneration)
            PopulateMap();

        AddBorders();

        if (createTextFile && !useABGeneration)
            SaveMapAsText();

        return map;
    }

    // Digs the map. The direction of the digger is coded as: 1 for up, 2 for rigth, 3 for down and 4 for left.
    // Turning left means decreasing the direction by 1 in a circular fashion and vice versa for turning rigth.
    private void DigMap() {
        currentX = width / 2;
        currentY = height / 2;
        direction = 1;

        int stopCount = width * height * roomPercentage / 100;

        visitedTiles = new List<Coord> {
            new Coord(currentX, currentY)
        };
        map[currentX, currentY] = roomChar;

        while (visitedTiles.Count < stopCount) {
            int nextAction = pseudoRandomGen.Next(0, 100);
            if (nextAction < forwardProbability) {
                MoveDiggerForward();
            } else if (nextAction < leftProbability) {
                direction = CircularIncrease(direction, 1, 4, false);
                MoveDiggerForward();
            } else if (nextAction < rigthProbability) {
                direction = CircularIncrease(direction, 1, 4, false);
                MoveDiggerForward();
            } else if (nextAction < visitedProbability) {
                MoveDiggerRandomly();
            } else {
                map[currentX, currentY] = CircularIncrease(direction, 1, 4, GetRandomBoolean()).ToString()[0];
            }
        }

        Debug.Log("Visited: " + visitedTiles);
    }

    // Moves the digger forward.
    private void MoveDiggerForward() {
        switch (direction) {
            case 1:
                if (IsInMapRange(currentX, currentY - 1)) {
                    currentY -= 1;
                    map[currentX, currentY] = roomChar;
                } else
                    MoveDiggerRandomly();
                break;
            case 2:
                if (IsInMapRange(currentX + 1, currentY)) {
                    currentX += 1;
                    map[currentX, currentY] = roomChar;
                } else
                    MoveDiggerRandomly();
                break;
            case 3:
                if (IsInMapRange(currentX, currentY + 1)) {
                    currentY += 1;
                    map[currentX, currentY] = roomChar;
                } else
                    MoveDiggerRandomly();
                break;
            case 4:
                if (IsInMapRange(currentX - 1, currentY)) {
                    currentX -= 1;
                    map[currentX, currentY] = roomChar;
                } else
                    MoveDiggerRandomly();
                break;
        }

        map[currentX, currentY] = roomChar;
        visitedTiles.Add(new Coord(currentX, currentY));
    }

    // Moves the digger to a random tile.
    private void MoveDiggerRandomly() {
        Coord c = visitedTiles[pseudoRandomGen.Next(0, visitedTiles.Count - 1)];
        currentX = c.tileX;
        currentY = c.tileY;
    }

    // Makes a circular sum.
    private int CircularIncrease(int value, int min, int max, bool increase) {
        if (increase)
            return (value == max) ? min : value + 1;
        else
            return (value == min) ? max : value - 1;
    }

    // Validates the probabilities and corrects them if needed.
    private void ValidateProbabilities() {
        int totalSum = forwardProbability;

        leftProbability = ScaleProbability(leftProbability, totalSum);
        totalSum = leftProbability;

        rigthProbability = ScaleProbability(rigthProbability, totalSum);
        totalSum = rigthProbability;

        visitedProbability = ScaleProbability(visitedProbability, totalSum);
        totalSum = visitedProbability;

        stairProbability = ScaleProbability(stairProbability, totalSum);
        totalSum = stairProbability;

        if (totalSum < 100) {
            visitedProbability += 100 - totalSum;
            stairProbability = 100;
        }
    }

    // Scales a probability.
    private int ScaleProbability(int p, int s) {
        if (s + p > 100)
            return 100;
        else
            return s + p;
    }

    // Decodes the genome setting the probabilities.
    private void ParseGenome() {
        string currentValue = "";
        int currentChar = 1;

        // I've already skipped the first char, now get the forward probability.
        while (Char.IsNumber(seed[currentChar]) || seed[currentChar] == '.') {
            currentValue += seed[currentChar];
            currentChar++;
        }
        forwardProbability = (int)float.Parse(currentValue) * 100;

        currentValue = "";
        currentChar++;

        // Get the left probability.
        while (Char.IsNumber(seed[currentChar]) || seed[currentChar] == '.') {
            currentValue += seed[currentChar];
            currentChar++;
        }
        leftProbability = (int)float.Parse(currentValue) * 100;

        currentValue = "";
        currentChar++;

        // Get the rigth probability.
        while (Char.IsNumber(seed[currentChar]) || seed[currentChar] == '.') {
            currentValue += seed[currentChar];
            currentChar++;
        }
        rigthProbability = (int)float.Parse(currentValue) * 100;

        currentValue = "";
        currentChar++;

        // Get the visited probability.
        while (Char.IsNumber(seed[currentChar]) || seed[currentChar] == '.') {
            currentValue += seed[currentChar];
            currentChar++;
        }
        visitedProbability = (int)float.Parse(currentValue) * 100;

        currentValue = "";
        currentChar++;

        // Get the stair probability.
        while (Char.IsNumber(seed[currentChar]) || seed[currentChar] == '.') {
            currentValue += seed[currentChar];
            currentChar++;
        }
        stairProbability = (int)float.Parse(currentValue) * 100;
    }

}