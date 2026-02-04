using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace TileMaster.Entity.Tiles
{
    [Serializable]
    public abstract class Tile : BaseTile
    {

        public Texture2D Texture { get; set; }
        public Rectangle Rectangle { get; set; }
        public Item PlacedItem { get; set; }

        /// <summary>
        /// Runtime color filter using actual RGB(A) values. Not serialized.
        /// When present, this takes precedence over the string-based Color name.
        /// </summary>
        public Color? ColorFilter { get; set; } = null;

        private static Dictionary<string, Color> _colorTable = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        public void Draw(SpriteBatch spriteBatch)
        {
            if (GlobalId == 8621)
            {

            }
            var drawTexture = Global.Atlas;
            if (Rotation == 0)
            {
                spriteBatch.Draw(drawTexture, Rectangle, SourceRectangle, getColor());
            }
            else
            {
                // draw using the position+scale overload so rotation origin is positioned correctly
                var origin = SourceRectangle.HasValue
                    ? new Vector2(SourceRectangle.Value.Width * 0.5f, SourceRectangle.Value.Height * 0.5f)
                    : new Vector2(drawTexture.Width * 0.5f, drawTexture.Height * 0.5f);

                var texWidth = SourceRectangle.HasValue ? SourceRectangle.Value.Width : drawTexture.Width;
                var texHeight = SourceRectangle.HasValue ? SourceRectangle.Value.Height : drawTexture.Height;

                var scale = new Vector2(Rectangle.Width / (float)texWidth, Rectangle.Height / (float)texHeight);
                var position = new Vector2(Rectangle.X + Rectangle.Width * 0.5f, Rectangle.Y + Rectangle.Height * 0.5f);

                spriteBatch.Draw(drawTexture,
                                 position,        // center position in screen pixels
                                 SourceRectangle, // source rectangle (part of atlas or whole texture)
                                 getColor(),
                                 Rotation,
                                 origin,          // origin in texture pixels (center)
                                 scale,           // scale to fit the destination rectangle
                                 SpriteEffects.None,
                                 0f);
            }

            if (PlacedItem != null)
            {
                if (PlacedItem.Texture is null)
                {
                    Game.LogMessage($"Placed item {PlacedItem.Name} has no texture!!!", null);
                    return;
                }
                // Draw the item centered on its designated tile area
                var itemTexture = PlacedItem.Texture;

                // Calculate total area size in pixels
                float targetWidth = PlacedItem.Width * Global.TileSize;
                float targetHeight = PlacedItem.Height * Global.TileSize;

                // Single-tile items often have some padding (80% of tile size), 
                // but large objects like anvils might need to fill the space more.
                float padding = (PlacedItem.Width == 1 && PlacedItem.Height == 1) ? 0.8f : 1.0f;

                var itemScale = new Vector2(
                    (float)targetWidth / itemTexture.Width * padding,
                    (float)targetHeight / itemTexture.Height * padding
                );

                // Position is the center of the NxM area
                var itemPosition = new Vector2(
                    Rectangle.X + targetWidth / 2f,
                    Rectangle.Y + targetHeight / 2f
                );

                var itemOrigin = new Vector2(itemTexture.Width / 2f, itemTexture.Height / 2f);

                spriteBatch.Draw(itemTexture, itemPosition, null, Microsoft.Xna.Framework.Color.White, 0f, itemOrigin, itemScale, SpriteEffects.None, 0f);
            }
        }

        public void InitializeTexture()
        {
            var refTile = Global.ReferenceTiles[(int)TileId];

            if (IsSlope)
            {            
                SourceRectangle = Global.AtlasMap.FirstOrDefault(x=>x.Value.TextureId==TextureId).Value.Rectangle;
                return;
            }
            if (TextureName == "Air")
            {
                return;
            }
            //temporary fix
            //add number to txturename that does not end with number
            if (!char.IsDigit(TextureName[TextureName.Length - 1]))
            {
                TextureName += "1";
            }

            var texture =  Global.AtlasMap[TextureName];
            if (texture.HaveAlternativeData)
            {
                SourceRectangle = texture.AlternativeRectangles[Game.rnd.Next(0, texture.AlternativeRectangles.Count)];
            }
            else
            {
                SourceRectangle = Global.AtlasMap[TextureName].Rectangle;
            }                  
        }
        
        #region Private Methods
        /// <summary>
        /// Helper to set color via bytes (RGB[A]). Sets runtime ColorFilter and persists the value into ColorArgb.
        /// </summary>
        public void SetColor(byte r, byte g, byte b, byte a = 255)
        {
            ColorFilter = new Color(r, g, b, a);
            ColorArgb = PackArgb(ColorFilter.Value);
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

            if (string.IsNullOrEmpty(Color))
                return Microsoft.Xna.Framework.Color.White;

            if (_colorTable.TryGetValue(Color, out var cachedColor))
                return cachedColor;

            // Fallback to the existing reflection-based named color lookup so older code/data still works.
            var prop = typeof(Color).GetProperty(Color);
            if (prop != null)
            {
                var colorValue = (Color)prop.GetValue(null, null);
                _colorTable[Color] = colorValue;
                return colorValue;
            }
            return Microsoft.Xna.Framework.Color.White;
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