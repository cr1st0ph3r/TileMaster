using Xunit;
using TileMaster.Entity.Enums;
using TileMaster.Map;
using TileMaster.Manager;
using TileMaster.Entity;
using System.Linq;

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

        [Fact]
        public void SaveAndLoad_WaterTile_PreservesWater()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            int chunkId = 1;

            // Set a water tile
            map.SetTile(chunkId, globalId, (int)TileType.Water);
            var chunk = map.GetChunk(chunkId);
            Assert.True(chunk.HasWater);

            // Act - Mock save/load by manually calling Write/Read methods or simulating the process
            // Since SaveDataManager works with archives, it's easier to verify the logic via Read/Write methods if they were accessible,
            // or perform a full save/load if the environment allows.
            
            // For this test, we'll verify that the chunk HasWater flag is correctly reconstructed by ReadChunkEntry (which is internal/static)
            // But since we are testing via SaveDataManager.SaveGame / LoadChunk, let's use those.
            
            var activeChunks = new System.Collections.Generic.Dictionary<int, Chunk> { { chunkId, chunk } };
            var worldData = new WorldData { WorldWidth = Global.MapWidth, WorldHeight = Global.MapHeight };
            
            SaveDataManager.SaveGame(worldData, activeChunks);
            
            // Clear the chunk to ensure we are loading fresh
            map.Chunks[chunkId - 1] = null;
            
            // Act
            var loadedChunk = SaveDataManager.LoadChunk(chunkId);
            
            // Assert
            Assert.NotNull(loadedChunk);
            Assert.True(loadedChunk.HasWater, "Loaded chunk should have HasWater flag set.");
            var loadedTile = loadedChunk.Tiles[TileMaster.Map.Map.GlobalToLocalIndex(x, y)];
            Assert.Equal((int)TileType.Water, loadedTile.TileId);
            Assert.False(loadedTile.IsOccupied, "Water tile should NOT be occupied per Tiles.json.");
        }
    }
}
