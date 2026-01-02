using System.Linq;
using TileMaster.Entity;

namespace TileMaster.Map
{
    public class GrassManager
    {
        private Map map;
        private int Up = 1;
        private int Left = 3;
        private int Right = 5;
        private int Down = 7;

        public GrassManager(Map map)
        {
            this.map = map;
        }
        /// <summary>
        /// Gets all surrounding tiles and check whether they can have grass grown onto them
        /// </summary>
        /// <param name="chunkId"></param>
        public void GrowGrass(int chunkId)
        {
            var hasChanged = false;
            foreach (var tile in map.ChunkDictionary[chunkId].Tiles.Where(x => x.Value.TileId == (int)TileType.DirtWithGrass).ToList())
            {
                //checks if the neighboring block is dirt and if it has air above it so it can grow grass              
                var neighbors = map.tileInspector.GetNeighboringTiles(tile.Value);

                //TODO INVESTIGATE THIS
                if(neighbors.Count < 9)
                {
                    return;
                }
                //[x]<-[]
                hasChanged = CheckTileEligibilityForGrass(neighbors[Left]);
                //[]->[x]
                hasChanged = CheckTileEligibilityForGrass(neighbors[Right]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                hasChanged = CheckTileEligibilityForGrass(neighbors[8]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[Down]);
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[6]);
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[2]);
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[Up]);
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                hasChanged = CheckTileEligibilityForGrass(neighbors[0]);
            }
            //need to check whether the chunk has changed or not
            if (hasChanged)
            {
                map.ChunkDictionary[chunkId].NeedGrassUpdate = hasChanged;
            }
            SetTileColor(chunkId);
        }

        public void SetTileColor(int chunkId)
        {
            var hasChanged = false;
            foreach (var tile in map.ChunkDictionary[chunkId].Tiles.Where(x => x.Value.IsSolid).ToList())
            {
                //checks if the neighboring block is dirt and if it has air above it so it can grow grass              
                var neighbors = map.tileInspector.GetNeighboringTiles(tile.Value,2);
                if(neighbors.All(x=>x.IsSolid))
                {
                    tile.Value.Color = "Gray";
                }
                else
                {
                    tile.Value.Color = "White";
                }
            }            
        }

        /// <summary>
        /// Checks whether a tile can have grass
        /// </summary>
        /// <param name="destTile"></param>
        /// <returns></returns>
        private bool CheckTileEligibilityForGrass(Tile destTile)
        {

            if (destTile.TileId == (int)TileType.Dirt)
            {
                return SetGrassTile(destTile);
            }
            return false;
        }
        /// <summary>
        /// Set a dirt block into dirt with grass
        /// </summary>
        /// <param name="destinationTile"></param>
        /// <returns></returns>
        private bool SetGrassTile_old(Map map, Tile destinationTile)
        {
            var top = false;
            var bottom = false;
            var left = false;
            var right = false;

            var neighbors = map.tileInspector.GetNeighboringTiles(destinationTile);

            var hasChanged = true;

            //grass can be planted
            //check neighboring tiles to define where on the tile grass will grow
            if (neighbors[Up].TileId == (int)TileType.Air)
            {
                top = true;
            }
            if (neighbors[Left].TileId == (int)TileType.Air)
            {
                left = true;
            }
            if (neighbors[Right].TileId == (int)TileType.Air)
            {
                right = true;
            }
            if (neighbors[Down].TileId == (int)TileType.Air)
            {
                bottom = true;
            }


            if (top == false && bottom == false && left == false && right == false)
            {
                //0000
                //no dice
                if (destinationTile.TileId != (int)TileType.Dirt)
                {
                    map.SetTile(destinationTile.GlobalId, (int)TileType.Dirt, destinationTile.ChunkId);
                }
                hasChanged = false;
            }
            else if (top == false && bottom == false && left == false && right)
            {
                //0001
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile9"));
            }
            else if (top == false && bottom == false && left && right == false)
            {
                //0010
                Game.LogMessage($"Will set tile {destinationTile.GlobalId} ({destinationTile.Name} as DirtWithGrass)", null);
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile10"));
            }
            else if (top == false && bottom == false && left && right)
            {
                //0011
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile11"));
            }
            else if (top == false && bottom && left == false && right == false)
            {
                //0100
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile8"));
            }
            else if (top == false && bottom && left == false && right)
            {
                //0101
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile12"));
            }
            else if (top == false && bottom && left && right == false)
            {
                //0110
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile13"));
            }
            else if (top == false && bottom && left && right)
            {
                //0111
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile14"));
            }
            else if (top && bottom == false && left == false && right == false)
            {
                //1000
                map.SetTile(destinationTile.GlobalId, (int)TileType.DirtWithGrass, destinationTile.ChunkId);
                map.ChunkDictionary[destinationTile.ChunkId].HasGrass = true;
            }
            else if (top && bottom == false && left == false && right)
            {
                //1001
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile4"));
            }
            else if (top && bottom == false && left && right == false)
            {
                //1010
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile5"));
            }
            else if (top && bottom == false && left && right)
            {
                //1011
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile6"));
            }
            else if (top && bottom && left == false && right == false)
            {
                //1100
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile15"));
            }
            else if (top && bottom && left == false && right)
            {
                //1101
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile16"));
            }
            else if (top && bottom && left && right == false)
            {
                //1110
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile17"));
            }
            else
            {
                //1111
                map.SetTileAsGrass(destinationTile.GlobalId,
                (int)TileType.DirtWithGrass,
                destinationTile.ChunkId,
                map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.Dirt).Textures.FirstOrDefault(x => x.Name == "Tile7"));
            }
            return hasChanged;
        }

        /// <summary>
        /// Set a dirt block into dirt with grass
        /// </summary>
        /// <param name="destinationTile"></param>
        /// <returns></returns>
        private bool SetGrassTile(Tile destinationTile)
        {
            // Ensure the destination tile is actually dirt before attempting to grow grass
            if (destinationTile.TileId != (int)TileType.Dirt)
            {
                // If it's not dirt and not already grass, no change happens.
                // If it's already grass, we might want to re-evaluate its texture.
                // For now, let's assume we only process dirt tiles.
                return false;
            }

            var neighbors = map.tileInspector.GetNeighboringTiles(destinationTile);

            // Define the bitmask for each direction
            const int TOP_BIT = 1;
            const int BOTTOM_BIT = 2;
            const int LEFT_BIT = 4;
            const int RIGHT_BIT = 8;

            var grassPatternMask = 0;

            // Check neighboring tiles and build the bitmask
            if (neighbors[1].TileId == (int)TileType.Air) // Top
            {
                grassPatternMask |= TOP_BIT;
            }
            if (neighbors[7].TileId == (int)TileType.Air) // Bottom
            {
                grassPatternMask |= BOTTOM_BIT;
            }
            if (neighbors[3].TileId == (int)TileType.Air) // Left
            {
                grassPatternMask |= LEFT_BIT;
            }
            if (neighbors[5].TileId == (int)TileType.Air) // Right
            {
                grassPatternMask |= RIGHT_BIT;
            }

            string textureName = null;

            // Use a switch statement or a dictionary for mapping the mask to texture names
            // This makes the mapping explicit and easy to read/modify.
            switch (grassPatternMask)
            {
                case 0: // 0000 - No adjacent air tiles
                        // If it's dirt and surrounded by non-air, it doesn't grow grass.
                        // Or if it was grass, it reverts to plain dirt.
                    if (destinationTile.TileId != (int)TileType.Dirt) // If it was grass, turn it back to dirt
                    {
                        map.SetTile(destinationTile.GlobalId, (int)TileType.Dirt, destinationTile.ChunkId);
                        return true; // A change occurred
                    }
                    return false; // No change needed
                case RIGHT_BIT: textureName = "Tile9"; break;    // 0001 (Right)
                case LEFT_BIT: textureName = "Tile10"; break;   // 0010 (Left)
                case LEFT_BIT | RIGHT_BIT: textureName = "Tile11"; break;// 0011 (Left, Right)
                case BOTTOM_BIT: textureName = "Tile8"; break;    // 0100 (Bottom)
                case BOTTOM_BIT | RIGHT_BIT: textureName = "Tile12"; break; // 0101 (Bottom, Right)
                case BOTTOM_BIT | LEFT_BIT: textureName = "Tile13"; break; // 0110 (Bottom, Left)
                case BOTTOM_BIT | LEFT_BIT | RIGHT_BIT: textureName = "Tile14"; break; // 0111 (Bottom, Left, Right)
                case TOP_BIT:
                    // This case in your original code seems to be a special one:
                    // SetTile(destinationTile.GlobalId, (int)TileType.DirtWithGrass, destinationTile.ChunkId);
                    // ChunkDictionary[destinationTile.ChunkId].HasGrass = true;
                    // This suggests "Tile1" (or default grass texture) for top-only exposure.
                    // Let's assume a default grass tile here if no specific texture is provided.
                    // If "Tile1" means the basic top grass, then use it.
                    // If you had a specific "top-only" texture like "Tile0", it would go here.
                    // For simplicity, let's assume the basic grass texture is implicitly handled by SetTileAsGrass.
                    // If there's a specific "main grass" texture, let's get it.
                    textureName = "Tile3"; // Or whatever your default top grass texture is.
                    break;
                case TOP_BIT | RIGHT_BIT: textureName = "Tile4"; break;    // 1001 (Top, Right)
                case TOP_BIT | LEFT_BIT: textureName = "Tile5"; break;     // 1010 (Top, Left)
                case TOP_BIT | LEFT_BIT | RIGHT_BIT: textureName = "Tile6"; break; // 1011 (Top, Left, Right)
                case TOP_BIT | BOTTOM_BIT: textureName = "Tile15"; break;  // 1100 (Top, Bottom)
                case TOP_BIT | BOTTOM_BIT | RIGHT_BIT: textureName = "Tile16"; break; // 1101 (Top, Bottom, Right)
                case TOP_BIT | BOTTOM_BIT | LEFT_BIT: textureName = "Tile17"; break; // 1110 (Top, Bottom, Left)
                case TOP_BIT | BOTTOM_BIT | LEFT_BIT | RIGHT_BIT: textureName = "Tile7"; break; // 1111 (All four sides)
                default:
                    // This case should ideally not be hit if all combinations are covered.
                    // Or you might have a default grass texture for unlisted combinations.
                    textureName = "Tile1"; // Fallback to a generic grass texture if none matches exactly
                    break;
            }

            // Get the relevant TileDefinition for Dirt (where the grass textures are stored)
            var dirtTileDefinition = map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.DirtWithGrass);


            if (dirtTileDefinition != null && textureName != null)
            {
                var grassTexture = dirtTileDefinition.Textures.FirstOrDefault(x => x.Name == textureName);
                if (grassTexture != null)
                {
                    map.SetTileAsGrass(destinationTile.GlobalId, (int)TileType.DirtWithGrass, destinationTile.ChunkId, grassTexture);
                    // You had this line for the 1000 case, consider if it's always needed or specific
                    // ChunkDictionary[destinationTile.ChunkId].HasGrass = true;
                    return true; // A change occurred
                }
            }

            // If no texture was found or some condition prevented setting, no change.
            return false;
        }

        /// <summary>
        /// Verifies if neighboring tiles need to have its texture changed
        /// </summary>
        /// <param name="chunkId"></param>
        /// <param name="blockId"></param>
        public void CheckNeighboringGrassBlocks(Map map, int chunkId, int blockId)
        {
            //verifies if neighboring grass blocks needs to have its texture updated

            var neighbors = map.tileInspector.GetNeighboringTiles(map.ChunkDictionary[chunkId].Tiles[blockId]);
            if (neighbors[Down].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[Down]);
            }
            if (neighbors[Right].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[Right]);
            }
            if (neighbors[Left].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[Left]);
            }
            if (neighbors[Up].Name == TileType.DirtWithGrass.ToString())
            {
                SetGrassTile(neighbors[Up]);
            }

        }       
    }
}
