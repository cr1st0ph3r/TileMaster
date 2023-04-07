using System.Linq;
using Microsoft.Xna.Framework.Input;
using TileMaster.Entity;

namespace TileMaster.Helper
{
    public static class InputHelper
    {
   
        public static bool HandleMovingRight(Player player, Map map)
        {
            //var s = map.getTileAt(player.onBlock + 1, player.onChunk);
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                var tileAt = map.getTileAt(player.onBlock+1, player.onChunk, "right");
                if (tileAt != null && tileAt.IsOccupied == false)
                {
                    return true;//proceed with the moving
                }
            }

            player.velocity.X = 0;
            return false;
        }
         
        public static bool HandleMovingLeft(Player player, Map map)
        {

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                var tileAt = map.getTileAt(player.onBlock-1, player.onChunk, "left");
                if (tileAt != null && tileAt.IsOccupied == false)
                    return true;//proceed with the moving
            } 
            //player.velocity.X = 0;
            player.CheckBoundaries();
            return false;
        }
         
        public static bool HandleMovingDown(Player player, Map map)
        {
            UpdateChunk(player, map);
            if (map.IsBlockonChunk(player.onChunk, player.SteppingOn) == true)
            {
                if (map.ChunkDictionary[player.onChunk].Tiles[player.SteppingOn].IsOccupied == false)
                {
                    return true;//proceed with the moving
                }
                return false;

            }
            else
            {
                //handle the player transitioning from a chunk to another
                return false;
            } 
        }

        public static bool HandleJump(Player player, Map map)
        {
            //var s = map.getTileAt(player.onBlock + 1, player.onChunk);
           
                var tileAt = map.getTileAt(player.onBlock -Global.MapWidth, player.onChunk, "up");
                if (tileAt != null && tileAt.IsOccupied == false)
                {
                    return true;//proceed with the moving
                }
            return false;

            
        }

        public static void UpdateChunk(Player player, Map map)
        {
           

            //up down
            if (player.SteppingOn > map.ChunkDictionary[player.onChunk].Tiles.Last().Key)
            {
                player.onChunk += (Global.MapHeight) / Global.ChunkSize;
            }
            if (map.ChunkDictionary[player.onChunk].Tiles.ContainsKey(player.SteppingOn) == false)
            {
                // var chunk = map.ChunkDictionary.FirstOrDefault(x=>x.Value.)
                // player.onChunk++;
            }

        }


        //public static bool HandleKeyPress(Player player, Map map, Keys key)
        //{
        //    if (key == Keys.D)
        //    {
        //        return HandleMovingRight(player, map);
        //    }
        //    return false;
        //}
    }
}
