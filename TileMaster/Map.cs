﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileMaster.Entity;
using TileMaster.Manager;
using TileMaster.Util;

namespace TileMaster
{
    public class Map
    {
        //The map dictionary used for map generation
        public Dictionary<int, CollisionTiles> MapDictionary { get; set; }
        //The chunk dictionary used for chunk storage
        public Dictionary<int, Chunk> ChunkDictionary { get; set; }
        //Tile types
        public List<CollisionTiles> Tiletypes { get; set; }
        //Tile colors (used for texture generation on the go)
        public List<TileColor> TileColors { get; set; }
        TileManager TileMgr { get; set; }

        public static int Progress;

        private int width, height;
        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        public Map()
        {
            ChunkDictionary = new Dictionary<int, Chunk>();
            TileMgr = new TileManager();
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
                    return GetTileAt(blockId, chunkId - (Global.MapWidth / Global.ChunkSize), "up", true);
                }
            }


            return null;


        }

        public bool CheckIfMapDataExists()
        {
            return File.Exists(Global.MapDataLocation);
        }

        /// <summary>
        /// Generate a dictionary map from a 2d integer array
        /// </summary>
        /// <param name="map"></param>
        public void GenerateMapDictionaryRetail(int[,] map)
        {
            Tiletypes = CollisionTiles.LoadTilesTypes();
            string createText = "";
            MapDictionary = new Dictionary<int, CollisionTiles>();
            //the global counter should always start at zero for proper tile calculation
            int globalCounter = 0;
            for (int x = 0; x < map.GetLength(1); x++)
            {
                for (int y = 0; y < map.GetLength(0); y++)
                {
                    int number = map[x, y];

                    createText += number + ",";
                    //X e Y are inverted
                    var tType = Tiletypes.FirstOrDefault(x => x.TileId == number);
                    MapDictionary.Add(globalCounter, new CollisionTiles(tType, x, y, 0, globalCounter));
                    globalCounter++;

                }
                createText += Environment.NewLine;
            }



            File.WriteAllText("map.csv", createText);
        }
        /// <summary>
        /// Generate a dictionary map from a 2d integer array using threads
        /// </summary>
        /// <param name="map"></param>
        public void GenerateMapDictionary(int[,] map)
        {
            MapDictionary = new Dictionary<int, CollisionTiles>();
            Tiletypes = CollisionTiles.LoadTilesTypes();
            //List< Dictionary<int, CollisionTiles> > dictList = new List<Dictionary<int, CollisionTiles>>();
            ConcurrentBag<Dictionary<int, CollisionTiles>> dictList = new ConcurrentBag<Dictionary<int, CollisionTiles>>();
            int multiplier = map.GetLength(1);
            var taskList = new List<Task>();

            foreach (var x in Enumerable.Range(0, multiplier))
            {
                var t = new Task(() =>
                {
                    var rowDict = GenRow(map, x, multiplier * x);
                    if (rowDict != null)
                    {
                        dictList.Add(rowDict);
                    }

                });
                taskList.Add(t);
                t.Start();
            }

            Task.WaitAll(taskList.ToArray());

            List<Dictionary<int, CollisionTiles>> dl = new List<Dictionary<int, CollisionTiles>>();
            foreach (var dict in dictList)
            {
                dl.AddRange(dictList);
                MapDictionary = MapDictionary.Concat(dict).ToDictionary(k => k.Key, v => v.Value);
            }

            MapDictionary = MapDictionary.OrderBy(x => x.Key).ToDictionary(k => k.Key, v => v.Value);

        }
        /// <summary>
        /// generates a row of blocks
        /// </summary>
        /// <param name="map"></param>
        /// <param name="startingX"></param>
        /// <param name="globalCounter"></param>
        /// <returns></returns>
        public Dictionary<int, CollisionTiles> GenRow(int[,] map, int startingX, int globalCounter)
        {
            Dictionary<int, CollisionTiles> dictMap = new Dictionary<int, CollisionTiles>();
            for (int y = 0; y < map.GetLength(0); y++)
            {
                int number = map[startingX, y];

                //X e Y are inverted
                var tType = Tiletypes.FirstOrDefault(x => x.TileId == number);
                dictMap.Add(globalCounter, new CollisionTiles(tType, startingX, y, 0, globalCounter));
                globalCounter++;

            }
            return dictMap;
        }
        /// <summary>
        /// Saves the map data into their respective files
        /// </summary>
        public void SaveMap()
        {
            if (Directory.Exists(Global.ChunkFolderLocation) == false)
            {
                Directory.CreateDirectory(Global.ChunkFolderLocation);
            }
            foreach (string sFile in Directory.GetFiles(@"Chunks\", "*.bin"))
            {
                File.Delete(sFile);
            }
            DictionaryHelper.Serialize(MapDictionary, File.Open(Global.MapDataLocation, FileMode.Create));
            ChunkSizer();
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
            Tiletypes = CollisionTiles.LoadTilesTypes();
            TileColors = TileHelper.GetTileColors(Global.TileColorDataLocation);
            TileMgr.Load(TileColors);
            if (Global.UseTileTextureRandomization)
            {
                Tiletypes = TileMgr.LoadTileTextures(Tiletypes);
            }

            //sw.Stop();
            //var time = sw.Elapsed.TotalSeconds;


            //upon load, tiles has no information about texture or other complex properties
            //because these can't be serialized
            this.MapDictionary = DictionaryHelper.DeSerialize<Dictionary<int, CollisionTiles>>(File.Open(Global.MapDataLocation, FileMode.Open));

            List<Tuple<int, string>> chunks = new List<Tuple<int, string>>();
            foreach (string file in Directory.GetFiles("Chunks/").Where(x => x.Contains("chunk") && x.Contains(".bin")))
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
            int chunkId = 1;
            int globalCounter = 0;
            //loads chunks files into dictionaries
            foreach (var file in chunks)
            {
                Chunk chunk = new Chunk();
                Dictionary<int, CollisionTiles> dict = DictionaryHelper.DeSerialize<Dictionary<int, CollisionTiles>>(File.Open(file.Item2, FileMode.Open));
                Progress = chunkId * 100 / (chunks.Count);
                //set the blocks ids, positions and textures
                for (int i = 0; i < dict.Count; i++)
                {
                    var chunkTile = dict.ElementAt(i).Value;
                    var globalId = dict[dict.ElementAt(i).Key].GlobalId;
                    dict[dict.ElementAt(i).Key] = new CollisionTiles(Tiletypes.FirstOrDefault(x => x.TileId == dict.ElementAt(i).Value.TileId), dict[dict.ElementAt(i).Key])
                    {
                        ChunkId = chunkId
                    };

                    //make global map aware of this information
                    MapDictionary[globalId].ChunkId = dict[dict.ElementAt(i).Key].ChunkId;
                    MapDictionary[globalId].LocalId = dict[dict.ElementAt(i).Key].LocalId;

                    globalCounter++;

                }

                if (dict.Values.Any(x => x.TileId == ((int)TileType.DirtWithGrass)))
                {
                    chunk.HasGrass = true;
                    chunk.NeedGrassUpdate = true;
                }

                chunk.Tiles = dict;
                chunk.PositionOnscreen = chunkId;
                ChunkDictionary.Add(chunkId, chunk);
                chunkId++;
            }

            width = Global.MapWidth * Global.TileSize;
            height = Global.MapHeight * Global.TileSize;

            Global.isMapLoaded = true;
        }
        /// <summary>
        /// Turn a map dictionary into smaller dictionaries for better management
        /// </summary>
        public void ChunkSizer()
        {
            //divides the map into chunks of x size
            Dictionary<int, Chunk> Chunks = new Dictionary<int, Chunk>();
            int blockCount = 1;

            int SectorsInX = Global.MapWidth / Global.ChunkSize;
            int SectorsInY = Global.MapHeight / Global.ChunkSize;
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
            int tempX = 0;
            int tempY = 0;
            int gridCounter = 1;
            int dictionaryCounter = 1;
            int pointOnscreenCounter = 0;
            int rowMultiplier = 0;
            for (int gridX = 0; gridX < SectorsInX; gridX++)
            {
                for (int gridY = 0; gridY < SectorsInY; gridY++)
                {
                    Chunk chunk = new Chunk();
                    int localChunkCounter = 0;
                    chunk.PositionOnscreen = pointOnscreenCounter;
                    pointOnscreenCounter++;
                    tempX += rowMultiplier;
                    for (int x = 0; x < Global.ChunkSize; x++)
                    {
                        for (int y = 0; y < Global.ChunkSize; y++)
                        {
                            bool isEdgeTile;
                            if ((x == 0 || x == Global.ChunkSize - 1) || (y == 0 || y == Global.ChunkSize - 1))
                            {
                                isEdgeTile = true;
                            }
                            else
                            {
                                isEdgeTile = false;
                            }

                            chunk.Tiles[tempX] = MapDictionary[tempX];
                            chunk.Tiles[tempX].ChunkId = dictionaryCounter;
                            chunk.Tiles[tempX].isEdgeTile = isEdgeTile;
                            MapDictionary[tempX].isEdgeTile = isEdgeTile;
                            chunk.Tiles[tempX].LocalId = localChunkCounter;
                            chunk.Tiles[tempX].GlobalId = MapDictionary[tempX].GlobalId;

                            blockCount++;
                            localChunkCounter++;
                            gridCounter++;
                            tempX++;
                        }

                        //sends the x to the next line, ignoring the other blocks on other chunks
                        tempX = ((Global.MapWidth) * (x + 1) + (Global.ChunkSize * gridY) + rowMultiplier);
                    }

                    tempX = (Global.ChunkSize * (gridY + 1));

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



            foreach (var chunk in Chunks.Values)
            {
                foreach (var tile in chunk.Tiles.Where(x => x.Value.isEdgeTile))
                {
                    var neibs = GetNeighboringTiles(tile.Value);
                    tile.Value.neighboringTiles = neibs.Select(x => new KeyValuePair<int, int>(x.ChunkId, x.LocalId)).ToList();
                }
            }



            int iii = 0;
            foreach (var item in Chunks.Values)
            {
                DictionaryHelper.Serialize(item.Tiles, File.Open(@"Chunks\chunk" + iii + ".bin", FileMode.Create));
                iii++;
            }
        }
        /// <summary>
        /// places a block at a given location
        /// </summary>
        /// <param name="tileId"></param>
        /// <param name="mouseIsOverBlock"></param>
        /// <param name="chunkId"></param>
        public void PlaceBlockAt(int tileId, int mouseIsOverBlock, int chunkId)
        {
            SetTile(mouseIsOverBlock, tileId, chunkId);
            //adding a block
            CheckNeighboringGrassBlocks(chunkId, mouseIsOverBlock);
        }
        public void SetTile(Tile tile, int tileId)
        {
            SetTile(tile.GlobalId, tileId, tile.ChunkId);
        }
        public void SetTile(int blockId, int tileId, int chunkId, Texture2D texture)
        {
            var game = Game.GetInstance();

            //TODO: check this as this verification is not needed
            if (IsBlockOnChunk(chunkId, blockId))
            {
                //var tile = ChunkDictionary[chunkId].Tiles[blockId];
                ChunkDictionary[chunkId].Tiles[blockId].texture = texture;
                ChunkDictionary[chunkId].Tiles[blockId].Name = ((TileType)tileId).ToString();
                ChunkDictionary[chunkId].Tiles[blockId].TileId = tileId;
                game.LogMessage("Setting block " + blockId + " on chunk " + chunkId + " to " + (TileType)tileId, Color.Green);
            }
            else
            {
                game.LogMessage("Block " + blockId + " was not present on chunk  " + chunkId, Color.Green);
            }
        }
        public void SetTileAsGrass(int blockId, int tileId, int chunkId, Texture2D texture)
        {
            SetTile(blockId, tileId, chunkId, texture);
            ChunkDictionary[chunkId].HasGrass = true;
            ChunkDictionary[chunkId].NeedGrassUpdate = true;
        }
        public void SetTile(int blockId, int tileId, int chunkId)
        {
            var game = Game.GetInstance();
            //this verification isn't needed
            if (IsBlockOnChunk(chunkId, blockId))
            {

                var refTile = Tiletypes.FirstOrDefault(x => x.TileId == tileId);
                if (refTile.AlternateTextures.Any())
                {
                    ChunkDictionary[chunkId].Tiles[blockId].texture = refTile.AltTextures[Game.rnd.Next(refTile.AltTextures.Count)];

                }
                else
                {
                    ChunkDictionary[chunkId].Tiles[blockId].texture = refTile.texture;
                }

                ChunkDictionary[chunkId].Tiles[blockId].Name = ((TileType)tileId).ToString();
                ChunkDictionary[chunkId].Tiles[blockId].TileId = tileId;
                ChunkDictionary[chunkId].Tiles[blockId].IsOccupied = refTile.IsOccupied;
                ChunkDictionary[chunkId].Tiles[blockId].IsSolid = refTile.IsSolid;
                ChunkDictionary[chunkId].NeedGrassUpdate = true;

                MapDictionary[blockId].Name = ChunkDictionary[chunkId].Tiles[blockId].Name;
                MapDictionary[blockId].TileId = ChunkDictionary[chunkId].Tiles[blockId].TileId;
                MapDictionary[blockId].IsOccupied = ChunkDictionary[chunkId].Tiles[blockId].IsOccupied;
                MapDictionary[blockId].IsSolid = ChunkDictionary[chunkId].Tiles[blockId].IsSolid;

                game.LogMessage("Setting block " + blockId + " on chunk " + chunkId + " to " + (TileType)tileId, Color.Green);
            }
            else
            {
                game.LogMessage("Block " + blockId + " was not present on chunk  " + chunkId, Color.Green);
            }
        }

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
            int chunksOnTheScreenHorizontally = 2;//((((Global.WindowWidth) / (Global.ChunkSize * Global.TileSize))) - 1);
            int chunksOnTheScreenVertically = 2;// (((Global.WindowHeight) / (Global.ChunkSize * Global.TileSize)));

            //used to access upper and lower row chunks
            int rowMultiplier = Global.MapWidth / Global.ChunkSize;

            List<Tile> tiles = new List<Tile>();
            List<int> CTD = new List<int>();
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
            //tiles.AddRange(ChunkDictionary[referenceChunk].Tiles.Values);
            return tiles;


            //if (Global.RenderOnlyPlayerAtChunk == false)
            //{

            //    int[] chunksToDraw = new int[14] {
            //        referenceChunk+1,
            //        referenceChunk+2,
            //        referenceChunk-1,
            //        referenceChunk-2,

            //        referenceChunk+rowMultiplier,
            //        referenceChunk+rowMultiplier+1,
            //        referenceChunk+rowMultiplier+2,
            //        referenceChunk+rowMultiplier-1,
            //        referenceChunk+rowMultiplier-2,

            //        referenceChunk-rowMultiplier,
            //        referenceChunk-rowMultiplier+1,
            //        referenceChunk-rowMultiplier+2,
            //        referenceChunk-rowMultiplier-1,
            //        referenceChunk-rowMultiplier-2,
            //    };

            //    for (int i = 0; i < chunksToDraw.Length; i++)
            //    {
            //        if (IsChunkPresent(chunksToDraw[i]))
            //        {
            //            tiles.AddRange(ChunkDictionary[chunksToDraw[i]].Tiles.Values);
            //        }

            //    }
            //    tiles.AddRange(ChunkDictionary[referenceChunk].Tiles.Values);
            //    return tiles;
            //}
            //else
            //{
            //    tiles.AddRange(ChunkDictionary[referenceChunk].Tiles.Values);
            //    return tiles;
            //}

        }

        #region Grass Logic
        /// <summary>
        /// Gets all surrounding tiles and check whether they can have grass grown onto them
        /// </summary>
        /// <param name="chunkId"></param>
        public void GrowGrass(int chunkId)
        {
            bool hasChanged = false;
            foreach (var tile in ChunkDictionary[chunkId].Tiles.Where(x => x.Value.TileId == (int)TileType.DirtWithGrass).ToList())
            {
                //checks if the neighboring block is dirt and if it has air above it so it can grow grass
                var neighbors = GetNeighboringTiles(tile.Value);

                //[x]<-[]
                hasChanged = CheckTileEligibilityForGrass(neighbors[3]);
                //[]->[x]
                hasChanged = CheckTileEligibilityForGrass(neighbors[5]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                hasChanged = CheckTileEligibilityForGrass(neighbors[8]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[7]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[6]);
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[2]);
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[1]);
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[0]);
            }
            //need to check whether the chunk has changed or not
            if (hasChanged)
            {
                ChunkDictionary[chunkId].NeedGrassUpdate = hasChanged;
            }
        }

        /// <summary>
        /// Creates a random tree
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        public void GrowTree(int chunkId, int blockId)
        {
            try
            {
                int treeBase = ChunkDictionary[chunkId].Tiles[blockId + 3].GlobalId;
                int trunkHeight = Game.rnd.Next(5, 10);
                int accumulator = 0;

                //tree shaft
                for (int i = 0; i < trunkHeight; i++)
                {
                    SetTile(treeBase - accumulator, (int)TileType.TreeTrunk, chunkId);
                    accumulator += Global.MapWidth;
                }
                int leaveSpread = Game.rnd.Next(5, 10);

                //the uppermost point of the tree
                int reference = (((treeBase - accumulator) - (leaveSpread * Global.MapWidth) - leaveSpread / 2));


                //tree leaves

                //length
                for (int i = 0; i < leaveSpread; i++)
                {
                    //width
                    for (int j = 0; j < leaveSpread + (leaveSpread / 2); j++)
                    {
                        SetTile(reference + (j * Global.MapWidth) + (i), (int)TileType.TreeLeaf, chunkId);
                    }
                }
            }
            catch
            {
                //the game may fail at finding the spot to generate a tree 
                //because of chunk boundaries
                //not grow a tre :'(
            }


        }

        /// <summary>
        /// Checks whether a tile can have grass
        /// </summary>
        /// <param name="destTile"></param>
        /// <returns></returns>
        private bool CheckTileEligibilityForGrass(Tile destTile)
        {
            if (destTile.TileId == (int)TileType.Dirt)
            {
                return SetGrassTile(destTile);
            }
            return false;
        }
        /// <summary>
        /// Set a dirt block into dirt with grass
        /// </summary>
        /// <param name="destinationTile"></param>
        /// <returns></returns>
        private bool SetGrassTile(Tile destinationTile)
        {
            bool top = false;
            bool bottom = false;
            bool left = false;
            bool right = false;

            var neighbors = GetNeighboringTiles(destinationTile);

            bool hasChanged = true;

            //grass can be planted
            //check neighboring tiles to define where on the tile grass will grow
            if (neighbors[1].TileId == (int)TileType.Air)
            {
                top = true;
            }
            if (neighbors[3].TileId == (int)TileType.Air)
            {
                left = true;
            }
            if (neighbors[5].TileId == (int)TileType.Air)
            {
                right = true;
            }
            if (neighbors[7].TileId == (int)TileType.Air)
            {
                bottom = true;
            }


            if (top == false && bottom == false && left == false && right == false)
            {
                //0000
                //no dice
                if (destinationTile.TileId != (int)TileType.Dirt)
                {
                    SetTile(destinationTile.GlobalId, (int)TileType.Dirt, destinationTile.ChunkId);
                }
                hasChanged = false;
            }
            else if (top == false && bottom == false && left == false && right)
            {
                //0001
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile9"));
            }
            else if (top == false && bottom == false && left && right == false)
            {
                //0010
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile10"));
            }
            else if (top == false && bottom == false && left && right)
            {
                //0011
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile11"));
            }
            else if (top == false && bottom && left == false && right == false)
            {
                //0100
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile8"));
            }
            else if (top == false && bottom && left == false && right)
            {
                //0101
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile12"));
            }
            else if (top == false && bottom && left && right == false)
            {
                //0110
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile13"));
            }
            else if (top == false && bottom && left && right)
            {
                //0111
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile14"));
            }
            else if (top && bottom == false && left == false && right == false)
            {
                //1000
                SetTile(destinationTile.GlobalId, (int)TileType.DirtWithGrass, destinationTile.ChunkId);
                ChunkDictionary[destinationTile.ChunkId].HasGrass = true;
            }
            else if (top && bottom == false && left == false && right)
            {
                //1001
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile4"));
            }
            else if (top && bottom == false && left && right == false)
            {
                //1010
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile5"));
            }
            else if (top && bottom == false && left && right)
            {
                //1011
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile6"));
            }
            else if (top && bottom && left == false && right == false)
            {
                //1100
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile15"));
            }
            else if (top && bottom && left == false && right)
            {
                //1101
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile16"));
            }
            else if (top && bottom && left && right == false)
            {
                //1110
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile17"));
            }
            else
            {
                //1111
                SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                Tiletypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile7"));
            }
            return hasChanged;
        }
        /// <summary>
        /// Verifies if neighboring tiles need to have its texture changed
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        public void CheckNeighboringGrassBlocks(int chunkId, int blockId)
        {
            //verifies if neighboring grass blocks needs to have its texture updated

            var neighbors = GetNeighboringTiles(ChunkDictionary[chunkId].Tiles[blockId]);
            if (neighbors[7].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[7]);
            }
            if (neighbors[5].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[5]);
            }
            if (neighbors[3].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[3]);
            }
            if (neighbors[1].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[1]);
            }

        }

        /// <summary>
        /// retrieves the neighboring tiles from a given tile
        /// </summary>
        /// <param name="refTile"></param>
        /// <returns></returns>
        private List<Tile> GetNeighboringTiles(Tile refTile)
        {
            if (refTile.isEdgeTile)
            {
                return GetNeighboringTilesCrossChunk(refTile);
            }
            else
            {
                return GetNeighboringTilesFromSameChunk(refTile);
            }
        }
        private List<Tile> GetNeighboringTilesFromSameChunk(Tile refTile)
        {
            List<Tile> neighbors = new List<Tile>
            {
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize - 1).Value,
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize).Value,
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize + 1).Value,
                //[ ][ ][ ]
                //[x][x][ ]
                //[ ][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                refTile,
                //[ ][ ][ ]
                //[ ][x][x]
                //[ ][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize + 1).Value
            };

            return neighbors;
        }
        private List<Tile> GetNeighboringTilesCrossChunk(Tile refTile)
        {
            List<Tile> neighbors = new List<Tile>
            {
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth - 1).Value,
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth).Value,
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth + 1).Value,
                //[ ][ ][ ]
                //[x][x][ ]
                //[ ][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                refTile,
                //[ ][ ][ ]
                //[ ][x][x]
                //[ ][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId + 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth + 1).Value
            };

            return neighbors;
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
                        tile.Color = "Gold";
                    }
                }

                tile.Draw(spriteBatch);
            }

        }
    }

}