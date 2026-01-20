namespace TileMaster.Entity.Tiles
{
    public class InventoryItem
    {
        public InventoryItem(Item item,int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
        public Item Item { get; set; }
        public int Quantity { get; set; }
    }
}
