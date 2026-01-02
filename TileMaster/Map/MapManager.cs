using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TileMaster.Entity;
using TileMaster.Util;

namespace TileMaster.Map
{
    public class MapManager
    {
        private Map map;
        public static int Progress;
        public MapManager(Map map)
        {
            this.map = map;
        }
        #region Map Generation
        /// <summary>
        /// Generate a dictionary map from a 2d integer array
        /// </summary>
        /// <param name="mapMatrice"></param>
        public void GenerateMapDictionaryRetail(int[,] mapMatrice)
        {
            map.TileTypes = CollisionTiles.LoadTilesTypes();
            var createText = "";
            map.MapDictionary = new Dictionary<int, CollisionTiles>();
            //the global counter should always start at zero for proper tile calculation
            var globalCounter = 0;
            for (var x = 0; x < mapMatrice.GetLength(1); x++)
            {
                for (var y = 0; y < mapMatrice.GetLength(0); y++)
                {
                    var number = mapMatrice[x, y];

                    createText += number + ",";
                    //X e Y are inverted
                    var tType = map.TileTypes.FirstOrDefault(x => x.TileId == number);
                    map.MapDictionary.Add(globalCounter, new CollisionTiles(tType, x, y, 0, globalCounter));
                    globalCounter++;

                }
                createText += Environment.NewLine;
            }
            File.WriteAllText("map.csv", createText);
        }

        /// <summary>
        /// Generate a dictionary map from a 2d integer array using threads
        /// </summary>
        /// <param name="mapMatrice"></param>
        public void GenerateMapDictionary(int[,] mapMatrice)
        {
            map.MapDictionary = new Dictionary<int, CollisionTiles>();
            map.TileTypes = CollisionTiles.LoadTilesTypes();
            //List< Dictionary<int, CollisionTiles> > dictList = new List<Dictionary<int, CollisionTiles>>();
            var dictList = new ConcurrentBag<Dictionary<int, CollisionTiles>>();
            var multiplier = mapMatrice.GetLength(1);
            var taskList = new List<Task>();

            foreach (var x in Enumerable.Range(0, multiplier))
            {
                var t = new Task(() =>
                {
                    var rowDict = GenRow(mapMatrice, x, multiplier * x);
                    if (rowDict != null)
                    {
                        dictList.Add(rowDict);
                    }

                });
                taskList.Add(t);
                t.Start();
            }

            Task.WaitAll(taskList.ToArray());

            var dl = new List<Dictionary<int, CollisionTiles>>();
            foreach (var dict in dictList)
            {
                dl.AddRange(dictList);
                map.MapDictionary = map.MapDictionary.Concat(dict).ToDictionary(k => k.Key, v => v.Value);
            }

            map.MapDictionary = map.MapDictionary.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);

        }

        /// <summary>
        /// generates a row of blocks
        /// </summary>
        /// <param name="mapMatrice"></param>
        /// <param name="startingX"></param>
        /// <param name="globalCounter"></param>
        /// <returns></returns>
        public Dictionary<int, CollisionTiles> GenRow(int[,] mapMatrice, int startingX, int globalCounter)
        {
            var dictMap = new Dictionary<int, CollisionTiles>();
            for (var y = 0; y < mapMatrice.GetLength(0); y++)
            {
                var number = mapMatrice[startingX, y];
                //X e Y are inverted
                var tType = map.TileTypes.FirstOrDefault(x => x.TileId == number);
                dictMap.Add(globalCounter, new CollisionTiles(tType, startingX, y, 0, globalCounter));
                globalCounter++;

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
            if (map.MapDictionary != null)
            {
                if (Directory.Exists(Global.ChunkFolderLocation) == false)
                {
                    Directory.CreateDirectory(Global.ChunkFolderLocation);
                }
                foreach (var sFile in Directory.GetFiles(@"Chunks\", "*.bin"))
                {
                    File.Delete(sFile);
                }
                //delete
                var tile = map.MapDictionary.FirstOrDefault(x => x.Value.GlobalId == 44337);
                DictionaryHelper.Serialize(map.MapDictionary, File.Open(Global.MapDataLocation, FileMode.Create));
                ChunkSizer();
            }
        }

        /// <summary>
        /// Loads a map from a binary source
        /// </summary>
        /// <param name="content"></param>
        public void LoadMap()
        {
            //var sw = new Stopwatch();
            //sw.Start(); 
            //load tile data (namely colors) so we can build tiles at runtime
            map.TileTypes = CollisionTiles.LoadTilesTypes();
            map.TileColors = TileHelper.GetTileColors(Global.TileColorDataLocation);
            map.TileMgr.Load(map.TileColors);
            if (Global.UseTileTextureRandomization)
            {
                map.TileTypes = map.TileMgr.LoadTileTextures(map.TileTypes);
            }

            //sw.Stop();
            //var time = sw.Elapsed.TotalSeconds;


            //upon load, tiles has no information about texture or other complex properties
            //because these can't be serialized
            map.MapDictionary = DictionaryHelper.DeSerialize<Dictionary<int, CollisionTiles>>(File.Open(Global.MapDataLocation, FileMode.Open));


            var chunks = new List<Tuple<int, string>>();
            foreach (var file in Directory.GetFiles("Chunks/").Where(x => x.Contains("chunk") && x.Contains(".bin")))
            {
                chunks.Add(new Tuple<int, string>(int.Parse(file.Replace("Chunks/chunk", "").Replace(".bin", "")), file));
            }
            if (chunks.Count == 0)
            {
                //chunks are empty, needs regen
                ChunkSizer();
            }
            chunks.Sort((y, x) => y.Item1.CompareTo(x.Item1));

            //chunk never stat at zero
            var chunkId = 1;
            var globalCounter = 0;
            //loads chunks files into dictionaries
            foreach (var file in chunks)
            {
                var chunk = new Chunk();
                Dictionary<int, CollisionTiles> dict = DictionaryHelper.DeSerialize<Dictionary<int, CollisionTiles>>(File.Open(file.Item2, FileMode.Open));
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

                    //make global map aware of this information
                    map.MapDictionary[globalId].ChunkId = dict[dict.ElementAt(i).Key].ChunkId;
                    map.MapDictionary[globalId].LocalId = dict[dict.ElementAt(i).Key].LocalId;

                    globalCounter++;

                }

                if (dict.Values.Any(x => x.TileId == (int)TileType.DirtWithGrass))
                {
                    chunk.HasGrass = true;
                    chunk.NeedGrassUpdate = true;
                }

                chunk.Tiles = dict;
                chunk.PositionOnscreen = chunkId;
                map.ChunkDictionary.Add(chunkId, chunk);
                chunkId++;
            }

            map.Width = Global.MapWidth * Global.TileSize;
            map.Height = Global.MapHeight * Global.TileSize;

            Global.IsMapLoaded = true;
        }
        #endregion

        /// <summary>
        /// Turn a map dictionary into smaller dictionaries for better management
        /// </summary>
        public void ChunkSizer()
        {
            //divides the map into chunks of x size
            var Chunks = new Dictionary<int, Chunk>();
            var blockCount = 1;

            var SectorsInX = Global.MapWidth / Global.ChunkSize;
            var SectorsInY = Global.MapHeight / Global.ChunkSize;
            //[x][x][x][x][x][x][x][x][x][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][ ][ ][ ][ ][ ][ ][ ][ ][x]
            //[x][x][x][x][x][x][x][x][x][x]
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

                            chunk.Tiles[tempX] = map.MapDictionary[tempX];
                            chunk.Tiles[tempX].ChunkId = dictionaryCounter;
                            chunk.Tiles[tempX].isEdgeTile = isEdgeTile;
                            map.MapDictionary[tempX].isEdgeTile = isEdgeTile;
                            chunk.Tiles[tempX].LocalId = localChunkCounter;
                            chunk.Tiles[tempX].GlobalId = map.MapDictionary[tempX].GlobalId;

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



            //var taskList = new List<Task>();

            //for (int i = 0; i < Chunks.Values.Count; i++)
            //{
            //    var t = new Task(() =>
            //    {
            //        foreach (var tile in Chunks[i].Tiles.Where(x => x.Value.isEdgeTile))
            //        {
            //            var neibs = GetNeighboringTiles(tile.Value);
            //            tile.Value.neighboringTiles = neibs.Select(x => new KeyValuePair<int, int>(x.ChunkId, x.LocalId)).ToList();
            //        }

            //    });
            //    taskList.Add(t);
            //    t.Start();

            //}


            //Task.WaitAll(taskList.ToArray());



            //foreach (var chunk in Chunks.Values)
            //{
            //    foreach (var tile in chunk.Tiles.Where(x => x.Value.isEdgeTile))
            //    {
            //        var neibs = GetNeighboringTiles(tile.Value);
            //        tile.Value.neighboringTiles = neibs.Select(x => new KeyValuePair<int, int>(x.ChunkId, x.LocalId)).ToList();
            //    }
            //}



            var iii = 0;
            foreach (var item in Chunks.Values)
            {
                DictionaryHelper.Serialize(item.Tiles, File.Open(@"Chunks\chunk" + iii + ".bin", FileMode.Create));
                iii++;
            }
        }
    }
}
