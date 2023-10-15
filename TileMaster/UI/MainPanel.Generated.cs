using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D;
using Myra;
using Myra.Graphics2D.UI;
using TileMaster.Entity;
using Myra.Graphics2D.Brushes;
using SharpDX.Direct3D9;

namespace TileMaster.UI
{
	partial class MainPanel: Panel
	{
		private void BuildUI()
		{
			 BuildActionBar();

			_debugButton = new TextButton();
			_debugButton.Text = "Debug";
			_debugButton.Toggleable = true;
			_debugButton.Id = "_button1";
			  
			_button2 = new TextButton();
			_button2.Text = "Window 2";
			_button2.Toggleable = true;
			_button2.Id = "_button2";

			_button3 = new TextButton();
			_button3.Text = "Window 3";
			_button3.Toggleable = true;
			_button3.Id = "_button3";

            _loadMapButton = new TextButton();
            _loadMapButton.Text = "Load Map";
            _loadMapButton.Toggleable = true;
            _loadMapButton.Id = "_button1";

            _openInventoryButton = new TextButton();
            _openInventoryButton.Text = "Open Inventory";
            _openInventoryButton.Toggleable = true;
            _openInventoryButton.Id = "_button1";

            _quitButton = new TextButton();
            _quitButton.Text = "Quit";
            _quitButton.Toggleable = true;
            _quitButton.Id = "_button1";

            _horizontalProgressBar = new HorizontalProgressBar();
            _horizontalProgressBar.GridRow = 2;
            _horizontalProgressBar.Visible = false;
            _horizontalProgressBar.Id = "_horizontalProgressBar";
            _horizontalProgressBar.VerticalAlignment=Myra.Graphics2D.UI.VerticalAlignment.Center;

            var horizontalStackPanel1 = new HorizontalStackPanel();
			horizontalStackPanel1.Spacing = 8;
			horizontalStackPanel1.Widgets.Add(_debugButton);
			horizontalStackPanel1.Widgets.Add(_button2);
			horizontalStackPanel1.Widgets.Add(_button3);
			horizontalStackPanel1.Widgets.Add(_loadMapButton); 
			horizontalStackPanel1.Widgets.Add(_openInventoryButton); 
			horizontalStackPanel1.Widgets.Add(_quitButton); 

			_labelOverGui = new Label();
			_labelOverGui.Text = "Is mouse over GUI: true";
			_labelOverGui.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Bottom;
			_labelOverGui.Id = "_labelOverGui";

			Widgets.Add(horizontalStackPanel1);
			Widgets.Add(_horizontalProgressBar);
			Widgets.Add(_labelOverGui);
	
		}

		void BuildActionBar()
		{
            ActionBarPanel = new Panel();
            ActionBarPanel.Height = 60;
            ActionBarPanel.Width = 510;
            ActionBarPanel.Left = (Global.WindowWidth / 2 - (ActionBarPanel.Width.Value / 2));
            ActionBarPanel.Top = (Global.WindowHeight - ActionBarPanel.Height.Value);
            ActionBarPanel.Background = new SolidBrush(Global.PanelColor);


            var Tiletypes = CollisionTiles.LoadTilesTypes();

            int buttonWidth = 40;
            for (int i = 0; i < 10; i++)
            {
                var butt = new ImageTextButton();
                butt.Id = "ActionBarButton"+i;
				butt.Text = "99";
				butt.TextPosition = ImageTextButton.TextPositionEnum.OverlapsImage;
				butt.Width = buttonWidth;
                butt.Padding = new Thickness(5,5);
                butt.PressedChanged += _actionBarButtonPress;
                //butt.Image = DefaultAssets.UITextureRegionAtlas["icon-star-outline"];
                butt.Background = new SolidBrush(Global.ActionBarButtonColor);
                butt.Image = MyraEnvironment.DefaultAssetManager.Load<TextureRegion>("content/dirt.png");
 

                butt.Height = 40;
                butt.Top = 10;
                butt.Left = 10 + (i * buttonWidth) + ((i * buttonWidth) / 4);

                ActionBarPanel.Widgets.Add(butt);
            }
            Widgets.Add(ActionBarPanel);
        }

		public TextButton _debugButton;
		public TextButton _button2;
		public TextButton _button3;
		public TextButton _loadMapButton;
		public TextButton _openInventoryButton;
		public TextButton _quitButton;
		public Label _labelOverGui;
        public HorizontalProgressBar _horizontalProgressBar;

    }
}
