using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Model;

namespace TileMaster.Manager
{
    public class CraftingManager
    {
        public List<Recipe> Recipes { get; private set; }

        public CraftingManager()
        {
            Recipes = new List<Recipe>();
            InitializeDefaultRecipes();
        }

        private void InitializeDefaultRecipes()
        {
            // Test recipes
            var stoneRecipe = new Recipe("Stone from Dirt", 2, 1); // 2 is Stone
            stoneRecipe.Ingredients.Add(new Ingredient(1, 10)); // 1 is Dirt
            Recipes.Add(stoneRecipe);

            var anvilRecipe = new Recipe("Anvil", 7, 1); // 7 is Anvil
            anvilRecipe.Ingredients.Add(new Ingredient(2, 20)); // 2 is Stone
            Recipes.Add(anvilRecipe);

            // Recipe that requires Anvil
            var advancedRecipe = new Recipe("Advanced Block", 2, 5, "Crafting");
            advancedRecipe.Ingredients.Add(new Ingredient(1, 5));
            Recipes.Add(advancedRecipe);
        }

        public List<Recipe> GetAvailableRecipes(Player player, string nearbyStation = null)
        {
            return Recipes.Where(r => 
                (string.IsNullOrEmpty(r.RequiredStation) || r.RequiredStation == nearbyStation) &&
                CanCraft(player, r)
            ).ToList();
        }

        public bool CanCraft(Player player, Recipe recipe)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                int totalOwned = GetTotalItemQuantity(player, ingredient.ItemId);
                if (totalOwned < ingredient.Quantity)
                    return false;
            }
            return true;
        }

        public bool Craft(Player player, Recipe recipe)
        {
            if (!CanCraft(player, recipe))
                return false;

            // Consume ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                player.ConsumeItem(ingredient.ItemId, ingredient.Quantity);
            }

            // Add output to player ActionBar (for now) or Inventory
            var itemRef = Global.ReferenceItems.FirstOrDefault(i => i.Id == recipe.OutputItemId);
            if (itemRef != null)
            {
                player.AddItem(itemRef, recipe.OutputQuantity);
            }

            return true;
        }

        private int GetTotalItemQuantity(Player player, int itemId)
        {
            int total = 0;
            foreach (var item in player.ActionBar.Values)
            {
                if (item != null && item.Item != null && item.Item.Id == itemId)
                    total += item.Quantity;
            }
            foreach (var item in player.Inventory.Values)
            {
                if (item != null && item.Item != null && item.Item.Id == itemId)
                    total += item.Quantity;
            }
            return total;
        }
    }
}
