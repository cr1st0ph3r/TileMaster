using System.Text.Json.Serialization;

namespace TileMaster.Entity.Tiles
{
    public class InventoryItem
    {
        public InventoryItem(Item item,int quantity)
        {
            Item = item;
            ItemId = item.Id;
            Quantity = quantity;
        }
        [JsonConstructorAttribute]
        public InventoryItem(int itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
        [JsonIgnore]
        public Item Item { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }
}
