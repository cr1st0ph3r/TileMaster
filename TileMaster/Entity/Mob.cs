using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TileMaster.Entity.Enums;
using TileMaster.Entity.MobMovement;

namespace TileMaster.Entity
{
    public class Mob : Entity
    {
        public Entity Target { get; set; }
        public Movement Movement { get; set; }
        public void Load(ContentManager content, Vector2 position, string name,float movespeed,Movement movement)
        {
            texture = content.Load<Texture2D>(name);
            rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
            this.position = position;
            MoveSpeed = movespeed;
            Movement = movement;
        }
        public override void Update(GameTime gameTime, Map.Map map)
        {
            if (Game._state == GameState.Running)
            {
                Movement.Move(gameTime, this, map);
                base.Update(gameTime, map);
            }
        }
    }
}
