using System.Linq;
using System.Collections.Generic;
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

            // First phase: collect unique candidate tiles (don't mutate the map while collecting).
            var candidates = new Dictionary<int, Tile>(); // key = GlobalId to avoid duplicates across neighbors

            foreach (var tile in map.ChunkDictionary[chunkId].Tiles.Where(x => x.Value.TileId == (int)TileType.DirtWithGrass).ToList())
            {
                var neighbors = map.tileInspector.GetNeighboringTiles(tile.Value);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor == tile.Value)
                    {
                        //update the tile itself as well
                        UpdateSorceGrassTile(neighbor);
                        continue;
                    }

                    // Only consider tiles that might change or influence grass (dirt or already dirt-with-grass)
                    if (neighbor.TileId == (int)TileType.Dirt || neighbor.TileId == (int)TileType.DirtWithGrass)
                    {
                        if (!candidates.ContainsKey(neighbor.GlobalId))
                            candidates[neighbor.GlobalId] = neighbor;
                    }
                }
            }

            // Second phase: apply changes using the snapshot of candidates.
            // This ensures mask computations use the original map state (no race between neighboring updates).
            foreach (var candidate in candidates.Values)
            {
                hasChanged |= CheckTileEligibilityForGrass(candidate);
            }

            // mark chunk if any change occurred           
            map.ChunkDictionary[chunkId].NeedGrassUpdate = hasChanged;


            SetTileColor(chunkId);
        }

        public void SetTileColor(int chunkId)
        {
            foreach (var tile in map.ChunkDictionary[chunkId].Tiles.Where(x => x.Value.IsSolid).ToList())
            {
                //checks if the neighboring block is dirt and if it has air above it so it can grow grass              
                var neighbors = map.tileInspector.GetNeighboringTiles(tile.Value, 2);
                if (neighbors.All(x => x.IsSolid))
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
            if (destTile.TileId == (int)TileType.Dirt || destTile.TileId == (int)TileType.DirtWithGrass)
            {
                return SetGrassTile(destTile);
            }
            return false;
        }

        private void UpdateSorceGrassTile(Tile refTile)
        {
            int mask = GetGrassMask(refTile);
            if (mask != 0)
            {
                if (!refTile.TextureName.EndsWith(mask.ToString()))
                {
                    string textureName = $"Grass{mask}";
                    var grassDef = map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.DirtWithGrass);
                    var grassTexture = grassDef?.Textures.FirstOrDefault(x => x.Name.EndsWith(textureName));
                    map.SetTileAsGrass(refTile.GlobalId, (int)TileType.DirtWithGrass, refTile.ChunkId, grassTexture);
                }
            }
        }
        private bool SetGrassTile(Tile destinationTile)
        {
            if (destinationTile.TileId != (int)TileType.Dirt && destinationTile.TileId != (int)TileType.DirtWithGrass)
            {
                return false;
            }

            int mask = GetGrassMask(destinationTile);
            if (destinationTile.TextureName.EndsWith(mask.ToString()))
            {
                return false;
            }
            // 0 means it's surrounded by solid blocks (no air contact)
            if (mask == 0)
            {
                if (destinationTile.TileId == (int)TileType.DirtWithGrass)
                {
                    map.SetTile(destinationTile.GlobalId, (int)TileType.Dirt, destinationTile.ChunkId);
                    return true;
                }
                return false;
            }
            else
            {         // Mapping the mask value to your "TileX" naming convention
                string textureName = $"DirtWithGrass{mask}";

                var grassDef = map.TileTypes.FirstOrDefault(x => x.TileId == (int)TileType.DirtWithGrass);
                var grassTexture = grassDef?.Textures.FirstOrDefault(x => x.Name.EndsWith(textureName));

                if (grassTexture != null)
                {
                    map.SetTileAsGrass(destinationTile.GlobalId, (int)TileType.DirtWithGrass, destinationTile.ChunkId, grassTexture);
                    return true;
                }

                return false;
            }

        }

        /// <summary>
        /// Calculates a bitmask indicating which sides of the specified tile are adjacent to air tiles.
        /// </summary>
        /// <remarks>The returned mask uses the following bit positions: 0 (top), 1 (right), 2 (bottom),
        /// and 3 (left). This can be used to determine where grass edges should be rendered around the tile.</remarks>
        /// <param name="tile">The tile for which to determine the grass mask.</param>
        /// <returns>An integer bitmask where each bit represents whether the corresponding side of the tile is adjacent to an
        /// air tile: bit 0 for top, bit 1 for right, bit 2 for bottom, and bit 3 for left. A set bit indicates
        /// adjacency to an air tile on that side.</returns>
        private int GetGrassMask(Tile tile)
        {
            var neighbors = map.tileInspector.GetNeighboringTiles(tile);
            int mask = 0;

            // We use the same indices your TileInspector currently provides:
            // 1: Up, 3: Left, 5: Right, 7: Down
            if (neighbors[1].TileId == (int)TileType.Air) mask |= 1;  // Top
            if (neighbors[5].TileId == (int)TileType.Air) mask |= 2;  // Right
            if (neighbors[7].TileId == (int)TileType.Air) mask |= 4;  // Bottom
            if (neighbors[3].TileId == (int)TileType.Air) mask |= 8;  // Left

            return mask;
        }

        private List<string> GetInnerCornerDecorations(Tile tile)
        {
            var neighbors = map.tileInspector.GetNeighboringTiles(tile);
            var tufts = new List<string>();

            // Condition: Cardinal neighbors are Solid, but Diagonal is Air
            // Top-Left Tuft
            if (neighbors[1].IsSolid && neighbors[3].IsSolid && neighbors[0].TileId == (int)TileType.Air)
                tufts.Add("InnerCorner_TL");

            // Top-Right Tuft
            if (neighbors[1].IsSolid && neighbors[5].IsSolid && neighbors[2].TileId == (int)TileType.Air)
                tufts.Add("InnerCorner_TR");

            // Bottom-Left Tuft
            if (neighbors[7].IsSolid && neighbors[3].IsSolid && neighbors[6].TileId == (int)TileType.Air)
                tufts.Add("InnerCorner_BL");

            // Bottom-Right Tuft
            if (neighbors[7].IsSolid && neighbors[5].IsSolid && neighbors[8].TileId == (int)TileType.Air)
                tufts.Add("InnerCorner_BR");

            return tufts;
        }
    }
}