using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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

                _sprites.Add(new Sprite.Sprite(texture)
                {
                    //TODO aplicar a altura correta para que a textura seja alocada proximo do groundlevel
                    //apply the correct height so the textures are close to the groundlevel
                    //replace 800 with the said parameter
                    Position = new Vector2(i * texture.Width - Math.Min(i, i + 1), Global.WindowHeight - texture.Height + 800),
                });
            }

            _scrollingSpeed = scrollingSpeed;

            _constantSpeed = constantSpeed;
        }



        private void ApplySpeed(GameTime gameTime)
        {
            _speed = (float)(_scrollingSpeed * gameTime.ElapsedGameTime.TotalSeconds);

            if (!_constantSpeed || _player.velocity.X > 0)
                _speed *= (_player.velocity.X/20);

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
                    var index = i - 1;

                    if (index < 0)
                        index = _sprites.Count - 1;

                    sprite.X = _sprites[index].Rectangle.Right - (_speed * 2);
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
