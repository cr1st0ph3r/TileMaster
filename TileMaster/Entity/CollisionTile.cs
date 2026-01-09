using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace TileMaster.Entity
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
            textureId = refTile.textureId;
            TextureName = refTile.TextureName;
            LocalId = positionOnChunk;
            Color = refTile.Color;
            Rectangle = new Rectangle(x * Global.TileSize, y * Global.TileSize, Global.TileSize, Global.TileSize);
            X = x;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = y;
        }

        public static List<ReferenceTile> LoadTilesTypes()
        {
            var json = System.IO.File.ReadAllText(Global.TileDataLocation);
            var Tiles = JsonConvert.DeserializeObject<List<ReferenceTile>>(json);
            var tilePath = "Tiles";

            //load the texture
            foreach (var tile in Tiles.ToList())
            {
                tile.Texture = Content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{tile.TextureName}");
                tile.Textures = new List<Texture2D>();
                tile.AltTextures = new List<Texture2D>();
                foreach (var subTiles in tile.TileSet)
                {
                    tile.Textures.Add(Content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{subTiles}"));
                }
                foreach (var alt in tile.AlternateTextures)
                {
                    tile.AltTextures.Add(Content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{alt}"));
                }
            }
            return Tiles;
        }
    }
}
