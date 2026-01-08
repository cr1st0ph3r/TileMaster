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
        // breath-first search from each solid tile to find nearest air tile and set shading accordingly
        public void UpdateTileShadingForChunk(int chunkId)
        {
            // maximum distance (in tiles) we'll propagate light from air
            const int maxDistance = 10;

            if (!map.ChunkDictionary.ContainsKey(chunkId))
                return;

            var tiles = map.ChunkDictionary[chunkId].Tiles
                .Where(x => x.Value.IsSolid)
                .ToList();

            foreach (var kv in tiles)
            {
                var tile = kv.Value;
                var startX = tile.X;
                var startY = tile.Y;

                // BFS outwards from the tile to find the nearest non-solid (air) tile
                var visited = new HashSet<(int x, int y)>();
                var q = new Queue<(int x, int y, int d)>();

                // enqueue 4-neighbors at distance 1
                var neighbors = new (int x, int y)[]
                {
                    (startX + 1, startY),
                    (startX - 1, startY),
                    (startX, startY + 1),
                    (startX, startY - 1)
                };

                foreach (var n in neighbors)
                {
                    q.Enqueue((n.x, n.y, 1));
                    visited.Add((n.x, n.y));
                }

                int foundDistance = -1;
                while (q.Count > 0)
                {
                    var (x, y, d) = q.Dequeue();
                    // out of range short-circuit
                    if (d > maxDistance)
                        continue;

                    var t = map.GetTileAt(x, y);
                    // treat null or non-solid as "air"
                    if (t == null || !t.IsSolid)
                    {
                        foundDistance = d;
                        break;
                    }

                    // enqueue neighbors for next ring
                    var ring = new (int x, int y)[]
                    {
                        (x + 1, y),
                        (x - 1, y),
                        (x, y + 1),
                        (x, y - 1)
                    };

                    foreach (var r in ring)
                    {
                        if (!visited.Contains((r.x, r.y)))
                        {
                            visited.Add((r.x, r.y));
                            q.Enqueue((r.x, r.y, d + 1));
                        }
                    }
                }

                // Compute a smooth brightness value (1.0 for distance==1, decreasing to 0 at >maxDistance)
                float brightness;
                if (foundDistance <= 0)
                {
                    brightness = 0f;
                }
                else
                {
                    // distance 1 -> 1.0, distance maxDistance -> ~1 - (maxDistance-1)/maxDistance
                    brightness = 1f - (foundDistance - 1) * (1f / maxDistance);
                    brightness = MathHelper.Clamp(brightness, 0f, 1f);
                }

                // Use an actual RGB(A) filter instead of XNA named colors to get smoother steps.
                // Multiply white by brightness for simple light/dark; change baseColor for tinted light.
                byte level = (byte)(brightness * 255f);
                tile.SetColor(level, level, level, 255);
                map.UpdateTile(tile);
            }
        }

        public void UpdateTileShadingForMap()
        {
            foreach(var chunk in map.ChunkDictionary.Keys)
            {
                UpdateTileShadingForChunk(chunk);
            }
        }
        public void UpdateTileShadingForModifiedChunks()
        {
            foreach (var chunk in map.ChunkDictionary.Where(x=>x.Value.NeedUpdate))
            {
                UpdateTileShadingForChunk(chunk.Key);
                chunk.Value.NeedUpdate = false;
            }
        }
    }
}