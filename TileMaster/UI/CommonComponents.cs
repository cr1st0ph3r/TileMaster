using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System.Collections.Generic;

namespace TileMaster.UI
{
    public class CommonComponents
    {
        public CommonComponents()
        {
            BuildWidgets();
        }

        public Dictionary<string, Widget> Widgets = new Dictionary<string, Widget>();
        public static readonly Microsoft.Xna.Framework.Color BorderColor = Microsoft.Xna.Framework.Color.Aquamarine;
        public static readonly string PanelColor = "#545454";
        public static readonly string ActionBarButtonColor = "#808080";
        public static readonly string ButtonPressedColor = "#cf5c15";

        void BuildWidgets()
        {
            BuildItemInfoPanel();
        }     
        
        void BuildItemInfoPanel()
        {
            var ItemInfoPanel = new Panel();
            ItemInfoPanel.Height = 200;
            ItemInfoPanel.Width = 150;
            ItemInfoPanel.Visible = false;

            ItemInfoPanel.Left = (Global.WindowWidth / 2);
            ItemInfoPanel.Top = (Global.WindowHeight / 2);
            ItemInfoPanel.Background = new SolidBrush(PanelColor);
            ItemInfoPanel.Border = new SolidBrush(BorderColor);
            ItemInfoPanel.BorderThickness = new Thickness(1);

            var ItemInfoTitle = new Label();
            ItemInfoTitle.Text = "The Item";
            ItemInfoTitle.Top = 10;
            ItemInfoPanel.Widgets.Add(ItemInfoTitle);

            var cursorX = new Label();
            cursorX.Id = "cursorX";
            cursorX.Text = "Cursor X:";
            cursorX.Top = ItemInfoTitle.Top + 30;
            ItemInfoPanel.Widgets.Add(cursorX);

            Widgets.Add("ItemInfoPanel", ItemInfoPanel);
        }

    }
}
