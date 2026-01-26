using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;
using TileMaster.Entity.Tiles;
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
                ConsumeItem(player, ingredient.ItemId, ingredient.Quantity);
            }

            // Add output to player ActionBar (for now) or Inventory
            AddItemToPlayer(player, recipe.OutputItemId, recipe.OutputQuantity);

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

        private void ConsumeItem(Player player, int itemId, int quantity)
        {
            int remaining = quantity;
            // ActionBar first
            foreach (var item in player.ActionBar.Values)
            {
                if (item != null && item.Item != null && item.Item.Id == itemId)
                {
                    int toConsume = System.Math.Min(remaining, item.Quantity);
                    item.Quantity -= toConsume;
                    remaining -= toConsume;
                    if (remaining <= 0) return;
                }
            }
            // Inventory
            foreach (var item in player.Inventory.Values)
            {
                if (item != null && item.Item != null && item.Item.Id == itemId)
                {
                    int toConsume = System.Math.Min(remaining, item.Quantity);
                    item.Quantity -= toConsume;
                    remaining -= toConsume;
                    if (remaining <= 0) return;
                }
            }
        }

        private void AddItemToPlayer(Player player, int itemId, int quantity)
        {
            var itemRef = Global.ReferenceItems.FirstOrDefault(i => i.Id == itemId);
            if (itemRef == null) return;

            // Try to add to existing stack in ActionBar
            foreach (var slot in player.ActionBar.Values)
            {
                if (slot != null && slot.Item != null && slot.Item.Id == itemId && slot.Quantity < slot.Item.StackSize)
                {
                    int canAdd = slot.Item.StackSize - slot.Quantity;
                    int toAdd = System.Math.Min(canAdd, quantity);
                    slot.Quantity += toAdd;
                    quantity -= toAdd;
                    if (quantity <= 0) return;
                }
            }

            // Try to find empty slot in ActionBar
            for (int i = 0; i < 10; i++)
            {
                if (!player.ActionBar.ContainsKey(i) || player.ActionBar[i] == null || player.ActionBar[i].Item == null)
                {
                    player.ActionBar[i] = new InventoryItem { Item = itemRef, Quantity = quantity };
                    return;
                }
            }

            // Fallback to Log for now if inventory is full
            Game.LogMessage($"Crafted {itemRef.Name}, but no space in ActionBar!", Microsoft.Xna.Framework.Color.Yellow);
        }
    }
}
