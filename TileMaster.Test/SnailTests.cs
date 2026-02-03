using Xunit;
using Microsoft.Xna.Framework;
using TileMaster.Entity;
using TileMaster.Entity.MobMovement;
using TileMaster.Entity.Enums;

namespace TileMaster.Test
{
    public class SnailTests
    {
        [Fact]
        public void Snail_ClimbsWall_WhenMovingRight()
        {
            // Arrange
            var map = TestHelper.CreateTestMap();
            var snailMov = new Snail();
            var mob = new Mob();
            mob.Movement = snailMov;
            mob.SetPosition(new Vector2(0, 0)); // Top-Left of world
            // Set bounds manually since texture is missing
            // mob.rectangle update relies on Texture. 
            // We need to Hack Mob or use Reflection?
            // Entity.SetPosition sets rectangle using Texture. 
            // We need to set Texture to null (default) then set rectangle directly?
            // Entity.rectangle is protected.
            // But Mob inherits Entity. We can't access protected from here.
            // But checking Snail.cs... it calls mob.GetRectangle().
            
            // Hack: Create a subclass of Mob for testing that allows setting rectangle?
            // Or just assume Mob needs a dummy texture.
            // Since we can't create Texture2D easily.
            // Maybe we can rely on IsRectCollidingWithMap using mob.GetRectangle().
            
            // Let's create a TestMob class here.
        }
        
        private class TestMob : Mob
        {
            public TestMob()
            {
                // Init with dummy rect
                this.rectangle = new Rectangle(0, 0, 16, 16);
                this.MoveSpeed = 100f;
                this.velocity = Vector2.Zero;
            }
            
            public void SetRect(Rectangle r)
            {
                this.rectangle = r;
            }
            
            public override void Update(GameTime gameTime, Map.Map map)
            {
                base.Update(gameTime, map);
                // Sync rect position
                this.rectangle.X = (int)position.X;
                this.rectangle.Y = (int)position.Y;
            }
        }
        
        [Fact]
        public void Snail_WalksOnFloor()
        {
            var map = TestHelper.CreateTestMap();
            // Set floor at y=1 (16px)
            for(int x=0; x<10; x++) map.SetTile(1, x + Global.MapWidth, (int)TileType.Dirt);
            
            var mob = new TestMob();
            mob.Movement = new Snail();
            mob.SetPosition(new Vector2(16, 0)); // On top of tile 1,1
            mob.Target = new Entity.Entity(); // Dummy target
            mob.Target.SetPosition(new Vector2(100, 0)); // To the right
            
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1));
            
            // Act
            mob.Movement.Move(gameTime, mob, map); // 1st frame
            
            // Assert
            Assert.True(mob.velocity.X > 0, "Should move right");
            Assert.Equal(0, mob.velocity.Y);
            Assert.Equal(0, mob.Rotation); // 0 rotation on floor
        }

        [Fact]
        public void Snail_ClimbsWall()
        {
            var map = TestHelper.CreateTestMap();
            // Floor at y=1. Wall at x=2, y=0.
            //   W
            // S F F
            map.SetTile(1, 1 * Global.MapWidth + 0, (int)TileType.Dirt); // Floor
            map.SetTile(1, 1 * Global.MapWidth + 1, (int)TileType.Dirt); // Floor
            map.SetTile(1, 0 * Global.MapWidth + 2, (int)TileType.Dirt); // Wall at x=2
            
            var mob = new TestMob();
            mob.Movement = new Snail();
            mob.SetPosition(new Vector2(16, 0)); // At x=1 (16px)
            mob.Target = new Entity.Entity();
            mob.Target.SetPosition(new Vector2(100, -50)); // Right and Up
            
            // Move it close to wall
            mob.SetPosition(new Vector2(30, 0)); // Nearly touching x=32
            
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1));
            
            // Act 1: Move into wall
            mob.Movement.Move(gameTime, mob, map);
            
            // It should have hit the wall.
            // Snail logic: collision -> _gravityDir changes to Right (1, 0). Velocity becomes 0.
            
            // Move again to see climb
            // Now gravity is Right (1, 0). Tangent is Down (-1)?
            // Grav(1,0). Tangent(0, -1). Up.
            // Correct.
            
            // Move 2nd frame
            mob.Movement.Move(gameTime, mob, map);
            
            float rot = mob.Rotation;
            Assert.Equal(-MathHelper.PiOver2, rot, 0.1);
        }
        
        [Fact]
        public void Snail_WalksOffLedge_Rotates()
        {
            var map = TestHelper.CreateTestMap();
            // Floor at (0,1). Air at (1,1).
            map.SetTile(1, 1 * Global.MapWidth + 0, (int)TileType.Dirt);
            
            var mob = new TestMob();
            mob.Movement = new Snail();
            mob.SetPosition(new Vector2(0, 0));
            mob.Target = new Entity.Entity();
            mob.Target.SetPosition(new Vector2(100, 0)); // Right
            
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1));
            
            // 1. Move while on ground to establish direction state
            mob.Movement.Move(gameTime, mob, map); // Should move right, set _lastMoveDir = 1
            
            // 2. Teleport to edge/Just off edge?
            // If we assume logic works, we can just set position to (17, 0) now.
            // _lastMoveDir is preserved in the Snail instance.
            mob.SetPosition(new Vector2(17, 0));
            
            // 3. Act - Frame where we are in air
            mob.Movement.Move(gameTime, mob, map);
            
            // Should be at X > 16 (in air)
            // Should rotate.
            // Old Grav(0,1). Tangent(1,0).
            // New Grav = -Tangent = (-1, 0). Left.
            // Rot = Atan2(0, -1) - Pi/2 = Pi - Pi/2 = Pi/2 (90 deg).
            
            Assert.Equal(MathHelper.PiOver2, mob.Rotation, 0.1);
        }

        [Fact]
        public void Snail_SpawnsInAir_Falls()
        {
            var map = TestHelper.CreateTestMap();
            // All air by default
            
            var mob = new TestMob();
            mob.Movement = new Snail();
            mob.SetPosition(new Vector2(100, 100)); // Mid-air
            mob.Target = new Entity.Entity();
            mob.Target.SetPosition(new Vector2(200, 100)); // Right
            
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1));
            
            // Act
            mob.Movement.Move(gameTime, mob, map);
            
            // Assert
            // Should have moved Down (Gravity)
            // Should NOT have moved Right (No air control / or negligible)
            // Velocity Y should be positive (Down)
            
            Assert.True(mob.velocity.Y > 0, $"Velocity Y should be positive (falling), but was {mob.velocity.Y}");
            
            // Check rotation is 0 (Upright)
            Assert.Equal(0, mob.Rotation, 0.1);
        }

        [Fact]
        public void Snail_TraversesOneBlockDeepValley()
        {
            var map = TestHelper.CreateTestMap();
            // Create a valley: Floor at Y=1. Hole at X=2.
            // F F . F F
            //     F
            
            int yFloor = 1;
            for(int x=0; x<5; x++) 
            {
                if (x != 2) map.SetTile(1, x + Global.MapWidth * yFloor, (int)TileType.Dirt);
            }
            // Bottom of pit at Y=2, X=2
            map.SetTile(1, 2 + Global.MapWidth * (yFloor + 1), (int)TileType.Dirt);
            
            // Walls of pit need to be solid?
            // "Depression" usually implies walls.
            // X=1 (Floor). X=2 (Air). X=3 (Floor). Y=2 (Floor).
            // So X=1,Y=2 and X=3,Y=2 are effectively walls for the pit? No, X=1,Y=1 is the wall.
            // Actually, if it's 1 block deep.
            // X=0, Y=1 (Floor)
            // X=1, Y=1 (Floor)
            // X=2, Y=1 (Air - Pit)
            // X=3, Y=1 (Floor)
            // X=2, Y=2 (Floor - Pit Bottom)
            // X=1, Y=2 (Soil)
            // X=3, Y=2 (Soil)
            
            // Let's build side supports so snail can climb down/up
            map.SetTile(1, 1 + Global.MapWidth * 2, (int)TileType.Dirt);
            map.SetTile(1, 3 + Global.MapWidth * 2, (int)TileType.Dirt);
            
            var mob = new TestMob();
            mob.Movement = new Snail();
            mob.SetPosition(new Vector2(0, 0)); // Start left
            mob.Target = new Entity.Entity();
            mob.Target.SetPosition(new Vector2(64, 50)); // Target on right side (past pit) AND BELOW the exit of the pit.
            
            var gameTime = new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1));
            
            // Run multiple updates to simulate traversal
            // 1. Walk right 
            // 2. Fall into pit
            // 3. Walk right in pit
            // 4. Climb Left Wall of X=3
            // 5. At top of wall (Y=16), Target is (64, 50). Diff from (32, 16) is (32, 34).
            // Tangent Up (0, -1). Dot = -34.
            // Snail would want to go Down.
            
            for(int i=0; i<60; i++) // Increased frames for longer traversal
            {
                mob.Movement.Move(gameTime, mob, map);
                // Force sync rect because test mob
                ((TestMob)mob).SetRect(new Rectangle((int)mob.GetPosition().X, (int)mob.GetPosition().Y, 16, 16));
            }
            
            // Assert we are past the pit (X > 48)
            Assert.True(mob.GetPosition().X > 48, $"Snail stuck at {mob.GetPosition()}.");
        }
    }
}
