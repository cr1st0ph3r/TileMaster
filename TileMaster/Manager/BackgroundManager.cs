using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Misc;
using TileMaster.Entity;

namespace TileMaster.Manager
{
    internal class BackgroundManager
    {
        public List<ScrollingBackground> ScrollingBackgrounds;
        public void Load(ContentManager Content,Player player)
        {
            ScrollingBackgrounds = new List<ScrollingBackground>()
            {
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Sky"), player, 0f)
                {
                Layer = 0.1f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Clouds_Slow"), player, 1f, true)
                {
                Layer = 0.7f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Hills_Back"), player, 0f)
                {
                Layer = 0.77f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Clouds_Fast"), player, 2.5f, true)
                {
                Layer = 0.78f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Hills_Middle"), player, 3f)
                {
                Layer = 0.79f,
                }, new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Hills_Front"), player, 4f)
                {
                Layer = 0.8f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Floor"), player, 6f)
                {
                Layer = 0.9f,
                },
                new ScrollingBackground(Content.Load<Texture2D>("Levels/Sunny/Trees"), player, 6f)
                {
                Layer = 0.99f,
                }
            };

        }

        public void Update(GameTime gameTime)
        {
            foreach (var bg in ScrollingBackgrounds)
            {
                bg.Update(gameTime);
            }
        }
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (var bg in ScrollingBackgrounds)
            {
                bg.Draw(gameTime,spriteBatch);
            }
        }
    }

  

}
