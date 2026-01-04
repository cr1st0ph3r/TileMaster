using System.Collections.Generic;
using System.Linq;

namespace TileMaster.Manager
{
    public class TileShadeManager
    {
        private Map.Map map;

        public TileShadeManager(Map.Map map)
        {
            this.map = map;
        }

        public void UpdateTileShadingForChunk(int chunkId)
        {
            // maximum distance (in tiles) we'll propagate light from air
            const int maxDistance = 4;

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

                // Map foundDistance to a brightness level and then to a Color name string
                // immediate neighbor (distance == 1) = fully lit; distance increases -> darker
                string colorName;
                if (foundDistance == 1)
                {
                    colorName = "White"; // 100%
                }
                else if (foundDistance == 2)
                {
                    colorName = "LightGray"; // ~75%
                }
                else if (foundDistance == 3)
                {
                    colorName = "DarkGray"; // ~50%
                 
                }
                else if (foundDistance == 4)
                {
                    colorName = "Gray"; // ~25%
                }
                else
                {
                    colorName = "Black"; // no light within range
                }

                tile.Color = colorName;
            }
        }
    }
}