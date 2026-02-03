using TileMaster.Data;
using TileMaster.Entity.Tiles;
using TileMaster.Map;

namespace TileMaster.Test
{
    public static class TestHelper
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            // set up global paths for test environment
            // assuming we are running from TileMaster.Test/bin/Debug/net10.0-windows7.0
            // we need to point to TileMaster/Data
            // however, for simplicity in this environment, let's assume the current directory works
            // or the user has set up the project to copy these files.
            
            // initialize global state
            Global.ReferenceTiles = DataLoader.LoadTilesTypes(null);
            Global.ReferenceItems = DataLoader.LoadItems(null);
            
            _initialized = true;
        }

        public static Map.Map CreateTestMap(int widthChunks = 2, int heightChunks = 2)
        {
            Initialize();

            // Set global map size for tests
            // Global.MapWidth and MapHeight are static, so this affects all maps
            // This is a side effect of the singleton-like Global class.
            // In a better architecture, these would be instance members of Map.
            
            var map = new Map.Map();
            Global.MapWidth = widthChunks * Global.ChunkSize;
            Global.MapHeight = heightChunks * Global.ChunkSize;
            map.Width = Global.MapWidth;
            map.Height = Global.MapHeight;
            
            // initialize chunks
            map.Chunks = new Chunk[widthChunks * heightChunks];
            for (int i = 0; i < map.Chunks.Length; i++)
            {
                map.Chunks[i] = new Chunk();
                map.Chunks[i].Tiles = new CollisionTile[Global.ChunkSize * Global.ChunkSize];
                map.Chunks[i].BackgroundTiles = new BackgroundTile[Global.ChunkSize * Global.ChunkSize];
                
                // Fill with air by default
                for (int j = 0; j < map.Chunks[i].Tiles.Length; j++)
                {
                    int x = (i % widthChunks) * Global.ChunkSize + (j % Global.ChunkSize);
                    int y = (i / widthChunks) * Global.ChunkSize + (j / Global.ChunkSize);
                    
                    var airRef = Global.ReferenceTiles[0]; // Assuming 0 is Air
                    map.Chunks[i].Tiles[j] = new CollisionTile(airRef, x, y, j, y * (widthChunks * Global.ChunkSize) + x);
                    map.Chunks[i].Tiles[j].ChunkId = i + 1; // 1-based
                    map.Chunks[i].Tiles[j].IsOccupied = false; // Air is NOT occupied
                    
                    var bgAirRef = Global.ReferenceTiles[0];
                    map.Chunks[i].BackgroundTiles[j] = new BackgroundTile {
                        X = x,
                        Y = y,
                        GlobalId = y * (widthChunks * Global.ChunkSize) + x,
                        TileId = 0,
                        IsOccupied = false,
                        ChunkId = i + 1 // 1-based
                    };
                }
            }
            
            return map;
        }
    }
}
