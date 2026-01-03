using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TileMaster.Entity
{
    public class Entity
    {
        protected Texture2D texture;
        protected Rectangle rectangle;
        protected Vector2 position = new Vector2(Global.MapWidth * Global.TileSize / 2, (Global.GroundLevel - 20) * Global.TileSize);
        public Vector2 velocity;
        public int SteppingOn;
        public int onBlock;
        public int onChunk;
        public int GridX;
        public int GridY;
        protected bool hasJumped = false;
        public bool isMoving = false;
        public bool isOnSolidBlock = false;
        public int Height { get; protected set; }

        // physics constants (units: pixels, seconds)
        protected const float Gravity = 1000f;       // px/s^2
        protected const float MaxFallSpeed = 1000f;  // px/s
        protected const float JumpVelocity = 350f;   // px/s (initial upward velocity)
        protected const float MoveSpeed = 600f;      // px/s (horizontal)
        protected const float Friction = 400f;       // px/s^2 (deceleration)


        public virtual void Update(GameTime gameTime, Player player, Map.Map map)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, rectangle, Color.White);
        }

        public void CheckBoundaries()
        {
            //keep entity inside boundaries
            if (position.X < 0)
            {
                position.X = 0;
            }
            if (position.Y < 0)
            {
                position.Y = 0;
            }
            if (position.Y > ((Global.MapHeight - 2/*why 2? beats me*/) * Global.TileSize))
            {
                position.Y = (((Global.MapHeight - 2) * Global.TileSize) - 10);
            }
        }

    }
}
