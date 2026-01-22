using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Entity.Enums;
using TileMaster.Entity.MobMovement;
using TileMaster.Manager;
using TileMaster.Model;

namespace TileMaster.Entity
{
    public class Mob : Entity
    {
        private Dictionary<string, Animation> _animations;
        public Entity Target { get; set; }
        public Movement Movement { get; set; }
        public void Load(ContentManager content, Vector2 position, string name,float movespeed,Movement movement)
        {
            texture = content.Load<Texture2D>($"Entities/{name}/{name}");
            rectangle = new Rectangle((int)position.X, (int)position.Y, texture.Width, texture.Height);
            this.position = position;
            MoveSpeed = movespeed;
            Movement = movement;

            // Load textures (Ideally these are SpriteSheets with multiple frames)
            var idleTexture = content.Load<Texture2D>($"Entities/{name}/{name}");

            _animations = new Dictionary<string, Animation>();
            _animations.Add("Idle", new Animation(idleTexture, 1)); // Assuming 1 frame for now
            _animations.Add("Walk", new Animation(content.Load<Texture2D>($"Entities/{name}/Walk"), 4));

            _animationManager = new AnimationManager(_animations["Idle"]);

            // Keep reference for base entity logic if needed, though AnimationManager handles drawing now
            texture = idleTexture;

        }
        public override void Update(GameTime gameTime, Map.Map map)
        {
            if (Game._state == GameState.Running)
            {
                Movement.Move(gameTime, this, map);
                base.Update(gameTime, map);
            }
            UpdateAnimation(gameTime);
        }

        private void UpdateAnimation(GameTime gameTime)
        {
            if (velocity.X != 0)
                _animationManager.Play(_animations.ContainsKey("Walk") ? _animations["Walk"] : _animations["Idle"]);
            else
                _animationManager.Play(_animations["Idle"]);

            _animationManager.Update(gameTime);
            _animationManager.Position = position;
        }
    }
}
