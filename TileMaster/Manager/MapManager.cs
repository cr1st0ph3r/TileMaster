using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Helper;
using TileMaster.Map;

namespace TileMaster.Manager
{
    public class MapManager
    {
        
        private WorldData worldData;
        private Map.Map map;
        //The map dictionary used for map generation
        public Dictionary<int, CollisionTile> MapDictionary { get; set; }
        public Dictionary<int, CollisionTile> BackgroundMapDictionary { get; set; }

        private ConcurrentQueue<(int index, Chunk chunk)> _loadedChunksQueue = new ConcurrentQueue<(int index, Chunk chunk)>();
        private HashSet<int> _loadingChunks = new HashSet<int>();

        public MapManager(Map.Map map)
        {
            this.map = map;          
        }

        #region Map Loading
        /// <summary>
        /// Saves the currently loaded chunks to the save file
        /// </summary>
        public void SaveMap()
        {
            if (worldData == null)
            {
                worldData = new WorldData
                {
                    WorldHeight = Global.MapHeightMultiplier,
                    WorldWidth = Global.MapWidthMultiplier
                };
            }

            // Save all currently loaded chunks
            var loadedChunks = new Dictionary<int, Chunk>();
            if (map.Chunks != null)
            {
                for (int i = 0; i < map.Chunks.Length; i++)
                {
                    if (map.Chunks[i] != null)
                    {
                        loadedChunks[i + 1] = map.Chunks[i]; // 1-based key
                    }
                }
            }
            SaveDataManager.SaveGame(worldData, loadedChunks);
        }

        /// <summary>
        /// Initializes the map structure from save data but does NOT load chunks.
        /// Chunks are loaded dynamically via UpdateChunks.
        /// </summary>
        public void LoadMap()
        {
            var gameInstance = Game.GetInstance();
            worldData = SaveDataManager.LoadGame();
            
            if (worldData == null) return; // Handle error appropriately

            gameInstance._mainPanel.InitializeLoadProgress("Initializing map structure");

            Global.MapHeightMultiplier = worldData.WorldHeight;
            Global.MapWidthMultiplier = worldData.WorldWidth;
            
            // Recalculate global dimensions
            Global.MapWidth = Global.MapWidthMultiplier * Global.ChunkSize;
            Global.MapHeight = Global.MapHeightMultiplier * Global.ChunkSize;

            map.Width = Global.MapWidth * Global.TileSize;
            map.Height = Global.MapHeight * Global.TileSize;

            int totalChunks = worldData.WorldWidth * worldData.WorldHeight;
            map.Chunks = new Chunk[totalChunks];

            Global.IsMapLoaded = true;
            gameInstance._mainPanel.HideLoadProgress();
        }

        public void ProcessPendingChunks()
        {
            while (_loadedChunksQueue.TryDequeue(out var result))
            {
                if (map.Chunks != null && result.index >= 0 && result.index < map.Chunks.Length)
                {
                    map.Chunks[result.index] = result.chunk;
                }
                
                // Remove from loading set based on chunk ID (1-based)
                if (result.chunk != null)
                {
                     // We could track by ID here, but removing by index logic is tricky since we only have index here easily
                     // Actually we can reconstruct ID
                     int id = result.index + 1;
                     lock (_loadingChunks) { _loadingChunks.Remove(id); }
                }
            }
        }

        /// <summary>
        /// Updates loaded chunks based on player position.
        /// Loads chunks within range and unloads chunks outside range.
        /// </summary>
        public void UpdateChunks(Vector2 playerPosition)
        {
            if (map.Chunks == null) return;

            int playerTileX = (int)(playerPosition.X / Global.TileSize);
            int playerTileY = (int)(playerPosition.Y / Global.TileSize);
            
            // Safety check for bounds
            if (playerTileX < 0) playerTileX = 0;
            if (playerTileY < 0) playerTileY = 0;

            int centerChunkIndex = map.GlobalToChunkIndex(playerTileX, playerTileY);
            
            // Determine render distance (radius in chunks)
            // 2 horizontal radius means: center - 2 to center + 2 (5 chunks wide)
            int radiusX = 3; 
            int radiusY = 3;

            int chunksPerRow = map.ChunksPerRow;
            int totalChunks = map.Chunks.Length;

            int centerChunkX = centerChunkIndex % chunksPerRow;
            int centerChunkY = centerChunkIndex / chunksPerRow;

            var chunksToKeep = new HashSet<int>();

            // Identify chunks to keep/load
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    int targetChunkX = centerChunkX + x;
                    int targetChunkY = centerChunkY + y;

                    // Bounds check
                    if (targetChunkX >= 0 && targetChunkX < chunksPerRow &&
                        targetChunkY >= 0 && targetChunkY < (totalChunks / chunksPerRow))
                    {
                        int chunkIndex = targetChunkY * chunksPerRow + targetChunkX;
                        chunksToKeep.Add(chunkIndex);
                    }
                }
            }

            // 1. Unload distant chunks
            for (int i = 0; i < map.Chunks.Length; i++)
            {
                if (map.Chunks[i] != null && !chunksToKeep.Contains(i))
                {
                    // Save if modified before unloading
                    if (map.Chunks[i].HasBeenModified)
                    {
                        // Save just this chunk
                        var dict = new Dictionary<int, Chunk> { { i + 1, map.Chunks[i] } };
                        SaveDataManager.SaveGame(worldData, dict);
                    }
                    
                    // Unload
                    map.Chunks[i] = null;
                }
            }

            // 2. Load missing chunks asynchronously
            foreach (var chunkIndex in chunksToKeep)
            {
                // If chunk is not loaded AND not currently loading
                if (map.Chunks[chunkIndex] == null)
                {
                    int chunkId = chunkIndex + 1;
                    bool startLoad = false;
                    lock(_loadingChunks)
                    {
                        if (!_loadingChunks.Contains(chunkId))
                        {
                            _loadingChunks.Add(chunkId);
                            startLoad = true;
                        }
                    }

                    if (startLoad)
                    {
                        Task.Run(async () => 
                        {
                            try
                            {
                                var chunk = await SaveDataManager.LoadChunkAsync(chunkId);
                                if (chunk != null)
                                {
                                    _loadedChunksQueue.Enqueue((chunkIndex, chunk));
                                }
                                else
                                {
                                    // Failed to load or doesn't exist? Remove from loading so we can retry or handle it
                                     lock(_loadingChunks) { _loadingChunks.Remove(chunkId); }
                                }
                            }
                            catch
                            {
                                 lock(_loadingChunks) { _loadingChunks.Remove(chunkId); }
                            }
                        });
                    }
                }
            }
        }
        #endregion

        #region Map Generation

        public void GenerateMap()
        {  
            var gameInstance = Game.GetInstance();
            int seed = Game.rnd.Next(100000000); // Master seed for the world
            var initialArrayMap = Util.MapGenerator.GenerateRandomMap(seed);
            
            // Generate background with the SAME seed to ensure topography matches
            var backgroundArrayMap = Util.MapGenerator.GenerateBackgroundMap(seed);
          
            gameInstance._mainPanel.InitializeLoadProgress("Generating map dictionary");
            MapDictionary =  GenerateMapDictionary(initialArrayMap);
            ImageHelper.SaveMapDictionaryAsImage(MapDictionary, "GeneratedMap.png");
            BackgroundMapDictionary = GenerateMapDictionary(backgroundArrayMap);

            gameInstance._mainPanel.InitializeLoadProgress("Generating chunks");
            ToChunks();

            //fix grass textures before saving the map
            for (int i = 0; i < map.Chunks.Length; i++)
            {
                if (map.Chunks[i].Tiles.Any(x => x.TileId == (int)TileType.DirtWithGrass))
                {
                    map.grass.GrowGrass(i);

                }
            } 

            // Initialize worldData properly for saving
             worldData = new WorldData
            {
                WorldHeight = Global.MapHeightMultiplier,
                WorldWidth = Global.MapWidthMultiplier
            };
                       
            gameInstance._mainPanel.InitializeLoadProgress("Saving map to file");
            SaveMap();            
            gameInstance._mainPanel.HideLoadProgress();           
        }
        /// <summary>
        /// Generate a dictionary map from a 2d integer array using threads
        /// </summary>
        /// <param name="mapMatrice"></param>
        public Dictionary<int, CollisionTile> GenerateMapDictionary(int[,] mapMatrice)
        {
            int width = mapMatrice.GetLength(0);
            int height = mapMatrice.GetLength(1);
            int totalTiles = width * height;

            // 1. Pre-allocate an array to hold the results (fastest way to work in parallel)
            var tileArray = new CollisionTile[totalTiles];

            // 2. Use Parallel.For to handle thread pooling automatically
            Parallel.For(0, width, col =>
            {
                for (int row = 0; row < height; row++)
                {
                    int tileId = mapMatrice[col, row];
                    var tType = Global.ReferenceTiles[tileId];

                    // Use row-major indexing
                    int globalId = row * width + col;

                    tileArray[globalId] = new CollisionTile(tType, col, row, 0, globalId);
                }
            });

            // 3. Convert the array to a dictionary
            return tileArray.ToDictionary(t => t.GlobalId, t => t);           
        }

        private void ToChunks()
        {
            // Use ceiling to include partial sectors if map size isn't an exact multiple of chunk size
            var SectorsInX = (Global.MapWidth + Global.ChunkSize - 1) / Global.ChunkSize;
            var SectorsInY = (Global.MapHeight + Global.ChunkSize - 1) / Global.ChunkSize;
            int totalChunks = SectorsInX * SectorsInY;
            var chunks = new Chunk[totalChunks];
            
            var blockCount = 1;
            var chunkIndex = 0;
            var pointOnscreenCounter = 0;
            
            for (var gridY = 0; gridY < SectorsInY; gridY++)
            {
                for (var gridX = 0; gridX < SectorsInX; gridX++)
                {
                    var chunk = new Chunk();
                    chunk.PositionOnscreen = pointOnscreenCounter++;
                    var localChunkCounter = 0;
                    
                    for (var localY = 0; localY < Global.ChunkSize; localY++)
                    {
                        // iterate local coords inside the chunk
                        for (var localX = 0; localX < Global.ChunkSize; localX++)
                        {
                            var globalX = gridX * Global.ChunkSize + localX;
                            if (globalX >= Global.MapWidth) break; // outside map columns

                            var globalY = gridY * Global.ChunkSize + localY;
                            if (globalY >= Global.MapHeight) break; // outside map rows

                            // global index in row-major order (same as GenRow)
                            var globalId = globalY * Global.MapWidth + globalX;

                            var tile = MapDictionary[globalId];
                            //if (!MapDictionary.TryGetValue(globalId, out var tile))
                            //    continue; // defensive: skip missing entries

                            bool isEdgeTile = localX == 0 || localX == Global.ChunkSize - 1 || localY == 0 || localY == Global.ChunkSize - 1;

                            // Update tile metadata (1-based chunkId for compatibility)
                            tile.ChunkId = chunkIndex + 1;
                            tile.isEdgeTile = isEdgeTile;
                            tile.LocalId = localChunkCounter;
                            tile.GlobalId = globalId;

                            // store into chunk.Tiles using local index
                            chunk.Tiles[localChunkCounter] = tile;
                            
                            // Create background tile
                            var bgTile = BackgroundMapDictionary[globalId].ToBackgroundTile();
                            bgTile.Color = "Gray";
                            bgTile.LocalId = localChunkCounter;
                            bgTile.ChunkId = chunkIndex + 1;
                            chunk.BackgroundTiles[localChunkCounter] = bgTile;

                            // also update the master map entry
                            MapDictionary[globalId].isEdgeTile = isEdgeTile;

                            blockCount++;
                            localChunkCounter++;
                        }
                    }
                    chunks[chunkIndex] = chunk;
                    chunkIndex++;
                }
            }            
            map.Chunks = chunks;
        }
        /// <summary>
        /// generates a column of blocks (one x across all y)
        /// </summary>
        /// <param name="mapMatrice"></param>
        /// <param name="startingX">column (x) index</param>
        /// <param name="globalCounter"></param>
        /// <returns></returns>
        public Dictionary<int, CollisionTile> GenRow(int[,] mapMatrice, int startingX, int globalCounter)
        {
            var dictMap = new Dictionary<int, CollisionTile>();

            int width = mapMatrice.GetLength(0); // number of columns (x)
            int height = mapMatrice.GetLength(1); // number of rows (y)

            for (var y = 0; y < height; y++)
            {
                var number = mapMatrice[startingX, y];
                //var tType = Global.ReferenceTiles.FirstOrDefault(tt => tt.TileId == number);
                var tType = Global.ReferenceTiles[number];

                // Use row-major indexing: globalId = y * width + x
                var globalId = y * width + startingX;

                dictMap.Add(globalId, new CollisionTile(tType, startingX, y, 0, globalId));
            }
            return dictMap;
        }
        #endregion
    }
}