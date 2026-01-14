using System;
using System.Collections.Generic;
using TileMaster.Entity.Tiles;

namespace TileMaster.Map
{
    [Serializable]
    public class Chunk
    {
        public int PositionOnscreen;
        public int FirstBlock;
        public int LastBlock;
        public bool HasGrass;
        public bool NeedUpdate;
        /// <summary>
        /// Indicates if the chunk has been modified since the last save
        /// </summary>
        public bool HasBeenModified;

        public CollisionTile[] Tiles;
        public BackgroundTile[] BackgroundTiles;

        public Chunk() 
        { 
            Tiles = new CollisionTile[Global.ChunkSize * Global.ChunkSize];
            BackgroundTiles = new BackgroundTile[Global.ChunkSize * Global.ChunkSize];
        }

        #region Auxiliary Methods
        public Dictionary<int, BaseTile> ToBaseTiles() { 
            var baseTiles = new Dictionary<int, BaseTile>();
            for (int i = 0; i < Tiles.Length; i++)
            {
                if(Tiles[i] != null)
                {
                    baseTiles[Tiles[i].GlobalId] = Tiles[i];
                }
            }          
            return baseTiles;
        }
        public Dictionary<int, BaseTile> ToBaseBGTiles() { 
            var baseTiles = new Dictionary<int, BaseTile>();           
            for (int i = 0; i < BackgroundTiles.Length; i++)
            {
                if (BackgroundTiles[i] != null)
                {
                    baseTiles[BackgroundTiles[i].GlobalId] = BackgroundTiles[i];
                }
            }
            return baseTiles;
        }

        public void SetRectangles()
        {
            foreach (var tile in Tiles)
            {
                if(tile != null)
                    tile.Rectangle = new Microsoft.Xna.Framework.Rectangle(tile.X * Global.TileSize, tile.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            }
            foreach (var bgTile in BackgroundTiles)
            {
                if(bgTile != null)
                    bgTile.Rectangle = new Microsoft.Xna.Framework.Rectangle(bgTile.X * Global.TileSize, bgTile.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            }
        }

        public void InitializeTextures()
        {
            foreach (var tile in Tiles)
            {
                if(tile != null)
                    tile.InitializeTexture();
            }
            foreach (var bgTile in BackgroundTiles)
            {
                 if(bgTile != null)
                    bgTile.InitializeTexture();
            }
        }
        #endregion
    }
}
