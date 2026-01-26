using System;
using System.Collections.Generic;
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
