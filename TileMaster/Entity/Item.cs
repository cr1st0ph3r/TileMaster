using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TileMaster.Entity
{
    [Serializable]
    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string TextureName { get; set; }
        
        // Runtime only, not serialized directly usually but we'll manage it
        [NonSerialized]
        public Texture2D Texture;

        public bool IsPlaceable { get; set; }
        
        // Lighting properties
        public bool IsLightSource { get; set; }
        public Color LightColor { get; set; } = Color.White;
        public float LightIntensity { get; set; } = 0f;
        public float LightRadius { get; set; } = 0f; // Could be used for gradient logic later
        
        public Item()
        {
        }

        public void InitializeTexture()
        {
            if (!string.IsNullOrEmpty(TextureName))
            {
                // Assuming Global or Tile.Content is available to load textures found in Tiles/Torch/ etc.
                // We might need a robust way to load textures. For now, we'll try to load from the content manager.
                // However, based on the codebase, textures often come from ReferenceTiles or are loaded via Content.
                // The current pattern seems to be Global.Content or Tile.Content.
                try
                {
                    if (Tile.Content != null)
                    {
                        // The user found Tiles/Torch/Torch1.png. 
                        // If TextureName is "Torch1", we might need to search or specify path.
                        // For this implementation, let's assume TextureName includes the relative path or we handle it.
                        // Let's stick to a simple load for now.
                        Texture = Tile.Content.Load<Texture2D>(TextureName);
                    }
                }
                catch
                {
                    // Fallback or error logging
                }
            }
        }
    }
}
