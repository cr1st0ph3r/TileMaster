using System;
using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;

namespace TileMaster.Manager
{
    public static class ContainerManager
    {
        public static Dictionary<Guid, Container> Containers = new Dictionary<Guid, Container>();

        public static Container GetContainer(Guid id)
        {
            if (Containers.TryGetValue(id, out var container))
            {
                foreach (var item in container.Items.Where(x=>x.Value is not null))
                {
                    item.Value.Item = Global.ReferenceItems[item.Value.ItemId]; 
                }
                return container;
            }
            return null;
        }

        public static Container CreateContainer()
        {
            var container = new Container();
            Containers[container.Id] = container;
            return container;
        }

        public static void RemoveContainer(Guid id)
        {
            if (Containers.ContainsKey(id))
            {
                Containers.Remove(id);
            }
        }
    }
}
