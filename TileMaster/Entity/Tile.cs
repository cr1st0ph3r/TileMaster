
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TileMaster.Entity
{
    [Serializable]
    public abstract class Tile : BaseTile
    {

        public Texture2D Texture { get; set; }

        public List<Texture2D> Textures { get; set; }

        public List<Texture2D> AltTextures { get; set; }
    
        public Rectangle Rectangle { get; set; }

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
     
        public static ContentManager Content { get; set; }
          
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
        }

        public void InitializeTexture()
        {
            Texture = Global.ReferenceTiles[textureId].Texture;
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
        #endregion
    }


}