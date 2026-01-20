using Xunit;
using TileMaster.Entity.Enums;

namespace TileMaster.Test
{
    public class GrassManagerTests
    {
        [Fact]
        public void GrowGrass_DirtNextToGrass_SpreadsGrass()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int chunkId = 1;
            int grassX = 5, grassY = 5;
            int dirtX = 6, dirtY = 5;
            int airX = 6, airY = 4; // Above dirt

            int grassGlobalId = grassY * Global.MapWidth + grassX;
            int dirtGlobalId = dirtY * Global.MapWidth + dirtX;
            int airGlobalId = airY * Global.MapWidth + airX;

            map.SetTile(chunkId, grassGlobalId, (int)TileType.DirtWithGrass);
            map.SetTile(chunkId, dirtGlobalId, (int)TileType.Dirt);
            map.SetTile(chunkId, airGlobalId, (int)TileType.Air);

            // Act
            map.grass.GrowGrass(chunkId);

            // Assert
            var tile = map.GetTileAt(dirtX, dirtY);
            Assert.Equal((int)TileType.DirtWithGrass, tile.TileId);
        }

        [Fact]
        public void GrowTallGrass_AirAboveGrass_GrowsTallGrass()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int chunkId = 1;
            int grassX = 10, grassY = 10;
            int airX = 10, airY = 9;

            int grassGlobalId = grassY * Global.MapWidth + grassX;
            int airGlobalId = airY * Global.MapWidth + airX;

            map.SetTile(chunkId, grassGlobalId, (int)TileType.DirtWithGrass);
            map.SetTile(chunkId, airGlobalId, (int)TileType.Air);

            // Act - Repeat sufficient times for 5% chance
            bool grew = false;
            for (int i = 0; i < 500; i++)
            {
                map.grass.GrowGrass(chunkId);
                if (map.GetTileAt(airX, airY).TileId == (int)TileType.TallGrass)
                {
                    grew = true;
                    break;
                }
            }

            // Assert
            Assert.True(grew, "Tall grass should have grown after 500 attempts at 5% chance.");
        }
    }
}
