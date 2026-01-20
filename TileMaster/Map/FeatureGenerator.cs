using System;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;

namespace TileMaster.Map
{
    public static class FeatureGenerator
    {



        /// <summary>
        /// Creates a random tree with trunk variation, branches and a layered canopy.
        /// Replaces the previous flat rectangular canopy with:
        /// - Slightly leaning trunk
        /// - Several randomized branches
        /// - Layered canopy with jitter and holes for depth
        /// The routine only writes to chunks/tiles that are currently loaded (safe against chunk boundaries).
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="globalId"></param>
        public static void GrowTree(Map map, int chunkId, int globalId)
        {
            try
            {
                var chunk = map.GetChunk(chunkId);
                if (chunk == null) return;

                // Find the tile with globalId = blockId + 3 using direct math-based retrieval
                CollisionTile baseTile = map.GetTileByGlobalId(globalId + 3);
                if (baseTile == null) return;

                var treeBase = baseTile.GlobalId;

                // convert to (x,y)
                var mapWidth = Global.MapWidth;
                var mapHeight = Global.MapHeight;
                var chunksPerRow = Global.MapWidth / Global.ChunkSize;

                var baseX = treeBase % mapWidth;
                var baseY = treeBase / mapWidth;

                // local helper: safely attempt to set a tile if the target chunk & tile exist
                bool TrySet(int x, int y, int tileType)
                {
                    if (x < 0 || y < 0 || x >= mapWidth || y >= mapHeight) return false;
                    var globalId = y * mapWidth + x;
                    var chunkX = x / Global.ChunkSize;
                    var chunkY = y / Global.ChunkSize;
                    var targetChunkIndex = chunkY * chunksPerRow + chunkX;
                    if (map.Chunks == null || targetChunkIndex < 0 || targetChunkIndex >= map.Chunks.Length) return false;
                    var targetChunk = map.Chunks[targetChunkIndex];
                    if (targetChunk == null) return false;

                    // Find tile by globalId
                    bool found = false;
                    for (int i = 0; i < targetChunk.Tiles.Length; i++)
                    {
                        if (targetChunk.Tiles[i] != null && targetChunk.Tiles[i].GlobalId == globalId)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                    map.SetTile(targetChunkIndex + 1, globalId, tileType); // +1 for 1-based chunkId
                    return true;
                }

                var rnd = Game.rnd;

                // trunk parameters
                var trunkHeight = rnd.Next(6, 12);
                var lean = rnd.Next(-1, 2); // -1, 0 or 1 (slight lean)
                var trunkX = baseX;
                var trunkY = baseY;

                // Build trunk with subtle lean and occasional thicker segments
                for (var i = 0; i < trunkHeight; i++)
                {
                    trunkY -= 1;
                    // occasional lateral step to create a natural lean
                    if (i > 1 && rnd.NextDouble() < 0.25)
                    {
                        trunkX = Math.Max(0, Math.Min(mapWidth - 1, trunkX + lean));
                    }
                    TrySet(trunkX, trunkY, (int)TileType.TreeTrunk);

                    // Occasionally add a thicker trunk pixel (simulate 2x trunk)
                    if (rnd.NextDouble() < 0.15)
                    {
                        TrySet(Math.Max(0, trunkX - 1), trunkY, (int)TileType.TreeTrunk);
                        TrySet(Math.Min(mapWidth - 1, trunkX + 1), trunkY, (int)TileType.TreeTrunk);
                    }
                }

                // Branch generation: a few branches sprouting from mid/upper trunk
                var branches = rnd.Next(1, 4);
                for (var b = 0; b < branches; b++)
                {
                    // choose a trunk level to start branch (near top)
                    var startLevel = trunkY + rnd.Next(0, Math.Max(1, trunkHeight / 2));
                    var branchLength = rnd.Next(3, 7);
                    var direction = rnd.Next(0, 2) == 0 ? -1 : 1; // left or right
                    var bx = trunkX;
                    var by = startLevel;

                    for (var s = 0; s < branchLength; s++)
                    {
                        // step outwards and a bit upwards
                        bx = Math.Max(0, Math.Min(mapWidth - 1, bx + direction * (rnd.Next(1, 2))));
                        by = Math.Max(0, by - rnd.Next(0, 2));
                        TrySet(bx, by, (int)TileType.TreeTrunk);

                        // small leaf cluster at branch tip or intermittently
                        if (s == branchLength - 1 || rnd.NextDouble() < 0.25)
                        {
                            var clusterRadius = rnd.Next(2, 4);
                            for (var cx = -clusterRadius; cx <= clusterRadius; cx++)
                            {
                                for (var cy = -clusterRadius; cy <= clusterRadius; cy++)
                                {
                                    // circular-ish cluster with jitter and occasional holes
                                    if (Math.Sqrt(cx * cx + cy * cy) <= clusterRadius + rnd.NextDouble() * 0.5)
                                    {
                                        if (rnd.NextDouble() < 0.2) continue; // hole for depth
                                        TrySet(bx + cx, by + cy, (int)TileType.TreeLeaf);
                                    }
                                }
                            }
                        }
                    }
                }

                // Canopy: layered circular layers decreasing radius to form a rounded top
                var canopyLayers = rnd.Next(3, 5);
                var canopyBaseRadius = rnd.Next(3, 6);

                for (var layer = 0; layer < canopyLayers; layer++)
                {
                    var layerY = trunkY - layer;
                    // radius shrinks with layer index and gets a little random jitter
                    var layerRadius = canopyBaseRadius * (1.0 - (double)layer / canopyLayers) + rnd.NextDouble();
                    var r = (int)Math.Ceiling(layerRadius);

                    for (var dx = -r; dx <= r; dx++)
                    {
                        for (var dy = -r; dy <= r; dy++)
                        {
                            var dist = Math.Sqrt(dx * dx + dy * dy);
                            // add some randomness to keep canopy organic and avoid perfect circles
                            var jitter = rnd.NextDouble() * 0.6 - 0.3;
                            if (dist <= layerRadius + jitter)
                            {
                                // occasionally skip tiles to create holes and depth
                                if (rnd.NextDouble() < 0.12) continue;

                                var lx = trunkX + dx + rnd.Next(-1, 2); // small horizontal jitter
                                var ly = layerY + dy;
                                TrySet(lx, ly, (int)TileType.TreeLeaf);
                            }
                        }
                    }
                }

                // Additional scattered leaves under canopy for depth
                var scatter = rnd.Next(6, 12);
                for (var s = 0; s < scatter; s++)
                {
                    var sx = trunkX + rnd.Next(-canopyBaseRadius - 2, canopyBaseRadius + 3);
                    var sy = trunkY + rnd.Next(-2, canopyLayers + 1);
                    if (rnd.NextDouble() < 0.5) TrySet(sx, sy, (int)TileType.TreeLeaf);
                }
            }
            catch
            {
                // Safe-fail: if chunk boundaries or missing chunks cause writes to fail, don't crash the generator.
            }
        }



    }
}
