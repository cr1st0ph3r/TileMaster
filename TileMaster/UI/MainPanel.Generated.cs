using AssetManagementBase;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System.Linq;
using TileMaster.Entity;

namespace TileMaster.UI
{
    partial class MainPanel : Panel
    {
        private void BuildUI()
        {
            _gameUIContainer = new Panel();
            _gameUIContainer.Visible = false;

            _debugButton = new ToggleButton();
            _debugButton.Content = new Label { Text = "Debug" };
            _debugButton.Id = "_button1";

            _loadMapButton = new ToggleButton();
            _loadMapButton.Content = new Label { Text = "Load Map" };
            _loadMapButton.Id = "_button4";

            _saveMapButton = new ToggleButton();
            _saveMapButton.Content = new Label { Text = "Save Map" };
            _saveMapButton.Id = "_button5";

            _openInventoryButton = new ToggleButton();
            var _openInventoryButtonLabel = new Label();
            _openInventoryButtonLabel.Text = "Open Inventory";
            _openInventoryButton.Content = _openInventoryButtonLabel;
            _openInventoryButton.Id = "_openInventoryButton";

            _quitButtonGameplay = new ToggleButton();
            _quitButtonGameplay.Content = new Label { Text = "Quit" };
            _quitButtonGameplay.Id = "_buttonQuitGameplay";

            CommandBox = new TextBox();
            CommandBox.Visible = false;
            CommandBox.Top = 50;
            CommandBox.Width = 800;
            CommandBox.KeyDown += (s, e) =>
            {
                if (e.Data == Microsoft.Xna.Framework.Input.Keys.Enter)
                {
                    string input = CommandBox.Text;

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        ProccessCommand(input);
                    }
                }
            };

            //progres bar
            _loadMapProgressBar = new HorizontalProgressBar();
            Grid.SetColumn(_loadMapProgressBar, 2);
            _loadMapProgressBar.Visible = false;
            _loadMapProgressBar.Id = "_horizontalProgressBar";
            _loadMapProgressBar.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Center;

            _progreessLabel = new Label();
            _progreessLabel.Text = "placehoder";
            _progreessLabel.TextColor = Microsoft.Xna.Framework.Color.White;
            _progreessLabel.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Center;
            _progreessLabel.HorizontalAlignment = Myra.Graphics2D.UI.HorizontalAlignment.Center;
            _progreessLabel.Id = "_progreessLabel";
            _progreessLabel.Visible = false;
            _progreessLabel.ZIndex = 999;

            var horizontalStackPanel1 = new HorizontalStackPanel();
            horizontalStackPanel1.Spacing = 8;
            horizontalStackPanel1.Widgets.Add(_debugButton);
            horizontalStackPanel1.Widgets.Add(_loadMapButton);
            horizontalStackPanel1.Widgets.Add(_saveMapButton);
            horizontalStackPanel1.Widgets.Add(_openInventoryButton);
            horizontalStackPanel1.Widgets.Add(_quitButtonGameplay);

            _gameUIContainer.Widgets.Add(horizontalStackPanel1);
            _gameUIContainer.Widgets.Add(_loadMapProgressBar);
            _gameUIContainer.Widgets.Add(_progreessLabel);            
            _gameUIContainer.Widgets.Add(CommandBox);

            // Menu UI
            _menuContainer = new VerticalStackPanel();
            _menuContainer.Spacing = 15;
            _menuContainer.HorizontalAlignment = HorizontalAlignment.Center;
            _menuContainer.VerticalAlignment = VerticalAlignment.Center;
            _menuContainer.Width = 300;

            _startButton = new Button
            {
                Id = "StartButton",
                Content = new Label { Text = "START" },
                Padding = new Thickness(10, 20),
                Background = new SolidBrush("#3a3a3a"),
                OverBackground = new SolidBrush("#4a4a4a"),
                PressedBackground = new SolidBrush("#cf5c15")
            };

            _optionsButton = new Button
            {
                Id = "OptionsButton",
                Content = new Label { Text = "OPTIONS"},
                Padding = new Thickness(10, 20),
                Background = new SolidBrush("#3a3a3a"),
                OverBackground = new SolidBrush("#4a4a4a"),
                PressedBackground = new SolidBrush("#cf5c15")
            };

            _quitButtonMenu = new Button
            {
                Id = "QuitButton",
                Content = new Label { Text = "QUIT" },
                Padding = new Thickness(10, 20),
                Background = new SolidBrush("#3a3a3a"),
                OverBackground = new SolidBrush("#4a4a4a"),
                PressedBackground = new SolidBrush("#cf5c15")
            };

            _menuContainer.Widgets.Add(_startButton);
            _menuContainer.Widgets.Add(_optionsButton);
            _menuContainer.Widgets.Add(_quitButtonMenu);

            Widgets.Add(_gameUIContainer);
            Widgets.Add(_menuContainer);
        }

        public ToggleButton _debugButton;  
        public ToggleButton _loadMapButton;
        public ToggleButton _saveMapButton;
        public static ToggleButton _openInventoryButton;
        public ToggleButton _quitButtonGameplay;
        public Button _startButton;
        public Button _optionsButton;
        public Button _quitButtonMenu;
        public Panel _gameUIContainer;
        public VerticalStackPanel _menuContainer;
        public Label _progreessLabel;
        public HorizontalProgressBar _loadMapProgressBar;
        public TextBox CommandBox;

    }
}
