using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
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

            BuildActionBar();

  
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
            var button = ActionBarPanel.Widgets.FirstOrDefault(x => x.Id == "ActionBarButton" + selectedIndex) as Button;
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
        public void HandleActionBarPress(Button pressedButton)
        {
            pressedButton.Background = new SolidBrush(CommonComponents.ButtonPressedColor);
            SelectedItem = pressedButton.MinHeight.Value;
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
        #endregion

        #region Handlers


        private void _actionBarButtonPress(object sender, EventArgs e)
        {
            HandleActionBarPress(sender as Button);
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