using Microsoft.Xna.Framework;

namespace TileMaster.Entity.MobMovement
{
    public class Snail : Movement
    {
        private Vector2 _gravityDir = new Vector2(0, 1); // Points towards the surface
        private float _currentRotation = 0f;

        public Snail()
        {
            CanJump = false;
        }

        private float _lastMoveDir = 0;

        public override void Move(GameTime gameTime, Mob mob, Map.Map map)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (mob.Texture != null)
            {
                mob.Origin = new Vector2(mob.Texture.Width / 2f, mob.Texture.Height / 2f);
            }

            // 1. Check Ground Status First
            Rectangle bounds = mob.GetRectangle();
            Vector2 gravCheckPos = mob.GetPosition() + _gravityDir * 5f;
            Rectangle gravRect = new Rectangle((int)gravCheckPos.X, (int)gravCheckPos.Y, bounds.Width, bounds.Height);
            bool onGround = mob.IsRectCollidingWithMap(gravRect, map, out int gHitX, out int gHitY);

            // 2. Decide Mode: Attached vs Air
            if (onGround)
            {
                HandleAttachedMovement(dt, mob, map, bounds, gHitX, gHitY);
            }
            else
            {
                HandleAirMovement(dt, mob, map, bounds);
            }

            // 3. Apply Rotation
            mob.Rotation = _currentRotation;     
        }
        private void HandleAttachedMovement(float dt, Mob mob, Map.Map map, Rectangle bounds, int gHitX, int gHitY)
        {
            // 1. Fixed Direction logic
            // We default to 1 (right/forward) if no direction is set.
            if (_lastMoveDir == 0) _lastMoveDir = 1;

            // 2. Tangent Calculation
            // Tangent is always relative to our current gravity/attachment
            Vector2 tangentDir = new Vector2(_gravityDir.Y, -_gravityDir.X);

            float speed = mob.MoveSpeed * 0.5f;
            Vector2 targetVelocity = tangentDir * _lastMoveDir * speed;

            // Lerp towards target velocity to allow knockback to persist
            mob.velocity = Vector2.Lerp(mob.velocity, targetVelocity, 10f * dt);

            Vector2 position = mob.GetPosition();
            Vector2 nextPos = position + mob.velocity * dt;

            // 3. Simple Forward Collision (Concave Corners)
            Rectangle nextRect = new Rectangle((int)nextPos.X, (int)nextPos.Y, bounds.Width, bounds.Height);
            if (mob.IsRectCollidingWithMap(nextRect, map, out int hitX, out int hitY))
            {
                // We hit a wall. Instead of stopping, we "climb" it.
                // We snap to the wall and our new gravity becomes the direction we were just moving.
                SnapToWall(mob, hitX, hitY, tangentDir, position, bounds);
 
                // New gravity is into the wall we just hit
                _gravityDir = Vector2.Normalize(mob.velocity);

                // Stop current velocity so we don't 'jitter' through the wall
                mob.velocity = Vector2.Zero;
            }
            else
            {
                mob.SetPosition(nextPos);
            }

            // Update visuals
            UpdateRotation(map.GetTileAt(gHitX, gHitY));
        }

        private void HandleAirMovement(float dt, Mob mob, Map.Map map, Rectangle bounds)
        {
            // We are in the air.
            // Check if we just walked off a ledge (Convex Corner).
            // We verify this by looking "Diagonally Inwards" from our previous movement.

            bool foundCorner = false;

            if (_lastMoveDir != 0)
            {
                Vector2 tangentDir = new Vector2(_gravityDir.Y, -_gravityDir.X);
                Vector2 backTangent = -Vector2.Normalize(tangentDir * _lastMoveDir);
                Vector2 diagonalDir = Vector2.Normalize(_gravityDir + backTangent);

                // Look for block in diagonal direction
                // Shift check rect by enough to hit the block we just left
                Vector2 offset = diagonalDir * 10f;

                Rectangle checkRect = new Rectangle((int)(mob.GetPosition().X + offset.X), (int)(mob.GetPosition().Y + offset.Y), bounds.Width, bounds.Height);
                if (mob.IsRectCollidingWithMap(checkRect, map, out int cHitX, out int cHitY))
                {
                    // Found corner block!
                    foundCorner = true;

                    _gravityDir = backTangent; // New Gravity is towards the side face
                    mob.velocity = Vector2.Zero; // Stop fall

                    // Precise Snap to Surface
                    // We must overlap the block (cHitX, cHitY) we just attached to.
                    // Adjust Position to sit on the surface defined by New Gravity.

                    float newX = mob.GetPosition().X;
                    float newY = mob.GetPosition().Y;

                    // If Gravity is Horizontal (Sticking to wall)
                    if (System.Math.Abs(_gravityDir.X) > 0.5f)
                    {
                        // Snap X to wall face
                        if (_gravityDir.X > 0) newX = cHitX * Global.TileSize - bounds.Width; // Left Face (Grav points Right) -> Wait. Grav points TO Wall.
                                                                                              // If Grav=(1,0), Wall is Right. Snap to Left side of wall.
                                                                                              // Wait, HitX/Y is the tile.
                                                                                              // If Grav(1,0), we are on Left side of tile.
                                                                                              // X = hitX * 16 - Width. CORRECT.
                        else newX = (cHitX + 1) * Global.TileSize; // Right Face (Grav points Left) -> Snap to Right side.

                        // Clamp Y to overlap the block
                        // Ensure top/bottom overlaps
                        float blockTop = cHitY * Global.TileSize;
                        float blockBottom = (cHitY + 1) * Global.TileSize;
                        // Center snail on block center? Or clamp? 
                        // Let's just Clamp to ensure significant overlap (at least 4px?)
                        float min = blockTop - bounds.Height + 4;
                        float max = blockBottom - 4;
                        newY = MathHelper.Clamp(newY, min, max);
                    }
                    else // Gravity Vertical (Ceiling/Floor)
                    {
                        // Snap Y to surface
                        if (_gravityDir.Y > 0) newY = cHitY * Global.TileSize - bounds.Height; // Top Face (Grav Down) -> Snap to top.
                        else newY = (cHitY + 1) * Global.TileSize; // Bottom Face (Grav Up) -> Snap to bottom.

                        // Clamp X
                        float blockLeft = cHitX * Global.TileSize;
                        float blockRight = (cHitX + 1) * Global.TileSize;
                        float min = blockLeft - bounds.Width + 4;
                        float max = blockRight - 4;
                        newX = MathHelper.Clamp(newX, min, max);
                    }

                    mob.SetPosition(new Vector2(newX, newY));

                    UpdateRotation(null);
                }
            }

            if (!foundCorner)
            {
                // True Air / Falling
                // Reset Gravity to Global Down
                _gravityDir = new Vector2(0, 1);

                // Apply Global Gravity
                mob.velocity.Y += 1000f * dt; // Gravity
                
                // Gradually slow down horizontal movement if in air (friction)
                mob.velocity.X = MathHelper.Lerp(mob.velocity.X, 0, 2f * dt);

                Vector2 nextPos = mob.GetPosition() + mob.velocity * dt;

                Rectangle nextRect = new Rectangle((int)nextPos.X, (int)nextPos.Y, bounds.Width, bounds.Height);
                if (mob.IsRectCollidingWithMap(nextRect, map, out int hitX, out int hitY))
                {
                    // Hit floor
                    if (mob.velocity.Y > 0)
                    {
                        mob.SetPosition(new Vector2(nextPos.X, hitY * Global.TileSize - bounds.Height));
                        mob.velocity.Y = 0;
                    }
                }
                else
                {
                    mob.SetPosition(nextPos);
                }

                _currentRotation = 0;
            }
        }

        private void UpdateRotation(TileMaster.Entity.Tiles.CollisionTile groundTile)
        {
            if (groundTile != null && groundTile.IsSlope)
            {
                float baseRot = (float)System.Math.Atan2(_gravityDir.Y, _gravityDir.X) - MathHelper.PiOver2;
                float slopeOffset = (groundTile.SlopeRotation == 0 || groundTile.SlopeRotation == 2) ? MathHelper.PiOver4 : -MathHelper.PiOver4;
                _currentRotation = baseRot + slopeOffset;
            }
            else
            {
                _currentRotation = (float)System.Math.Atan2(_gravityDir.Y, _gravityDir.X) - MathHelper.PiOver2;
            }
        }

        private void SnapToWall(Mob mob, int hitX, int hitY, Vector2 tangentDir, Vector2 position, Rectangle bounds)
        {
            if (tangentDir.X > 0.5f) mob.SetPosition(new Vector2(hitX * Global.TileSize - bounds.Width, position.Y));
            else if (tangentDir.X < -0.5f) mob.SetPosition(new Vector2((hitX + 1) * Global.TileSize, position.Y));
            else if (tangentDir.Y > 0.5f) mob.SetPosition(new Vector2(position.X, hitY * Global.TileSize - bounds.Height));
            else if (tangentDir.Y < -0.5f) mob.SetPosition(new Vector2(position.X, (hitY + 1) * Global.TileSize));
        }
    }
}
