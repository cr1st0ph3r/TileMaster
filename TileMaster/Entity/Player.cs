using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileMaster.Helper;

namespace TileMaster.Entity
{
    public class Player
    {
        //for the all that is holy encapsulate this later
        private Texture2D texture;
        private Vector2 position = new Vector2(Global.MapWidth*Global.Tilesize / 2, (Global.GroundLevel - 20)*Global.Tilesize);
 

        public Vector2 velocity;
        private Rectangle rectangle;
        public int SteppingOn;
        public int onBlock;
        public int onChunk;
        public int GridX;
        public int GridY;
        private bool hasJumped = false;
        public bool isMoving = false; 
        public bool isOnSolidBlock = false;

        public Vector2 Position
        {
            get { return position; }

        }
        public Rectangle Rectangle
        {
            get { return rectangle; }

        }
        public Player()
        {
        }

        public void Load(ContentManager content)
        {
            texture = content.Load<Texture2D>("Player");
        }

        public void Update(GameTime gameTime, Player player, Map map)
        {

            position += velocity;
            rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
            Input(gameTime, player, map);

            //constantly pulls the player down
            if (velocity.Y < 10 && !isOnSolidBlock)
            {
                velocity.Y +=(float)(0.4F * 60 * gameTime.ElapsedGameTime.TotalSeconds);
            }

            //set is the player is in motion or not
            if (velocity.X > 0 || velocity.Y > 0.4f || velocity.X < 0 || velocity.Y < -0.4f)
            {
                isMoving = true;
            }
            else isMoving = false;

        }

        public void Input(GameTime gameTime, Player player, Map map)
        {

            //mover para direita
            if (InputHelper.HandleMovingRight(player, map))
            {
                velocity.X = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 3);
            }

            //mover para esquerda
            else if (InputHelper.HandleMovingLeft(player, map))
            {
                velocity.X = -(float)gameTime.ElapsedGameTime.TotalMilliseconds / 3;
            }
           


            //momento linear esquerda direita
            if (velocity.X > 0.4F)
            {
                velocity.X -= (float)(0.4F * 60 * gameTime.ElapsedGameTime.TotalSeconds);
            }
            else if (velocity.X < -0.4F)
            {
                velocity.X += (float)(0.4F * 60 * gameTime.ElapsedGameTime.TotalSeconds);
            }
            else { velocity.X = 0; }

            //queda do player
            if (!InputHelper.HandleMovingDown(player, map))
            {
                velocity.Y = 0;              
                isOnSolidBlock = true;
                hasJumped = false;
               
            }
            else
            {
                isOnSolidBlock = false;
            }


            if (Keyboard.GetState().IsKeyDown(Keys.Space) && hasJumped == false)
            {
                position.Y -= 5F;
                velocity.Y = -9f;
                hasJumped = true;
                isOnSolidBlock = false;
            }
          

            if (isOnSolidBlock)
            {
                //TODO criar transicao para definir a posicao de y gradativamente
                //pois atualment está fazendo de forma muito agressiva
                player.position.Y = ((player.GridY) * 16) - 1;
            }

            if (hasJumped)
            {
                if (!InputHelper.HandleJump(player, map))
                {
                    velocity.Y = 0.4F;
                    position.Y += 5F;
                }
            }
          

        }
        public void CheckBoundaries()
        {
            
            //keep player inside boundaries
            if (position.X < 0)
            {
                position.X = 0; 
            }
            if (position.Y < 0)
            {
                position.Y = 0;
            }
            if (position.Y > ((Global.MapHeight-2/*why 2? beats me*/)*Global.Tilesize))
            {
                position.Y = (((Global.MapHeight - 2) * Global.Tilesize)-10);
               
            }
        }
        public void Collision(Rectangle newRectangle, int xOffset, int yOffset)
        {
            if (rectangle.TouchTopOf(newRectangle))
            {
                rectangle.Y = newRectangle.Y - rectangle.Height;
                velocity.Y = 0f;
                hasJumped = false;
            }
            if (rectangle.TouchLeftOf(newRectangle))
            {
                position.X = newRectangle.X - rectangle.Width - 2;/*change this value for scalling the tiles*/
            }
            if (rectangle.TouchRightOf(newRectangle))
            {
                position.X = newRectangle.X + rectangle.Width + 2;/*change this value for scalling the tiles*/
            }
            if (rectangle.TouchBottomOf(newRectangle))
            {
                velocity.Y = 1f;
            }

            CheckBoundaries();

        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, rectangle, Color.White);
        }
    }
}
