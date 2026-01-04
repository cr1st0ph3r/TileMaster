using Myra.Graphics2D;
using Myra;
using Myra.Graphics2D.UI;
using TileMaster.Entity;
using Myra.Graphics2D.Brushes;
using AssetManagementBase;
using Myra.Graphics2D.UI.Styles;

namespace TileMaster.UI
{
    partial class InventoryWindow : Window
    {
        public Panel InventoryPanel;
        private void BuildUI()
        {
            BuildInventory();
        }

        void BuildInventory(int inventoryTier = 2)
        {
            InventoryPanel = new Panel();
            int calculatedHeight = inventoryTier / 2 * 100 + 10;
            int minHeight = 60;
            if(calculatedHeight < minHeight) calculatedHeight = minHeight;
            InventoryPanel.Height = calculatedHeight;
            InventoryPanel.Width = 510;
            InventoryPanel.Background = new SolidBrush(CommonComponents.PanelColor);



            var TileTypes = CollisionTiles.LoadTilesTypes();

            int buttonWidthHeight = 40;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < inventoryTier; j++)
                {
                    var _imageButton = new Myra.Graphics2D.UI.Button();
                    var style = new ImageButtonStyle();
                    var image1 = new Image();
                    if(j%2==0)
                        image1.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion("content/UI/UIStone.png");
                    else
                        image1.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion("content/UI/UIDirt.png");

                    var inventoryItemAmount = new Label();
                    inventoryItemAmount.Text = "99";
                    inventoryItemAmount.TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Right;

                    var horizontalStackPanel3 = new HorizontalStackPanel();
                    horizontalStackPanel3.Widgets.Add(image1);
                    horizontalStackPanel3.Widgets.Add(inventoryItemAmount);

                    var butt = new Button();
                    butt.Id = "ActionBarButton" + i;
                    butt.Width = buttonWidthHeight;
                    butt.Padding = new Thickness(5, 5);
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                    butt.MouseEntered += inventoryItem_HoverIn;
                    butt.MouseLeft += inventoryItem_HoverOut;
                    butt.Content = horizontalStackPanel3;

                    butt.Height = buttonWidthHeight;
                    butt.Top = 10 + (j * buttonWidthHeight) + ((j * buttonWidthHeight) / 4);
                    butt.Left = 10 + (i * buttonWidthHeight) + ((i * buttonWidthHeight) / 4);

                    InventoryPanel.Widgets.Add(butt);
                }
            }
            var label1 = new Label();
            label1.Text = "Inventory";
            label1.Top = -30;
            InventoryPanel.Widgets.Add(label1);

            //The item info panel
            ItemInfoPanel = MainPanel.CommonComponents.Widgets["ItemInfoPanel"] as Panel;
            InventoryPanel.Widgets.Add(ItemInfoPanel);

            Content = InventoryPanel;
        }
    }
}
