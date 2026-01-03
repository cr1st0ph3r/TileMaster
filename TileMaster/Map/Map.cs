using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileMaster.Entity;
using TileMaster.Manager;
using TileMaster.Util;

namespace TileMaster.Map
{
    public class Map
    {
        public TileInspector tileInspector;
        public GrassManager grass;
        public MapManager mapManager;
        //The map dictionary used for map generation
        public Dictionary<int, CollisionTiles> MapDictionary { get; set; }
        //The chunk dictionary used for chunk storage
        public Dictionary<int, Chunk> ChunkDictionary { get; set; }
        //Tile types
        public List<CollisionTiles> TileTypes { get; set; }
        //Tile colors (used for texture generation on the go)
        public List<TileColor> TileColors { get; set; }
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

            // Fallback to the global MapDictionary
            if (MapDictionary != null && MapDictionary.ContainsKey(globalId))
            {
                return MapDictionary[globalId];
            }

            return null;
        }

        public bool CheckIfMapDataExists()
        {
            return File.Exists(Global.MapDataLocation);
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
        }
        public void SetTile(int blockId, int tileId, int chunkId, Texture2D texture)
        {
            ChunkDictionary[chunkId].Tiles[blockId].texture = texture;
            ChunkDictionary[chunkId].Tiles[blockId].TextureName = texture.Name;
            ChunkDictionary[chunkId].Tiles[blockId].Name = ((TileType)tileId).ToString();
            ChunkDictionary[chunkId].Tiles[blockId].TileId = tileId;
     
            MapDictionary[blockId].texture = ChunkDictionary[chunkId].Tiles[blockId].texture;
            MapDictionary[blockId].TextureName = ChunkDictionary[chunkId].Tiles[blockId].TextureName;
            MapDictionary[blockId].Name = ChunkDictionary[chunkId].Tiles[blockId].Name;
            MapDictionary[blockId].TileId = ChunkDictionary[chunkId].Tiles[blockId].TileId;

            MapDictionary[blockId].IsOccupied = ChunkDictionary[chunkId].Tiles[blockId].IsOccupied;
            MapDictionary[blockId].IsSolid = ChunkDictionary[chunkId].Tiles[blockId].IsSolid;

            Game.LogMessage("Setting block " + blockId + " on chunk " + chunkId + " to " + (TileType)tileId, Color.Green);

        }
        public void SetTileAsGrass(int blockId, int tileId, int chunkId, Texture2D texture)
        {
            SetTile(blockId, tileId, chunkId, texture);
            ChunkDictionary[chunkId].HasGrass = true;
            ChunkDictionary[chunkId].NeedGrassUpdate = true;
        }


        public void SetTile(int blockId, int tileId, int chunkId)
        {
            //this verification isn't needed
            if (IsBlockOnChunk(chunkId, blockId))
            {
                var refTile = TileTypes.FirstOrDefault(x => x.TileId == tileId);
                if (refTile.AlternateTextures.Any())
                {
                    ChunkDictionary[chunkId].Tiles[blockId].texture = refTile.AltTextures[Game.rnd.Next(refTile.AltTextures.Count)];
                }
                else
                {
                    ChunkDictionary[chunkId].Tiles[blockId].texture = refTile.texture;
                }

                ChunkDictionary[chunkId].Tiles[blockId].Name = ((TileType)tileId).ToString();
                ChunkDictionary[chunkId].Tiles[blockId].TextureName = ChunkDictionary[chunkId].Tiles[blockId].texture.Name;
                ChunkDictionary[chunkId].Tiles[blockId].TileId = tileId;
                ChunkDictionary[chunkId].Tiles[blockId].IsOccupied = refTile.IsOccupied;
                ChunkDictionary[chunkId].Tiles[blockId].IsSolid = refTile.IsSolid;
                ChunkDictionary[chunkId].NeedGrassUpdate = true;

                MapDictionary[blockId].Name = ChunkDictionary[chunkId].Tiles[blockId].Name;
                MapDictionary[blockId].TextureName = ChunkDictionary[chunkId].Tiles[blockId].TextureName;
                MapDictionary[blockId].texture = ChunkDictionary[chunkId].Tiles[blockId].texture;
                MapDictionary[blockId].TileId = ChunkDictionary[chunkId].Tiles[blockId].TileId;
                MapDictionary[blockId].IsOccupied = ChunkDictionary[chunkId].Tiles[blockId].IsOccupied;
                MapDictionary[blockId].IsSolid = ChunkDictionary[chunkId].Tiles[blockId].IsSolid;

                Game.LogMessage("Setting block " + blockId + " on chunk " + chunkId + " to " + (TileType)tileId, Color.Green);
            }
            else
            {
                Game.LogMessage("Block " + blockId + " was not present on chunk  " + chunkId, Color.Green);
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
            var chunksOnTheScreenHorizontally = 2;//((((Global.WindowWidth) / (Global.ChunkSize * Global.TileSize))) - 1);
            var chunksOnTheScreenVertically = 2;// (((Global.WindowHeight) / (Global.ChunkSize * Global.TileSize)));

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
        /// Creates a random tree
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        public void GrowTree(int chunkId, int blockId)
        {
            try
            {
                var treeBase = ChunkDictionary[chunkId].Tiles[blockId + 3].GlobalId;
                var trunkHeight = Game.rnd.Next(5, 10);
                var accumulator = 0;

                //tree shaft
                for (var i = 0; i < trunkHeight; i++)
                {
                    SetTile(treeBase - accumulator, (int)TileType.TreeTrunk, chunkId);
                    accumulator += Global.MapWidth;
                }
                var leaveSpread = Game.rnd.Next(5, 10);

                //the uppermost point of the tree
                var reference = treeBase - accumulator - leaveSpread * Global.MapWidth - leaveSpread / 2;


                //tree leaves

                //length
                for (var i = 0; i < leaveSpread; i++)
                {
                    //width
                    for (var j = 0; j < leaveSpread + leaveSpread / 2; j++)
                    {
                        SetTile(reference + j * Global.MapWidth + i, (int)TileType.TreeLeaf, chunkId);
                    }
                }
            }
            catch
            {
                //the game may fail at finding the spot to generate a tree 
                //because of chunk boundaries
                //not grow a tree :'(
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
    }

}