using UnityEngine;

public abstract class MapAssebler : CoreComponent {

    public abstract void AssembleMap(char[,] map, float squareSize, float h);

}