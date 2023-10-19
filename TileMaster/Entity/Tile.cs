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
        public string Color = "White";

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
            var prop = typeof(Color).GetProperty(this.Color);
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

       
        public CollisionTiles(CollisionTiles refTile, int x, int y, int positionOnchunk, int blockId)
        {
            this.IsOccupied = refTile.IsOccupied;
            this.IsSolid = refTile.IsSolid;
            this.GlobalId = blockId;
            this.Name = refTile.Name;
            this.TileId = refTile.TileId;
            //this.texture = refTile.texture;
            this.textureId = refTile.textureId;
            this.TextureName = refTile.TextureName;
            this.LocalId = positionOnchunk;
            this.Color = refTile.Color;
            this.Rectangle = new Rectangle(y * Global.TileSize, x * Global.TileSize, Global.TileSize, Global.TileSize);
            this.X = x;
            this.Height = Global.TileSize;
            this.Width = Global.TileSize;
            this.Y = y;
            
        }
     
        public CollisionTiles(CollisionTiles tileType, CollisionTiles tileRef)
        {
            this.IsOccupied = tileType.IsOccupied;
            this.IsSolid = tileType.IsSolid;
            this.GlobalId = tileRef.GlobalId;
            this.isEdgeTile = tileRef.isEdgeTile;
            this.neighboringTiles = tileRef.neighboringTiles;
            this.Name = tileType.Name;
            this.TileId = tileType.TileId;
            if (tileType.AlternateTextures.Any()&&Global.UseAlternateTiles)
            {
                //give random texture                
                this.texture = tileType.AltTextures[Game.rnd.Next(tileType.AltTextures.Count)];
            }
            else
            {
                this.texture = tileType.texture;
            }
            this.textureId = tileType.textureId;
            this.TextureName = tileType.TextureName;
            this.LocalId = tileRef.LocalId;
            this.ChunkId = tileRef.ChunkId;
            this.Color = tileType.Color;
            this.Rectangle = new Rectangle(tileRef.Y * Global.TileSize, tileRef.X * Global.TileSize, Global.TileSize, Global.TileSize);           
            this.X = tileRef.X;
            this.Height = Global.TileSize;
            this.Width = Global.TileSize;
            this.Y = tileRef.Y;
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
        public static Texture2D rectangVectorBuilder(int textureId)
        {
            return Content.Load<Texture2D>("Tile" + textureId);
        }


    }
     
}




