using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Manager;

namespace TileMaster.Map
{
    public class Map
    {
        public TileInspector tileInspector;
        public GrassManager grass;
        public WaterManager water;
        public TileShadeManager tileShadeMgr;
        public MapManager mapManager;

        // The chunk array used for chunk storage (0-indexed, row-major order)
        public Chunk[] Chunks { get; set; }

        /// <summary>
        /// The list of modified tiles
        /// </summary>
        public List<CollisionTile> ModifiedTiles { get; set; }
     
        /// <summary>
        /// The tile manager used for tile storage and management
        /// </summary>
        public TileManager TileMgr { get; set; }

        /// <summary>
        /// The focus point of the map
        /// </summary>
        public Point? FocusPoint { get; set; } = null;

        /// <summary>
        /// The width of the map
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// The height of the map
        /// </summary>
        public int Height { get; set; }

        public Map()
        {
            Chunks = null; // Will be initialized by MapManager
            TileMgr = new TileManager();
            grass = new GrassManager(this);
            water = new WaterManager(this);
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
        /// Retrieves a tile by its global ID using mathematical extrapolation.
        /// </summary>
        public CollisionTile GetTileByGlobalId(int globalId)
        {
            if (globalId < 0 || globalId >= Global.MapWidth * Global.MapHeight)
                return null;

            int x = globalId % Global.MapWidth;
            int y = globalId / Global.MapWidth;
            return GetTileAt(x, y);
        }

        /// <summary>
        /// Retrieves a background tile by its global ID using mathematical extrapolation.
        /// </summary>
        public BackgroundTile GetBackgroundTileByGlobalId(int globalId)
        {
            if (globalId < 0 || globalId >= Global.MapWidth * Global.MapHeight)
                return null;

            int x = globalId % Global.MapWidth;
            int y = globalId / Global.MapWidth;
            return GetBackgroundTileAt(x, y);
        }

        /// <summary>
        /// Retrieves a tile by its global coordinates using mathematical extrapolation.
        /// </summary>
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

        /// <summary>
        /// Retrieves a background tile by its global coordinates using mathematical extrapolation.
        /// </summary>
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

        /// <summary>
        /// Checks if the map data exists
        /// </summary>
        public bool CheckIfMapDataExists()
        {
            return File.Exists($"{Global.SaveDataFolderName}/map.tlm");
        }

        #region Modify Tiles
        /// <summary>
        /// Sets a tile at a given global ID
        /// </summary>
        public void SetTile(int chunkId, int globalId, int referenceTileId)
        {
            var targetTile = GetTileByGlobalId(globalId);
            if (targetTile == null) return;
            SetTile(targetTile, referenceTileId);
        }

        /// <summary>
        /// Places an item at a given global ID
        /// </summary>
        public void PlaceItem(int chunkId, int globalId, Item item)
        {
            var targetTile = GetTileByGlobalId(globalId);
            if (targetTile == null) return;

            int startX = targetTile.X;
            int startY = targetTile.Y;

            // 1. Validation Logic for the entire NxM area
            for (int ix = 0; ix < item.Width; ix++)
            {
                for (int iy = 0; iy < item.Height; iy++)
                {
                    var currentTile = GetTileAt(startX + ix, startY + iy);
                    if (currentTile == null)
                    {
                        if (Game.GetInstance() != null)
                            Game.LogMessage("Item placement out of bounds.", Color.Red);
                        return;
                    }

                    // Check if already occupied by a solid block or another item
                    if ((currentTile.IsSolid && currentTile.TileId != (int)TileType.Air) || currentTile.PlacedItem != null || currentTile.MultiTileOffset != Point.Zero)
                    {
                        if (Game.GetInstance() != null)
                            Game.LogMessage("Area is already occupied.", Color.Red);
                        return;
                    }
                }
            }

            // 2. Check for support (Background or other criteria)
            // For multi-tile objects, we check if at least one tile has support? 
            // Or if the bottom row has support? Usually, multi-tile objects need floor support.
            bool hasSupport = false;

            for (int ix = 0; ix < item.Width; ix++)
            {
                // Check floor support for the bottom row of the object
                var tileBelow = GetTileAt(startX + ix, startY + item.Height);
                if (tileBelow != null && tileBelow.IsSolid)
                {
                    hasSupport = true;
                    break;
                }

                // Check background support for ANY tile if item allows it
                if (item.PlaceableOnBackground)
                {
                    for (int iy = 0; iy < item.Height; iy++)
                    {
                        var bgTile = GetBackgroundTileAt(startX + ix, startY + iy);
                        if (bgTile != null && bgTile.TileId != (int)TileType.Air)
                        {
                            hasSupport = true;
                            break;
                        }
                    }
                }
                if (hasSupport) break;
            }

            if (!hasSupport)
            {
                if (Game.GetInstance() != null)
                    Game.LogMessage("Item needs support (background wall or solid ground).", Color.Red);
                return;
            }

            // 3. Placement
            Guid? containerId = null;
            if (item.IsContainer)
            {
                var container = ContainerManager.CreateContainer();
                containerId = container.Id;
            }

            for (int ix = 0; ix < item.Width; ix++)
            {
                for (int iy = 0; iy < item.Height; iy++)
                {
                    var currentTile = GetTileAt(startX + ix, startY + iy);
                    
                    if (ix == 0 && iy == 0)
                    {
                        // Master Tile
                        currentTile.PlacedItem = item;
                        currentTile.MultiTileOffset = Point.Zero;
                    }
                    else
                    {
                        // Slave Tile
                        currentTile.PlacedItem = null;
                        currentTile.MultiTileOffset = new Point(-ix, -iy);
                    }

                    currentTile.ContainerId = containerId;
                    currentTile.IsOccupied = false; // Item-only tile is not a block
                    currentTile.IsSolid = false;    // Items don't block movement normally

                    var actualChunk = GetChunk(currentTile.ChunkId);
                    if (actualChunk != null)
                    {
                        actualChunk.HasBeenModified = true;
                        actualChunk.NeedUpdate = true;
                    }

                    AddTileToModificationTracker(currentTile);
                }
            }
        }

        /// <summary>
        /// Performs an action on a tile at a given global ID and action
        /// </summary>
        public void PerformActionOnTile(int chunkId, int globalId, ToolAction action)
        {
            var targetTile = GetTileByGlobalId(globalId);
            if (targetTile == null) return;

            if (action == ToolAction.MineBlock)
            {
                // Check if it's a multi-tile part
                if (targetTile.MultiTileOffset != Point.Zero || targetTile.PlacedItem != null)
                {
                    // Find master tile
                    var masterTile = GetTileAt(targetTile.X + targetTile.MultiTileOffset.X, targetTile.Y + targetTile.MultiTileOffset.Y);
                    if (masterTile != null && masterTile.PlacedItem != null)
                    {
                        var item = masterTile.PlacedItem;
                        int mX = masterTile.X;
                        int mY = masterTile.Y;

                        // Clear all parts
                        for (int ix = 0; ix < item.Width; ix++)
                        {
                            for (int iy = 0; iy < item.Height; iy++)
                            {
                                var part = GetTileAt(mX + ix, mY + iy);
                                if (part != null)
                                {
                                    if (part.ContainerId.HasValue)
                                    {
                                        ContainerManager.RemoveContainer(part.ContainerId.Value);
                                    }
                                    part.PlacedItem = null;
                                    part.ContainerId = null;
                                    part.MultiTileOffset = Point.Zero;
                                    SetTile(part, (int)TileType.Air); // Reset to air/empty
                                }
                            }
                        }
                        return;
                    }
                }

                // Standard block removal
                SetTile(targetTile, (int)TileType.Air);
            }
            else if (action == ToolAction.TransformBlock)
            {
                HammerTile(targetTile);
            }
        }
        public void HammerTile(Tile targetTile)
        {
            var referenceTile = Global.ReferenceTiles[targetTile.TileId];

            if (!targetTile.IsSlope)
            {
                // First time hammering - convert to slope
                targetTile.IsSlope = true;
                targetTile.SlopeRotation = 0;
                targetTile.Rotation = 0f;
                
                var slopeTexture = referenceTile.Textures.FirstOrDefault(x => x.Name.EndsWith("Slope"));
                if (slopeTexture != null)
                {
                    targetTile.Texture = slopeTexture;
                    targetTile.TextureName = slopeTexture.Name;
                }
                else
                {
                    // If no slope texture found, don't convert to slope
                    targetTile.IsSlope = false;
                    return;
                }
            }
            else
            {
                // Already a slope - cycle rotation or revert
                targetTile.SlopeRotation++;

                if (targetTile.SlopeRotation < 4)
                {
                    // Rotate the tile (90, 180, 270 degrees)
                    targetTile.Rotation = targetTile.SlopeRotation * (MathF.PI / 2f);
                }
                else
                {
                    // Revert to original texture
                    targetTile.IsSlope = false;
                    targetTile.SlopeRotation = 0;
                    targetTile.Rotation = 0f;

                    if (referenceTile.AlternateTextures != null && referenceTile.AlternateTextures.Any() && referenceTile.AltTextures != null && referenceTile.AltTextures.Any())
                    {
                        var random = Game.rnd ?? new Random();
                        targetTile.Texture = referenceTile.AltTextures[random.Next(referenceTile.AltTextures.Count)];
                    }
                    else
                    {
                        targetTile.Texture = referenceTile.Texture;
                    }
                    targetTile.TextureName = targetTile.Texture?.Name ?? "None";
                }
            }

            AddTileToModificationTracker(targetTile);
            UpdateTile(targetTile);
        }
        /// <summary>
        /// Updates a given tile with a new reference tile
        /// </summary>
        public void SetTile(Tile targetTile, int referenceTileId, float rotation = 0f)
        {
            var referenceTile = Global.ReferenceTiles[referenceTileId];

            if (referenceTile.AlternateTextures != null && referenceTile.AlternateTextures.Any() && referenceTile.AltTextures != null && referenceTile.AltTextures.Any())
            {
                // Use a local random if Game instance is not available (headless tests)
                var random = Game.rnd ?? new Random();
                targetTile.Texture = referenceTile.AltTextures[random.Next(referenceTile.AltTextures.Count)];
            }
            else
            {
                targetTile.Texture = referenceTile.Texture;
            }

            targetTile.Name = ((TileType)referenceTileId).ToString();
            targetTile.TextureName = targetTile.Texture?.Name ?? "None";
            targetTile.TileId = referenceTileId;
            targetTile.IsOccupied = referenceTile.IsOccupied;
            targetTile.IsSolid = referenceTile.IsSolid;
            targetTile.TextureId = referenceTile.TextureId;
            targetTile.Rotation = rotation;
            var chunk = GetChunk(targetTile.ChunkId);
            if (chunk != null)
            {
                chunk.NeedUpdate = true;
                chunk.HasBeenModified = true;
                if (referenceTileId == (int)TileType.Water)
                {
                    chunk.HasWater = true;
                }
            }
            AddTileToModificationTracker(targetTile);

            UpdateTile(targetTile);
        }
        public void SetTile(Tile targetTile, Texture2D texture = default, float rotation = 0f)
        {
            targetTile.Texture = texture;
            targetTile.TextureName = targetTile.Texture?.Name ?? "None";
            targetTile.Rotation = rotation;
            var chunk = GetChunk(targetTile.ChunkId);
            if (chunk != null)
            {
                chunk.NeedUpdate = true;
                chunk.HasBeenModified = true;
            }

            UpdateTile(targetTile);
        }
        /// <summary>
        /// Updates a tile in the map
        /// </summary>
        public void UpdateTile(Tile updated)
        {
            int globalX = updated.X;
            int globalY = updated.Y;
            int chunkIndex = GlobalToChunkIndex(globalX, globalY);

            if (Chunks != null && chunkIndex >= 0 && chunkIndex < Chunks.Length)
            {
                var chunk = Chunks[chunkIndex];
                if (chunk != null && chunk.Tiles != null)
                {
                    int localIndex = GlobalToLocalIndex(globalX, globalY);
                    if (localIndex >= 0 && localIndex < chunk.Tiles.Length)
                    {
                        chunk.Tiles[localIndex] = (CollisionTile)updated;
                    }
                }
            }
            //Chunks[updated.ChunkId].Tiles[updated.LocalId] = (CollisionTile)updated;
        }
        /// <summary>
        /// Sets a background tile at a given global ID
        /// </summary>
        public void SetBackgroundTile(int chunkId, int globalId, int referenceTileId)
        {
            var targetTile = GetBackgroundTileByGlobalId(globalId);
            if (targetTile == null) return;
            SetBackgroundTile(targetTile, referenceTileId);
        }

        /// <summary>
        /// Updates a background tile in the map
        /// </summary>
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
            targetTile.TextureName = targetTile.Texture?.Name ?? "None";
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

        /// <summary>
        /// Updates a background tile in the map
        /// </summary>
        public void UpdateBackgroundTile(BackgroundTile updated)
        {
            int globalX = updated.X;
            int globalY = updated.Y;
            int chunkIndex = GlobalToChunkIndex(globalX, globalY);

            if (Chunks != null && chunkIndex >= 0 && chunkIndex < Chunks.Length)
            {
                var chunk = Chunks[chunkIndex];
                if (chunk != null && chunk.BackgroundTiles != null)
                {
                    int localIndex = GlobalToLocalIndex(globalX, globalY);
                    if (localIndex >= 0 && localIndex < chunk.BackgroundTiles.Length)
                    {
                        chunk.BackgroundTiles[localIndex] = updated;
                    }
                }
            }
        }

        /// <summary>
        /// Adds a tile to the modification tracker
        /// </summary>
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
        /// <param name="globalId">Global tile ID</param>
        /// <returns></returns>
        public bool IsBlockOnChunk(int chunkId, int globalId)
        {
            var tile = GetTileByGlobalId(globalId);
            return tile != null && tile.ChunkId == chunkId;
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
                            if (bgTile.Color == "Gray" && !bgTile.ColorArgb.HasValue && !bgTile.ColorFilter.HasValue)
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
                    if (tile.IsEdgeTile)
                    {
                        tile.Color = "Gray";
                    }
                }

                tile.Draw(spriteBatch);
            }

        }

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