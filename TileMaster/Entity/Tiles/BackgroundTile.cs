using System;
using Microsoft.Xna.Framework;

namespace TileMaster.Entity.Tiles
{
    [Serializable]
    public class BackgroundTile : Tile
    {
        public BackgroundTile()
        {
            Rectangle = new Rectangle(X * Global.TileSize, Y * Global.TileSize, Global.TileSize, Global.TileSize);
        }

        public BackgroundTile(CollisionTile tileType, CollisionTile tileRef)
        {
            IsOccupied = false; // Background tiles are generally not occupied in the collision sense
            IsSolid = false;    // Background tiles are never solid
            GlobalId = tileRef.GlobalId;
            IsEdgeTile = tileRef.IsEdgeTile;
            // neighboringTiles = tileRef.neighboringTiles; // Might not need this for background
            Name = tileType.Name;
            TileId = tileType.TileId;
            // Texture Logic
            //random???
            Texture = tileType.Texture;
            TextureId = tileType.TextureId;
            TextureName = tileRef.TextureName;
            LocalId = tileRef.LocalId;
            ColorArgb = tileRef.ColorArgb;
            ChunkId = tileRef.ChunkId;

            // Default to a darker color for background tiles if no color is specified
            Color = "Gray";

            Rectangle = new Rectangle(tileRef.X * Global.TileSize, tileRef.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            X = tileRef.X;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = tileRef.Y;
        }
    }
}
