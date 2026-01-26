using System.Collections.Generic;

namespace TileMaster.Model
{
    public class Recipe
    {
        public string Name { get; set; }
        public List<Ingredient> Ingredients { get; set; }
        public int OutputItemId { get; set; }
        public int OutputQuantity { get; set; }
        public string RequiredStation { get; set; }

        public Recipe(string name, int outputItemId, int outputQuantity, string requiredStation = null)
        {
            Name = name;
            OutputItemId = outputItemId;
            OutputQuantity = outputQuantity;
            RequiredStation = requiredStation;
            Ingredients = new List<Ingredient>();
        }
    }

    public class Ingredient
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }

        public Ingredient(int itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
