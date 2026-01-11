using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TileMaster.Entity;
using TileMaster.Entity.Enums;
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
        public MapManager(Map.Map map)
        {
            this.map = map;          
        }
   

        #region Map Loading
        /// <summary>
        /// Saves the map data into their respective files
        /// </summary>
        public void SaveMap()
        {
            worldData = new WorldData
            {
                WorldHeight = Global.MapHeightMultiplier,
                WorldWidth = Global.MapWidthMultiplier
            };
            SaveDataManager.SaveGame(worldData, map.ChunkDictionary);
        }

        private BackgroundTile GenerateEmptyBackgroundTile(Tile tile)
        {
            return new BackgroundTile
            {
                Name = "Air",
                X = tile.X,
                Y = tile.Y,
                Height = tile.Height,
                Width = tile.Width,
                GlobalId = tile.GlobalId,
                LocalId = tile.LocalId,
                ChunkId = tile.ChunkId,
            };
        }

        /// <summary>
        /// Loads a map from a binary source
        /// </summary>
        /// <param name="content"></param>
        public void LoadMap()
        {
            var gameInstance = Game.GetInstance();
            map.ChunkDictionary = new Dictionary<int, Chunk>();
            worldData = SaveDataManager.LoadGame();
            gameInstance._mainPanel.InitializeLoadProgress("Calculating chunk data");
            var chunkId = 1;

            foreach (var rawChunk in worldData.RawMapData)
            {
                var chunk = new Chunk();

                gameInstance._mainPanel.UpdateLoadProgress(chunkId * 100 / worldData.RawMapData.Count);

                if (rawChunk.Value.Values.Any(x => x.TileId == (int)TileType.DirtWithGrass))
                {
                    chunk.HasGrass = true;
                    chunk.NeedUpdate = true;
                }

                chunk.Tiles = rawChunk.Value;

                // Attempt to load background tiles
                // chunkId corresponds to 1-based index (1, 2, ...).
                // SaveDataManager saves/loads files with 0-based index (chunk0, chunk1...) and sorts them.
                // So we expect fileID = chunkId - 1.
                // RawBackgroundData is keyed by fileID.
                var backgroundKey = chunkId - 1;
                if (worldData.RawBackgroundData.ContainsKey(backgroundKey))
                {
                    var bgTiles = worldData.RawBackgroundData[backgroundKey];
                    if (bgTiles != null)
                    {
                        chunk.BackgroundTiles = bgTiles;
                    }
                }
                chunk.SetRectangles();
                chunk.InitializeTextures();
                chunk.PositionOnscreen = chunkId;
                map.ChunkDictionary.Add(chunkId, chunk);
                chunkId++;
            }

            Global.MapHeightMultiplier = worldData.WorldHeight;
            Global.MapWidthMultiplier = worldData.WorldWidth;
            map.Width = worldData.WorldWidth * Global.ChunkSize * Global.TileSize;
            map.Height = worldData.WorldHeight * Global.ChunkSize * Global.TileSize;

            Global.IsMapLoaded = true;
            ImageHelper.SaveChunkDictionaryAsImage(map.ChunkDictionary, "loaded_map.png");
            gameInstance._mainPanel.HideLoadProgress();
        }
        #endregion

        #region Map Generation

        public void GenerateMap()
        {
            var initialArrayMap = Util.MapGenerator.GenerateRandomMap();

            var gameInstance = Game.GetInstance();
            gameInstance._mainPanel.InitializeLoadProgress("Generating map dictionary");
            GenerateMapDictionary(initialArrayMap);
            gameInstance._mainPanel.InitializeLoadProgress("Generating chunks");
            ToChunks();
            gameInstance._mainPanel.InitializeLoadProgress("Saving map to file");
            SaveMap();            
            gameInstance._mainPanel.HideLoadProgress();           
        }
        /// <summary>
        /// Generate a dictionary map from a 2d integer array using threads
        /// </summary>
        /// <param name="mapMatrice"></param>
        public void GenerateMapDictionary(int[,] mapMatrice)
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

                    // Use your row-major indexing
                    int globalId = row * width + col;

                    tileArray[globalId] = new CollisionTile(tType, col, row, 0, globalId);
                }
            });

            // 3. Convert the array to a dictionary in one pass
            // This avoids the expensive repeated 'Concat' operations
            MapDictionary = tileArray.ToDictionary(t => t.GlobalId, t => t);

            // Save the map
            ImageHelper.SaveMapDictionaryAsImage(MapDictionary, "GeneratedMap.png");
        }

        private void ToChunks()
        {   // Use ceiling to include partial sectors if map size isn't an exact multiple of chunk size
            var SectorsInX = (Global.MapWidth + Global.ChunkSize - 1) / Global.ChunkSize;
            var SectorsInY = (Global.MapHeight + Global.ChunkSize - 1) / Global.ChunkSize;
            var Chunks = new Dictionary<int, Chunk>();
            var blockCount = 1;
            var dictionaryCounter = 1;
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

                            if (!MapDictionary.TryGetValue(globalId, out var tile))
                                continue; // defensive: skip missing entries

                            bool isEdgeTile = localX == 0 || localX == Global.ChunkSize - 1 || localY == 0 || localY == Global.ChunkSize - 1;

                            // Update tile metadata
                            tile.ChunkId = dictionaryCounter;
                            tile.isEdgeTile = isEdgeTile;
                            tile.LocalId = localChunkCounter;
                            tile.GlobalId = globalId;

                            // store into chunk.Tiles using a local key/index
                            chunk.Tiles[globalId] = tile;
                            chunk.BackgroundTiles[globalId] = GenerateEmptyBackgroundTile(tile);

                            // also update the master map entry
                            MapDictionary[globalId].isEdgeTile = isEdgeTile;

                            blockCount++;
                            localChunkCounter++;
                        }
                    }
                    Chunks.Add(dictionaryCounter, chunk);
                    dictionaryCounter++;
                }
            }
            map.ChunkDictionary = Chunks;
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