using Microsoft.Xna.Framework;
using TileMaster.Entity.Enums;
using TileMaster.Entity.Tiles;

namespace TileMaster.Manager
{
    public class WaterManager
    {
        private Map.Map map;
        private float updateTimer = 0f;
        private const float UpdateInterval = 0.1f; // 100ms update rate

        public WaterManager(Map.Map map)
        {
            this.map = map;
        }

        public void Update(GameTime gameTime)
        {
            updateTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (updateTimer >= UpdateInterval)
            {
                updateTimer = 0f;
                UpdateWaterFlow();
            }
        }

        private void UpdateWaterFlow()
        {
            // We need to iterate over chunks that are currently loaded/active.
            // Ideally we should only update chunks near the player or active chunks.
            if (map.Chunks == null) return;

            // To avoid race conditions where water moves multiple times in one frame or propagates instantly,
            // we'll collect changes and apply them. Or strictly iterate carefully.
            // A simple reliable way for cellular automata is to iterate and track 'moved' status to prevent double moves.
            // But complex fluid sim is expensive. Let's do a simple pass.
            
            // We Iterate chunks.
            // Note: Iterating all chunks might be slow if the map is huge.
            // Optimized approach: Only iterate chunks that *have* water or are near player?
            // For now, let's iterate all loaded chunks.
            
            // Randomize update order? Or Top-down?
            // Top-down is usually better for falling liquids to prevent "teleporting" to bottom in one frame
            // blocked by processing order. Bottom-up is better to let liquid fall into empty space created below it?
            // Actually, if we process bottom-up, a block at Y+1 moves down, creating space at Y+1. 
            // Then block at Y moves to Y+1. This allows a whole column to fall 1 step. Correct.

            // However, chunks are stored in an array. We can just iterate them.
            // Within a chunk we should probably iterate bottom-up.

            foreach (var chunk in map.Chunks)
            {
                if (chunk == null || chunk.Tiles == null || !chunk.HasWater) continue;
                
                bool foundWater = false;
                
                // Iterating backwards (bottom-up)
                for (int i = chunk.Tiles.Length - 1; i >= 0; i--)
                {
                    var tile = chunk.Tiles[i];
                    if (tile != null && tile.TileId == (int)TileType.Water)
                    {
                        foundWater = true;
                        SimulateWaterTile(tile);
                    }
                }
                
                // If we finished iterating and found no water, we can disable the flag.
                // Note: If water flowed INTO this chunk during this frame, it might be set to true by SetTile.
                // We should be careful not to overwrite it if we didn't check those new tiles.
                // However, since we iterate *chunk* tiles, we see current state.
                // If SetTile updated a tile to water, it's in the array.
                // But we iterate backwards. If we process index 10, and it moves to index 20 (processed earlier?), wait.
                // If we move water FROM 10 to 20. 20 is > 10.
                // Bottom-up iteration (Length-1 to 0).
                // We process 20 first. If 20 is empty, nothing.
                // Then we process 10. Water at 10 moves to 20. 
                // SetTile(20, Water) sets chunk.HasWater = true.
                // SetTile(10, Air).
                // Loop continues.
                // At end, foundWater was true (we saw tile at 10).
                // So we set HasWater = true. Correct.
                
                // What if water at 10 moves to different chunk?
                // SetTile(otherChunk, Water) -> otherChunk.HasWater = true.
                // SetTile(10, Air).
                // If 10 was the ONLY water, foundWater = true (because we saw it).
                // Next frame, we see 10 is Air. foundWater = false. HasWater = false. Correct.
                
                chunk.HasWater = foundWater;
            }
        }

        private void SimulateWaterTile(Tile tile)
        {
            // 1. Try to move down
            var tileBelow = map.GetTileAt(tile.X, tile.Y + 1);
            if (tileBelow != null)
            {
                if (tileBelow.TileId == (int)TileType.Air)
                {
                    // Move water down
                    MoveWater(tile, tileBelow);
                    return;
                }
                // If tile below is water, we don't need to do anything (it's full).
                // Unless we implement pressure/levels. For now, binary water (Full/Empty).
            }

            // 2. If blocked below, try to move sideways (spill)
            // Check Left and Right.
            // Randomize order to avoid bias?
            bool tryLeftFirst = Game.rnd.Next(2) == 0;
            
            if (tryLeftFirst)
            {
                if (TryFlowSide(tile, -1)) return;
                if (TryFlowSide(tile, 1)) return;
            }
            else
            {
                if (TryFlowSide(tile, 1)) return;
                if (TryFlowSide(tile, -1)) return;
            }
        }

        private bool TryFlowSide(Tile sourceTile, int dirX)
        {
            var targetTile = map.GetTileAt(sourceTile.X + dirX, sourceTile.Y);
            if (targetTile != null && targetTile.TileId == (int)TileType.Air)
            {
                //for now we just move water sideways if possible
                //This will need some serious polishing in the future
                MoveWater(sourceTile, targetTile);
                return true;
            }
            return false;
        }

        private void MoveWater(Tile source, Tile target)
        {
            // Set target to Water
            map.SetTile(target, (int)TileType.Water);
            // Set source to Air
            map.SetTile(source, (int)TileType.Air);
        }
    }
}
