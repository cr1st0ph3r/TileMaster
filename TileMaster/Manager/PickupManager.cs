using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using TileMaster.Entity;
using Myra.Graphics2D.TextureAtlases;
using Myra;
using AssetManagementBase;

namespace TileMaster.Manager
{
    public class PickupManager
    {
        private class PickupEntity
        {
            public Vector2 Position;
            public Vector2 StartPosition;
            public Entity.Entity Target;
            public Item ItemRef;
            public int Quantity;
            public float Speed;
            public TextureRegion TextureRegion;
            public bool IsActive;

            // Simple movement logic
            public void Update(float dt, PickupManager manager)
            {
                if (!IsActive) return;

                Vector2 targetPos = Target.GetPosition();
                // Aim for center of player
                targetPos += new Vector2(Target.GetRectangle().Width / 2f, Target.GetRectangle().Height / 2f);
                
                // Direction to target
                Vector2 direction = targetPos - Position;
                float distance = direction.Length();

                if (distance < 20f) // Close enough to "pickup"
                {
                    manager.OnPickup(this);
                    IsActive = false;
                    return;
                }

                direction.Normalize();
                
                // Accelerate
                Speed += 500f * dt; 
                Position += direction * Speed * dt;
            }
        }

        private List<PickupEntity> _pickups;
        private Game _game;

        public PickupManager(Game game)
        {
            _game = game;
            _pickups = new List<PickupEntity>();
        }

        public void Spawn(Vector2 startPos, Item item, int quantity, Entity.Entity target)
        {
            var region = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{item.UIIcon}.png");
            
            _pickups.Add(new PickupEntity
            {
                Position = startPos,
                StartPosition = startPos,
                Target = target,
                ItemRef = item,
                Quantity = quantity,
                Speed = 100f, // Initial speed
                TextureRegion = region,
                IsActive = true
            });
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                var pickup = _pickups[i];
                pickup.Update(dt, this);
                if (!pickup.IsActive)
                {
                    _pickups.RemoveAt(i);
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var pickup in _pickups)
            {
                if (pickup.TextureRegion != null)
                {
                    // Draw centered at Position
                    var tex = pickup.TextureRegion;
                    // Scalling down a bit as icons might be large, or keep as is. Let's start with 0.5 scale for "world" view if icons are large 32x32
                    // 32x32 is actually fine for world view (similar to block size)
                    Rectangle dest = new Rectangle(
                        (int)pickup.Position.X - 8, 
                        (int)pickup.Position.Y - 8, 
                        16, 16); // 16x16 size for the flying item

                    spriteBatch.Draw(tex.Texture, dest, tex.Bounds, Color.White);
                }
            }
        }

        private void OnPickup(PickupEntity pickup)
        {
            // Add to inventory
            if (pickup.Target is Player player)
            {
                player.AddItem(pickup.ItemRef, pickup.Quantity);
                
                // Show floating text
                if (_game.DamageNumberManager != null)
                {
                    // "Dirt (1)"
                    string text = $"{pickup.ItemRef.Name} ({pickup.Quantity})";
                    // Greenish color for pickup
                    Color color = Color.LightGreen;
                    
                    // Spawn text above player
                    Vector2 textPos = player.GetPosition();
                    textPos.X += player.GetRectangle().Width / 2f;
                    textPos.Y -= 20f;

                    _game.DamageNumberManager.Add(textPos, text, color);
                }
            }
        }
    }
}
