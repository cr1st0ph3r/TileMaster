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

        public float Rotation { get; set; }
        public Vector2 Origin { get; set; }

        public void Draw(SpriteBatch spriteBatch, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            var sourceRectangle = new Rectangle(CurrentFrame * _animation.FrameWidth, 0, _animation.FrameWidth, _animation.FrameHeight);

            // Default origin to center if not set? Or leave as Zero?
            // If we want rotation to work nicely, center origin is usually best. 
            // However, existing code might rely on top-left origin (Position).
            // If I change Origin, I effectively change the render position. 
            // The Entity.Position defines top-left of hitbox.
            // If AnimationManager.Draw uses Origin, it subtracts Origin from Position (or rather, renders AT Position with Origin as the anchor).
            // So if Position is Top-Left, and Origin is Center, the sprite will be drawn shifted up-left.
            // I should leave Origin as Zero by default, but allow setting it.
            // If Rotation is used, the user (Entity) MUST adjust Position or Origin.

            spriteBatch.Draw(_animation.Texture, Position, sourceRectangle, Color.White, Rotation, Origin, 1f, spriteEffects, Layer);
        }
    }
}