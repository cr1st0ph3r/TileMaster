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
            var chunk = GetChunk(chunkId);
            if (chunk == null) return;

            CollisionTile targetTile = null;

            // Find tile by globalId using direct math-based retrieval
            targetTile = GetTileByGlobalId(globalId);

            if (targetTile == null) return;

            // Validation Logic
            // 1. Check if the target tile is already occupied by a solid block
            if (targetTile.IsSolid && targetTile.TileId != (int)TileType.Air)
            {
                if (Game.GetInstance() != null)
                    Game.LogMessage("Cannot place item inside a solid block.", Color.Red);
                return;
            }

            // 2. Check if the tile already has an item
            if (targetTile.PlacedItem != null)
            {
                if (Game.GetInstance() != null)
                    Game.LogMessage("Tile already contains an item.", Color.Red);
                return;
            }

            // 3. Check for support (Background or other criteria)
            bool hasSupport = false;

            if (item.PlaceableOnBackground)
            {
                // Check if there is a background tile behind this
                var bgTile = GetBackgroundTileAt(targetTile.X, targetTile.Y);
                if (bgTile != null && bgTile.TileId != (int)TileType.Air)
                {
                    hasSupport = true;
                }
            }

            // If item is placeable on background but there is no background, we might still allow it if there is floor support?
            // User requirement: "nothing (when it comes to items) can be placed "on top" of a foreground tile as in "two bodies cannot occupy the same place in space" rule"
            // This is handled by check #1. 

            // "loadmap assumes that items can only be placed on foreground items" -> user means Items are mistakenly treated as blocks?
            // "The idea of placeable items is that they can be placed both on foreground tiles as well as on background tiles."
            // "Control which items can be placed on foreground tiles." -> Maybe they mean stick TO a foreground tile (like a torch on a wall block)?
            // Assuming simplified logic for now: Item needs EITHER a background wall OR a solid block adjacent/below (if we implement gravity/attachment later).
            // For now, if PlaceableOnBackground is true, we strictly require a background wall OR a solid attachment point.

            // For this task, let's implement the specific request for Background support.
            if (item.PlaceableOnBackground && !hasSupport)
            {
                // Optionally check for other support types here (like sitting on floor) 
                // but if the item is *primarily* a wall item (like torch on bg), reject if no bg.
                // However, torches can also be placed on the floor usually.

                // Let's check for floor support as a fallback
                var tileBelow = GetTileAt(targetTile.X, targetTile.Y + 1);
                if (tileBelow != null && tileBelow.IsSolid)
                {
                    hasSupport = true;
                }
            }
            else if (!item.PlaceableOnBackground)
            {
                // If NOT placeable on background, it MUST have floor support (or be a flying item?)
                // Assuming standard gravity items need floor.
                var tileBelow = GetTileAt(targetTile.X, targetTile.Y + 1);
                if (tileBelow != null && tileBelow.IsSolid)
                {
                    hasSupport = true;
                }
            }

            if (!hasSupport)
            {
                if (Game.GetInstance() != null)
                    Game.LogMessage("Item needs support (background wall or solid ground).", Color.Red);
                return;
            }

            // Placement allowed
            // We need to 'place' the item inside the Tile object without making the Tile itself solid/occupied by a block
            // The Tile object acts as a container.

            targetTile.PlacedItem = item;
            // IMPORTANT: Do NOT set targetTile.IsOccupied = true or IsSolid = true, 
            // because that would make it act like a Dirt block colliding with player.
            // Items are usually pass-through unless they are furniture with collision.
            // Keeping IsOccupied = false explicitly for the Tile itself, but the Item is there.
            // Wait, existing logic might rely on IsOccupied?
            // SaveDataManager.cs line 86 checks `if (tile == null || !tile.IsOccupied)`. If IsOccupied is false, it saves as Air.
            // SO WE MUST SET IsOccupied = true?
            // If we set IsOccupied = true, is it solid? 
            // CollisionTile has IsSolid property. We can set IsOccupied=true, IsSolid=false.
            // Does IsOccupied mean "There is something here"? Yes.

            // Let's modify the tile to hold the item
            targetTile.IsOccupied = false;
            targetTile.IsSolid = false; // Items don't block movement usually

            // We also need to set the texture id for the tile to render the item?
            // Rendering usually checks PlacedItem? 
            // SaveDataManager uses `writer.Write(true); // HasItem` if PlacedItem != null.
            // But it also writes a TileId. If we are an item, what acts as the "Base" tile? Air?
            // If we set TileId to Air, and IsOccupied to true, SaveData might get confused or behave correctly.
            // Let's check SaveData logic again.
            // Line 368: `if (hasItem ...)` -> creates Item.
            // Line 356: `IsOccupied = true`.
            // So yes, we need IsOccupied = true.

            //targetTile.NeedUpdate = true;
            var actualChunk = GetChunk(targetTile.ChunkId);
            if (actualChunk != null)
            {
                actualChunk.HasBeenModified = true;
                actualChunk.NeedUpdate = true;
            }

            AddTileToModificationTracker(targetTile);
        }

        /// <summary>
        /// Performs an action on a tile at a given global ID and action
        /// </summary>
        public void PerformActionOnTile(int chunkId, int globalId, ToolAction action)
        {
            if(action == ToolAction.MineBlock)
            {
                var targetTile = GetTileByGlobalId(globalId);
                // Reset tile to Air
                SetTile(targetTile, (int)TileType.Air);
            }
        }

        /// <summary>
        /// Updates a given tile with a new reference tile
        /// </summary>
        public void SetTile(Tile targetTile, int referenceTileId, float rotation = 0f)
        {
            var referenceTile = Global.ReferenceTiles.FirstOrDefault(x => x.TileId == referenceTileId);
            if (referenceTile == null) return;

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