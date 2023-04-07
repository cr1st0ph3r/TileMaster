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

       public Dictionary<int,CollisionTiles> Tiles;

       public Chunk (){Tiles = new Dictionary<int, CollisionTiles>();}

   }
}
