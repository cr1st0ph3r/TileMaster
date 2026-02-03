using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Misc;

namespace TileMaster.Manager
{
    public class DamageNumberManager
    {
        private List<DamageNumber> _damageNumbers = new List<DamageNumber>();

        public void Add(Vector2 position, int damage)
        {
            // Determine color based on damage (white to red gradient)
            // Regular damage is white, higher damage shifts towards red.
            // threshold for "higher than normal" gradient
            float t = MathHelper.Clamp((damage - 5) / 20f, 0, 1); 
            Color color = Color.Lerp(Color.White, Color.Red, t);

            _damageNumbers.Add(new DamageNumber(position, damage.ToString(), color, 1.2f));
        }

        public void Add(Vector2 position, string text, Color color)
        {
            _damageNumbers.Add(new DamageNumber(position, text, color, 1.2f));
        }

        public void Update(GameTime gameTime)
        {
            for (int i = _damageNumbers.Count - 1; i >= 0; i--)
            {
                var dn = _damageNumbers[i];
                dn.Update(gameTime);
                if (dn.LifeTime <= 0)
                {
                    _damageNumbers.RemoveAt(i);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            foreach (var dn in _damageNumbers)
            {
                // Measure string to center it horizontally
                Vector2 size = font.MeasureString(dn.Text);
                Vector2 centeredPos = dn.Position - new Vector2(size.X / 2f, 0);

                // Draw with a slight shadow for better visibility
                spriteBatch.DrawString(font, dn.Text, centeredPos + new Vector2(1, 1), Color.Black * dn.Alpha);
                spriteBatch.DrawString(font, dn.Text, centeredPos, dn.Color * dn.Alpha);
            }
        }
    }
}
