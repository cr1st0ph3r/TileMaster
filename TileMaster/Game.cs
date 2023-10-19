using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using TileMaster.Entity;
using TileMaster.Manager;
using TileMaster.UI;

namespace TileMaster
{
    public class Game : Microsoft.Xna.Framework.Game
    {
        public MainPanel _mainPanel;
        public static GameState _state;
        private Desktop _desktop;
        #region Variables
        private static Game _game;
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private Map map;
        private Player player;
        public static Camera camera;
        private SpriteFont _debugFont;
        public int mouseIsOverBlock;
        private int[,] initialArrayMap;
        public static readonly Random rnd = new(DateTime.Now.GetHashCode());
        private MouseState current_mouse;
        private int cursorOnChunk = 0;
        List<int> ChunksToUpdate;

        //TODO remover
        private int cursorGridX = 0;
        private int cursorGridY = 0;

        //timers
        float timer5s = 1000;
        const float TIMER5S = 5000;
        float timer2s = 1500;
        const float TIMER2S = 2500;


        /// <summary>
        /// messages
        /// </summary>
        private List<Misc.Message> Messages;

        #region Managers
        /// <summary>
        /// Background manager
        /// </summary>
        BackgroundManager bgMgr;
        /// <summary>
        /// button manager
        /// </summary>
        ButtonManager buttonMgr;
        /// <summary>
        /// TileManager
        /// </summary>
        /// 
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
            IsFixedTimeStep = false;

        }

        public static Game GetInstance()
        {
            if (_game == null)
                throw new Exception("Call SetInstance() with a valid object");
            return _game;
        }
        public void LoadMap()
        {
            //if (Global.GenerateMapOnStartup)
            //{
            //    var sw = new Stopwatch();
            //    sw.Start();

            //    initialArrayMap = Util.MapGenerator.GenerateRandomMap();
            //    map.GenerateMapDictionary(initialArrayMap);
            //    map.SaveMap();

            //    sw.Stop();
            //    var time = sw.Elapsed.TotalSeconds;
            //}

            //do I have a map to load?
            if (map.CheckIfMapDataExists() == false)
            {
                initialArrayMap = Util.MapGenerator.GenerateRandomMap();
                map.GenerateMapDictionary(initialArrayMap);
                map.SaveMap();
            }
            map.LoadMap(); 
        }
        protected override void Initialize()
        {
            map = new Map();
            player = new Player();
            IsMouseVisible = true;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            MyraEnvironment.Game = this;
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            camera = new Camera(GraphicsDevice.Viewport);
            _debugFont = Content.Load<SpriteFont>("Fonts/Font");
            Messages = new List<Misc.Message>();
            Tile.Content = Content;
            ChunksToUpdate = new List<int>();

            bgMgr = new BackgroundManager();
            buttonMgr = new ButtonManager();

            buttonMgr.CreateButton(Content, "Generic Button", GenericAction, new Vector2(1500, 1000), 1);
            bgMgr.Load(Content, player);





            _desktop = new Desktop
            {
                HasExternalTextInput = true
            };
            _mainPanel = new MainPanel();

            _desktop.Root = _mainPanel;

            _mainPanel.ShowWindows();

            player.Load(Content);

        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Game._state == GameState.Running && Global.isMapLoaded)
            {

                //updates the player info about block positioning
                int playerOnGridX = (int)((player.Position.X + (player.Rectangle.Width / 2)) / Global.TileSize);
                int playerOnGridY = (int)((player.Position.Y + (player.Rectangle.Height)) / Global.TileSize);
                player.onBlock = (playerOnGridY * Global.MapWidth) + (playerOnGridX);
                player.SteppingOn = (int)(player.onBlock + Global.MapWidth);
                player.GridX = (int)((player.Position.X + (player.Rectangle.Width / 2)) / Global.TileSize);
                player.GridY = (int)((player.Position.Y + (player.Rectangle.Height)) / Global.TileSize);
                int playerChunkX = (int)(player.GridX / Global.ChunkSize);
                int playerChunkY = (int)(player.GridY / Global.ChunkSize);
                player.onChunk = (1/*chunks are 1 based*/+ ((playerChunkY * (Global.MapWidth / Global.ChunkSize)) + playerChunkX));
                Vector2 cursorPosition = Vector2.Transform(new Vector2(current_mouse.Position.X, current_mouse.Position.Y), Matrix.Invert(camera.Transform));
                mouseIsOverBlock = ((int)((cursorPosition.Y) / Global.TileSize) * Global.MapWidth + (int)((cursorPosition.X) / Global.TileSize) + Global.MapWidth);
                cursorGridX = (int)((cursorPosition.X) / Global.TileSize);
                cursorGridY = (int)((cursorPosition.Y) / Global.TileSize) + 1;
                int cursorChunkX = (int)(cursorGridX / Global.ChunkSize);
                int cursorChunkY = (int)(cursorGridY / Global.ChunkSize);
                cursorOnChunk = (1/*chunks are 1 based*/+ ((cursorChunkY * (Global.MapWidth / Global.ChunkSize)) + cursorChunkX));

                //these actions should only be checked if the game windows is active
                HandleMouseEvents();
                //updates player
                player.Update(gameTime, player, map);

                //if (mouseIsOverBlock > 0 && mouseIsOverBlock < map.MapDictionary.Last().Key)
                //{
                //    map.MapDictionary[mouseIsOverBlock].Color = "Gold";
                //}
                //map.MapDictionary[player.SteppingOn].Color = "Red";

                camera.Update(player.Position, map.Width, map.Height);

                bgMgr.Update(gameTime);

                buttonMgr.Update(gameTime);

                //timer
                float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                timer5s -= elapsed;
                timer2s -= elapsed;
                if (timer5s < 0)
                {
                    timer5s = TIMER5S;

                    if (ChunksToUpdate.Any() == false)
                    {
                        foreach (var chunk in map.ChunkDictionary.Where(x => x.Value.HasGrass && x.Value.NeedGrassUpdate))
                        {
                            ChunksToUpdate.Add(chunk.Key);
                        }
                        LogMessage("checking tiles for grass grow", Color.Red);
                    }



                }
                if (timer2s < 0)
                {
                    timer2s = TIMER2S;
                    CheckChunkForGrass();
                }
                _mainPanel._horizontalProgressBar.Visible = false;
            }
            //handle the progress of loading the map 
            _mainPanel._horizontalProgressBar.Value = Map.Progress;


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, camera.Transform);



            bgMgr.Draw(gameTime, spriteBatch);

            if (_state == GameState.Running&&Global.isMapLoaded)
            {
                map.Draw(spriteBatch, player.onChunk);

                player.Draw(spriteBatch);
            }

            //Cursor info
            current_mouse = Mouse.GetState();
            Global.CursorX = (int)current_mouse.Position.X;
            Global.CursorY = (int)current_mouse.Position.Y;

            if (Global.isDebugging)
            {
                WriteDebugInformation();
            }

            //messages
            foreach (var mess in Messages.ToList())
            {
                if (mess.Timeout > 0)
                {
                    DrawWithShadow(mess.Text, new Vector2(camera.Center.X - (((Global.WindowWidth / 2) - 20)), camera.Center.Y + ((Global.WindowHeight / 2) - 40) - (mess.Id * 20)), mess.Color);
                    mess.Timeout--;
                }
                else
                {
                    Messages.Remove(mess);
                }

            }

            Global.FrameRate = (Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds)).ToString();
            _mainPanel.UpdateFPS((int)(Math.Round(1 / gameTime.ElapsedGameTime.TotalSeconds)));


            buttonMgr.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            base.Draw(gameTime);
            _desktop.Render();

        }

        #region Misc
        public void LogMessage(string message, Color color, int timeout = 300)
        {

            //drawstring cannot be called at will, it must be called within the draw event
            //in this case a list of messages must be defined and then when the game is drawing, 
            //this list must be called and then the messages will be shown
            //also a timeout must be defined to define for how long the messages will be displayed
            //DrawWithShadow(message, new Vector2(camera.Center.X + ((Global.WindowWidth/2)-20), camera.Center.Y + ((Global.WindowHeight / 2) - 20)),color);
            if (Messages.ToList().Any(x => x.Text == message))
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

        #region Debug
        private void WriteDebugInformation()
        {


            float debugXCoordinate = camera.Center.X - 800;
            float debugYCoordinate = camera.Center.Y - 500;
            Vector2 worldPosition = Vector2.Transform(new Vector2(current_mouse.Position.X, current_mouse.Position.Y), Matrix.Invert(camera.Transform));

            mouseIsOverBlock = (((int)((worldPosition.Y) / 16)) * Global.MapWidth + (int)((worldPosition.X) / 16) + Global.MapWidth);



            _mainPanel.UpdatePlayerPos((int)player.Position.X, (int)player.Position.Y);

            string cameraPosition = string.Format("Camera Position: ({0:0.0}, {1:0.0})", GraphicsDevice.Viewport.X, GraphicsDevice.Viewport.Y);

            string Map = "Map:" + map.Width + " x " + map.Height;

            //PLAYER
            string gridTest = "Player na grid:" + player.GridX + " x " + player.GridY;
            string gridTestCursor = "Player na grid:" + cursorGridX + " x " + cursorGridY;
            string isPlayerMoving = "isMoving?:" + player.isMoving;
            string playerVelocities = "Player Velocities: x" + player.velocity.X + "y:" + player.velocity.Y;
            string playerInside = "Player is inside of block n.: " + (player.onBlock);
            string playerSteppingOn = "Player is stepping on block n.: " + (player.SteppingOn);
            string playerOnChunk = "Player is inside chunk n.: " + player.onChunk;
            string playerOnSolidGround = "Player is on solid ground? " + player.isOnSolidBlock;
            //cursor
            string mouseOnChunk = "Cursor is on chunk: " + cursorOnChunk;
            string MousePos = "Cursor on map:" + worldPosition.X + " x " + worldPosition.Y;
            string mouseBlockIn = "Cursor over block:" + mouseIsOverBlock;

            if (IsActive)
            {
                if (map.IsBlockOnChunk(cursorOnChunk, mouseIsOverBlock))
                {

                    var block = map.ChunkDictionary[cursorOnChunk].Tiles[mouseIsOverBlock];
                    DrawWithShadow("Tile TileId:" + block.TileId, new Vector2(debugXCoordinate + 350, debugYCoordinate));
                    DrawWithShadow("Tile Name:" + block.Name, new Vector2(debugXCoordinate + 350, debugYCoordinate + 20));
                    DrawWithShadow("Tile Local Id:" + block.LocalId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 40));
                    DrawWithShadow("Tile Global Id:" + block.GlobalId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 60));
                    DrawWithShadow("Tile Chunk Id:" + block.ChunkId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 80));
                    DrawWithShadow("Is Edge Tile?: " + block.isEdgeTile, new Vector2(debugXCoordinate + 350, debugYCoordinate + 100));

                    DrawWithShadow("Tile from global map: ", new Vector2(debugXCoordinate + 350, debugYCoordinate + 140));
                    DrawWithShadow("edge?: " + map.MapDictionary[block.GlobalId].isEdgeTile, new Vector2(debugXCoordinate + 350, debugYCoordinate + 160));
                    DrawWithShadow("chunkId: " + map.MapDictionary[block.GlobalId].ChunkId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 180));
                    DrawWithShadow("localId: " + map.MapDictionary[block.GlobalId].LocalId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 200));
                    DrawWithShadow("globalId: " + map.MapDictionary[block.GlobalId].GlobalId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 220));
                    DrawWithShadow("TileId: " + map.MapDictionary[block.GlobalId].TileId, new Vector2(debugXCoordinate + 350, debugYCoordinate + 240));
                    DrawWithShadow("Name: " + map.MapDictionary[block.GlobalId].Name, new Vector2(debugXCoordinate + 350, debugYCoordinate + 260));

                }
                //else
                //{
                //    var block = map.MapDictionary[mouseIsOverBlock];
                //    DrawWithShadow("Tile TileId:" + block.GlobalId + " is expected to be on chunk:" + block.ChunkId, new Vector2(debugXCoordinate + 350, debugYCoordinate));
                //}
            }





            buttonMgr.UpdateButton(1, new Vector2(debugXCoordinate, debugYCoordinate + 500));

            //DrawWithShadow(positionInText, new Vector2(debugXCoordinate, debugYCoordinate));
            DrawWithShadow(cameraPosition, new Vector2(debugXCoordinate, debugYCoordinate + 20));
            DrawWithShadow(playerOnChunk, new Vector2(debugXCoordinate, debugYCoordinate + 40));
            DrawWithShadow(playerOnSolidGround, new Vector2(debugXCoordinate, debugYCoordinate + 60));
            DrawWithShadow(Map, new Vector2(debugXCoordinate, debugYCoordinate + 80));
            DrawWithShadow(gridTest, new Vector2(debugXCoordinate, debugYCoordinate + 100));
            DrawWithShadow(gridTestCursor, new Vector2(debugXCoordinate, debugYCoordinate + 120));
            DrawWithShadow(isPlayerMoving, new Vector2(debugXCoordinate, debugYCoordinate + 140));
            DrawWithShadow(playerVelocities, new Vector2(debugXCoordinate, debugYCoordinate + 160));
            DrawWithShadow(playerSteppingOn, new Vector2(debugXCoordinate, debugYCoordinate + 180));
            DrawWithShadow(playerInside, new Vector2(debugXCoordinate, debugYCoordinate + 200));

            DrawWithShadow(mouseBlockIn, new Vector2(debugXCoordinate, debugYCoordinate + 240));
            DrawWithShadow(MousePos, new Vector2(debugXCoordinate, debugYCoordinate + 260));
            DrawWithShadow(mouseOnChunk, new Vector2(debugXCoordinate, debugYCoordinate + 280));



            //DrawWithShadow("Tile type: "+map.ChunkDictionary[cursorOnChunk].Tiles[mouseIsOverBlock].texture.Name, new Vector2(debugXcoordinate, debugYcoordinate + 240));


            //DrawWithShadow("FPS: " + Global.FrameRate, new Vector2(debugXCoordinate, debugYCoordinate + 350));

        }
        private void DrawWithShadow(string text, Vector2 position)
        {
            spriteBatch.DrawString(_debugFont, text, position + Vector2.One, Color.Black);
            spriteBatch.DrawString(_debugFont, text, position, Color.LightYellow);
        }
        private void DrawWithShadow(string text, Vector2 position, Color color)
        {
            spriteBatch.DrawString(_debugFont, text, position, Color.White);
            spriteBatch.DrawString(_debugFont, text, position + Vector2.One, color);
        }
        private void CheckChunkForGrass()
        {
            if (ChunksToUpdate.Any())
            {
                int chunkId = ChunksToUpdate.FirstOrDefault();
                Thread thread = new Thread(() =>
                {
                    map.GrowGrass(chunkId);
                    ChunksToUpdate.Remove(chunkId);
                });
                thread.Start();
                LogMessage("Checking Chunk " + chunkId + " for grass growth", Color.Green, 180);

            }

        }
        #endregion

        #region Event Handlers
        private void HandleMouseEvents()
        {
            if (this.IsActive && Global.isCursorOverAButton == false)
            {
                //temporary handlers for the buttons
                if (current_mouse.LeftButton == ButtonState.Pressed && _desktop.IsMouseOverGUI == false)
                {
                    try
                    {
                        map.PlaceBlockAt((int)TileType.Dirt, mouseIsOverBlock, cursorOnChunk);
                    }
                    catch
                    {
                        //mouse clicked outside the game context
                        //for the mean time this can be neglected
                    }
                }
                if (current_mouse.RightButton == ButtonState.Pressed)
                {

                    if (map.IsBlockOnChunk(cursorOnChunk, mouseIsOverBlock))
                    {
                        map.PlaceBlockAt((int)TileType.Air, mouseIsOverBlock, cursorOnChunk);
                    }
                    else
                    {
                        LogMessage("Block ID " + mouseIsOverBlock + " was not present at chunk " + cursorOnChunk, Color.Red);
                        //for some reason the first line of blocks of a given chunk is not being recognized as being of the said chunk
                        //but instead of the next (+8) chunk, for now this fixes the problem, but it need to be addressed
                        //if (map.IsBlockOnChunk(cursorOnChunk + 8, mouseIsOverBlock))
                        //{
                        //    map.PlaceBlockAt((int)TileType.Air, mouseIsOverBlock, cursorOnChunk + 8);
                        //}
                    }
                }
                //leave game
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                    Exit();
            }
        }
        private void GenericAction(object sender, EventArgs e)
        {

            //map.GrowGrass(player.onChunk);
            map.GrowTree(player.onChunk, player.onBlock);

        }
        #endregion
    }
}
