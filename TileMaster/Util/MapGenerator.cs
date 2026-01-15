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
            matrice = SpreadTile(matrice, Global.RockLevel + 5, 0.01F, 4, 1, 10);
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

        /// <summary>
        /// randomly spread a tile to the map
        /// </summary>
        /// <param name="matrice"></param>
        /// <param name="startLayer"></param>
        /// <param name="percentage"></param>
        /// <param name="tileId"></param>
        /// <param name="minSize"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
        public static int[,] SpreadTile(int[,] matrice, int startLayer, float percentage, int tileId, int minSize, int maxSize)
        {
            var size = Game.rnd.Next(minSize, maxSize);
            for (int x = 0; x < matrice.GetLength(0); x++)
            {
                for (int yy = startLayer; yy < matrice.GetLength(1); yy++)
                {
                    //make sure to replace solid tiles only
                    if (matrice[x, yy] > 0)
                    {
                        matrice[x, yy] = CoinFlipper(percentage, matrice[x, yy], tileId);
                        for (int i = 0; i < size; i++)
                        {
                            var randN = GetRandomNeighborBlock(x, yy);
                            if (x == 0 || yy == 0)
                            {
                                continue;
                            }
                            if (randN.Item1 <= matrice.GetLength(0) || randN.Item2 <= matrice.GetLength(1))
                            {
                                matrice[randN.Item1, randN.Item2] = CoinFlipper(percentage, matrice[x, yy], tileId);
                            }
                            else
                            {
                                //out of bounds
                                break;
                            }

                        }
                    }
                }
            }

            return matrice;
        }

        public static int CoinFlipper(float probability, int currentTileId, int tileId)
        {
            int perCent = Game.rnd.Next(0, 100);
            if (perCent < probability)
            {
                return tileId;
            }
            return currentTileId;
        }

        private static Tuple<int, int> GetRandomNeighborBlock(int X, int Y)
        {
            return Tuple.Create(Game.rnd.Next(X - 1, X + 1), Game.rnd.Next(Y - 1, Y + 1));
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
