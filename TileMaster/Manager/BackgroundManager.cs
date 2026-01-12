using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Misc;
using TileMaster.Entity;
using TileMaster.Entity.Enums;

namespace TileMaster.Manager
{
    internal class BackgroundManager
    {
        public Dictionary<Layer, List<ScrollingBackground>> Backgrounds;
        private Player _player;
        private Layer _currentLayer;

        public void Load(ContentManager Content, Player player)
        {
            _player = player;
            Backgrounds = new Dictionary<Layer, List<ScrollingBackground>>();

            // 1. Sky Layer
            // Assuming 0.0 parallax for "sky" (fixed to camera) or very low.
            Backgrounds[Layer.Sky] = new List<ScrollingBackground>()
            {
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Sky/Background"), player, 0.0f) { Layer = 0.1f }
            };

            // 2. Surface Layer (Detailed)
            Backgrounds[Layer.Surface] = new List<ScrollingBackground>()
            {
                // Far background (Sky) - Fixed
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Background"), player, 0.0f)
                {
                    Layer = 0.1f,
                },
                // Clouds Slow - Auto move, slight parallax
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Clouds_Slow"), player, 0.1f, true, 10f)
                {
                    Layer = 0.2f,
                },
                // Hills Back - Far away, moves slowly with camera
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Hills_Back"), player, 0.2f)
                {
                    Layer = 0.3f,
                },
                // Clouds Fast - Auto move, closer
                 new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Clouds_Fast"), player, 0.3f, true, 25f)
                {
                    Layer = 0.4f,
                },
                // Hills Middle
                 new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Hills_Middle"), player, 0.5f)
                {
                    Layer = 0.5f,
                }, 
                // Hills Front - Closer
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Hills_Front"), player, 0.8f)
                {
                    Layer = 0.6f,
                },
                // Floor/Ground - Should track world closely (1.0) or be near it
                // Usually the graphical "floor" behind tiles is parallax 1.0 but drawn behind.
                // If it's a distant floor, maybe 0.9.
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Floor"), player, 0.9f)
                {
                    Layer = 0.7f,
                },
                // Trees - Foreground elements? Or far trees?
                new ScrollingBackground(Content.Load<Texture2D>("Layers/Surface/Trees"), player, 0.95f)
                {
                    Layer = 0.8f,
                }
            };

            // 3. Caverns
            Backgrounds[Layer.Caverns] = new List<ScrollingBackground>()
            {
                 new ScrollingBackground(Content.Load<Texture2D>("Layers/Caverns/Background"), player, 0.1f) { Layer = 0.1f }
            };

            // 4. Underground
            Backgrounds[Layer.Underground] = new List<ScrollingBackground>()
            {
                 new ScrollingBackground(Content.Load<Texture2D>("Layers/Underground/Background"), player, 0.1f) { Layer = 0.1f }
            };

            // 5. Underworld
            Backgrounds[Layer.Underworld] = new List<ScrollingBackground>()
            {
                 new ScrollingBackground(Content.Load<Texture2D>("Layers/Underworld/Background"), player, 0.1f) { Layer = 0.1f }
            };
        }

        public void Update(GameTime gameTime)
        {
            _currentLayer = _player.Layer;

            if (Backgrounds.ContainsKey(_currentLayer))
            {
                foreach (var bg in Backgrounds[_currentLayer])
                {
                    bg.Update(gameTime, Game.camera.Position);
                }
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (Backgrounds.ContainsKey(_currentLayer))
            {
                foreach (var bg in Backgrounds[_currentLayer])
                {
                     bg.Draw(gameTime, spriteBatch);
                }
            }
        }
    }
}