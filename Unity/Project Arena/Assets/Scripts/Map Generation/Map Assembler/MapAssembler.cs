public abstract class MapAssebler : CoreComponent {

    public abstract void AssembleMap(char[,] map, char wallChar, char roomChar, float squareSize, float h);

    public abstract void AssembleMap(char[,] map, char wallChar, char roomChar, char voidChar, float squareSize, float h, bool generateMeshes);

}