using Microsoft.Xna.Framework;

namespace TileMaster.Entity.MobMovement
{
    public abstract class Movement
    {
        public bool CanJump { get; set; }
        public abstract void Move(GameTime gameTime, Mob mob, Map.Map map);
    }
}
