using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;
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
                    //we shall not store the texture on the reference tiles
                    //tile.Texture = content.Load<Texture2D>($"{tilePath}/{tile.TextureName}/{tile.TextureName}");

                }
                catch (ContentLoadException exc)
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

                        // Sync hardness from item to reference tile if provided in Items.json
                        if (item.Hardness != 100)
                        {
                            Global.ReferenceTiles[item.TileId].Hardness = item.Hardness;
                        }
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

        /// <summary>
        /// Loads a list of reference mob definitions from the data source.
        /// </summary>
        /// <param name="content">The content manager to use for loading additional resources, or null if no content loading is required.</param>
        /// <returns>A list of reference mobs loaded from the data source. The list may be empty if no mobs are defined.</returns>
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

        /// <summary>
        /// Loads tile map data from the file specified by the global tile map data location and constructs a dictionary
        /// mapping tile names to their corresponding rectangle data.
        /// </summary>
        /// <remarks>The method reads and deserializes tile map data from the file path specified by <see
        /// cref="Global.TileMapDataLocation"/>. Each tile entry is mapped by its name. If a tile has alternative
        /// rectangles defined, they are included in the <see cref="RectangleData.AlternativeRectangles"/> property. The
        /// method will throw an exception if the file cannot be read or the data is invalid.</remarks>
        /// <returns>A dictionary where each key is a tile name and each value is a <see cref="RectangleData"/> representing the
        /// tile's rectangle and any alternative rectangles. The dictionary will be empty if no tile data is found.</returns>
        public static Dictionary<string, RectangleData> LoadTileMap()
        {
            var tileMap = new Dictionary<string, RectangleData>();
            var json = System.IO.File.ReadAllText(Global.TileMapDataLocation);
            var tilemap = JsonConvert.DeserializeObject<List<TileMap>>(json);

            foreach (var tile in tilemap)
            {
                var rectData = new RectangleData() { Rectangle = new Rectangle(tile.X * Global.TileSize, tile.Y * Global.TileSize, Global.TileSize, Global.TileSize) };
                if (tile.Alt is not null && tile.Alt.Length > 0)
                {
                    rectData.AlternativeRectangles = new List<Rectangle>();
                    foreach (var item in tile.Alt)
                    {
                        var altTile = tilemap.FirstOrDefault(x=>x.Id==item);

                        rectData.AlternativeRectangles.Add(new Rectangle(altTile.X * Global.TileSize, altTile.Y * Global.TileSize, Global.TileSize, Global.TileSize));
                    }
                }
                tileMap.Add(tile.Name,rectData);
            }

            return tileMap;
        }
    }
}
