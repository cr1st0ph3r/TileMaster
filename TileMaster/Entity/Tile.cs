
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Kept as a string for serialization backwards-compatibility (existing data uses names).
        /// </summary>
        public string Color = "Gray";

        /// <summary>
        /// Serialized integer ARGB representation of the runtime color filter.
        /// Storing this allows tiles to be deserialized with the calculated color already present,
        /// avoiding expensive recalculation for off-screen chunks.
        /// Format: (A << 24) | (R << 16) | (G << 8) | B
        /// </summary>
        public int? ColorArgb = null;

        /// <summary>
        /// Runtime color filter using actual RGB(A) values. Not serialized.
        /// When present, this takes precedence over the string-based Color name.
        /// </summary>
        [NonSerialized]
        public Color? ColorFilter = null;

        /// <summary>
        /// Rotation in radians. Default 0. Set to MathHelper.ToRadians(90/180/270) to rotate sprite on draw.
        /// </summary>
        public float Rotation { get; set; } = 0f;

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
                if (Rotation == 0)
                {
                    spriteBatch.Draw(texture, rectangle, getColor());
                }
                else
                {
                    // draw using the position+scale overload so rotation origin is positioned correctly
                    var origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
                    var scale = new Vector2(rectangle.Width / (float)texture.Width, rectangle.Height / (float)texture.Height);
                    var position = new Vector2(rectangle.X + rectangle.Width * 0.5f, rectangle.Y + rectangle.Height * 0.5f);

                    spriteBatch.Draw(texture,
                                     position,        // center position in screen pixels
                                     null,            // source rectangle (whole texture)
                                     getColor(),
                                     Rotation,
                                     origin,          // origin in texture pixels (center)
                                     scale,           // scale to fit the destination rectangle
                                     SpriteEffects.None,
                                     0f);
                }
            }
        }

        private Color getColor()
        {
            // If a runtime RGB(A) color filter is present, use it (preferred for smooth gradients).
            if (ColorFilter.HasValue)
                return ColorFilter.Value;

            // If an ARGB integer was stored with the tile, restore it to ColorFilter and use it.
            if (ColorArgb.HasValue)
            {
                ColorFilter = UnpackArgb(ColorArgb.Value);
                return ColorFilter.Value;
            }

            // Fallback to the existing reflection-based named color lookup so older code/data still works.
            var prop = typeof(Color).GetProperty(Color);
            if (prop != null)
                return (Color)prop.GetValue(null, null);
            return default;
        }

        /// <summary>
        /// Helper to set color via bytes (RGB[A]). Sets runtime ColorFilter and persists the value into ColorArgb.
        /// </summary>
        public void SetColor(byte r, byte g, byte b, byte a = 255)
        {
            ColorFilter = new Color(r, g, b, a);
            ColorArgb = PackArgb(ColorFilter.Value);            
        }

        /// <summary>
        /// Helper to clear runtime color filter and revert to named color.
        /// Does NOT remove the saved ColorArgb; call ClearSavedColor to remove stored value as well.
        /// </summary>
        public void ClearRuntimeColor()
        {
            ColorFilter = null;
        }

        /// <summary>
        /// Remove any saved ARGB so the tile will fully revert to the legacy named color.
        /// </summary>
        public void ClearSavedColor()
        {
            ColorArgb = null;
            ColorFilter = null;
        }

        /// <summary>
        /// Pack a Color into an int (A<<24 | R<<16 | G<<8 | B).
        /// </summary>
        private static int PackArgb(Color c)
        {
            return (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        }

        /// <summary>
        /// Unpack an int ARGB into a Color.
        /// </summary>
        private static Color UnpackArgb(int argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return new Color(r, g, b, a);
        }
    }
    [Serializable]
    public class CollisionTiles : Tile
    {
        //needed by deserialization
        public CollisionTiles()
        {

        }


        //constructor for map generation
        public CollisionTiles(CollisionTiles refTile, int x, int y, int positionOnChunk, int blockId)
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

        public CollisionTiles(CollisionTiles tileType, CollisionTiles tileRef)
        {
            IsOccupied = tileType.IsOccupied;
            IsSolid = tileType.IsSolid;
            GlobalId = tileRef.GlobalId;
            isEdgeTile = tileRef.isEdgeTile;
            neighboringTiles = tileRef.neighboringTiles;
            Name = tileType.Name;
            TileId = tileType.TileId;
            if (tileType.TextureName != tileRef.TextureName)
            {
                texture = tileType.Textures.FirstOrDefault(x => x.Name == tileRef.TextureName);
            }
            else if (tileType.AlternateTextures.Any() && Global.UseAlternateTiles)
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
            ColorArgb = tileRef.ColorArgb;
            ChunkId = tileRef.ChunkId;
            Color = tileType.Color;
            Rectangle = new Rectangle(tileRef.X * Global.TileSize, tileRef.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            X = tileRef.X;
            Height = Global.TileSize;
            Width = Global.TileSize;
            Y = tileRef.Y;     
        }

        public static List<CollisionTiles> LoadTilesTypes()
        {
            var json = System.IO.File.ReadAllText(Global.TileDataLocation);
            var Tiles = JsonConvert.DeserializeObject<List<CollisionTiles>>(json);
            var tilePath = "Tiles";

            //load the texture
            foreach (var tile in Tiles.ToList())
            {
                tile.texture = Content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{tile.TextureName}");
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