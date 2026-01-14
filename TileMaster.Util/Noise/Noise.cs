namespace TileMaster.Util.Noise
{

    public class Noise
    {
        public static int[,] RandomWalkTopSmoothed(int[,] map, float seed, int minSectionWidth, int maxSectionWidth, int groundLevel)
        {
            // Seed our random
            System.Random rand = new System.Random(seed.GetHashCode());

            // Determine the start position
            int lastHeight = Math.Clamp(groundLevel - 1, 0, map.GetLength(1) - 1);

            // Used to determine which direction to go
            int nextMove = 0;
            // Used to keep track of the current section's width
            int sectionWidth = 0;

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Work through the array width (x axis)
            for (int x = 0; x < width; x++)
            {
                var sectionWidthRef = rand.Next(minSectionWidth, maxSectionWidth);
                // Determine the next move
                nextMove = rand.Next(2);

                // Only change the height if we have used the current height more than the minimum required section width
                if (nextMove == 0 && lastHeight > 0 && sectionWidth > sectionWidthRef)
                {
                    lastHeight = Math.Max(0, lastHeight - 1);
                    sectionWidth = 0;
                }
                else if (nextMove == 1 && lastHeight < height - 1 && sectionWidth > sectionWidthRef)
                {
                    lastHeight = Math.Min(height - 1, lastHeight + 1);
                    sectionWidth = 0;
                }
                // Increment the section width
                sectionWidth++;

                // Work our way from the height down to 0 and mark as "empty" (0)
                for (int y = lastHeight; y >= 0; y--)
                {
                    map[x, y] = 0;
                }
            }

            // Return the modified map
            return map;
        }

        /// <summary>
        /// The main orchestrator for cave generation using a layered step approach.
        /// </summary>
        public static int[,] GenerateCaves(int[,] map, int startingDepth, float seed)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            var MapSize = ((width * height)/1000)/2;
            Random mainRand = new Random(seed.GetHashCode());

            // --- STEP 1: Initialization ---
            // Fill the cave area entirely with walls (1) to start fresh.
            for (int x = 0; x < width; x++)
                for (int y = startingDepth; y < height; y++)
                    map[x, y] = 1;

            // --- STEP 2: The Backbone (Perlin Worms) ---
            // Ensures long-distance connectivity across the map.
            // Reduced count slightly as other features will add open space.
            map = GeneratePerlinWorm(map, startingDepth, seed, wormCount: MapSize/2, wormLength: 100, minRadius: 1, maxRadius: 3);

            // --- STEP 3: Large Oval Chambers ---
            // Adds distinct large rooms.
            map = GenerateOvalChambers(map, startingDepth, seed + 1, chamberCount: MapSize/5, minRadius: 5, maxRadius: 12);

            // --- STEP 4: Vertical Ravines ---
            // Adds tall, narrow vertical drops.
            map = GenerateLinearFeature(map, startingDepth, seed + 2, count: MapSize / 5, minLength: 40, maxLength: 80, avgThickness: 3, vertical: true);

            // --- STEP 5: Horizontal Tunnels ---
            // Adds wider, mostly straight horizontal passages.
            map = GenerateLinearFeature(map, startingDepth, seed + 3, count: MapSize / 5, minLength: 50, maxLength: 100, avgThickness: 4, vertical: false);

            // --- STEP 6: Erosion (Swiss Cheese "Cruft") ---
            // Adds small random noise pockets to break up solid walls.
            map = ApplySwissCheeseErosion(map, startingDepth, seed + 4, pocketProbability: 3);

            // --- STEP 7: The Polishing (Smoothing) ---
            // Blend all the distinct features together organically.
            // 3-4 passes is usually good.
            map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls: true, smoothCount: 4);

            return map;
        }


        #region Feature Generators

        static int[,] GenerateOvalChambers(int[,] map, int startingDepth, float seed, int chamberCount, int minRadius, int maxRadius)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            for (int i = 0; i < chamberCount; i++)
            {
                int centerX = rand.Next(maxRadius + 1, width - maxRadius - 1);
                // Ensure chambers don't spawn too close to the surface layer
                int centerY = rand.Next(startingDepth + maxRadius + 5, height - maxRadius - 1);

                int radiusX = rand.Next(minRadius, maxRadius);
                // Make Y radius slightly smaller on average for flattened "room" shapes
                int radiusY = rand.Next(minRadius, (int)(maxRadius * 0.8f));

                // Iterate over the bounding box of the ellipse
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                    {
                        // Standard ellipse equation check: (x-h)^2/a^2 + (y-k)^2/b^2 <= 1
                        // Using doubles for precision during calculation
                        double normalizedX = Math.Pow(x - centerX, 2) / Math.Pow(radiusX, 2);
                        double normalizedY = Math.Pow(y - centerY, 2) / Math.Pow(radiusY, 2);

                        if (normalizedX + normalizedY <= 1.0)
                        {
                            if (x > 0 && x < width - 1 && y > startingDepth && y < height - 1)
                            {
                                map[x, y] = 0; // Carve air
                            }
                        }
                    }
                }
            }
            return map;
        }

        static int[,] GenerateLinearFeature(int[,] map, int startingDepth, float seed, int count, int minLength, int maxLength, int avgThickness, bool vertical)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            for (int i = 0; i < count; i++)
            {
                int length = rand.Next(minLength, maxLength);
                int currentThickness = avgThickness;

                // Start positions depend on direction
                float currentX = vertical ? rand.Next(10, width - 10) : rand.Next(10, width - length - 10);
                float currentY = vertical ? rand.Next(startingDepth + 10, height - length - 10) : rand.Next(startingDepth + 10, height - 10);

                for (int step = 0; step < length; step++)
                {
                    // Vary thickness slightly for organic feel
                    currentThickness = Math.Clamp(currentThickness + rand.Next(-1, 2), avgThickness - 1, avgThickness + 2);
                    int radius = currentThickness / 2;

                    // Carve a cross-section
                    for (int t = -radius; t <= radius; t++)
                    {
                        int carveX = (int)currentX + (vertical ? t : 0);
                        int carveY = (int)currentY + (vertical ? 0 : t);

                        if (carveX > 0 && carveX < width - 1 && carveY > startingDepth && carveY < height - 1)
                        {
                            map[carveX, carveY] = 0;
                        }
                    }

                    // Move forward
                    if (vertical) currentY++; else currentX++;

                    // Add slight "wobble" perpendicular to movement
                    float drift = (float)(rand.NextDouble() * 1.0 - 0.5);
                    if (vertical) currentX += drift; else currentY += drift;

                    // Boundary check to stop early
                    if (currentX <= 1 || currentX >= width - 2 || currentY <= startingDepth + 1 || currentY >= height - 2)
                        break;
                }
            }
            return map;
        }
    
        static int[,] GeneratePerlinWorm(int[,] map, int startingDepth, float seed, int wormCount, int wormLength, int minRadius, int maxRadius)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Initialization removed here, handled in main orchestrator

            for (int i = 0; i < wormCount; i++)
            {
                float currentX = rand.Next(10, width - 10);
                float currentY = rand.Next(startingDepth + 10, height - 10);
                float angle = (float)(rand.NextDouble() * Math.PI * 2);

                // Use parameters for radius
                int radius = rand.Next(minRadius, maxRadius + 1);

                for (int step = 0; step < wormLength; step++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            if (x * x + y * y <= radius * radius)
                            {
                                int carveX = (int)currentX + x;
                                int carveY = (int)currentY + y;

                                if (carveX > 0 && carveX < width - 1 && carveY > startingDepth && carveY < height - 1)
                                {
                                    map[carveX, carveY] = 0;
                                }
                            }
                        }
                    }
                    angle += (float)(rand.NextDouble() * 1.0 - 0.5);
                    currentX += (float)Math.Cos(angle);
                    currentY += (float)Math.Sin(angle);

                    if (currentX <= 1 || currentX >= width - 2 || currentY <= startingDepth + 1 || currentY >= height - 2)
                        break;
                }
            }
            return map;
        }

        static int[,] ApplySwissCheeseErosion(int[,] map, int startingDepth, float seed, int pocketProbability)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = startingDepth + 1; y < height - 1; y++)
                {
                    if (map[x, y] == 1 && rand.Next(0, 100) < pocketProbability)
                    {
                        map[x, y] = 0;
                    }
                }
            }
            return map;
        }

        static int[,] SmoothMooreCellularAutomata(int[,] map, int startingDepth, bool edgesAreWalls, int smoothCount)
        {
            // (This method remains exactly the same as in your original provided code)
            for (int i = 0; i < smoothCount; i++)
            {
                for (int x = 0; x < map.GetUpperBound(0); x++)
                {
                    for (int y = startingDepth; y < map.GetUpperBound(1); y++)
                    {
                        int surroundingTiles = GetMooreSurroundingTiles(map, x, y, edgesAreWalls);

                        if (edgesAreWalls && (x == 0 || x == (map.GetUpperBound(0) - 1) || y == 0 || y == (map.GetUpperBound(1) - 1)))
                        {
                            map[x, y] = 1;
                        }
                        else if (surroundingTiles > 4)
                        {
                            map[x, y] = 1;
                        }
                        else if (surroundingTiles < 4)
                        {
                            map[x, y] = 0;
                        }
                    }
                }
            }
            return map;
        }

        static int GetMooreSurroundingTiles(int[,] map, int x, int y, bool edgesAreWalls)
        {
            int tileCount = 0;
            for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
            {
                for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
                {
                    if (neighbourX >= 0 && neighbourX < map.GetUpperBound(0) && neighbourY >= 0 && neighbourY < map.GetUpperBound(1))
                    {
                        if (neighbourX != x || neighbourY != y)
                        {
                            tileCount += map[neighbourX, neighbourY];
                        }
                    }
                }
            }
            return tileCount;
        }
        #endregion
    }
}
