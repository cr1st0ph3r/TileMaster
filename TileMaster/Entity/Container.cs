using System;
using System.Collections.Generic;
using TileMaster.Entity.Tiles;

namespace TileMaster.Entity
{
    [Serializable]
    public class Container
    {
        public Guid Id { get; set; }
        public Dictionary<int, InventoryItem> Items { get; set; }

        public Container()
        {
            Id = Guid.NewGuid();
            Items = new Dictionary<int, InventoryItem>(40);
            for (int i = 0; i < 40; i++)
            {
                Items[i] = null;
            }
        }
    }
}
