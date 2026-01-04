using Myra.Graphics2D.UI;
using System;

namespace TileMaster.UI
{
    partial class DebugWindow : Window
    {
        private void BuildUI()
        {
            Title = "Debug Info";
            Left = 395;
            Top = 210;

            //FPS
            var label2 = new Label();
            label2.Text = "FPS";

            var spinButton1 = SPFramesPerSecond;
            spinButton1.Value = 5;
            spinButton1.Width = 60;
            Grid.SetColumn(spinButton1, 1);

            //Player X
            var label3 = new Label();
            label3.Text = "Player X";
            Grid.SetRow(label3, 1);

            var spinButton2 = SPPlayerPositionX;
            spinButton2.Value = 5;
            spinButton2.Width = 60;
            Grid.SetColumn(spinButton2, 1);
            Grid.SetRow(spinButton2, 1);

            //Player Y
            var label4 = new Label();
            label4.Text = "Player Y";
            Grid.SetRow(label4, 2);

            var spinButton3 = SPPlayerPositionY;

            spinButton3.Value = 5;
            spinButton3.Width = 60;
            Grid.SetColumn(spinButton3, 1);
            Grid.SetRow(spinButton3, 2);

            // Framerate cap
            var capFpsTo60 = new CheckButton
            {Content = new Label{Text = "Limit FPS to 60"}};
            Grid.SetColumn(capFpsTo60, 3);
              capFpsTo60.IsCheckedChanged += (s, a) =>
                {
                    var game = Game.GetInstance();
                    if (capFpsTo60.IsChecked)
                    {
                        game.IsFixedTimeStep = true;
                    }
                    else
                    {
                        game.IsFixedTimeStep = false;
                    }
                };

            //show chunk boundaries
            var showChunkBoundaries = new CheckButton
            { Content = new Label { Text = "Show Chunk Boundaries" } };
            Grid.SetColumn(showChunkBoundaries, 3);
            Grid.SetRow(showChunkBoundaries, 1);
            showChunkBoundaries.IsCheckedChanged += (s, a) =>
            {
                if (showChunkBoundaries.IsChecked)
                {
                    Global.MarkTilesOnTheEdge = true;
                }
                else
                {
                    Global.MarkTilesOnTheEdge = false;
                }
            };

            var grid1 = new Grid();
            grid1.ColumnSpacing = 8;
            grid1.RowSpacing = 8;
            grid1.DefaultColumnProportion = new Proportion
            {
                Type = Myra.Graphics2D.UI.ProportionType.Auto,
            };
            grid1.DefaultRowProportion = new Proportion
            {
                Type = Myra.Graphics2D.UI.ProportionType.Auto,
            };

            grid1.Widgets.Add(label2);
            grid1.Widgets.Add(spinButton1);
            grid1.Widgets.Add(label3);
            grid1.Widgets.Add(spinButton2);
            grid1.Widgets.Add(label4);
            grid1.Widgets.Add(spinButton3);
            grid1.Widgets.Add(capFpsTo60);
            grid1.Widgets.Add(showChunkBoundaries);

            var verticalStackPanel1 = new VerticalStackPanel();
            verticalStackPanel1.Spacing = 8;
            verticalStackPanel1.Widgets.Add(grid1);
            Content = verticalStackPanel1;
        }
        public void SetValue()
        {

        }

    }
}
