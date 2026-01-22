using TileMaster.Entity.Enums;
using TileMaster.Helper;
using Xunit;

namespace TileMaster.Test
{
    public class SlopeCollisionTests
    {
        [Fact]
        public void SlopeCollisionHelper_GetSlopeHeightAt_CalculatesCorrectHeights()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var tile = map.GetTileAt(5, 5);
            tile.IsSlope = true;
            tile.IsOccupied = true;
            tile.IsSolid = true;
            
            // Act & Assert - Test each rotation
            // Rotation 0: Slope rising to right (/)
            tile.SlopeRotation = 0;
            Assert.Equal(0f, SlopeCollisionHelper.GetSlopeHeightAt(tile, 0f), 0.01f); // Left edge should be at bottom
            Assert.Equal(Global.TileSize, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize), 0.01f); // Right edge should be at top
            Assert.Equal(Global.TileSize / 2f, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize / 2f), 0.01f); // Center should be at half height
            
            // Rotation 1: Slope rising to left (\)
            tile.SlopeRotation = 1;
            Assert.Equal(Global.TileSize, SlopeCollisionHelper.GetSlopeHeightAt(tile, 0f), 0.01f); // Left edge should be at top
            Assert.Equal(0f, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize), 0.01f); // Right edge should be at bottom
            Assert.Equal(Global.TileSize / 2f, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize / 2f), 0.01f); // Center should be at half height
            
            // Rotation 2: Inverted slope rising to left
            tile.SlopeRotation = 2;
            Assert.Equal(0f, SlopeCollisionHelper.GetSlopeHeightAt(tile, 0f), 0.01f); // Left edge should be at bottom
            Assert.Equal(Global.TileSize, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize), 0.01f); // Right edge should be at top
            
            // Rotation 3: Inverted slope rising to right
            tile.SlopeRotation = 3;
            Assert.Equal(Global.TileSize, SlopeCollisionHelper.GetSlopeHeightAt(tile, 0f), 0.01f); // Left edge should be at top
            Assert.Equal(0f, SlopeCollisionHelper.GetSlopeHeightAt(tile, Global.TileSize), 0.01f); // Right edge should be at bottom
        }
        
        [Fact]
        public void GrassSpreadingOnSlopes_DirtSlopeWithAirContact_GrowsGrassOnSlope()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int chunkId = 1;
            
            // Create a dirt slope tile
            int slopeX = 8;
            int slopeY = 8;
            int slopeGlobalId = slopeY * Global.MapWidth + slopeX;
            map.SetTile(chunkId, slopeGlobalId, (int)TileType.Dirt);
            
            var slopeTile = map.GetTileAt(slopeX, slopeY);
            slopeTile.IsSlope = true;
            slopeTile.SlopeRotation = 0; // Slope rising to right
            slopeTile.IsOccupied = true;
            slopeTile.IsSolid = true;
            
            // Add air contact above slope (required for grass growth)
            int airX = slopeX;
            int airY = slopeY - 1;
            int airGlobalId = airY * Global.MapWidth + airX;
            map.SetTile(chunkId, airGlobalId, (int)TileType.Air);
            
            // Add existing grass nearby to trigger spreading
            int grassX = slopeX + 1;
            int grassY = slopeY;
            int grassGlobalId = grassY * Global.MapWidth + grassX;
            map.SetTile(chunkId, grassGlobalId, (int)TileType.DirtWithGrass);
            
            // Act
            map.grass.GrowGrass(chunkId);
            
            // Assert
            var updatedTile = map.GetTileAt(slopeX, slopeY);
            Assert.Equal((int)TileType.DirtWithGrass, updatedTile.TileId); // Slope should have grown grass
            Assert.True(updatedTile.IsSlope, "Tile should remain a slope after growing grass");
            Assert.Equal(0, updatedTile.SlopeRotation); // Slope rotation should be preserved
        }
    
        [Fact]
        public void FiveSlopeHillTest_CreatesHillOfSlopes_WorksCorrectly()
        {
            // Arrange
            var map = TestHelper.CreateTestMap(3, 2);           
            
            // Create a hill of 5 slope tiles
            int startX = 5;
            int groundY = 10;
            int slopeCount = 5;
            
            // Create slope hill (slopes rising to right - rotation 0)
            for (int i = 0; i < slopeCount; i++)
            {
                int x = startX + i;
                var tile = map.GetTileAt(x, groundY);
                if (tile != null)
                {
                    tile.TileId = (int)TileType.Dirt;
                    tile.IsSlope = true;
                    tile.SlopeRotation = 0; // Slope rising to right
                    tile.IsOccupied = true;
                    tile.IsSolid = true;
                }
            }
            
            // Test positions at different points on the slope hill
            for (int i = 0; i < slopeCount; i++)
            {
                int currentX = startX + i;
                var currentSlopeTile = map.GetTileAt(currentX, groundY);
                
                if (currentSlopeTile != null && currentSlopeTile.IsSlope)
                {
                    // Test slope height at different X positions
                    float leftHeight = SlopeCollisionHelper.GetSlopeHeightAt(currentSlopeTile, 0f);
                    float rightHeight = SlopeCollisionHelper.GetSlopeHeightAt(currentSlopeTile, Global.TileSize);
                    float centerHeight = SlopeCollisionHelper.GetSlopeHeightAt(currentSlopeTile, Global.TileSize / 2f);
                    
                    // Verify the heights make sense for a slope rising to right
                    Assert.True(leftHeight >= 0f && leftHeight <= Global.TileSize, 
                        $"Slope {i} left height should be valid: {leftHeight}");
                    Assert.True(rightHeight >= 0f && rightHeight <= Global.TileSize, 
                        $"Slope {i} right height should be valid: {rightHeight}");
                    
                    // For a slope rising to right, right should be higher than left
                    Assert.True(rightHeight >= leftHeight, 
                        $"Slope {i} right height ({rightHeight}) should be >= left height ({leftHeight})");
                }
            }
        }
    }
}