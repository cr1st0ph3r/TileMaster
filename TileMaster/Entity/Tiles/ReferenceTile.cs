namespace TileMaster.Entity.Tiles
{
    public class ReferenceTile : Tile
    {
        public string Atlas { get; set; }
        public System.Collections.Generic.Dictionary<string, Microsoft.Xna.Framework.Rectangle> AtlasMap { get; set; } = new System.Collections.Generic.Dictionary<string, Microsoft.Xna.Framework.Rectangle>();
    }
}
