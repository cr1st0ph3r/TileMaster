using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Controls;

namespace TileMaster.Manager
{
    internal class ButtonManager
    {
        public List<Button> Buttons;
        public ButtonManager()
        {
            Buttons = new List<Button>();
        }
       

        public void CreateButton(ContentManager Content,string Name,EventHandler ev,Vector2 position,int id)
        {
            
            var b = new Button(Content.Load<Texture2D>("Controls/Button"), Content.Load<SpriteFont>("Fonts/Font"))
            {
                Position = position,
                Id = id,
                Text =Name,
            };
            b.Click += ev;

            Buttons.Add(b);
        }
        public void UpdateButton(int buttonId,Vector2 position)
        {
            Buttons.FirstOrDefault(x => x.Id == buttonId).Position = position;
        }

        public void Update(GameTime gameTime)
        {
            foreach (var b in Buttons)
            {
                b.Update(gameTime);
            }
        }
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            foreach (var b in Buttons)
            {
                b.Draw(gameTime, spriteBatch);
            }
        }
    }
}
