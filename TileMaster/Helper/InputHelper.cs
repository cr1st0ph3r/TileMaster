using System.Linq;
using Microsoft.Xna.Framework.Input;
using TileMaster.Entity;

namespace TileMaster.Helper
{
    public static class InputHelper
    {
        public static bool HandleMovingRight(Player player, Map.Map map)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                var tileAt = map.GetTileAt(player.onBlock + 1, player.onChunk, "right");
                if (tileAt != null && tileAt.IsOccupied == false)
                {
                    return true;//proceed with the moving
                }
            }

            player.velocity.X = 0;
            return false;
        }

        public static bool HandleMovingLeft(Player player, Map.Map map)
        {

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                var tileAt = map.GetTileAt(player.onBlock - 1, player.onChunk, "left");
                if (tileAt != null && tileAt.IsOccupied == false)
                    return true;//proceed with the moving
            }
            player.CheckBoundaries();
            return false;
        }

        public static bool HandleMovingDown_old(Player player, Map.Map map)
        {
            UpdateChunk(player, map);
            if (map.IsBlockOnChunk(player.onChunk, player.SteppingOn) == true)
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

        public static bool HandleMovingDown(Player player, Map.Map map)
        {
            // Update chunk info first (keeps existing behavior)
            UpdateChunk(player, map);

            // compute the left-most and right-most tile indices overlapped by the player's width
            int leftGridX = (int)(player.GetPosition().X / Global.TileSize);
            int rightGridX = (int)((player.GetPosition().X + player.GetRectangle().Width - 1) / Global.TileSize);

            // compute the grid row below the player's feet           
            int footGridY = player.GridY + player.Height;

            // If any tile below the player's horizontal span is occupied -> player is supported (do not fall).
            // If all are empty -> player should fall (return true).
            for (int gx = leftGridX; gx <= rightGridX; gx++)
            {
                int blockId = (footGridY * Global.MapWidth) + gx;

                // Try to resolve the tile. Use left/right direction to help GetTileAt cross chunk lookups.
                string direction = gx > player.GridX ? "right" : (gx < player.GridX ? "left" : "right");
                var tile = map.GetTileAt(blockId, player.onChunk, direction);

                // If tile is missing (null) keep previous conservative behavior: treat as "no safe result" and don't allow falling.
                if (tile == null)
                {
                    return true;
                }

                // If any tile below is occupied, the player should not fall through.
                if (tile.IsOccupied)
                {
                    return false;
                }
            }

            // No supporting tiles under the entire width -> allow falling
            return true;
        }

        public static bool HandleJump(Player player, Map.Map map)
        {
            var tileAt = map.GetTileAt(player.onBlock - (Global.MapWidth * player.Height), player.onChunk, "up");
            if (tileAt != null && tileAt.IsOccupied == false)
            {
                return true;//proceed with the moving
            }
            return false;
        }

        public static void UpdateChunk(Player player, Map.Map map)
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
    }
}
