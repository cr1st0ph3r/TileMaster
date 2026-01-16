using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TileMaster.Model;

namespace TileMaster.Manager
{
    public class AnimationManager
    {
        private Animation _animation;
        private float _timer;

        public Animation CurrentAnimation => _animation;
        public int CurrentFrame { get; private set; }
        public Vector2 Position { get; set; }
        public float Layer { get; set; }

        public AnimationManager(Animation animation)
        {
            _animation = animation;
        }

        public void Play(Animation animation)
        {
            if (_animation == animation)
                return;

            _animation = animation;
            _animation = animation;
            CurrentFrame = 0;
            _timer = 0;
        }

        public void Stop()
        {
            _timer = 0f;
            CurrentFrame = 0;
        }

        public void Update(GameTime gameTime)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_timer > _animation.FrameSpeed)
            {
                _timer = 0f;
                CurrentFrame++;

                if (CurrentFrame >= _animation.FrameCount)
                    CurrentFrame = _animation.IsLooping ? 0 : _animation.FrameCount - 1;
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            var sourceRectangle = new Rectangle(CurrentFrame * _animation.FrameWidth, 0, _animation.FrameWidth, _animation.FrameHeight);

            spriteBatch.Draw(_animation.Texture, Position, sourceRectangle, Color.White, 0f, Vector2.Zero, 1f, spriteEffects, Layer);
        }
    }
}