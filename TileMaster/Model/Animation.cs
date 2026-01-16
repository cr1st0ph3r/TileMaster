using Microsoft.Xna.Framework.Graphics;

namespace TileMaster.Model
{
    public class Animation
    {
        public Texture2D Texture { get; private set; }
        public int FrameCount { get; private set; }
        public float FrameSpeed { get; private set; }
        public bool IsLooping { get; private set; }

        public int FrameWidth => Texture.Width / FrameCount;
        public int FrameHeight => Texture.Height;

        public Animation(Texture2D texture, int frameCount, float frameSpeed = 0.2f, bool isLooping = true)
        {
            Texture = texture;
            FrameCount = frameCount;
            FrameSpeed = frameSpeed;
            IsLooping = isLooping;
        }
    }
}