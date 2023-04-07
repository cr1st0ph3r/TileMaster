using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Helper;
using TileMaster.Util;

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

        public void AddTileTexture(int tileId, int amount, List<System.Drawing.Color> colors)
        {

            var game = Game.GetInstance();
            

            var listT = new List<Texture2D>();
            for (int i = 0; i < amount; i++)
            {
                var bmp = ColorRandomizer.RandomTile(colors, 16, 16);
                var t2d = Texture2DHelper.GetTexture2DFromBitmap(game.GraphicsDevice, bmp);
                listT.Add(t2d);
            }

            TileTextures.Add(tileId, listT);
        }

        public void Load(List<TileColor> tileColors)
        {
            foreach (var tileC in tileColors)
            {
                AddTileTexture(tileC.Id, Global.RandomizationFactorAmount, tileC.BuildColors());
            }
        }
        public List<CollisionTiles> LoadTileTextures(List<CollisionTiles> tiles)
        {
            foreach (var tile in tiles)
            {
                if (TileTextures.ContainsKey(tile.TileId))
                {
                    tile.AltTextures = TileTextures[tile.TileId];
                }
            }
            return tiles;
        }

        public Texture2D GetRandomTexture(Tile t)
        {
            if (TileTextures.ContainsKey(t.TileId))
            {
                return GetRandomTexture(t.TileId);
            }
            return t.texture;
        }
        public Texture2D GetRandomTexture(int tileId)
        {
            return TileTextures[tileId].OrderBy(x => Guid.NewGuid()).FirstOrDefault();
        }
    }
}
