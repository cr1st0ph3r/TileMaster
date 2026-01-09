using System;
using System.Drawing.Imaging;
using TileMaster.Entity.Enums;
using TileMaster.Helper;

namespace TileMaster.Util
{
    public static class MapGenerator
    {
        public static int[,] GenerateRandomMap()
        {
            int X = Global.MapWidth;
            int Y = Global.MapHeight;

            //0
            //1 dirt
            //2 rocks

            Random r = new Random();
            int[,] matrice = GenerateInitialArrayMap(X, Y);


            //cave generator

            //random cave
            //matrice = Noise.RandomWalkCave(matrice, r.Next(100000000), 10);
            //directional tunnels
            //matrice = Noise.DirectionalTunnel(matrice, 5, 5,50,15,10);
            //cellular automata 

            //Create surface terrain discrepancies in height for a more natural look
            matrice = Noise.Noise.RandomWalkTopSmoothed(matrice, r.Next(100000000), 3, 7, Global.GroundLevel);


            //create caves
            matrice = Noise.Noise.GenerateCaves(matrice, Global.RockLevel - 5, r.Next(100000000), 50, true, 10);



            //set layer to rock after certain depth
            matrice = setTilesAfterLayer(matrice, Global.RockLevel, 2);
            //adds granite
            matrice = SpreadTile(matrice, Global.RockLevel + 5, 0.01F, 4, 1, 10);
            //layer blending
            matrice = randomizeLayer(matrice, (Global.RockLevel - 2), new int[4] { 1, 2, 1, 1 });
            matrice = randomizeLayer(matrice, (Global.RockLevel - 1), new int[3] { 1, 2, 1 });
            matrice = randomizeLayer(matrice, Global.RockLevel, new int[2] { 1, 2 });
            matrice = randomizeLayer(matrice, (Global.RockLevel + 1), new int[3] { 1, 2, 2 });
            matrice = randomizeLayer(matrice, (Global.RockLevel + 2), new int[4] { 1, 2, 2, 2 });
            //plant gras on surface
            matrice = plantGrass(matrice);

            ImageHelper.SaveMatrixAsImage(matrice, "initial_map.png");

            return matrice;
        }
        private static int[,] plantGrass(int[,] matrice)
        {
            int grassRange = 5;
            for (int x = 0; x < matrice.GetLength(1); x++)
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
        private static int[,] randomizeLayer(int[,] matrice, int layer, int[] values)
        {
            Random r = new Random();
            for (int x = 0; x < matrice.GetLength(1); x++)
            {
                if (matrice[x, layer] > 0)
                {
                    matrice[x, layer] = values[r.Next(0, values.Length)];
                }
            }
            return matrice;
        }

        private static int[,] setTilesAfterLayer(int[,] matrice, int layer, int material)
        {
            for (int xx = 0; xx < matrice.GetLength(0); xx++)
            {
                for (int yy = layer; yy < matrice.GetLength(1); yy++)
                {
                    if (matrice[xx, yy] > 0)
                    {
                        matrice[xx, yy] = material;
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
