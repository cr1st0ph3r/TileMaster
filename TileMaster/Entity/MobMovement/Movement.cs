using Microsoft.Xna.Framework;

namespace TileMaster.Entity.MobMovement
{
    public abstract class Movement
    {
        /// <summary>
        /// Whether the movement type can jump or not.
        /// </summary>
        public bool CanJump { get; set; }

        /// <summary>
        /// Moves the mob.
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="mob"></param>
        /// <param name="map"></param>
        public abstract void Move(GameTime gameTime, Mob mob, Map.Map map);
    }
}
