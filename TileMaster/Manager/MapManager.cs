using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TileMaster.Entity;

namespace TileMaster.Manager
{
    public class MapManager
    {
        private Map.Map map;
        public static int Progress;
        //The map dictionary used for map generation
        public Dictionary<int, CollisionTiles> MapDictionary { get; set; }
        public MapManager(Map.Map map)
        {
            this.map = map;
        }
        #region Map Generation
        /// <summary>
        /// Generate a dictionary map from a 2d integer array using threads
        /// </summary>
        /// <param name="mapMatrice"></param>
        public void GenerateMapDictionary(int[,] mapMatrice)
        {
            MapDictionary = new Dictionary<int, CollisionTiles>();
            map.TileTypes = CollisionTiles.LoadTilesTypes();
            var dictList = new ConcurrentBag<Dictionary<int, CollisionTiles>>();
            var multiplier = mapMatrice.GetLength(0/*x*/);
            var taskList = new List<Task>();

            foreach (var col in Enumerable.Range(0, multiplier))
            {
                var capturedCol = col;
                var t = new Task(() =>
                {
                    var rowDict = GenRow(mapMatrice, capturedCol, multiplier * capturedCol);
                    if (rowDict != null)
                    {
                        dictList.Add(rowDict);
                    }

                });
                taskList.Add(t);
                t.Start();
            }

            Task.WaitAll(taskList.ToArray());

            foreach (var dict in dictList)
            {
                MapDictionary = MapDictionary.Concat(dict).ToDictionary(k => k.Key, v => v.Value);
            }

            MapDictionary = MapDictionary.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);

            SaveMapDictionaryAsImage(MapDictionary, "GeneratedMap.png");
        }

        /// <summary>
        /// Save the current MapDictionary into a PNG image file.
        /// Each tile maps to a single pixel (x = column, y = row). The routine will:
        /// - Prefer a stored ColorArgb on the tile (if present).
        /// - Otherwise fall back to a quick TileType -> color mapping.
        /// </summary>
        /// <param name="fileName">Output file path (png recommended).</param>
        public void SaveMapDictionaryAsImage(Dictionary<int, CollisionTiles> MapDictionary, string fileName)
        {
            try
            {
                if (MapDictionary == null)
                    throw new InvalidOperationException("MapDictionary is null.");

                int width = Global.MapWidth;
                int height = Global.MapHeight;

                using (var bitmap = new Bitmap(width, height))
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            var globalId = y * width + x;
                            Color pixelColor = Color.White;

                            if (MapDictionary.TryGetValue(globalId, out var tile) && tile != null)
                            {
                                // Fallback mapping by TileId
                                switch ((TileType)tile.TileId)
                                {
                                    case TileType.Air:
                                        pixelColor = Color.White;
                                        break;
                                    case TileType.Dirt:
                                        pixelColor = Color.Brown;
                                        break;
                                    case TileType.Stone:
                                        pixelColor = Color.Gray;
                                        break;
                                    case TileType.DirtWithGrass:
                                        pixelColor = Color.Green;
                                        break;
                                    case TileType.Granite:
                                        pixelColor = Color.DarkRed;
                                        break;
                                    case TileType.TreeTrunk:
                                        pixelColor = Color.SaddleBrown;
                                        break;
                                    case TileType.TreeLeaf:
                                        pixelColor = Color.FromArgb(255, 34, 139, 34); // forest green
                                        break;
                                    default:
                                        pixelColor = Color.LightGray;
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
                // Swallowing exceptions is not ideal, but keeps the editor/game stable.
                // If needed, replace with a logging call (Game.LogMessage) or rethrow.
                try
                {
                    Game.LogMessage($"SaveMapDictionaryAsImage failed: {ex.Message}", Microsoft.Xna.Framework.Color.Red);
                }
                catch { }
            }
        }

        /// <summary>
        /// generates a column of blocks (one x across all y)
        /// </summary>
        /// <param name="mapMatrice"></param>
        /// <param name="startingX">column (x) index</param>
        /// <param name="globalCounter"></param>
        /// <returns></returns>
        public Dictionary<int, CollisionTiles> GenRow(int[,] mapMatrice, int startingX, int globalCounter)
        {
            var dictMap = new Dictionary<int, CollisionTiles>();

            int width = mapMatrice.GetLength(0); // number of columns (x)
            int height = mapMatrice.GetLength(1); // number of rows (y)

            for (var y = 0; y < height; y++)
            {
                var number = mapMatrice[startingX, y];
                var tType = map.TileTypes.FirstOrDefault(tt => tt.TileId == number);

                // Use row-major indexing: globalId = y * width + x
                var globalId = y * width + startingX;

                dictMap.Add(globalId, new CollisionTiles(tType, startingX, y, 0, globalId));
            }
            return dictMap;
        }
        #endregion

        #region Map Loading
        /// <summary>
        /// Saves the map data into their respective files
        /// </summary>
        public void SaveMap()
        {
            //if (map.MapDictionary != null)
            {
                if (Directory.Exists(Global.ChunkFolderLocation) == false)
                {
                    Directory.CreateDirectory(Global.ChunkFolderLocation);
                }

                // remove legacy per-chunk files
                foreach (var sFile in Directory.GetFiles(Global.ChunkFolderLocation, "*.bin"))
                {
                    File.Delete(sFile);
                }

                // remove existing single-archive if present
                var archivePath = Path.Combine(Global.ChunkFolderLocation, "map.tlm");
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }

                // create new archive containing all chunks
                ChunkSizer(archivePath);
            }
        }

        /// <summary>
        /// Loads a map from a binary source
        /// </summary>
        /// <param name="content"></param>
        public void LoadMap()
        {
            //load tile data (namely colors) so we can build tiles at runtime
            map.TileTypes = CollisionTiles.LoadTilesTypes();
            if (Global.UseTileTextureRandomization)
            {
                map.TileTypes = map.TileMgr.LoadTileTextures(map.TileTypes);
            }

            // Prefer reading single archive if present
            var archivePath = Path.Combine(Global.ChunkFolderLocation, "map.tlm");
            var chunks = new List<Tuple<int, string>>();

            if (File.Exists(archivePath))
            {
                using (var fs = File.OpenRead(archivePath))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Expect entry names like "chunk{n}.json" or similar
                        if (entry.Name.StartsWith("chunk", StringComparison.OrdinalIgnoreCase))
                        {
                            // try to parse the number part
                            var name = entry.Name;
                            var numPart = name.Replace("chunk", "").Replace(".json", "").Replace(".bin", "");
                            if (int.TryParse(numPart, out var id))
                            {
                                chunks.Add(new Tuple<int, string>(id, name));
                            }
                        }
                    }

                    if (chunks.Count == 0)
                    {
                        // archive empty or unexpected -> regenerate
                        ChunkSizer(archivePath);
                    }
                    else
                    {
                        var chunkId = 1;

                        foreach (var file in chunks)
                        {
                            var chunk = new Chunk();
                            var entry = archive.GetEntry(file.Item2);
                            if (entry == null) continue;

                            var options = new JsonSerializerOptions { IncludeFields = true };
                            Dictionary<int, CollisionTiles> dict = null;
                            using (var entryStream = entry.Open())
                            {
                                dict = JsonSerializer.Deserialize<Dictionary<int, CollisionTiles>>(entryStream, options);
                            }

                            if (dict == null) continue;

                            Progress = chunkId * 100 / chunks.Count;
                            //set the blocks ids, positions and textures
                            for (var i = 0; i < dict.Count; i++)
                            {
                                var chunkTile = dict.ElementAt(i).Value;
                                var globalId = dict[dict.ElementAt(i).Key].GlobalId;
                                dict[dict.ElementAt(i).Key] = new CollisionTiles(map.TileTypes.FirstOrDefault(x => x.Name == chunkTile.Name), dict[dict.ElementAt(i).Key])
                                {
                                    ChunkId = chunkId
                                };

                                globalId++; // keep same progression as before
                            }

                            if (dict.Values.Any(x => x.TileId == (int)TileType.DirtWithGrass))
                            {
                                chunk.HasGrass = true;
                                chunk.NeedUpdate = true;
                            }

                            chunk.Tiles = dict;
                            chunk.PositionOnscreen = chunkId;                      
                            map.ChunkDictionary.Add(chunkId, chunk);
                            chunkId++;
                        }

                        map.Width = Global.MapWidth * Global.TileSize;
                        map.Height = Global.MapHeight * Global.TileSize;

                        Global.IsMapLoaded = true;
                        return;
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Turn a map dictionary into smaller dictionaries for better management
        /// </summary>
        public void ChunkSizer_(string archivePath = null)
        {
            //divides the map into chunks of x size
            var Chunks = new Dictionary<int, Chunk>();
            var blockCount = 1;

            var SectorsInX = Global.MapWidth / Global.ChunkSize;
            var SectorsInY = Global.MapHeight / Global.ChunkSize;
            var tempX = 0;
            var tempY = 0;
            var gridCounter = 1;
            var dictionaryCounter = 1;
            var pointOnscreenCounter = 0;
            var rowMultiplier = 0;
            for (var gridX = 0; gridX < SectorsInX; gridX++)
            {
                for (var gridY = 0; gridY < SectorsInY; gridY++)
                {
                    var chunk = new Chunk();
                    var localChunkCounter = 0;
                    chunk.PositionOnscreen = pointOnscreenCounter;
                    pointOnscreenCounter++;
                    tempX += rowMultiplier;
                    for (var x = 0; x < Global.ChunkSize; x++)
                    {
                        for (var y = 0; y < Global.ChunkSize; y++)
                        {
                            bool isEdgeTile;
                            if (x == 0 || x == Global.ChunkSize - 1 || y == 0 || y == Global.ChunkSize - 1)
                            {
                                isEdgeTile = true;
                            }
                            else
                            {
                                isEdgeTile = false;
                            }

                            //chunk.Tiles[tempX] = map.MapDictionary[tempX];
                            chunk.Tiles[tempX].ChunkId = dictionaryCounter;
                            chunk.Tiles[tempX].isEdgeTile = isEdgeTile;
                            //map.MapDictionary[tempX].isEdgeTile = isEdgeTile;
                            chunk.Tiles[tempX].LocalId = localChunkCounter;
                            //chunk.Tiles[tempX].GlobalId = map.MapDictionary[tempX].GlobalId;

                            blockCount++;
                            localChunkCounter++;
                            gridCounter++;
                            tempX++;
                        }

                        //sends the x to the next line, ignoring the other blocks on other chunks
                        tempX = Global.MapWidth * (x + 1) + Global.ChunkSize * gridY + rowMultiplier;
                    }

                    tempX = Global.ChunkSize * (gridY + 1);

                    tempY += Global.MapHeight;
                    Chunks.Add(dictionaryCounter, chunk);
                    dictionaryCounter++;
                }
                tempX = 0;
                rowMultiplier = gridCounter - 1;
            }

            // If archivePath provided, write all chunks to a single ZIP archive (.tlm)
            if (!string.IsNullOrEmpty(archivePath))
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = false
                };

                using (var fs = File.Open(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    var iii = 0;
                    foreach (var item in Chunks.Values)
                    {
                        var entry = archive.CreateEntry($"chunk{iii}.json", CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            var bytes = JsonSerializer.SerializeToUtf8Bytes(item.Tiles, options);
                            entryStream.Write(bytes, 0, bytes.Length);
                        }
                        iii++;
                    }
                }

                // free the big map dictionary to reduce memory
                MapDictionary = null;
                return;
            }
        }
        public void ChunkSizer__(string archivePath = null)
        {
            var Chunks = new Dictionary<int, Chunk>();
            var blockCount = 1;

            // Use ceiling to include partial sectors if map size isn't an exact multiple of chunk size
            var SectorsInX = (Global.MapWidth + Global.ChunkSize - 1) / Global.ChunkSize;
            var SectorsInY = (Global.MapHeight + Global.ChunkSize - 1) / Global.ChunkSize;

            var dictionaryCounter = 1;
            var pointOnscreenCounter = 0;
            for (var gridX = 0; gridX < SectorsInX; gridX++)
            {
                for (var gridY = 0; gridY < SectorsInY; gridY++)
                {

                    var chunk = new Chunk();
                    chunk.PositionOnscreen = pointOnscreenCounter++;
                    var localChunkCounter = 0;

                    // iterate local coords inside the chunk
                    for (var localX = 0; localX < Global.ChunkSize; localX++)
                    {
                        var globalX = gridX * Global.ChunkSize + localX;
                        if (globalX >= Global.MapWidth) break; // outside map columns

                        for (var localY = 0; localY < Global.ChunkSize; localY++)
                        {
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
                            chunk.Tiles[localChunkCounter] = tile;

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

            // If archivePath provided, write all chunks to a single ZIP archive (.tlm)
            if (!string.IsNullOrEmpty(archivePath))
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = false
                };

                using (var fs = File.Open(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    var iii = 0;
                    foreach (var item in Chunks.Values)
                    {
                        var entry = archive.CreateEntry($"chunk{iii}.json", CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            var bytes = JsonSerializer.SerializeToUtf8Bytes(item.Tiles, options);
                            entryStream.Write(bytes, 0, bytes.Length);
                        }
                        iii++;
                    }
                }

                // free the big map dictionary to reduce memory
                MapDictionary = null;
                return;
            }
        }
        public void ChunkSizer(string archivePath = null)
        {
            var Chunks = new Dictionary<int, Chunk>();
            var blockCount = 1;

            // Use ceiling to include partial sectors if map size isn't an exact multiple of chunk size
            var SectorsInX = (Global.MapWidth + Global.ChunkSize - 1) / Global.ChunkSize;
            var SectorsInY = (Global.MapHeight + Global.ChunkSize - 1) / Global.ChunkSize;

            var dictionaryCounter = 1;
            var pointOnscreenCounter = 0;

            // Iterate chunks row-major: scan chunk rows (gridY) then chunk columns (gridX)
            for (var gridY = 0; gridY < SectorsInY; gridY++)
            {
                for (var gridX = 0; gridX < SectorsInX; gridX++)
                {
                    var chunk = new Chunk();
                    chunk.PositionOnscreen = pointOnscreenCounter++;
                    var localChunkCounter = 0;

                    // Iterate local coords inside the chunk in row-major order:
                    // localY outer, localX inner so tiles are stored scanning each row of the chunk left-to-right
                    for (var localY = 0; localY < Global.ChunkSize; localY++)
                    {
                        var globalY = gridY * Global.ChunkSize + localY;
                        if (globalY >= Global.MapHeight) break; // outside map rows

                        for (var localX = 0; localX < Global.ChunkSize; localX++)
                        {
                            var globalX = gridX * Global.ChunkSize + localX;
                            if (globalX >= Global.MapWidth) break; // outside map columns

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

                            // store into chunk.Tiles using a local key/index (row-major inside chunk)
                            chunk.Tiles[globalId] = tile;

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

            // If archivePath provided, write all chunks to a single ZIP archive (.tlm)
            if (!string.IsNullOrEmpty(archivePath))
            {
                var options = new JsonSerializerOptions
                {
                    IncludeFields = true,
                    WriteIndented = false
                };

                using (var fs = File.Open(archivePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
                {
                    var iii = 0;
                    foreach (var item in Chunks.Values)
                    {
                        var entry = archive.CreateEntry($"chunk{iii}.json", CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        {
                            var bytes = JsonSerializer.SerializeToUtf8Bytes(item.Tiles, options);
                            entryStream.Write(bytes, 0, bytes.Length);
                        }
                        iii++;
                    }
                }

                // free the big map dictionary to reduce memory
                MapDictionary = null;
                return;
            }
        }
    }
}