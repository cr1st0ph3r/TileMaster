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
        public bool NeedGrassUpdate;
        /// <summary>
        /// Indicates if the chunk has been modified since the last save
        /// </summary>
        public bool HasBeenModified;

        public Dictionary<int, CollisionTiles> Tiles;

        public Chunk() { Tiles = new Dictionary<int, CollisionTiles>(); }

    }
}
