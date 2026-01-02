using Myra.Graphics2D.UI;
using System.Linq;

namespace TileMaster.UI
{
    public partial class InventoryWindow
    {
        public Panel ItemInfoPanel;

        public InventoryWindow()
        {
            BuildUI();
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
        #endregion
    }
}