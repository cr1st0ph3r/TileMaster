using System;
using TileMaster.Entity.Enums;
using TileMaster.Helper;

namespace TileMaster.Util
{
    public static class MapGenerator
    {
        public static int[,] GenerateRandomMap(int seed)
        {
            var gameInstance = Game.GetInstance();
            gameInstance._mainPanel.InitializeLoadProgress("Generating primitive map");
            int X = Global.MapWidth;
            int Y = Global.MapHeight;

            //0
            //1 dirt
            //2 rocks

            Random r = new Random(seed);
            int[,] matrice = GenerateInitialArrayMap(X, Y);

            //Create surface terrain discrepancies in height for a more natural look
            gameInstance._mainPanel.InitializeLoadProgress("Generating surface topology");
            //matrice = Noise.Noise.RandomWalkTopSmoothed(matrice, r.Next(100000000), 3, 7, Global.GroundLevel);
            matrice = GenerateSurfaceTopography(matrice, seed, Global.GroundLevel);

            //create caves
            gameInstance._mainPanel.InitializeLoadProgress("Generating caves");
            matrice = Noise.Noise.GenerateCaves(matrice, Global.RockLevel - 5, r.Next(100000000));

            // Generate rock layer with natural transition
            gameInstance._mainPanel.InitializeLoadProgress("Generating rock layer");
            matrice = GenerateRockLayer(matrice, Global.RockLevel);

            //adds granite
            GenerateVeins(matrice, (int)TileType.Granite, Global.RockLevel + 5,veinCount: 20,veinLength: 5, 2, 4);
            //plant gras on surface
            gameInstance._mainPanel.InitializeLoadProgress("Planting grass");
            matrice = plantGrass(matrice);

            ImageHelper.SaveMatrixAsImage(matrice, "initial_map.png");

            gameInstance._mainPanel.HideLoadProgress();
            return matrice;
        }

        public static int[,] GenerateBackgroundMap(int seed)
        {
            int X = Global.MapWidth;
            int Y = Global.MapHeight;
            
            // Background starts same as foreground initial state
            int[,] matrice = GenerateInitialArrayMap(X, Y);

            // Apply EXACT same surface topography so sky/ground match
            matrice = GenerateSurfaceTopography(matrice, seed, Global.GroundLevel);

            // Do NOT generate caves or ores. We want solid walls behind deep ground.
            
            // Identify where there is "Sky" vs "Ground"
            // Since GenerateSurfaceTopography sets Air and Dirt, we can trust it.
            // But we might want to change the "Dirt" to a generic "DirtWall" or "StoneWall" later.
            // For now, MapManager.ToChunks converts whatever ID is here into a BackgroundTile.
            // If we leave it as Dirt (1), it becomes Dirt Wall. 
            // If we leave it as Air (0), it becomes Air (No Wall).
            
            // Optimization: Maybe convert deep nodes to StoneWall (if that exists) or just keep DirtWall everywhere?
            // Existing logic in MapManager: `var bgTile = BackgroundMapDictionary[globalId].ToBackgroundTile();`
            // It takes the TileType properties.
            
            // Let's just return the map with surface cuts.
            return matrice;
        }
        private static int[,] GenerateSurfaceTopography(int[,] map, int seed, int groundLevel)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            System.Random r = new System.Random(seed);
            
            // Base Terrain Parameters
            float baseFreq = 0.02f;
            float baseAmp = 10.0f;

            // Mountain Parameters
            float mtnFreq = 0.005f; 
            float mtnAmp = 60.0f; // High peaks

            // Valley Parameters
            float valleyFreq = 0.008f;
            float valleyAmp = 30.0f;

            // Generate noise maps
            // We use different seeds (offsets) for each feature to make them independent
            
            for (int x = 0; x < width; x++)
            {
                // 1. Base Rolling Hills
                float noiseVal = Noise.Noise.PerlinNoise1D(x * baseFreq, seed, 4); 
                float elevation = (noiseVal - 0.5f) * baseAmp; // +/- baseAmp/2 blocks

                // 2. Mountains (large positive features)
                float mtnVal = Noise.Noise.PerlinNoise1D(x * mtnFreq + 100, seed, 4);
                // Sharp peaks: only apply if above threshold
                if (mtnVal > 0.6f) 
                {
                    float mtnHeight = (mtnVal - 0.6f) * 2.5f; // Normalized 0..1 above threshold
                    elevation -= mtnHeight * mtnAmp; // Subtracting Y raises the terrain (Y=0 is top)
                }

                // 3. Valleys (dips)
                float valleyVal = Noise.Noise.PerlinNoise1D(x * valleyFreq + 200, seed, 4);
                 if (valleyVal > 0.6f)
                {
                    float valleyDepth = (valleyVal - 0.6f) * 2.5f;
                    elevation += valleyDepth * valleyAmp; // Adding Y lowers the terrain
                }
                
                // 4. Local Roughness
                 float rough = Noise.Noise.PerlinNoise1D(x * 0.1f, seed, 2);
                 elevation += (rough - 0.5f) * 3;

                int surfaceY = groundLevel + (int)elevation;

                // Clamp
                surfaceY = Math.Clamp(surfaceY, 10, height - 10);

                // Apply to column
                for (int y = 0; y < height; y++)
                {
                    if (y < surfaceY)
                    {
                        map[x, y] = (int)TileType.Air;
                    }
                    else if (y >= surfaceY)
                    {
                         // Maintain lower layers if they were already generated? 
                         // GenerateInitialArrayMap fills everything below GroundLevel with dirt.
                         // But we want to overwrite "Air" that might be there if we raised the land (made surfaceY smaller).
                         // And overwrite "Dirt" with "Air" if we lowered the land (made surfaceY bigger).
                         
                         // Note: We haven't generated caves/rocks yet (those are later steps in GenerateRandomMap).
                         // BUT GenerateInitialArrayMap sets Dirt/Air split at GroundLevel.
                         
                         // So we just set Dirt from surfaceY downwards.
                         // However, if we go purely by surfaceY, we might overwrite Deep stone?
                         // GenerateRandomMap order:
                         // 1. Initial (Dirt below GroundLevel)
                         // 2. Surface Topology (THIS STEP)
                         // 3. Caves
                         // 4. Rocks
                         
                         // So at this stage, it is just Dirt and Air.
                         map[x, y] = (int)TileType.Dirt;
                    }
                }
            }
            return map;
        }

        private static int[,] plantGrass(int[,] matrice)
        {
            int grassRange = 5;
            for (int x = 0; x < matrice.GetLength(0); x++)
            {
                for (int y = Global.GroundLevel - grassRange; y < Global.GroundLevel + grassRange; y++)
                {

                    //check if the block is dirt
                    if (matrice[x, y] == (int)TileType.Dirt)
                    {
                        //check if the tile has air above it
                        if (matrice[x, y - 1] == (int)TileType.Air)
                        {
                            matrice[x, y] = (int)TileType.DirtWithGrass;
                        }

                    }
                }
            }

            return matrice;
        }

        private static int[,] GenerateRockLayer(int[,] matrice, int baseLevel)
        {
            int width = matrice.GetLength(0);
            int height = matrice.GetLength(1);
            Random r = new Random();

            // Generate a noise profile for the rock boundary
            // We use a simple 1D value noise approach
            int sampleRate = 16; // Sample noise every 16 blocks
            float[] noiseSamples = new float[(width / sampleRate) + 2];

            for (int i = 0; i < noiseSamples.Length; i++)
            {
                noiseSamples[i] = (float)(r.NextDouble() * 2.0 - 1.0); // Range -1 to 1
            }

            for (int x = 0; x < width; x++)
            {
                // Interpolate noise
                int sampleIndex = x / sampleRate;
                float t = (float)(x % sampleRate) / sampleRate;

                // Smoothstep interpolation: t * t * (3 - 2 * t)
                float smoothT = t * t * (3 - 2 * t);
                float noiseVal = noiseSamples[sampleIndex] * (1 - smoothT) + noiseSamples[sampleIndex + 1] * smoothT;

                // Calculate the transition height for this column (Amplitude of 6 blocks)
                int transitionY = baseLevel + (int)(noiseVal * 6);

                for (int y = 0; y < height; y++)
                {
                    // Skip air (caves, surface)
                    if (matrice[x, y] == (int)TileType.Air) continue;

                    // Determine if this should be stone based on depth relative to transitionY
                    // We create a dithering zone of +/- 3 blocks around the transitionY
                    if (y > transitionY + 3)
                    {
                        matrice[x, y] = (int)TileType.Stone;
                    }
                    else if (y >= transitionY - 3)
                    {
                        // In the transition zone, blend based on probability
                        float depthInZone = y - (transitionY - 3);
                        float probability = depthInZone / 6.0f; // 0.0 to 1.0

                        if (r.NextDouble() < probability)
                        {
                            matrice[x, y] = (int)TileType.Stone;
                        }
                    }
                }
            }
            return matrice;
        }

        public static int[,] GenerateVeins(int[,] matrice, int tileId, int startLayer, int veinCount, int veinLength, int minRadius, int maxRadius)
        {
            int width = matrice.GetLength(0);
            int height = matrice.GetLength(1);

            for (int i = 0; i < veinCount; i++)
            {
                float currentX = Game.rnd.Next(10, width - 10);
                float currentY = Game.rnd.Next(startLayer + 10, height - 10);
                float angle = (float)(Game.rnd.NextDouble() * Math.PI * 2);

                int radius = Game.rnd.Next(minRadius, maxRadius + 1);

                for (int step = 0; step < veinLength; step++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        for (int y = -radius; y <= radius; y++)
                        {
                            if (x * x + y * y <= radius * radius)
                            {
                                int carveX = (int)currentX + x;
                                int carveY = (int)currentY + y;

                                if (carveX > 0 && carveX < width - 1 && carveY > startLayer && carveY < height - 1)
                                {
                                    //don't replace air
                                    if (matrice[carveX, carveY] != (int)TileType.Air)
                                    {
                                        matrice[carveX, carveY] = tileId;
                                    }
                                }
                            }
                        }
                    }
                    angle += (float)(Game.rnd.NextDouble() * 1.0 - 0.5);
                    currentX += (float)Math.Cos(angle);
                    currentY += (float)Math.Sin(angle);

                    if (currentX <= 1 || currentX >= width - 2 || currentY <= startLayer + 1 || currentY >= height - 2)
                        break;
                }
            }
            return matrice;
        }

        public static int[,] GenerateInitialArrayMap(int x, int y)
        {
            //000000000000000000
            //000000000000000000
            //111111111111111111
            //111111111111111111
            //111111111111111111
            //111111111111111111
            int[,] matrice = new int[x, y];


            for (int xx = 0; xx < matrice.GetLength(0); xx++)
            {
                for (int yy = 0; yy < matrice.GetLength(1); yy++)
                {
                    if (yy > Global.GroundLevel)
                    {
                        matrice[xx, yy] = (int)TileType.Dirt;
                    }
                    else
                    {
                        matrice[xx, yy] = (int)TileType.Air;
                    }

                }
            }

            return matrice;
        }
    }
}
