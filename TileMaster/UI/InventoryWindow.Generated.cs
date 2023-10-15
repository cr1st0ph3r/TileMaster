using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D;
using Myra;
using Myra.Graphics2D.UI;
using TileMaster.Entity;
using Myra.Graphics2D.Brushes;

namespace TileMaster.UI
{
	partial class InventoryWindow : Window
	{
        public Panel InventoryPanel;
        private void BuildUI()
		{
            BuildInventory();

        }

        void BuildInventory()
        {
         
        
            InventoryPanel = new Panel();
            InventoryPanel.Height = 500;
            InventoryPanel.Width = 500;         
            InventoryPanel.Background = new SolidBrush(Global.PanelColor);


            var TileTypes = CollisionTiles.LoadTilesTypes();

            int buttonWidthHeight = 40;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var butt = new ImageTextButton();
                    butt.Id = "ActionBarButton" + i;
                    butt.Text = "99";
                    butt.TextPosition = ImageTextButton.TextPositionEnum.OverlapsImage;
                    butt.Width = buttonWidthHeight;
                    butt.Padding = new Thickness(5, 5);
                    //butt.Image = DefaultAssets.UITextureRegionAtlas["icon-star-outline"];
                    butt.Background = new SolidBrush(Global.ActionBarButtonColor);
                    butt.Image = MyraEnvironment.DefaultAssetManager.Load<TextureRegion>("content/dirt.png");


                    butt.Height = buttonWidthHeight;
                    butt.Top = 10 + (j* buttonWidthHeight) + ((j * buttonWidthHeight) / 4);
                    butt.Left = 10 + (i * buttonWidthHeight) + ((i * buttonWidthHeight) / 4);

                    InventoryPanel.Widgets.Add(butt);
                }
               
            }
            var label1 = new Label();
            label1.Text = "Inventory";
            label1.Top = -30;
            InventoryPanel.Widgets.Add(label1);
          
            Content = InventoryPanel;
        }
    }
}
