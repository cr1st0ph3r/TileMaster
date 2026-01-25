using Xunit;
using Microsoft.Xna.Framework;
using TileMaster.Entity;
using TileMaster.Entity.Enums;
using TileMaster.Map;
using System.Linq;
using TileMaster.Data;

namespace TileMaster.Test
{
    public class ProjectileTests
    {
        [Fact]
        public void Projectile_MovesLinear_NoGravity()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var ammo = Global.ReferenceItems.First(i => i.IsAmmo);
            ammo.AffectedByGravity = false;
            
            Vector2 startPos = new Vector2(100, 100);
            Vector2 velocity = new Vector2(100, 0); // 100 px/s right
            float maxDistance = 500;
            var projectile = new Projectile(ammo, startPos, velocity, maxDistance, 0, 0);
            
            // Act
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(1));
            projectile.Update(gameTime, map);
            
            // Assert
            Assert.Equal(200f, projectile.GetPosition().X);
            Assert.Equal(100f, projectile.GetPosition().Y);
            Assert.True(projectile.IsActive);
        }

        [Fact]
        public void Projectile_CollidesWithSolid()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var ammo = Global.ReferenceItems.First(i => i.IsAmmo);
            ammo.AffectedByGravity = false;
            
            Vector2 startPos = new Vector2(16, 16); // Tile (1,1)
            Vector2 velocity = new Vector2(100, 0); // Moving right
            var projectile = new Projectile(ammo, startPos, velocity, 1000, 0, 0);
            
            // Place solid block at (2,1)
            int targetX = 2, targetY = 1;
            int globalId = targetY * Global.MapWidth + targetX;
            map.SetTile(1, globalId, (int)TileType.Stone);
            
            var tile = map.GetTileAt(targetX, targetY);
            Assert.NotNull(tile);
            Assert.True(tile.IsOccupied, "Tile should be occupied");
            
            // Act
            // Move enough to collide with block at x=32
            // At 100px/s, 0.2s = 20px. 16+20 = 36. Block is at [32, 48].
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.2), System.TimeSpan.FromSeconds(0.2));
            projectile.Update(gameTime, map);
            
            // Assert
            Assert.False(projectile.IsActive, $"Projectile should have deactivated at {projectile.GetPosition()}. Rect: {projectile.GetRectangle()}");
        }

        [Fact]
        public void Projectile_AffectedByGravity_Arches()
        {
             // Arrange
            var map = TestHelper.CreateTestMap();
            var ammo = Global.ReferenceItems.First(i => i.IsAmmo);
            ammo.AffectedByGravity = true;
            
            Vector2 startPos = new Vector2(100, 100);
            Vector2 velocity = new Vector2(100, 0); // Moving right
            var projectile = new Projectile(ammo, startPos, velocity, 1000, 0, 0);
            
            // Act
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(1));
            projectile.Update(gameTime, map);
            
            // Assert
            Assert.True(projectile.GetPosition().Y > 100, "Projectile should have fallen due to gravity.");
        }
    }
}
