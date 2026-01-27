using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;
using TileMaster.Helper;
using TileMaster.Manager;
using TileMaster.Model;

namespace TileMaster.Entity
{
    public class Player : Entity
    {
        public Layer Layer { get; set; } = Layer.Surface;
        public bool InterruptInput { get; set; }
        public float UseCooldown { get; set; }
        public int InventoryTier { get; set; }

        private Dictionary<string, Animation> _animations;
        public Dictionary<int, InventoryItem> Inventory { get; set; } = new Dictionary<int, InventoryItem>(40);
        public Dictionary<int, InventoryItem> ActionBar { get; set; } = new Dictionary<int, InventoryItem>(10);


        public Player()
        {   //the height of the player in blocks
            this.Height = 3;
            this.Health = 100;
        }

        public bool HasAmmo(AmmoType type, out InventoryItem ammoItem)
        {
            ammoItem = null;
            // First check ActionBar
            foreach (var item in ActionBar.Values)
            {
                if (item != null && item.Item != null && item.Item.IsAmmo && item.Item.AmmoType == type && item.Quantity > 0)
                {
                    ammoItem = item;
                    return true;
                }
            }
            // Then check main Inventory
            foreach (var item in Inventory.Values)
            {
                if (item != null && item.Item != null && item.Item.IsAmmo && item.Item.AmmoType == type && item.Quantity > 0)
                {
                    ammoItem = item;
                    return true;
                }
            }
            return false;
        }

        public void ConsumeAmmo(AmmoType type)
        {
            // Check ActionBar first
            foreach (var kvp in ActionBar)
            {
                var item = kvp.Value;
                if (item != null && item.Item != null && item.Item.IsAmmo && item.Item.AmmoType == type && item.Quantity > 0)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0)
                    {
                        ActionBar[kvp.Key] = null;
                    }
                    return;
                }
            }
            // Check Inventory
            foreach (var kvp in Inventory)
            {
                var item = kvp.Value;
                if (item != null && item.Item != null && item.Item.IsAmmo && item.Item.AmmoType == type && item.Quantity > 0)
                {
                    item.Quantity--;
                    if (item.Quantity <= 0)
                    {
                        Inventory[kvp.Key] = null;
                    }
                    return;
                }
            }
        }

        public void AddItem(Item itemRef, int quantity)
        {
            if (itemRef == null) return;

            // Try to add to existing stack in ActionBar
            foreach (var slot in ActionBar.Values)
            {
                if (slot != null && slot.Item != null && slot.Item.Id == itemRef.Id && slot.Quantity < slot.Item.StackSize)
                {
                    int canAdd = slot.Item.StackSize - slot.Quantity;
                    int toAdd = Math.Min(canAdd, quantity);
                    slot.Quantity += toAdd;
                    quantity -= toAdd;
                    if (quantity <= 0) return;
                }
            }

            // Try to find empty slot in ActionBar
            for (int i = 0; i < 10; i++)
            {
                if (!ActionBar.ContainsKey(i) || ActionBar[i] == null || ActionBar[i].Item == null)
                {
                    ActionBar[i] = new InventoryItem(itemRef, quantity);
                    return;
                }
            }

            // Try to add to existing stack in Inventory
            foreach (var slot in Inventory.Values)
            {
                if (slot != null && slot.Item != null && slot.Item.Id == itemRef.Id && slot.Quantity < slot.Item.StackSize)
                {
                    int canAdd = slot.Item.StackSize - slot.Quantity;
                    int toAdd = Math.Min(canAdd, quantity);
                    slot.Quantity += toAdd;
                    quantity -= toAdd;
                    if (quantity <= 0) return;
                }
            }

            // Try to find empty slot in Inventory
            for (int i = 0; i < 40; i++)
            {
                if (!Inventory.ContainsKey(i) || Inventory[i] == null || Inventory[i].Item == null)
                {
                    Inventory[i] = new InventoryItem(itemRef, quantity);
                    return;
                }
            }

            // Fallback: Drop on ground or log failure
            Game.LogMessage($"Inventory full! Could not add {itemRef.Name}", Color.Yellow);
        }

        public void ConsumeItem(int itemId, int quantity)
        {
            int remaining = quantity;
            // ActionBar first
            foreach (var kvp in ActionBar)
            {
                var item = kvp.Value;
                if (item != null && item.Item != null && item.Item.Id == itemId)
                {
                    int toConsume = Math.Min(remaining, item.Quantity);
                    item.Quantity -= toConsume;
                    remaining -= toConsume;
                    if (item.Quantity <= 0) ActionBar[kvp.Key] = null;
                    if (remaining <= 0) return;
                }
            }
            // Inventory
            foreach (var kvp in Inventory)
            {
                var item = kvp.Value;
                if (item != null && item.Item != null && item.Item.Id == itemId)
                {
                    int toConsume = Math.Min(remaining, item.Quantity);
                    item.Quantity -= toConsume;
                    remaining -= toConsume;
                    if (item.Quantity <= 0) Inventory[kvp.Key] = null;
                    if (remaining <= 0) return;
                }
            }
        }

        public int RemoveItemFromSlot(int slotIndex, int quantity, bool fromActionBar = true)
        {
            var targetDict = fromActionBar ? ActionBar : Inventory;
            if (targetDict.TryGetValue(slotIndex, out var item) && item != null)
            {
                item.Quantity -= quantity;
                if (item.Quantity <= 0)
                {
                    targetDict[slotIndex] = null;
                    return 0;
                }
                return item.Quantity;
            }
            return 0;
        }

        public void Load(ContentManager content)
        {
            // Load textures (Ideally these are SpriteSheets with multiple frames)
            var idleTexture = content.Load<Texture2D>("Entities/Player/Player");

            _animations = new Dictionary<string, Animation>();
            _animations.Add("Idle", new Animation(idleTexture, 1)); // Assuming 1 frame for now
            _animations.Add("Walk", new Animation(content.Load<Texture2D>("Entities/Player/Walk"), 4));

            _animationManager = new AnimationManager(_animations["Idle"]);

            // Keep reference for base entity logic if needed, though AnimationManager handles drawing now
            Texture = idleTexture;
        }

        public bool IsInWater { get; private set; }

        private bool CheckIfInWater(Map.Map map)
        {
            // Check tile at center of player
            int centerX = (int)(position.X + Texture.Width / 2) / Global.TileSize;
            int centerY = (int)(position.Y + Texture.Height / 2) / Global.TileSize;
            var tile = map.GetTileAt(centerX, centerY);
            return tile != null && tile.TileId == (int)TileType.Water;
        }

        public override void Update(GameTime gameTime, Map.Map map)
        {
            var keyboardState = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            IsInWater = CheckIfInWater(map);

            if (UseCooldown > 0)
                UseCooldown -= (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            
            // set if the player is in motion or not
            // We use a slightly more generous threshold for "stationary" to account for floating point jitter
            isMoving = Math.Abs(velocity.X) > 0.1f || Math.Abs(velocity.Y) > 0.5f;

            // Check if we can skip physics (Stationary check)
            // Skip physics if: Not moving, No movement keys pressed, and already on solid block
            bool moveKeyPressed = keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Space);
            
            if (!isMoving && !moveKeyPressed && IsOnSolidBlock && !InterruptInput && !IsInWater)
            {
                // We are stationary. We still need a minimal check to see if the block under us was removed.
                if (InputHelper.HandleMovingDown(this, map))
                {
                    IsOnSolidBlock = false;
                    isMoving = true;
                    // Fall through to gravity/physics logic
                }
                else
                {
                    // Truly stationary and supported. Skip physics and just update animation/layers.
                    UpdateStationaryState(gameTime);
                    return;
                }
            }

            // compute current grid indices from current position (needed by InputHelper)
            UpdateGridPosition();

            // process input first (decides velocity / intent)
            if (!InterruptInput)
            {
                Input(gameTime, this, map, keyboardState);
            }


            // Ground detection: use the same helper used elsewhere but keep it
            // out of Input() to avoid duplicate snapping logic. HandleMovingDown
            // returns true if the player should fall (no support under feet).
            bool shouldFall = InputHelper.HandleMovingDown(this, map);
            IsOnSolidBlock = !shouldFall;

            // gravity (time-based) - applied to velocity before integration
            // Water Physics: Buoyancy and Drag
            if (IsInWater)
            {
                // Falling slower in water (Drag/Buoyancy)
                if (velocity.Y < 300f) // Max fall speed in water
                {
                    velocity.Y += (Gravity * 0.3f) * dt; // Reduced gravity
                }
                else
                {
                     velocity.Y -= (Gravity * 0.5f) * dt; // Slow down if falling too fast entering water
                }
                
                // Horizontal Drag
                velocity.X *= 0.9f; 
                velocity.Y *= 0.9f; 
            }
            else
            {
                // Normal Gravity
                 if (velocity.Y < MaxFallSpeed && !IsOnSolidBlock)
                {
                    velocity.Y += Gravity * dt;
                }
            }


            // per-axis integration with collision resolution to avoid tunneling
            // Horizontal movement
            float newX = position.X + velocity.X * dt;
            Rectangle testRectX = new Rectangle((int)newX, (int)position.Y, Texture.Width, Texture.Height);

            bool ignoreCollisionX = false;
            if (IsRectCollidingWithMap(testRectX, map, out int hitTileX, out int hitTileY, findRightmost: velocity.X < 0))
            {
                // Check if we hit a slope that we can climb
                var hitTile = map.GetTileAt(hitTileX, hitTileY);
                if (hitTile != null && hitTile.IsSlope)
                {
                    int feetTileY = (int)((position.Y + Texture.Height - 1) / Global.TileSize);
                    if (hitTileY == feetTileY)
                    {
                        // Allow movement through all slopes at feet level (climbing and descending)
                        ignoreCollisionX = true;
                    }
                }

                if (!ignoreCollisionX)
                {
                    if (velocity.X > 0)
                        position.X = hitTileX * Global.TileSize - Texture.Width;
                    else if (velocity.X < 0)
                        position.X = (hitTileX + 1) * Global.TileSize;

                    velocity.X = 0f;
                }
                else
                {
                    position.X = newX;
                    // Ensure the player climbs OR descends the slope as they move horizontally
                    float slopeRestY = SlopeCollisionHelper.GetSlopeRestPosition(hitTile, position.Y + Texture.Height, position.X, position.X + Texture.Width);
                    
                    // Always push the player UP to prevent penetration
                    if (slopeRestY < position.Y + Texture.Height)
                    {
                        position.Y = slopeRestY - Texture.Height;
                    }
                    // Snap the player DOWN if they were already grounded to keep them stuck to the slope
                    else if (IsOnSolidBlock)
                    {
                        position.Y = slopeRestY - Texture.Height;
                    }
                }
            }
            else
            {
                position.X = newX;
            }

            // Vertical movement
            float newY = position.Y + velocity.Y * dt;
            Rectangle testRectY = new Rectangle((int)position.X, (int)newY, Texture.Width, Texture.Height);

if (IsRectCollidingWithMap(testRectY, map, out hitTileX, out hitTileY, findBottommost: velocity.Y < 0))
            {
                // Get the tile we collided with
                var hitTile = map.GetTileAt(hitTileX, hitTileY);
                
                // collided on Y axis: clamp and stop vertical velocity
                if (velocity.Y > 0)
                {
                    // falling: check if we hit a slope
                    if (hitTile != null && hitTile.IsSlope)
                    {
                        // For slopes, adjust position to rest on the slope surface
                        float slopeRestY = SlopeCollisionHelper.GetSlopeRestPosition(hitTile, testRectY.Bottom, testRectY.Left, testRectY.Right);
                        position.Y = slopeRestY - Texture.Height;
                        
                        // Adjust velocity for slope influence
                        var adjustedVelocity = SlopeCollisionHelper.AdjustVelocityForSlope(hitTile, velocity.X, true);
                        velocity.Y = adjustedVelocity.Y;
                    }
                    else
                    {
                        // Regular tile: place player's bottom on top of the tile
                        position.Y = hitTileY * Global.TileSize - Texture.Height;
                        velocity.Y = 0f;
                    }
                    
                    IsOnSolidBlock = true;
                    hasJumped = false;
                }
                else if (velocity.Y < 0)
                {
                    // rising: check if we hit a slope at feet level (climbing) or head level (ceiling)
                    if (hitTile != null && hitTile.IsSlope)
                    {
                        int feetTileY = (int)((position.Y + Texture.Height - 1) / Global.TileSize);
                        if (hitTileY == feetTileY)
                        {
                            // If it's a slope at feet level, ignore it when moving up (we are climbing)
                            position.Y = newY; 
                        }
                        else
                        {
                            // Actual ceiling collision
                            position.Y = (hitTileY + 1) * Global.TileSize;
                            velocity.Y = 0f;
                        }
                    }
                    else
                    {
                        // Regular ceiling tile
                        position.Y = (hitTileY + 1) * Global.TileSize;
                        velocity.Y = 0f;
                    }
                }
                else
                {
                    velocity.Y = 0f;
                }
            }
            else
            {
                position.Y = newY;
                // if we are moving down and didn't hit anything, we are not on solid ground
                if (velocity.Y > 0) IsOnSolidBlock = false;
            }

            // update rectangle after applying resolved position
            // Use the animation frame width/height for collision, not the entire texture sheet
            rectangle = new Rectangle((int)position.X, (int)position.Y, _animationManager.CurrentAnimation.FrameWidth, _animationManager.CurrentAnimation.FrameHeight);

            // small conditional snap to ground to avoid tiny floating above tiles (keeps previous behavior)
            if (IsOnSolidBlock)
            {
                // check if we are on a slope
                int feetX = (int)(position.X + rectangle.Width / 2) / Global.TileSize;
                int feetY = (int)(position.Y + rectangle.Height - 1) / Global.TileSize;
                var tileBelow = map.GetTileAt(feetX, feetY);
                bool onSlope = tileBelow != null && tileBelow.IsSlope;

                if (!onSlope)
                {
                    // Snap only when the player's bottom is very near the tile top.
                    var bottom = position.Y + rectangle.Height;
                    int tileBelowIdx = (int)(bottom / Global.TileSize);
                    float tileTop = tileBelowIdx * Global.TileSize;
                    float delta = tileTop - bottom;

                    const float snapTolerance = 3f;
                    if (Math.Abs(delta) <= snapTolerance)
                    {
                        position.Y = tileTop - rectangle.Height;
                        velocity.Y = 0f;
                        hasJumped = false;
                        rectangle = new Rectangle((int)position.X, (int)position.Y, Texture.Width, Texture.Height);
                    }
                }
            }

            // update grid indices to reflect new position
            UpdateGridPosition();

            // set layers
            UpdateLayer();

            UpdateAnimation(gameTime);

        }

        private void UpdateStationaryState(GameTime gameTime)
        {
            UpdateLayer();
            UpdateAnimation(gameTime);
            _animationManager.Position = position;
        }

        private void UpdateLayer()
        {
            int heightDelta = GridY - Global.GroundLevel;

            if (heightDelta <= -50)
            {
                Layer = Layer.Sky;
            }
            else if (heightDelta >= 300)
            {
                Layer = Layer.Underworld;
            }
            else if (heightDelta >= 150)
            {
                Layer = Layer.Underground;
            }
            else if (heightDelta >= 50)
            {
                Layer = Layer.Caverns;
            }
            else
            {
                Layer = Layer.Surface;
            }
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

        public void Input(GameTime gameTime, Player player, Map.Map map, KeyboardState keyboardState)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // move right
            if (InputHelper.HandleMovingRight(player, map, keyboardState))
            {
                velocity.X = MoveSpeed;
            }

            // move left
            else if (InputHelper.HandleMovingLeft(player, map, keyboardState))
            {
                velocity.X = -MoveSpeed;
            }

            // linear momentum left/right (friction)
            if (velocity.X > 0.4F)
            {
                velocity.X -= Friction * dt;
                if (velocity.X < 0f) velocity.X = 0f;
            }
            else if (velocity.X < -0.4F)
            {
                velocity.X += Friction * dt;
                if (velocity.X > 0f) velocity.X = 0f;
            }
            else { velocity.X = 0; }

            // handle player jump (jump impulse is in px/s)
            // only allow a jump when we believe we are on solid ground OR IN WATER (Swimming)
            if (keyboardState.IsKeyDown(Keys.Space) && hasJumped == false && (IsOnSolidBlock || IsInWater))
            {
                // small positional tweak to avoid immediate collision
                position.Y -= 5F;
                
                if (IsInWater)
                {
                     velocity.Y = -JumpVelocity * 0.7f; // Reduced jump strength in water (swimming up)
                }
                else
                {
                     velocity.Y = -JumpVelocity;
                }
               
                hasJumped = true;
                IsOnSolidBlock = false;
            }
            // Reset jump flag if space is released while in water to allow repeated swim strokes
            if (IsInWater && keyboardState.IsKeyUp(Keys.Space))
            {
                hasJumped = false;
            }
            
            if (hasJumped)
            {
                if (!InputHelper.HandleJump(player, map))
                {
                    // collision while jumping: cancel upward motion and nudge down
                    velocity.Y = 0f;
                    position.Y += 5F;
                    hasJumped = false;
                }
            }
        }
    }
}