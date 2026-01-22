using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TileMaster.Entity.Tiles
{
    [Serializable]
    public abstract class Tile : BaseTile
    {

        public Texture2D Texture { get; set; }

        public List<Texture2D> Textures { get; set; }

        public List<Texture2D> AltTextures { get; set; }
    
        public Rectangle Rectangle { get; set; }

        public Item PlacedItem { get; set; }

        public List<string> TileSet { get; set; }
      
        /// <summary>
        /// List of alternative textures. Used to give a better visual look to the landscape
        /// </summary>
        public List<string> AlternateTextures { get; set; }
      
        /// <summary>
        /// Runtime color filter using actual RGB(A) values. Not serialized.
        /// When present, this takes precedence over the string-based Color name.
        /// </summary>
        public Color? ColorFilter { get; set; } = null;
     
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null)
            {
                if (Rotation == 0)
                {
                    spriteBatch.Draw(Texture, Rectangle, getColor());
                }
                else
                {
                    // draw using the position+scale overload so rotation origin is positioned correctly
                    var origin = new Vector2(Texture.Width * 0.5f, Texture.Height * 0.5f);
                    var scale = new Vector2(Rectangle.Width / (float)Texture.Width, Rectangle.Height / (float)Texture.Height);
                    var position = new Vector2(Rectangle.X + Rectangle.Width * 0.5f, Rectangle.Y + Rectangle.Height * 0.5f);

                    spriteBatch.Draw(Texture,
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
            else
            {
                Game.LogMessage($"Tile {GlobalId} of type {Name} has no texture!!!",null);
            }
            
            
            if (PlacedItem != null && PlacedItem.Texture != null)
            {
                 // Draw the item centered on the tile
                 var itemTexture = PlacedItem.Texture;
                 var itemScale = new Vector2(
                     (float)Rectangle.Width / itemTexture.Width * 0.8f, // Scale to 80% of tile size for padding
                     (float)Rectangle.Height / itemTexture.Height * 0.8f
                 );
                 var itemPosition = new Vector2(
                     Rectangle.X + Rectangle.Width / 2,
                     Rectangle.Y + Rectangle.Height / 2
                 );
                 var itemOrigin = new Vector2(itemTexture.Width / 2f, itemTexture.Height / 2f);

                 spriteBatch.Draw(itemTexture, itemPosition, null, Microsoft.Xna.Framework.Color.White, 0f, itemOrigin, itemScale, SpriteEffects.None, 0f);
            }
        }

        public void InitializeTexture()
        {
            if (IsSlope)
            {
                var refTile = Global.ReferenceTiles[TileId];
                if (refTile.Textures != null)
                {
                    var slopeTexture = refTile.Textures.FirstOrDefault(x => x != null && x.Name != null && x.Name.EndsWith("Slope"));
                    if (slopeTexture != null)
                    {
                        Texture = slopeTexture;
                        TextureName = slopeTexture.Name;
                        return;
                    }
                }
            }
            if (TextureId == 0)
            {
                Texture = Global.ReferenceTiles[TileId].Texture;
            }
            else {
                var refTile = Global.ReferenceTiles[TileId];
                //fatal flaw: we dont save which texture we are reffereing to, we have alternative textures, textures etc
                if (refTile.Textures.Any())
                {
                    Texture = refTile.Textures.FirstOrDefault(x => x.Name.EndsWith($"{Name}{TextureId}"));
                }
                else
                {
                    Texture = refTile.AltTextures.FirstOrDefault(x => x.Name.EndsWith($"{Name}{TextureId}"));
                }
           
            }
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

        #region Private Methods
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
        /// Pack a Color into an int (A<<24 | R<<16 | G<<8 | B).
        /// </summary>
        public static int PackArgb(Color c)
        {
            return (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        }

        /// <summary>
        /// Unpack an int ARGB into a Color.
        /// </summary>
        public static Color UnpackArgb(int argb)
        {
            byte a = (byte)((argb >> 24) & 0xFF);
            byte r = (byte)((argb >> 16) & 0xFF);
            byte g = (byte)((argb >> 8) & 0xFF);
            byte b = (byte)(argb & 0xFF);
            return new Color(r, g, b, a);
        }
        #endregion
    }


}