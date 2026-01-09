using System;
using System.Collections.Generic;

namespace TileMaster.Entity
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

        public Dictionary<int, CollisionTile> Tiles;
        public Dictionary<int, BackgroundTile> BackgroundTiles;

        public Chunk() 
        { 
            Tiles = new Dictionary<int, CollisionTile>();
            BackgroundTiles = new Dictionary<int, BackgroundTile>();
        }

        #region Auxiliary Methods
        public Dictionary<int, BaseTile> ToBaseTiles() { 
            var baseTiles = new Dictionary<int, BaseTile>();
            foreach (var tile in Tiles)
            {
                baseTiles[tile.Key] = tile.Value;
            }          
            return baseTiles;
        }
        public Dictionary<int, BaseTile> ToBaseBGTiles() { 
            var baseTiles = new Dictionary<int, BaseTile>();           
            foreach (var bgTile in BackgroundTiles)
            {
                baseTiles[bgTile.Key] = bgTile.Value;
            }
            return baseTiles;
        }

        public void SetRectangles()
        {
            foreach (var tile in Tiles.Values)
            {
                tile.Rectangle = new Microsoft.Xna.Framework.Rectangle(tile.X * Global.TileSize, tile.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            }
            foreach (var bgTile in BackgroundTiles.Values)
            {
                bgTile.Rectangle = new Microsoft.Xna.Framework.Rectangle(bgTile.X * Global.TileSize, bgTile.Y * Global.TileSize, Global.TileSize, Global.TileSize);
            }
        }

        public void InitializeTextures()
        {
            foreach (var tile in Tiles.Values)
            {
                tile.InitializeTexture();
            }
            foreach (var bgTile in BackgroundTiles.Values)
            {
                bgTile.InitializeTexture();
            }
        }
        #endregion
    }
}
