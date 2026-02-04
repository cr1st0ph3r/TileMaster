using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;

namespace TileMaster.Manager
{
    public class GrassManager
    {
        private Map.Map map;

        // Map from inner-corner mask -> (texture name, rotation degrees)
        private static readonly IReadOnlyDictionary<int, (string Texture, float RotationDegrees)> InnerCornerMap =
            new Dictionary<int, (string, float)>
            {
                // single-corner cases (use Corner1, rotated)
                { 1,  ("DirtWithGrass6",   0f) },
                { 2,  ("DirtWithGrass6",  90f) },
                { 4,  ("DirtWithGrass6", 180f) },
                { 8,  ("DirtWithGrass6", 270f) },

                // two-corner cases (Corner2)
                { 5,  ("DirtWithGrass7",   0f) },
                { 10, ("DirtWithGrass7",  90f) },

                // multi-corner cases (Corner3, rotated as needed)
                { 7,  ("DirtWithGrass8",   0f) },
                { 11, ("DirtWithGrass8",  90f) },
                { 13, ("DirtWithGrass8", 180f) },
                { 14, ("DirtWithGrass8", 270f) }
            };
        private static readonly IReadOnlyDictionary<int, (string Texture, float RotationDegrees)> GrassSurfaceMap =
            new Dictionary<int, (string, float)>
            {
                // One surface with grass
                { 1,  ("DirtWithGrass1",   0f) },
                { 2,  ("DirtWithGrass1",  90f) },
                { 4,  ("DirtWithGrass1", 180f) },
                { 8,  ("DirtWithGrass1", 270f) },

                //top and down or left and right
                { 5, ("DirtWithGrass3",   0f) },
                { 10,("DirtWithGrass3",  90f) },

                // Two surfaces with grass
                { 3, ("DirtWithGrass2",   0f) },
                { 6, ("DirtWithGrass2",  90f) },
                { 9, ("DirtWithGrass2",  270f) },
                { 12,("DirtWithGrass2",  180f) },

                // Three surfaces with grass
                { 7,  ("DirtWithGrass4",   0f) },
                { 11, ("DirtWithGrass4",  90f) },
                { 13, ("DirtWithGrass4", 180f) },
                { 14, ("DirtWithGrass4", 270f) }
            };

        public GrassManager(Map.Map map)
        {
            this.map = map;
        }

        /// <summary>
        /// Gets all surrounding tiles and check whether they can have grass grown onto them
        /// </summary>
        /// <param name="chunkId">1-based chunk ID</param>
        public void GrowGrass(int chunkId)
        {
            var hasChanged = false;
            var chunk = map.GetChunk(chunkId);
            if (chunk == null || chunk.Tiles == null) return;

            // First phase: collect unique candidate tiles (don't mutate the map while collecting).
            var candidates = new Dictionary<int, Tile>(); // key = GlobalId to avoid duplicates across neighbors

            foreach (var tile in chunk.Tiles.Where(x => x != null && x.TileId == (int)TileType.DirtWithGrass).ToList())
            {
                var neighbors = map.tileInspector.GetNeighboringTiles(tile);
                foreach (var neighbor in neighbors)
                {
                    if (neighbor == tile)
                    {
                        if (!candidates.ContainsKey(neighbor.GlobalId))
                            candidates[neighbor.GlobalId] = neighbor;
                        continue;
                    }

                    // Only consider tiles that might change or influence grass (dirt, dirt-with-grass, or slope dirt)
                    if (neighbor.TileId == (int)TileType.Dirt ||
                        neighbor.TileId == (int)TileType.DirtWithGrass ||
                        (neighbor.IsSlope && neighbor.TileId == (int)TileType.Dirt))
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
                bool tileChanged = CheckTileEligibilityForGrass(candidate);
                if (tileChanged)
                {
                    Game.LogMessage($"Tile {candidate.GlobalId} changed to grass.", null);
                }
                hasChanged |= tileChanged;
            }

            // mark chunk if any change occurred           
            chunk.NeedUpdate = hasChanged;

            // Phase 3: Grow TallGrass on top of existing grass tiles
            GrowTallGrass(chunkId);

            // Phase 4: Clean up floating TallGrass
            CleanupFloatingTallGrass(chunkId);
        }

        /// <summary>
        /// Periodically checks tiles with grass that are in contact with air above and grows tall grass.
        /// </summary>
        /// <param name="chunkId"></param>
        private void GrowTallGrass(int chunkId)
        {
            var chunk = map.GetChunk(chunkId);
            if (chunk == null || chunk.Tiles == null) return;

            foreach (var tile in chunk.Tiles.Where(x => x != null && x.TileId == (int)TileType.DirtWithGrass).ToList())
            {
                // Check tile above
                var tileAbove = map.GetTileAt(tile.X, tile.Y - 1);
                if (tileAbove != null && tileAbove.TileId == (int)TileType.Air)
                {
                    // Grow tall grass with a small chance (5%)
                    var random = Game.rnd ?? new System.Random();
                    if (random.Next(100) < 5)
                    {
                        map.SetTile(tileAbove.ChunkId, tileAbove.GlobalId, (int)TileType.TallGrass);
                        chunk.NeedUpdate = true;
                    }
                }
            }
        }

        /// <summary>
        /// Removes tall grass that does not have a supporting grass block underneath
        /// </summary>
        /// <param name="chunkId"></param>
        private void CleanupFloatingTallGrass(int chunkId)
        {
            var chunk = map.GetChunk(chunkId);
            if (chunk == null || chunk.Tiles == null) return;

            foreach (var tile in chunk.Tiles.Where(x => x != null && x.TileId == (int)TileType.TallGrass).ToList())
            {
                var tileBelow = map.GetTileAt(tile.X, tile.Y + 1);
                if (tileBelow == null || tileBelow.TileId != (int)TileType.DirtWithGrass)
                {
                    map.SetTile(tile, (int)TileType.Air);
                    chunk.NeedUpdate = true;
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
            // Handle regular dirt tiles
            if (destTile.TileId == (int)TileType.Dirt || destTile.TileId == (int)TileType.DirtWithGrass)
            {
                return SetGrassTile(destTile);
            }

            // Handle slope dirt tiles
            if (destTile.IsSlope && destTile.TileId == (int)TileType.Dirt)
            {
                return SetGrassTile(destTile);
            }

            return false;
        }

        /// <summary>
        /// Sets a grass tile based on its eligibility and available grass textures.
        /// </summary>
        /// <param name="destinationTile">The tile to set grass on.</param>
        /// <returns>True if the grass was successfully set, false otherwise.</returns>
        private bool SetGrassTile(Tile destinationTile)
        {
            if (destinationTile.IsSlope)
            {
                return SetSlopeGrassTile(destinationTile);
            }
            int mask = GetGrassMask(destinationTile);
            if (IsTileAlreadCorrectyGrass(destinationTile, mask))
            {
                return false;
            }
            // 0 means it's surrounded by solid blocks (no air contact)
            if (mask == 0)
            {
                // check for inner corners (diagonal air)
                var res = GetInnerCornerDecorations(destinationTile);
                if (res > 0)
                {
                    // determine texture and rotation using a lookup map (reduces branching)
                    float rotation = 0f;
                    string textureToUse = "DirtWithGrass9"; // default (all solid, no single/multi corner match)
                    var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];

                    if (InnerCornerMap.TryGetValue(res, out var cfg))
                    {
                        textureToUse = cfg.Texture;
                        rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(cfg.RotationDegrees);
                    }
                    if (textureToUse == destinationTile.TextureName)
                    {
                        return false;
                    }

                    destinationTile.TextureName = textureToUse;
                    destinationTile.SourceRectangle = Global.AtlasMap[textureToUse].Rectangle;
                    destinationTile.Rotation = rotation;
                    return true;
                }
                else
                {
                    //tile might not have any contact with air
                    //set back to dirt              
                    var dirtDef = Global.ReferenceTiles[(int)TileType.Dirt];
                    destinationTile.SourceRectangle = Global.AtlasMap[dirtDef.TextureName].Rectangle;
                    destinationTile.TextureId = mask;
                    destinationTile.TileId = (int)TileType.Dirt;
                    destinationTile.TextureName = dirtDef.TextureName;
                    destinationTile.Rotation = 0;
                    destinationTile.Name = dirtDef.Name;
                    return false;
                }
            }
            else
            {
                (var textureName, var rotation) = getTextureNameForMask(mask);
                var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];
                var grassTile = Global.AtlasMap[textureName];
                destinationTile.Name = grassDef.Name;
                destinationTile.SourceRectangle = grassTile.Rectangle;
                destinationTile.TextureId = mask;
                destinationTile.TileId = (int)TileType.DirtWithGrass;
                destinationTile.TextureName = textureName;
                destinationTile.Rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(rotation);
                return true;
            }
        } 

        /// <summary>
        /// Performs a check to see if the tile is already set to the correct grass texture and rotation
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="mask"></param>
        /// <returns></returns>
        bool IsTileAlreadCorrectyGrass(Tile tile, int mask)
        {
            (var textureName, var rotation) = getTextureNameForMask(mask);
            if (tile.Rotation == Microsoft.Xna.Framework.MathHelper.ToRadians(rotation) &&
               tile.TextureName == textureName)
            {
                return true;
            }
            return false;
        }
     
        /// <summary>
        /// Retrieves the texture name and rotation angle associated with the specified grass mask value.
        /// </summary>
        /// <remarks>If the provided mask does not exist in the configuration, the method returns a
        /// default texture and rotation representing grass on all sides.</remarks>
        /// <param name="mask">An integer representing the grass mask. Determines which texture and rotation are selected based on the mask
        /// configuration.</param>
        /// <returns>A tuple containing the texture name as a string and the rotation angle in degrees as a float. Returns
        /// ("DirtWithGrass5", 0f) if the mask is not found.</returns>
        (string, float) getTextureNameForMask(int mask)
        {
            if (GrassSurfaceMap.TryGetValue(mask, out var cfg))
            {
                return (cfg.Texture, cfg.RotationDegrees);
            }
            return ("DirtWithGrass5", 0f);// default (grass on all sides)
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

            if (!neighbors[1].IsOccupied) mask |= 1;  // Top
            if (!neighbors[5].IsOccupied) mask |= 2;  // Right
            if (!neighbors[7].IsOccupied) mask |= 4;  // Bottom
            if (!neighbors[3].IsOccupied) mask |= 8;  // Left

            return mask;
        }

        /// <summary>
        /// Calculates a bitmask indicating which inner corners of the specified tile are adjacent to air tiles.
        /// </summary>
        /// <remarks>The returned mask uses the following bit positions: 0 (top-left), 1 (top-right), 2 (bottom-left),
        /// and 3 (bottom-right). This can be used to determine where grass corners should be rendered around the tile.</remarks>
        /// <param name="tile">The tile for which to determine the grass corner mask.</param>
        /// <returns>An integer bitmask where each bit represents whether the corresponding corner of the tile is adjacent to an
        /// air tile: bit 0 for top-left, bit 1 for top-right, bit 2 for bottom-left, and bit 3 for bottom-right. A set bit indicates
        /// adjacency to an air tile on that corner.</returns>
        private int GetInnerCornerDecorations(Tile tile)
        {
            var neighbors = map.tileInspector.GetNeighboringTiles(tile);
            int mask = 0;

            // Condition: Cardinal neighbors are Solid, but Diagonal is Air
            // Top-Left Tuft
            if (neighbors[1].IsSolid && neighbors[3].IsSolid && !neighbors[0].IsSolid)
                mask |= 1;  // Top Left Tuft

            // Top-Right Tuft
            if (neighbors[1].IsSolid && neighbors[5].IsSolid && !neighbors[2].IsSolid)
                mask |= 2; // Top Right Tuft

            // Bottom-Left Tuft
            if (neighbors[7].IsSolid && neighbors[3].IsSolid && !neighbors[6].IsSolid)
                mask |= 8; // Bottom Left Tuft

            // Bottom-Right Tuft
            if (neighbors[7].IsSolid && neighbors[5].IsSolid && !neighbors[8].IsSolid)
                mask |= 4; // Bottom Right Tuft

            return mask;
        }

        /// <summary>
        /// Sets grass on a slope tile, maintaining the slope angle
        /// </summary>
        /// <param name="destinationTile">The slope tile to set grass on</param>
        /// <returns>True if grass was successfully set, false otherwise</returns>
        private bool SetSlopeGrassTile(Tile destinationTile)
        {
            // Only allow grass on dirt slopes
            if (destinationTile.TileId != (int)TileType.Dirt)
                return false;

            // Check if the slope has air contact (required for grass growth)
            var neighbors = map.tileInspector.GetNeighboringTiles(destinationTile);
            bool hasAirContact = false;

            // Check different sides based on slope rotation
            switch (destinationTile.SlopeRotation)
            {
                case 0: // Slope rising to right - check top and right sides
                    hasAirContact = !neighbors[1].IsOccupied || !neighbors[5].IsOccupied;
                    break;
                case 1: // Slope rising to left - check top and left sides
                    hasAirContact = !neighbors[1].IsOccupied || !neighbors[3].IsOccupied;
                    break;
                case 2: // Inverted slope rising to left - check bottom and left sides
                    hasAirContact = !neighbors[7].IsOccupied || !neighbors[3].IsOccupied;
                    break;
                case 3: // Inverted slope rising to right - check bottom and right sides
                    hasAirContact = !neighbors[7].IsOccupied || !neighbors[5].IsOccupied;
                    break;
            }

            if (!hasAirContact)
                return false;

            // Try to find a slope grass texture
            var grassDef = Global.ReferenceTiles[(int)TileType.DirtWithGrass];
            //var slopeGrassTexture = grassDef?.Textures?.FirstOrDefault(x => x.Name.EndsWith("DirtWithGrassSlope"));
            var slopeGrassTextureRectangle = Global.AtlasMap["DirtWithGrassSlope"];

            // Found the slope grass texture - apply rotation based on slope rotation
            destinationTile.TileId = (int)TileType.DirtWithGrass;
            destinationTile.TextureName = "DirtWithGrassSlope";
            destinationTile.IsSlope = true; // Maintain slope property
            destinationTile.SlopeRotation = destinationTile.SlopeRotation; // Maintain rotation

            // Set rotation for the texture
            float rotation = 0f;
            switch (destinationTile.SlopeRotation)
            {
                case 1:
                    rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(90f);
                    break;
                case 2:
                    rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(180f);
                    break;
                case 3:
                    rotation = Microsoft.Xna.Framework.MathHelper.ToRadians(270f);
                    break;
            }

            destinationTile.Rotation = rotation;
            map.SetTile(destinationTile, (int)TileType.DirtWithGrass, "DirtWithGrassSlope", rotation);
            return true;
        }
    }
}