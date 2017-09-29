using System.Collections;
using UnityEngine;

public interface MapBuilderFromText {

	void BuildMap(char[,] map, char charWall, float squareSize, float h);

}