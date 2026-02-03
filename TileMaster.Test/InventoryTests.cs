using Xunit;
using TileMaster.Entity;
using TileMaster.Entity.Tiles;
using TileMaster.Entity.Enums;

namespace TileMaster.Test
{
    public class InventoryTests
    {
        [Fact]
        public void AddItem_StacksCorrectly()
        {
            // Arrange
            TestHelper.Initialize();
            var player = new Player();
            var item = Global.ReferenceItems.First(i => i.StackSize > 1);
            
            // Act
            player.AddItem(item, 10);
            player.AddItem(item, 5);

            // Assert
            var slot = player.ActionBar.Values.FirstOrDefault(s => s != null && s.Item.Id == item.Id);
            Assert.NotNull(slot);
            Assert.Equal(15, slot.Quantity);
        }

        [Fact]
        public void AddItem_FillsActionBarThenInventory()
        {
            // Arrange
            TestHelper.Initialize();
            var player = new Player();
            var item = Global.ReferenceItems.First();

            // Fill ActionBar
            for (int i = 0; i < 10; i++)
            {
                player.ActionBar[i] = new InventoryItem(item, item.StackSize);
            }

            // Act
            player.AddItem(item, 10);

            // Assert
            var slot = player.Inventory.Values.FirstOrDefault(s => s != null && s.Item.Id == item.Id);
            Assert.NotNull(slot);
            Assert.Equal(10, slot.Quantity);
        }

        [Fact]
        public void ConsumeItem_ReducesQuantityAndClearsSlot()
        {
            // Arrange
            TestHelper.Initialize();
            var player = new Player();
            var item = Global.ReferenceItems.First();
            player.ActionBar[0] = new InventoryItem(item, 10);

            // Act
            player.ConsumeItem(item.Id, 10);

            // Assert
            Assert.Null(player.ActionBar[0]);
        }

        [Fact]
        public void PerformActionOnTile_MiningReturnsItem()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            map.SetTile(1, globalId, (int)TileType.Stone);

            // Act
            var dropped = map.PerformActionOnTile(1, globalId, ToolAction.MineBlock);

            // Assert
            Assert.Single(dropped);
            Assert.Equal((int)TileType.Stone, dropped[0].TileId);
            Assert.Equal((int)TileType.Air, map.GetTileAt(x, y).TileId);
        }

        [Fact]
        public void PerformActionOnTile_MiningItemReturnsItem()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var torch = Global.ReferenceItems.First(i => i.Name == "Torch");
            int x = 5, y = 5;
            int globalId = y * Global.MapWidth + x;
            
            // Support block
            map.SetTile(1, (y + 1) * Global.MapWidth + x, (int)TileType.Stone);
            map.PlaceItem(1, globalId, torch);

            // Act
            var dropped = map.PerformActionOnTile(1, globalId, ToolAction.MineBlock);

            // Assert
            Assert.Single(dropped);
            Assert.Equal("Torch", dropped[0].Name);
            Assert.Null(map.GetTileAt(x, y).PlacedItem);
        }
    }
}
