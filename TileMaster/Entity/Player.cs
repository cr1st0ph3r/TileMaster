using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileMaster.Helper;

namespace TileMaster.Entity
{
    public class Player : Entity
    {
        public Player()
        {
            this.Height = 3;//the height of the player in blocks
        }

        public Vector2 GetPosition()
        {
            return position;
        }

        public Rectangle GetRectangle()
        {
            return rectangle;
        }

        public void Load(ContentManager content)
        {
            texture = content.Load<Texture2D>("Player");
        }

        public override void Update(GameTime gameTime, Player player, Map.Map map)
        {
            if (Game._state == GameState.Running && Global.IsMapLoaded)
            {
                // compute current grid indices from current position (needed by InputHelper)
                int playerOnGridX = (int)((player.GetPosition().X + (player.GetRectangle().Width / 2)) / Global.TileSize);
                int playerOnGridY = (int)((player.GetPosition().Y + player.GetRectangle().Height - 1) / Global.TileSize); // bottom tile index

                player.onBlock = (playerOnGridY * Global.MapWidth) + (playerOnGridX);
                player.SteppingOn = (player.onBlock + Global.MapWidth);
                player.GridX = playerOnGridX;
                // GridY should refer to the tile row at the player's feet (bottom-most pixel)
                player.GridY = playerOnGridY;

                int playerChunkX = (player.GridX / Global.ChunkSize);
                int playerChunkY = (player.GridY / Global.ChunkSize);
                player.onChunk = (1/*chunks are 1 based*/+ ((playerChunkY * (Global.MapWidth / Global.ChunkSize)) + playerChunkX));

                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // process input first (decides velocity / intent)
                Input(gameTime, player, map);

                // Ground detection: use the same helper used elsewhere but keep it
                // out of Input() to avoid duplicate snapping logic. HandleMovingDown
                // returns true if the player should fall (no support under feet).
                bool shouldFall = InputHelper.HandleMovingDown(player, map);
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

                if (IsRectCollidingWithMap(testRectX, map, out int hitTileX, out int hitTileY))
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

                if (IsRectCollidingWithMap(testRectY, map, out hitTileX, out hitTileY))
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
                    // Avoid using player.GridY (which may be stale or computed differently); compute from rectangle instead.
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
                player.GridX = (int)((player.GetPosition().X + (player.GetRectangle().Width / 2)) / Global.TileSize);
                // make GridY reflect the tile row containing the player's feet (bottom-most pixel)
                player.GridY = (int)((player.GetPosition().Y + player.GetRectangle().Height - 1) / Global.TileSize);

                int newChunkX = player.GridX / Global.ChunkSize;
                int newChunkY = player.GridY / Global.ChunkSize;
                player.onChunk = (1 + ((newChunkY * (Global.MapWidth / Global.ChunkSize)) + newChunkX));
                player.onBlock = (player.GridY * Global.MapWidth) + player.GridX;
                player.SteppingOn = player.onBlock + Global.MapWidth;

                // set if the player is in motion or not
                if (velocity.X > 0.01f || velocity.Y > 0.4f || velocity.X < -0.01f || velocity.Y < -0.4f)
                {
                    isMoving = true;
                }
                else isMoving = false;
            }
        }

        // checks whether 'rect' overlaps any occupied tile in the map.
        // If a collision is found, returns true and outputs the tile coordinates (tileX, tileY) of the first colliding tile.
        private bool IsRectCollidingWithMap(Rectangle rect, Map.Map map, out int tileX, out int tileY)
        {
            tileX = -1;
            tileY = -1;

            int leftTile = rect.Left / Global.TileSize;
            int rightTile = (rect.Right - 1) / Global.TileSize;
            int topTile = rect.Top / Global.TileSize;
            int bottomTile = (rect.Bottom - 1) / Global.TileSize;

            // clamp tile coordinates to map bounds
            leftTile = MathHelper.Clamp(leftTile, 0, Global.MapWidth - 1);
            rightTile = MathHelper.Clamp(rightTile, 0, Global.MapWidth - 1);
            topTile = MathHelper.Clamp(topTile, 0, Global.MapHeight - 1);
            bottomTile = MathHelper.Clamp(bottomTile, 0, Global.MapHeight - 1);

            for (int y = topTile; y <= bottomTile; y++)
            {
                for (int x = leftTile; x <= rightTile; x++)
                {
                    // compute global tile index
                    int idx = (y * Global.MapWidth) + x;
                    // compute chunk id (1-based, same formula used elsewhere)
                    int chunkX = x / Global.ChunkSize;
                    int chunkY = y / Global.ChunkSize;
                    int chunkId = 1 + (chunkY * (Global.MapWidth / Global.ChunkSize)) + chunkX;

                    // ensure the chunk contains the block index and that it's occupied
                    if (map.IsBlockOnChunk(chunkId, idx))
                    {
                        if (map.ChunkDictionary[chunkId].Tiles[idx].IsOccupied)
                        {
                            tileX = x;
                            tileY = y;
                            return true;
                        }
                    }
                }
            }

            return false;
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

            // NOTE:
            // Ground detection that ran here previously caused a second, slightly different
            // determination of "isOnSolidBlock" which conflicted with the Y-axis collision
            // resolution performed later in Update(). That produced toggling of isOnSolidBlock
            // and tiny positional adjustments every frame (the observed jitter).
            //
            // We now rely on the vertical collision resolution performed in Update() to set
            // isOnSolidBlock and to clamp the player's Y position. Removing the duplicate
            // ground-check avoids oscillation.

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