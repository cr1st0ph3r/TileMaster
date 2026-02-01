using Microsoft.Xna.Framework;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using System;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Manager;
using TileMaster.Model;

namespace TileMaster.UI
{
    public class CraftingWindow : Window
    {
        private VerticalStackPanel _recipeList;
        private CraftingManager _craftingManager;
        private Player _player;
        private string _stationType;

        public CraftingWindow()
        {
            Title = "Crafting";
            Width = 400;
            Height = 500;
            this.Closed += (sender, args) =>
            {
                Console.WriteLine("Window has been closed!");
               this.Visible = false;
            };
        }

        public void Build(Player player, CraftingManager craftingManager, string stationType)
        {
            _player = player;
            _craftingManager = craftingManager;
            _stationType = stationType;

            var mainPanel = new VerticalStackPanel
            {
                Spacing = 10,
                Padding = new Myra.Graphics2D.Thickness(10)
            };

            _recipeList = new VerticalStackPanel
            {
                Spacing = 5
            };

            var scrollViewer = new ScrollViewer
            {
                Content = _recipeList,
                Height = 400
            };

            mainPanel.Widgets.Add(scrollViewer);
            Content = mainPanel;
            
            RefreshRecipes();
        }

        public void RefreshRecipes()
        {
            _recipeList.Widgets.Clear();

            var recipes = _craftingManager.Recipes.Where(r => 
                string.IsNullOrEmpty(r.RequiredStation) || r.RequiredStation == _stationType
            ).ToList();

            foreach (var recipe in recipes)
            {
                _recipeList.Widgets.Add(CreateRecipeWidget(recipe));
            }
        }

        private Widget CreateRecipeWidget(Recipe recipe)
        {
            var panel = new HorizontalStackPanel
            {
                Spacing = 10,
                Padding = new Myra.Graphics2D.Thickness(5),
                Background = new SolidBrush(Color.DimGray)
            };

            var outputItem = Global.ReferenceItems.FirstOrDefault(i => i.Id == recipe.OutputItemId);
            
            var label = new Label
            {
                Text = $"{recipe.Name} ({recipe.OutputQuantity})",
                VerticalAlignment = VerticalAlignment.Center,
                Width = 200
            };

            var craftButton = new Button
            {
                Content = new Label { Text = "Craft" },
                Enabled = _craftingManager.CanCraft(_player, recipe),
                Width = 80
            };

            craftButton.Click += (s, a) =>
            {
                if (_craftingManager.Craft(_player, recipe))
                {
                    Game.LogMessage($"Crafted {recipe.Name}!", Color.Green, 100);
                    RefreshRecipes(); // Refresh to update button states (Enabled/Disabled)
                }
            };

            panel.Widgets.Add(label);
            panel.Widgets.Add(craftButton);

            return panel;
        }
    }
}
