using Myra.Graphics2D.UI;

namespace TileMaster.UI
{
	public partial class InventoryWindow
    {
        public Panel ItemInfoPanel;

        public InventoryWindow()
		{
			BuildUI();
		} 
        public InventoryWindow(Panel ItemInfoPanel)
		{
			BuildUI();
            this.ItemInfoPanel = ItemInfoPanel;
		}
        void HandleHoverOverAItem(ImageTextButton button)
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
            ItemInfoPanel.Top = Global.CursorY;
            ItemInfoPanel.Left = Global.CursorX;
            ItemInfoPanel.Visible = true;

        }
        public void HideItemInfoPanelLocation()
        {
            ItemInfoPanel.Visible = false;
        }
        

        #region Handlers
        private void inventoryItem_HoverIn(object sender, System.EventArgs e)
        {
            HandleHoverOverAItem(sender as ImageTextButton);
        }  
        private void inventoryItem_HoverOut(object sender, System.EventArgs e)
        {
            HandleExitHoverOverAItem();
        }
        #endregion
    }
}