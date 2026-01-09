using System;
using System.Collections.Generic;
using System.Linq;
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

        public void UpdateLighting()
        {
            int width = Global.MapWidth;
            int height = Global.MapHeight;

            // Initialize light map if needed or resize
            if (lightMap == null || lightMap.GetLength(0) != width || lightMap.GetLength(1) != height)
            {
                lightMap = new float[width, height];
            }

            // Clear light map
            Array.Clear(lightMap, 0, lightMap.Length);

            // Queue for light propagation
            var lightQueue = new Queue<Point>();

            // 1. Sunlight Pass (Vertical Rays)
            // Iterate over every column
            for (int x = 0; x < width; x++)
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

            // 2. Light Propagation (Flood Fill with Decay)
            // Light spreads from high intensity neighbors to lower intensity ones
            while (lightQueue.Count > 0)
            {
                var p = lightQueue.Dequeue();
                float currentLight = lightMap[p.X, p.Y];

                // If light is too low, stop propagating
                if (currentLight <= 0.05f) continue;

                // Neighbors (up, down, left, right)
                var neighbors = new Point[]
                {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    // Check bounds
                    if (n.X < 0 || n.X >= width || n.Y < 0 || n.Y >= height) continue;

                    var neighborTile = map.GetTileAt(n.X, n.Y);
                    bool isSolid = (neighborTile != null && neighborTile.IsSolid);

                    // Light decay factor
                    // - Air transmits light well (small decay)
                    // - Solid blocks block light heavily (or completely)
                    float decay = 0.1f; 
                    if (isSolid) decay = 0.4f; // Light penetrates solids very poorly

                    float potentialLight = currentLight - decay;

                    // If we found a brighter path to this neighbor, update it and queue
                    if (potentialLight > lightMap[n.X, n.Y])
                    {
                        lightMap[n.X, n.Y] = potentialLight;
                        
                        // Only propagate FROM non-solid blocks or slightly into solids.
                        // We typically don't propagate *out* of a solid block deeply, 
                        // but allowing 1 step of absorption into the wall is good.
                        if (!isSolid)
                        {
                            lightQueue.Enqueue(n);
                        }
                    }
                }
            }

            // 3. Apply Light to Tiles
            foreach (var chunk in map.ChunkDictionary.Values)
            {
                // Optimization: if we wanted, we could only update modified chunks, 
                // but global light propagation affects everything, so we update visuals for all.
                
                foreach (var tile in chunk.Tiles.Values)
                {
                    float l = lightMap[tile.X, tile.Y];
                    byte val = (byte)(l * 255);
                    // Minimal ambient light so complete darkness isn't pitch black logic-wise 
                    // (optional, but pure black can be hard to see). Let's stick to calculated.
                    
                    tile.SetColor(val, val, val, 255);
                }
                chunk.NeedUpdate = false;
            }
        }

        // Wrapper to match potential external calls, although we recommend calling UpdateLighting directly.
        public void UpdateTileShadingForMap()
        {
            UpdateLighting();
        }
        
        // This old method signature is kept for compatibility if needed, but it triggers a full update now
        // because light is global.
        public void UpdateTileShadingForModifiedChunks()
        {
             // Check if any chunk needs update
             bool anyUpdate = map.ChunkDictionary.Values.Any(c => c.NeedUpdate);
             if (anyUpdate)
             {
                 UpdateLighting();
             }
        }
    }
}