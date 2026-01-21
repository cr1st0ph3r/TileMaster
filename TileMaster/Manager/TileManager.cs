using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TileMaster.Manager
{
    public class TileManager
    {
     
        //TODO check whether this list need to exist
        public Dictionary<int, List<Texture2D>> TileTextures { get; set; }
        public TileManager()
        {
            TileTextures = new Dictionary<int, List<Texture2D>>();
        }
    }
}
