using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace TileMaster.Entity
{
    enum TileType
    {
        Air = 0,
        Dirt = 1,
        Stone = 2,
        DirtWithGrass = 3,
        Granite = 4,
        TreeTrunk = 5,
        TreeLeaf = 6
    }

    [Serializable]
    public abstract class Tile
    {
        [NonSerialized]
        public Texture2D texture;
        [NonSerialized]
        public List<Texture2D> Textures;
        [NonSerialized]
        public List<Texture2D> AltTextures;
        [NonSerialized]
        private Rectangle rectangle;

        public int X, Y, Height, Width, textureId;
        /// <summary>
        /// the global identifier of the tile (zero based)
        /// </summary>
        public int GlobalId;
        /// <summary>
        /// The chunk which the block belongs to (one based)
        /// </summary>
        public int ChunkId;
        /// <summary>
        /// The tile Texture's Id
        /// </summary>
        public int TileId;
        /// <summary>
        /// The id of the tile respective to its chunk (local Id) (zero based)
        /// </summary>
        public int LocalId;
        /// <summary>
        /// Defines the parent tile in case of multiple tiles of the same kind
        /// </summary>
        public int ParentTileId;

        public List<string> TileSet;
        /// <summary>
        /// List of alternative textures. Used to give a better visual look to the landscape
        /// </summary>
        public List<string> AlternateTextures;
        /// <summary>
        /// Denotes if the space is occupied
        /// </summary>
        public bool IsOccupied;
        /// <summary>
        /// Denotes if the block is solid
        /// </summary>
        public bool IsSolid;
        /// <summary>
        /// Denotes if this tile is on the edge of a chunk
        /// </summary>
        public bool isEdgeTile;
        /// <summary>
        /// Denotes if this tile is neighboring a different tile type. Used for blend logic
        /// </summary>
        public bool isNeighboringDifferentTile;
        /// <summary>
        /// The name of the tile´s texture
        /// </summary>
        public string Name;
        /// <summary>
        /// The texture´s file name
        /// </summary>
        public string TextureName;
        /// <summary>
        /// The color filter which MonoGame uses to paint the tile upon drawing. White for no filter.
        /// </summary>
        public string Color = "Gray";

        public List<KeyValuePair<int, int>> neighboringTiles;

        public bool isOpenToAir;

        public Rectangle Rectangle
        {
            get { return rectangle; }
            set { rectangle = value; }
        }

        private static ContentManager content;
        public static ContentManager Content
        {
            get { return content; }
            set { content = value; }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (texture != null)
            {
                spriteBatch.Draw(texture, rectangle, getColor());
            }
        }

        private Color getColor()
        {
            var prop = typeof(Color).GetProperty(Color);
            if (prop != null)
                return (Color)prop.GetValue(null, null);
            return default(Color);
        }
    }
    [Serializable]
    public class CollisionTiles : Tile
    {
        public CollisionTiles()
        {

        }


        //TODO whys is this needed?
        public CollisionTiles(CollisionTiles refTile, int x, int y, int positionOnChunk, int blockId)
        {
            IsOccupied = refTile.IsOccupied;
            IsSolid = refTile.IsSolid;
            GlobalId = blockId;
            Name = refTile.Name;
            TileId = refTile.TileId;
            //this.texture = refTile.texture;
            textureId = refTile.textureId;
            TextureName = refTile.TextureName;
            LocalId = positionOnChunk;
            Color = refTile.Color;
            Rectangle = new Rectangle(y * Global.TileSize, x * Global.TileSize, Global.TileSize, Global.TileSize);
            X = x;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = y;

        }

        public CollisionTiles(CollisionTiles tileType, CollisionTiles tileRef)
        {
            IsOccupied = tileType.IsOccupied;
            IsSolid = tileType.IsSolid;
            GlobalId = tileRef.GlobalId;
            isEdgeTile = tileRef.isEdgeTile;
            neighboringTiles = tileRef.neighboringTiles;
            Name = tileType.Name;
            TileId = tileType.TileId;
            if (tileType.AlternateTextures.Any() && Global.UseAlternateTiles)
            {
                //give random texture                
                texture = tileType.AltTextures[Game.rnd.Next(tileType.AltTextures.Count)];
            }
            else
            {
                if (tileType.TextureName != tileRef.TextureName)
                {
                    texture = tileType.Textures.FirstOrDefault(x => x.Name == tileRef.TextureName);
                }
                else
                {
                    texture = tileType.texture;
                }
            }
            textureId = tileType.textureId;
            TextureName = tileRef.TextureName;
            LocalId = tileRef.LocalId;
            ChunkId = tileRef.ChunkId;
            Color = tileType.Color;
            Rectangle = new Rectangle(tileRef.Y * Global.TileSize, tileRef.X * Global.TileSize, Global.TileSize, Global.TileSize);
            X = tileRef.X;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = tileRef.Y;
        }

        public static List<CollisionTiles> LoadTilesTypes()
        {
            var json = System.IO.File.ReadAllText(Global.TileDataLocation);
            var Tiles = JsonConvert.DeserializeObject<List<CollisionTiles>>(json);

            //load the texture
            foreach (var tile in Tiles.ToList())
            {
                tile.texture = Content.Load<Texture2D>(tile.TextureName);
                tile.Textures = new List<Texture2D>();
                tile.AltTextures = new List<Texture2D>();
                foreach (var subTiles in tile.TileSet)
                {
                    tile.Textures.Add(Content.Load<Texture2D>(subTiles));
                }
                foreach (var alt in tile.AlternateTextures)
                {
                    tile.AltTextures.Add(Content.Load<Texture2D>(alt));
                }
            }
            return Tiles;
        }

        public static Rectangle rectangleBuilder(int X, int Y, int Height, int Width)
        {
            return new Rectangle(X, Y, Width, Height);
        }
        public static Texture2D rectangleVectorBuilder(int textureId)
        {
            return Content.Load<Texture2D>("Tile" + textureId);
        }


    }

}




