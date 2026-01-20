using AssetManagementBase;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using TileMaster.Entity;
using TileMaster.Entity.Enums;

namespace TileMaster.UI
{
    public partial class MainPanel
    {
        private readonly DebugWindow _debugWindow = new DebugWindow();
        InventoryWindow inventoryWindow;
        public Panel ActionBarPanel;
        public static CommonComponents CommonComponents = new CommonComponents();
        private int selectedIndex = 0;
        public int SelectedItem = 1;
        public MainPanel()
        {
            BuildUI();

            _debugButton.PressedChanged += _button1_PressedChanged;
            _loadMapButton.PressedChanged += _loadMapButton_PressedChanged;
            _saveMapButton.PressedChanged += _saveMapButton_PressedChanged;
            _openInventoryButton.PressedChanged += _openInventoryButton_PressedChanged;
            _quitButtonGameplay.PressedChanged += _quitButton_PressedChanged;

            _startButton.Click += (s, e) => StartGame();
            _quitButtonMenu.Click += (s, e) => _quitButton_PressedChanged(s, e);
            _optionsButton.Click += (s, e) => Game.LogMessage("Options not implemented yet", Color.Yellow);


            _debugWindow.Closed += (s, a) =>
            {
                _debugButton.IsPressed = false;
            };

            inventoryWindow = new InventoryWindow();

            ActionBarPanel = new Panel();


        }

        public void BuildActionBar(Player player)
        {
            ActionBarPanel.Height = 60;
            ActionBarPanel.Width = 510;
            ActionBarPanel.Left = (Global.WindowWidth / 2 - (ActionBarPanel.Width.Value / 2));
            ActionBarPanel.Top = (Global.WindowHeight - ActionBarPanel.Height.Value);
            ActionBarPanel.Background = new SolidBrush(CommonComponents.PanelColor);

            int buttonWidth = 40;
            for (int i = 0; i < 10; i++)
            {
                var butt = new ItemButton();
                butt.Id = "ActionBarButton" + i;

                var panel = new Panel();
                var image = new Image();
                image.Id = "Image";
                panel.Widgets.Add(image);

                if (player.ActionBar.ContainsKey(i))
                {
                    var label = new Label();
                    label.Text = player.ActionBar[i].Quantity.ToString();
                    label.TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    label.Id = "Label";
                    panel.Widgets.Add(label);
                    image.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{player.ActionBar[i].Item.UIIcon}.png");
                    butt.Index = i;


                }

                butt.Content = panel;
                butt.Width = buttonWidth;
                butt.Padding = new Thickness(5, 5);
                butt.PressedChanged += _actionBarButtonPress;
                butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                butt.Height = 40;
                butt.Top = 10;
                butt.Left = 10 + (i * buttonWidth) + ((i * buttonWidth) / 4);

                ActionBarPanel.Widgets.Add(butt);
            }
            Widgets.Add(ActionBarPanel);

            //set the first action bar button as selected
            ActionBarPanel.Widgets.First(x => x.Id == "ActionBarButton0").Background = new SolidBrush(CommonComponents.ButtonPressedColor);
        }

        public void UpdateState(Entity.Enums.GameState state)
        {
            if (state == Entity.Enums.GameState.Menu)
            {
                _menuContainer.Visible = true;
                _gameUIContainer.Visible = false;
                ActionBarPanel.Visible = false;
            }
            else if (state == Entity.Enums.GameState.Running)
            {
                _menuContainer.Visible = false;
                _gameUIContainer.Visible = true;
                ActionBarPanel.Visible = true;
            }
        }

        private void StartGame()
        {
            LoadMap();
        }
        public void ShowWindows()
        {
            //_debugButton.IsPressed = true;
            //_button2.IsPressed = true;
            //_button3.IsPressed = true;
            // _actionBarWindow.Show(Desktop, new Point(300, Global.WindowHeight-100));
        }

        public void ChangeActionBarSelectedItem(int index)
        {
            selectedIndex += index;
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }
            else if (selectedIndex > 9)
            {
                selectedIndex = 9;
            }
            var button = ActionBarPanel.Widgets.FirstOrDefault(x => x.Id == "ActionBarButton" + selectedIndex) as ItemButton;
            if (button != null)
            {
                HandleActionBarPress(button);
            }
        }

        public void LoadMap()
        {
            var game = Game.GetInstance();
            game._mainPanel._loadMapProgressBar.Visible = true;
            //run this heavy process on a task as not to block the UI
            var task = Task.Run(() => game.LoadMap());
            Game._state = GameState.Running;
            UpdateState(GameState.Running);
            //when the task of loading the map is over, hide the progress bar
            task.ContinueWith(t => { game._mainPanel._loadMapProgressBar.Visible = false; });
        }

        public void SaveMap()
        {
            var game = Game.GetInstance();
            game._mainPanel._loadMapProgressBar.Visible = true;
            //run this heavy process on a task as not to block the UI
            var task = Task.Run(() => game.SaveMap());
            Game._state = GameState.Running;
            //when the task of loading the map is over, hide the progress bar
            task.ContinueWith(t => { game._mainPanel._loadMapProgressBar.Visible = false; });
        }
        public void HandleActionBarPress(ItemButton pressedButton)
        {
            pressedButton.Background = new SolidBrush(CommonComponents.ButtonPressedColor);

            SelectedItem = pressedButton.Index;

            foreach (var butt in ActionBarPanel.Widgets.Where(x => x.Id != pressedButton.Id))
            {
                butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
            }
        }

        public void InitializeLoadProgress(string action)
        {
            _loadMapProgressBar.Value = 0;
            _loadMapProgressBar.Visible = true;
            _progreessLabel.Text = action;
            _progreessLabel.Visible = true;
        }
        public void UpdateLoadProgress(int value)
        {
            _loadMapProgressBar.Value = value;
        }
        public void HideLoadProgress()
        {
            _loadMapProgressBar.Visible = false;
            _progreessLabel.Visible = false;
        }

        public void ToggleCommand()
        {
            if (!CommandBox.Visible)
            {
                CommandBox.Visible = true;
                CommandBox.Text = string.Empty;
            }

            CommandBox.SetKeyboardFocus();

        }
        private void ProccessCommand(string command)
        {
            CommandBox.Text = "";
            CommandBox.Visible = false;
            Game.GetInstance().ProccessCommand(command);
        }
        #region Debug
        public void UpdateFPS(int value)
        {
            _debugWindow.SPFramesPerSecond.Value = value;
        }
        public void UpdatePlayerPos(int x, int y)
        {
            _debugWindow.SPPlayerPositionX.Value = x;
            _debugWindow.SPPlayerPositionY.Value = y;
        }

        public void UpdateDebugInfo(
            string cameraPos, string mapSize, string playerGrid, string cursorGrid,
            string isMoving, string velocity, string insideBlock, string layer,
            string steppingOn, string onChunk, string solidGround, string mouseChunk,
            string mousePos, string mouseBlock,
            TileMaster.Entity.Tiles.Tile block)
        {
            _debugWindow.UpdateDebugInfo(cameraPos, mapSize, playerGrid, cursorGrid, isMoving, velocity, insideBlock, layer, steppingOn, onChunk, solidGround, mouseChunk, mousePos, mouseBlock, block);
        }
        #endregion

        #region Handlers


        private void _actionBarButtonPress(object sender, EventArgs e)
        {
            HandleActionBarPress(sender as ItemButton);
        }

        private void _button1_PressedChanged(object sender, EventArgs e)
        {
            if (_debugButton.IsPressed)
            {
                _debugWindow.Show(Desktop, new Point(Global.WindowWidth - 500, 100));
            }
            else
            {
                _debugWindow.Close();
            }
        }

        private void _loadMapButton_PressedChanged(object sender, EventArgs e)
        {
            if (_loadMapButton.IsPressed)
            {
                LoadMap();
            }
        }

        private void _saveMapButton_PressedChanged(object sender, EventArgs e)
        {
            if (_saveMapButton.IsPressed)
            {
                SaveMap();
            }
        }

        /// <summary>
        /// Handles the opening and closing of the inventory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _openInventoryButton_PressedChanged(object sender, EventArgs e)
        {
            if (_openInventoryButton.IsPressed)
            {
                inventoryWindow.Show(Desktop, new Point(Global.WindowWidth / 2 - 100, Global.WindowHeight / 2));
            }
            else
            {
                inventoryWindow.Close();
            }
        }

        private void _quitButton_PressedChanged(object sender, EventArgs e)
        {
            //since the games takes a bit to unload everything and quit,
            //perform a minimize action to make it looks like the game quit faster
            var game = Game.GetInstance();
            var form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(game.Window.Handle);
            form.WindowState = System.Windows.Forms.FormWindowState.Minimized;

            //quit game dialog?

            //quit
            Environment.Exit(0);
        }
        #endregion
    }
}