using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TileMaster.Entity.Enums;

namespace TileMaster.Manager
{
    public class TileShadeManager
    {
        private Map.Map map;

        public TileShadeManager(Map.Map map)
        {
            this.map = map;
        }
        // Global light map: [x, y] -> light level (Vector3 for RGB)
        private Vector3[,] lightMap;
        // Background light map: [x, y] -> light level (Vector3 for RGB) - only from artificial sources, with extended range
        private Vector3[,] backgroundLightMap;

        // Flag to indicate if a lighting update is currently in progress
        public bool IsUpdating { get; private set; }

        public void UpdateLightingAsync(GameTime gameTime, Layer currentLayer, Point? center = null, int chunkRadius = 4)
        {
            if (IsUpdating) return;

            IsUpdating = true;
            
            // We capture necessary state to avoid thread issues if properties change
            // gameTime.TotalGameTime is safe to read as it's a struct copy
            double totalSeconds = gameTime.TotalGameTime.TotalSeconds;

            Task.Run(() =>
            {
                try
                {
                    UpdateLightingInternal(totalSeconds, currentLayer, center, chunkRadius);
                }
                finally
                {
                    IsUpdating = false;
                }
            });
        }

        private void UpdateLightingInternal(double totalSeconds, Layer currentLayer, Point? center = null, int chunkRadius = 4)
        {
            int width = Global.MapWidth;
            int height = Global.MapHeight;

            // Initialize light maps if needed or resize
            if (lightMap == null || lightMap.GetLength(0) != width || lightMap.GetLength(1) != height)
            {
                lightMap = new Vector3[width, height];
            }
            if (backgroundLightMap == null || backgroundLightMap.GetLength(0) != width || backgroundLightMap.GetLength(1) != height)
            {
                backgroundLightMap = new Vector3[width, height];
            }

            // Determine bounds
            int startX = 0;
            int endX = width;

            if (center.HasValue)
            {
                // Calculate bounds based on chunk radius
                int radiusInTiles = chunkRadius * Global.ChunkSize;
                startX = Math.Max(0, center.Value.X - radiusInTiles);
                endX = Math.Min(width, center.Value.X + radiusInTiles);
            }
            else
            {
                // If no center is provided, clear everything.
                Array.Clear(lightMap, 0, lightMap.Length);
                Array.Clear(backgroundLightMap, 0, backgroundLightMap.Length);
            }

            if (center.HasValue)
            {
                // Clear only the affected vertical strip
                for (int x = startX; x < endX; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        lightMap[x, y] = Vector3.Zero;
                        backgroundLightMap[x, y] = Vector3.Zero;
                    }
                }
            }

            // Queues for light propagation
            var lightQueue = new Queue<Point>();
            var backgroundLightQueue = new Queue<Point>();

            // 1. Sunlight Pass (Vertical Rays)
            for (int x = startX; x < endX; x++)
            {
                float currentIntensity = 1.0f;
                // Sunlight comes from top (y=0) down
                for (int y = 0; y < height; y++)
                {
                    var tile = map.GetTileAt(x, y);

                    // Sunlight is white, intensity reduces when passing through solid tiles
                    lightMap[x, y] = new Vector3(currentIntensity);
                    lightQueue.Enqueue(new Point(x, y));

                    // Background light is also affected by sunlight
                    backgroundLightMap[x, y] = new Vector3(currentIntensity);
                    backgroundLightQueue.Enqueue(new Point(x, y));

                    // If tile is solid, sunlight starts to fade
                    if (tile != null && tile.IsSolid)
                    {
                        currentIntensity -= 0.25f; 
                    }

                    // Stop when light is completely gone
                    if (currentIntensity <= 0f)
                    {
                        break;
                    }
                }
            }

            // 1.2. Background Holes Pass (Only in Surface Layer)
            // RESTORED: This pass identifies "holes" in the background which let sky light through 
            // as radial light sources when the player is on the surface.
            if (currentLayer == Layer.Surface)
            {
                for (int x = startX; x < endX; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        // A "hole" is where there is NO background tile
                        var bgTile = map.GetBackgroundTileAt(x, y);

                        // If there's no background tile, it's a hole to the sky
                        if (bgTile == null || bgTile.TileId == 0)
                        {
                            // Background holes act as white light sources
                            Vector3 holeIntensity = Vector3.One;

                            // Seed light maps if this is brighter than current light
                            if (holeIntensity.X > lightMap[x, y].X)
                            {
                                lightMap[x, y] = holeIntensity;
                                lightQueue.Enqueue(new Point(x, y));
                            }

                            if (holeIntensity.X > backgroundLightMap[x, y].X)
                            {
                                backgroundLightMap[x, y] = holeIntensity;
                                backgroundLightQueue.Enqueue(new Point(x, y));
                            }
                        }
                    }
                }
            }

            // 1.5. Item Light Sources Pass
            if (map.Chunks != null)
            {
                int startChunkX = startX / Global.ChunkSize;
                int endChunkX = (endX - 1) / Global.ChunkSize;
                int chunksHeight = Global.MapHeight / Global.ChunkSize;

                for (int cx = startChunkX; cx <= endChunkX; cx++)
                {
                    for (int cy = 0; cy < chunksHeight; cy++)
                    {
                        int widthInChunks = Global.MapWidth / Global.ChunkSize;
                        int chunkIndex = (cy * widthInChunks) + cx;

                        if (chunkIndex >= 0 && chunkIndex < map.Chunks.Length)
                        {
                            var chunk = map.Chunks[chunkIndex];
                            if (chunk != null && chunk.Tiles != null)
                            {
                                foreach (var tile in chunk.Tiles)
                                {
                                    if (tile == null) continue;
                                    if (tile.X < startX || tile.X >= endX) continue;

                                    if (tile.PlacedItem != null && tile.PlacedItem.IsLightSource)
                                    {
                                        Vector3 color = tile.PlacedItem.LightColor?.ToVector3() ?? Vector3.One;
                                        float lightIntensity = tile.PlacedItem.LightIntensity;

                                        if (tile.PlacedItem.IsFlickeringLight)
                                        {
                                            lightIntensity *= GetFlickerFactor(tile.X, tile.Y, totalSeconds);
                                        }

                                        Vector3 intensity = color * lightIntensity;

                                        bool updated = false;
                                        if (intensity.X > lightMap[tile.X, tile.Y].X) { lightMap[tile.X, tile.Y].X = intensity.X; updated = true; }
                                        if (intensity.Y > lightMap[tile.X, tile.Y].Y) { lightMap[tile.X, tile.Y].Y = intensity.Y; updated = true; }
                                        if (intensity.Z > lightMap[tile.X, tile.Y].Z) { lightMap[tile.X, tile.Y].Z = intensity.Z; updated = true; }

                                        if (updated)
                                        {
                                            lightQueue.Enqueue(new Point(tile.X, tile.Y));
                                        }

                                        // Also seed background light map for artificial sources
                                        bool bgUpdated = false;
                                        if (intensity.X > backgroundLightMap[tile.X, tile.Y].X) { backgroundLightMap[tile.X, tile.Y].X = intensity.X; bgUpdated = true; }
                                        if (intensity.Y > backgroundLightMap[tile.X, tile.Y].Y) { backgroundLightMap[tile.X, tile.Y].Y = intensity.Y; bgUpdated = true; }
                                        if (intensity.Z > backgroundLightMap[tile.X, tile.Y].Z) { backgroundLightMap[tile.X, tile.Y].Z = intensity.Z; bgUpdated = true; }

                                        if (bgUpdated)
                                        {
                                            backgroundLightQueue.Enqueue(new Point(tile.X, tile.Y));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 2. Light Propagation (Flood Fill with Decay)
            while (lightQueue.Count > 0)
            {
                var p = lightQueue.Dequeue();
                Vector3 currentLight = lightMap[p.X, p.Y];

                if (currentLight.X <= 0.05f && currentLight.Y <= 0.05f && currentLight.Z <= 0.05f) continue;

                var neighbors = new Point[]
                {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X < startX || n.X >= endX || n.Y < 0 || n.Y >= height) continue;

                    var neighborTile = map.GetTileAt(n.X, n.Y);
                    bool isSolid = (neighborTile != null && neighborTile.IsSolid);

                    float decay = isSolid ? Global.LightDecayTiles : 0.1f;
                    Vector3 potentialLight = currentLight - new Vector3(decay);

                    bool updated = false;
                    if (potentialLight.X > lightMap[n.X, n.Y].X) { lightMap[n.X, n.Y].X = potentialLight.X; updated = true; }
                    if (potentialLight.Y > lightMap[n.X, n.Y].Y) { lightMap[n.X, n.Y].Y = potentialLight.Y; updated = true; }
                    if (potentialLight.Z > lightMap[n.X, n.Y].Z) { lightMap[n.X, n.Y].Z = potentialLight.Z; updated = true; }

                    if (updated && !isSolid)
                    {
                        lightQueue.Enqueue(n);
                    }
                }
            }

            // 2.5. Background Light Propagation (Extended Range - 3x)
            while (backgroundLightQueue.Count > 0)
            {
                var p = backgroundLightQueue.Dequeue();
                Vector3 currentLight = backgroundLightMap[p.X, p.Y];

                if (currentLight.X <= 0.02f && currentLight.Y <= 0.02f && currentLight.Z <= 0.02f) continue;

                var neighbors = new Point[]
                {
                    new Point(p.X + 1, p.Y),
                    new Point(p.X - 1, p.Y),
                    new Point(p.X, p.Y + 1),
                    new Point(p.X, p.Y - 1)
                };

                foreach (var n in neighbors)
                {
                    if (n.X < startX || n.X >= endX || n.Y < 0 || n.Y >= height) continue;

                    var neighborTile = map.GetTileAt(n.X, n.Y);
                    bool isSolid = (neighborTile != null && neighborTile.IsSolid);

                    float decay = isSolid ? Global.LightDecayOnBackground : 0.033f;
                    Vector3 potentialLight = currentLight - new Vector3(decay);

                    bool updated = false;
                    if (potentialLight.X > backgroundLightMap[n.X, n.Y].X) { backgroundLightMap[n.X, n.Y].X = potentialLight.X; updated = true; }
                    if (potentialLight.Y > backgroundLightMap[n.X, n.Y].Y) { backgroundLightMap[n.X, n.Y].Y = potentialLight.Y; updated = true; }
                    if (potentialLight.Z > backgroundLightMap[n.X, n.Y].Z) { backgroundLightMap[n.X, n.Y].Z = potentialLight.Z; updated = true; }

                    if (updated)
                    {
                        backgroundLightQueue.Enqueue(n);
                    }
                }
            }

            // 3. Apply Light to Tiles
            int startCX = startX / Global.ChunkSize;
            int endCX = (endX - 1) / Global.ChunkSize;
            int hChunks = Global.MapHeight / Global.ChunkSize;
            int wChunks = Global.MapWidth / Global.ChunkSize;

            for (int cx = startCX; cx <= endCX; cx++)
            {
                for (int cy = 0; cy < hChunks; cy++)
                {
                    int chunkIndex = (cy * wChunks) + cx;

                    if (chunkIndex >= 0 && chunkIndex < map.Chunks.Length)
                    {
                        var chunk = map.Chunks[chunkIndex];
                        if (chunk != null && chunk.Tiles != null)
                        {
                            foreach (var tile in chunk.Tiles)
                            {
                                if (tile == null) continue;
                                if (tile.X < startX || tile.X >= endX) continue;

                                Vector3 l = lightMap[tile.X, tile.Y];
                                // Clamp to [0, 1]
                                l.X = MathHelper.Clamp(l.X, 0, 1);
                                l.Y = MathHelper.Clamp(l.Y, 0, 1);
                                l.Z = MathHelper.Clamp(l.Z, 0, 1);

                                tile.SetColor((byte)(l.X * 255), (byte)(l.Y * 255), (byte)(l.Z * 255), 255);
                            }

                            if (chunk.BackgroundTiles != null)
                            {
                                foreach (var bgTile in chunk.BackgroundTiles)
                                {
                                    if (bgTile == null) continue;
                                    if (bgTile.X < startX || bgTile.X >= endX) continue;

                                    Vector3 bgLight = backgroundLightMap[bgTile.X, bgTile.Y];
                                    // Dimmed version for background
                                    bgLight *= 0.7f;

                                    bgLight.X = MathHelper.Clamp(bgLight.X, 0, 1);
                                    bgLight.Y = MathHelper.Clamp(bgLight.Y, 0, 1);
                                    bgLight.Z = MathHelper.Clamp(bgLight.Z, 0, 1);

                                    bgTile.SetColor((byte)(bgLight.X * 255), (byte)(bgLight.Y * 255), (byte)(bgLight.Z * 255), 255);
                                }
                            }

                            chunk.NeedUpdate = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a pseudo-random flicker factor based on position and time.
        /// </summary>
        private float GetFlickerFactor(int x, int y, double time)
        {
            // Use sin waves with different frequencies and offsets based on position
            // to create a desynchronized natural flicker.
            float baseFlicker = (float)(Math.Sin(time * 7.5 + (x * 0.25) + (y * 0.15)) * 0.05);
            float noise = (float)(Math.Sin(time * 17.5 + (x * 6.15) + (y * 3.55)) * 0.02);

            // Result is roughly between 0.93 and 1.07
            return 1.0f + baseFlicker + noise;
        }
    }
}