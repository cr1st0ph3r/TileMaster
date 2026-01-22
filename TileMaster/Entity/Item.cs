using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity.Enums;

namespace TileMaster.Entity
{
    [Serializable]
    public class Item
    {
        public int Id { get; set; }
        public int TileId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string TextureName { get; set; }
        public string UIIcon { get; set; }
        public string LightColorName { get; set; }
        public int StackSize { get; set; } = 1000;
        public bool IsTile { get; set; }
        /// <summary>
        /// Whether the item is a tool (e.g., pickaxe, axe).
        /// </summary>
        public bool IsTool { get; set; }
        /// <summary>
        /// The action performed by the tool (if IsTool is true).
        /// </summary>
        public ToolAction ToolAction { get; set; }
        /// <summary>
        /// The time (in milliseconds) it takes to use the item (tools).
        /// </summary>
        public int UseTime { get; set; }

        [NonSerialized]
        public Texture2D Texture;

        public bool IsPlaceable { get; set; }
        public bool PlaceableOnBackground { get; set; }
        
        // Lighting properties
        public bool IsLightSource { get; set; }
        public bool IsFlickeringLight { get; set; }
        public Color? LightColor { get; set; } = Color.White;
        public float LightIntensity { get; set; } = 0f;
        public float LightRadius { get; set; } = 0f; // Could be used for gradient logic later
        
        public Item()
        {
        }

        public static List<Item> LoadItems(ContentManager content)
        {
            var json = System.IO.File.ReadAllText(Global.ItemsDataLocation);
            var items = JsonConvert.DeserializeObject<List<Item>>(json);
            
            if (content == null)
            {
                return items;
            }

            var tilePath = "Items";

            //load the texture
            foreach (var item in items.ToList())
            {
                try
                {
                    if (item.IsTile)
                    {
                        item.Texture = Global.ReferenceTiles[item.TileId].Texture;
                    }
                    else
                    {
                        item.Texture = content.Load<Texture2D>($"{tilePath}/{item.Name}/{item.TextureName}");
                    }
                }
                catch (Exception)
                {
                    // Skip texture loading if it fails (e.g. in test environment)
                }

                if (!string.IsNullOrEmpty(item.LightColorName))
                {
                    var prop = typeof(Color).GetProperty(item.LightColorName);
                    if (prop != null)
                    {
                        item.LightColor = (Color)prop.GetValue(null, null);
                    }
                }
            }
            return items;
        }
    }
}
