using System.Collections.Generic;

/// <summary>
/// MapAssebler is an abstract class used to implement any kind of map assembler weapon. A map
/// assembler is used to generate a physical representation of a map starting from its matrix form.
/// </summary>
public abstract class MapAssebler : CoreComponent {

    public abstract void AssembleMap(char[,] map, char wallChar, char roomChar, float squareSize,
        float h);

    public abstract void AssembleMap(List<char[,]> maps, char wallChar, char roomChar,
        char voidChar, float squareSize, float h);

}