namespace Maps.MapGenerator
{
    public class Area
    {
        public readonly int bottomRow;
        public readonly bool isCorridor;
        public readonly int leftColumn;
        public readonly int rightColumn;
        public readonly int topRow;

        public Area(
            int leftColumn,
            int bottomRow,
            int rightColumn,
            int topRow,
            bool isCorridor = false
        )
        {
            this.leftColumn = leftColumn;
            this.bottomRow = bottomRow;
            this.rightColumn = rightColumn;
            this.topRow = topRow;
            this.isCorridor = isCorridor;
        }
    }
}