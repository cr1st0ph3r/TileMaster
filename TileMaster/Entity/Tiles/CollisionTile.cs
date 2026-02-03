using Microsoft.Xna.Framework;

namespace TileMaster.Entity.Tiles
{
    public class CollisionTile : Tile
    {
        //needed by deserialization
        public CollisionTile()
        {
            Rectangle = new Rectangle(X * Global.TileSize, Y * Global.TileSize, Global.TileSize, Global.TileSize);
        }


        //constructor for map generation
        public CollisionTile(Tile refTile, int x, int y, int positionOnChunk, int blockId)
        {
            IsOccupied = refTile.IsOccupied;
            ColorArgb = refTile.ColorArgb;
            IsSolid = refTile.IsSolid;
            GlobalId = blockId;
            Name = refTile.Name;
            TileId = refTile.TileId;
            TextureId = refTile.TextureId;
            TextureName = refTile.TextureName;
            LocalId = positionOnChunk;
            Color = refTile.Color;
            Rectangle = new Rectangle(x * Global.TileSize, y * Global.TileSize, Global.TileSize, Global.TileSize);
            X = x;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = y;
        }

        public BackgroundTile ToBackgroundTile()
        {
            var bgTile = new BackgroundTile();
            bgTile.X = X;
            bgTile.Y = Y;
            bgTile.GlobalId = GlobalId;
            bgTile.IsOccupied = IsOccupied;
            bgTile.ColorArgb = ColorArgb;
            bgTile.IsSolid = IsSolid;
            bgTile.Name = Name;
            bgTile.TileId = TileId;
            bgTile.TextureId = TextureId;
            bgTile.TextureName = TextureName;
            bgTile.Rectangle = new Rectangle(X * Global.TileSize, Y * Global.TileSize, Global.TileSize, Global.TileSize);
            bgTile.Color = Color;
            return bgTile;
        }
    }
}
