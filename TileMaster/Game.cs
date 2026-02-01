using AssetManagementBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TileMaster.Data;
using TileMaster.Entity;
using TileMaster.Entity.Enums;
using TileMaster.Manager;
using TileMaster.Map;
using TileMaster.UI;
using ButtonState = Microsoft.Xna.Framework.Input.ButtonState;

namespace TileMaster
{
    public partial class Game : Microsoft.Xna.Framework.Game
    {
        #region Variables
        public MainPanel _mainPanel;
        public static GameState _state;
        public static Camera camera;
        public int mouseIsOverBlock;
        public static readonly Random rnd = new(DateTime.Now.GetHashCode());
        public event Action<int> ScrollWheelChanged;

        private static Game _game;
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Map.Map map;
        public Player player;
        private SpriteFont _debugFont;
        private Desktop _desktop;
        private MouseState current_mouse;
        private MouseState previous_mouse;
        private KeyboardState _lastKeyboardState;
        private int cursorOnChunk = 0;
        private int lastPlayerChunk = 0;
        private List<int> ChunksToUpdate;
        public List<Mob> Mobs => mobs;
        private List<Mob> mobs;
        private Mob hoveredMob;
        private Texture2D mainMenuBackground;
        private float _mainMenuScrollOffset = 0f;
        private const float MainMenuScrollSpeed = 20f; // Pixels per second
        private List<Projectile> projectiles;
        //TODO remover
        private int cursorGridX = 0;
        private int cursorGridY = 0;

        private TextureRegion _cachedHeldItemTexture;
        private string _cachedHeldItemIcon;

        //timers
        float timer5s = 1000;
        const float TIMER5S = 5000;
        float timer2s = 1500;
        const float TIMER2S = 2500;
        float timer100ms = 0;
        const float TIMER_LIGHTING = 100; // 100ms periodic lighting update
        bool lightingDirty = false; // Set to true when tiles are modified

        public CraftingManager CraftingManager { get; private set; }

        /// <summary>
        /// messages
        /// </summary>
        private List<Misc.Message> Messages;

        #region Managers
        /// <summary>
        /// Background manager
        /// </summary>
        BackgroundManager backgroundManager;

        /// <summary>
        /// Damage number manager
        /// </summary>
        public DamageNumberManager DamageNumberManager;
        #endregion

        #endregion

        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.IsFullScreen = Global.FullScreen;
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = Global.WindowWidth;
            graphics.PreferredBackBufferHeight = Global.WindowHeight;
            _game = this;
            //Limits the framerate to 60 fps
            IsFixedTimeStep = false;
            //show or hide title bar
            Window.IsBorderless = true;
            Window.Position = new Point(50, 50);
            mobs = new List<Mob>();
            projectiles = new List<Projectile>();
        }
        public static void LogMessage(string message, Color? color, int timeout = 300)
        {
            if (color == null)
            {
                color = Color.White;
            }
            var instance = GetInstance();
            instance.LogMessage(message, color.Value, timeout);
        }
        public static Game GetInstance()
        {
            return _game;
        }

        public void LoadMap()
        {
            //do I have a map to load?
            if (map.CheckIfMapDataExists() == false)
            {
                map.mapManager.GenerateMap();
            }
            map.mapManager.LoadMap(player);
            // Initial chunk update to load area around player
            map.mapManager.UpdateChunks(player.GetPosition());
            _mainPanel.BuildActionBar(player);
            _mainPanel.BuildPlayerInventory(player);
        }

        public void SaveMap()
        {
            map.mapManager.SaveMap(player);
            LogMessage("Map saved successfully", Color.Green, 300);
        }

        #region Game Overrides
        protected override void Initialize()
        {
            map = new Map.Map();
            player = new Player();
            IsMouseVisible = true;
            this.Exiting += OnGameExiting;
            CraftingManager = new CraftingManager();
            base.Initialize();
        }
        protected override void LoadContent()
        {
            MyraEnvironment.Game = this;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            camera = new Camera(GraphicsDevice.Viewport);
            _debugFont = Content.Load<SpriteFont>("Fonts/FineFont");
            Messages = new List<Misc.Message>();
            ChunksToUpdate = new List<int>();

            backgroundManager = new BackgroundManager();

            backgroundManager.Load(Content, player);

            _desktop = new Desktop
            {
                // HasExternalTextInput = true
            };
            _mainPanel = new MainPanel();

            _desktop.Root = _mainPanel;

            _mainPanel.ShowWindows();

            player.Load(Content);

            mainMenuBackground = Content.Load<Texture2D>("UI/MainMenuBackground");

            //load tile data
            Global.ReferenceTiles = DataLoader.LoadTilesTypes(Content);
            //load item data
            Global.ReferenceItems = DataLoader.LoadItems(Content);
            //load mob data
            Global.ReferenceMobs = DataLoader.LoadMobs(Content);

            DamageNumberManager = new DamageNumberManager();
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        #region Updates
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (_state == GameState.Menu)
            {
                updateMenuState(gameTime);
            }

            else if (_state == GameState.Running && Global.IsMapLoaded)
            {
                updateRunningState(gameTime);
            }

            base.Update(gameTime);
        }
        void updateMenuState(GameTime gameTime)
        {
            _mainMenuScrollOffset += MainMenuScrollSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (mainMenuBackground != null)
            {
                _mainMenuScrollOffset %= mainMenuBackground.Width;
            }
        }
        void updateRunningState(GameTime gameTime)
        {
            // Update focus point for lighting optimization
            map.FocusPoint = new Point((int)player.GetPosition().X / Global.TileSize, (int)player.GetPosition().Y / Global.TileSize);
            
            // Process pending chunk loads/unloads
            map.mapManager.ProcessPendingChunks();
            
            // Input handling
            UpdateInputHandling(gameTime);
            //updates player           
            player.Update(gameTime, map);
                 
            // Check if player changed chunk to update loaded areas
            if (player.OnChunk != lastPlayerChunk)
            {
                map.mapManager.UpdateChunks(player.GetPosition());
                lastPlayerChunk = player.OnChunk;
                LogMessage($"Updated chunks around chunk {player.OnChunk}", Color.LightGreen, 300);
            }

            //update mobs
            for (int i = mobs.Count - 1; i >= 0; i--)
            {
                var mob = mobs[i];
                if (mob.Health < 1)
                {
                    mobs.RemoveAt(i);
                    LogMessage("Mob defeated", Color.OrangeRed, 100);
                    continue;
                }
                if (mob.MobType == MobType.Critter)
                {
                    // Despawn logic
                    float distance = Vector2.Distance(mob.GetPosition(), player.GetPosition());
                    if (distance > Global.MobDispawnDistance) // Approx 2 chunks width
                    {
                        mobs.RemoveAt(i);
                        LogMessage("Critter despawned", Color.Yellow, 100);
                        continue;
                    }

                    // Movement logic: Move to opposite side
                    if (mob.Target == null && mob.MobType != MobType.Critter)
                    {
                        Entity.Entity target = new Entity.Entity();
                        Vector2 targetPos;

                        // If on left side, go right. If on right side, go left.
                        if (mob.GetPosition().X < (map.Width * Global.TileSize) / 2)
                        {
                            targetPos = new Vector2(map.Width * Global.TileSize, mob.GetPosition().Y);
                        }
                        else
                        {
                            targetPos = new Vector2(0, mob.GetPosition().Y);
                        }

                        target.SetPosition(targetPos);
                        mob.Target = target;
                    }
                }
                else
                {
                    mob.Target = player; // Simple AI test: chase player
                }

                mob.Update(gameTime, map);
            }
            //update projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = projectiles[i];
                projectile.Update(gameTime, map);
                if (!projectile.IsActive)
                {
                    projectiles.RemoveAt(i);
                }
            }

            //update camera
            camera.Update(player.GetPosition(), map.Width, map.Height);
            
            //update background
            backgroundManager.Update(gameTime);
            //timer
            float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            timer5s -= elapsed;
            timer2s -= elapsed;
            timer100ms -= elapsed;
            if (timer5s < 0)
            {
                UpdateEvery5000ms(gameTime);
                timer5s = TIMER5S;
            }
            if (timer2s < 0)
            {
                UpdateEvery2000ms(gameTime);
                timer2s = TIMER2S;
            }

            // Lighting update logic: 
            // - Immediate update when a block is placed/removed (lightingDirty)
            // - Periodic background update every 100ms to catch edge cases

            if (timer100ms < 0)
            {
                UpdateEvery100ms(gameTime);
                timer100ms = TIMER_LIGHTING;
            }
            // Immediate lighting update if dirty
            if (lightingDirty)
            {
                UpdateLighting(gameTime);
            }

            map.UpdateModifiedTiles();
            map.water.Update(gameTime);
            DamageNumberManager.Update(gameTime);
        }
        void UpdateEvery100ms(GameTime gameTime)
        {
            //lighting
            if (!map.tileShadeMgr.IsUpdating)
            {
                map.tileShadeMgr.UpdateLightingAsync(gameTime, player.Layer, map.FocusPoint);
                lightingDirty = false;
            }
            //Frame rate figure
            Global.FrameRate = (Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds)).ToString();
            _mainPanel.UpdateFPS((int)(Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds)));
            //reset timer

        }
        void UpdateEvery2000ms(GameTime gameTime)
        {
            //check chunks for updates
            CheckChunkForUpdates();
        }
        void UpdateEvery5000ms(GameTime gameTime)
        {
            if (ChunksToUpdate.Any() == false)
            {
                for (int i = 0; i < map.Chunks.Length; i++)
                {
                    var chunk = map.Chunks[i];
                    if (chunk != null && chunk.HasGrass && chunk.NeedUpdate)
                    {
                        ChunksToUpdate.Add(i + 1); // 1-based chunkId
                    }
                }

                LogMessage("checking tiles for grass grow", Color.Red);
            }
        }
        void UpdateLighting(GameTime gameTime)
        {
            if (!map.tileShadeMgr.IsUpdating)
            {
                map.tileShadeMgr.UpdateLightingAsync(gameTime, player.Layer, map.FocusPoint);
                lightingDirty = false;
            }
        }
        void UpdateInputHandling(GameTime gameTime)
        {
            // Capture mouse state at start of Update so input handling is consistent
            previous_mouse = current_mouse;
            current_mouse = Mouse.GetState();
            // detect scroll wheel changes
            int scrollDelta = current_mouse.ScrollWheelValue - previous_mouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                // forward the event only when the game window is active and GUI doesn't own the input
                if (this.IsActive && (_desktop?.IsMouseOverGUI == false))
                {
                    OnScrollWheelChanged(-scrollDelta);
                }
            }
            //these actions should only be checked if the game windows is active
            HandleMouseEvents();

            HandleKeyboardEvents();
            Vector2 cursorPosition = Vector2.Transform(new Vector2(current_mouse.Position.X, current_mouse.Position.Y), Matrix.Invert(camera.Transform));
            var mouseY = (int)((cursorPosition.Y) / Global.TileSize) * Global.MapWidth;
            var mouseX = (int)((cursorPosition.X) / Global.TileSize);
            mouseIsOverBlock = (mouseX + mouseY);

            cursorGridX = (int)((cursorPosition.X) / Global.TileSize);
            cursorGridY = (int)((cursorPosition.Y) / Global.TileSize);
            int cursorChunkX = (cursorGridX / Global.ChunkSize);
            int cursorChunkY = (cursorGridY / Global.ChunkSize);
            cursorOnChunk = (1/*chunks are 1 based*/+ ((cursorChunkY * (Global.MapWidth / Global.ChunkSize)) + cursorChunkX));

            // Check for mob hover detection
            hoveredMob = null;
            foreach (var mob in mobs)
            {
                Rectangle mobBounds = mob.GetRectangle();
                Vector2 mobScreenPos = Vector2.Transform(mob.GetPosition(), camera.Transform);
                Rectangle mobScreenRect = new Rectangle((int)mobScreenPos.X, (int)mobScreenPos.Y, mobBounds.Width, mobBounds.Height);

                if (mobScreenRect.Contains(current_mouse.Position.X, current_mouse.Position.Y))
                {
                    hoveredMob = mob;
                    break;
                }
            }
        }
        #endregion
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            if (_state == GameState.Menu)
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);

                // Draw the background using a source rectangle that is offset by _mainMenuScrollOffset.
                // SamplerState.LinearWrap will handle the tiling.
                Rectangle destinationRectangle = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
                Rectangle sourceRectangle = new Rectangle((int)_mainMenuScrollOffset, 0, mainMenuBackground.Width, mainMenuBackground.Height);
                spriteBatch.Draw(mainMenuBackground, destinationRectangle, sourceRectangle, Color.White);
                spriteBatch.End();
            }
            else
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, camera.Transform);

                backgroundManager.Draw(gameTime, spriteBatch);

                if (_state == GameState.Running && Global.IsMapLoaded)
                {
                    map.Draw(spriteBatch, player.OnChunk);

                    player.Draw(spriteBatch);
                }
                foreach (var mob in mobs)
                {
                    mob.Draw(spriteBatch);
                }

                if (hoveredMob != null && _state == GameState.Running)
                {
                    hoveredMob.DrawHealthDisplay(spriteBatch, _debugFont, hoveredMob.GetPosition());
                }

                foreach (var projectile in projectiles)
                {
                    projectile.Draw(spriteBatch);
                }

                DamageNumberManager.Draw(spriteBatch, _debugFont);

                //Cursor info (mouse state is captured in Update)
                Global.CursorX = current_mouse.Position.X;
                Global.CursorY = current_mouse.Position.Y;

                if (Global.isDebugging)
                {
                    UpdateDebugInformation();
                }

                //messages
                for (int i = Messages.Count - 1; i >= 0; i--)
                {
                    var mess = Messages[i];
                    if (mess.Timeout > 0)
                    {
                        DrawWithShadow(mess.Text, new Vector2(camera.Center.X - (((Global.WindowWidth / 2) - 20)), camera.Center.Y + ((Global.WindowHeight / 2) - 40) - (mess.Id * 20)), mess.Color);
                        mess.Timeout--;
                    }
                    else
                    {
                        Messages.RemoveAt(i);
                    }
                }
                spriteBatch.End();
            }
            base.Draw(gameTime);
            _desktop.Render();

            // Draw Held Item
            if (Global.HeldItem != null)
            {
                spriteBatch.Begin();
                var heldItem = Global.HeldItem;
                if (_cachedHeldItemIcon != heldItem.Item.UIIcon)
                {
                    _cachedHeldItemIcon = heldItem.Item.UIIcon;
                    _cachedHeldItemTexture = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{heldItem.Item.UIIcon}.png");
                }

                // Draw icon
                if (_cachedHeldItemTexture != null)
                {
                    spriteBatch.Draw(_cachedHeldItemTexture.Texture, new Rectangle(current_mouse.X - 16, current_mouse.Y - 16, 32, 32), _cachedHeldItemTexture.Bounds, Color.White);
                }

                // Draw quantity
                string text = heldItem.Quantity.ToString();
                spriteBatch.DrawString(_debugFont, text, new Vector2(current_mouse.X, current_mouse.Y), Color.White);

                spriteBatch.End();
            }
        }
        #endregion

        #region Misc
        public void LogMessage(string message, Color color, int timeout = 300)
        {
            //drawstring cannot be called at will, it must be called within the draw event
            //in this case a list of messages must be defined and then when the game is drawing, 
            //this list must be called and then the messages will be shown
            //also a timeout must be defined to define for how long the messages will be displayed
            //DrawWithShadow(message, new Vector2(camera.Center.X + ((Global.WindowWidth/2)-20), camera.Center.Y + ((Global.WindowHeight / 2) - 20)),color);
            if (Messages.Any(x => x.Text == message))
            {
                var ms = Messages.FirstOrDefault(x => x.Text == message);
                ms.Timeout = timeout;
            }
            else
            {
                var mess = new Misc.Message
                {
                    Text = message,
                    Color = color,
                    Timeout = timeout,
                    Id = Messages.Count
                };
                Messages.Add(mess);
            }

        }
        #endregion

        #region Event Handlers
        private void HandleMouseEvents()
        {
            if (IsActive)
            {
                //temporary handlers for the buttons
                if (current_mouse.LeftButton == ButtonState.Pressed && _desktop.IsMouseOverGUI == false)
                {
                    int actionBarIndex = _mainPanel.SelectedItem;
                    var inventoryItem = player.ActionBar[actionBarIndex];
                    if (inventoryItem is null)
                    {
                        //no item selected
                        return;
                    }
                    var item = inventoryItem.Item;

                    if (player.UseCooldown <= 0)
                    {
                        if (Keyboard.GetState().IsKeyDown(Keys.B))
                        {
                            PlaceBackgroundTile(item);
                        }
                        else
                        {
                            try
                            {
                                if (item.IsTile)
                                {
                                    PlaceTile(item, actionBarIndex);
                                }
                                else if (item.IsPlaceable)
                                {
                                    PlaceItem(item, actionBarIndex);
                                }
                                else if (item.IsTool)
                                {
                                    UseTool(item);
                                }
                            }
                            catch (Exception e)
                            {
                                LogMessage($"Error using item:{e.Message}", Color.Red);
                            }
                        }
                    }
                }
                else if (current_mouse.RightButton == ButtonState.Pressed && _desktop.IsMouseOverGUI == false && previous_mouse.RightButton == ButtonState.Released)
                {
                    if (Keyboard.GetState().IsKeyDown(Keys.B))
                    {
                        //if (player.UseCooldown <= 0)
                        //{
                        //    try
                        //    {
                        //        map.SetBackgroundTile(cursorOnChunk, mouseIsOverBlock, 0);
                        //        player.UseCooldown = 200;
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        LogMessage("Failed to set background: " + ex.Message, Color.Red);
                        //    }
                        //}
                    }
                    else
                    {
                        // Check for interactive item first
                        var targetTile = map.GetTileByGlobalId(mouseIsOverBlock);
                        if (targetTile != null)
                        {
                            // If it's a multi-tile part, find the master tile
                            if (targetTile.MultiTileOffset != Point.Zero)
                            {
                                targetTile = map.GetTileAt(targetTile.X + targetTile.MultiTileOffset.X, targetTile.Y + targetTile.MultiTileOffset.Y);
                            }

                            if (targetTile != null && targetTile.PlacedItem != null && targetTile.PlacedItem.IsInteractive)
                            {
                                // Proximity Check: Player must be close to the item
                                float distance = Vector2.Distance(player.GetPosition(), new Vector2(targetTile.X * Global.TileSize, targetTile.Y * Global.TileSize));
                                if (distance <= 5 * Global.TileSize) // 5 tiles range
                                {
                                    HandleInteraction(targetTile.PlacedItem);
                                    return;
                                }
                                else
                                {
                                    LogMessage("You are too far away!", Color.Yellow, 100);
                                }
                            }
                        }

                        //if (player.UseCooldown <= 0)
                        //{
                        //    if (map.IsBlockOnChunk(cursorOnChunk, mouseIsOverBlock))
                        //    {
                        //        var dropped = map.PerformActionOnTile(cursorOnChunk, mouseIsOverBlock, ToolAction.MineBlock);
                        //        foreach (var drop in dropped)
                        //        {
                        //            player.AddItem(drop, 1);
                        //        }
                        //        lightingDirty = true;
                        //        player.UseCooldown = 200; // Standard block mining cooldown
                        //    }
                        //    else
                        //    {
                        //        LogMessage("Block ID " + mouseIsOverBlock + " was not present at chunk " + cursorOnChunk, Color.Red);
                        //    }
                        //}
                    }
                }
                //leave game
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                    Exit();
            }
        }

        private void PlaceBackgroundTile(Item item)
        {
            try
            {
                // Only Tiles can be placed as background (walls)
                if (item.IsTile)
                {
                    map.SetBackgroundTile(cursorOnChunk, mouseIsOverBlock, item.TileId);
                    player.UseCooldown = item.UseTime;
                }
            }
            catch (Exception ex)
            {
                LogMessage("Failed to set background: " + ex.Message, Color.Red);
            }
        }

        private void PlaceTile(Item item, int actionBarIndex)
        {
            if (map.SetTile(cursorOnChunk, mouseIsOverBlock, item.TileId))
            {
                player.RemoveItemFromSlot(actionBarIndex, 1);
                lightingDirty = true;
                player.UseCooldown = item.UseTime;
            }
        }

        private void PlaceItem(Item item, int actionBarIndex)
        {
            map.PlaceItem(cursorOnChunk, mouseIsOverBlock, item);
            player.RemoveItemFromSlot(actionBarIndex, 1);
            lightingDirty = true;
            player.UseCooldown = item.UseTime;
        }

        private void UseTool(Item item)
        {
            if (item.ToolAction == ToolAction.RangedWeapon)
            {
                if (item.RequiresAmmo)
                {
                    if (player.HasAmmo(item.RequiredAmmoType, out var ammoInvItem))
                    {
                        SpawnProjectile(player, item, ammoInvItem.Item);
                        player.ConsumeAmmo(item.RequiredAmmoType);
                        player.UseCooldown = item.UseTime;
                    }
                    else
                    {
                        LogMessage("No ammunition!", Color.Red, 100);
                    }
                }
                else
                {
                    SpawnProjectile(player, item, item); // Self as ammo if none required? 
                    player.UseCooldown = item.UseTime;
                }
            }
            else
            {
                var dropped = map.PerformActionOnTile(cursorOnChunk, mouseIsOverBlock, item.ToolAction, item);
                foreach (var drop in dropped)
                {
                    player.AddItem(drop, 1);
                }
                player.UseCooldown = item.UseTime;
            }
        }
     
        private void HandleInteraction(Item item)
        {
            if (item.InteractionType == InteractionType.Crafting)
            {
                // Open Crafting UI
                //if (_craftingWindow == null)
                //{
                //    _craftingWindow = new CraftingWindow();
                //}

                //if (!_desktop.Widgets.Contains(_craftingWindow))
                //{
                //    _craftingWindow.Build(player, CraftingManager, "Crafting");
                //    _desktop.Widgets.Add(_craftingWindow);
                //}
                //_craftingWindow.Show(_desktop);
                _mainPanel.BuildAndDisplayCraftingWindow(player, CraftingManager, "Crafting");
            }
            else if (item.InteractionType == InteractionType.Container)
            {
                // Find tile at mouse position to get ContainerId
                var targetTile = map.GetTileByGlobalId(mouseIsOverBlock);
                if (targetTile != null)
                {
                    // If it's a multi-tile part, find the master tile
                    if (targetTile.MultiTileOffset != Point.Zero)
                    {
                        targetTile = map.GetTileAt(targetTile.X + targetTile.MultiTileOffset.X, targetTile.Y + targetTile.MultiTileOffset.Y);
                    }

                    if (targetTile != null && targetTile.ContainerId.HasValue)
                    {
                        var container = ContainerManager.GetContainer(targetTile.ContainerId.Value);
                        if (container != null)
                        {
                            //if (_containerInventoryWindow == null)
                            //{
                            //    _containerInventoryWindow = new ContainerInventoryWindow();
                            //}

                            //if (!_desktop.Widgets.Contains(_containerInventoryWindow))
                            //{
                            //    _containerInventoryWindow.BuildInventory(container.Items, item.Name);
                            //    _desktop.Widgets.Add(_containerInventoryWindow);
                            //}
                            //_containerInventoryWindow.Show(_desktop);
                            _mainPanel.BuildAndDisplayContainerInventory(container.Items, item.Name);
                        }
                    }
                }
            }
            else
            {
                LogMessage($"Interaction {item.InteractionType} not implemented yet.", Color.Yellow, 100);
            }
        }

        private void HandleKeyboardEvents()
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // Only toggle if Enter was JUST pressed this frame
            if (currentKeyboardState.IsKeyDown(Keys.Enter) && _lastKeyboardState.IsKeyUp(Keys.Enter))
            {
                _mainPanel.ToggleCommand();
                player.InterruptInput = true;
            }

            _lastKeyboardState = currentKeyboardState;
        }
  
        public void GenericAction()
        {
            //map.GrowGrass(player.onChunk);
            FeatureGenerator.GrowTree(map, player.OnChunk, player.OnBlock);
        }
        private void OnScrollWheelChanged(int delta)
        {
            // raise event for external subscribers
            ScrollWheelChanged?.Invoke(delta);
            _mainPanel.ChangeActionBarSelectedItem(delta > 0 ? 1 : -1);
        }

        private void SpawnProjectile(Player player, Item weapon, Item ammo)
        {
            Vector2 playerCenter = player.GetPosition() + new Vector2(player.GetRectangle().Width / 2f, player.GetRectangle().Height / 2f);
            Vector2 cursorWorldPos = Vector2.Transform(new Vector2(current_mouse.Position.X, current_mouse.Position.Y), Matrix.Invert(camera.Transform));

            Vector2 direction = cursorWorldPos - playerCenter;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }
            else
            {
                direction = new Vector2(1, 0); // Default if cursor on player
            }

            // weapon.RangedVelocity is scaled to match game's pixel-per-second expectations
            Vector2 initialVelocity = direction * weapon.RangedVelocity * 100f;

            float maxDistance = weapon.RangedDistance * Global.TileSize;
            if (maxDistance <= 0) maxDistance = 2000f; // Default long distance if not set

            int totalDamage = weapon.WeaponDamage + ammo.WeaponDamage;
            float totalKnockback = weapon.WeaponKnockback + ammo.WeaponKnockback;

            Projectile projectile = new Projectile(ammo, playerCenter, initialVelocity, maxDistance, totalDamage, totalKnockback);
            projectiles.Add(projectile);
        }
        private void OnGameExiting(object sender, EventArgs e)
        {
            if (map != null && map.mapManager != null)
            {
                System.Diagnostics.Debug.WriteLine("Saving map before exit...");
                map.mapManager.SaveMap(player);
            }
        }
        #endregion

        #region Debug
        private void UpdateDebugInformation()
        {
            Vector2 worldPosition = Vector2.Transform(new Vector2(current_mouse.Position.X, current_mouse.Position.Y), Matrix.Invert(camera.Transform));

            string cameraPosition = string.Format("({0:0.0}, {1:0.0})", GraphicsDevice.Viewport.X, GraphicsDevice.Viewport.Y);
            string mapSize = map.Width + " x " + map.Height;
            string playerGrid = player.GridX + " x " + player.GridY;
            string cursorGrid = cursorGridX + " x " + cursorGridY;
            string isMoving = player.isMoving.ToString();
            string velocity = "x:" + player.velocity.X + " y:" + player.velocity.Y;
            string playerInside = player.OnBlock.ToString();
            string playerOnLayer = player.Layer.ToString();
            string playerSteppingOn = player.SteppingOn.ToString();
            string playerOnChunk = player.OnChunk.ToString();
            string playerOnSolidGround = player.IsOnSolidBlock.ToString();
            string mouseOnChunk = cursorOnChunk.ToString();
            string mousePos = worldPosition.X + " x " + worldPosition.Y;
            string mouseBlockIn = mouseIsOverBlock.ToString();

            TileMaster.Entity.Tiles.Tile block = null;
            if (IsActive)
            {
                block = map.GetTileAt(cursorGridX, cursorGridY);
            }

            _mainPanel.UpdateDebugInfo(
                cameraPosition, mapSize, playerGrid, cursorGrid,
                isMoving, velocity, playerInside, playerOnLayer,
                playerSteppingOn, playerOnChunk, playerOnSolidGround, mouseOnChunk,
                mousePos, mouseBlockIn, block);
        }
        private void DrawWithShadow(string text, Vector2 position, Color color)
        {
            spriteBatch.DrawString(_debugFont, text, position, Color.White);
        }
        private void CheckChunkForUpdates()
        {
            if (Global.updatePlayerChunkOnly)
            {
                // Run on main thread to avoid race conditions and crashes
                map.grass.GrowGrass(player.OnChunk);
                // Lighting is handled via dirty flag, but grass growth may affect light.
                // Mark lighting dirty to update on next cycle.
                lightingDirty = true;
                ChunksToUpdate.Remove(player.OnChunk);
                LogMessage("Checking Chunk " + player.OnChunk + " for grass growth", Color.Green, 180);
            }
            else if (ChunksToUpdate.Any())
            {
                int chunkId = ChunksToUpdate.FirstOrDefault();
                // Run on main thread
                // map.grass.GrowGrass(chunkId);
                ChunksToUpdate.Remove(chunkId);
                LogMessage("Checking Chunk " + chunkId + " for grass growth", Color.Green, 180);
            }

            //test remove
            map.grass.GrowGrass(player.OnChunk);
        }
        #endregion

        #region Commands
        public void ProccessCommand(string command)
        {
            player.InterruptInput = false;
            var commandParts = (command.ToLower()).Split(' ');
            if (commandParts[0] == "add")
            {
                ProcessAdd(commandParts.Skip(1).ToArray());
            }
            if (commandParts[0] == "set")
            {
                ProcessSet(commandParts.Skip(1).ToArray());
            }
        }
        private void ProcessAdd(string[] commandParts)
        {
            if (commandParts.Length < 2)
            {
                LogMessage("Usage: add <entity>", Color.Red);
                return;
            }

            if (commandParts[0] == "tile")
            {
                AddTile(commandParts.Skip(1).ToArray());
            }
            else if (commandParts[0] == "item")
            {
                AddItem(commandParts.Skip(1).ToArray());
            }
            else if (commandParts[0] == "mob")
            {
                AddMob(commandParts.Skip(1).ToArray());
            }
        }
        private void ProcessSet(string[] commandParts)
        {
            if (commandParts.Length < 2)
            {
                LogMessage("Usage: set <entity>", Color.Red);
                return;
            }

            if (commandParts[0] == "tile")
            {
                AddTile(commandParts.Skip(1).ToArray());
            }
        }
        private void AddTile(string[] commandParts)
        {
            if (commandParts.Length < 2)
            {
                LogMessage("Usage: add tile <tileId> (<x>,<y>)", Color.Red);
                return;
            }
            try
            {
                int tileId = int.Parse(commandParts[0]);
                var coordinates = GetCoordinatesFromString(commandParts[1]);
                var testTile = map.GetTileAt(coordinates.Item1, coordinates.Item2);
                map.SetTile(testTile.ChunkId, testTile.GlobalId, tileId);
                LogMessage($"Added tile {tileId} at block {testTile.GlobalId} on chunk {testTile.ChunkId}", Color.Green);
            }
            catch (Exception ex)
            {
                LogMessage("Error adding tile: " + ex.Message, Color.Red);
            }
        }

        private void AddItem(string[] commandParts)
        {
            if (commandParts.Length < 2)
            {
                LogMessage("Usage: add item <name> (<x>,<y>)", Color.Red);
                return;
            }
            try
            {
                var item = commandParts[0];
                var coordinates = GetCoordinatesFromString(commandParts[1]);

                var testTile = map.GetTileAt(coordinates.Item1, coordinates.Item2);
                if (testTile != null)
                {
                    var torch = Global.ReferenceItems[(int)Items.Torch];
                    map.PlaceItem(cursorOnChunk, mouseIsOverBlock, torch);
                }



            }
            catch (Exception ex)
            {
                Game.LogMessage("TEST FAILED: " + ex.Message, Color.Red, 500);
            }
        }

        private void AddMob(string[] commandParts)
        {
            var mob = new Mob();
            var coordinates = GetCoordinatesFromString(commandParts[1]);
            Vector2 position = new Vector2(coordinates.Item1 * Global.TileSize, coordinates.Item2 * Global.TileSize);
            //mob.Load(Content, position, "Slime", 100, new Hop());
            int mobId = Convert.ToInt32(commandParts[0]);
            mob.Load(Content, position, Global.ReferenceMobs[mobId]);
            mobs.Add(mob);
        }



        (int, int) GetCoordinatesFromString(string coordinates)
        {
            if (coordinates == "cursor" || coordinates == "cur" || coordinates == "c")
            {
                return (cursorGridX, cursorGridY);
            }
            //extract x, y from string (format: x,y)
            Match match = Regex.Match(coordinates, @"\((\d+),(\d+)\)");

            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);
                return (x, y);
            }
            return (0, 0);

        }
    }
    #endregion
}