using Microsoft.Xna.Framework;
using System;
using TileMaster.Entity.Tiles;

namespace TileMaster.Helper
{
    public static class SlopeCollisionHelper
    {
        /// <summary>
        /// Gets the height of a slope tile at a specific X position within the tile
        /// </summary>
        /// <param name="tile">The slope tile</param>
        /// <param name="localX">X position within the tile (0 to tile size)</param>
        /// <returns>Height from the bottom of the tile (0 = bottom, tile size = top)</returns>
        public static float GetSlopeHeightAt(Tile tile, float localX)
        {
            if (!tile.IsSlope)
                return Global.TileSize; // Non-slope tiles are full height

            // Clamp localX to tile bounds
            localX = MathHelper.Clamp(localX, 0, Global.TileSize);

            // Calculate height based on slope rotation
            switch (tile.SlopeRotation)
            {
                case 0: // Slope rising to the right (/)
                    return (localX / Global.TileSize) * Global.TileSize;
                
                case 1: // Slope rising to the left (\)
                    return Global.TileSize - ((localX / Global.TileSize) * Global.TileSize);
                
                case 2: // Inverted slope rising to the left (\)
                    return (localX / Global.TileSize) * Global.TileSize;
                
                case 3: // Inverted slope rising to the right (/)
                    return Global.TileSize - ((localX / Global.TileSize) * Global.TileSize);
                
                default:
                    return Global.TileSize;
            }
        }

        /// <summary>
        /// Checks if a point collides with a slope tile
        /// </summary>
        /// <param name="tile">The slope tile</param>
        /// <param name="worldX">World X position to check</param>
        /// <param name="worldY">World Y position to check</param>
        /// <returns>True if the point collides with the slope</returns>
        public static bool IsPointCollidingWithSlope(Tile tile, float worldX, float worldY)
        {
            if (!tile.IsSlope)
                return tile.IsSolid;

            // Get local coordinates within the tile
            float localX = worldX - tile.Rectangle.Left;
            float localY = worldY - tile.Rectangle.Top;

            // Get the slope height at this X position
            float slopeHeight = GetSlopeHeightAt(tile, localX);
            
            // Check if the point is below the slope surface
            return localY >= (Global.TileSize - slopeHeight);
        }

        /// <summary>
        /// Gets the Y position where a rectangle should rest on a slope tile
        /// </summary>
        /// <param name="tile">The slope tile</param>
        /// <param name="rectBottom">Bottom Y of the rectangle in world coordinates</param>
        /// <param name="rectLeft">Left X of the rectangle in world coordinates</param>
        /// <param name="rectRight">Right X of the rectangle in world coordinates</param>
        /// <returns>The Y position where the rectangle should rest on the slope</returns>
        public static float GetSlopeRestPosition(Tile tile, float rectBottom, float rectLeft, float rectRight)
        {
            if (!tile.IsSlope)
                return tile.Rectangle.Top;

            // Sample multiple points across the rectangle for better accuracy
            float leftHeight = GetSlopeHeightAt(tile, rectLeft - tile.Rectangle.Left);
            float rightHeight = GetSlopeHeightAt(tile, rectRight - tile.Rectangle.Left);
            float centerHeight = GetSlopeHeightAt(tile, ((rectLeft + rectRight) / 2f) - tile.Rectangle.Left);

            // Use the highest point to prevent sinking into the slope
            float maxHeight = MathHelper.Max(leftHeight, MathHelper.Max(rightHeight, centerHeight));
            
            return tile.Rectangle.Top + (Global.TileSize - maxHeight);
        }

        /// <summary>
        /// Checks if a rectangle is properly supported by a slope tile
        /// </summary>
        /// <param name="tile">The slope tile</param>
        /// <param name="rect">The rectangle to check</param>
        /// <param name="tolerance">Vertical tolerance for support detection</param>
        /// <returns>True if the rectangle is supported by the slope</returns>
        public static bool IsRectangleSupportedBySlope(Tile tile, Rectangle rect, int tolerance = 4)
        {
            if (!tile.IsSlope)
                return tile.IsSolid && rect.Bottom >= tile.Rectangle.Top && rect.Bottom <= tile.Rectangle.Top + tolerance;

            // Check if rectangle intersects with the slope tile horizontally
            if (rect.Right <= tile.Rectangle.Left || rect.Left >= tile.Rectangle.Right)
                return false;

            // Get the slope height at the rectangle's bottom edge
            float slopeHeight = GetSlopeHeightAt(tile, rect.Left - tile.Rectangle.Left);
            float slopeTop = tile.Rectangle.Top + (Global.TileSize - slopeHeight);

            // Check if the rectangle is within tolerance of the slope surface
            return Math.Abs(rect.Bottom - slopeTop) <= tolerance;
        }

        /// <summary>
        /// Adjusts player velocity when walking on a slope
        /// </summary>
        /// <param name="tile">The slope tile</param>
        /// <param name="velocityX">Player's horizontal velocity</param>
        /// <param name="isOnGround">Whether player is on ground</param>
        /// <returns>Adjusted velocity</returns>
        public static Vector2 AdjustVelocityForSlope(Tile tile, float velocityX, bool isOnGround)
        {
            if (!tile.IsSlope || !isOnGround)
                return new Vector2(velocityX, 0);

            // Calculate slope angle based on rotation
            float slopeAngle = 0f;
            switch (tile.SlopeRotation)
            {
                case 0: // 45 degrees up
                    slopeAngle = MathHelper.ToRadians(-45f);
                    break;
                case 1: // 45 degrees up (opposite direction)
                    slopeAngle = MathHelper.ToRadians(45f);
                    break;
                case 2: // 45 degrees down
                    slopeAngle = MathHelper.ToRadians(45f);
                    break;
                case 3: // 45 degrees down (opposite direction)
                    slopeAngle = MathHelper.ToRadians(-45f);
                    break;
            }

            // Apply slope influence to horizontal movement
            float slopeInfluence = (float)System.Math.Sin(slopeAngle) * Math.Abs(velocityX) * 0.3f;
            
            return new Vector2(velocityX, slopeInfluence);
        }
    }
}