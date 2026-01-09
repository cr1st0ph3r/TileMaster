using Microsoft.Xna.Framework.Input;
using System;
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
            UpdatePlayerChunk(player, map);

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
            UpdatePlayerChunk(player, map);

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
            int footRow = player.GridY+1;

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
            var game = Game.GetInstance();
            if (tileAt is null)
            {
             
                game.LogMessage($"Jump found null block at block {player.onBlock - (Global.MapWidth * player.Height)} on chunk {player.onChunk}",Microsoft.Xna.Framework.Color.Red);
                return false;
            }
            if (tileAt.IsOccupied == false)
            {
                return true;//proceed with the moving
            } 
            game.LogMessage("Jump found null block at ", Microsoft.Xna.Framework.Color.Red);
            return false;
        }

        public static void UpdatePlayerChunk(Player player, Map.Map map)
        {
            // Use player's bottom-center point to decide which chunk they belong to.
            // This is robust against slight overlaps and works regardless of current
            // values on the Player object (keeps InputHelper self-contained).
            var rect = player.GetRectangle();

            // reference point: horizontal center, just above the bottom pixel
            int refX = rect.Left + rect.Width / 2;
            int refY = rect.Bottom - 1;

            // clamp to world pixel bounds
            refX = Math.Max(0, Math.Min(refX, Global.MapWidth * Global.TileSize - 1));
            refY = Math.Max(0, Math.Min(refY, Global.MapHeight * Global.TileSize - 1));

            // convert to grid coordinates (tile indices)
            int gridX = refX / Global.TileSize;
            int gridY = refY / Global.TileSize;

            // update player's grid indices (keeps state consistent)
            player.GridX = gridX;
            player.GridY = gridY;

            // compute chunk coords and 1-based chunk id
            int chunksPerRow = Global.MapWidth / Global.ChunkSize;
            int chunksPerCol = Global.MapHeight / Global.ChunkSize;
            int chunkX = gridX / Global.ChunkSize;
            int chunkY = gridY / Global.ChunkSize;

            // clamp chunk coordinates into valid ranges
            chunkX = Math.Max(0, Math.Min(chunkX, chunksPerRow - 1));
            chunkY = Math.Max(0, Math.Min(chunkY, chunksPerCol - 1));

            int newChunk = 1 + (chunkY * chunksPerRow) + chunkX;

            // prefer assigning the exact chunk when present, otherwise search nearby loaded neighbors
            if (map.ChunkDictionary.ContainsKey(newChunk))
            {
                player.onChunk = newChunk;
            }
            else
            {
                // fallback: find nearest loaded chunk within a 1-chunk radius
                bool found = false;
                for (int dy = -1; dy <= 1 && !found; dy++)
                {
                    for (int dx = -1; dx <= 1 && !found; dx++)
                    {
                        int cx = chunkX + dx;
                        int cy = chunkY + dy;
                        if (cx < 0 || cx >= chunksPerRow || cy < 0 || cy >= chunksPerCol) continue;
                        int cid = 1 + (cy * chunksPerRow) + cx;
                        if (map.ChunkDictionary.ContainsKey(cid))
                        {
                            player.onChunk = cid;
                            found = true;
                        }
                    }
                }
                // if still not found, leave player.onChunk unchanged (prevents spurious assignments)
            }

            // keep onBlock / SteppingOn consistent with the chosen grid indices
            player.onBlock = (player.GridY * Global.MapWidth) + player.GridX;
            player.SteppingOn = player.onBlock + Global.MapWidth;
        }
    }
}
