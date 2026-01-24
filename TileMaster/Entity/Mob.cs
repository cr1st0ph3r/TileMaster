using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Data;
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
        public MobType MobType { get; set; }
        public int WalkFrames { get; set; }
        public void Load(ContentManager content, Vector2 position, ReferenceMob reference)
        {
            // Load textures (Ideally these are SpriteSheets with multiple frames)
            var idleTexture = content.Load<Texture2D>($"Entities/{reference.Name}/{reference.Name}");
            // Keep reference for base entity logic if needed, though AnimationManager handles drawing now
            Texture = idleTexture;

            rectangle = new Rectangle((int)position.X, (int)position.Y, Texture.Width, Texture.Height);
            this.position = position;
            MoveSpeed = reference.MoveSpeed;
            WalkFrames = reference.WalkFrames;
            MobType = reference.MobType;
            Movement = reference.Movement switch
            {
                "Hop" => new Hop(),
                "Fly" => new Fly(),
                "Snail" => new Snail(),
                _ => new Walk(),
            };
            if (Movement is Snail)
            {
                CanFlip = false;
            }

            _animations = new Dictionary<string, Animation>();
            _animations.Add("Idle", new Animation(idleTexture, 1)); // Assuming 1 frame for now
            if (WalkFrames > 1)
            {
                _animations.Add("Walk", new Animation(content.Load<Texture2D>($"Entities/{reference.Name}/Walk"), WalkFrames));
            }

            _animationManager = new AnimationManager(_animations["Idle"]);



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
            _animationManager.Position = position + (Origin != Vector2.Zero ? new Vector2(rectangle.Width / 2f, rectangle.Height / 2f) : Vector2.Zero);
        }
    }
}
