using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Entity;

namespace TileMaster.Misc
{
    public class ScrollingBackground : Component
    {
        private float _layer;
        private List<Texture2D> _textures;
        private readonly Player _player;
        private float _parallaxFactor; // 0 = moves with camera (far background), 1 = moves with world (foreground)
        private bool _constantMove; // For clouds that move on their own
        private float _autoMoveSpeed; // Speed for constant moving backgrounds
        private float _autoScrollOffset; // Accumulated offset for auto-moving backgrounds

        public float Layer
        {
            get { return _layer; }
            set { _layer = value; }
        }

        /// <summary>
        /// Creates a scrolling background.
        /// </summary>
        /// <param name="texture">The texture to use.</param>
        /// <param name="player">Reference to player (unused now but kept for compatibility if needed later).</param>
        /// <param name="parallaxFactor">Multiplier for camera movement. 0 = static sky, 1 = fixed to ground. values in between create depth.</param>
        /// <param name="constantMove">If true, the background scrolls automatically (e.g. clouds).</param>
        /// <param name="autoMoveSpeed">Speed of automatic scrolling in pixels per second.</param>
        public ScrollingBackground(Texture2D texture, Player player, float parallaxFactor, bool constantMove = false, float autoMoveSpeed = 0f)
          : this(new List<Texture2D>() { texture }, player, parallaxFactor, constantMove, autoMoveSpeed)
        {
        }

        private float _baseY;

        public ScrollingBackground(List<Texture2D> textures, Player player, float parallaxFactor, bool constantMove = false, float autoMoveSpeed = 0f)
        {
            _player = player;
            _textures = textures;
            _parallaxFactor = parallaxFactor;
            _constantMove = constantMove;
            _autoMoveSpeed = autoMoveSpeed;
            _autoScrollOffset = 0f;
            // Initialize BaseY based on the original logic
            Texture2D texture = textures[0];
            _baseY = Global.WindowHeight - texture.Height + 800;
        }

        // New Update method that takes the camera position
        public void Update(GameTime gameTime, Vector2 cameraPosition)
        {
            if (_constantMove)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _autoScrollOffset += _autoMoveSpeed * dt;
            }
        }
        
        // Deprecated Update method kept for compatibility if called without camera pos, but won't work correctly for parallax
        public override void Update(GameTime gameTime)
        {
            Vector2 camPos = Game.camera.Position;
            Update(gameTime, camPos);
        }


        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 cameraPosition = Game.camera.Position;
            int screenWidth = Global.WindowWidth; 
            int screenHeight = Global.WindowHeight;

            // We will use the first texture for now. If list has multiple, we could randomize or alternate.
            // For simple parallax, we assume one repeatable texture.
            Texture2D texture = _textures[0];
            
            // Calculate parallax position
            
            // Horizontal Parallax
            float worldX = cameraPosition.X * (1 - _parallaxFactor);
            float parallaxShift = cameraPosition.X * _parallaxFactor; 
            if (_constantMove) parallaxShift -= _autoScrollOffset;
            float offset = -(parallaxShift % texture.Width);
            if (offset > 0) offset -= texture.Width;
            
            // Vertical Parallax
            // Formula: drawY = BaseY * P + CameraY * (1 - P)
            // If P=0 (Sky), drawY = CameraY (Follows camera)
            // If P=1 (Ground), drawY = BaseY (Fixed in world)
            // Note: We might want to adjust BaseY for P=0 cases to center it? 
            // For now, let's keep it simple as it ensures Sky is visible.
            float drawY = _baseY * _parallaxFactor + cameraPosition.Y * (1 - _parallaxFactor);

            float x = offset;
            // Ensure we cover the screen width. 
            // Since 'x' is in "screen space" relative to the camera (due to logic above?), 
            // Wait. 'offset' is relative to... 
            // Loop draws at 'x'. SpriteBatch has Camera Transform!
            // So we are drawing in WORLD SPACE.
            
            // If we are drawing in World Space:
            // The camera is at cameraPosition.
            // The left edge of the screen in World Space is cameraPosition.X.
            // We want to draw tiles starting from cameraPosition.X + offset.
            
            float startX = cameraPosition.X + offset;
            
            // However, 'offset' was calculated as -(parallaxShift % width).
            // parallaxShift = camX * P.
            // visualX = (worldX - camX) -> No.
            
            // Let's deduce correct World X to draw at.
            // ScreenX = WorldX - CameraX.
            // We want ScreenX to shift by -parallaxShift?
            // Actually, let's look at the result we want.
            // We want the texture pattern to appear shifted by 'parallaxShift'.
            
            // Effectively, we want to tile textures such that at CameraX, the texture phase is determined by parallaxShift.
            
            // WorldX of tile = CameraX + offset + i * width?
            // Yes.
            
            x = startX;
            // Draw enough tiles to cover screen
            while (x < cameraPosition.X + screenWidth + texture.Width)
            {
               spriteBatch.Draw(texture, new Vector2(x, drawY), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, _layer);
               x += texture.Width;
            }
        }
    }
}