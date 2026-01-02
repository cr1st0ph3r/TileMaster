using System.Collections.Generic;
using System.Linq;
using TileMaster.Entity;

namespace TileMaster.Map
{
    public class TileInspector
    {
        Map map;
        public TileInspector(Map map)
        {
            this.map = map;
        }

        /// <summary>
        /// retrieves the neighboring tiles from a given tile
        /// </summary>
        /// <param name="refTile"></param>
        /// <returns></returns>
        public List<Tile> GetNeighboringTiles(Tile refTile, int range = 1)
        {
            if (refTile.isEdgeTile)
            {
                return map.tileInspector.GetNeighboringTilesCrossChunk(refTile, range);
            }
            else
            {
                return map.tileInspector.GetNeighboringTilesFromSameChunk(refTile);
            }
        }

        public List<Tile> GetNeighboringTilesFromSameChunk(Tile refTile)
        {
            var neighbors = new List<Tile>
            {
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize - 1).Value,
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize).Value,
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - Global.ChunkSize + 1).Value,
                //[ ][ ][ ]
                //[x][x][ ]
                //[ ][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                refTile,
                //[ ][ ][ ]
                //[ ][x][x]
                //[ ][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(refTile.LocalId + Global.ChunkSize + 1).Value
            };

            return neighbors;
        }

        public List<Tile> GetNeighboringTilesFromSameChunk(Tile refTile, int range)
        {
            var neighbors = new List<Tile>();
            int chunkSize = Global.ChunkSize; // The chunk is assumed to be a square grid: chunkSize x chunkSize.
            int centerX = refTile.LocalId % chunkSize;
            int centerY = refTile.LocalId / chunkSize;

            // For every cell in the square of side length (2*range + 1) around the reference tile:
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                for (int xOffset = -range; xOffset <= range; xOffset++)
                {
                    int newX = centerX + xOffset;
                    int newY = centerY + yOffset;

                    // Ensure the new coordinates are within the bounds of the chunk.
                    if (newX >= 0 && newX < chunkSize && newY >= 0 && newY < chunkSize)
                    {
                        int neighborLocalId = newY * chunkSize + newX;

                        // Retrieve the tile from the chunk dictionary using the calculated local index.
                        //if (map.ChunkDictionary[refTile.ChunkId].Tiles.TryGetValue(neighborLocalId, out CollisionTiles neighborTile))
                        //{
                        //    neighbors.Add(neighborTile);
                        //}
                        neighbors.Add(map.ChunkDictionary[refTile.ChunkId].Tiles.ElementAt(neighborLocalId).Value);
                    }
                }
            }

            return neighbors;
        }

        public List<Tile> GetNeighboringTilesCrossChunk(Tile refTile)
        {
            var neighbors = new List<Tile>
            {
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth - 1).Value,
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth).Value,
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId - Global.MapWidth + 1).Value,
                //[ ][ ][ ]
                //[x][x][ ]
                //[ ][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                refTile,
                //[ ][ ][ ]
                //[ ][x][x]
                //[ ][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId + 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth - 1).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                map.MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth).Value,
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                map.MapDictionary.ElementAt(refTile.GlobalId + Global.MapWidth + 1).Value
            };

            //the info we need is within the chunk dictionary, so we need to convert the global ids to local ids
            for (var i = 0; i < neighbors.Count; i++)
            {
                neighbors[i] = map.ChunkDictionary[neighbors[i].ChunkId].Tiles[neighbors[i].GlobalId];
            }
            return neighbors;
        }

        /// <summary>
        /// Retrieves a list of neighboring tiles around a reference tile within a specified range,
        /// handling cross-chunk tile retrieval.
        /// </summary>
        /// <param name="refTile">The reference tile.</param>
        /// <param name="range">The range to check for neighbors (e.g., 1 for immediate neighbors, 2 for a 5x5 area).</param>
        /// <returns>A list of neighboring tiles, including the reference tile, within the specified range.</returns>
        public List<Tile> GetNeighboringTilesCrossChunk(Tile refTile, int range = 1)
        {
            var neighbors = new List<Tile>();

            // Calculate the row and column of the reference tile
            int refRow = refTile.GlobalId / Global.MapWidth;
            int refCol = refTile.GlobalId % Global.MapWidth;

            for (int rowOffset = -range; rowOffset <= range; rowOffset++)
            {
                for (int colOffset = -range; colOffset <= range; colOffset++)
                {
                    int neighborRow = refRow + rowOffset;
                    int neighborCol = refCol + colOffset;

                    // Check for valid map boundaries
                    if (neighborRow >= 0 && neighborRow < Global.MapHeight &&
                        neighborCol >= 0 && neighborCol < Global.MapWidth)
                    {
                        int neighborGlobalId = neighborRow * Global.MapWidth + neighborCol;

                        if (map.MapDictionary.TryGetValue(neighborGlobalId, out CollisionTiles neighborTile))
                        {
                            neighbors.Add(neighborTile);
                        }
                    }
                }
            }

            return neighbors;
        }
    }
}

