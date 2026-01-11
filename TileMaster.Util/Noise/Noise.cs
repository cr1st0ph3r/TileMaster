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

        public static int[,] GenerateCaves(int[,] map, int startingDepth, float seed, int fillPercent, bool edgesAreWalls, int smoothCount)
        {
            //map = GenerateCellularAutomata(map, startingDepth, seed, fillPercent, edgesAreWalls);
            //map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls, smoothCount);
           


            //// Use the Walker instead of the random noise generator
            //map = GenerateDrunkardWalk(map, startingDepth, seed, fillPercent);
            //// Optional: Smooth it once or twice to make the tunnels look less "blocky"
            //map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls, smoothCount);



            // 1. Create the winding tunnels
            // Parameters: map, depth, seed, number of worms, length of each worm
            //map = GeneratePerlinWorm(map, startingDepth, seed, 15, 200);
            // 2. Smooth them out using your existing method
            // This removes the "blocky" look from the circles
           // map = SmoothMooreCellularAutomata(map, startingDepth, true, 2);


            map = GenerateSwissCheeseCaves(map, startingDepth, seed);

            return map;
        }
        public static int[,] GenerateCellularAutomata(int[,] map, int startingDepth, float seed, int fillPercent, bool edgesAreWalls)
        {
            // Seed our random number generator
            System.Random rand = new System.Random(seed.GetHashCode());

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Initialise the map
            for (int x = 0; x < width; x++)
            {
                for (int y = startingDepth; y < height; y++)
                {
                    // If we have the edges set to be walls, ensure the cell is set to on (1)
                    if (edgesAreWalls && (x == 0 || x == width - 1 || y == 0 || y == height - 1))
                    {
                        map[x, y] = 1;
                    }
                    else
                    {
                        // Randomly generate the grid
                        map[x, y] = (rand.Next(0, 100) < fillPercent) ? 1 : 0;
                    }
                }
            }
            return map;
        }    
        public static int[,] SmoothMooreCellularAutomata(int[,] map, int startingDepth, bool edgesAreWalls, int smoothCount)
        {
            for (int i = 0; i < smoothCount; i++)
            {
                for (int x = 0; x < map.GetUpperBound(0); x++)
                {
                    for (int y = startingDepth; y < map.GetUpperBound(1); y++)
                    {
                        int surroundingTiles = GetMooreSurroundingTiles(map, x, y, edgesAreWalls);

                        if (edgesAreWalls && (x == 0 || x == (map.GetUpperBound(0) - 1) || y == 0 || y == (map.GetUpperBound(1) - 1)))
                        {
                            //Set the edge to be a wall if we have edgesAreWalls to be true
                            map[x, y] = 1;
                        }
                        //The default moore rule requires more than 4 neighbours
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
            //Return the modified map
            return map;
        }
        static int GetMooreSurroundingTiles(int[,] map, int x, int y, bool edgesAreWalls)
        {
            /* Moore Neighbourhood looks like this ('T' is our tile, 'N' is our neighbours)
             *
             * N N N
             * N T N
             * N N N
             *
             */

            int tileCount = 0;

            for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
            {
                for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
                {
                    if (neighbourX >= 0 && neighbourX < map.GetUpperBound(0) && neighbourY >= 0 && neighbourY < map.GetUpperBound(1))
                    {
                        //We don't want to count the tile we are checking the surroundings of
                        if (neighbourX != x || neighbourY != y)
                        {
                            tileCount += map[neighbourX, neighbourY];
                        }
                    }
                }
            }
            return tileCount;
        }

        public static int[,] GenerateDrunkardWalk(int[,] map, int startingDepth, float seed, int targetFloorPercent)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // 1. Fill the entire map with walls (1) initially
            for (int x = 0; x < width; x++)
                for (int y = startingDepth; y < height; y++)
                    map[x, y] = 1;

            // 2. Setup our walker
            int startX = width / 2;
            int startY = (height + startingDepth) / 2;
            int currentX = startX;
            int currentY = startY;

            int totalTiles = width * (height - startingDepth);
            int desiredFloorTiles = (int)(totalTiles * (targetFloorPercent / 100f));
            int floorCount = 0;

            // 3. Walk until we've carved enough floor
            while (floorCount < desiredFloorTiles)
            {
                // If this is a new floor tile, count it
                if (map[currentX, currentY] == 1)
                {
                    map[currentX, currentY] = 0;
                    floorCount++;
                }

                // Pick a random direction (0: North, 1: South, 2: East, 3: West)
                int dir = rand.Next(0, 4);
                int nextX = currentX;
                int nextY = currentY;

                if (dir == 0) nextY++;
                else if (dir == 1) nextY--;
                else if (dir == 2) nextX++;
                else if (dir == 3) nextX--;

                // Stay within bounds (and respect the startingDepth)
                if (nextX > 0 && nextX < width - 1 && nextY > startingDepth && nextY < height - 1)
                {
                    currentX = nextX;
                    currentY = nextY;
                }
            }

            return map;
        }

        public static int[,] GeneratePerlinWorm(int[,] map, int startingDepth, float seed, int wormCount, int wormLength)
        {
            System.Random rand = new System.Random(seed.GetHashCode());
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Initialise the map to walls (1)
            for (int x = 0; x < width; x++)
                for (int y = startingDepth; y < height; y++)
                    map[x, y] = 1;

            for (int i = 0; i < wormCount; i++)
            {
                // Random start position
                float currentX = rand.Next(10, width - 10);
                float currentY = rand.Next(startingDepth + 10, height - 10);

                // Random starting angle (in radians)
                float angle = (float)(rand.NextDouble() * Math.PI * 2);

                // Random thickness for this specific worm
                int radius = rand.Next(1, 4);

                for (int step = 0; step < wormLength; step++)
                {
                    // 1. Carve the current position
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            // Check if the tile is within the circular radius
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

                    // 2. "Steer" the worm: nudge the angle slightly
                    // Using a small change creates smooth curves; a large change makes it erratic
                    angle += (float)(rand.NextDouble() * 1.0 - 0.5);

                    // 3. Move the worm forward based on the angle
                    currentX += (float)Math.Cos(angle);
                    currentY += (float)Math.Sin(angle);

                    // 4. Boundary check - if it hits an edge, kill this worm early
                    if (currentX <= 1 || currentX >= width - 2 || currentY <= startingDepth + 1 || currentY >= height - 2)
                        break;
                }
            }
            return map;
        }

        public static int[,] GenerateSwissCheeseCaves(int[,] map, int startingDepth, float seed)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // --- PASS 1: The Backbone (Perlin Worms) ---
            // We start by carving the main pathing so we guarantee connectivity.
            map = GeneratePerlinWorm(map, startingDepth, seed, wormCount: 12, wormLength: 150);

            // --- PASS 2: The Erosion (Modified Cellular Automata) ---
            // We use a random number generator similar to your GenerateCellularAutomata logic.
            System.Random rand = new System.Random(seed.GetHashCode());

            // We iterate through the map, but we only turn walls into air (0) 
            // at a very low frequency to create "pockets."
            int pocketProbability = 5; // Low percentage to avoid destroying the structure

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = startingDepth + 1; y < height - 1; y++)
                {
                    // If it's currently a wall, there's a small chance to become a pocket
                    if (map[x, y] == 1 && rand.Next(0, 100) < pocketProbability)
                    {
                        map[x, y] = 0;
                    }
                }
            }

            // --- PASS 3: The Polishing (Your Original Smoothing) ---
            // Now we use your SmoothMooreCellularAutomata to blend the worms 
            // and the new pockets together.
            // 3 passes is usually the "sweet spot" for organic looks.
            map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls: true, smoothCount: 3);

            return map;
        }
    }
}
