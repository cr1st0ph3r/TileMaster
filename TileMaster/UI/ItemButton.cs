using AssetManagementBase;
using Myra.Graphics2D.UI;
using System.Linq;
using TileMaster.Entity.Tiles;

namespace TileMaster.UI
{
    public class ItemButton : Button
    {
        public int Index { get; set; }
        public System.Collections.Generic.Dictionary<int, InventoryItem> SourceInventory { get; set; }

        public void RefreshSlot()
        {
            var panel = Content as Panel;
            if (panel == null) return;

            var image = panel.Widgets.FirstOrDefault(x => x.Id == "Image") as Image;
            var label = panel.Widgets.FirstOrDefault(x => x.Id == "Label") as Label;

            if (SourceInventory != null && SourceInventory.ContainsKey(Index) && SourceInventory[Index] != null)
            {
                var invItem = SourceInventory[Index];
                if(invItem.Quantity < 1)
                {
                    label.Visible = false;
                    image.Visible = false;                                      
                }
                else
                {
                    if (image != null)
                    {
                        image.Renderable = Myra.MyraEnvironment.DefaultAssetManager.LoadTextureRegion($"{Global.UIIconsLocation}{invItem.Item.UIIcon}.png");
                        image.Visible = true;
                    }
                    if (label != null)
                    {
                        label.Text = invItem.Quantity.ToString();
                        label.Visible = true;
                    }
                }
              
            }
            else
            {
                if (image != null) image.Visible = false;
                if (label != null) label.Visible = false;
            }
        }
    }
}
