
using System;

namespace TileMaster.Util
{
    public class Noise
    {

        public static int[,] RandomWalkTop(int[,] map, float seed)
        {
            //Seed our random
            System.Random rand = new System.Random(seed.GetHashCode());

            //Set our starting height
            //int lastHeight = rand.Next(0, map.GetUpperBound(1));
            int lastHeight = Global.GroundLevel;

            //Cycle through our width
            for (int x = 0; x < map.GetUpperBound(0); x++)
            {
                //Flip a coin
                int nextMove = rand.Next(2);

                //If heads, and we aren't near the bottom, minus some height
                if (nextMove == 0 && lastHeight > 2)
                {
                    lastHeight--;
                }
                //If tails, and we aren't near the top, add some height
                else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) - 2)
                {
                    lastHeight++;
                }

                //Circle through from the lastheight to the bottom
                for (int y = lastHeight; y >= 0; y--)
                {
                    map[y,x] = 0;
                }
            }
            //Return the map
            return map;
        }
        public static int[,] RandomWalkTopSmoothed(int[,] map, float seed, int minSectionWidth,int maxSectionWidth)
        {
            //Seed our random
            System.Random rand = new System.Random(seed.GetHashCode());
      
            //Determine the start position
            int lastHeight = Global.GroundLevel-1;

            //Used to determine which direction to go
            int nextMove = 0;
            //Used to keep track of the current sections width
            int sectionWidth = 0;

            //Work through the array width
            var xlimit = map.GetUpperBound(1);
            for (int x = 0; x <= xlimit; x++)
            {
                var sectionWidthRef = rand.Next(minSectionWidth, maxSectionWidth);
                //Determine the next move
                nextMove = rand.Next(2);

                //Only change the height if we have used the current height more than the minimum required section width
                if (nextMove == 0 && lastHeight > 0 && sectionWidth > sectionWidthRef)
                {
                    lastHeight--;
                    sectionWidth = 0;
                }
                else if (nextMove == 1 && lastHeight < map.GetUpperBound(1) && sectionWidth > sectionWidthRef)
                {
                    lastHeight++;
                    sectionWidth = 0;
                }
                //Increment the section width
                sectionWidth++;

                //Work our way from the height down to 0
                for (int y = lastHeight; y >= 0; y--)
                {
                    //map[x, y] = 1;
                    map[y,x] = 0;

                }
            }

            //Return the modified map
            return map;
        }

        public static int[,] RandomWalkCave(int[,] map, float seed, int requiredFloorPercent)
        {
            //Seed our random
            System.Random rand = new System.Random(seed.GetHashCode());

            //Define our start x position
            int floorX = rand.Next(1, map.GetUpperBound(0) - 1);
            //Define our start y position
            int floorY = rand.Next(1, map.GetUpperBound(1) - 1);
            //Determine our required floorAmount
            int reqFloorAmount = ((map.GetUpperBound(1) * map.GetUpperBound(0)) * requiredFloorPercent) / 100;
            //Used for our while loop, when this reaches our reqFloorAmount we will stop tunneling
            int floorCount = 0;

            //Set our start position to not be a tile (0 = no tile, 1 = tile)
            map[floorX, floorY] = 0;
            //Increase our floor count
            floorCount++;

            int missCount = 0;
            while (floorCount < reqFloorAmount)
            {
                if (missCount > 40)
                {
                    floorCount++;
                    missCount = 0;
                }

                //Determine our next direction
                int randDir = rand.Next(4);

                switch (randDir)
                {
                    //Up
                    case 0:
                        //Ensure that the edges are still tiles
                        if ((floorY + 1) < map.GetUpperBound(1) - 1)
                        {
                            //Move the y up one
                            floorY++;

                            //Check if that piece is currently still a tile
                            if (map[floorX, floorY] > 0)
                            {
                                //Change it to not a tile
                                map[floorX, floorY] = 0;
                                //Increase floor count
                                floorCount++;
                            }
                            else
                            {
                                missCount++;
                            }
                        }
                        break;
                    //Down
                    case 1:
                        //Ensure that the edges are still tiles
                        if ((floorY - 1) > 1)
                        {
                            //Move the y down one
                            floorY--;
                            //Check if that piece is currently still a tile
                            if (map[floorX, floorY] > 0)
                            {
                                //Change it to not a tile
                                map[floorX, floorY] = 0;
                                //Increase the floor count
                                floorCount++;
                            }
                            else
                            {
                                missCount++;
                            }
                        }
                        break;
                    //Right
                    case 2:
                        //Ensure that the edges are still tiles
                        if ((floorX + 1) < map.GetUpperBound(0) - 1)
                        {
                            //Move the x to the right
                            floorX++;
                            //Check if that piece is currently still a tile
                            if (map[floorX, floorY] > 0)
                            {
                                //Change it to not a tile
                                map[floorX, floorY] = 0;
                                //Increase the floor count
                                floorCount++;
                            }
                            else
                            {
                                missCount++;
                            }
                        }
                        break;
                    //Left
                    case 3:
                        //Ensure that the edges are still tiles
                        if ((floorX - 1) > 1)
                        {
                            //Move the x to the left
                            floorX--;
                            //Check if that piece is currently still a tile
                            if (map[floorX, floorY] > 0)
                            {
                                //Change it to not a tile
                                map[floorX, floorY] = 0;
                                //Increase the floor count
                                floorCount++;
                            }
                            else
                            {
                                missCount++;
                            }
                        }
                        break;
                }
            }
            //Return the updated map
            return map;
        }

        public static int[,] DirectionalTunnel(int[,] map, int minPathWidth, int maxPathWidth, int maxPathChange, int roughness, int curvyness)
        {
            //This value goes from its minus counterpart to its positive value, in this case with a width value of 1, the width of the tunnel is 3
            int tunnelWidth = 1;
            //Set the start X position to the center of the tunnel
            int x = map.GetUpperBound(0) / 2;

            //Set up our random with the seed
            System.Random rand = new System.Random(DateTime.Now.GetHashCode());

            //Create the first part of the tunnel
            for (int i = -tunnelWidth; i <= tunnelWidth; i++)
            {
                map[x + i, 0] = 0;
            }
            //Cycle through the array
            for (int y = 1; y < map.GetUpperBound(1); y++)
            {
                //Check if we can change the roughness
                if (rand.Next(0, 100) > roughness)
                {
                    //Get the amount we will change for the width
                    //int widthChange = Random.Range(-maxPathWidth, maxPathWidth);
                    int widthChange = rand.Next(-maxPathWidth, maxPathWidth);
                    //Add it to our tunnel width value
                    tunnelWidth += widthChange;
                    //Check to see we arent making the path too small
                    if (tunnelWidth < minPathWidth)
                    {
                        tunnelWidth = minPathWidth;
                    }
                    //Check that the path width isnt over our maximum
                    if (tunnelWidth > maxPathWidth)
                    {
                        tunnelWidth = maxPathWidth;
                    }
                }

                //Check if we can change the curve
                if (rand.Next(0, 100) > curvyness)
                {
                    //Get the amount we will change for the x position
                    int xChange = rand.Next(-maxPathWidth, maxPathWidth);
                    //Add it to our x value
                    x += xChange;
                    //Check we arent too close to the left side of the map
                    if (x < maxPathWidth)
                    {
                        x = maxPathWidth;
                    }
                    //Check we arent too close to the right side of the map
                    if (x > (map.GetUpperBound(0) - maxPathWidth))
                    {
                        x = map.GetUpperBound(0) - maxPathWidth;
                    }
                }

                //Work through the width of the tunnel
                for (int i = -tunnelWidth; i <= tunnelWidth; i++)
                {
                    map[x + i, y] = 0;
                }
            }
            return map;
        }

        public static int[,] GenerateCellularAutomata(int[,] map,int startingDepth, float seed, int fillPercent, bool edgesAreWalls)
        {
            //Seed our random number generator
            System.Random rand = new System.Random(seed.GetHashCode());

            //Initialise the map
           
            for (int x = startingDepth; x < map.GetUpperBound(0); x++)
            {
                for (int y = 0; y < map.GetUpperBound(1); y++)
                {
                    //If we have the edges set to be walls, ensure the cell is set to on (1)
                    if (edgesAreWalls && (x == 0 || x == map.GetUpperBound(0) - 1 || y == 0 || y == map.GetUpperBound(1) - 1))
                    {
                        map[x, y] = 1;
                    }
                    else
                    {
                        //Randomly generate the grid
                        map[x, y] = (rand.Next(0, 100) < fillPercent) ? 1 : 0;
                    }
                }
            }
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
        public static int[,] SmoothMooreCellularAutomata(int[,] map,int startingDepth, bool edgesAreWalls, int smoothCount)
        {
            for (int i = 0; i < smoothCount; i++)
            {
                for (int x = startingDepth; x < map.GetUpperBound(0); x++)
                {
                    for (int y = 0; y < map.GetUpperBound(1); y++)
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

        public static int[,] GenerateCaves(int[,] map, int startingDepth, float seed, int fillPercent, bool edgesAreWalls,int smoothCount) { 
        
            map = GenerateCellularAutomata(map,startingDepth,seed,fillPercent,edgesAreWalls);
            map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls, smoothCount);
            return map;
        }

        public static int[,] GenerateMineral(int[,] map, int startingDepth, float seed, int fillPercent, bool edgesAreWalls, int smoothCount)
        {

            map = GenerateCellularAutomata(map, startingDepth, seed, fillPercent, edgesAreWalls);
            map = SmoothMooreCellularAutomata(map, startingDepth, edgesAreWalls, smoothCount);
            return map;
        }
    }
}
