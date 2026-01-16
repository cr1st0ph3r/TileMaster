using System;
using TileMaster.Entity.Enums;
using TileMaster.Helper;

namespace TileMaster.Util
{
    public static class MapGenerator
    {
        public static int[,] GenerateRandomMap()
        {
            var gameInstance = Game.GetInstance();
            gameInstance._mainPanel.InitializeLoadProgress("Generating primitive map");
            int X = Global.MapWidth;
            int Y = Global.MapHeight;

            //0
            //1 dirt
            //2 rocks

            Random r = new Random();
            int[,] matrice = GenerateInitialArrayMap(X, Y);

            //Create surface terrain discrepancies in height for a more natural look
            gameInstance._mainPanel.InitializeLoadProgress("Generating surface topology");
            matrice = Noise.Noise.RandomWalkTopSmoothed(matrice, r.Next(100000000), 3, 7, Global.GroundLevel);

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
