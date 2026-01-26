using AssetManagementBase;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System.Linq;
using TileMaster.Entity;

namespace TileMaster.UI
{
    public partial class PlayerInventoryWindow : Window
    {
        public Panel ItemInfoPanel;
        public Panel InventoryPanel;

        public PlayerInventoryWindow()
        {

        }
        void HandleHoverOverAItem(Button button)
        {
            UpdateItemInfoPanelLocation();
        }
        void HandleExitHoverOverAItem()
        {
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

        public void BuildInventory(Player player)
        {
            InventoryPanel = new Panel();
            int calculatedHeight = player.InventoryTier / 2 * 100 + 10;
            int calculatedTier = 1 + player.InventoryTier;
            int minHeight = 60;
            if (calculatedHeight < minHeight) calculatedHeight = minHeight;
            InventoryPanel.Height = calculatedHeight;
            InventoryPanel.Width = 510;
            InventoryPanel.Background = new SolidBrush(CommonComponents.PanelColor);


            int buttonWidthHeight = 40;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < calculatedTier; j++)
                {
                    var butt = new ItemButton();
                    butt.Id = "InventoryButton" + i;
                    var panel = new Panel();
                    var image = new Image();
                    image.Id = "Image";
                    panel.Widgets.Add(image);

                    if (player.Inventory.ContainsKey(i + j))
                    {
                        var label = new Label();
                        label.Text = player.Inventory[i + j].Quantity.ToString();
                        label.TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                        label.VerticalAlignment = VerticalAlignment.Center;
                        label.HorizontalAlignment = HorizontalAlignment.Center;
                        label.Id = "Label";
                        panel.Widgets.Add(label);
                        image.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{player.Inventory[i + j].Item.UIIcon}.png");
                        butt.Index = i;
                    }

                    butt.Id = "ActionBarButton" + i;
                    butt.Width = buttonWidthHeight;
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                    butt.MouseEntered += inventoryItem_HoverIn;
                    butt.MouseLeft += inventoryItem_HoverOut;

                    butt.Height = buttonWidthHeight;
                    butt.Top = 10 + (j * buttonWidthHeight) + ((j * buttonWidthHeight) / 4);
                    butt.Left = 10 + (i * buttonWidthHeight) + ((i * buttonWidthHeight) / 4);
                    butt.Content = panel;
                    butt.Padding = new Thickness(5, 5);
                    //butt.PressedChanged += _actionBarButtonPress;
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);


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
        #endregion
    }
}