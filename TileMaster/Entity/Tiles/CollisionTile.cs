using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

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

        public static List<ReferenceTile> LoadTilesTypes(ContentManager content)
        {
            var json = System.IO.File.ReadAllText(Global.TileDataLocation);
            var Tiles = JsonConvert.DeserializeObject<List<ReferenceTile>>(json);
            var tilePath = "Tiles";

            //load the texture
            foreach (var tile in Tiles.ToList())
            {
                tile.Texture = content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{tile.TextureName}");
                tile.Textures = new List<Texture2D>();
                tile.AltTextures = new List<Texture2D>();
                foreach (var subTiles in tile.TileSet)
                {
                    tile.Textures.Add(content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{subTiles}"));
                }
                foreach (var alt in tile.AlternateTextures)
                {
                    tile.AltTextures.Add(content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{alt}"));
                }
            }
            return Tiles;
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
            bgTile.textureId = textureId;
            bgTile.TextureName = TextureName;
            bgTile.Rectangle = new Rectangle(X * Global.TileSize, Y * Global.TileSize, Global.TileSize, Global.TileSize);
            bgTile.Color = Color;
            return bgTile;
        }
    }
}
