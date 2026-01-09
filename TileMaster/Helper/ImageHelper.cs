using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using TileMaster.Entity;
using TileMaster.Entity.Enums;

namespace TileMaster.Helper
{
    public static class ImageHelper
    {
        /// <summary>
        /// Save the current MapDictionary into a PNG image file.
        /// Each tile maps to a single pixel (x = column, y = row). The routine will:
        /// - Prefer a stored ColorArgb on the tile (if present).
        /// - Otherwise fall back to a quick TileType -> color mapping.
        /// </summary>
        /// <param name="fileName">Output file path (png recommended).</param>
        public static void SaveMapDictionaryAsImage(Dictionary<int, CollisionTile> MapDictionary, string fileName)
        {
            try
            {
                if (MapDictionary == null)
                    throw new InvalidOperationException("MapDictionary is null.");

                int width = Global.MapWidth;
                int height = Global.MapHeight;

                using (var bitmap = new Bitmap(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var globalId = y * width + x;
                            Color pixelColor = Color.White;

                            if (MapDictionary.TryGetValue(globalId, out var tile) && tile != null)
                            {
                                // Fallback mapping by TileId
                                switch ((TileType)tile.TileId)
                                {
                                    case TileType.Air:
                                        pixelColor = Color.White;
                                        break;
                                    case TileType.Dirt:
                                        pixelColor = Color.Brown;
                                        break;
                                    case TileType.Stone:
                                        pixelColor = Color.Gray;
                                        break;
                                    case TileType.DirtWithGrass:
                                        pixelColor = Color.Green;
                                        break;
                                    case TileType.Granite:
                                        pixelColor = Color.DarkRed;
                                        break;
                                    case TileType.TreeTrunk:
                                        pixelColor = Color.SaddleBrown;
                                        break;
                                    case TileType.TreeLeaf:
                                        pixelColor = Color.FromArgb(255, 34, 139, 34); // forest green
                                        break;
                                    default:
                                        pixelColor = Color.LightGray;
                                        break;
                                }

                            }

                            bitmap.SetPixel(x, y, pixelColor);
                        }
                    }

                    // Ensure directory exists
                    var dir = Path.GetDirectoryName(fileName);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    bitmap.Save(fileName, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                // Swallowing exceptions is not ideal, but keeps the editor/game stable.
                // If needed, replace with a logging call (Game.LogMessage) or rethrow.
                try
                {
                    Game.LogMessage($"SaveMapDictionaryAsImage failed: {ex.Message}", Microsoft.Xna.Framework.Color.Red);
                }
                catch { }
            }
        }

        /// <summary>
        /// Save the provided chunk dictionary into a PNG image file.
        /// Each tile maps to a single pixel (x = column, y = row).
        /// Preference: use tile entries found in chunks; fall back to a simple TileType -> color mapping.
        /// </summary>
        /// <param name="chunkDict">Chunk dictionary (key = chunkId, value = Chunk)</param>
        /// <param name="fileName">Output file path (png recommended)</param>
        public static void SaveChunkDictionaryAsImage(Dictionary<int, Chunk> chunkDict, string fileName)
        {
            try
            {
                if (chunkDict == null)
                    throw new InvalidOperationException("chunkDict is null.");

                int width = Global.MapWidth;
                int height = Global.MapHeight;

                // Flatten chunk tiles into a quick lookup of globalId -> CollisionTiles
                var mapLookup = new Dictionary<int, CollisionTile>(width * height);
                foreach (var chunk in chunkDict.Values)
                {
                    foreach (var kv in chunk.Tiles)
                    {
                        // kv.Key is expected to be the globalId (consistent with other code)
                        mapLookup[kv.Key] = kv.Value;
                    }
                }

                using (var bitmap = new System.Drawing.Bitmap(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var globalId = y * width + x;
                            System.Drawing.Color pixelColor = System.Drawing.Color.White;

                            if (mapLookup.TryGetValue(globalId, out var tile) && tile != null)
                            {
                                // Fallback mapping by TileId (mirrors MapManager.SaveMapDictionaryAsImage)
                                switch ((TileType)tile.TileId)
                                {
                                    case TileType.Air:
                                        pixelColor = System.Drawing.Color.White;
                                        break;
                                    case TileType.Dirt:
                                        pixelColor = System.Drawing.Color.Brown;
                                        break;
                                    case TileType.Stone:
                                        pixelColor = System.Drawing.Color.Gray;
                                        break;
                                    case TileType.DirtWithGrass:
                                        pixelColor = System.Drawing.Color.Green;
                                        break;
                                    case TileType.Granite:
                                        pixelColor = System.Drawing.Color.DarkRed;
                                        break;
                                    case TileType.TreeTrunk:
                                        pixelColor = System.Drawing.Color.SaddleBrown;
                                        break;
                                    case TileType.TreeLeaf:
                                        pixelColor = System.Drawing.Color.LightGreen;
                                        break;
                                    default:
                                        pixelColor = System.Drawing.Color.LightGray;
                                        break;
                                }
                            }

                            bitmap.SetPixel(x, y, pixelColor);
                        }
                    }

                    // Ensure directory exists
                    var dir = Path.GetDirectoryName(fileName);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    bitmap.Save(fileName, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Game.LogMessage($"SaveChunkDictionaryAsImage failed: {ex.Message}", Microsoft.Xna.Framework.Color.Red);
                }
                catch { }
            }
        }

        /// <summary>
        /// Save the provided matrix into a PNG image file.
        /// Each tile maps to a single pixel (x = column, y = row).
        /// Preference: use tile entries found in chunks; fall back to a simple TileType -> color mapping.
        /// </summary>
        /// <param name="matrix">Matrix to save</param>
        /// <param name="fileName">Output file path (png recommended)</param>
        public static void SaveMatrixAsImage(int[,] matrix, string fileName)
        {
            int width = matrix.GetLength(0);
            int height = matrix.GetLength(1);

            // Create a new bitmap with the same dimensions as the matrix
            using (System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height))
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        System.Drawing.Color pixelColor = System.Drawing.Color.White;

                        if (matrix[x, y] == (int)TileType.Dirt)
                        {
                            pixelColor = System.Drawing.Color.Brown;
                        }
                        else if (matrix[x, y] == (int)TileType.Stone)
                        {
                            pixelColor = System.Drawing.Color.Gray;
                        }
                        else if (matrix[x, y] == (int)TileType.DirtWithGrass)
                        {
                            pixelColor = System.Drawing.Color.Green;
                        }
                        else if (matrix[x, y] == (int)TileType.Granite)
                        {
                            pixelColor = System.Drawing.Color.DarkRed;
                        }


                        bitmap.SetPixel(x, y, pixelColor);
                    }
                }

                // Save the result as a PNG
                bitmap.Save(fileName, ImageFormat.Png);
            }
        }
    }
}
