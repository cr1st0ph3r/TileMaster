using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Manager;

namespace TileMaster.Map
{
    public class Map
    {
        public TileInspector tileInspector;
        public GrassManager grass;
        public TileShadeManager tileShadeMgr;
        public MapManager mapManager;

        //The chunk dictionary used for chunk storage
        public Dictionary<int, Chunk> ChunkDictionary { get; set; }
        //Tile types
        public List<CollisionTiles> TileTypes { get; set; }
        public List<CollisionTiles> ModifiedTiles { get; set; }
        public TileManager TileMgr { get; set; }

        //shoudnt be public
        public int Width, Height;

        public Map()
        {
            ChunkDictionary = new Dictionary<int, Chunk>();
            TileMgr = new TileManager();
            grass = new GrassManager(this);
            tileInspector = new TileInspector(this);
            mapManager = new MapManager(this);
            tileShadeMgr = new TileShadeManager(this);
            ModifiedTiles = new List<CollisionTiles>();
        }

        /// <summary>
        /// retrieves a tile at a given location. Accounts for cross chunk tiles
        /// </summary>
        /// <param name="blockId"></param>
        /// <param name="chunkId"></param>
        /// <param name="direction"></param>
        /// <param name="retrial"></param>
        /// <returns></returns>
        public CollisionTiles GetTileAt(int blockId, int chunkId, string direction, bool retrial = false)
        {
            if (IsBlockOnChunk(chunkId, blockId))
            {
                return ChunkDictionary[chunkId].Tiles[blockId];
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

        public CollisionTiles GetTileAt(int globalX, int globalY)
        {
            // out of bounds guard
            if (globalX < 0 || globalY < 0 || globalX >= Global.MapWidth || globalY >= Global.MapHeight)
            {
                return null;
            }

            // global id: row-major order (row = y)
            var globalId = globalY * Global.MapWidth + globalX;

            // determine chunk coordinates and 1-based chunk id
            var chunkX = globalX / Global.ChunkSize;
            var chunkY = globalY / Global.ChunkSize;
            var chunksPerRow = Global.MapWidth / Global.ChunkSize;
            var chunkId = 1 + (chunkY * chunksPerRow + chunkX);

            // Prefer the loaded chunk tile (has textures and runtime state) if available
            if (ChunkDictionary != null && ChunkDictionary.ContainsKey(chunkId))
            {
                var chunk = ChunkDictionary[chunkId];
                if (chunk != null && chunk.Tiles != null && chunk.Tiles.ContainsKey(globalId))
                {
                    return chunk.Tiles[globalId];
                }
            }
            return null;
        }

        public bool CheckIfMapDataExists()
        {
            return File.Exists($"{Global.ChunkFolderLocation}/map.tlm");
        }

        #region Modify Tiles
        public void SetTile(int chunkId, int blockId, int referenceTileId)
        {
            var targetTile = ChunkDictionary[chunkId].Tiles[blockId];
            SetTile(targetTile, referenceTileId);
        }
        public void SetTile(Tile targetTile, int referenceTileId, float rotation = 0f)
        {
            var referenceTile = TileTypes.FirstOrDefault(x => x.TileId == referenceTileId);

            if (referenceTile.AlternateTextures.Any())
            {
                targetTile.texture = referenceTile.AltTextures[Game.rnd.Next(referenceTile.AltTextures.Count)];
            }
            else
            {
                targetTile.texture = referenceTile.texture;
            }

            targetTile.Name = ((TileType)referenceTileId).ToString();
            targetTile.TextureName = targetTile.texture.Name;
            targetTile.TileId = referenceTileId;
            targetTile.IsOccupied = referenceTile.IsOccupied;
            targetTile.IsSolid = referenceTile.IsSolid;
            targetTile.Rotation = rotation;
            ChunkDictionary[targetTile.ChunkId].NeedUpdate = true;
            AddTileToModificationTracker(targetTile);

            UpdateTile(targetTile);
        }
        public void SetTile(Tile targetTile, Texture2D texture = default, float rotation = 0f)
        {
            targetTile.texture = texture;
            targetTile.TextureName = targetTile.texture.Name;
            targetTile.Rotation = rotation;       
            ChunkDictionary[targetTile.ChunkId].NeedUpdate = true;

            UpdateTile(targetTile);
        }
        public void UpdateTile(Tile updated)
        {
            ChunkDictionary[updated.ChunkId].Tiles[updated.GlobalId] = (CollisionTiles)updated;
        }
        private void AddTileToModificationTracker(Tile tile)
        {
            if (!ModifiedTiles.Contains((CollisionTiles)tile))
            {
                ModifiedTiles.Add((CollisionTiles)tile);
            }
        }
        #endregion

        /// <summary>
        /// Checks whether a chunk is present on the chunk dictionary
        /// </summary>
        /// <param name="chunkId"></param>
        /// <returns></returns>
        private bool IsChunkPresent(int chunkId)
        {
            if (ChunkDictionary.ContainsKey(chunkId))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Checks whether a block is at the specified chunk
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        /// <returns></returns>
        public bool IsBlockOnChunk(int chunkId, int blockId)
        {
            if (ChunkDictionary.ContainsKey(chunkId))
            {
                if (ChunkDictionary[chunkId].Tiles.ContainsKey(blockId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// returns a list containing all the tiles near the player so they can be drawn
        /// </summary>
        /// <param name="referenceChunk"></param>
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
                    tiles.AddRange(ChunkDictionary[c].Tiles.Values);
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
                // base tile (ground) used by original implementation
                var treeBase = ChunkDictionary[chunkId].Tiles[blockId + 3].GlobalId;

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
                    var targetChunkId = 1 + (chunkY * chunksPerRow + chunkX);
                    if (!ChunkDictionary.ContainsKey(targetChunkId)) return false;
                    if (!ChunkDictionary[targetChunkId].Tiles.ContainsKey(globalId)) return false;
                    SetTile(targetChunkId, globalId, tileType);
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
            //draw the entire map
            //foreach (var chunk in ChunkDictionary.Values)
            //{
            //    foreach (var tile in chunk.Tiles.Values)
            //    {
            //        tile.Draw(spriteBatch);
            //    }
            //}

            //draw relevant chunks
            var tiles = GetTilesToDraw(chunkId);
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
            tileShadeMgr.UpdateTileShadingForModifiedChunks();
        }
        /// <summary>
        /// Save the provided chunk dictionary into a PNG image file.
        /// Each tile maps to a single pixel (x = column, y = row).
        /// Preference: use tile entries found in chunks; fall back to a simple TileType -> color mapping.
        /// </summary>
        /// <param name="chunkDict">Chunk dictionary (key = chunkId, value = Chunk)</param>
        /// <param name="fileName">Output file path (png recommended)</param>
        public void SaveChunkDictionaryAsImage(Dictionary<int, Chunk> chunkDict, string fileName)
        {
            try
            {
                if (chunkDict == null)
                    throw new InvalidOperationException("chunkDict is null.");

                int width = Global.MapWidth;
                int height = Global.MapHeight;

                // Flatten chunk tiles into a quick lookup of globalId -> CollisionTiles
                var mapLookup = new Dictionary<int, CollisionTiles>(width * height);
                foreach (var chunk in chunkDict.Values)
                {
                    if (chunk?.Tiles == null) continue;
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
    }

}