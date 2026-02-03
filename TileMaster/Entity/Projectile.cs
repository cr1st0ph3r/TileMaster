using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TileMaster.Entity
{
    public class Projectile : Entity
    {
        public Item AmmunitionItem { get; private set; }
        public bool IsActive { get; set; } = true;
        public float DistanceTraveled { get; private set; }
        public float MaxDistance { get; private set; }
        public int Damage { get; set; }
        public float Knockback { get; set; }



        public Projectile(Item ammoItem, Vector2 startPosition, Vector2 initialVelocity, float maxDistance, int damage, float knockback)
        {
            AmmunitionItem = ammoItem;
            this.position = startPosition;
            this.velocity = initialVelocity;
            this.MaxDistance = maxDistance;
            this.Damage = damage;
            this.Knockback = knockback;

            
            if (ammoItem.Texture != null)
            {
                rectangle = new Rectangle((int)position.X, (int)position.Y, ammoItem.Texture.Width, ammoItem.Texture.Height);
                Texture = ammoItem.Texture;
            }
            else
            {
                // Small default rectangle if texture is missing
                rectangle = new Rectangle((int)position.X, (int)position.Y, 4, 4);
            }
        }

        public override void Update(GameTime gameTime, Map.Map map)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply gravity if affected
            if (AmmunitionItem.AffectedByGravity)
            {
                velocity.Y += Gravity * dt;
            }

            // Update rotation based on velocity
            if (velocity.LengthSquared() > 0.001f)
            {
                Rotation = (float)Math.Atan2(velocity.Y, velocity.X);
            }

            // Movement and distance tracking
            Vector2 prePosition = position;
            Vector2 movement = velocity * dt;
            position += movement;
            DistanceTraveled += movement.Length();

            if (DistanceTraveled >= MaxDistance)
            {
                IsActive = false;
                return;
            }

            // Update rectangle for collision
            if (Texture != null)
                rectangle = new Rectangle((int)position.X, (int)position.Y, Texture.Width, Texture.Height);
            else
                 rectangle = new Rectangle((int)position.X, (int)position.Y, rectangle.Width, rectangle.Height);

            // Collision check with map
            if (IsRectCollidingWithMap(rectangle, map, out _, out _))
            {
                IsActive = false;
                return;
            }

            // Collision check with mobs
            var game = Game.GetInstance();
            if (game != null && game.Mobs != null)
            {
                foreach (var mob in game.Mobs)
                {
                    if (rectangle.Intersects(mob.GetRectangle()))
                    {
                        // Calculate knockback direction (repelled horizontally with a slight upward pop)
                        Vector2 knockbackDir = new Vector2(System.Math.Sign(velocity.X), -0.4f);
                        if (knockbackDir.X == 0) knockbackDir.X = (position.X < mob.GetPosition().X) ? 1 : -1;
                        knockbackDir.Normalize();
                        
                        mob.TakeDamage(Damage, knockbackDir * Knockback * 150f); // Scaling knockback
                        IsActive = false;
                        return;
                    }
                }
            }

            // Boundary check
            CheckBoundaries();
            
            // If CheckBoundaries snapped us, it might mean we hit something or went out of bounds
            if (position.Y >= ((Global.MapHeight - 2) * Global.TileSize) - 11)
            {
                IsActive = false;
            }
        }

        public new void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            if (Texture != null)
            {
                // Draw with rotation around center
                Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
                // Adjust position to draw from center to match collision box (roughly)
                spriteBatch.Draw(Texture, position + origin, null, Color.White, Rotation, origin, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
