using Xunit;
using TileMaster.Entity.Enums;

namespace TileMaster.Test
{
    public class MapTests
    {
        [Fact]
        public void PlaceItem_ValidSupport_PlacesItem()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var torch = Global.Items.First(i => i.Name == "Torch");
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            int chunkId = 1;

            // Place a solid block below for support
            map.SetTile(chunkId, (y + 1) * Global.MapWidth + x, (int)TileType.Stone);

            // Act
            map.PlaceItem(chunkId, globalId, torch);

            // Assert
            var tile = map.GetTileAt(x, y);
            Assert.NotNull(tile.PlacedItem);
            Assert.Equal("Torch", tile.PlacedItem.Name);
        }

        [Fact]
        public void PlaceItem_NoSupport_RejectsPlacement()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var torch = Global.Items.First(i => i.Name == "Torch");
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            int chunkId = 1;

            // Act
            map.PlaceItem(chunkId, globalId, torch);

            // Assert
            var tile = map.GetTileAt(x, y);
            Assert.Null(tile.PlacedItem);
        }

        [Fact]
        public void PlaceItem_InsideSolid_RejectsPlacement()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var torch = Global.Items.First(i => i.Name == "Torch");
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            int chunkId = 1;

            // Make target tile solid
            map.SetTile(chunkId, globalId, (int)TileType.Stone);

            // Act
            map.PlaceItem(chunkId, globalId, torch);

            // Assert
            var tile = map.GetTileAt(x, y);
            Assert.Null(tile.PlacedItem);
            Assert.Equal((int)TileType.Stone, tile.TileId);
        }
    }
}
