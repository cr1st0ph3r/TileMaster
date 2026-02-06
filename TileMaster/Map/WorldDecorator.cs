using System;
using System.Collections.Generic;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;

namespace TileMaster.Map
{
    public static class WorldDecorator
    {
        private static Random _random = new Random();

        /// <summary>
        /// Decorates the world with random features like stalactites and stalagmites.
        /// Should be called after the main map generation but before chunk creation.
        /// </summary>
        /// <param name="mapDictionary">The dictionary containing all map tiles.</param>
        public static void DecorateCaves(Dictionary<int, CollisionTile> mapDictionary)
        {
            // Decoration parameters
            float decorationChance = 0.1f; // 10% chance to place a decoration on a valid spot
            
            // We iterate through a snapshot of keys or standard loop if we are sure we won't modify the collection structure (we won't remove keys, only update values)
            // Since we might need to access neighbors, working with the dictionary is fine.

            // Get map dimensions from Global to avoid iterating everything to find bounds is risky if map is irregular, 
            // but MapManager guarantees a rectangular map of Global.MapWidth x Global.MapHeight
            
            int mapWidth = Global.MapWidth;
            int mapHeight = Global.MapHeight;

            for (int globalId = 0; globalId < mapWidth * mapHeight; globalId++)
            {
                if (!mapDictionary.TryGetValue(globalId, out var currentTile))
                    continue;

                // We only decorate Air tiles (putting things INTO the air, attached to blocks)
                if (currentTile.TileId != (int)TileType.Air)
                    continue;

                // Check depth - only decorate below RockLevel
                if (currentTile.Y < Global.RockLevel)
                    continue;

                // Random chance check first to avoid expensive checks
                if (_random.NextDouble() > decorationChance)
                    continue;

                // 1. Identify context
                // We need neighbors: Up, Down
                int upId = globalId - mapWidth;
                int downId = globalId + mapWidth;

                bool hasCeiling = IsSolidBlock(mapDictionary, upId);
                bool hasFloor = IsSolidBlock(mapDictionary, downId);

                if (hasCeiling && hasFloor)
                {
                    // If we have both ceiling and floor (1 block high gap), pick one randomly
                    if (_random.NextDouble() > 0.5)
                    {
                         var newTile = CreateDecorationTile((int)TileType.Stalactite, currentTile.X, currentTile.Y, globalId);
                         mapDictionary[globalId] = newTile;
                    }
                    else
                    {
                         var newTile = CreateDecorationTile((int)TileType.Stalagmite, currentTile.X, currentTile.Y, globalId);
                         mapDictionary[globalId] = newTile;
                    }
                }
                else if (hasCeiling)
                {
                    // Potential Stalactite (hanging from roof)
                    var newTile = CreateDecorationTile((int)TileType.Stalactite, currentTile.X, currentTile.Y, globalId);
                    mapDictionary[globalId] = newTile;
                }
                else if (hasFloor)
                {
                    // Potential Stalagmite (standing on floor)
                    var newTile = CreateDecorationTile((int)TileType.Stalagmite, currentTile.X, currentTile.Y, globalId);
                    mapDictionary[globalId] = newTile;
                }
            }
        }

        private static bool IsSolidBlock(Dictionary<int, CollisionTile> map, int globalId)
        {
            if (map.TryGetValue(globalId, out var tile))
            {
                // Simple solid check. Refine if we have specific non-solid blocks to exclude (like other decorations)
                // Assuming Air is 0. 
                // Also check if it's not another non-solid block like Water or TallGrass if they exist at this stage.
                return tile.TileId != (int)TileType.Air && 
                       tile.TileId != (int)TileType.Water && 
                       tile.TileId != (int)TileType.TallGrass &&
                       tile.TileId != (int)TileType.Stalactite && 
                       tile.TileId != (int)TileType.Stalagmite;
            }
            return false; // Out of bounds is not solid for attachment purposes usually
        }

        private static CollisionTile CreateDecorationTile(int tileTypeId, int x, int y, int globalId)
        {
            var tType = Global.ReferenceTiles[tileTypeId];
            return new CollisionTile(tType, x, y, 0, globalId);
        }
    }
}
