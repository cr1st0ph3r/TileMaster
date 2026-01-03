using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
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

        public static bool HandleMovingDown(Player player, Map.Map map)
        {
            // Update chunk info first (keeps existing behavior)
            UpdateChunk(player, map);

            // Use a small physics-aware test instead of only grid occupancy.
            // Sample a few points under the player's feet (left, center, right).
            // Only treat a tile as "supporting" if:
            //  - it's occupied
            //  - the tile top is within a small vertical tolerance from the player's bottom
            //  - there is horizontal overlap between player and that tile
            //
            // This prevents slight horizontal clipping into wall tiles from being
            // interpreted as "standing on top" of that tile (the usual wall-sticking cause).

            var pr = player.GetRectangle();
            int footRow = player.GridY + player.Height;

            int[] sampleXs = new int[]
            {
                pr.Left + 2,
                pr.Left + pr.Width / 2,
                pr.Right - 2
            };

            const int supportTolerancePx = 4;    // max gap allowed between feet and tile top to count as supported
            const int maxPenetrationPx = 4;      // allow tiny penetration before rejecting (prevents jitter)

            foreach (var sampleX in sampleXs)
            {
                // clamp sampleX inside map bounds
                if (sampleX < 0) continue;
                int gx = sampleX / Global.TileSize;
                if (gx < 0 || gx >= Global.MapWidth) continue;

                int blockId = (footRow * Global.MapWidth) + gx;
                string direction = gx > player.GridX ? "right" : (gx < player.GridX ? "left" : "right");
                var tile = map.GetTileAt(blockId, player.onChunk, direction);

                // if tile is missing, preserve previous behavior and allow falling
                if (tile == null)
                    return true;

                if (!tile.IsOccupied)
                    continue;

                // get tile rectangle and check vertical relationship with player's bottom
                var tileTop = tile.Rectangle.Top;
                int verticalDelta = tileTop - pr.Bottom; // 0 when perfectly aligned, negative if player penetrates tile

                // require horizontal overlap to be non-zero (avoid counting side-touching tiles)
                int horizOverlap = Math.Min(pr.Right, tile.Rectangle.Right) - Math.Max(pr.Left, tile.Rectangle.Left);

                if (horizOverlap > 0 && verticalDelta <= supportTolerancePx && verticalDelta >= -maxPenetrationPx)
                {
                    // found a supporting tile under player's foot -> do not fall
                    return false;
                }
            }

            // no supporting tiles under sampled foot points -> allow falling
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
