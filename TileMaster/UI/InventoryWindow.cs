using AssetManagementBase;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using System.Linq;
using TileMaster.Entity;

namespace TileMaster.UI
{
    public partial class InventoryWindow
    {
        public Panel ItemInfoPanel;

        public InventoryWindow()
        {
           
        }
        void HandleHoverOverAItem(Button button)
        {
            //var game = Game.GetInstance();
            //game._mainPanel.UpdateItemInfoPanelLocation();
            UpdateItemInfoPanelLocation();
        }
        void HandleExitHoverOverAItem()
        {
            //var game = Game.GetInstance();
            //game._mainPanel.HideItemInfoPanelLocation();
            HideItemInfoPanelLocation();
        }


        public void UpdateItemInfoPanelLocation()
        {
            ItemInfoPanel.Top = Global.CursorY - Top;
            ItemInfoPanel.Left = Global.CursorX - Left;
            ItemInfoPanel.Visible = true;
            var label = ItemInfoPanel.Widgets.FirstOrDefault(x => x.Id == "cursorX") as Label;
            label.Text = "Cursor X: " + Global.CursorX + " x " + Global.CursorY;

        }
        public void HideItemInfoPanelLocation()
        {
            ItemInfoPanel.Visible = false;
        }


        #region Handlers
        private void inventoryItem_HoverIn(object sender, System.EventArgs e)
        {
            HandleHoverOverAItem(sender as Button);
        }
        private void inventoryItem_HoverOut(object sender, System.EventArgs e)
        {
            HandleExitHoverOverAItem();
        }

        public override void Close()
        {
            MainPanel._openInventoryButton.IsPressed = false;
            base.Close();
        }

        public void BuildInventory(Player player)
        {
            InventoryPanel = new Panel();
            int calculatedHeight = player.InventoryTier / 2 * 100 + 10;
            int minHeight = 60;
            if (calculatedHeight < minHeight) calculatedHeight = minHeight;
            InventoryPanel.Height = calculatedHeight;
            InventoryPanel.Width = 510;
            InventoryPanel.Background = new SolidBrush(CommonComponents.PanelColor);


            int buttonWidthHeight = 40;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < player.InventoryTier; j++)
                {
                    var _imageButton = new Myra.Graphics2D.UI.Button();
                    var style = new ImageButtonStyle();
                    var image1 = new Image();
                    if (j % 2 == 0)
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

        #endregion
    }
}