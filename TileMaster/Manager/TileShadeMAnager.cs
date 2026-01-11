using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileMaster.Manager
{
    public class TileShadeManager
    {
        private Map.Map map;

        public TileShadeManager(Map.Map map)
        {
            this.map = map;
        }
        // Global light map: [x, y] -> light level (0.0 to 1.0)
        private float[,] lightMap;

        public void UpdateLighting(Point? center = null, int chunkRadius = 4)
        {
            int width = Global.MapWidth;
            int height = Global.MapHeight;

            // Initialize light map if needed or resize
            if (lightMap == null || lightMap.GetLength(0) != width || lightMap.GetLength(1) != height)
            {
                lightMap = new float[width, height];
            }

            // Determine bounds
            int startX = 0;
            int endX = width;

            if (center.HasValue)
            {
                // Calculate bounds based on chunk radius
                // each chunk is Global.ChunkSize wide
                int radiusInTiles = chunkRadius * Global.ChunkSize;
                startX = Math.Max(0, center.Value.X - radiusInTiles);
                endX = Math.Min(width, center.Value.X + radiusInTiles);
            }
            else
            {
                // If no center is provided, we clear everything.
                // If a center IS provided, we generally assume the existing lightMap has valid data elsewhere,
                // so we only clear/update the affected strip.
                // However, 'Array.Clear' is fast, but if we want to preserve other areas, we must iterate.
                Array.Clear(lightMap, 0, lightMap.Length);
            }

            // Correction: If we are doing a partial update, we should only clear the region we are updating.
            // But 'Array.Clear' clears the whole thing.
            // If we want to support 'partial updates' while keeping the rest of the world lit, 
            // we should NOT clear the entire map if a center is provided.
            if (center.HasValue)
            {
                // Clear only the affected vertical strip
                for (int x = startX; x < endX; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        lightMap[x, y] = 0f;
                    }
                }
            }

            // Queue for light propagation
            var lightQueue = new Queue<Point>();

            // 1. Sunlight Pass (Vertical Rays)
            // Iterate over restricted column range
            for (int x = startX; x < endX; x++)
            {
                // Sunlight comes from top (y=0) down
                for (int y = 0; y < height; y++)
                {
                    var tile = map.GetTileAt(x, y);

                    // If tile is solid, sunlight stops here (but the tile itself gets lit if it's the first hit)
                    if (tile != null && tile.IsSolid)
                    {
                        // Solid blocks exposed to sun get full light
                        lightMap[x, y] = 1.0f;
                        lightQueue.Enqueue(new Point(x, y));
                        break; // Stop vertical ray
                    }

                    // Otherwise, it's air (or transparent), gets full sunlight
                    lightMap[x, y] = 1.0f;
                    lightQueue.Enqueue(new Point(x, y));
                }
            }

            // 1.5. Item Light Sources Pass
            // Iterate through valid chunks in the range
            if (map.ChunkDictionary != null)
            {
                // Calculate chunk range based on startX and endX
                int startChunkX = startX / Global.ChunkSize;
                int endChunkX = (endX - 1) / Global.ChunkSize;
                int chunksHeight = Global.MapHeight / Global.ChunkSize;

                // We need to iterate over all relevant chunks that overlap the horizontal strip
                // Chunks are 1-based in the dictionary according to Game.cs logic:
                // cursorOnChunk = (1 + ((cursorChunkY * (Global.MapWidth / Global.ChunkSize)) + cursorChunkX));
                
                // Let's iterate vertically as well since the light calculation covers the full height
                for (int cx = startChunkX; cx <= endChunkX; cx++)
                {
                    for (int cy = 0; cy < chunksHeight; cy++)
                    {
                         // Reconstruct chunk ID
                         // NOTE: Game.cs formula was: 
                         // cursorOnChunk = (1 + ((cursorChunkY * (MapWidth/ChunkSize)) + cursorChunkX));
                         // We assume map width in chunks is (Global.MapWidth / Global.ChunkSize)
                         int widthInChunks = Global.MapWidth / Global.ChunkSize;
                         int chunkId = 1 + (cy * widthInChunks) + cx;

                         if (map.ChunkDictionary.TryGetValue(chunkId, out var chunk))
                         {
                             if (chunk.Tiles != null)
                             {
                                 foreach (var tile in chunk.Tiles.Values)
                                 {
                                     // Double check X range just in case
                                     if (tile.X < startX || tile.X >= endX) continue;

                                     if (tile.PlacedItem != null && tile.PlacedItem.IsLightSource)
                                     {
                                         float intensity = tile.PlacedItem.LightIntensity;
                                         if (intensity > lightMap[tile.X, tile.Y])
                                         {
                                             lightMap[tile.X, tile.Y] = intensity;
                                             lightQueue.Enqueue(new Point(tile.X, tile.Y));
                                         }
                                     }
                                 }
                             }
                         }
                    }
                }
            }

            // 2. Light Propagation (Flood Fill with Decay)
            while (lightQueue.Count > 0)
            {
                var p = lightQueue.Dequeue();
                float currentLight = lightMap[p.X, p.Y];

                if (currentLight <= 0.05f) continue;

                var neighbors = new Point[]
                {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    // Check bounds - constrain strictly to the update strip?
                    // If we constrain strictly, light won't bleed out of the strip.
                    // If we allow it to go out, we might be writing to areas we didn't clear/init logic for.
                    // Ideally, we clamp 'n' to [startX, endX).
                    if (n.X < startX || n.X >= endX || n.Y < 0 || n.Y >= height) continue;

                    var neighborTile = map.GetTileAt(n.X, n.Y);
                    bool isSolid = (neighborTile != null && neighborTile.IsSolid);

                    float decay = 0.1f;
                    if (isSolid) decay = 0.4f;

                    float potentialLight = currentLight - decay;

                    if (potentialLight > lightMap[n.X, n.Y])
                    {
                        lightMap[n.X, n.Y] = potentialLight;

                        if (!isSolid)
                        {
                            lightQueue.Enqueue(n);
                        }
                    }
                }
            }

            // 3. Apply Light to Tiles
            // Only update tiles in the affected range
            
            // Calculate chunk range based on startX and endX (reusing logic if possible, or recalculating)
            int startCX = startX / Global.ChunkSize;
            int endCX = (endX - 1) / Global.ChunkSize;
            int hChunks = Global.MapHeight / Global.ChunkSize;
            int wChunks = Global.MapWidth / Global.ChunkSize;

            for (int cx = startCX; cx <= endCX; cx++)
            {
                for (int cy = 0; cy < hChunks; cy++)
                {
                    int chunkId = 1 + (cy * wChunks) + cx;

                    if (map.ChunkDictionary.TryGetValue(chunkId, out var chunk))
                    {
                        if (chunk.Tiles != null)
                        {
                            foreach (var tile in chunk.Tiles.Values)
                            {
                                if (tile.X < startX || tile.X >= endX) continue;

                                float l = lightMap[tile.X, tile.Y];
                                byte val = (byte)(l * 255);
                                tile.SetColor(val, val, val, 255);
                            }
                        }
                        chunk.NeedUpdate = false;
                    }
                }
            }
        }        
    }
}