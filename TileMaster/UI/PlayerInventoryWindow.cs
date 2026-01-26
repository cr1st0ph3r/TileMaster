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
                    int index = i + (j * 10);
                    var butt = new ItemButton();
                    butt.Index = index;
                    butt.SourceInventory = player.Inventory;
                    butt.Click += inventoryItem_Click;

                    var panel = new Panel();
                    var image = new Image();
                    image.Id = "Image";
                    panel.Widgets.Add(image);

                    var label = new Label();
                    label.Id = "Label";
                    label.TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.HorizontalAlignment = HorizontalAlignment.Center;
                    panel.Widgets.Add(label);

                    if (player.Inventory.ContainsKey(index) && player.Inventory[index] != null)
                    {
                        var invItem = player.Inventory[index];
                        label.Text = invItem.Quantity.ToString();
                        image.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{invItem.Item.UIIcon}.png");
                    }
                    else
                    {
                        image.Visible = false;
                        label.Visible = false;
                    }

                    butt.Width = buttonWidthHeight;
                    butt.Height = buttonWidthHeight;
                    butt.Top = 10 + (j * buttonWidthHeight) + ((j * buttonWidthHeight) / 4);
                    butt.Left = 10 + (i * buttonWidthHeight) + ((i * buttonWidthHeight) / 4);
                    butt.Content = panel;
                    butt.Padding = new Thickness(5, 5);
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                    butt.MouseEntered += inventoryItem_HoverIn;
                    butt.MouseLeft += inventoryItem_HoverOut;

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

        private void inventoryItem_Click(object sender, System.EventArgs e)
        {
            var butt = sender as ItemButton;
            if (butt == null) return;

            var inventory = butt.SourceInventory;
            int index = butt.Index;

            var heldItem = Global.HeldItem;
            var slotItem = inventory.ContainsKey(index) ? inventory[index] : null;

            if (heldItem == null && slotItem != null)
            {
                // Pick up item
                Global.HeldItem = slotItem;
                inventory[index] = null;
            }
            else if (heldItem != null)
            {
                if (slotItem == null)
                {
                    // Drop item into empty slot
                    inventory[index] = heldItem;
                    Global.HeldItem = null;
                }
                else
                {
                    // Item Swap or Stacking
                    if (heldItem.ItemId == slotItem.ItemId)
                    {
                        slotItem.Quantity += heldItem.Quantity;
                        Global.HeldItem = null;
                    }
                    else
                    {
                        // Swap
                        inventory[index] = heldItem;
                        Global.HeldItem = slotItem;
                    }
                }
            }

            butt.RefreshSlot();
        }
        #endregion
    }
}