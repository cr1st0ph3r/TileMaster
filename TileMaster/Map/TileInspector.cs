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
        public List<Tile> GetNeighboringTiles__(Tile refTile, int range = 1)
        {
            var neighbors = new List<Tile>
            {                
                //[x][ ][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.GetTileAt(refTile.X-1,refTile.Y-1),
                //[ ][x][ ]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.GetTileAt(refTile.X,refTile.Y-1),
                //[ ][ ][x]
                //[ ][x][ ]
                //[ ][ ][ ]
                map.GetTileAt(refTile.X-1,refTile.Y),
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
                map.GetTileAt(refTile.X+1,refTile.Y),
                //[ ][ ][ ]
                //[ ][x][ ]
                //[x][ ][ ]
                map.GetTileAt(refTile.X-1,refTile.Y+1),
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][x][ ]
                map.GetTileAt(refTile.X+1,refTile.Y),
                //[ ][ ][ ]
                //[ ][x][ ]
                //[ ][ ][x]
                map.GetTileAt(refTile.X+1,refTile.Y+1),
            };
            return neighbors;
        }

        public List<Tile> GetNeighboringTiles(Tile refTile, int range = 1)
        {
            var neighbors = new List<Tile>();

            if (refTile == null || range < 1)
                return neighbors;

            // Ensure we always return a (2*range+1)^2 list in row-major order (top-left -> bottom-right).
            // Out-of-bounds positions are returned as an "Air" placeholder tile so callers can index safely.
            for (int dy = -range; dy <= range; dy++)
            {
                for (int dx = -range; dx <= range; dx++)
                {
                    // center: keep the actual reference tile so callers relying on identity still work
                    if (dx == 0 && dy == 0)
                    {
                        neighbors.Add(refTile);
                        continue;
                    }

                    var tile = map.GetTileAt(refTile.X + dx, refTile.Y + dy);
                    if (tile != null)
                    {
                        neighbors.Add(tile);
                    }
                    else
                    {
                        // create a lightweight placeholder representing "Air" for out-of-map tiles
                        var air = new CollisionTiles()
                        {
                            TileId = (int)TileType.Air,
                            IsSolid = false,
                            GlobalId = -1,
                            ChunkId = refTile.ChunkId,
                            X = refTile.X + dx,
                            Y = refTile.Y + dy,
                            TextureName = "Air"
                        };
                        neighbors.Add(air);
                    }
                }
            }

            return neighbors;
        }
    }
}