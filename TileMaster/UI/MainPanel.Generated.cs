using Myra.Graphics2D;
using Myra;
using Myra.Graphics2D.UI;
using TileMaster.Entity;
using Myra.Graphics2D.Brushes;
using AssetManagementBase;

namespace TileMaster.UI
{
    partial class MainPanel : Panel
    {
        private void BuildUI()
        {
            BuildActionBar();

            _debugButton = new TextButton();
            _debugButton.Text = "Debug";
            _debugButton.Toggleable = true;
            _debugButton.Id = "_button1";       

            _loadMapButton = new TextButton();
            _loadMapButton.Text = "Load Map";
            _loadMapButton.Toggleable = true;
            _loadMapButton.Id = "_button4";

            _saveMapButton = new TextButton();
            _saveMapButton.Text = "Save Map";
            _saveMapButton.Toggleable = true;
            _saveMapButton.Id = "_button5";

            _openInventoryButton = new ToggleButton();
            var _openInventoryButtonLabel = new Label();
            _openInventoryButtonLabel.Text = "Open Inventory";
            _openInventoryButton.Content = _openInventoryButtonLabel;
            _openInventoryButton.Id = "_openInventoryButton";

            _quitButton = new TextButton();
            _quitButton.Text = "Quit";
            _quitButton.Toggleable = true;
            _quitButton.Id = "_button1";

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
            horizontalStackPanel1.Widgets.Add(_quitButton);

            _labelOverGui = new Label();
            _labelOverGui.Text = "Is mouse over GUI: true";
            _labelOverGui.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Bottom;
            _labelOverGui.Id = "_labelOverGui";

            Widgets.Add(horizontalStackPanel1);
            Widgets.Add(_loadMapProgressBar);
            Widgets.Add(_progreessLabel);
            Widgets.Add(_labelOverGui);
            Widgets.Add(CommandBox);
        }

        void BuildActionBar()
        {
            ActionBarPanel = new Panel();
            ActionBarPanel.Height = 60;
            ActionBarPanel.Width = 510;
            ActionBarPanel.Left = (Global.WindowWidth / 2 - (ActionBarPanel.Width.Value / 2));
            ActionBarPanel.Top = (Global.WindowHeight - ActionBarPanel.Height.Value);
            ActionBarPanel.Background = new SolidBrush(CommonComponents.PanelColor);

            int buttonWidth = 40;
            for (int i = 0; i < 10; i++)
            {
                var butt = new ImageTextButton();             
                butt.Id = "ActionBarButton" + i;
                butt.Text = "99";
                butt.TextPosition = ImageTextButton.TextPositionEnum.OverlapsImage;
                butt.Width = buttonWidth;
                butt.Padding = new Thickness(5, 5);
                butt.PressedChanged += _actionBarButtonPress;
                butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                if (i % 2 == 0)
                {
                    butt.Image = MyraEnvironment.DefaultAssetManager.LoadTextureRegion("content/UI/UIStone.png");
                    butt.MinHeight = 2;
                }

                else
                {
                    butt.Image = MyraEnvironment.DefaultAssetManager.LoadTextureRegion("content/UI/UIDirt.png");
                    butt.MinHeight = 1;
                }


                butt.Height = 40;
                butt.Top = 10;
                butt.Left = 10 + (i * buttonWidth) + ((i * buttonWidth) / 4);

                ActionBarPanel.Widgets.Add(butt);
            }
            Widgets.Add(ActionBarPanel);
        }

        public TextButton _debugButton;  
        public TextButton _loadMapButton;
        public TextButton _saveMapButton;
        public static ToggleButton _openInventoryButton;
        public TextButton _quitButton;
        public Label _progreessLabel;
        public Label _labelOverGui;
        public HorizontalProgressBar _loadMapProgressBar;
        public TextBox CommandBox;

    }
}
