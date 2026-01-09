using System.Collections.Generic;
using System.Text.Json.Serialization;
using TileMaster.Entity;

namespace TileMaster.Map
{
    public class WorldData
    {
        [JsonIgnore]
        public Dictionary<int,Dictionary<int, CollisionTile>> RawMapData { get; set; }
        [JsonIgnore]
        public Dictionary<int,Dictionary<int, BackgroundTile>> RawBackgroundData { get; set; }
        public int WorldHeight { get; set; }
        public int WorldWidth { get; set; }
    }
}
