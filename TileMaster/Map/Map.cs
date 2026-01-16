using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Manager;

namespace TileMaster.Map
{
    public class Map
    {
        public TileInspector tileInspector;
        public GrassManager grass;
        public TileShadeManager tileShadeMgr;
        public MapManager mapManager;

        // The chunk array used for chunk storage (0-indexed, row-major order)
        public Chunk[] Chunks { get; set; }
        public List<CollisionTile> ModifiedTiles { get; set; }
        public TileManager TileMgr { get; set; }

        //shouldn't be public
        public int Width, Height;

        public Map()
        {
            Chunks = null; // Will be initialized by MapManager
            TileMgr = new TileManager();
            grass = new GrassManager(this);
            tileInspector = new TileInspector(this);
            mapManager = new MapManager(this);
            tileShadeMgr = new TileShadeManager(this);
            ModifiedTiles = new List<CollisionTile>();
        }

        /// <summary>
        /// Returns the number of chunks per row
        /// </summary>
        public int ChunksPerRow => Global.MapWidth / Global.ChunkSize;

        /// <summary>
        /// Returns the total number of chunks
        /// </summary>
        public int TotalChunks => (Global.MapWidth / Global.ChunkSize) * (Global.MapHeight / Global.ChunkSize);

        /// <summary>
        /// Gets a chunk by its 1-based ID (for backward compatibility)
        /// </summary>
        public Chunk GetChunk(int chunkId)
        {
            int index = chunkId - 1; // Convert 1-based to 0-based
            if (index < 0 || Chunks == null || index >= Chunks.Length)
                return null;
            return Chunks[index];
        }

        /// <summary>
        /// Gets a chunk by its 0-based index
        /// </summary>
        public Chunk GetChunkByIndex(int index)
        {
            if (index < 0 || Chunks == null || index >= Chunks.Length)
                return null;
            return Chunks[index];
        }

        /// <summary>
        /// Converts global tile coordinates to local chunk index (0-based within the chunk's Tiles array)
        /// </summary>
        public static int GlobalToLocalIndex(int globalX, int globalY)
        {
            int localX = globalX % Global.ChunkSize;
            int localY = globalY % Global.ChunkSize;
            return localY * Global.ChunkSize + localX;
        }

        /// <summary>
        /// Converts global tile coordinates to chunk index (0-based)
        /// </summary>
        public int GlobalToChunkIndex(int globalX, int globalY)
        {
            int chunkX = globalX / Global.ChunkSize;
            int chunkY = globalY / Global.ChunkSize;
            return chunkY * ChunksPerRow + chunkX;
        }

        /// <summary>
        /// Converts a 1-based chunk ID to a 0-based index
        /// </summary>
        public static int ChunkIdToIndex(int chunkId) => chunkId - 1;

        /// <summary>
        /// Converts a 0-based chunk index to a 1-based chunk ID
        /// </summary>
        public static int ChunkIndexToId(int index) => index + 1;

        /// <summary>
        /// retrieves a tile at a given location. Accounts for cross chunk tiles
        /// </summary>
        /// <param name="blockId"></param>
        /// <param name="chunkId"></param>
        /// <param name="direction"></param>
        /// <param name="retrial"></param>
        /// <returns></returns>
        public CollisionTile GetTileAt(int blockId, int chunkId, string direction, bool retrial = false)
        {
            var chunk = GetChunk(chunkId);
            if (chunk != null)
            {
                // blockId is a globalId, we need to find it in the chunk
                // Search in the chunk's tiles for the matching globalId
                foreach (var tile in chunk.Tiles)
                {
                    if (tile != null && tile.GlobalId == blockId)
                        return tile;
                }
            }
            if (retrial == false)
            {
                if (direction == "right")
                {
                    return GetTileAt(blockId, chunkId + 1, "right", true);
                }
                if (direction == "left")
                {
                    return GetTileAt(blockId, chunkId - 1, "left", true);
                }
                if (direction == "up")
                {
                    return GetTileAt(blockId, chunkId - Global.MapWidth / Global.ChunkSize, "up", true);
                }
            }
            return null;
        }

        public CollisionTile GetTileAt(int globalX, int globalY)
        {
            // out of bounds guard
            if (globalX < 0 || globalY < 0 || globalX >= Global.MapWidth || globalY >= Global.MapHeight)
            {
                return null;
            }

            // determine chunk index (0-based)
            int chunkIndex = GlobalToChunkIndex(globalX, globalY);

            // Prefer the loaded chunk tile (has textures and runtime state) if available
            if (Chunks != null && chunkIndex >= 0 && chunkIndex < Chunks.Length)
            {
                var chunk = Chunks[chunkIndex];
                if (chunk != null && chunk.Tiles != null)
                {
                    int localIndex = GlobalToLocalIndex(globalX, globalY);
                    if (localIndex >= 0 && localIndex < chunk.Tiles.Length)
                    {
                        return chunk.Tiles[localIndex];
                    }
                }
            }
            return null;
        }

        public BackgroundTile GetBackgroundTileAt(int globalX, int globalY)
        {
            // out of bounds guard
            if (globalX < 0 || globalY < 0 || globalX >= Global.MapWidth || globalY >= Global.MapHeight)
            {
                return null;
            }

            // determine chunk index (0-based)
            int chunkIndex = GlobalToChunkIndex(globalX, globalY);

            // Prefer the loaded chunk tile (has textures and runtime state) if available
            if (Chunks != null && chunkIndex >= 0 && chunkIndex < Chunks.Length)
            {
                var chunk = Chunks[chunkIndex];
                if (chunk != null && chunk.BackgroundTiles != null)
                {
                    int localIndex = GlobalToLocalIndex(globalX, globalY);
                    if (localIndex >= 0 && localIndex < chunk.BackgroundTiles.Length)
                    {
                        return chunk.BackgroundTiles[localIndex];
                    }
                }
            }
            return null;
        }

        public bool CheckIfMapDataExists()
        {
            return File.Exists($"{Global.SaveDataFolderName}/map.tlm");
        }

        #region Modify Tiles
        public void SetTile(int chunkId, int blockId, int referenceTileId)
        {
            var chunk = GetChunk(chunkId);
            if (chunk == null) return;

            // Find tile by globalId
            for (int i = 0; i < chunk.Tiles.Length; i++)
            {
                if (chunk.Tiles[i] != null && chunk.Tiles[i].GlobalId == blockId)
                {
                    SetTile(chunk.Tiles[i], referenceTileId);
                    chunk.HasBeenModified = true;
                    return;
                }
            }
        }
        public void SetTile(Tile targetTile, int referenceTileId, float rotation = 0f)
        {
            var referenceTile = Global.ReferenceTiles.FirstOrDefault(x => x.TileId == referenceTileId);

            if (referenceTile.AlternateTextures.Any())
            {
                targetTile.Texture = referenceTile.AltTextures[Game.rnd.Next(referenceTile.AltTextures.Count)];
            }
            else
            {
                targetTile.Texture = referenceTile.Texture;
            }

            targetTile.Name = ((TileType)referenceTileId).ToString();
            targetTile.TextureName = targetTile.Texture.Name;
            targetTile.TileId = referenceTileId;
            targetTile.IsOccupied = referenceTile.IsOccupied;
            targetTile.IsSolid = referenceTile.IsSolid;
            targetTile.Rotation = rotation;
            var chunk = GetChunk(targetTile.ChunkId);
            if (chunk != null) 
            {
                chunk.NeedUpdate = true;
                chunk.HasBeenModified = true;
            }
            AddTileToModificationTracker(targetTile);

            UpdateTile(targetTile);
        }
        public void SetTile(Tile targetTile, Texture2D texture = default, float rotation = 0f)
        {
            targetTile.Texture = texture;
            targetTile.TextureName = targetTile.Texture.Name;
            targetTile.Rotation = rotation;
            var chunk = GetChunk(targetTile.ChunkId);
            if (chunk != null) 
            {
                chunk.NeedUpdate = true;
                chunk.HasBeenModified = true;
            }

            UpdateTile(targetTile);
        }
        public void UpdateTile(Tile updated)
        {
            var chunk = GetChunk(updated.ChunkId);
            if (chunk == null) return;

            // Find and update by globalId
            for (int i = 0; i < chunk.Tiles.Length; i++)
            {
                if (chunk.Tiles[i] != null && chunk.Tiles[i].GlobalId == updated.GlobalId)
                {
                    chunk.Tiles[i] = (CollisionTile)updated;
                    return;
                }
            }
        }
        public void SetBackgroundTile(int chunkId, int blockId, int referenceTileId)
        {
            var chunk = GetChunk(chunkId);
            if (chunk == null) return;

            // Find tile by globalId
            for (int i = 0; i < chunk.BackgroundTiles.Length; i++)
            {
                if (chunk.BackgroundTiles[i] != null && chunk.BackgroundTiles[i].GlobalId == blockId)
                {
                    SetBackgroundTile(chunk.BackgroundTiles[i], referenceTileId);
                    chunk.HasBeenModified = true;
                    return;
                }
            }
        }

        public void SetBackgroundTile(BackgroundTile targetTile, int referenceTileId, float rotation = 0f)
        {
            var referenceTile = Global.ReferenceTiles.FirstOrDefault(x => x.TileId == referenceTileId);

            if (referenceTile.AlternateTextures.Any())
            {
                targetTile.Texture = referenceTile.AltTextures[Game.rnd.Next(referenceTile.AltTextures.Count)];
            }
            else
            {
                targetTile.Texture = referenceTile.Texture;
            }

            targetTile.Name = ((TileType)referenceTileId).ToString();
            targetTile.TextureName = targetTile.Texture.Name;
            targetTile.TileId = referenceTileId;
            targetTile.Rotation = rotation;
            targetTile.Color = "Gray"; // Ensure background tiles stay dark/dimmed
            
            var chunk = GetChunk(targetTile.ChunkId);
            if (chunk != null) 
            {
                chunk.NeedUpdate = true;
                chunk.HasBeenModified = true;
            }
            // AddTileToModificationTracker(targetTile); // TODO: Add tracker for background tiles if needed

            UpdateBackgroundTile(targetTile);
        }

        public void UpdateBackgroundTile(BackgroundTile updated)
        {
            var chunk = GetChunk(updated.ChunkId);
            if (chunk == null) return;

            // Find and update by globalId
            for (int i = 0; i < chunk.BackgroundTiles.Length; i++)
            {
                if (chunk.BackgroundTiles[i] != null && chunk.BackgroundTiles[i].GlobalId == updated.GlobalId)
                {
                    chunk.BackgroundTiles[i] = updated;
                    return;
                }
            }
        }

        private void AddTileToModificationTracker(Tile tile)
        {
            if (!ModifiedTiles.Contains((CollisionTile)tile))
            {
                ModifiedTiles.Add((CollisionTile)tile);
            }
        }
        #endregion

        /// <summary>
        /// Checks whether a chunk is present in the array
        /// </summary>
        /// <param name="chunkId">1-based chunk ID</param>
        /// <returns></returns>
        private bool IsChunkPresent(int chunkId)
        {
            int index = chunkId - 1;
            return Chunks != null && index >= 0 && index < Chunks.Length && Chunks[index] != null;
        }
        /// <summary>
        /// Checks whether a block is at the specified chunk (by globalId)
        /// </summary>
        /// <param name="chunkId">1-based chunk ID</param>
        /// <param name="blockId">Global tile ID</param>
        /// <returns></returns>
        public bool IsBlockOnChunk(int chunkId, int blockId)
        {
            var chunk = GetChunk(chunkId);
            if (chunk != null && chunk.Tiles != null)
            {
                foreach (var tile in chunk.Tiles)
                {
                    if (tile != null && tile.GlobalId == blockId)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// returns a list containing all the tiles near the player so they can be drawn
        /// </summary>
        /// <param name="referenceChunk">1-based chunk ID</param>
        /// <returns></returns>
        private List<Tile> GetTilesToDraw(int referenceChunk)
        {
            //this calculation can take into consideration the current window size, although if zoom is implemented,
            //it will also have to be taken into account as well.
            //Currently, for the standard 1920x1080 resolution the approximate value for chunks to be rendered is 2
            //how many chunks fit on the screen?
            var chunksOnTheScreenHorizontally = 2;
            var chunksOnTheScreenVertically = 2;

            //used to access upper and lower row chunks
            var rowMultiplier = Global.MapWidth / Global.ChunkSize;

            var tiles = new List<Tile>();
            var CTD = new List<int>();
            //horizontal
            foreach (var i in Enumerable.Range(1, chunksOnTheScreenHorizontally))
            {
                CTD.Add(referenceChunk - i);
                CTD.Add(referenceChunk + i);
            }
            //vertical
            foreach (var i in Enumerable.Range(1, chunksOnTheScreenVertically))
            {
                CTD.Add(referenceChunk + rowMultiplier + i);
                CTD.Add(referenceChunk - rowMultiplier + i);
                CTD.Add(referenceChunk + rowMultiplier - i);
                CTD.Add(referenceChunk - rowMultiplier - i);
            }
            CTD.Add(referenceChunk + rowMultiplier);
            CTD.Add(referenceChunk - rowMultiplier);
            CTD.Add(referenceChunk);

            foreach (var c in CTD)
            {
                //because of player being on very edge map
                if (IsChunkPresent(c))
                {
                    var chunk = GetChunk(c);
                    if (chunk != null && chunk.Tiles != null)
                    {
                        tiles.AddRange(chunk.Tiles.Where(t => t != null));
                    }
                }
            }
            return tiles;
        }

        #region Tree Logic

        /// <summary>
        /// Creates a random tree with trunk variation, branches and a layered canopy.
        /// Replaces the previous flat rectangular canopy with:
        /// - Slightly leaning trunk
        /// - Several randomized branches
        /// - Layered canopy with jitter and holes for depth
        /// The routine only writes to chunks/tiles that are currently loaded (safe against chunk boundaries).
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        public void GrowTree(int chunkId, int blockId)
        {
            try
            {
                var chunk = GetChunk(chunkId);
                if (chunk == null) return;

                // Find the tile with globalId = blockId + 3
                CollisionTile baseTile = null;
                foreach (var tile in chunk.Tiles)
                {
                    if (tile != null && tile.GlobalId == blockId + 3)
                    {
                        baseTile = tile;
                        break;
                    }
                }
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
                    if (Chunks == null || targetChunkIndex < 0 || targetChunkIndex >= Chunks.Length) return false;
                    var targetChunk = Chunks[targetChunkIndex];
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
                    SetTile(targetChunkIndex + 1, globalId, tileType); // +1 for 1-based chunkId
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
        #endregion

        public void Draw(SpriteBatch spriteBatch, int chunkId)
        {
            //draw relevant chunks
            var tiles = GetTilesToDraw(chunkId);
            
            // Draw background tiles first
            foreach (var tile in tiles)
            {
                var chunk = GetChunk(tile.ChunkId);
                if (chunk != null && chunk.BackgroundTiles != null)
                {
                    // Find background tile by matching local index
                    int localIndex = GlobalToLocalIndex(tile.X, tile.Y);
                    if (localIndex >= 0 && localIndex < chunk.BackgroundTiles.Length)
                    {
                        var bgTile = chunk.BackgroundTiles[localIndex];
                        if (bgTile != null)
                        {
                            // Ensure background tiles are always drawn with a specific color filter to distinguish them
                            if(bgTile.Color == "Gray" && !bgTile.ColorArgb.HasValue && !bgTile.ColorFilter.HasValue) 
                            {
                                // Force a visual dimming if using default
                                bgTile.ColorFilter = Microsoft.Xna.Framework.Color.Gray;
                            }
                            bgTile.Draw(spriteBatch);
                        }
                    }
                }
            }

            // Draw foreground tiles
            foreach (var tile in tiles)
            {
                if (Global.MarkTilesOnTheEdge)
                {
                    if (tile.isEdgeTile)
                    {
                        tile.Color = "Gray";
                    }
                }

                tile.Draw(spriteBatch);
            }

        }
        
        public Point? FocusPoint { get; set; } = null;

        /// <summary>
        /// Processes all tiles that have been marked as modified and clears the list of modified tiles.
        /// </summary>
        /// <remarks>Call this method after making changes to tiles to ensure that all modifications are
        /// handled and the internal list of modified tiles is reset. This method should be called before performing
        /// operations that require the tile state to be up to date.</remarks>
        public void UpdateModifiedTiles()
        {
            foreach (var tile in ModifiedTiles)
            {
                //do whatever needs to be done and move on
            }
            ModifiedTiles.Clear();
        }
    }
}