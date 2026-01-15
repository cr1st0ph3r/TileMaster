using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileMaster.Entity.Enums;
using TileMaster.Helper;

namespace TileMaster.Entity
{
    public class Player : Entity
    {
        public Layer Layer { get; set; } = Layer.Surface;
        public bool InterruptInput { get; set; }
        public Player()
        {   //the height of the player in blocks
            this.Height = 3;
        }

        public void Load(ContentManager content)
        {
            texture = content.Load<Texture2D>("Entities/Player/Player");
        }

        public override void Update(GameTime gameTime, Map.Map map)
        {
            if (Game._state == GameState.Running && Global.IsMapLoaded)
            {
                // compute current grid indices from current position (needed by InputHelper)
                int playerOnGridX = (int)((GetPosition().X + (GetRectangle().Width / 2)) / Global.TileSize);
                int playerOnGridY = (int)((GetPosition().Y + GetRectangle().Height - 1) / Global.TileSize); // bottom tile index

                onBlock = (playerOnGridY * Global.MapWidth) + (playerOnGridX);
                SteppingOn = (onBlock + Global.MapWidth);
                GridX = playerOnGridX;
                // GridY should refer to the tile row at the player's feet (bottom-most pixel)
                GridY = playerOnGridY;

                int playerChunkX = (GridX / Global.ChunkSize);
                int playerChunkY = (GridY / Global.ChunkSize);
                onChunk = (1/*chunks are 1 based*/+ ((playerChunkY * (Global.MapWidth / Global.ChunkSize)) + playerChunkX));

                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // process input first (decides velocity / intent)
                if (!InterruptInput)
                {
                    Input(gameTime, this, map);
                }


                // Ground detection: use the same helper used elsewhere but keep it
                // out of Input() to avoid duplicate snapping logic. HandleMovingDown
                // returns true if the player should fall (no support under feet).
                bool shouldFall = InputHelper.HandleMovingDown(this, map);
                isOnSolidBlock = !shouldFall;

                // gravity (time-based) - applied to velocity before integration
                if (velocity.Y < MaxFallSpeed && !isOnSolidBlock)
                {
                    velocity.Y += Gravity * dt;
                }

                // per-axis integration with collision resolution to avoid tunneling
                // Horizontal movement
                float newX = position.X + velocity.X * dt;
                Rectangle testRectX = new Rectangle((int)newX, (int)position.Y, texture.Width, texture.Height);

                if (IsRectCollidingWithMap(testRectX, map, out int hitTileX, out int hitTileY, findRightmost: velocity.X < 0))
                {
                    // collided on X axis: clamp to tile edge and stop horizontal velocity
                    if (velocity.X > 0)
                    {
                        // moving right: place player's right edge to the left side of the tile we hit
                        position.X = hitTileX * Global.TileSize - texture.Width;
                    }
                    else if (velocity.X < 0)
                    {
                        // moving left: place player's left edge to the right side of the tile we hit
                        position.X = (hitTileX + 1) * Global.TileSize;
                    }
                    velocity.X = 0f;
                }
                else
                {
                    position.X = newX;
                }

                // Vertical movement
                float newY = position.Y + velocity.Y * dt;
                Rectangle testRectY = new Rectangle((int)position.X, (int)newY, texture.Width, texture.Height);

                if (IsRectCollidingWithMap(testRectY, map, out hitTileX, out hitTileY, findBottommost: velocity.Y < 0))
                {
                    // collided on Y axis: clamp and stop vertical velocity
                    if (velocity.Y > 0)
                    {
                        // falling: place player's bottom on top of the tile
                        position.Y = hitTileY * Global.TileSize - texture.Height;
                        isOnSolidBlock = true;
                        hasJumped = false;
                    }
                    else if (velocity.Y < 0)
                    {
                        // rising: place player's top below the tile
                        position.Y = (hitTileY + 1) * Global.TileSize;
                    }
                    velocity.Y = 0f;
                }
                else
                {
                    position.Y = newY;
                    // if we are moving down and didn't hit anything, we are not on solid ground
                    if (velocity.Y > 0) isOnSolidBlock = false;
                }

                // update rectangle after applying resolved position
                rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);

                // small conditional snap to ground to avoid tiny floating above tiles (keeps previous behavior)
                if (isOnSolidBlock)
                {
                    // Snap only when the player's bottom is very near the tile top.
                    // Avoid using GridY (which may be stale or computed differently); compute from rectangle instead.
                    var bottom = position.Y + rectangle.Height;
                    int tileBelow = (int)(bottom / Global.TileSize);
                    float tileTop = tileBelow * Global.TileSize;
                    float delta = tileTop - bottom; // negative if penetrating

                    const float snapTolerance = 3f; // pixels
                    if (Math.Abs(delta) <= snapTolerance)
                    {
                        position.Y = tileTop - rectangle.Height;
                        velocity.Y = 0f;
                        hasJumped = false;
                        rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
                    }
                }

                // update grid indices to reflect new position
                GridX = (int)((GetPosition().X + (GetRectangle().Width / 2)) / Global.TileSize);
                // make GridY reflect the tile row containing the player's feet (bottom-most pixel)
                GridY = (int)((GetPosition().Y + GetRectangle().Height - 1) / Global.TileSize);

                int newChunkX = GridX / Global.ChunkSize;
                int newChunkY = GridY / Global.ChunkSize;
                onChunk = (1 + ((newChunkY * (Global.MapWidth / Global.ChunkSize)) + newChunkX));
                onBlock = (GridY * Global.MapWidth) + GridX;
                SteppingOn = onBlock + Global.MapWidth;

                // set layers
                // Sky: > 50 blocks above GroundLevel (GridY is smaller than GroundLevel)
                // Surface: +/- 50 blocks from GroundLevel
                // Caverns: 50 to 150 blocks below GroundLevel
                // Underground: 150 to 300 blocks below GroundLevel
                // Underworld: > 300 blocks below GroundLevel

                int heightDelta = GridY - Global.GroundLevel;

                if (heightDelta <= -50)
                {
                    Layer = Layer.Sky;
                }
                else if (heightDelta >= 300)
                {
                    Layer = Layer.Underworld;
                }
                else if (heightDelta >= 150)
                {
                    Layer = Layer.Underground;
                }
                else if (heightDelta >= 50)
                {
                    Layer = Layer.Caverns;
                }
                else
                {
                    Layer = Layer.Surface;
                }

                // set if the player is in motion or not
                if (velocity.X > 0.01f || velocity.Y > 0.4f || velocity.X < -0.01f || velocity.Y < -0.4f)
                {
                    isMoving = true;
                }
                else isMoving = false;
            }
        }

        public void Input(GameTime gameTime, Player player, Map.Map map)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // move right
            if (InputHelper.HandleMovingRight(player, map))
            {
                velocity.X = MoveSpeed;
            }

            // move left
            else if (InputHelper.HandleMovingLeft(player, map))
            {
                velocity.X = -MoveSpeed;
            }

            // linear momentum left/right (friction)
            if (velocity.X > 0.4F)
            {
                velocity.X -= Friction * dt;
                if (velocity.X < 0f) velocity.X = 0f;
            }
            else if (velocity.X < -0.4F)
            {
                velocity.X += Friction * dt;
                if (velocity.X > 0f) velocity.X = 0f;
            }
            else { velocity.X = 0; }

            // handle player jump (jump impulse is in px/s)
            // only allow a jump when we believe we are on solid ground
            if (Keyboard.GetState().IsKeyDown(Keys.Space) && hasJumped == false && isOnSolidBlock)
            {
                // small positional tweak to avoid immediate collision
                position.Y -= 5F;
                velocity.Y = -JumpVelocity;
                hasJumped = true;
                isOnSolidBlock = false;
            }
            if (hasJumped)
            {
                if (!InputHelper.HandleJump(player, map))
                {
                    // collision while jumping: cancel upward motion and nudge down
                    velocity.Y = 0f;
                    position.Y += 5F;
                    hasJumped = false;
                }
            }
        }
    }
}