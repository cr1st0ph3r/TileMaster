using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TileMaster.Entity;
using TileMaster.Entity.MobMovement;
using TileMaster.Entity.Tiles;
using TileMaster.Model;

namespace TileMaster.Data
{
    public static class DataLoader
    {
        /// <summary>
        /// Loads tile types from a JSON file and their associated textures.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static List<ReferenceTile> LoadTilesTypes(ContentManager content)
        {
            var json = System.IO.File.ReadAllText(Global.TileDataLocation);
            var Tiles = JsonConvert.DeserializeObject<List<ReferenceTile>>(json);

            if (content == null)
            {
                return Tiles;
            }

            var tilePath = "Tiles";

            //load the texture
            foreach (var tile in Tiles.ToList())
            {
                try
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
                catch (ContentLoadException)
                {
                    // If content loading fails (e.g. in a test environment), we just skip textures
                    System.Diagnostics.Debug.WriteLine($"Failed to load textures for tile: {tile.Name}");
                }
            }
            return Tiles;
        }

        /// <summary>
        /// Loads item definitions from a JSON file and their associated textures.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
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


        public static List<ReferenceMob> LoadMobs(ContentManager content)
        {
            var json = System.IO.File.ReadAllText(Global.MobsDataLocation);
            var mobs = JsonConvert.DeserializeObject<List<ReferenceMob>>(json);

            if (content == null)
            {
                return mobs;
            }
             
            return mobs;
        }
    }
}
