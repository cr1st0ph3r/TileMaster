using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Entity;

namespace TileMaster.Misc
{
    public class ScrollingBackground : Component
    {
        private bool _constantSpeed;

        private float _layer;

        private float _scrollingSpeed;

        private List<Sprite.Sprite> _sprites;

        private readonly Player _player;

        private float _speed;

        public float Layer
        {
            get { return _layer; }
            set
            {
                _layer = value;

                foreach (var sprite in _sprites)
                    sprite.Layer = _layer;
            }
        }

        public ScrollingBackground(Texture2D texture, Player player, float scrollingSpeed, bool constantSpeed = false)
          : this(new List<Texture2D>() { texture, texture, texture, texture }, player, scrollingSpeed, constantSpeed)
        {

        }

        public ScrollingBackground(List<Texture2D> textures, Player player, float scrollingSpeed, bool constantSpeed = false)
        {
            _player = player;

            _sprites = new List<Sprite.Sprite>();

            for (int i = 0; i < textures.Count; i++)
            {
                var texture = textures[i];

                // Position sprites exactly next to each other (no odd Math.Min trick that created off-by-one drift)
                _sprites.Add(new Sprite.Sprite(texture)
                {
                    // TODO apply the correct height so the textures are close to the groundlevel
                    Position = new Vector2(i * texture.Width, Global.WindowHeight - texture.Height + 800),
                });
            }

            _scrollingSpeed = scrollingSpeed;

            _constantSpeed = constantSpeed;
        }



        private void ApplySpeed(GameTime gameTime)
        {
            _speed = (float)(_scrollingSpeed * gameTime.ElapsedGameTime.TotalSeconds);

            if (!_constantSpeed || _player.velocity.X > 0)
                _speed *= (_player.velocity.X / 20);

            foreach (var sprite in _sprites)
            {
                sprite.X -= _speed;
            }
        }

        private void CheckPosition()
        {
            for (int i = 0; i < _sprites.Count; i++)
            {
                var sprite = _sprites[i];

                if (sprite.Rectangle.Right <= 0)
                {
                    // Find the current rightmost sprite (robust regardless of list order)
                    int rightmostIndex = 0;
                    int maxRight = _sprites[0].Rectangle.Right;
                    for (int j = 1; j < _sprites.Count; j++)
                    {
                        if (_sprites[j].Rectangle.Right > maxRight)
                        {
                            maxRight = _sprites[j].Rectangle.Right;
                            rightmostIndex = j;
                        }
                    }

                    // Place this off-screen sprite immediately to the right of the rightmost one.
                    // Using exact right edge avoids accumulating gaps when the world is large.
                    sprite.X = _sprites[rightmostIndex].Rectangle.Right;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            ApplySpeed(gameTime);

            CheckPosition();
        }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (var sprite in _sprites)
                sprite.Draw(gameTime, spriteBatch);
        }
    }
}