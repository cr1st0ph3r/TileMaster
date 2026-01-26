using AssetManagementBase;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity.Tiles;

namespace TileMaster.UI
{
    public partial class ContainerInventoryWindow : Window
    {
        public Panel ItemInfoPanel;
        public Panel InventoryPanel;

        public ContainerInventoryWindow()
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

        public void BuildInventory(Dictionary<int, InventoryItem> items, string title)
        {
            InventoryPanel = new Panel();
            // Default chest size is 40 (4 rows of 10)
            int rows = 4;
            int calculatedHeight = rows * 50 + 40;
            
            InventoryPanel.Height = calculatedHeight;
            InventoryPanel.Width = 510;
            InventoryPanel.Background = new SolidBrush(CommonComponents.PanelColor);

            int buttonWidthHeight = 40;
            for (int j = 0; j < rows; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    int index = j * 10 + i;
                    var butt = new ItemButton();
                    butt.Id = "ContainerButton" + index;
                    var panel = new Panel();
                    var image = new Image();
                    image.Id = "Image";
                    panel.Widgets.Add(image);

                    if (items.ContainsKey(index) && items[index] != null)
                    {
                        var label = new Label();
                        label.Text = items[index].Quantity.ToString();
                        label.TextAlign = FontStashSharp.RichText.TextHorizontalAlignment.Center;
                        label.VerticalAlignment = VerticalAlignment.Center;
                        label.HorizontalAlignment = HorizontalAlignment.Center;
                        label.Id = "Label";
                        panel.Widgets.Add(label);
                        image.Renderable = MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{items[index].Item.UIIcon}.png");
                    }
                    else
                    {
                        var label = new Label();
                        label.Id = "Label";
                        label.Visible = false;
                        panel.Widgets.Add(label);
                        image.Visible = false;
                    }

                    butt.Index = index;
                    butt.SourceInventory = items;
                    butt.Click += containerItem_Click;

                    butt.Width = buttonWidthHeight;
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);
                    butt.MouseEntered += inventoryItem_HoverIn;
                    butt.MouseLeft += inventoryItem_HoverOut;

                    butt.Height = buttonWidthHeight;
                    butt.Top = 10 + (j * 50);
                    butt.Left = 10 + (i * 50);
                    butt.Content = panel;
                    butt.Padding = new Thickness(5, 5);
                    butt.Background = new SolidBrush(CommonComponents.ActionBarButtonColor);

                    InventoryPanel.Widgets.Add(butt);
                }
            }

            var labelTitle = new Label();
            labelTitle.Text = title;
            labelTitle.Top = -30;
            labelTitle.HorizontalAlignment = HorizontalAlignment.Center;
            InventoryPanel.Widgets.Add(labelTitle);

            Content = InventoryPanel;
            ItemInfoPanel = MainPanel.CommonComponents.Widgets["ItemInfoPanel"] as Panel;
            InventoryPanel.Widgets.Add(ItemInfoPanel);

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

        private void containerItem_Click(object sender, System.EventArgs e)
        {
            var butt = sender as ItemButton;
            if (butt == null) return;

            var inventory = butt.SourceInventory;
            int index = butt.Index;

            var heldItem = Global.HeldItem;
            var slotItem = inventory.ContainsKey(index) ? inventory[index] : null;

            if (heldItem == null && slotItem != null)
            {
                Global.HeldItem = slotItem;
                inventory[index] = null;
            }
            else if (heldItem != null)
            {
                if (slotItem == null)
                {
                    inventory[index] = heldItem;
                    Global.HeldItem = null;
                }
                else
                {
                    if (heldItem.ItemId == slotItem.ItemId)
                    {
                        slotItem.Quantity += heldItem.Quantity;
                        Global.HeldItem = null;
                    }
                    else
                    {
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