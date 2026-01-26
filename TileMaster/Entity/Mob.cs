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
        public MobType MobType { get; set; }
        public int WalkFrames { get; set; }
        public int DamageFrames { get; set; }
        private float _damageTimer;
        private const float DamageAnimationDuration = 0.5f; // half a second animation

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
Health = reference.Health;
            MaxHealth = reference.Health;
            AttackPower = reference.AttackPower;
            Defense = reference.Defense;
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
            if (reference.DamageFrames > 0)
            {
                _animations.Add("Damage", new Animation(content.Load<Texture2D>($"Entities/{reference.Name}/Damage"), reference.DamageFrames, isLooping: false));
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
            if (_damageTimer > 0)
            {
                _damageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_animations.ContainsKey("Damage"))
                {
                    _animationManager.Play(_animations["Damage"]);
                }
            }
            else
            {
                if (velocity.X != 0)
                    _animationManager.Play(_animations.ContainsKey("Walk") ? _animations["Walk"] : _animations["Idle"]);
                else
                    _animationManager.Play(_animations["Idle"]);
            }

            _animationManager.Update(gameTime);
            _animationManager.Position = position + (Origin != Vector2.Zero ? new Vector2(rectangle.Width / 2f, rectangle.Height / 2f) : Vector2.Zero);
        }

        public override void TakeDamage(int damage, Vector2 knockback)
        {
            base.TakeDamage(damage, knockback);
            _damageTimer = DamageAnimationDuration;
        }

        public void DrawHealthDisplay(SpriteBatch spriteBatch, SpriteFont font, Vector2 worldPosition)
        {
            string healthText = $"{Health}/{MaxHealth}";
            Vector2 textSize = font.MeasureString(healthText);
            float centerX = worldPosition.X + (GetRectangle().Width / 2f) - (textSize.X / 2f);
            Vector2 textPosition = new Vector2(centerX, worldPosition.Y + GetRectangle().Height + 5);
            
            // Use existing text rendering pattern with shadow
            spriteBatch.DrawString(font, healthText, textPosition, Color.Black);
            spriteBatch.DrawString(font, healthText, textPosition + Vector2.One, Color.Red);
        }
    }
}
