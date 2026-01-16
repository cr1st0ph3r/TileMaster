using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileMaster.Manager;

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
        public const float Gravity = 1000f;       // px/s^2
        public float MaxFallSpeed = 1000f;  // px/s
        public float JumpVelocity = 350f;   // px/s (initial upward velocity)
        public float MoveSpeed = 600f;      // px/s (horizontal)
        protected const float Friction = 400f;       // px/s^2 (deceleration)

        protected AnimationManager _animationManager;

        public Rectangle GetRectangle()
        {
            return rectangle;
        }

        public Vector2 GetPosition()
        {
            return position;
        }

        public void SetPosition(Vector2 newPos)
        {
            position = newPos;
            if (texture != null)
                rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
        }

        public virtual void Update(GameTime gameTime, Map.Map map)
        {
        }

        // checks whether 'rect' overlaps any occupied tile in the map.
        // If a collision is found, returns true and outputs the tile coordinates (tileX, tileY) of the first colliding tile.
        internal bool IsRectCollidingWithMap(Rectangle rect, Map.Map map, out int tileX, out int tileY, bool findRightmost = false, bool findBottommost = false)
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

            bool found = false;

            // If we need to find the rightmost or bottommost, we should iterate in reverse order
            // or just keep updating tileX/tileY if a later one is found.
            // Iterating in standard order (top to bottom, left to right) means the FIRST found
            // is the topmost/leftmost. 
            // If findRightmost is true, we want the largest X.
            // If findBottommost is true, we want the largest Y.

            for (int y = topTile; y <= bottomTile; y++)
            {
                for (int x = leftTile; x <= rightTile; x++)
                {
                    // Use GetTileAt for direct access via the new array structure
                    var tile = map.GetTileAt(x, y);
                    if (tile != null && tile.IsOccupied)
                    {
                        if (!found)
                        {
                            tileX = x;
                            tileY = y;
                            found = true;
                        }
                        else
                        {
                            // if we already found one, update if these match the search criteria
                            if (findRightmost && x > tileX)
                            {
                                tileX = x;
                                tileY = y;
                            }
                            if (findBottommost && y > tileY)
                            {
                                tileY = y;
                                tileX = x;
                            }
                        }
                        
                        // if we aren't looking for specific ones, we can exit early
                        if (!findRightmost && !findBottommost) return true;
                    }
                }
            }

            return found;
        }

        public void UpdateGridPosition()
        {
            // update grid indices to reflect new position
            GridX = (int)((position.X + (rectangle.Width / 2)) / Global.TileSize);
            // make GridY reflect the tile row containing the entity's feet (bottom-most pixel)
            GridY = (int)((position.Y + rectangle.Height - 1) / Global.TileSize);

            int newChunkX = GridX / Global.ChunkSize;
            int newChunkY = GridY / Global.ChunkSize;
            onChunk = (1 + ((newChunkY * (Global.MapWidth / Global.ChunkSize)) + newChunkX));
            onBlock = (GridY * Global.MapWidth) + GridX;
            SteppingOn = onBlock + Global.MapWidth;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            //spriteBatch.Draw(texture, rectangle, Color.White);
            var flip = velocity.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            _animationManager.Draw(spriteBatch, flip);
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
