using Xunit;
using TileMaster.Entity.Enums;
using TileMaster.Map;
using TileMaster.Manager;

namespace TileMaster.Test
{
    public class MapTests
    {
        [Fact]
        public void PlaceItem_ValidSupport_PlacesItem()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var torch = Global.ReferenceItems.First(i => i.Name == "Torch");
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
            var torch = Global.ReferenceItems.First(i => i.Name == "Torch");
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
            var torch = Global.ReferenceItems.First(i => i.Name == "Torch");
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

        //[Fact]
        //public void HammerTile_CyclesAndReverts()
        //{
        //    // Arrange
        //    var map = TestHelper.CreateTestMap();
        //    int x = 5, y = 5;
        //    int globalId = y * Global.MapWidth + x;
        //    int chunkId = 1;

        //    // Set a stone tile
        //    map.SetTile(chunkId, globalId, (int)TileType.Stone);
        //    var tile = map.GetTileAt(x, y);

        //    // Mock textures to ensure HammerTile works in test environment
        //    var refTile = Global.ReferenceTiles[tile.TileId];
        //    if (refTile.Textures == null) refTile.Textures = new System.Collections.Generic.List<Microsoft.Xna.Framework.Graphics.Texture2D>();
            
        //    // Add a mock slope texture if not present
        //    if (!refTile.Textures.Any(x => x != null && x.Name != null && x.Name.EndsWith("Slope")))
        //    {
        //        // We can't easily create a Texture2D without a GraphicsDevice, 
        //        // but we can check if the logic handles the absence or if we can mock the list.
        //        // For the purpose of this logic test, let's just ensure the list exists and has a dummy item if needed,
        //        // or better, we just test that it DOES NOT crash now with the safety fix.
        //    }

        //    // Since we can't easily mock Texture2D here, let's at least test the rotation logic 
        //    // by manually setting IsSlope to true if the hammer failed to find a texture, 
        //    // OR we just assume the test environment might have SOME tile with a slope.
            
        //    // Actually, let's just test that it cycles IF it is a slope.
        //    tile.IsSlope = true;
        //    tile.SlopeRotation = 0;
        //    tile.Rotation = 0f;

        //    // Act & Assert
        //    // 2nd Hammer (pretend): Slope (Rotation 0) -> Slope (Rotation 90 deg)
        //    map.HammerTile(tile);
        //    Assert.True(tile.IsSlope);
        //    Assert.Equal(1, tile.SlopeRotation);
        //    Assert.Equal(System.MathF.PI / 2f, tile.Rotation);

        //    // 3rd Hammer: Slope (Rotation 90 deg) -> Slope (Rotation 180 deg)
        //    map.HammerTile(tile);
        //    Assert.True(tile.IsSlope);
        //    Assert.Equal(2, tile.SlopeRotation);
        //    Assert.Equal(System.MathF.PI, tile.Rotation);

        //    // 4th Hammer: Slope (Rotation 180 deg) -> Slope (Rotation 270 deg)
        //    map.HammerTile(tile);
        //    Assert.True(tile.IsSlope);
        //    Assert.Equal(3, tile.SlopeRotation);
        //    Assert.Equal(3f * System.MathF.PI / 2f, tile.Rotation);

        //    // 5th Hammer: Slope (Rotation 270 deg) -> Regular (No Rotation)
        //    map.HammerTile(tile);
        //    Assert.False(tile.IsSlope);
        //    Assert.Equal(0, tile.SlopeRotation);
        //    Assert.Equal(0f, tile.Rotation);
        //}

        [Fact]
        public void SaveAndLoad_SlopeTile_PreservesSlopeData()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            int chunkId = 1;

            // Set a slope tile manually for testing
            map.SetTile(chunkId, globalId, (int)TileType.Stone);
            var tile = map.GetTileAt(x, y);
            tile.IsSlope = true;
            tile.SlopeRotation = 2;
            tile.Rotation = System.MathF.PI;

            var activeChunks = new System.Collections.Generic.Dictionary<int, Chunk> { { chunkId, map.GetChunk(chunkId) } };
            var worldData = new WorldData { WorldWidth = Global.MapWidth, WorldHeight = Global.MapHeight };

            // Act
            SaveDataManager.SaveGame(worldData, activeChunks);
            var loadedChunk = SaveDataManager.LoadChunk(chunkId);

            // Assert
            Assert.NotNull(loadedChunk);
            var loadedTile = loadedChunk.Tiles[TileMaster.Map.Map.GlobalToLocalIndex(x, y)];
            Assert.True(loadedTile.IsSlope, "Loaded tile should be a slope.");
            Assert.Equal(2, loadedTile.SlopeRotation);
            Assert.Equal(System.MathF.PI, loadedTile.Rotation);
        }
    }
}
