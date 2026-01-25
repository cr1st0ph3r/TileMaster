using Microsoft.Xna.Framework;
using TileMaster.Helper;

namespace TileMaster.Entity.MobMovement
{
    public class Hop : Movement
    {
        private float hopTimer = 0f;
        private const float HopInterval = 0.8f; // Time between hops
        private bool isHopping = false;

        public Hop()
        {
            CanJump = true;
        }

        public override void Move(GameTime gameTime, Mob mob, Map.Map map)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update hop timer
            hopTimer += dt;

            float targetVelX = 0;
            if (mob.Target != null)
            {
                // Calculate direction towards target
                Vector2 targetPos = mob.Target.GetPosition();
                Vector2 mobPos = mob.GetPosition();

                if (targetPos.X > mobPos.X + 10) // Tolerance to prevent jitter
                {
                    targetVelX = mob.MoveSpeed;
                }
                else if (targetPos.X < mobPos.X - 10)
                {
                    targetVelX = -mob.MoveSpeed;
                }

                // Apply acceleration/friction (Lerp toward target velocity)
                float accel = mob.IsOnSolidBlock ? 10f : 2f; // Faster on ground, slower in air
                mob.velocity.X = MathHelper.Lerp(mob.velocity.X, targetVelX, accel * dt);

                // Initiate hop periodically if on ground and moving toward target
                if (mob.IsOnSolidBlock && !isHopping && hopTimer >= HopInterval && targetVelX != 0)
                {
                    mob.velocity.Y = -mob.JumpVelocity;
                    mob.IsOnSolidBlock = false;
                    isHopping = true;
                    hopTimer = 0f;
                }
            }
            else
            {
                // Decelerate if no target
                float accel = mob.IsOnSolidBlock ? 10f : 2f;
                mob.velocity.X = MathHelper.Lerp(mob.velocity.X, 0, accel * dt);
            }

            // Reset hop state when landed
            if (mob.IsOnSolidBlock && isHopping)
            {
                isHopping = false;
            }

            // 1.5 Check Ground (Prevent floating over pits)
            bool shouldFall = InputHelper.HandleMovingDown(mob, map);
            mob.IsOnSolidBlock = !shouldFall;

            // 2. Apply Gravity/Vertical Velocity
            if (!mob.IsOnSolidBlock && mob.velocity.Y < mob.MaxFallSpeed)
            {
                mob.velocity.Y += Entity.Gravity * dt;
            }

            // 3. Collision Resolution - Horizontal
            Vector2 position = mob.GetPosition();
            Rectangle rectangle = mob.GetRectangle();

            float newX = position.X + mob.velocity.X * dt;
            Rectangle testRectX = new Rectangle((int)newX, (int)position.Y, rectangle.Width, rectangle.Height);

            if (mob.IsRectCollidingWithMap(testRectX, map, out int hitTileX, out int hitTileY, findRightmost: mob.velocity.X < 0))
            {
                // collided on X axis: clamp/stop
                if (mob.velocity.X > 0)
                {
                    mob.SetPosition(new Vector2(hitTileX * Global.TileSize - rectangle.Width, position.Y));
                }
                else if (mob.velocity.X < 0)
                {
                    mob.SetPosition(new Vector2((hitTileX + 1) * Global.TileSize, position.Y));
                }
                mob.velocity.X = 0f;
                
                // Auto-jump logic for obstacles
                if (mob.IsOnSolidBlock && CanJump && !isHopping) 
                {
                    mob.velocity.Y = -mob.JumpVelocity; 
                    mob.IsOnSolidBlock = false;
                    isHopping = true;
                    hopTimer = 0f;
                }
            }
            else
            {
                mob.SetPosition(new Vector2(newX, position.Y));
            }
            
            // Re-read position for Y calculation
            position = mob.GetPosition();

            // 4. Collision Resolution - Vertical
            float newY = position.Y + mob.velocity.Y * dt;
            Rectangle testRectY = new Rectangle((int)position.X, (int)newY, rectangle.Width, rectangle.Height);

            if (mob.IsRectCollidingWithMap(testRectY, map, out hitTileX, out hitTileY, findBottommost: mob.velocity.Y < 0))
            {
                if (mob.velocity.Y > 0)
                {
                    // Landed
                    mob.SetPosition(new Vector2(position.X, hitTileY * Global.TileSize - rectangle.Height));
                    mob.IsOnSolidBlock = true;
                }
                else if (mob.velocity.Y < 0)
                {
                    // Head bump
                    mob.SetPosition(new Vector2(position.X, (hitTileY + 1) * Global.TileSize));
                }
                mob.velocity.Y = 0f;
            }
            else
            {
                mob.SetPosition(new Vector2(position.X, newY));
                if (mob.velocity.Y > 0) mob.IsOnSolidBlock = false;
            }

            // 5. Update Grid status
            mob.UpdateGridPosition();             
        }
    }
}
