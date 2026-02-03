namespace TileMaster.Data
{
    public class TileMap
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        public int[] Alt { get; set; }
    }
}
