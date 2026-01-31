using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace TileMaster.Entity.Tiles
{
    [Serializable]
    public abstract class Tile : BaseTile
    {

        public Texture2D Texture { get; set; }
        public Texture2D AtlasTexture { get; set; }

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

        private static Dictionary<string, Color> _colorTable = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
     
        public void Draw(SpriteBatch spriteBatch)
        {
            var drawTexture = (AtlasTexture != null && SourceRectangle != null) ? AtlasTexture : GetTexture();

            if (drawTexture != null)
            {
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
            }
            else
            {
                Game.LogMessage($"Tile {GlobalId} of type {Name} has no texture!!!",null);
            }
            
            
            if (PlacedItem != null && PlacedItem.Texture != null)
            {
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
            var refTile = Global.ReferenceTiles[TileId];
            AtlasTexture = refTile.AtlasTexture;

            if (IsSlope)
            {
                if (refTile.Textures != null)
                {
                    var slopeTexture = refTile.Textures.FirstOrDefault(x => x != null && x.Name != null && x.Name.EndsWith("Slope"));
                    if (slopeTexture != null)
                    {
                        Texture = slopeTexture;
                        TextureName = slopeTexture.Name;
                        if (AtlasTexture != null && refTile.AtlasMap.TryGetValue(TextureName, out var rect))
                        {
                            SourceRectangle = rect;
                        }
                        return;
                    }
                }
            }
            if(AtlasTexture is not null)
            {
                TextureName = TextureName;
                if (AtlasTexture != null && refTile.AtlasMap != null && refTile.AtlasMap.TryGetValue(TextureName, out var rect))
                {
                    SourceRectangle = rect;
                }
            }
            else
            {
                if (TextureId == 0)
                {
                    if (Global.UseAlternateTiles && refTile.AltTextures.Any())
                    {
                        Texture = refTile.AltTextures[Game.rnd.Next(refTile.AltTextures.Count)];
                    }
                    else
                    {
                        Texture = refTile.GetTexture();
                    }
                }
                else
                {
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
        private Texture2D GetTexture()
        {
            return Texture;
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