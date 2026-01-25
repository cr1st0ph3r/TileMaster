using Microsoft.Xna.Framework;
using System;

namespace TileMaster.Misc
{
    public class DamageNumber
    {
        public Vector2 Position { get; set; }
        public string Text { get; set; }
        public Color Color { get; set; }
        public float Alpha { get; set; } = 1.0f;
        public Vector2 Velocity { get; set; }
        public float LifeTime { get; set; } // in seconds
        public float MaxLifeTime { get; private set; }

        public DamageNumber(Vector2 position, string text, Color color, float lifeTime)
        {
            Position = position;
            Text = text;
            Color = color;
            LifeTime = lifeTime;
            MaxLifeTime = lifeTime;
            Velocity = new Vector2(0, -40f); // Float upwards slowly
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;
            LifeTime -= dt;
            
            // Fade out as lifetime decreases
            Alpha = MathHelper.Clamp(LifeTime / MaxLifeTime, 0, 1);
        }
    }
}
