namespace Maps
{
    public class ABTile
    {
        public char value;
        public int x;
        public int y;

        public ABTile()
        {
        }

        public ABTile(int x, int y, char value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }
    }

    // Stores all information about an All Black room.
    public class ABRoom
    {
        public int dimension;
        public int originX;
        public int originY;

        public ABRoom()
        {
        }

        public ABRoom(int x, int y, int d)
        {
            originX = x;
            originY = y;
            dimension = d;
        }
    }
}