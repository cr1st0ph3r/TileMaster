using System.Linq;
using System.Collections.Generic;
using TileMaster.Entity;
using TileMaster.Entity.Enums;

namespace TileMaster.Manager
{
    public class GrassManager
    {
        private Map.Map map;

        public GrassManager(Map.Map map)
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
                if (candidate.GlobalId == 22464)
                {

                }
                hasChanged |= CheckTileEligibilityForGrass(candidate);
            }

            // mark chunk if any change occurred           
            map.ChunkDictionary[chunkId].NeedUpdate = hasChanged;
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
            else if (destTile.TileId == (int)TileType.DirtWithGrass)
            {

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
                    var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];
                    var grassTexture = grassDef?.Textures.FirstOrDefault(x => x.Name.EndsWith(textureName));
                    map.SetTile(refTile, grassTexture);
                }
            }
        }
        private bool SetGrassTile(Tile destinationTile)
        {
            int mask = GetGrassMask(destinationTile);
            if (destinationTile.TextureName.EndsWith($"DirtWithGrass{mask.ToString()}"))
            {
                return false;
            }
            // 0 means it's surrounded by solid blocks (no air contact)
            if (mask == 0)
            {
                if (destinationTile.TileId == (int)TileType.DirtWithGrass)
                {
                    map.SetTile(destinationTile, (int)TileType.Dirt);
                    return true;
                }
                else if (destinationTile.TileId == (int)TileType.Dirt)
                {
                    //check corner
                    var res = GetInnerCornerDecorations(destinationTile);
                    if (res > 0)
                    {
                        // determine rotation (radians) for single-corner cases (values from GetInnerCornerDecorations)
                        float rotation = 0f;
                        var textureToUse = "DirtWithGrassCorner4";
                        var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];
                        if (res == 1)
                        {
                            textureToUse = "DirtWithGrassCorner1";
                        }
                        if (res == 2)
                        {
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(90f);
                            textureToUse = "DirtWithGrassCorner1";
                        }
                        if (res == 4)
                        {
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(180f);
                            textureToUse = "DirtWithGrassCorner1";
                        }
                        if (res == 8)
                        {
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(270f);
                            textureToUse = "DirtWithGrassCorner1";
                        }
                        else if (res == 5)
                        {
                            textureToUse = "DirtWithGrassCorner2";
                        }
                        else if (res == 10)
                        {
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(90f);
                            textureToUse = "DirtWithGrassCorner2";
                        }
                        else if (res == 7)
                        {
                            textureToUse = "DirtWithGrassCorner3";
                        }
                        else if (res == 11)
                        {
                            textureToUse = "DirtWithGrassCorner3";
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(90f);
                        }
                        else if (res == 13)
                        {
                            textureToUse = "DirtWithGrassCorner3";
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(1800f);
                        }
                        else if (res == 14)
                        {
                            textureToUse = "DirtWithGrassCorner3";
                            rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(270f);
                        }
                        var grassTexture = grassDef?.Textures.FirstOrDefault(x => x.Name.EndsWith(textureToUse));
                        map.SetTile(destinationTile, grassTexture, rotation);
                    }
                }
                return false;
            }
            else
            {
                // Mapping the mask value to your "TileX" naming convention
                string textureName = $"DirtWithGrass{mask}";

                var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];
                var grassTexture = grassDef?.Textures.FirstOrDefault(x => x.Name.EndsWith(textureName));
                destinationTile.TextureName = grassTexture.Name;
                destinationTile.TileId = (int)TileType.DirtWithGrass;
                map.SetTile(destinationTile, grassTexture);
                return true;
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

        private int GetInnerCornerDecorations(Tile tile)
        {
            var neighbors = map.tileInspector.GetNeighboringTiles(tile);
            int mask = 0;

            // Condition: Cardinal neighbors are Solid, but Diagonal is Air
            // Top-Left Tuft
            if (neighbors[1].IsSolid && neighbors[3].IsSolid && neighbors[0].TileId == (int)TileType.Air)
                mask |= 1;  // Top Left Tuft

            // Top-Right Tuft
            if (neighbors[1].IsSolid && neighbors[5].IsSolid && neighbors[2].TileId == (int)TileType.Air)
                mask |= 2; // Top Right Tuft

            // Bottom-Left Tuft
            if (neighbors[7].IsSolid && neighbors[3].IsSolid && neighbors[6].TileId == (int)TileType.Air)
                mask |= 8; // Bottom Left Tuft

            // Bottom-Right Tuft
            if (neighbors[7].IsSolid && neighbors[5].IsSolid && neighbors[8].TileId == (int)TileType.Air)
                mask |= 4; // Bottom Right Tuft

            return mask;
        }
    }
}