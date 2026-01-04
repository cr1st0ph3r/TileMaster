using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileMaster.Helper;

namespace TileMaster.Entity
{
    public class Player:Entity
    {    
        public Player(){
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
                //updates the player info about block positioning
                int playerOnGridX = (int)((player.GetPosition().X + (player.GetRectangle().Width / 2)) / Global.TileSize);
                int playerOnGridY = (int)((player.GetPosition().Y + (player.GetRectangle().Height)) / Global.TileSize);
                player.onBlock = (playerOnGridY * Global.MapWidth) + (playerOnGridX);
                player.SteppingOn = (int)(player.onBlock + Global.MapWidth);
                player.GridX = (int)((player.GetPosition().X + (player.GetRectangle().Width / 2)) / Global.TileSize);
                player.GridY = (int)((player.GetPosition().Y + (player.GetRectangle().Height / player.Height)) / Global.TileSize);
                int playerChunkX = (int)(player.GridX / Global.ChunkSize);
                int playerChunkY = (int)(player.GridY / Global.ChunkSize);
                player.onChunk = (1/*chunks are 1 based*/+ ((playerChunkY * (Global.MapWidth / Global.ChunkSize)) + playerChunkX));

                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

                // integrate position using velocity in px/s
                position += velocity * dt;

                rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
                Input(gameTime, player, map);

                // gravity (time-based)
                if (velocity.Y < MaxFallSpeed && !isOnSolidBlock)
                {
                    velocity.Y += Gravity * dt;
                }

                // small conditional snap to ground to avoid tiny floating above tiles
                // only snap if player is marked on ground and within a small pixel threshold
                if (isOnSolidBlock)
                {
                    // compute the expected Y for the player's top so player's bottom sits on tile top
                    float targetY = (player.GridY) * Global.TileSize -0.1f;
                    position.Y = (int)targetY;
                    velocity.Y = 0f;
                    hasJumped = false;
                    // update rectangle after changing position
                    rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
                }

                // set is the player is in motion or not
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

            // ground detection
            if (!InputHelper.HandleMovingDown(player, map))
            {
                float targetY = (player.GridY) * Global.TileSize - 1; // matches previous layout
                float delta = targetY - position.Y;
                if (delta < 1)
                {
                    velocity.Y = 0;
                    isOnSolidBlock = true;
                    hasJumped = false;
                }
            }
            else
            {
                isOnSolidBlock = false;
            }

            // handle player jump (jump impulse is in px/s)
            if (Keyboard.GetState().IsKeyDown(Keys.Space) && hasJumped == false)
            {
                // small positional tweak to avoid immediate collision
                position.Y -= 5F;
                velocity.Y = -JumpVelocity;
                hasJumped = true;
                isOnSolidBlock = false;
            }

            if (isOnSolidBlock)
            {
                // removed abrupt snap to grid; handled with small conditional snap in Update
            }

            if (hasJumped)
            {
                if (!InputHelper.HandleJump(player, map))
                {
                    // collision while jumping: cancel upward motion and nudge down
                    velocity.Y = 0f;
                    position.Y += 5F;
                }
            }
        }       
    }
}